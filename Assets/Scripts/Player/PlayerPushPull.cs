using UnityEngine;

public class PlayerPushPull : MonoBehaviour
{
    [Header("Push / Pull Settings")]
    [SerializeField] private float interactRange = 1f;
    [SerializeField] private LayerMask pushableLayer;
    [SerializeField] private float pushSpeed = 3f;

    private PushPullObject currentObject;
    private Rigidbody2D rb;
    public bool isPushing = false;
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
        Collider2D hit = Physics2D.OverlapCircle(transform.position, interactRange, pushableLayer);
        if (hit == null) return;

        currentObject = hit.GetComponent<PushPullObject>();
        if (currentObject == null) return;

        Vector2 playerPos = transform.position;
        Vector2 objectPos = currentObject.transform.position;

        float verticalDifference = playerPos.y - objectPos.y;
        float horizontalDifference = Mathf.Abs(playerPos.x - objectPos.x);

        // ✅ Get height of the object for more adaptive comparison
        float objectHeight = currentObject.GetComponent<Collider2D>().bounds.size.y;

        // ✅ Allow some "corner" tolerance — can be slightly above the box and still push
        bool besideObject = verticalDifference < objectHeight * 0.7f; // was 0.5f before
        bool closeHorizontally = horizontalDifference < interactRange + 0.5f;

        if (besideObject && closeHorizontally)
        {
            isPushing = true;
            currentObject.StartPush();

            var relJoint = gameObject.AddComponent<RelativeJoint2D>();
            relJoint.connectedBody = currentObject.GetComponent<Rigidbody2D>();
            relJoint.autoConfigureOffset = true;
            relJoint.maxForce = 1000f;
            relJoint.enableCollision = false;
            joint = relJoint;
        }
        else
        {
            // 🚫 Block push if truly standing above
            Debug.Log("Can't push from above!");
        }
    }



    private void DetachObject()
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

    public bool IsPushing()
    {
        return isPushing;
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
