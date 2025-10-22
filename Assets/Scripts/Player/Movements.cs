using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
  private Rigidbody2D body;
  private Animator animator;
  private BoxCollider2D boxCollider2D;
  private PlayerRespawn respawnHandler;

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

  // --- We only need 'wasGroundedLastFrame' for fall detection ---
  private bool wasGroundedLastFrame;

  private float horizontalInput;
  private bool canMove = true;
  private float wallJumpCooldown;
  private bool isDead = false;

  private float footstepTimer = 0f;
  private bool isWalking => Mathf.Abs(horizontalInput) > 0.01f && isGrounded();

  private void Awake()
  {
    body = GetComponent<Rigidbody2D>();
    animator = GetComponent<Animator>();
    boxCollider2D = GetComponent<BoxCollider2D>();
    respawnHandler = GetComponent<PlayerRespawn>();

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
    CameraMovement cameraMovement = FindObjectOfType<CameraMovement>();
    if (cameraMovement != null)
      cameraMovement.setTarget(transform);

    if (isGrounded())
      wasGroundedLastFrame = true;

    ApplyPlayerAppearance();
  }


  private void Update()
  {
    // --- REMOVED ---
    // The Y-level death check is gone.

    if (isDead) return; // Don't do anything else if dead

    bool groundedNow = isGrounded();

    // --- THIS IS THE NEW FALL DAMAGE LOGIC ---
    if (groundedNow && !wasGroundedLastFrame)
    {
      // We just landed. Check our vertical speed.
      // body.linearVelocity.y will be a large negative number.
      // We use Mathf.Abs() to make it positive for the check.
      if (Mathf.Abs(body.linearVelocity.y) > fatalFallSpeed)
      {
        Debug.Log(Mathf.Abs(body.linearVelocity.y));
        Die();
        return; // Stop processing this frame
      }
    }
    wasGroundedLastFrame = groundedNow;
    // --- END OF NEW LOGIC ---

    // --- Input Reading ---
    horizontalInput = moveAction != null ? moveAction.ReadValue<Vector2>().x : Input.GetAxisRaw("Horizontal");

    // Flip sprite
    if (horizontalInput > 0.01f)
      transform.localScale = new Vector3(1, 1, 1);
    else if (horizontalInput < -0.01f)
      transform.localScale = new Vector3(-1, 1, 1);

    // Jump
    if (wallJumpCooldown > 0.2f && jumpAction != null && jumpAction.WasPressedThisFrame())
    {
      var pushPull = GetComponent<PlayerPushPull>();
      if (pushPull != null && pushPull.isPushing)
        return;

      Jump();
    }
    else
    {
      wallJumpCooldown += Time.deltaTime;
    }

    HandleAirbornePhysics();
    HandleAnimations();
    HandleFootstepSounds();
  }

  private void FixedUpdate()
  {
    if (jumpIgnoreTimer > 0f)
      jumpIgnoreTimer -= Time.deltaTime;

    ProbeSlope();

    if (!canMove || isDead) return; // Don't move if dead

    // Saat sliding, jangan timpa velocity.x
    bool didSlide = HandleSlopeSliding();
    if (!didSlide)
      body.linearVelocity = new Vector2(horizontalInput * speed, body.linearVelocity.y);
  }

  private void Jump()
  {
    if (isGrounded() || isOnSteppableObject())
    {
      jumpIgnoreTimer = jumpIgnoreSlopeTime;
      body.linearVelocity = new Vector2(body.linearVelocity.x, jumpPower);

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

    switch (playerInput.playerIndex)
    {
      case 0: // Player 1
        sr.sprite = Resources.Load<Sprite>("Marie 1");
        gameObject.layer = LayerMask.NameToLayer("Player1");

        transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        col.size = new Vector2(1f, 2.8f);
        col.offset = new Vector2(0f, -0.1f);
        break;

      case 1: // Player 2
        sr.sprite = Resources.Load<Sprite>("Mimi 2");
        gameObject.layer = LayerMask.NameToLayer("Player2");

        transform.localScale = new Vector3(1f, 1f, 1f);
        col.size = new Vector2(1f, 1.55f);
        col.offset = new Vector2(0f, -0.1f);
        break;
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
    if (!isGrounded())
    {
      animator.SetTrigger("jump");
    }

    animator.SetBool("run", Mathf.Abs(horizontalInput) > 0.01f);
    animator.SetBool("grounded", isGrounded());
  }

  // ====== SLOPE PROBING ======
  private void ProbeSlope()
  {
    Bounds b = boxCollider2D.bounds;
    Vector2 foot = new Vector2(b.center.x, b.min.y);

    // 1) bawah
    RaycastHit2D hit = Physics2D.CircleCast(foot, slopeFootRadius, Vector2.down, slopeProbeDistance, slopeGroundLayer);

    // 2) kiri/kanan (untuk dinding / hampir vertikal)
    if (!hit.collider)
    {
      var leftHit = Physics2D.CircleCast(foot, slopeFootRadius, Vector2.left, wallProbeDistance, slopeGroundLayer);
      var rightHit = Physics2D.CircleCast(foot, slopeFootRadius, Vector2.right, wallProbeDistance, slopeGroundLayer);
      if (leftHit.collider && rightHit.collider) hit = leftHit.distance <= rightHit.distance ? leftHit : rightHit;
      else if (leftHit.collider) hit = leftHit;
      else if (rightHit.collider) hit = rightHit;
    }

    if (hit.collider != null)
    {
      slopeGrounded = true;
      slopeNormal = hit.normal;
      slopeAngle = Vector2.Angle(slopeNormal, Vector2.up);
      onSlope = slopeAngle > 0.01f;
    }
    else
    {
      slopeGrounded = false;
      onSlope = false;
      slopeAngle = 0f;
      slopeNormal = Vector2.up;
    }

    // Override dengan data kontak fisik bila ada — lebih akurat di hampir vertikal
    if (contactHasSlope)
    {
      slopeGrounded = true;
      slopeNormal = contactNormal;
      slopeAngle = contactAngle;
      onSlope = slopeAngle > 0.01f;
    }

    // Hitung tangent menurun
    Vector2 t = new Vector2(slopeNormal.y, -slopeNormal.x);
    Vector2 g = Physics2D.gravity * body.gravityScale;
    if (Vector2.Dot(t, g) < 0f) t = -t;
    slopeTangent = t.normalized;

    sliding = slopeGrounded && (alwaysSlippery ? onSlope : (onSlope && slopeAngle > 45f));
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
    if (((1 << c.collider.gameObject.layer) & slopeGroundLayer) == 0) return;

    float bestAngle = -1f;
    Vector2 bestNormal = Vector2.up;

    for (int i = 0; i < c.contactCount; i++)
    {
      var n = c.GetContact(i).normal;
      float ang = Vector2.Angle(n, Vector2.up);
      if (ang > bestAngle) { bestAngle = ang; bestNormal = n; }
    }

    contactHasSlope = true;
    contactNormal = bestNormal;
    contactAngle = bestAngle;
  }

  private void OnCollisionExit2D(Collision2D c)
  {
    if (((1 << c.collider.gameObject.layer) & slopeGroundLayer) == 0) return;
    contactHasSlope = false;
  }

  private bool isGrounded()
  {
    RaycastHit2D raycastHit = Physics2D.BoxCast(
                  boxCollider2D.bounds.center,
                  boxCollider2D.bounds.size,
                  0,
                  Vector2.down,
                  0.1f,
                  groundLayer
              );
    return raycastHit.collider != null;
  }

  private bool isOnSteppableObject()
  {
    RaycastHit2D raycastHit = Physics2D.BoxCast(
        boxCollider2D.bounds.center,
        boxCollider2D.bounds.size,
        0,
        Vector2.down,
        0.1f,
        steppableObjectLayer
    );
    return raycastHit.collider != null;
  }

  public void Die()
  {
    if (isDead) return;

    isDead = true;
    Debug.Log($"{gameObject.name} died (local)");

    animator.SetTrigger("die");
    body.linearVelocity = Vector2.zero;
    body.simulated = false;

    this.enabled = false;
    Invoke(nameof(HandleRespawn), 0.1f);
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
}