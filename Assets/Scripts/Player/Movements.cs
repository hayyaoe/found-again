using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class Movement : MonoBehaviour
{
  private Rigidbody2D body;
  private Animator animator;
  private BoxCollider2D boxCollider2D;
  private PlayerRespawn respawnHandler;
  private PlayerPushPull pushPull;

  [Header("Input System")]
  [SerializeField] private PlayerInput playerInput; // Each player prefab should have its own PlayerInput
  private InputAction moveAction;
  private InputAction jumpAction;

  [Header("Movement")]
  [SerializeField] private float speed = 5f;
  [SerializeField] private float jumpPower = 10f;

  [Header("Jump Adjustment")]
  [SerializeField] private float shortJumpMultiplier = 1.5f;
  [SerializeField] private float shortHopCut = 0.5f;
  [SerializeField] private float fallMultiplier = 2f;

  // --- MODIFIED ---
  // We only need the 'fatalFallSpeed' now
  [Header("Fall Damage")]
  [SerializeField] private float fatalFallSpeed = 25f; // Die if landing speed is greater than
  [SerializeField] private float deathYLevel = -30f; // Die if player falls below this Y-level

  [Header("Layers")]
  [SerializeField] private LayerMask groundLayer;
  [SerializeField] private LayerMask steppableObjectLayer;

  [Header("Slope Slide (no material)")]
  [SerializeField] private LayerMask slopeGroundLayer;     // biasanya sama dgn groundLayer
  [SerializeField] private float slopeProbeDistance = 0.25f;
  [SerializeField] private float slopeFootRadius = 0.16f;
  [SerializeField] private bool alwaysSlippery = true;     // true = semua slope licin
  [SerializeField] private float minSlideAccel = 14f;      // dorongan minimum biar ga mandek
  [SerializeField] private float slideAccelBoost = 1.6f;   // boost g*sin(theta)
  [SerializeField] private float maxSlideSpeed = 24f;      // batas kecepatan meluncur
  [SerializeField] private float groundStickForce = 35f;   // tekan ke permukaan (kecil saja)
  [SerializeField] private float jumpIgnoreSlopeTime = 0.12f; // buffer setelah lompat

  [Header("Wall-slide (hampir vertikal)")]
  [SerializeField] private float wallStartAngle = 72f;         // >= ini dianggap wallish
  [SerializeField] private float wallProbeDistance = 0.25f;
  [SerializeField] private float wallSlideAccel = 22f;         // akselerasi dasar saat wall-slide
  [SerializeField] private float wallSlideBoost = 2.0f;        // pengali gravitasi (full, bukan sin)
  [SerializeField] private float wallMaxSlideSpeed = 32f;      // top speed saat wall-slide
  [SerializeField] private float minWallSpeed = 8f;            // kick kecepatan minimum
  [SerializeField] private float wallStartImpulse = 3.5f;      // kick awal bila hampir diam

  [Header("Sound Effects")]
  [SerializeField] private AudioClip jumpSFX;
  [SerializeField] private float jumpVolume = 1f;
  [SerializeField] private AudioClip[] footstepSFX; // ✅ multiple footstep clips
  [SerializeField] private float footstepVolume = 0.8f;
  [SerializeField] private float footstepInterval = 0.35f;

  // NEW ✅
  [SerializeField] private RuntimeAnimatorController mimiAnimator;
  [SerializeField] private RuntimeAnimatorController marieAnimator;

  // runtime slope state
  private bool slopeGrounded, onSlope, sliding;
  private float slopeAngle;
  private Vector2 slopeNormal = Vector2.up;
  private Vector2 slopeTangent = Vector2.right;
  private float jumpIgnoreTimer;

  // contact-based normal (lebih akurat di hampir vertikal)
  private bool contactHasSlope;
  private Vector2 contactNormal;
  private float contactAngle;

  private bool isPhysicallyGrounded;    // Is the player *actually* touching ground?
  private bool isGroundedWithLatch;     // Is the player on ground OR in coyote time?
  private bool wasPhysicallyGroundedLastFrame; // Was the player touching ground last frame?
  private float lastAirborneYVelocity; // Stores our speed just before landing

  private float horizontalInput;
  private bool canMove = true;
  private float wallJumpCooldown;
  private bool isDead = false;

  private float footstepTimer = 0f;
  private bool isWalking => Mathf.Abs(horizontalInput) > 0.01f && isGrounded();

  [SerializeField] private float groundedLatchSeconds = 0.08f;
  [Tooltip("How much to shrink the ground-check boxcast, to avoid hitting side walls.")]
  [SerializeField] private float groundCheckWidthReduction = 0.1f;
  private float groundedLatchTimer = 0f;

  [SerializeField] private float acceleration = 12f;
  [SerializeField] private float deceleration = 16f;
  [SerializeField] private float velocityPower = 0.9f;


  // private PlayerPushPull pushPull;
  private void Awake()
  {
    body = GetComponent<Rigidbody2D>();
    animator = GetComponent<Animator>();
    boxCollider2D = GetComponent<BoxCollider2D>();
    respawnHandler = GetComponent<PlayerRespawn>();
    pushPull = GetComponent<PlayerPushPull>();

    body.freezeRotation = true;
    body.interpolation = RigidbodyInterpolation2D.Interpolate;

    if (playerInput == null)
      playerInput = GetComponent<PlayerInput>();

    if (playerInput != null)
    {
      moveAction = playerInput.actions["Move"];
      jumpAction = playerInput.actions["Jump"];
    }
    else
    {
      Debug.LogWarning("⚠️ PlayerInput not assigned on " + gameObject.name);
    }
  }

  private void Start()
  {
    CheckpointManager.RegisterPlayer(this);

    // Initialize all our NEW ground-state variables
    // to reflect that the player is starting on the ground.
    // This now matches the logic in HandleRespawn().
    isPhysicallyGrounded = true;
    isGroundedWithLatch = true;
    wasPhysicallyGroundedLastFrame = true;
    groundedLatchTimer = 0f;
    lastAirborneYVelocity = 0f;
  }


  private void Update()
  {
    if (isDead || PauseMenu.GameIsPaused)
    {
      return; // Do nothing
    }

    // Check if the player has fallen below the world
    if (transform.position.y < deathYLevel)
    {
      Debug.Log("Player fell below death Y-level.");
      Die();
      return;
    }

    // --- THIS IS THE NEW CORE LOGIC ---
    // Run all ground checks ONCE at the start of the frame.
    // This updates both 'isPhysicallyGrounded' and 'isGroundedWithLatch'.
    CheckGroundedState();

    // --- Fall Damage Check (Uses the PHYSICAL state, ignores latch) ---
    if (isPhysicallyGrounded && !wasPhysicallyGroundedLastFrame)
    {
      // We just physically landed. Check our stored speed.
      if (Mathf.Abs(lastAirborneYVelocity) > fatalFallSpeed)
      {
        Debug.Log($"Landed with speed {lastAirborneYVelocity}. Dying.");
        Die();
        return; // Stop processing this frame
      }

      // ✅ Add this:
      animator.ResetTrigger("jump");
      animator.SetTrigger("land");
    }

    // --- Record Fall Speed (Uses the PHYSICAL state) ---
    if (!isPhysicallyGrounded)
    {
      // We are physically in the air, so record our speed.
      lastAirborneYVelocity = body.linearVelocity.y;
    }
    // --- END OF NEW CORE LOGIC ---

    // --- Input Reading ---
    horizontalInput = moveAction != null ? moveAction.ReadValue<Vector2>().x : Input.GetAxisRaw("Horizontal");

    bool pushingNow = pushPull != null && pushPull.isPushing;
    if (!pushingNow)
    {
      if (horizontalInput > 0.01f)
        transform.localScale = new Vector3(1, 1, 1);
      else if (horizontalInput < -0.01f)
        transform.localScale = new Vector3(-1, 1, 1);
    }

    // --- Jump (Uses the LATCHED state) ---
    if (wallJumpCooldown > 0.2f && jumpAction != null && jumpAction.WasPressedThisFrame())
    {
      var pushPull = GetComponent<PlayerPushPull>();
      if (pushPull != null && pushPull.isPushing)
        return;

      Jump(); // Jump() calls isGrounded(), which correctly uses the latch
    }
    else
    {
      wallJumpCooldown += Time.deltaTime;
    }

    // --- These functions all use the LATCHED state from isGrounded() ---
    HandleAirbornePhysics();
    HandleAnimations();
    HandleFootstepSounds();

    // --- Update the 'last frame' state (uses the PHYSICAL state) ---
    wasPhysicallyGroundedLastFrame = isPhysicallyGrounded;
  }

  private void FixedUpdate()
  {
    if (jumpIgnoreTimer > 0f)
      jumpIgnoreTimer -= Time.deltaTime;

    ProbeSlope();

    if (!canMove || isDead) return;

    bool didSlide = HandleSlopeSliding();

    bool pushingNow = pushPull != null && pushPull.isPushing;

    if (!didSlide && !pushingNow)
    {
      float targetSpeed = horizontalInput * speed;
      float speedDiff = targetSpeed - body.linearVelocity.x;

      // Choose accel rate depending on whether we're accelerating or decelerating
      float accelRate = (Mathf.Abs(targetSpeed) > 0.01f)
          ? acceleration
          : deceleration;

      // Apply acceleration curve (power < 1 makes it snappier, > 1 makes it smoother)
      float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, velocityPower) * Mathf.Sign(speedDiff);

      body.AddForce(movement * Vector2.right);

      // Optional clamp: limit X speed so you don’t overshoot
      if (Mathf.Abs(body.linearVelocity.x) > speed)
      {
        body.linearVelocity = new Vector2(Mathf.Sign(body.linearVelocity.x) * speed, body.linearVelocity.y);
      }
    }
  }

  private void Jump()
  {
    if (isPhysicallyGrounded || isOnSteppableObject())
    {
      jumpIgnoreTimer = jumpIgnoreSlopeTime;
      body.linearVelocity = new Vector2(body.linearVelocity.x, jumpPower);

      // Immediately clear latch so you can't jump again until re-grounded
      groundedLatchTimer = 0f;
      isGroundedWithLatch = false;
      isPhysicallyGrounded = false;

      if (animator) animator.SetTrigger("jump");

      // ✅ Play jump sound
      if (SoundFXManager.instance != null && jumpSFX != null)
      {
        SoundFXManager.instance.PlaySoundFXClip(jumpSFX, transform, jumpVolume);
      }
    }
  }


  private void ApplyPlayerAppearance()
  {
    if (playerInput == null) return;

    SpriteRenderer sr = GetComponent<SpriteRenderer>();
    BoxCollider2D col = GetComponent<BoxCollider2D>();
    animator = GetComponent<Animator>();

    // Detect based on prefab tag or name (case-insensitive)
    string prefabTag = gameObject.tag.ToLower();

    // Assign layers based on player index
    gameObject.layer = LayerMask.NameToLayer(
        playerInput.playerIndex == 0 ? "Player1" : "Player2"
    );

    if (prefabTag.Contains("Marie"))
    {
      // ✅ Apply Marie-specific appearance
      if (animator != null && marieAnimator != null)
        animator.runtimeAnimatorController = marieAnimator;

      col.size = new Vector2(1f, 2.8f);
      col.offset = new Vector2(0f, -0.1f);
      transform.localScale = Vector3.one;

      Debug.Log($"🎀 Applied Marie appearance for Player {playerInput.playerIndex}");
    }
    else if (prefabTag.Contains("Mimi"))
    {
      // ✅ Apply Mimi-specific appearance
      if (animator != null && mimiAnimator != null)
        animator.runtimeAnimatorController = mimiAnimator;

      col.size = new Vector2(1f, 1.55f);
      col.offset = new Vector2(0f, -0.1f);
      transform.localScale = Vector3.one;

      Debug.Log($"🐾 Applied Mimi appearance for Player {playerInput.playerIndex}");
    }
    else
    {
      Debug.LogWarning($"⚠️ Unknown prefab type for {gameObject.name}");
    }
  }

  private void HandleAirbornePhysics()
  {
    if (jumpAction != null && jumpAction.WasReleasedThisFrame() && body.linearVelocity.y > 0f)
    {
      body.linearVelocity = new Vector2(body.linearVelocity.x, body.linearVelocity.y * shortHopCut);
    }

    if (!isGrounded())
    {
      if (body.linearVelocity.y < 0f)
      {
        body.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.deltaTime;
      }
      else if (body.linearVelocity.y > 0f && (jumpAction == null || !jumpAction.IsPressed()))
      {
        body.linearVelocity += Vector2.up * Physics2D.gravity.y * (shortJumpMultiplier - 1f) * Time.deltaTime;
      }
    }
  }

  private void HandleAnimations()
  {
    if (!animator) return;

    bool groundedNow = isGrounded();
    float vy = body.linearVelocity.y;

    bool interactingNow = pushPull != null && (pushPull.isPushing || pushPull.isPulling);

    bool shouldRun = !interactingNow && Mathf.Abs(horizontalInput) > 0.01f;

    animator.SetBool("run", shouldRun);
    animator.SetBool("grounded", groundedNow);
    animator.SetFloat("yVelocity", vy);
    animator.SetBool("sliding", sliding);
    animator.SetBool("isInteracting", interactingNow);

    Debug.Log($"Grounded: {groundedNow}, vy: {vy}");

  }

  // ====== SLOPE PROBING ======
  private void ProbeSlope()
  {
    Bounds b = boxCollider2D.bounds;
    Vector2 foot = new Vector2(b.center.x, b.min.y);

    // 1) Check DOWN for slopes/ground
    RaycastHit2D hit = Physics2D.CircleCast(foot, slopeFootRadius, Vector2.down, slopeProbeDistance, slopeGroundLayer);

    if (hit.collider != null)
    {
      // --- GROUNDED PATH ---
      // We are on the ground or a slope.
      slopeGrounded = true;
      slopeNormal = hit.normal;
      slopeAngle = Vector2.Angle(slopeNormal, Vector2.up);
      onSlope = slopeAngle > 0.01f;
    }
    else
    {
      // --- AIRBORNE PATH ---
      // We are in the air. Check for walls for wall-sliding.
      var leftHit = Physics2D.CircleCast(foot, slopeFootRadius, Vector2.left, wallProbeDistance, slopeGroundLayer);
      var rightHit = Physics2D.CircleCast(foot, slopeFootRadius, Vector2.right, wallProbeDistance, slopeGroundLayer);

      RaycastHit2D wallHit = default;
      if (leftHit.collider && rightHit.collider) wallHit = leftHit.distance <= rightHit.distance ? leftHit : rightHit;
      else if (leftHit.collider) wallHit = leftHit;
      else if (rightHit.collider) wallHit = rightHit;

      if (wallHit.collider != null)
      {
        // We are touching a wall, BUT WE ARE NOT GROUNDED.
        slopeGrounded = false; // <-- This is the critical fix
        slopeNormal = wallHit.normal;
        slopeAngle = Vector2.Angle(slopeNormal, Vector2.up);
        onSlope = slopeAngle > 0.01f;
      }
      else
      {
        // We are in the air, not touching anything
        slopeGrounded = false;
        onSlope = false;
        slopeAngle = 0f;
        slopeNormal = Vector2.up;
      }
    }

    // 3) Override with physical contact (for sharp corners)
    // This is stronger than the raycasts.
    if (contactHasSlope)
    {
      slopeGrounded = true;
      slopeNormal = contactNormal;
      slopeAngle = contactAngle;
      onSlope = slopeAngle > 0.01f;
    }

    // Calculate tangent for sliding
    Vector2 t = new Vector2(slopeNormal.y, -slopeNormal.x);
    Vector2 g = Physics2D.gravity * body.gravityScale;
    if (Vector2.Dot(t, g) < 0f) t = -t;
    slopeTangent = t.normalized;

    // Check for sliding conditions
    // We are sliding if we are physically on a slope OR if we are on a steep wall
    bool slidingOnGround = slopeGrounded && (alwaysSlippery ? onSlope : (onSlope && slopeAngle > 45f));
    bool slidingOnWall = !slopeGrounded && onSlope && slopeAngle >= wallStartAngle;
    sliding = slidingOnGround || slidingOnWall;
  }

  // ====== SLIDE PHYSICS ======
  private bool HandleSlopeSliding()
  {
    bool rising = body.linearVelocity.y > 0.05f;
    if (!slopeGrounded || !onSlope || rising || jumpIgnoreTimer > 0f)
      return false;

    // Hilangkan komponen normal → anti-friction
    Vector2 v = body.linearVelocity;
    float vN = Vector2.Dot(v, slopeNormal);
    v -= vN * slopeNormal;

    float gmag = Physics2D.gravity.magnitude * body.gravityScale;
    float theta = slopeAngle * Mathf.Deg2Rad;
    float gAlong = gmag * Mathf.Sin(theta);

    bool wallish = slopeAngle >= wallStartAngle;

    float accel, vmax, vTan;
    if (wallish)
    {
      // WALL-SLIDE: pakai full gravity (atau lebih), + kick awal bila hampir diam
      accel = Mathf.Max(wallSlideAccel, gmag * wallSlideBoost);
      vmax = wallMaxSlideSpeed;

      vTan = Vector2.Dot(v, slopeTangent);
      if (Mathf.Abs(vTan) < minWallSpeed)
      {
        float signDown = Mathf.Sign(Vector2.Dot(slopeTangent, Physics2D.gravity));
        vTan = signDown * Mathf.Max(minWallSpeed, wallStartImpulse);
      }
      vTan += accel * Time.fixedDeltaTime;
      vTan = Mathf.Clamp(vTan, -vmax, vmax);
      v = slopeTangent * vTan;

      // penting: tidak memodif posisi maupun memberi gaya ke normal
    }
    else
    {
      // FLOOR-SLIDE: masih gunakan sin(theta) tapi diboost
      accel = Mathf.Max(minSlideAccel, gAlong * slideAccelBoost);
      vmax = maxSlideSpeed;

      vTan = Vector2.Dot(v, slopeTangent);
      vTan += accel * Time.fixedDeltaTime;
      vTan = Mathf.Clamp(vTan, -vmax, vmax);
      v = slopeTangent * vTan;

      if (groundStickForce > 0f)
        body.AddForce(-slopeNormal * groundStickForce, ForceMode2D.Force);
    }

    body.linearVelocity = v;
    return true;
  }

  // ====== Collision normals (prioritas tinggi di hampir vertikal) ======
  private void OnCollisionStay2D(Collision2D c)
  {
    // Check if we even collided with the ground layer
    if (((1 << c.collider.gameObject.layer) & slopeGroundLayer) == 0)
    {
      // We didn't hit the ground, so we are definitely not on a contact slope.
      // This is important to reset the flag if we *only* touch a wall.
      contactHasSlope = false;
      return;
    }

    float bestAngle = 91f; // Start with an invalid angle (steeper than any wall)
    Vector2 bestNormal = Vector2.up;
    bool foundValidContact = false;

    // Loop through all points of contact
    for (int i = 0; i < c.contactCount; i++)
    {
      var contact = c.GetContact(i);

      // --- THIS IS THE NEW CHECK ---
      // Only consider contacts that are on the BOTTOM HALF of our collider
      if (contact.point.y < boxCollider2D.bounds.center.y)
      {
        float ang = Vector2.Angle(contact.normal, Vector2.up);

        // Find the "flattest" surface we are touching
        if (ang < bestAngle)
        {
          bestAngle = ang;
          bestNormal = contact.normal;
          foundValidContact = true;
        }
      }
    }

    // --- UPDATED FIX ---
    // Now, check if the "flattest" surface we found is actually a floor
    if (foundValidContact && bestAngle < wallStartAngle)
    {
      // Yes, this is a valid floor or walkable slope
      contactHasSlope = true;
      contactNormal = bestNormal;
      contactAngle = bestAngle;
    }
    else
    {
      // No, this is a wall brush or a side-only collision. Ignore it.
      contactHasSlope = false;
    }
  }

  private void OnCollisionExit2D(Collision2D c)
  {
    if (((1 << c.collider.gameObject.layer) & slopeGroundLayer) == 0) return;
    contactHasSlope = false;
  }

  private bool isGrounded()
  {
    // This function now just returns the result from CheckGroundedState()
    return isGroundedWithLatch;
  }

  private void CheckGroundedState()
  {
    // 1. Perform all physical checks
    Vector2 groundCheckSize = boxCollider2D.bounds.size;
    groundCheckSize.x -= groundCheckWidthReduction;

    bool hitGround = Physics2D.BoxCast(
        boxCollider2D.bounds.center,
        groundCheckSize,
        0f, Vector2.down, 0.1f, groundLayer
    ).collider != null;

    bool onSteppable = Physics2D.BoxCast(
        boxCollider2D.bounds.center,
        groundCheckSize,
        0f, Vector2.down, 0.1f, steppableObjectLayer
    ).collider != null;

    bool slopeAsGround = slopeGrounded; // From ProbeSlope()

    // 2. Update our "physical" state
    isPhysicallyGrounded = hitGround || onSteppable || slopeAsGround;

    // 3. Apply the "coyote time" latch
    if (isPhysicallyGrounded)
    {
      groundedLatchTimer = groundedLatchSeconds;
    }
    else
    {
      groundedLatchTimer = Mathf.Max(0f, groundedLatchTimer - Time.deltaTime);
    }

    // 4. Update our "gameplay feel" state
    isGroundedWithLatch = isPhysicallyGrounded || groundedLatchTimer > 0f;
  }


  private bool isOnSteppableObject()
  {
    Vector2 groundCheckSize = boxCollider2D.bounds.size;
    groundCheckSize.x -= groundCheckWidthReduction;

    RaycastHit2D raycastHit = Physics2D.BoxCast(
        boxCollider2D.bounds.center,
        groundCheckSize,
        0,
        Vector2.down,
        0.1f,
        steppableObjectLayer
    );
    return raycastHit.collider != null;
  }

  public void DieAndRespawn()
  {
    // Ensure this logic only runs once
    if (isDead) return;

    isDead = true;
    Debug.Log($"{gameObject.name} died (local)");

    // --- NEW ---
    // Force detach from any object before dying
    if (pushPull != null)
    {
      pushPull.ForceDetach();
    }

    animator.SetTrigger("die");
    body.linearVelocity = Vector2.zero;
    body.simulated = false;

    this.enabled = false;
    Invoke(nameof(HandleRespawn), 0.1f);
  }

  public void Die()
  {
    // Instead of dying locally, tell the manager to reset everything
    if (!isDead) // Prevent this from being called 100 times
    {
      CheckpointManager.instance.TriggerFullRespawn();
    }
  }

  private void HandleRespawn()
  {
    body.simulated = true;
    this.enabled = true;
    animator.ResetTrigger("die");

    if (respawnHandler != null)
      respawnHandler.Respawn();
    else
      Debug.LogWarning("⚠️ PlayerRespawn component missing on player!");

    isDead = false;

    // --- UPDATED RESET LOGIC ---
    // Force-reset all landing state variables
    wasPhysicallyGroundedLastFrame = true;
    isPhysicallyGrounded = true;
    isGroundedWithLatch = true;
    groundedLatchTimer = 0f;
    lastAirborneYVelocity = 0f;
  }

  private void HandleFootstepSounds()
  {
    // Don't play footsteps if dead or not grounded
    if (!isWalking || isDead) return;

    // Countdown timer
    footstepTimer -= Time.deltaTime;

    if (footstepTimer <= 0f)
    {
      footstepTimer = footstepInterval;

      // Pick a random footstep sound
      if (footstepSFX != null && footstepSFX.Length > 0 && SoundFXManager.instance != null)
      {
        AudioClip randomStep = footstepSFX[Random.Range(0, footstepSFX.Length)];
        SoundFXManager.instance.PlaySoundFXClip(randomStep, transform, footstepVolume);
      }
    }
  }

  private void OnDestroy()
  {
    CheckpointManager.UnregisterPlayer(this);
  }

}