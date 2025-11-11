using UnityEngine;

public class PulleySystem : MonoBehaviour
{
    [Header("References")]
    public Transform pulleyPivot;
    public Transform leftStar;
    public Transform lift;

    [Header("Ropes")]
    public LineRenderer ropeLeft;
    public LineRenderer ropeRight;

    [Header("Lift Settings")]
    [Tooltip("How much the lift moves relative to the star. 1 = equal distance.")]
    public float ropeRatio = 1f;

    [Header("Lift Limits")]
    public float minLiftY = -3f;
    public float maxLiftY = 3f;

    [Header("Configuration")]
    [Tooltip("If true, the lift cannot go lower than it starts (prevents pushing star right past start).")]
    public bool lockMinYToStart = true;

    [Header("State")]
    public bool isAtMinHeight { get; private set; }
    public bool isAtMaxHeight { get; private set; }

    // Internal state
    private float baseLeftDistance;
    private float baseLiftY;
    private float minAllowedDist;
    private float maxAllowedDist;

    void Start()
    {
        // 1. Initialize base positions
        baseLeftDistance = Vector2.Distance(leftStar.position, pulleyPivot.position);
        baseLiftY = lift.position.y;

        // 2. Optional: Lock the minimum height to where it currently is.
        if (lockMinYToStart)
        {
            minLiftY = baseLiftY;
        }

        // 3. Calculate the allowed distance range for the star based on lift limits.
        minAllowedDist = baseLeftDistance + (minLiftY - baseLiftY) / ropeRatio;
        maxAllowedDist = baseLeftDistance + (maxLiftY - baseLiftY) / ropeRatio;
    }

    void LateUpdate()
    {
        // Calculate desired lift position based on star's current distance
        float currentLeftDistance = Vector2.Distance(leftStar.position, pulleyPivot.position);
        float ropeDelta = currentLeftDistance - baseLeftDistance;
        float desiredY = baseLiftY + ropeDelta * ropeRatio;

        // Update state flags so other scripts (like PushPullObject) know if we hit a limit
        isAtMinHeight = desiredY <= minLiftY + 0.001f;
        isAtMaxHeight = desiredY >= maxLiftY - 0.001f;

        // Clamp and apply position to lift
        Vector3 liftPos = lift.position;
        liftPos.y = Mathf.Clamp(desiredY, minLiftY, maxLiftY);
        lift.position = liftPos;

        UpdateRopes();
    }

    /// <summary>
    /// Returns a valid position for the star that respects the lift's min/max Y limits.
    /// </summary>
    public Vector2 ClampStarPosition(Vector2 proposedPos)
    {
        Vector2 dir = proposedPos - (Vector2)pulleyPivot.position;
        float dist = dir.magnitude;

        if (dist < minAllowedDist || dist > maxAllowedDist)
        {
             float clampedDist = Mathf.Clamp(dist, minAllowedDist, maxAllowedDist);
             return (Vector2)pulleyPivot.position + (dir.normalized * clampedDist);
        }

        return proposedPos;
    }

    void UpdateRopes()
    {
        if (ropeLeft)
        {
            ropeLeft.positionCount = 2;
            ropeLeft.SetPosition(0, pulleyPivot.position);
            ropeLeft.SetPosition(1, leftStar.position);
        }
        if (ropeRight)
        {
            ropeRight.positionCount = 2;
            ropeRight.SetPosition(0, pulleyPivot.position);
            ropeRight.SetPosition(1, lift.position);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (pulleyPivot == null) return;

        // Draw standard base distance
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(pulleyPivot.position, baseLeftDistance);

        if (!Application.isPlaying) return;

        // Draw min/max allowed distances when playing
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pulleyPivot.position, maxAllowedDist);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(pulleyPivot.position, minAllowedDist);
    }
}