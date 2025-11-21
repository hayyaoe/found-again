using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Camera))]
public class CameraMovement : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Transform[] players; // Assigned dynamically
    [SerializeField] private float followSmoothTime = 0.15f;

    [Header("Zoom Settings")]
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 10f;
    [SerializeField] private float zoomLimiter = 10f;
    [SerializeField] private float zoomSmoothTime = 0.2f;

    [Header("Room Boundaries")]
    [SerializeField] private Vector2 minBounds;
    [SerializeField] private Vector2 maxBounds;

    private float minX = -Mathf.Infinity;
    private float maxX = Mathf.Infinity;
    private float minY = -Mathf.Infinity;
    private float maxY = Mathf.Infinity;

    [Header("Lock Settings")]
    [SerializeField] private float lockSmoothTime = 0.3f;

    private Vector3 velocity;
    private Camera cam;
    private bool cameraLocked = false;
    private Vector3 lockedPosition;
    private float targetZoom;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (cameraLocked)
        {
            transform.position = Vector3.SmoothDamp(transform.position, lockedPosition, ref velocity, lockSmoothTime);
            return;
        }

        // --- Ensure players list is valid ---
        if (players == null || players.Length == 0)
            return;

        players = players.Where(p => p != null).ToArray(); // remove destroyed ones
        if (players.Length == 0)
            return;

        Move();
        Zoom();
    }

    private void Move()
    {
        Vector3 centerPoint = GetCenterPoint();
        Vector3 newPosition = new Vector3(centerPoint.x, centerPoint.y, transform.position.z);

        // Clamp camera inside bounds
        newPosition.x = Mathf.Clamp(newPosition.x, minBounds.x, maxBounds.x);
        newPosition.y = Mathf.Clamp(newPosition.y, minBounds.y, maxBounds.y);

        transform.position = Vector3.SmoothDamp(transform.position, newPosition, ref velocity, followSmoothTime);
    }

    private void Zoom()
    {
        float greatestDistance = GetGreatestDistance();
        targetZoom = Mathf.Lerp(maxZoom, minZoom, greatestDistance / zoomLimiter);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime / zoomSmoothTime);
    }

    private float GetGreatestDistance()
    {
        if (players.Length == 1)
            return 0f;

        var bounds = new Bounds(players[0].position, Vector3.zero);
        foreach (var player in players)
        {
            bounds.Encapsulate(player.position);
        }
        return Mathf.Max(bounds.size.x, bounds.size.y);
    }

    private Vector3 GetCenterPoint()
    {
        if (players.Length == 1)
            return players[0].position;

        var bounds = new Bounds(players[0].position, Vector3.zero);
        foreach (var player in players)
        {
            bounds.Encapsulate(player.position);
        }
        return bounds.center;
    }

    // ---------------- Public Methods ----------------

    public void SetTargets(Transform[] newPlayers)
    {
        players = newPlayers;
    }

    public void LockToPosition(Vector3 position)
    {
        cameraLocked = true;
        lockedPosition = new Vector3(position.x, position.y, transform.position.z);
    }

    public void UnlockCamera()
    {
        cameraLocked = false;
        velocity = Vector3.zero; // reset smooth damp velocity to prevent snapping
    }


    public void UpdateBounds(Vector2 newMin, Vector2 newMax)
    {
        minBounds = newMin;
        maxBounds = newMax;
    }

    public void SnapToTargets()
    {
        if (players == null || players.Length == 0)
            return;

        // Remove missing players
        players = players.Where(p => p != null).ToArray();
        if (players.Length == 0)
            return;

        Vector3 center = GetCenterPoint();
        Vector3 newPos = new Vector3(center.x, center.y, transform.position.z);

        // Clamp inside room bounds
        newPos.x = Mathf.Clamp(newPos.x, minBounds.x, maxBounds.x);
        newPos.y = Mathf.Clamp(newPos.y, minBounds.y, maxBounds.y);

        // TELEPORT camera without smoothing
        transform.position = newPos;

        // Also snap zoom instantly
        float dist = GetGreatestDistance();
        float newZoom = Mathf.Lerp(maxZoom, minZoom, dist / zoomLimiter);
        cam.orthographicSize = newZoom;
    }

    public void SetClampingBounds(float min_x, float max_x, float min_y, float max_y)
    {
        minX = min_x;
        maxX = max_x;
        minY = min_y;
        maxY = max_y;
    }
}
