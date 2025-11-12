using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Slope : MonoBehaviour
{
    [Header("Layers")]
    [SerializeField] private LayerMask groundLayer = ~0;

    [Header("Probe (foot)")]
    [SerializeField] private float groundCheckDistance = 0.25f;
    [SerializeField] private Vector2 footOffset = new(0f, 0.02f);
    [SerializeField] private float footRadius = 0.16f;

    [Header("Slide (no PhysicsMaterial2D)")]
    [Tooltip("Selalu licin di slope apa pun.")]
    [SerializeField] private bool alwaysSlippery = true;
    [Tooltip("Akselerasi minimum sepanjang slope (agar tidak mandek).")]
    [SerializeField] private float minSlideAccel = 12f;
    [Tooltip("Boost ke komponen gravitasi sepanjang slope.")]
    [SerializeField] private float slideAccelBoost = 1.25f;
    [Tooltip("Maksimal kecepatan sepanjang slope.")]
    [SerializeField] private float maxSlideSpeed = 22f;
    [Tooltip("Dorongan kecil menempel ke permukaan (tidak dipakai saat jump buffer).")]
    [SerializeField] private float groundStickForce = 35f;
    [Tooltip("Berapa detik mengabaikan logic slope setelah lompat.")]
    [SerializeField] private float jumpIgnoreSlopeTime = 0.10f;

    [Header("Angle Gate (kalau tidak alwaysSlippery)")]
    [SerializeField] private float maxSlopeAngle = 45f;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = false;

    // Runtime
    private Rigidbody2D rb;
    private Collider2D col;

    // State
    public bool IsGrounded { get; private set; }
    public bool IsOnSlope  { get; private set; }
    public bool IsSliding  { get; private set; }
    public float SlopeAngle { get; private set; }
    public Vector2 SlopeNormal { get; private set; } = Vector2.up;
    public Vector2 SlopeTangent { get; private set; } = Vector2.right;

    private float jumpIgnoreTimer;   // buffer setelah jump
    private bool jumpBuffered;       // flag selama buffer

    // ==== Public API (panggil dari controller saat tombol lompat ditekan) ====
    public void NotifyJumpPressed()
    {
        jumpIgnoreTimer = jumpIgnoreSlopeTime;
        jumpBuffered = true;
    }

    private void Awake()
    {
        rb  = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.freezeRotation = true;
        rb.interpolation  = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void FixedUpdate()
    {
        // turunkan timer buffer jump
        if (jumpIgnoreTimer > 0f)
        {
            jumpIgnoreTimer -= Time.fixedDeltaTime;
            if (jumpIgnoreTimer <= 0f) jumpBuffered = false;
        }

        ProbeGround();
        HandleSlopeSliding();
    }

    private void ProbeGround()
    {
        Bounds b = col.bounds;
        Vector2 foot = new(b.center.x, b.min.y);
        foot += footOffset;

        var hit = Physics2D.CircleCast(foot, footRadius, Vector2.down, groundCheckDistance, groundLayer);

        if (hit.collider != null)
        {
            IsGrounded  = true;
            SlopeNormal = hit.normal;
            SlopeAngle  = Vector2.Angle(SlopeNormal, Vector2.up);
            IsOnSlope   = SlopeAngle > 0.01f;

            // Tangent = perp(normal). Pilih arah MENURUN (searah gravitasi proyeksi).
            Vector2 t = new(SlopeNormal.y, -SlopeNormal.x);
            Vector2 g = Physics2D.gravity * rb.gravityScale;
            if (Vector2.Dot(t, g) < 0f) t = -t;
            SlopeTangent = t.normalized;

            IsSliding = alwaysSlippery ? IsOnSlope : (IsOnSlope && SlopeAngle > maxSlopeAngle);
        }
        else
        {
            IsGrounded  = false;
            IsOnSlope   = false;
            IsSliding   = false;
            SlopeAngle  = 0f;
            SlopeNormal = Vector2.up;
            SlopeTangent= Vector2.right;
        }
    }

    private void HandleSlopeSliding()
    {
        // 1) Jangan ganggu saat di udara atau lagi naik karena jump
        bool rising = rb.linearVelocity.y > 0.05f;
        if (!IsGrounded || rising || jumpBuffered) return;

        if (!IsOnSlope) return;

        // 2) Tenangkan komponen normal (tanpa mematikan loncat)
        Vector2 v = rb.linearVelocity;
        float vN  = Vector2.Dot(v, SlopeNormal);
        v -= vN * SlopeNormal; // buang komponen normal -> friction “hilang”

        // 3) Hitung percepatan sepanjang bidang (gravitasi + minimum accel + boost)
        float theta = SlopeAngle * Mathf.Deg2Rad;
        float gAlong = Physics2D.gravity.magnitude * rb.gravityScale * Mathf.Sin(theta); // bisa 0 di slope landai
        float accel  = Mathf.Max(minSlideAccel, gAlong * slideAccelBoost);

        // 4) Update komponen tangen saja (preserve komponen Y kecil kalau ada)
        float vTan = Vector2.Dot(v, SlopeTangent);
        vTan += accel * Time.fixedDeltaTime;
        vTan = Mathf.Clamp(vTan, -maxSlideSpeed, maxSlideSpeed);

        // rebuild velocity: hanya tangen (normal sudah dihapus)
        v = SlopeTangent * vTan;

        // 5) Stick force ringan agar tidak ‘lepas’ dari permukaan (tidak saat ingin jump)
        if (groundStickForce > 0f)
            rb.AddForce(-SlopeNormal * groundStickForce, ForceMode2D.Force);

        rb.linearVelocity = v;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        if (col == null) col = GetComponent<Collider2D>();
        Bounds b = col.bounds;
        Vector2 foot = new(b.center.x, b.min.y);
        foot += footOffset;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(foot, footRadius);
        Gizmos.DrawLine(foot, foot + Vector2.down * groundCheckDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + SlopeNormal);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + SlopeTangent);
    }
#endif
}
