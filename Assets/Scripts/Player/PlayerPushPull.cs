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
    private DistanceJoint2D joint;


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
        if (isPushing && joint != null && currentObject != null)
        {
            // update joint distance to stay near contact point
            joint.distance = Vector2.Distance(transform.position, currentObject.transform.position);
        }
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

                joint = gameObject.AddComponent<DistanceJoint2D>();
                joint.connectedBody = currentObject.GetComponent<Rigidbody2D>();
                joint.autoConfigureDistance = false;
                joint.distance = Vector2.Distance(transform.position, currentObject.transform.position);
                joint.enableCollision = false;
            }
        }
    }

    private void DetachObject()
    {
        if (currentObject != null)
        {
            currentObject.StopPush();
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
