using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ResettableObject : MonoBehaviour
{
    // Changed to protected so derived classes (like BoatReset) can use them
    protected Vector3 startPosition;
    protected Quaternion startRotation;
    protected Rigidbody2D rb;

    // Changed to 'protected new' so derived classes can provide their own Awake logic.
    protected void Awake()
    {
        // Save our starting state
        startPosition = transform.position;
        startRotation = transform.rotation;
        rb = GetComponent<Rigidbody2D>();

        // Register this object with the manager
        CheckpointManager.RegisterResettable(this);
    }

    private void OnDestroy()
    {
        // Unregister when destroyed (e.g., changing scenes)
        CheckpointManager.UnregisterResettable(this);
    }

    // Changed to 'public new' to allow derived classes to override, although
    // the derived class must be careful to call the base method if it needs to.
    public void ResetObject()
    {
        // Stop all physics and move back to the start
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        transform.position = startPosition;
        transform.rotation = startRotation;
    }
}