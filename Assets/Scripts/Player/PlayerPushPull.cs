using UnityEngine;

public class PlayerPushPull : MonoBehaviour
{
    [Header("Push / Pull Settings")]
    [SerializeField] private float interactRange = 1f;
    [SerializeField] private LayerMask pushableLayer;
    [SerializeField] private float pushSpeed = 3f;

    private PushPullObject currentObject;
    private Rigidbody2D rb;
    private bool isPushing = false;
    private float horizontalInput;
    private RelativeJoint2D joint;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetKeyDown(KeyCode.E))
            TryAttachToObject();

        if (Input.GetKeyUp(KeyCode.E))
            DetachObject();
    }

    private void FixedUpdate()
    {
        // if (isPushing && joint != null && currentObject != null)
        // {
        //     // update joint distance to stay near contact point
        //     joint.distance = Vector2.Distance(transform.position, currentObject.transform.position);
        // }
    }

    private void TryAttachToObject()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, interactRange, pushableLayer);

        if (hit != null)
        {
            currentObject = hit.GetComponent<PushPullObject>();
            if (currentObject != null)
            {
                isPushing = true;
                currentObject.StartPush();

                var relJoint = gameObject.AddComponent<RelativeJoint2D>();
                relJoint.connectedBody = currentObject.GetComponent<Rigidbody2D>();
                relJoint.autoConfigureOffset = true; // keeps their current relative offset
                relJoint.maxForce = 1000f;           // prevent excessive force jitter
                relJoint.enableCollision = false;
                joint = relJoint; // if you want to keep the same variable name
            }
        }
    }

    private void DetachObject()
    {
        if (currentObject != null)
        {
            currentObject.StopPush();

            // 🟡 If the object has a DraggableStar, tell it to return
            DraggableStar star = currentObject.GetComponent<DraggableStar>();
            if (star != null)
                star.ReturnToStart();

            currentObject = null;
        }

        // ✅ Remove the joint
        if (joint != null)
            Destroy(joint);

        isPushing = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
