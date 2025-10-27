using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ResettableObject : MonoBehaviour
{
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Rigidbody2D rb;

    private void Awake()
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