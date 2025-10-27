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
    public float ropeRatio = 1f; // 1 means equal movement

    [Header("Lift Limits")]
    public float minLiftY = -3f;
    public float maxLiftY = 3f;

    [Header("State (read-only flags)")]
    public bool isAtMinHeight;   // ⬅️ add
    public bool isAtMaxHeight;   // ⬅️ add

    private float baseLeftDistance;
    private float baseLiftY;

    void Start()
    {
        baseLeftDistance = Vector2.Distance(leftStar.position, pulleyPivot.position);
        baseLiftY = lift.position.y;
    }

    void Update()
    {
        float currentLeftDistance = Vector2.Distance(leftStar.position, pulleyPivot.position);
        float ropeDelta = currentLeftDistance - baseLeftDistance;

        float desiredY = baseLiftY + ropeDelta * ropeRatio;

        // set flags based on where desiredY wants to go
        isAtMinHeight = desiredY <= minLiftY;
        isAtMaxHeight = desiredY >= maxLiftY;

        Vector3 liftPos = lift.position;
        liftPos.y = Mathf.Clamp(desiredY, minLiftY, maxLiftY);
        lift.position = liftPos;

        if (isAtMaxHeight)
        {
            Debug.Log("🚀 Pulley reached MAX height");
        }
        else if (isAtMinHeight)
        {
            Debug.Log("⬇️ Pulley reached MIN height");
        }

        UpdateRopes();
    }

    void UpdateRopes()
    {
        ropeLeft.positionCount = 2;
        ropeLeft.SetPosition(0, pulleyPivot.position);
        ropeLeft.SetPosition(1, leftStar.position);

        ropeRight.positionCount = 2;
        ropeRight.SetPosition(0, pulleyPivot.position);
        ropeRight.SetPosition(1, lift.position);
    }
}
