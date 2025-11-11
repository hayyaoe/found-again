using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DraggableStar : MonoBehaviour
{
    [Header("Pulley Link")]
    public PulleySystem pulley;

    [Header("Return Settings")]
    public float returnSpeed = 3f;

    private Rigidbody2D rb;
    private Vector2 startPos;
    private bool isReturning;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        startPos = rb.position;
    }

    void FixedUpdate()
    {
        if (isReturning)
        {
            HandleReturn();
            return;
        }

        // 1. Enforce Hard Right Limit (Start Position)
        // If we are at or past the start line AND moving right...
        if (rb.position.x >= startPos.x - 0.001f && rb.linearVelocity.x > 0.01f)
        {
            // Kill rightward velocity instantly.
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
             // Hard snap back to the line if we crossed it.
            if (rb.position.x > startPos.x)
            {
                rb.MovePosition(new Vector2(startPos.x, rb.position.y));
            }
        }

        // 2. Enforce Pulley Limit (Far Left/Down)
        if (pulley != null)
        {
            EnforcePulleyLimit();
        }
    }

    void LateUpdate()
    {
        if (!isReturning && pulley != null)
        {
            // Final visual safety net for rendering ONLY.
            // Does not affect physics, just stops visual jitter past the line.
            Vector2 pos = transform.position;
            pos.x = Mathf.Min(pos.x, startPos.x); // Never visually right of start
            transform.position = pos;
        }
    }

    private void EnforcePulleyLimit()
    {
        Vector2 currentPos = rb.position;
        Vector2 allowedPos = pulley.ClampStarPosition(currentPos);
        Vector2 violation = currentPos - allowedPos;

        // If we are outside the allowed pulley area...
        if (violation.sqrMagnitude > 0.00001f)
        {
            // Check if we are trying to move FURTHER OUT.
            // Dot product > 0 means velocity is in same direction as violation (moving away from safety).
            float movingAwaySpeed = Vector2.Dot(rb.linearVelocity, violation.normalized);

            if (movingAwaySpeed > 0)
            {
                // We are actively moving wrong. Stop ONLY that component of movement.
                rb.MovePosition(allowedPos);
                rb.linearVelocity -= violation.normalized * movingAwaySpeed;
            }
            // If movingAwaySpeed <= 0, we are either stopped or moving safely inward.
            // DO NOTHING. This lets the player push it back freely.
        }
    }

    private void HandleReturn()
    {
        Vector2 target = startPos;
        if (returnSpeed <= 0f)
        {
            rb.MovePosition(target);
            rb.linearVelocity = Vector2.zero;
            isReturning = false;
            return;
        }

        Vector2 next = Vector2.MoveTowards(rb.position, target, returnSpeed * Time.fixedDeltaTime);
        rb.MovePosition(next);

        if (Vector2.Distance(next, target) < 0.005f)
        {
            rb.MovePosition(target);
            rb.linearVelocity = Vector2.zero;
            isReturning = false;
        }
    }

    public void ReturnToStart() { isReturning = true; }
    public void CancelReturn() { isReturning = false; }
}