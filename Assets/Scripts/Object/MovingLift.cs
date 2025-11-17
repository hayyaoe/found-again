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

    [Header("Passenger (No Parenting)")]
    [Tooltip("Tag pemain/penumpang (digunakan untuk pause-if-blocked-by-passenger).")]
    public string passengerTag = "Player";

    [Header("Anti-Crush")]
    [Tooltip("Layer penghalang (boleh banyak).")]
    public LayerMask obstacleMask;
    [Tooltip("Filter tambahan berdasarkan Tag (optional).")]
    public bool useTagFilter = false;
    [Tooltip("Tag yang dianggap penghalang bila useTagFilter = true.")]
    public string[] obstacleTags = new string[] { "Player", "Object" };
    [Min(0f)] public float skin = 0.02f;
    [Tooltip("Jika true: reverse saat tertahan (kecuali oleh passenger).")]
    public bool reverseWhenBlocked = true;
    [Tooltip("Jika true: bila yang nahan adalah passengerTag, lift PAUSE (tidak reverse) sampai clear.")]
    public bool pauseWhenBlockedByPassenger = true;

    [Header("Runtime (Read-only)")]
    [SerializeField] private bool isMoving = false;
    public State CurrentState { get; private set; } = State.Idle;
    public Vector2 PlatformVelocity { get; private set; }
    public bool IsBlockedByPassenger { get; private set; } // <- expose ke luar

    private Rigidbody2D rb2d;
    private Collider2D col2d;
    private float t01 = 0f;
    private Coroutine mover;

    private Vector2 lastPos2;
    private bool hasLast = false;
    private bool blockedDown = false;

    [Header("Puzzle Activation")]
    public bool requiresPlayersAtStart = true;     // Only blocks the very first move
    public int playersNeeded = 2;                  // Two players must stand on the lift
    private int currentPlayersOnLift = 0;          // Runtime tracking
    private bool initialMoveDone = false;          // After first trip, lift runs freely

    [Header("Lift Visuals")]
    public SpriteRenderer liftSpriteRenderer;
    public Sprite defaultSprite;
    public Sprite litSprite;


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

            // ✅ kontak & deteksi benturan lebih stabil untuk platform kinematic
            rb2d.useFullKinematicContacts = true;
            rb2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
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
        // First activation: wait for both players
        if (!initialMoveDone && requiresPlayersAtStart)
        {
            if (currentPlayersOnLift < playersNeeded)
            {
                // Do NOT move yet
                return;
            }
        }

        // Start normal movement
        if (isMoving) return;

        isMoving = true;
        mover = StartCoroutine(MoveRoutine());

        // Mark first activation complete
        if (!initialMoveDone)
            initialMoveDone = true;
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
        bool forward = (t01 <= 0.5f); // true: origin->target

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

                ApplyPosition(t01);

                // CASE 1: tertahan oleh passenger -> PAUSE di tempat (jangan reverse / jangan tambah elapsed)
                if (blockedDown && IsBlockedByPassenger && pauseWhenBlockedByPassenger)
                {
                    if (rb2d) yield return new WaitForFixedUpdate(); else yield return null;
                    continue; // ulangi frame ini sampai clear
                }

                // CASE 2: tertahan oleh obstacle lain -> REVERSE (tanpa teleport)
                if (blockedDown && !IsBlockedByPassenger && !forward && reverseWhenBlocked)
                {
                    forward = true;
                    if (waitAtEnds > 0f) yield return new WaitForSeconds(waitAtEnds);
                    goto ContinueOuter;
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

        ContinueOuter:
            continue;
        }

        CurrentState = State.Paused;
        mover = null;
    }

    private void ApplyPosition(float t, bool forceSnap = false)
    {
        blockedDown = false;
        IsBlockedByPassenger = false;

        Vector2 currentPos2 = rb2d ? rb2d.position : (Vector2)transform.position;
        Vector2 desiredPos2 = Vector2.Lerp(originXY, targetXY, t);
        Vector2 nextPos2 = desiredPos2;

        float deltaY = desiredPos2.y - currentPos2.y;

        // Cek & batasi hanya kalau bergerak TURUN
        if (col2d && deltaY < -Mathf.Epsilon)
        {
            Vector2 dir = Vector2.down;
            float wantDist = -deltaY;

            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(obstacleMask);
            filter.useTriggers = false;

            RaycastHit2D[] hits = new RaycastHit2D[8];
            int hitCount = col2d.Cast(dir, filter, hits, wantDist + skin);

            float minDist = float.MaxValue;
            Collider2D closest = null;

            for (int i = 0; i < hitCount; i++)
            {
                var h = hits[i];
                if (h.collider == null) continue;
                if (useTagFilter && !TagInList(h.collider.tag, obstacleTags)) continue;
                if (!IsBelow(h.collider)) continue;

                if (h.distance < minDist)
                {
                    minDist = h.distance;
                    closest = h.collider;
                }
            }

            if (closest != null)
            {
                float allowed = Mathf.Max(0f, minDist - skin);
                if (allowed < wantDist - 1e-4f)
                {
                    nextPos2 = currentPos2; // diam di frame ini
                    blockedDown = true;
                    if (closest.CompareTag(passengerTag)) IsBlockedByPassenger = true;
                }
            }
        }

        // Hitung velocity
        float dt = (rb2d ? Time.fixedDeltaTime : Time.deltaTime);
        if (dt <= 0f) dt = Time.deltaTime;
        if (!hasLast || forceSnap) PlatformVelocity = Vector2.zero;
        else PlatformVelocity = (nextPos2 - lastPos2) / dt;

        // Apply
        if (rb2d) rb2d.MovePosition(nextPos2);
        else transform.position = new Vector3(nextPos2.x, nextPos2.y, lockZ ? fixedZ : transform.position.z);

        lastPos2 = nextPos2;
        hasLast = true;
    }

    private bool TagInList(string tag, string[] list)
    {
        if (!useTagFilter) return true;
        if (list == null || list.Length == 0) return false;
        for (int i = 0; i < list.Length; i++)
            if (!string.IsNullOrEmpty(list[i]) && tag == list[i]) return true;
        return false;
    }

    // Relatif posisi
    private bool IsBelow(Collider2D other)
    {
        if (col2d == null || other == null) return false;
        float myBottom = col2d.bounds.min.y;
        float otherTop = other.bounds.max.y;
        return otherTop <= myBottom + 0.02f; // toleransi kecil
    }

    private void SnapToOrigin() { t01 = 0f; ApplyPosition(0f,true); }

    // === Gizmos ===
    void OnDrawGizmos()
    {
        Vector3 o = new Vector3(originXY.x, originXY.y, lockZ ? fixedZ : transform.position.z);
        Vector3 t = new Vector3(targetXY.x, targetXY.y, lockZ ? fixedZ : transform.position.z);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(o, t);
        Gizmos.DrawWireCube(o, Vector3.one * 0.3f);
        Gizmos.DrawWireCube(t, Vector3.one * 0.3f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(passengerTag))
        {
            currentPlayersOnLift++;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(passengerTag))
        {
            currentPlayersOnLift--;
            currentPlayersOnLift = Mathf.Max(0, currentPlayersOnLift);
        }
    }

    public void ActivateLift()
    {
        if (liftSpriteRenderer != null && litSprite != null)
            liftSpriteRenderer.sprite = litSprite;

        // Now attempt to move (will wait for players if required)
        StartMoving();
    }

}
