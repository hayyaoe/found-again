using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DraggableStar : MonoBehaviour
{
    [Header("Bounds (Optional Visual Aid)")]
    [Tooltip("If true, minX/maxX are absolute world X. If false, they’re offsets from start X.")]
    public bool useAbsoluteBounds = true;
    public float minX = -3f;
    public float maxX = 3f;

    [Header("Return Settings")]
    [Tooltip("Units/second for returning to start. <=0 means instant snap.")]
    public float returnSpeed = 3f;

    [Header("Pulley (optional)")]
    public PulleySystem pulley;

    private Rigidbody2D rb;
    private PushPullObject pushPull;

    private Vector2 startPos;
    private bool isReturning;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        pushPull = GetComponent<PushPullObject>();
    }

    void Start()
    {
        startPos = rb.position;
    }

    void FixedUpdate()
    {
        if (!isReturning)
            return;

        Vector2 target = new Vector2(startPos.x, rb.position.y);

        if (returnSpeed <= 0f)
        {
            rb.MovePosition(target);
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            isReturning = false;
            return;
        }

        Vector2 next = Vector2.MoveTowards(rb.position, target, returnSpeed * Time.fixedDeltaTime);
        rb.MovePosition(next);

        if (Mathf.Abs(next.x - target.x) < 0.005f)
        {
            rb.MovePosition(target);
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            isReturning = false;
        }
    }

    public void ReturnToStart()
    {
        isReturning = true;
    }

    public void CancelReturn()
    {
        isReturning = false;
    }


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        float min = useAbsoluteBounds
            ? Mathf.Min(minX, maxX)
            : transform.position.x + Mathf.Min(minX, maxX);

        float max = useAbsoluteBounds
            ? Mathf.Max(minX, maxX)
            : transform.position.x + Mathf.Max(minX, maxX);

        Vector3 a = new Vector3(min, transform.position.y, 0f);
        Vector3 b = new Vector3(max, transform.position.y, 0f);
        Gizmos.DrawLine(a, b);
    }
}
