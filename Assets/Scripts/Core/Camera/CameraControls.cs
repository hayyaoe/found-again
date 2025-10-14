using UnityEngine;

public class CameraControls : MonoBehaviour
{
    // Room Camera
    [SerializeField] private float speed;
    private float currentPosX;
    [SerializeField] private Vector2 yClamp = new Vector2(-3f, 3f); // Optional vertical limit
    private Vector3 targetPosition;
    private Vector3 smoothVelocity = Vector3.zero;

    // Player Camera
    [SerializeField] private Transform player;
    [SerializeField] private float aheadDistance;
    [SerializeField] private float cameraSpeed;

    [SerializeField] private float headSpace = 0.8f; // ✅ You can now set decimals in the Inspector (e.g. 0.5f, 1.75f, etc.)

    private float lookAhead;

    private void Update()
    {
        if (player == null) return;

        // --- Compute target position ---
        lookAhead = Mathf.Lerp(lookAhead, aheadDistance * player.localScale.x, Time.deltaTime * cameraSpeed);

        targetPosition = new Vector3(
            player.position.x + lookAhead,
            Mathf.Clamp(player.position.y + headSpace, yClamp.x, yClamp.y), // ✅ use headSpace here
            transform.position.z
        );

        // --- Smoothly move camera to target position ---
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref smoothVelocity, 0.2f);
    }

    public void SetTarget(Transform target)
    {
        player = target;
    }

    public void MoveToNewRoom(Transform _newRoom)
    {
        transform.position = new Vector3(
            _newRoom.position.x,
            _newRoom.position.y,
            transform.position.z
        );
    }
}
