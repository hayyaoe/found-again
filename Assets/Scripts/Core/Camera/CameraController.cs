using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody2D playerRigidbody;

    [Header("Follow Settings")]
    [Tooltip("Smaller values = slower smoothing, typical range: 0.05–0.15")]
    [SerializeField] private float followSmoothTime;
    [SerializeField] private Vector2 followOffset = new Vector2(0f, 5f);
    [SerializeField] private Vector2 deadZone;

    [Header("Room Boundaries")]
    [SerializeField] private Vector2 minBounds;
    [SerializeField] private Vector2 maxBounds;

    [Header("Look Ahead Settings")]
    [SerializeField] private float lookAheadDistance;
    [SerializeField] private float lookAheadLerpSpeed;
    [SerializeField] private float lookAheadReturnSpeed;
    [SerializeField] private float lookAheadThreshold;

    private float currentLookAheadX;
    private Vector3 targetPosition;
    private Vector3 cameraVelocity = Vector3.zero;

    private void LateUpdate()
    {
        if (player == null)
            return;

        float playerVelocityX = playerRigidbody != null ? playerRigidbody.linearVelocity.x : 0f;
        float targetLookAheadX = 0f;

        // Apply look-ahead only if player is moving fast enough
        if (Mathf.Abs(playerVelocityX) > lookAheadThreshold)
        {
            targetLookAheadX = Mathf.Sign(playerVelocityX) * lookAheadDistance;
        }

        // Smoothly interpolate look-ahead position
        currentLookAheadX = Mathf.MoveTowards(
            currentLookAheadX,
            targetLookAheadX,
            (targetLookAheadX == 0 ? lookAheadReturnSpeed : lookAheadLerpSpeed) * Time.deltaTime
        );

        // --- 2. Build target position ---
        Vector3 playerTargetPosition = player.position + new Vector3(currentLookAheadX + followOffset.x, followOffset.y, 0f);
        Vector3 currentCameraPosition = transform.position;

        // --- 3. Dead Zone logic ---
        if (Mathf.Abs(playerTargetPosition.x - currentCameraPosition.x) > deadZone.x)
            currentCameraPosition.x = Mathf.Lerp(currentCameraPosition.x, playerTargetPosition.x, 1f);

        if (Mathf.Abs(playerTargetPosition.y - currentCameraPosition.y) > deadZone.y)
            currentCameraPosition.y = Mathf.Lerp(currentCameraPosition.y, playerTargetPosition.y, 1f);

        // --- 4. Clamp inside room boundaries ---
        currentCameraPosition.x = Mathf.Clamp(currentCameraPosition.x, minBounds.x, maxBounds.x);
        currentCameraPosition.y = Mathf.Clamp(currentCameraPosition.y, minBounds.y, maxBounds.y);

        // --- 5. SmoothDamp to target ---
        targetPosition = new Vector3(currentCameraPosition.x, currentCameraPosition.y, transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref cameraVelocity, followSmoothTime);
    }

    public void setTarget(Transform target)
    {
        player = target;
    }
}