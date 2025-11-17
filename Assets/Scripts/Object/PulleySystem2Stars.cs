using UnityEngine;

public class PulleySystem2Stars : MonoBehaviour
{
    [Header("References")]
    public Transform pulleyPivot;

    [Header("Stars (two-sided pulley)")]
    public Transform leftStar;
    public Transform rightStar;

    private float baseLeftDistance;
    private float baseRightDistance;

    public Transform lift;

    [Header("Ropes")]
    public LineRenderer ropeLeft;
    public LineRenderer ropeCenter;
    public LineRenderer ropeRight;

    [Header("Lift Settings")]
    public float ropeRatio = 1f; // 1 means equal movement

    [Header("Lift Limits")]
    public float minLiftY = -3f;
    public float maxLiftY = 3f;

    [Header("State (read-only flags)")]
    public bool isAtMinHeight;   // ⬅️ add
    public bool isAtMaxHeight;   // ⬅️ add

    private float baseLiftY;

    void Start()
    {
        baseLeftDistance = Vector2.Distance(leftStar.position, pulleyPivot.position);
        baseRightDistance = Vector2.Distance(rightStar.position, pulleyPivot.position);
        baseLiftY = lift.position.y;
    }

    void Update()
    {
        // --- Two-Star Pulley Math ---
        float currentLeft  = Vector2.Distance(leftStar.position,  pulleyPivot.position);
        float currentRight = Vector2.Distance(rightStar.position, pulleyPivot.position);

        // Compare each one relative to their starting positions
        float deltaLeft  = currentLeft  - baseLeftDistance;
        float deltaRight = currentRight - baseRightDistance;

        // Left pulls lift UP, Right pulls lift DOWN
        float combinedDelta = deltaLeft - deltaRight;

        float desiredY = baseLiftY + combinedDelta * ropeRatio;

        // --- Your existing clamping & flags ---
        isAtMinHeight = desiredY <= minLiftY;
        isAtMaxHeight = desiredY >= maxLiftY;

        Vector3 liftPos = lift.position;
        liftPos.y = Mathf.Clamp(desiredY, minLiftY, maxLiftY);
        lift.position = liftPos;

        if (isAtMaxHeight)
            Debug.Log("🚀 Pulley reached MAX height");
        else if (isAtMinHeight)
            Debug.Log("⬇️ Pulley reached MIN height");

        UpdateRopes();
    }
    void UpdateRopes()
    {
        // Left rope (pulley → left star)
        ropeLeft.positionCount = 2;
        ropeLeft.SetPosition(0, pulleyPivot.position);
        ropeLeft.SetPosition(1, leftStar.position);

        // Right rope (pulley → right star)
        ropeRight.positionCount = 2;
        ropeRight.SetPosition(0, pulleyPivot.position);
        ropeRight.SetPosition(1, rightStar.position);

        // Center rope (pulley → lift)
        ropeCenter.positionCount = 2;
        ropeCenter.SetPosition(0, pulleyPivot.position);
        ropeCenter.SetPosition(1, lift.position);
    }
}
