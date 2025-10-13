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
  private bool justWallJumped = false;
  private float noSlideTimer = 0f;

  [Header("Layers")]
  [SerializeField] private LayerMask groundLayer;
  [SerializeField] private LayerMask wallLayer;

  [Header("Movement")]
  [SerializeField] private float speed;
  [SerializeField] private float jumpPower;

  [Header("Jump Adjustment")]
  [SerializeField] private float shortJumpMultiplier;
  [SerializeField] private float shortHopCut;
  [SerializeField] private float fallMultiplier;

  [Header("Wall Jump Adjustment")]
  [SerializeField] private float wallJumpKickX;
  [SerializeField] private float wallJumpKickY;
  [SerializeField] private float wallJumpControlLock;
  [SerializeField] private float wallJumpNoSlideTime = 0.12f;

  [Header("Wall Slide Adjustment")]
  [SerializeField] private float wallCheckDistance;
  [SerializeField] private float wallSlideMaximumFallSpeed;

  private void Start()
  {
    if (!IsOwner) return;

    CameraMovement cam = FindFirstObjectByType<CameraMovement>();

    if (cam != null)
    {
      cam.setTarget(transform);
    }
  }

  private void Awake()
  {
    // Get reference of Rigidbody2D and Animator
    body = GetComponent<Rigidbody2D>();
    animator = GetComponent<Animator>();
    boxCollider2D = GetComponent<BoxCollider2D>();

    body.freezeRotation = true;
    body.interpolation = RigidbodyInterpolation2D.Interpolate;
  }
  private void Update()
  {
    if (!IsOwner) return;

    if (canMove)
    {
      horizontalInput = Input.GetAxisRaw("Horizontal");
      body.linearVelocity = new Vector2(horizontalInput * speed, body.linearVelocity.y);
    }
    else
    {
      body.linearVelocity = new Vector2(body.linearVelocity.x, body.linearVelocity.y);
    }

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

  private void Jump()
  {
    if (isGrounded())
    {
      body.linearVelocity = new Vector2(body.linearVelocity.x, jumpPower);
    }

    int direction = inputDirection();
    float side = direction != 0 ? direction : Mathf.Sign(transform.localScale.x);

    if (onWallDirection(side) && !isGrounded())
    {
      canMove = false;
      transform.localScale = new Vector3(-side, 1, 1);

      justWallJumped = true;
      noSlideTimer = wallJumpNoSlideTime;

      StartCoroutine(WallJumpExecute(-side));
      animator.SetTrigger("jump");
    }
  }

  private void HandleAirbornePhysics()
  {
    if (Input.GetKeyUp(KeyCode.Space) && body.linearVelocity.y > 0f)
      body.linearVelocity = new Vector2(body.linearVelocity.x, body.linearVelocity.y * shortHopCut);

    if (!isGrounded())
    {
      if (noSlideTimer > 0f)
      {
        noSlideTimer -= Time.deltaTime;
      }

      if (noSlideTimer <= 0f)
      {
        if (!HandleWallSlide())
        {
          if (!justWallJumped)
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
      }
      else
      {
        animator.SetBool("wallSlide", false);
      }
    }
    else
    {
      animator.SetBool("wallSlide", false);
    }
  }


  private bool HandleWallSlide()
  {
    int direction = inputDirection();
    bool pressingIntoWall = direction != 0 && onWallDirection(direction);


    if (pressingIntoWall && !isGrounded())
    {
      float targetSlideSpeed = wallSlideMaximumFallSpeed;

      if (body.linearVelocity.y > targetSlideSpeed)
        body.linearVelocity = new Vector2(body.linearVelocity.x, targetSlideSpeed);

      animator.SetBool("wallSlide", true);
      return true;
    }

    animator.SetBool("wallSlide", false);
    return false;
  }

  private void HandleAnimations()
  {
    if (!isGrounded())
    {
      animator.SetTrigger("jump");
    }
    animator.SetBool("run", horizontalInput != 0);
    animator.SetBool("grounded", isGrounded());
  }

  // Helper
  private bool isGrounded()
  {
    RaycastHit2D raycastHit = Physics2D.BoxCast(boxCollider2D.bounds.center, boxCollider2D.bounds.size, 0, Vector2.down, 0.1f, groundLayer);
    return raycastHit.collider != null;
  }

  private bool onWallDirection(float directionSign)
  {
    if (directionSign == 0)
    {
      return false;
    }

    Vector2 dir = new Vector2(Mathf.Sign(directionSign), 0f);
    RaycastHit2D hit = Physics2D.BoxCast(boxCollider2D.bounds.center, boxCollider2D.bounds.size, 0f, dir, wallCheckDistance, wallLayer);

    return hit.collider != null;
  }

  private int inputDirection()
  {
    return horizontalInput > 0.01f ? 1 : (horizontalInput < -0.01f ? -1 : 0); ;
  }

  private IEnumerator WallJumpExecute(float side)
  {
    yield return new WaitForSeconds(0.05f);

    body.linearVelocity = new Vector2(side * wallJumpKickX, wallJumpKickY);

    yield return new WaitForSeconds(0.10f);
    justWallJumped = false;

    yield return new WaitForSeconds(wallJumpControlLock - 0.10f);
    canMove = true;
  }
}