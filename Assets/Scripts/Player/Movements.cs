using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class Movement : NetworkBehaviour
{
    private Rigidbody2D body;
    private Animator animator;
    private BoxCollider2D boxCollider2D;
    private float wallJumpCooldown;
    private float horizontalInput;
    private bool canMove = true;
    private PlayerRespawn respawnHandler;

    // Fall detection
    private float lastGroundY;
    private float fallDistance;
    private bool wasGroundedLastFrame;
    private bool pendingFallDeath; // True when player has fallen too far

    [Header("Layers")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask steppableObjectLayer;
    [SerializeField] private LayerMask wallLayer;

    [Header("Movement")]
    [SerializeField] private float speed;
    [SerializeField] private float jumpPower;

    [Header("Jump Adjustment")]
    [SerializeField] private float shortJumpMultiplier;
    [SerializeField] private float shortHopCut;
    [SerializeField] private float fallMultiplier;
    [SerializeField] private float fallThreshold = 7f; // Die if fall > 4 units
    [SerializeField] private float fatalFallSpeed = 15f;
    
    public bool IsPushing { get; set; }


    private void Start()
    {
        if (!IsOwner) return;

        CameraMovement cam = FindFirstObjectByType<CameraMovement>();

        if (cam != null)
        {
            cam.setTarget(transform);
        }
    }

    public override void OnNetworkSpawn()
    {
        body.simulated = IsOwner;
        body.interpolation = IsOwner ? RigidbodyInterpolation2D.Interpolate : RigidbodyInterpolation2D.None;

        if (isGrounded())
            lastGroundY = transform.position.y;
    }


    private void Awake()
    {
        // Get reference of Rigidbody2D and Animator
        body = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        boxCollider2D = GetComponent<BoxCollider2D>();
        respawnHandler = GetComponent<PlayerRespawn>();

        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
    }
    private void Update()
    {
        if (!IsOwner) return;

        bool groundedNow = isGrounded();

        // --- Update last grounded Y ---
        if (groundedNow && !wasGroundedLastFrame)
        {
            if (pendingFallDeath || Mathf.Abs(body.linearVelocityY) > fatalFallSpeed)
            {
                RequestDieServerRpc();
                pendingFallDeath = false;
                return;
            }

            lastGroundY = transform.position.y;
        }

        // --- Detect if falling past threshold ---
        if (!groundedNow)
        {
            fallDistance = lastGroundY - transform.position.y;
            if (fallDistance >= fallThreshold)
                pendingFallDeath = true;
        }

        wasGroundedLastFrame = groundedNow;
        
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // Buat Flip Characternya
        if (horizontalInput > 0.01f)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (horizontalInput < -0.01f)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }


        if (wallJumpCooldown > 0.2f && Input.GetKeyDown(KeyCode.Space))
        {
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
        if (!IsOwner) return;

        if (canMove && !IsPushing)
        {
            body.linearVelocity = new Vector2(horizontalInput * speed, body.linearVelocity.y);
        }
    }


    private void Jump()
    {
        if (IsPushing) return;

        if (isGrounded() || isOnSteppableObject())
        {
            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpPower);
        }
    }

    private void HandleAirbornePhysics()
    {
        if (Input.GetKeyUp(KeyCode.Space) && body.linearVelocity.y > 0f)
            body.linearVelocity = new Vector2(body.linearVelocity.x, body.linearVelocity.y * shortHopCut);

        if (!isGrounded())
        {
            if (body.linearVelocity.y < 0f)
            {
                body.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.deltaTime;
            }
            else if (body.linearVelocity.y > 0f && !Input.GetKey(KeyCode.Space))
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
        
        animator.SetBool("run", horizontalInput != 0 && !IsPushing);
        animator.SetBool("grounded", isGrounded());
    }

    // --- UPDATED ---
    public bool isGrounded()
    {
        // Use a slightly longer distance for the cast to handle slopes gracefully.
        float extraHeight = 0.25f; 
        RaycastHit2D raycastHit = Physics2D.BoxCast(boxCollider2D.bounds.center, boxCollider2D.bounds.size, 0f, Vector2.down, extraHeight, groundLayer);

        // For debugging, draw the box in the Scene view to visualize the ground check
        Color rayColor = (raycastHit.collider != null) ? Color.green : Color.red;
        
        // Draw the four sides of the box
        Vector3 center = boxCollider2D.bounds.center;
        Vector3 size = boxCollider2D.bounds.size;
        Vector3 bottomCenter = center + Vector3.down * (size.y / 2);
        Vector3 boxBottom = center + Vector3.down * (size.y/2 + extraHeight);

        Debug.DrawLine(bottomCenter + new Vector3(-size.x/2, 0), boxBottom + new Vector3(-size.x/2, 0), rayColor); // Left side
        Debug.DrawLine(bottomCenter + new Vector3(size.x/2, 0), boxBottom + new Vector3(size.x/2, 0), rayColor);   // Right side
        Debug.DrawLine(boxBottom + new Vector3(-size.x/2, 0), boxBottom + new Vector3(size.x/2, 0), rayColor);    // Bottom side


        return raycastHit.collider != null;
    }

    private bool isOnSteppableObject()
    {
        RaycastHit2D raycastHit = Physics2D.BoxCast(boxCollider2D.bounds.center, boxCollider2D.bounds.size, 0, Vector2.down, 0.1f, steppableObjectLayer);
        return raycastHit.collider != null;
    }

    [ServerRpc]
    private void RequestDieServerRpc(ServerRpcParams rpcParams = default)
    {
        DieClientRpc();
    }

    [ClientRpc]
    private void DieClientRpc(ClientRpcParams rpcParams = default)
    {
        Debug.Log($"{gameObject.name} died (client sync)");
        animator.SetTrigger("die");
        body.linearVelocity = Vector2.zero;
        body.simulated = false;

        if (IsOwner)
        {
            this.enabled = false;
            Invoke(nameof(HandleRespawn), 0.1f);
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
    }
}

