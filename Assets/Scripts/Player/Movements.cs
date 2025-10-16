using System.Collections;
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
  [SerializeField] private float fallThreshold = 7f; // Die if fall > threshold units
  [SerializeField] private float fatalFallSpeed = 15f;

  [Header("Layers")]
  [SerializeField] private LayerMask groundLayer;
  [SerializeField] private LayerMask steppableObjectLayer;

  // --- Fall detection ---
  private float lastGroundY;
  private float fallDistance;
  private bool wasGroundedLastFrame;
  private bool pendingFallDeath;

  private float horizontalInput;
  private bool canMove = true;
  private float wallJumpCooldown;

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
    {
      cameraMovement.setTarget(transform);
    }
    
    if (isGrounded())
      lastGroundY = transform.position.y;
  }

  private void Update()
  {
    bool groundedNow = isGrounded();

    // --- Fall Detection ---
    if (groundedNow && !wasGroundedLastFrame)
    {
      if (pendingFallDeath || Mathf.Abs(body.linearVelocityY) > fatalFallSpeed)
      {
        Die();
        pendingFallDeath = false;
        return;
      }

      lastGroundY = transform.position.y;
    }

    if (!groundedNow)
    {
      fallDistance = lastGroundY - transform.position.y;
      if (fallDistance >= fallThreshold)
        pendingFallDeath = true;
    }

    wasGroundedLastFrame = groundedNow;

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
      // Prevent jump if pushing/pulling
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
  }

  private void FixedUpdate()
  {
    if (!canMove) return;

    body.linearVelocity = new Vector2(horizontalInput * speed, body.linearVelocity.y);
  }

  private void Jump()
  {
    if (isGrounded() || isOnSteppableObject())
    {
      body.linearVelocity = new Vector2(body.linearVelocity.x, jumpPower);
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

  private void Die()
  {
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
  }
}