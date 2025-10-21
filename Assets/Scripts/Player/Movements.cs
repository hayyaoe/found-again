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
    [SerializeField] private PlayerInput playerInput;
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
    [SerializeField] private float fatalFallSpeed = 25f; // Die if landing speed is greater than this

    [Header("Layers")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask steppableObjectLayer;

    // --- We only need 'wasGroundedLastFrame' for fall detection ---
    private bool wasGroundedLastFrame;

    private float horizontalInput;
    private bool canMove = true;
    private float wallJumpCooldown;
    private bool isDead = false;

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
            wasGroundedLastFrame = true; // Set true on start if grounded

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
    }

    private void FixedUpdate()
    {
        if (!canMove || isDead) return; // Don't move if dead

        body.linearVelocity = new Vector2(horizontalInput * speed, body.linearVelocity.y);
    }

    private void Jump()
    {
        if (isGrounded() || isOnSteppableObject())
        {
            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpPower);
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
}