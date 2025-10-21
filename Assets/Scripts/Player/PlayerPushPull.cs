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
    [Range(0.2f, 1f)] [SerializeField] private float frontBoxHeightFactor = 0.8f;
    [SerializeField] private float frontBoxExtraWidth = 0.6f;
    [Tooltip("Turunin center box biar nyapu objek sedikit lebih rendah (slope).")]
    [SerializeField] private float frontProbeYOffset = 0.15f;
    [Range(0f, 45f)] [SerializeField] private float downProbeAngleDeg = 15f;
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
    [Range(0f, 60f)] [SerializeField] private float slopeAssistMaxAngleDeg = 45f;
    [Tooltip("Hanya bantu saat benar2 menanjak relatif gravitasi.")]
    [SerializeField] private bool assistOnlyWhenUphill = true;
    [Tooltip("Kurangi friction objek sementara ketika attach.")]
    [SerializeField] private bool reduceFrictionWhileAttached = true;
    [Tooltip("Friction sementara objek saat attach (0 = licin).")]
    [Range(0f, 1f)] [SerializeField] private float attachedFriction = 0.08f;

    private PushPullObject currentObject;
    private Rigidbody2D rb;
    private Rigidbody2D objectRb;
    private Collider2D selfCol;
    private Collider2D objectCol;

    private PlayerInput playerInput;
    private InputAction interactAction;

    // Slider dipasang di PLAYER
    private SliderJoint2D slider;

    private float leashTimer = 0f;

    public bool isPushing = false;
    private bool isPulling = false;
    private float horizontalInput;
    private bool facingRight = true;

    // sisi saat attach: +1 kalau objek di kanan player, -1 kalau di kiri (tetap sampai detach)
    private int sideSign = 1;

    // Friction swapping (safe, runtime clone)
    private PhysicsMaterial2D originalMat;
    private PhysicsMaterial2D runtimeMatClone;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        selfCol = GetComponent<Collider2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        facingRight = transform.localScale.x >= 0f;
    }

    private void OnEnable()
    {
        interactAction = playerInput.actions["Interact"];
        interactAction.performed += OnInteractPerformed;
        interactAction.canceled += OnInteractCanceled;
    }

    private void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.performed -= OnInteractPerformed;
            interactAction.canceled  -= OnInteractCanceled;
        }
    }

    private void Update()
    {
        horizontalInput = playerInput.actions["Move"].ReadValue<Vector2>().x;

        if (currentObject != null && objectRb != null)
        {
            Vector2 p = rb.worldCenterOfMass;
            Vector2 o = objectRb.worldCenterOfMass;

            // <0 = pull (menjauh), >0 = push (mendekat)
            float signedRelation = (o.x - p.x) * horizontalInput;
            isPulling = signedRelation < 0f && Mathf.Abs(horizontalInput) > 0.01f;

            if (isPulling) FaceTowards(Mathf.Sign(o.x - p.x));
            else if (Mathf.Abs(horizontalInput) > 0.01f) FaceTowards(Mathf.Sign(horizontalInput));

            // Leash berdasarkan error X dari posisi samping ideal (pakai sideSign yang dikunci)
            float desiredX = p.x + sideSign * (selfCol.bounds.extents.x + objectCol.bounds.extents.x + contactSkin);
            float errX = Mathf.Abs(o.x - desiredX);

            leashTimer = (errX > leashMaxDistanceX) ? leashTimer + Time.deltaTime : 0f;
            if (leashTimer >= leashGraceSeconds) DetachObject();
        }
        else
        {
            if (Mathf.Abs(horizontalInput) > 0.01f)
                FaceTowards(Mathf.Sign(horizontalInput));
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
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        TryAttachToFrontObject();
    }

    private void OnInteractCanceled(InputAction.CallbackContext ctx)
    {
        DetachObject();
    }

    private void TryAttachToFrontObject()
    {
        if (!FindFrontObjectSlopeFriendly(out PushPullObject target, out Collider2D targetCol))
            return;

        var targetRb = target.GetComponent<Rigidbody2D>();
        if (targetRb == null)
        {
            Debug.LogWarning("Pushable object needs a Rigidbody2D.");
            return;
        }

        Vector2 p = rb.worldCenterOfMass;
        Vector2 o = targetRb.worldCenterOfMass;

        // Toleransi slope
        float verticalDifference = Mathf.Abs(p.y - o.y);
        float objectHeight = targetCol.bounds.size.y;
        bool besideObject = verticalDifference < (objectHeight * 0.7f + attachVerticalSlack);
        if (!besideObject)
        {
            Debug.Log("Can't push from above (even with slack).");
            return;
        }

        currentObject = target;
        objectRb = targetRb;
        objectCol = targetCol;

        // LOCK sisi
        sideSign = (o.x >= p.x) ? +1 : -1;

        // Titik permukaan world di sisi player & objek
        Vector2 playerSurfaceWorld = new Vector2(
            selfCol.bounds.center.x + sideSign * (selfCol.bounds.extents.x + contactSkin),
            selfCol.bounds.center.y
        );
        Vector2 objectSurfaceWorld = new Vector2(
            objectCol.bounds.center.x - sideSign * (objectCol.bounds.extents.x + contactSkin),
            objectCol.bounds.center.y
        );

        // Buat SLIDER JOINT di PLAYER
        slider = gameObject.AddComponent<SliderJoint2D>();
        slider.connectedBody = objectRb;
        slider.autoConfigureAngle = false;
        slider.angle = 90f; // sumbu gerak vertikal => X relatif terkunci
        slider.enableCollision = true;
        slider.autoConfigureConnectedAnchor = false;

        slider.anchor = transform.InverseTransformPoint(playerSurfaceWorld);
        slider.connectedAnchor = objectRb.transform.InverseTransformPoint(objectSurfaceWorld);
        slider.useLimits = false;
        slider.useMotor  = false;

        // Turunin friction objek (runtime clone) supaya start-move di slope nggak butuh ancang2
        if (reduceFrictionWhileAttached && objectCol != null)
        {
            originalMat = objectCol.sharedMaterial;
            runtimeMatClone = new PhysicsMaterial2D(originalMat ? originalMat.name + " (RuntimeClone)" : "PushPullRuntime");
            if (originalMat != null)
            {
                runtimeMatClone.bounciness = originalMat.bounciness;
                // friction diambil dari slider; tapi kita overwrite ke nilai rendah
            }
            runtimeMatClone.friction = attachedFriction;
            objectCol.sharedMaterial = runtimeMatClone;
        }

        isPushing = true;
        isPulling = false;
        leashTimer = 0f;

        currentObject.AddPushingPlayer(gameObject);
        FaceTowards(Mathf.Sign(o.x - p.x));
    }

    private void DetachObject()
    {
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
    }

    // ==== PROBE DEPAN (ramah slope) ====
    private bool FindFrontObjectSlopeFriendly(out PushPullObject target, out Collider2D targetCol)
    {
        target = null;
        targetCol = null;
        if (selfCol == null) return false;

        Bounds b = selfCol.bounds;
        float boxH = b.size.y * Mathf.Clamp01(frontBoxHeightFactor);
        float boxW = b.size.x + frontBoxExtraWidth + interactRange;

        float dir = facingRight ? 1f : -1f;
        Vector2 center = new Vector2(
            b.center.x + dir * (b.size.x * 0.5f + (boxW - b.size.x) * 0.5f),
            b.center.y - frontProbeYOffset
        );

        Vector2 size = new Vector2(boxW, boxH);
        var hits = Physics2D.OverlapBoxAll(center, size, 0f, pushableLayer);

        Collider2D best = null;
        float bestScore = 0.3f;

        Vector2 forward = Vector2.right * dir;
        Vector2 start = b.center;

        foreach (var h in hits)
        {
            if (h == null || h.attachedRigidbody == rb) continue;
            Vector2 toObj = (Vector2)h.bounds.center - (Vector2)transform.position;
            float d = toObj.magnitude; if (d <= 0.001f) continue;
            float dot = Vector2.Dot(toObj.normalized, forward);
            float score = dot;
            if (score > bestScore) { bestScore = score; best = h; }
        }

        var hitFwd = Physics2D.Raycast(start, forward, probeRayDistance, pushableLayer);
        if (hitFwd.collider != null && hitFwd.rigidbody != rb)
        {
            if (0.95f > bestScore) { bestScore = 0.95f; best = hitFwd.collider; }
        }

        if (downProbeAngleDeg > 0f)
        {
            float rad = downProbeAngleDeg * Mathf.Deg2Rad;
            Vector2 downDir = new Vector2(forward.x, -Mathf.Tan(rad)).normalized;
            var hitDown = Physics2D.Raycast(start, downDir, probeRayDistance, pushableLayer);
            if (hitDown.collider != null && hitDown.rigidbody != rb)
            {
                if (0.85f > bestScore) { bestScore = 0.85f; best = hitDown.collider; }
            }
        }

        if (best == null) return false;

        target = best.GetComponent<PushPullObject>();
        if (target == null) return false;

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
}