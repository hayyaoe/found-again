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

    private float baseLeftDistance;
    private float baseLiftY;

    void Start()
    {
        baseLeftDistance = Vector2.Distance(leftStar.position, pulleyPivot.position);
        baseLiftY = lift.position.y; // store initial height
    }

    void Update()
    {
        // Distance from pulley to left star
        float currentLeftDistance = Vector2.Distance(leftStar.position, pulleyPivot.position);

        // Difference from starting rope length
        float ropeDelta = currentLeftDistance - baseLeftDistance;

        // Set lift height based on inverse rope change
        Vector3 liftPos = lift.position;
        liftPos.y = baseLiftY + ropeDelta * ropeRatio; // no accumulation!
        lift.position = liftPos;

        // Update rope visuals
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