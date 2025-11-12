using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class AutoElevator2D : MonoBehaviour
{
    public enum TravelMode { OneShot, PingPong }
    public enum State { Idle, MovingForward, MovingBackward, Paused }

    [Header("Waypoints (World XY)")]
    public Vector2 originXY;
    public Vector2 targetXY;

    [Header("Movement")]
    [Min(0.01f)] public float speed = 2f;
    [Min(0f)] public float waitAtEnds = 0.4f;
    public AnimationCurve ease = AnimationCurve.Linear(0,0,1,1);
    public TravelMode travelMode = TravelMode.PingPong;
    public bool autoStart = true;

    [Header("Z Handling")]
    public bool lockZ = true;
    public float fixedZ = 0f;

    [Header("Passenger Carry (Optional)")]
    public bool parentOnContact = false;
    public string passengerTag = "Player";

    [Header("Anti-Crush (Reverse When Blocked)")]
    [Tooltip("Layer yang dianggap menghalangi lift saat TURUN (bisa pilih banyak).")]
    public LayerMask obstacleMask;
    [Tooltip("Aktifkan jika ingin filter tambahan berdasarkan Tag.")]
    public bool useTagFilter = false;
    [Tooltip("Daftar tag yang dianggap penghalang (hanya dipakai jika useTagFilter = true).")]
    public string[] obstacleTags = new string[] { "Player", "Object" };
    [Tooltip("Jarak buffer agar tidak menempel persis.")]
    [Min(0f)] public float skin = 0.02f;

    [Header("Runtime (Read-only)")]
    [SerializeField] private bool isMoving = false;
    public State CurrentState { get; private set; } = State.Idle;

    public Vector2 PlatformVelocity { get; private set; }

    private Rigidbody2D rb2d;
    private Collider2D col2d;
    private float t01 = 0f;
    private Coroutine mover;

    private Vector2 lastPos2;
    private bool hasLast = false;
    private bool blockedDown = false;

    void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
        col2d = GetComponent<Collider2D>();

        if (rb2d != null)
        {
            rb2d.bodyType = RigidbodyType2D.Kinematic;
            rb2d.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb2d.gravityScale = 0f;
            rb2d.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    void Start()
    {
        if (originXY == Vector2.zero && targetXY == Vector2.zero)
        {
            originXY = transform.position;
            targetXY = originXY + Vector2.up * 5f;
        }

        SnapToOrigin();
        lastPos2 = (Vector2)transform.position;
        PlatformVelocity = Vector2.zero;
        hasLast = true;

        if (autoStart) StartMoving();
    }

    // ======== Public API ========
    public void StartMoving()
    {
        if (isMoving) return;
        isMoving = true;
        mover = StartCoroutine(MoveRoutine());
    }

    public void StopMoving(bool pause = true)
    {
        isMoving = false;
        if (mover != null) StopCoroutine(mover);
        mover = null;
        CurrentState = pause ? State.Paused : State.Idle;
    }

    public void ToggleMoving() { if (isMoving) StopMoving(); else StartMoving(); }
    public void GoToOrigin(bool andStop = false) { StopMoving(false); t01 = 0f; ApplyPosition(0f,true); if (!andStop) StartMoving(); }
    public void GoToTarget(bool andStop = false) { StopMoving(false); t01 = 1f; ApplyPosition(1f,true); if (!andStop) StartMoving(); }
    public void SetWaypointsXY(Vector2 newOrigin, Vector2 newTarget, bool snapToOrigin = true)
    { originXY = newOrigin; targetXY = newTarget; if (snapToOrigin) SnapToOrigin(); }

    // ======== Core Movement ========
    private IEnumerator MoveRoutine()
    {
        bool forward = (t01 <= 0.5f);

        while (isMoving)
        {
            float start = forward ? 0f : 1f;
            float end   = forward ? 1f : 0f;
            CurrentState = forward ? State.MovingForward : State.MovingBackward;

            float dist = Vector2.Distance(originXY, targetXY);
            float duration = Mathf.Max(0.0001f, dist / Mathf.Max(0.0001f, speed));

            float elapsed = 0f;

            while (elapsed < duration && isMoving)
            {
                float dt = (rb2d ? Time.fixedDeltaTime : Time.deltaTime);
                float raw = Mathf.Clamp01(elapsed / duration);
                t01 = Mathf.Lerp(start, end, ease.Evaluate(raw));

                // Apply, detect blockedDown
                ApplyPosition(t01);

                // Reverse instantly kalau ketahan saat TURUN
                if (blockedDown && !forward)
                {
                    forward = true; // balik arah (naik)
                    if (waitAtEnds > 0f) yield return new WaitForSeconds(waitAtEnds);
                    break;
                }

                elapsed += dt;
                if (rb2d) yield return new WaitForFixedUpdate();
                else yield return null;
            }

            t01 = end;
            ApplyPosition(t01);

            if (!isMoving) break;
            CurrentState = State.Idle;

            if (waitAtEnds > 0f)
                yield return new WaitForSeconds(waitAtEnds);

            if (travelMode == TravelMode.OneShot)
            { isMoving = false; CurrentState = State.Idle; mover = null; yield break; }

            forward = !forward;
        }

        CurrentState = State.Paused;
        mover = null;
    }

    private void ApplyPosition(float t, bool forceSnap = false)
    {
        blockedDown = false;

        Vector2 currentPos2 = rb2d ? rb2d.position : (Vector2)transform.position;
        Vector2 desiredPos2 = Vector2.Lerp(originXY, targetXY, t);
        Vector2 nextPos2 = desiredPos2;

        // ===== Anti-crush saat turun =====
        if (col2d && desiredPos2.y < currentPos2.y)
        {
            Vector2 dir = Vector2.down;
            float wantDist = currentPos2.y - desiredPos2.y;

            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(obstacleMask);
            filter.useTriggers = false;

            RaycastHit2D[] hits = new RaycastHit2D[8];
            int hitCount = col2d.Cast(dir, filter, hits, wantDist + skin);

            if (hitCount > 0)
            {
                float minDist = float.MaxValue;
                for (int i = 0; i < hitCount; i++)
                {
                    var h = hits[i];
                    if (h.collider == null) continue;

                    // Jika pakai filter tag, hanya terima collider dengan tag yang cocok
                    if (useTagFilter && !TagInList(h.collider.tag, obstacleTags))
                        continue;

                    if (h.distance < minDist) minDist = h.distance;
                }

                if (minDist < float.MaxValue)
                {
                    float allowed = Mathf.Max(0f, minDist - skin);
                    float want = wantDist;
                    if (allowed < want - 1e-4f)
                    {
                        nextPos2 = currentPos2 + dir * allowed;
                        blockedDown = true;
                    }
                }
            }
        }

        // === Hitung velocity ===
        float dt = (rb2d ? Time.fixedDeltaTime : Time.deltaTime);
        if (dt <= 0f) dt = Time.deltaTime;
        if (!hasLast || forceSnap) PlatformVelocity = Vector2.zero;
        else PlatformVelocity = (nextPos2 - lastPos2) / dt;

        // === Apply ===
        if (rb2d) rb2d.MovePosition(nextPos2);
        else transform.position = new Vector3(nextPos2.x, nextPos2.y, lockZ ? fixedZ : transform.position.z);

        lastPos2 = nextPos2;
        hasLast = true;
    }

    private bool TagInList(string tag, string[] list)
    {
        if (list == null || list.Length == 0) return false;
        for (int i = 0; i < list.Length; i++)
            if (!string.IsNullOrEmpty(list[i]) && tag == list[i]) return true;
        return false;
    }

    private void SnapToOrigin() { t01 = 0f; ApplyPosition(0f,true); }

    // ===== Parenting opsional =====
    void OnCollisionEnter2D(Collision2D col)
    {
        if (!parentOnContact) return;
        if (col.collider.CompareTag(passengerTag))
            col.collider.transform.SetParent(transform, true);
    }

    void OnCollisionExit2D(Collision2D col)
    {
        if (!parentOnContact) return;
        if (col.collider.CompareTag(passengerTag) && col.collider.transform.parent == transform)
            col.collider.transform.SetParent(null, true);
    }

    // ===== Gizmos =====
    void OnDrawGizmos()
    {
        Vector3 o = new Vector3(originXY.x, originXY.y, lockZ ? fixedZ : transform.position.z);
        Vector3 t = new Vector3(targetXY.x, targetXY.y, lockZ ? fixedZ : transform.position.z);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(o, t);
        Gizmos.DrawWireCube(o, Vector3.one * 0.3f);
        Gizmos.DrawWireCube(t, Vector3.one * 0.3f);
    }
}
