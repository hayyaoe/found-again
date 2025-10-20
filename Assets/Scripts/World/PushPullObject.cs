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

    private const string LOG_TAG = "[PushPullObject]";

    private Rigidbody2D rb;
    private DraggableStar star;

    private readonly HashSet<GameObject> pushingPlayers = new();

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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        star = GetComponent<DraggableStar>();

        if (pulley == null && autoFindPulleyInParent)   pulley = GetComponentInParent<PulleySystem>();
        if (pulley == null && autoFindPulleyInChildren) pulley = GetComponentInChildren<PulleySystem>();
        if (pulley == null && autoFindPulleyInScene)    pulley = FindObjectOfType<PulleySystem>();

        initialX = rb.position.x;
        targetReturnX = initialX;

        LockObject(); // idle = freeze X

        Log($"Awake | initX={initialX:0.###}, pulley={(pulley ? pulley.name : "null")}, star={(star? "OK":"null")}, lockWhileAtLimit={lockWhileAtLimit}");
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

        // Boleh mulai hold meskipun sedang MAX:
        // - Kita izinkan hold, tapi dorongan akan di-hard stop (pin X) selama MAX.

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
        else            StopPush();
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

        // Lepas freeze X dari MAX supaya return bisa jalan
        if (xFrozenByMax) SetFreezeXByMax(false);
        blockPushAtMax = false;

        Log("StopPush.");

        // Selalu boleh mulai return meski pulley masih MAX
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
            // “re-pin” posisi X supaya benar-benar nggak geser karena impulse kecil
            rb.MovePosition(new Vector2(holdXAtMax, rb.position.y));
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        // Return linear (Kinematic)
        if (returningToStart)
        {
            returnTimer += Time.fixedDeltaTime;

            float nextX = Mathf.MoveTowards(rb.position.x, targetReturnX, returnSpeed * Time.fixedDeltaTime);
            rb.MovePosition(new Vector2(nextX, rb.position.y));
            rb.linearVelocity = Vector2.zero;

            bool arrived  = Mathf.Abs(nextX - targetReturnX) <= returnSnapEpsilon;
            bool timedOut = (returnTimeout > 0f && returnTimer >= returnTimeout);

            if (arrived || timedOut)
            {
                returningToStart = false;
                returnTimer = 0f;

                rb.bodyType = RigidbodyType2D.Dynamic;
                LockObject(); // idle = freeze X lagi
                Log($"Return {(arrived ? "arrived" : "timedOut")} at x={nextX:0.###} → LockObject()");
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
    }

    // === Helpers ===
    private bool IsPulleyAtMax()
    {
        return pulley != null && pulley.isAtMaxHeight;
    }

    // Freeze/unfreeze bit X khusus karena MAX (tidak mengganggu bit lain)
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

    // logging
    private void Log(string msg) { if (debugLogging) Debug.Log($"{LOG_TAG} [{name}] {msg}"); }
    private void LogWarning(string msg) { if (debugLogging) Debug.LogWarning($"{LOG_TAG} [{name}] {msg}"); }
    private void LogIf(string msg, bool cond) { if (cond) Debug.Log($"{LOG_TAG} [{name}] {msg}"); }
}
