using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody2D playerRigidbody;

    [Header("Follow Settings")]
    [Tooltip("Smaller values = slower smoothing, typical range: 0.05–0.15")]
    [SerializeField] private float followSmoothTime = 0.1f;
    [SerializeField] private Vector2 followOffset = new Vector2(0f, 5f);
    [SerializeField] private Vector2 deadZone = new Vector2(1f, 1f);

    [Header("Room Boundaries")]
    [SerializeField] private Vector2 minBounds;
    [SerializeField] private Vector2 maxBounds;

    [Header("Look Ahead Settings")]
    [SerializeField] private float lookAheadDistance = 2f;
    [SerializeField] private float lookAheadLerpSpeed = 5f;
    [SerializeField] private float lookAheadReturnSpeed = 3f;
    [SerializeField] private float lookAheadThreshold = 0.1f;
    
    [Header("Lock Settings")]
    [SerializeField] private float lockSmoothTime = 0.3f; // Smaller = faster lock


    private float currentLookAheadX;
    private Vector3 targetPosition;
    private Vector3 cameraVelocity = Vector3.zero;

    // --- New ---
    private bool cameraLocked = false;
    private Vector3 lockedPosition;

    private void LateUpdate()
    {
        // If locked, hold position and skip all follow logic
        if (cameraLocked)
        {
            transform.position = Vector3.SmoothDamp(transform.position, lockedPosition, ref cameraVelocity, lockSmoothTime);
            return;
        }

        if (player == null)
            return;

        float playerVelocityX = playerRigidbody != null ? playerRigidbody.linearVelocity.x : 0f;
        float targetLookAheadX = 0f;

        // Apply look-ahead only if player is moving fast enough
        if (Mathf.Abs(playerVelocityX) > lookAheadThreshold)
        {
            targetLookAheadX = Mathf.Sign(playerVelocityX) * lookAheadDistance;
        }

        currentLookAheadX = Mathf.MoveTowards(
            currentLookAheadX,
            targetLookAheadX,
            (targetLookAheadX == 0 ? lookAheadReturnSpeed : lookAheadLerpSpeed) * Time.deltaTime
        );

        Vector3 playerTargetPosition = player.position + new Vector3(currentLookAheadX + followOffset.x, followOffset.y, 0f);
        Vector3 currentCameraPosition = transform.position;

        // Dead Zone logic
        if (Mathf.Abs(playerTargetPosition.x - currentCameraPosition.x) > deadZone.x)
            currentCameraPosition.x = Mathf.Lerp(currentCameraPosition.x, playerTargetPosition.x, 1f);

        if (Mathf.Abs(playerTargetPosition.y - currentCameraPosition.y) > deadZone.y)
            currentCameraPosition.y = Mathf.Lerp(currentCameraPosition.y, playerTargetPosition.y, 1f);

        // Clamp inside room boundaries
        currentCameraPosition.x = Mathf.Clamp(currentCameraPosition.x, minBounds.x, maxBounds.x);
        currentCameraPosition.y = Mathf.Clamp(currentCameraPosition.y, minBounds.y, maxBounds.y);

        targetPosition = new Vector3(currentCameraPosition.x, currentCameraPosition.y, transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref cameraVelocity, followSmoothTime);
    }

    public void setTarget(Transform target)
    {
        player = target;
    }

    // --- New Methods ---
    public void LockToPosition(Vector3 position)
    {
        cameraLocked = true;
        lockedPosition = new Vector3(position.x, position.y, transform.position.z);
    }

    public void UnlockCamera()
    {
        cameraLocked = false;
    }

    public void UpdateBounds(Vector2 newMin, Vector2 newMax)
    {
        minBounds = newMin;
        maxBounds = newMax;
    }
}
