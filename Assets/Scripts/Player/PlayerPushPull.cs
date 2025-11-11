using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerPushPull : MonoBehaviour
{
    [Header("Push / Pull Settings")]
    [SerializeField] private float interactRange = 1f;
    [SerializeField] private LayerMask pushableLayer;
    [SerializeField] private float pushSpeed = 3f;

    [Header("Facing / Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Front Probe (ramah slope)")]
    [Range(0.2f, 1f)][SerializeField] private float frontBoxHeightFactor = 0.8f;
    [SerializeField] private float frontBoxExtraWidth = 0.6f;
    [Tooltip("Turunin center box biar nyapu objek sedikit lebih rendah (slope).")]
    [SerializeField] private float frontProbeYOffset = 0.15f;
    [Range(0f, 45f)][SerializeField] private float downProbeAngleDeg = 15f;
    [SerializeField] private float probeRayDistance = 1.2f;

    [Header("Attach / Detach Logic")]
    [SerializeField] private float attachVerticalSlack = 0.25f;
    [SerializeField] private float leashMaxDistanceX = 1.6f;
    [SerializeField] private float leashGraceSeconds = 0.25f;

    // ====== SliderJoint2D (lock X relatif, Y bebas) ======
    [Header("Slider Joint")]
    [Tooltip("Jarak aman ekstra antar permukaan biar tidak saling menembus.")]
    [SerializeField] private float contactSkin = 0.01f;

    // ====== Slope Assist ======
    [Header("Slope Assist")]
    [Tooltip("Layer tanah buat raycast normal slope.")]
    [SerializeField] private LayerMask groundMask;
    [Tooltip("Dorongan bantu sepanjang slope (N).")]
    [SerializeField] private float slopeAssistForce = 35f;
    [Tooltip("Maks sudut slope yang masih dibantu (deg).")]
    [Range(0f, 60f)][SerializeField] private float slopeAssistMaxAngleDeg = 45f;
    [Tooltip("Hanya bantu saat benar2 menanjak relatif gravitasi.")]
    [SerializeField] private bool assistOnlyWhenUphill = true;
    [Tooltip("Kurangi friction objek sementara ketika attach.")]
    [SerializeField] private bool reduceFrictionWhileAttached = true;
    [Tooltip("Friction sementara objek saat attach (0 = licin).")]
    [Range(0f, 1f)][SerializeField] private float attachedFriction = 0.08f;

    [Header("Auto Detach (No Contact)")]
    [SerializeField] private bool autoDetachWhenNoContact = true;
    [SerializeField] private float noContactGraceSeconds = 0.12f; // buffer anti-jitter
    [SerializeField] private float nearContactEpsilon = 0.03f;

    private Animator animator;

    private float noContactTimer = 0f;
    private PushPullObject currentObject;
    private Rigidbody2D rb;
    private Rigidbody2D objectRb;
    private Collider2D selfCol;
    private Collider2D objectCol;
    private PlayerInput playerInput;
    private InputAction interactAction;
    private SliderJoint2D slider;
    private float leashTimer = 0f;
    public bool isPushing = false;
    public bool isPulling = false;
    private float horizontalInput;
    private bool facingRight = true;
    private int sideSign = 1;
    private PhysicsMaterial2D originalMat;
    private PhysicsMaterial2D runtimeMatClone;
    private bool _bootstrapped = false;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        selfCol = GetComponent<Collider2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        facingRight = transform.localScale.x >= 0f;
        animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        // Be defensive: in tests there may be no InputActionAsset at all.
        if (!playerInput) playerInput = GetComponent<PlayerInput>();
        if (playerInput != null && playerInput.actions != null)
        {
            interactAction = playerInput.actions.FindAction("Interact", throwIfNotFound: false);
            if (interactAction != null)
                interactAction.performed += OnInteractToggled;
        }
    }

    private void OnDisable()
    {
        if (interactAction != null)
            interactAction.performed -= OnInteractToggled;
    }


    private void Update()
    {
        // Stop all logic if the game is paused.
        if (typeof(PauseMenu).GetField("GameIsPaused") != null && PauseMenu.GameIsPaused)
            return;

        // Read horizontal input only if an actions map exists (tests may not have one)
        horizontalInput = 0f;
        if (playerInput != null && playerInput.actions != null)
        {
            var move = playerInput.actions.FindAction("Move", throwIfNotFound: false);
            if (move != null) horizontalInput = move.ReadValue<Vector2>().x;
        }


        if (currentObject != null && objectRb != null)
        {
            Vector2 p = rb.worldCenterOfMass;
            Vector2 o = objectRb.worldCenterOfMass;

            bool hasInput = Mathf.Abs(horizontalInput) > 0.1f;
            isPulling = hasInput && Mathf.Sign(horizontalInput) == -sideSign;

            if (isPulling) FaceTowards(Mathf.Sign(o.x - p.x));
            else if (Mathf.Abs(horizontalInput) > 0.01f) FaceTowards(Mathf.Sign(horizontalInput));

            float desiredX = p.x + sideSign * (selfCol.bounds.extents.x + objectCol.bounds.extents.x + contactSkin);
            float errX = Mathf.Abs(o.x - desiredX);

            leashTimer = (errX > leashMaxDistanceX) ? leashTimer + Time.deltaTime : 0f;
            if (leashTimer >= leashGraceSeconds) DetachObject();

            if (animator)
            {
                animator.SetBool("isInteracting", true);
                animator.SetBool("pushing", false);
                animator.SetBool("pulling", false);

                if (hasInput)
                {
                    if (isPulling) animator.SetBool("pulling", true);
                    else animator.SetBool("pushing", true);
                }
            }
        }
        else
        {
            if (Mathf.Abs(horizontalInput) > 0.01f)
                FaceTowards(Mathf.Sign(horizontalInput));

            if (animator)
            {
                animator.SetBool("isInteracting", false);
                animator.SetBool("pushing", false);
                animator.SetBool("pulling", false);
            }
        }

        if (autoDetachWhenNoContact && currentObject != null && objectCol != null && selfCol != null)
        {
            bool touching = selfCol.IsTouching(objectCol);

            if (!touching)
            {
                var dist = selfCol.Distance(objectCol);

                // 1) If overlapping, it's touching.
                if (dist.isOverlapped)
                    touching = true;
                else
                {
                    // 2) Treat "almost touching" as touching to absorb seams/jitter.
                    //    Using dist.distance is simpler and robust across collider types.
                    if (dist.distance <= nearContactEpsilon)
                        touching = true;
                }
            }

            if (!touching)
            {
                noContactTimer += Time.deltaTime;
                if (noContactTimer >= noContactGraceSeconds)
                {
                    DetachObject();
                    return;
                }
            }
            else
            {
                noContactTimer = 0f;
            }
        }
    }

    private void FixedUpdate()
    {
        if (currentObject != null)
        {
            // 1) Gerak player stabil
            var pv = rb.linearVelocity;
            pv.x = horizontalInput * pushSpeed;
            rb.linearVelocity = pv;

            // 2) Slope Assist untuk objek
            if (objectRb != null && objectCol != null && Mathf.Abs(horizontalInput) > 0.01f)
            {
                if (TryGetGroundNormal(objectCol, out var groundNormal, out float slopeAngleDeg))
                {
                    if (slopeAngleDeg <= slopeAssistMaxAngleDeg + 0.01f)
                    {
                        // Tangent slope (kanan = naik kalau normal menghadap atas)
                        Vector2 tangent = new Vector2(groundNormal.y, -groundNormal.x).normalized;
                        // Arah input sepanjang tangent
                        Vector2 moveDir = Mathf.Sign(horizontalInput) * tangent;

                        bool uphill = Vector2.Dot(moveDir, -Physics2D.gravity.normalized) > 0f;
                        if (!assistOnlyWhenUphill || uphill)
                        {
                            objectRb.AddForce(moveDir * slopeAssistForce, ForceMode2D.Force);

                            // optional: sedikit bantu player juga biar sync (lebih kecil supaya nggak “narik” player)
                            rb.AddForce(moveDir * (0.35f * slopeAssistForce), ForceMode2D.Force);
                        }
                    }
                }
            }

            Bounds b = selfCol.bounds;

            Vector2 origin = new Vector2(b.center.x, b.min.y + 0.05f);
            var hit = Physics2D.Raycast(origin, Vector2.down, 0.5f, groundMask);

            // float targetZ = 0f;
            // if (hit)
            // {
            //     Vector2 n = hit.normal.normalized;
            //     Vector2 tangent = new Vector2(n.y, -n.x);
            //     targetZ = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
            //     targetZ = Mathf.Clamp(targetZ, -50f, 50f); // batasin biar nggak ekstrem
            // }

            // float next = Mathf.MoveTowardsAngle(rb.rotation, targetZ, 360f * Time.fixedDeltaTime);
            // rb.MoveRotation(next);

        }
    }

    // --- MODIFIED ---
    // This one function now handles both attaching and detaching.
    private void OnInteractToggled(InputAction.CallbackContext ctx)
    {
        // Check if we are currently attached to an object
        if (currentObject != null)
        {
            // If we are, detach.
            DetachObject();
        }
        else
        {
            // If we are not, try to attach.
            TryAttachToFrontObject();
        }
    }
    // --- END OF MODIFICATION ---

    // --- REMOVED ---
    // We don't need OnInteractCanceled anymore.
    // private void OnInteractCanceled(InputAction.CallbackContext ctx)
    // {
    //     DetachObject();
    // }
    // --- END OF REMOVAL ---

    private void TryAttachToFrontObject()
    {
        EnsureInited();
        if (!selfCol) selfCol = GetComponent<Collider2D>();

        if (!FindFrontObjectSlopeFriendly(out PushPullObject target, out Collider2D targetCol))
            return;

        var targetRb = target.GetComponent<Rigidbody2D>();
        if (!targetRb || !selfCol || !targetCol) return;

        Vector2 p = rb.worldCenterOfMass;
        Vector2 o = targetRb.worldCenterOfMass;

        // Vertical slack check (friendlier a bit)
        float verticalDifference = Mathf.Abs(p.y - o.y);
        float objectHeight = targetCol.bounds.size.y;
        if (verticalDifference > (objectHeight * 0.75f + attachVerticalSlack))
            return;

        currentObject = target;
        objectRb = targetRb;
        objectCol = targetCol;

        sideSign = (o.x >= p.x) ? +1 : -1;

        Vector2 playerSurfaceWorld = new Vector2(
            selfCol.bounds.center.x + sideSign * (selfCol.bounds.extents.x + contactSkin),
            selfCol.bounds.center.y
        );
        Vector2 objectSurfaceWorld = new Vector2(
            objectCol.bounds.center.x - sideSign * (objectCol.bounds.extents.x + contactSkin),
            objectCol.bounds.center.y
        );

        // Create/assign the slider
        slider = gameObject.AddComponent<SliderJoint2D>();
        slider.connectedBody = objectRb;
        slider.autoConfigureAngle = false;
        slider.angle = 90f;
        slider.enableCollision = true;
        slider.autoConfigureConnectedAnchor = false;
        slider.anchor = transform.InverseTransformPoint(playerSurfaceWorld);
        slider.connectedAnchor = objectRb.transform.InverseTransformPoint(objectSurfaceWorld);
        slider.useLimits = false;
        slider.useMotor = false;

        if (reduceFrictionWhileAttached && objectCol)
        {
            originalMat = objectCol.sharedMaterial;
            runtimeMatClone = new PhysicsMaterial2D(originalMat ? originalMat.name + " (RuntimeClone)" : "PushPullRuntime")
            {
                bounciness = originalMat ? originalMat.bounciness : 0f,
                friction = attachedFriction
            };
            objectCol.sharedMaterial = runtimeMatClone;
        }

        isPushing = true;
        isPulling = false;
        leashTimer = 0f;

        currentObject.AddPushingPlayer(gameObject);
        FaceTowards(Mathf.Sign(o.x - p.x));

        if (animator)
        {
            animator.SetBool("isInteracting", true);
            animator.SetBool("pushing", false);
            animator.SetBool("pulling", false);
        }
    }

    private void DetachObject()
    {
        EnsureInited();
        if (currentObject != null)
        {
            currentObject.RemovePushingPlayer(gameObject);
            currentObject = null;
        }

        // Restore friction
        if (objectCol != null)
        {
            if (reduceFrictionWhileAttached)
            {
                objectCol.sharedMaterial = originalMat;
            }
        }
        runtimeMatClone = null;
        originalMat = null;

        objectRb = null;
        objectCol = null;

        if (slider != null)
        {
            Destroy(slider);
            slider = null;
        }

        isPushing = false;
        isPulling = false;
        leashTimer = 0f;

        if (animator)
        {
            animator.SetBool("isInteracting", false);
            animator.SetBool("pushing", false);
            animator.SetBool("pulling", false);
        }
    }

    // ==== PROBE DEPAN (ramah slope) ====
    private bool FindFrontObjectSlopeFriendly(out PushPullObject target, out Collider2D targetCol)
    {
        EnsureInited();

        target = null;
        targetCol = null;

        if (!selfCol) selfCol = GetComponent<Collider2D>();
        if (!selfCol) return false;

        // If the mask isn't set in inspector/tests, search all layers.
        int mask = pushableLayer.value == 0 ? ~0 : pushableLayer.value;

        Bounds b = selfCol.bounds;
        float dir = facingRight ? 1f : -1f;
        Vector2 forward = Vector2.right * dir;

        // Probe box in front (slightly lowered for slopes).
        float boxH = b.size.y * Mathf.Clamp01(frontBoxHeightFactor);
        float boxW = b.size.x + frontBoxExtraWidth + Mathf.Max(0.25f, interactRange);

        Vector2 boxCenter = new Vector2(
            b.center.x + dir * (b.size.x * 0.5f + (boxW - b.size.x) * 0.5f),
            b.center.y - frontProbeYOffset
        );
        Vector2 boxSize = new Vector2(boxW, boxH);

        Collider2D best = null;
        float bestProj = float.PositiveInfinity; // smallest projected distance along forward

        // 1) Overlap box
        var hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, mask);
        foreach (var h in hits)
        {
            if (!h || h.attachedRigidbody == rb) continue;
            Vector2 delta = (Vector2)h.bounds.center - (Vector2)b.center;
            float proj = Vector2.Dot(delta, forward);       // in front = positive
            if (proj <= 0f) continue;                       // behind us
            if (proj < bestProj) { bestProj = proj; best = h; }
        }

        // 2) BoxCast as a fallback (helps when the overlap box barely misses)
        if (!best)
        {
            float castDist = Mathf.Max(0.3f, interactRange + frontBoxExtraWidth);
            var cast = Physics2D.BoxCast(b.center, new Vector2(b.size.x, b.size.y * 0.9f), 0f, forward, castDist, mask);
            if (cast.collider && cast.rigidbody != rb)
                best = cast.collider;
        }

        // 3) Straight forward ray (short)
        if (!best)
        {
            var ray = Physics2D.Raycast(b.center, forward, probeRayDistance, mask);
            if (ray.collider && ray.rigidbody != rb)
                best = ray.collider;
        }

        // 4) Down-angled ray (to catch a slightly lower block on a slope)
        if (!best && downProbeAngleDeg > 0f)
        {
            float rad = downProbeAngleDeg * Mathf.Deg2Rad;
            Vector2 downDir = new Vector2(forward.x, -Mathf.Tan(rad)).normalized;
            var ray = Physics2D.Raycast(b.center, downDir, probeRayDistance, mask);
            if (ray.collider && ray.rigidbody != rb)
                best = ray.collider;
        }

        if (!best) return false;

        var pp = best.GetComponent<PushPullObject>();
        if (!pp) return false;

        target = pp;
        targetCol = best;
        return true;
    }


    // ==== Raycast normal tanah di bawah objek ====
    private bool TryGetGroundNormal(Collider2D col, out Vector2 normal, out float slopeAngleDeg)
    {
        normal = Vector2.up;
        slopeAngleDeg = 0f;

        Bounds b = col.bounds;
        Vector2 origin = new Vector2(b.center.x, b.min.y + 0.05f);
        float dist = 0.25f; // cukup pendek biar akurat di kaki
        var hit = Physics2D.Raycast(origin, Vector2.down, dist, groundMask);
        if (!hit) return false;

        normal = hit.normal.normalized;
        // sudut terhadap sumbu vertikal (0 = datar), pake arccos dot(normal, up)
        slopeAngleDeg = Mathf.Acos(Mathf.Clamp(Vector2.Dot(normal, Vector2.up), -1f, 1f)) * Mathf.Rad2Deg;
        return true;
    }

    private void FaceTowards(float dirSign)
    {
        bool wantRight = dirSign >= 0f;
        if (wantRight == facingRight) return;

        facingRight = wantRight;
        var s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (facingRight ? 1f : -1f);
        transform.localScale = s;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);

        if (selfCol != null)
        {
            Bounds b = selfCol.bounds;
            float boxH = b.size.y * Mathf.Clamp01(frontBoxHeightFactor);
            float boxW = b.size.x + frontBoxExtraWidth + interactRange;

            float dir = (transform.localScale.x >= 0f) ? 1f : -1f;
            Vector2 center = new Vector2(
                b.center.x + dir * (b.size.x * 0.5f + (boxW - b.size.x) * 0.5f),
                b.center.y - frontProbeYOffset
            );
            Vector2 size = new Vector2(boxW, boxH);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(center, size);

            // Ray gizmos (depan)
            Vector2 forward = Vector2.right * dir;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(b.center, b.center + (Vector3)(forward * probeRayDistance));

            if (downProbeAngleDeg > 0f)
            {
                float rad = downProbeAngleDeg * Mathf.Deg2Rad;
                Vector2 downDir = new Vector2(forward.x, -Mathf.Tan(rad)).normalized;
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(b.center, b.center + (Vector3)(downDir * probeRayDistance));
            }
        }
    }

    public void ForceDetach()
    {
        if (currentObject != null)
        {
            DetachObject();
        }
    }

    private void EnsureInited()
    {
        if (_bootstrapped) return;

        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!selfCol) selfCol = GetComponent<Collider2D>();
        if (!playerInput) playerInput = GetComponent<PlayerInput>();
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (!animator) animator = GetComponent<Animator>();

        // establish facing if not set yet (important in tests)
        facingRight = transform.localScale.x >= 0f;

        _bootstrapped = true;
    }
}