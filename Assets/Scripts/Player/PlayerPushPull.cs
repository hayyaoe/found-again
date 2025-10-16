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
