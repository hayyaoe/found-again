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

    if (canMove)
    {
      horizontalInput = Input.GetAxisRaw("Horizontal");
      body.linearVelocity = new Vector2(horizontalInput * speed, body.linearVelocity.y);
    }
    else
    {
      body.linearVelocity = new Vector2(body.linearVelocity.x, body.linearVelocity.y);
    }
  }


  private void Jump()
  {
    if (isGrounded())
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
    animator.SetBool("run", horizontalInput != 0);
    animator.SetBool("grounded", isGrounded());
  }

  // Helper
  private bool isGrounded()
  {
    RaycastHit2D raycastHit = Physics2D.BoxCast(boxCollider2D.bounds.center, boxCollider2D.bounds.size, 0, Vector2.down, 0.1f, groundLayer);
    return raycastHit.collider != null;
  }
}