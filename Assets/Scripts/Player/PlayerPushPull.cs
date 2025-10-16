using UnityEngine;

public class PlayerPushPull : MonoBehaviour
{
    [Header("Push / Pull Settings")]
    [SerializeField] private float interactRange = 1f;
    [SerializeField] private LayerMask pushableLayer;
    [SerializeField] private float pushSpeed = 3f;

    private PushPullObject currentObject;
    private Rigidbody2D rb;
    private bool isPushing = false; // Is the player actually joined and moving the object?
    private bool isAttemptingPush = false; // Is the player holding down the button, trying to push?
    private float horizontalInput;
    private RelativeJoint2D joint;

    private Movement movement;
    private BoxCollider2D playerCollider;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<Movement>();
        playerCollider = GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetKeyDown(KeyCode.E) && !isAttemptingPush)
            TryStartPush();
        else if (Input.GetKeyUp(KeyCode.E) && isAttemptingPush)
            TryStopPush();
        
        if (isAttemptingPush && !movement.isGrounded())
        {
            TryStopPush();
        }
    }

    private void FixedUpdate()
    {
        if (isPushing)
        {
            rb.linearVelocity = new Vector2(horizontalInput * pushSpeed, rb.linearVelocity.y);
        }
    }

    private void TryStartPush()
    {
        if (!movement.isGrounded()) return;

        Collider2D hit = Physics2D.OverlapCircle(transform.position, interactRange, pushableLayer);
        if (hit != null)
        {
            BoxCollider2D objectCollider = hit.GetComponent<BoxCollider2D>();
            if (objectCollider != null && playerCollider.bounds.min.y > objectCollider.bounds.max.y - 0.1f)
            {
                return; // Player is on top, do nothing.
            }
            
            currentObject = hit.GetComponent<PushPullObject>();
            if (currentObject != null)
            {
                isAttemptingPush = true;
                currentObject.AddPusher(this);
            }
        }
    }

    private void TryStopPush()
    {
        if (currentObject != null)
        {
            currentObject.RemovePusher(this);
        }
        Detach();
        isAttemptingPush = false;
        currentObject = null;
    }

    // Called by PushPullObject when enough players have joined
    public void OnPushSuccessful()
    {
        if (isAttemptingPush && !isPushing)
        {
            isPushing = true;
            movement.IsPushing = true;
            
            var relJoint = gameObject.AddComponent<RelativeJoint2D>();
            relJoint.connectedBody = currentObject.GetComponent<Rigidbody2D>();
            relJoint.autoConfigureOffset = true;
            relJoint.maxForce = 2000f; // Increased force for multiple players
            relJoint.enableCollision = false;
            joint = relJoint;
        }
    }

    // Called by PushPullObject when not enough players are available
    public void OnPushFailed()
    {
        Detach();
    }

    // Detaches the player from the object
    private void Detach()
    {
        if (joint != null)
        {
            Destroy(joint);
        }

        if (isPushing)
        {
            isPushing = false;
            movement.IsPushing = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
