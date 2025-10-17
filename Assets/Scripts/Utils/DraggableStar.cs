using UnityEngine;

public class DraggableStar : MonoBehaviour
{
    private Camera cam;
    private bool isDragging;
    private Vector3 originalPosition;

    [Header("Movement Limit")]
    public float minX = -3f;
    public float maxX = 3f;

    [Header("Return Settings")]
    public float returnSpeed = 3f;
    private bool isReturning = false;

    [Header("Pulley Link")]
    public PulleySystem pulley;
    private float baseLeftDistance;
    private float baseLiftY;

    void Start()
    {
        cam = Camera.main;
        originalPosition = transform.position;

        // cache baseline once
        if (pulley != null)
        {
            baseLeftDistance = Vector2.Distance(transform.position, pulley.pulleyPivot.position);
            baseLiftY = pulley.lift.position.y;
        }
    }

    void OnMouseDown()
    {
        isDragging = true;
        isReturning = false;
    }

    void OnMouseUp()
    {
        isDragging = false;
        ReturnToStart();
    }

    void Update()
    {
        if (isDragging)
        {
            // 🔒 HARD FREEZE when pulley is at either limit
            if (pulley != null && (pulley.isAtMaxHeight || pulley.isAtMinHeight))
            {
                return;
            }

            Vector3 mousePos = Input.mousePosition;
            mousePos.z = -cam.transform.position.z;
            Vector3 world = cam.ScreenToWorldPoint(mousePos);

            /// 1) your designer clamp
            float desiredX = Mathf.Clamp(world.x, minX, maxX);

            // 2) Pulley hard clamp (kept as-is from your version)
            if (pulley != null && !Mathf.Approximately(pulley.ropeRatio, 0f))
            {
                Vector2 pivot = pulley.pulleyPivot.position;
                float xp = pivot.x;
                float yp = pivot.y;

                float starY = transform.position.y;
                float h = Mathf.Abs(starY - yp);

                float DistanceForLiftY(float liftY) =>
                    baseLeftDistance + (liftY - baseLiftY) / pulley.ropeRatio;

                float ProposedDistanceForX(float x)
                {
                    float dx = Mathf.Abs(x - xp);
                    return Mathf.Sqrt(dx * dx + h * h);
                }

                float proposedDist = ProposedDistanceForX(desiredX);
                float impliedLiftY = baseLiftY + (proposedDist - baseLeftDistance) * pulley.ropeRatio;

                if (impliedLiftY > pulley.maxLiftY || impliedLiftY < pulley.minLiftY)
                {
                    float targetLift = Mathf.Clamp(impliedLiftY, pulley.minLiftY, pulley.maxLiftY);
                    float targetDist = DistanceForLiftY(targetLift);

                    if (targetDist <= h + 1e-6f)
                    {
                        desiredX = xp;
                    }
                    else
                    {
                        float targetRadius = Mathf.Sqrt(targetDist * targetDist - h * h);
                        float side = Mathf.Sign(
                            Mathf.Abs(desiredX - xp) > 1e-4f ? (desiredX - xp) :
                            (Mathf.Abs(transform.position.x - xp) > 1e-4f ? (transform.position.x - xp) : 1f)
                        );

                        desiredX = xp + side * targetRadius;
                        desiredX = Mathf.Clamp(desiredX, minX, maxX);
                    }
                }
            }

            transform.position = new Vector3(desiredX, transform.position.y, transform.position.z);
        }
        else if (isReturning)
        {
            transform.position = Vector3.MoveTowards(transform.position, originalPosition, returnSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, originalPosition) < 0.01f)
            {
                transform.position = originalPosition;
                isReturning = false;
            }
        }
    }

    public void ReturnToStart()
    {
        if (returnSpeed <= 0)
            transform.position = originalPosition;
        else
            isReturning = true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(minX, transform.position.y, 0),
                        new Vector3(maxX, transform.position.y, 0));
    }
}
