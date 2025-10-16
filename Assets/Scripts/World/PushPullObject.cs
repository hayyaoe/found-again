using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PushPullObject : MonoBehaviour
{
    [HideInInspector] public bool isBeingPushed = false;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        LockObject(); // Start locked/unmovable
    }

    public void StartPush()
    {
        isBeingPushed = true;
        UnlockObject();
    }

    public void StopPush()
    {
        isBeingPushed = false;
        LockObject();
    }

    private void LockObject()
    {
        // ✅ Make it immovable when not pushed
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;

        // Stop any current horizontal movement
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        rb.angularVelocity = 0f;
    }

    private void UnlockObject()
    {
        // ✅ Allow normal physics movement when being pushed
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; // keep upright
    }
}