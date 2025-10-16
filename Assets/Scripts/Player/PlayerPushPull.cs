using UnityEngine;
using UnityEngine.InputSystem; // ✅ Important

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

    private InputAction interactAction; // ✅ New
    private PlayerInput playerInput;    // ✅ New

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>(); // Get PlayerInput component
    }

    private void OnEnable()
    {
        // ✅ Get the Interact action from PlayerInput
        interactAction = playerInput.actions["Interact"];

        // ✅ Subscribe to the performed and canceled events
        interactAction.performed += OnInteractPerformed;
        interactAction.canceled += OnInteractCanceled;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        interactAction.performed -= OnInteractPerformed;
        interactAction.canceled -= OnInteractCanceled;
    }

    private void Update()
    {
        // Movement input can still be read like this:
        horizontalInput = playerInput.actions["Move"].ReadValue<Vector2>().x;
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        TryAttachToObject();
    }

    private void OnInteractCanceled(InputAction.CallbackContext context)
    {
        DetachObject();
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
        float objectHeight = currentObject.GetComponent<Collider2D>().bounds.size.y;

        bool besideObject = verticalDifference < objectHeight * 0.7f;
        bool closeHorizontally = horizontalDifference < interactRange + 0.5f;

        if (besideObject && closeHorizontally)
        {
        isPushing = true;
        currentObject.AddPushingPlayer(gameObject); // ✅ Instead of StartPush()

        var relJoint = gameObject.AddComponent<RelativeJoint2D>();
        relJoint.connectedBody = currentObject.GetComponent<Rigidbody2D>();
        relJoint.autoConfigureOffset = true;
        relJoint.maxForce = 1000f;
        relJoint.enableCollision = false;
        joint = relJoint;
        }
        else
        {
            Debug.Log("Can't push from above!");
        }
    }

    private void DetachObject()
    {
        if (currentObject != null)
        {
            currentObject.RemovePushingPlayer(gameObject); // ✅ Instead of StopPush()

            DraggableStar star = currentObject.GetComponent<DraggableStar>();
            if (star != null)
                star.ReturnToStart();

            currentObject = null;
        }

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