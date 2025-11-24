using System.Collections;              // <=== penting buat IEnumerator
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class PushPullObject : MonoBehaviour
{
    [HideInInspector] public bool isBeingPushed = false;

    [Header("Co-op")]
    [SerializeField] private bool requiresTwoPlayers = false;

    [Header("Pulley Limit Lock")]
    [Tooltip("Jika true, saat pulley di MAX pemain tidak bisa mendorong lebih jauh. Objek tetap bebas & akan return saat player lepas.")]
    [SerializeField] private bool lockWhileAtLimit = true;

    [Header("Pulley Reference")]
    [SerializeField] private PulleySystem pulley; // assign via Inspector
    [SerializeField] private bool autoFindPulleyInParent = true;
    [SerializeField] private bool autoFindPulleyInChildren = false;
    [SerializeField] private bool autoFindPulleyInScene = false;

    [Header("Return Settings")]
    [SerializeField] private float returnSpeed = 3f;
    [SerializeField] private float returnSnapEpsilon = 0.01f;
    [SerializeField] private float returnTimeout = 4f; // 0 = nonaktif

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;
    [SerializeField] private bool logEveryFixed = false;

    [Header("Return Collision")]
    [SerializeField] private bool stopReturnOnCollision = true;
    [Tooltip("Layer objek yang boleh menghentikan return (mis. 'Pushable').")]
    [SerializeField] private LayerMask returnBlockerMask;
    [SerializeField] private float returnCastSkin = 0.02f;

    private const string LOG_TAG = "[PushPullObject]";

    private Rigidbody2D rb;
    private DraggableStar star;
    private Collider2D col;

    private readonly HashSet<GameObject> pushingPlayers = new();

    // return-to-start tracking
    private bool returningToStart;
    private float initialX;
    private float targetReturnX;
    private float returnTimer;

    // pulley state
    private bool pulleyHardLocked;   // true saat isAtMax
    private bool blockPushAtMax;     // saat MAX & di-hold, blok dorong (tanpa lepas interaksi)

    // --- MAX brake state ---
    private bool xFrozenByMax = false; // apakah FreezePositionX aktif karena MAX
    private float holdXAtMax = 0f;     // X saat pertama kali kena MAX ketika di-hold

    // casts buffer
    private readonly RaycastHit2D[] castHits = new RaycastHit2D[8];

    // ===================== SFX =====================
    [Header("SFX")]
    [SerializeField] private AudioClip slideLoopSFX;   // file looping
    [SerializeField] private float slideLoopVolume = 0.7f;

    [Header("Speed Thresholds")]
    [SerializeField] private float slideStartSpeed = 0.02f;
    [SerializeField] private float volumeMaxSpeed = 2f;

    [Header("Fade")]
    [SerializeField] private float fadeInSpeed = 10f;
    [SerializeField] private float fadeOutSpeed = 12f;

    // internal SFX state
    private AudioSource activeLoopSource = null;
    private bool wasSliding = false;
    private float currentVolume = 0f;
    // ===================== END SFX =====================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        star = GetComponent<DraggableStar>();
        col = GetComponent<Collider2D>();
        if (col == null) LogWarning("Collider2D tidak ditemukan. Return-collision guard tidak akan bekerja.");

        // Auto-setup layer "Pushable" jika belum diisi di Inspector
        if (returnBlockerMask.value == 0)
            returnBlockerMask = LayerMask.GetMask("Pushable");

        if (pulley == null && autoFindPulleyInParent) pulley = GetComponentInParent<PulleySystem>();
        if (pulley == null && autoFindPulleyInChildren) pulley = GetComponentInChildren<PulleySystem>();
        if (pulley == null && autoFindPulleyInScene) pulley = FindFirstObjectByType<PulleySystem>();

        initialX = rb.position.x;
        targetReturnX = initialX;

        LockObject(); // idle = freeze X

        Log($"Awake | initX={initialX:0.###}, pulley={(pulley ? pulley.name : "null")}, star={(star ? "OK" : "null")}, lockWhileAtLimit={lockWhileAtLimit}");
        if (lockWhileAtLimit && pulley == null)
            LogWarning("lockWhileAtLimit=TRUE tapi PulleySystem BELUM di-assign.");
    }

    public void SetPulley(PulleySystem externalPulley)
    {
        pulley = externalPulley;
        Log($"Pulley reference SET → {(pulley ? pulley.name : "null")}");
    }

    // === Public API (called by player) ===
    public void AddPushingPlayer(GameObject player)
    {
        Log($"AddPushingPlayer by {player.name} | push={isBeingPushed} return={returningToStart} hardLocked={pulleyHardLocked} atLimit={IsPulleyAtMax()}");

        // Cancel return bila user hold lagi
        if (returningToStart)
        {
            returningToStart = false;
            returnTimer = 0f;
            if (star != null) star.CancelReturn();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            Log("Return canceled by player hold.");
        }

        if (pushingPlayers.Add(player))
        {
            UpdatePushState();
            Log($"Player {player.name} added. PushingCount={pushingPlayers.Count}");
        }
    }

    public void RemovePushingPlayer(GameObject player)
    {
        if (pushingPlayers.Remove(player))
        {
            Log($"Player {player.name} removed. PushingCount={pushingPlayers.Count}");
            UpdatePushState();
        }
    }

    private void UpdatePushState()
    {
        bool shouldPush = requiresTwoPlayers ? pushingPlayers.Count >= 2 : pushingPlayers.Count > 0;

        if (shouldPush) StartPush();
        else StopPush();
    }

    public void StartPush()
    {
        if (isBeingPushed) { Log("StartPush ignored: already pushing."); return; }

        isBeingPushed = true;
        returningToStart = false;
        returnTimer = 0f;

        UnlockObject(); // Dynamic + FreezeRotation only
        Log($"StartPush → UnlockObject (cons={rb.constraints})");
    }

    public void StopPush()
    {
        if (!isBeingPushed) { Log("StopPush ignored: not pushing."); return; }

        isBeingPushed = false;

        ForceStopSlideSFX();   // matiin loop kalau ada

        // Lepas freeze X dari MAX supaya return bisa jalan
        if (xFrozenByMax) SetFreezeXByMax(false);
        blockPushAtMax = false;

        Log("StopPush.");

        // Mulai return (meski pulley masih MAX)
        if (star != null)
        {
            targetReturnX = initialX;

            star.ReturnToStart();
            returningToStart = true;
            returnTimer = 0f;

            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            Log($"Return started → Kinematic. targetX={targetReturnX:0.###}, returnSpeed={returnSpeed}");
        }
        else
        {
            LockObject();
            Log("No DraggableStar → LockObject immediately.");
        }
    }

    private void FixedUpdate()
    {
        if (logEveryFixed)
        {
            Log($"FIXED t={Time.time:0.000} | x={rb.position.x:0.###} vX={rb.linearVelocity.x:0.###} push={isBeingPushed} return={returningToStart} hard={pulleyHardLocked} atMax={IsPulleyAtMax()} body={rb.bodyType} cons={rb.constraints}");
        }

        // --- Pulley MAX handler (interaction-only hard stop) ---
        if (lockWhileAtLimit && IsPulleyAtMax())
        {
            if (!pulleyHardLocked)
            {
                pulleyHardLocked = true;
                blockPushAtMax = true;

                if (isBeingPushed)
                {
                    holdXAtMax = rb.position.x;
                    SetFreezeXByMax(true); // benar-benar berhenti di X
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                    Log($"Pulley hit MAX → HARD STOP at X={holdXAtMax:0.###} (freeze X)");
                }
                else
                {
                    Log("Pulley hit MAX (no push) → waiting for release/hold.");
                }
            }
        }
        else if (pulleyHardLocked)
        {
            pulleyHardLocked = false;
            blockPushAtMax = false;

            // Lepas freeze X yang dipasang karena MAX
            SetFreezeXByMax(false);
            Log("Pulley left MAX → release hard stop (unfreeze X).");
        }

        // Jika sedang di-hold DAN masih MAX → pastikan tetap terpaku di X yang dipin
        if (isBeingPushed && blockPushAtMax && xFrozenByMax)
        {
            rb.MovePosition(new Vector2(holdXAtMax, rb.position.y));
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        // ===== Return linear (Kinematic) — depenetrate dulu, lalu sweep sampai jarak aman =====
        if (returningToStart)
        {
            returnTimer += Time.fixedDeltaTime;

            // A) Depenetration bila sudah overlap dengan blocker (hindari clip)
            ResolveInitialOverlap();

            // B) Hitung langkah target & batasi via sweep
            float posX = rb.position.x;
            float maxStep = returnSpeed * Time.fixedDeltaTime;
            float desired = Mathf.MoveTowards(posX, targetReturnX, maxStep);
            float rawStep = desired - posX;

            float safeStep = ComputeSafeStep(rawStep);
            float nextX = posX + safeStep;

            // C) Gerakkan
            rb.MovePosition(new Vector2(nextX, rb.position.y));
            rb.linearVelocity = Vector2.zero;

            bool arrived = Mathf.Abs(nextX - targetReturnX) <= returnSnapEpsilon;
            bool timedOut = (returnTimeout > 0f && returnTimer >= returnTimeout);

            if (arrived || timedOut)
            {
                returningToStart = false;
                returnTimer = 0f;

                rb.bodyType = RigidbodyType2D.Dynamic;
                LockObject(); // idle = freeze X
                Log($"Return {(arrived ? "arrived" : "timedOut")} at x={nextX:0.###} → LockObject()");
            }
            else
            {
                if (Mathf.Approximately(safeStep, 0f))
                    LogIf("Return blocked this frame (waiting for clear path)...", debugLogging && !logEveryFixed);
            }
        }

        // Idle fallback → pastikan terkunci X
        if (!isBeingPushed && !returningToStart && pushingPlayers.Count == 0)
        {
            if ((rb.constraints & RigidbodyConstraints2D.FreezePositionX) == 0)
            {
                LockObject();
                Log("Idle fallback → LockObject()");
            }
        }

        SlideSFX();
    }

    // === Helpers ===
    private bool IsPulleyAtMax()
    {
        return pulley != null && pulley.isAtMaxHeight;
    }

    private void SetFreezeXByMax(bool on)
    {
        if (on)
        {
            if (!xFrozenByMax)
            {
                rb.constraints |= RigidbodyConstraints2D.FreezePositionX;
                xFrozenByMax = true;
                LogIf("Freeze X BY MAX → ON", debugLogging && !logEveryFixed);
            }
        }
        else
        {
            if (xFrozenByMax)
            {
                rb.constraints &= ~RigidbodyConstraints2D.FreezePositionX;
                xFrozenByMax = false;
                LogIf("Freeze X BY MAX → OFF", debugLogging && !logEveryFixed);
            }
        }
    }

    private void LockObject()
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        rb.angularVelocity = 0f;
        LogIf($"LockObject | cons={rb.constraints}", debugLogging && !logEveryFixed);
    }

    private void UnlockObject()
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        LogIf($"UnlockObject | cons={rb.constraints}", debugLogging && !logEveryFixed);
    }

    // ===== Return collision utilities =====
    private ContactFilter2D MakeReturnFilter()
    {
        var f = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = returnBlockerMask,
            useTriggers = false
        };
        return f;
    }

    private void ResolveInitialOverlap()
    {
        if (!stopReturnOnCollision || col == null) return;

        var filter = MakeReturnFilter();
        Collider2D[] overlaps = new Collider2D[8];
        int n = col.Overlap(filter, overlaps);
        if (n <= 0) return;

        float pullX = 0f;

        for (int i = 0; i < n; i++)
        {
            var other = overlaps[i];
            if (other == null) continue;
            if (other.GetComponent<PushPullObject>() == null) continue;

            ColliderDistance2D d = col.Distance(other);
            if (d.isOverlapped)
            {
                Vector2 pull = d.normal * d.distance;
                pullX += pull.x;
            }
        }

        if (Mathf.Abs(pullX) > 1e-6f)
        {
            rb.MovePosition(new Vector2(rb.position.x + pullX, rb.position.y));
            rb.linearVelocity = Vector2.zero;
            Log($"Depenetrate X by {pullX:0.###}");
        }
    }

    private float ComputeSafeStep(float rawStep)
    {
        if (!stopReturnOnCollision || col == null || Mathf.Approximately(rawStep, 0f))
            return rawStep;

        float sign = Mathf.Sign(rawStep);
        float stepAbs = Mathf.Abs(rawStep);

        var filter = MakeReturnFilter();
        int hits = col.Cast(sign > 0 ? Vector2.right : Vector2.left, filter, castHits, stepAbs + returnCastSkin);
        if (hits <= 0) return rawStep;

        float minDist = float.PositiveInfinity;
        for (int i = 0; i < hits; i++)
        {
            var h = castHits[i];
            if (h.collider == null) continue;
            if (h.collider.GetComponent<PushPullObject>() == null) continue;

            if (h.distance < minDist) minDist = h.distance;
        }

        if (float.IsPositiveInfinity(minDist))
            return rawStep;

        float safe = Mathf.Max(0f, minDist - returnCastSkin);
        float allowed = Mathf.Min(stepAbs, safe);
        return sign * allowed;
    }

    // ================== SFX ==================
    private void SlideSFX()
    {
        if (SoundFXManager.instance == null || slideLoopSFX == null) return;

        float speedX = Mathf.Abs(rb.linearVelocity.x);
        bool slidingNow = isBeingPushed && !pulleyHardLocked && speedX > slideStartSpeed;

        // START SLIDING
        if (slidingNow && !wasSliding)
        {
            activeLoopSource = SoundFXManager.instance.CreateLoopingSFX(
                slideLoopSFX,
                transform.position,
                0f
            );
            currentVolume = 0f;
        }

        // UPDATE LOOP
        if (slidingNow && activeLoopSource != null)
        {
            float t = Mathf.InverseLerp(slideStartSpeed, volumeMaxSpeed, speedX);
            float targetVol = Mathf.Lerp(0.1f, slideLoopVolume, t);

            currentVolume = Mathf.MoveTowards(currentVolume, targetVol, fadeInSpeed * Time.fixedDeltaTime);

            activeLoopSource.volume = currentVolume;
            activeLoopSource.transform.position = transform.position;
        }

        // STOP SLIDING (by speed)
        if (!slidingNow && wasSliding)
        {
            StartCoroutine(FadeOutAndKill());
        }

        wasSliding = slidingNow;
    }

    private IEnumerator FadeOutAndKill()
    {
        if (activeLoopSource == null) yield break;

        while (currentVolume > 0.01f)
        {
            currentVolume -= fadeOutSpeed * Time.deltaTime;
            if (activeLoopSource != null)
                activeLoopSource.volume = currentVolume;

            yield return null;
        }

        if (activeLoopSource != null)
            Destroy(activeLoopSource.gameObject);

        activeLoopSource = null;
        currentVolume = 0f;
    }

    private void ForceStopSlideSFX()
    {
        if (activeLoopSource != null)
        {
            Destroy(activeLoopSource.gameObject);
            activeLoopSource = null;
        }

        currentVolume = 0f;
        wasSliding = false;
    }

    // ================== END SFX ==================

    // logging
    private void Log(string msg) { if (debugLogging) Debug.Log($"{LOG_TAG} [{name}] {msg}"); }
    private void LogWarning(string msg) { if (debugLogging) Debug.LogWarning($"{LOG_TAG} [{name}] {msg}"); }
    private void LogIf(string msg, bool cond) { if (cond) Debug.Log($"{LOG_TAG} [{name}] {msg}"); }
}
