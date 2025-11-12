using UnityEngine;

public class SlopeAlignVisual : MonoBehaviour
{
    [Header("Target")]
    public Transform graphics;
    public Collider2D col;
    public LayerMask groundMask = ~0;

    [Header("Ray Sample")]
    public float rayDown = 0.6f;
    public float insetX = 0.05f;

    [Header("Smoothing")]
    public float alignSpeedDeg = 540f;
    [Range(0, 70)] public float maxTilt = 50f;

    [Header("Stick (opsional)")]
    public float visualYOffset = 0f;

    bool warnedOnce;

    void Reset()
    {
        col = GetComponent<Collider2D>();
        AutoAssignGraphics();
    }

    void OnValidate()
    {
        if (col == null) col = GetComponent<Collider2D>();
        if (graphics == null) AutoAssignGraphics();
    }

    void Awake()
    {
        if (groundMask == ~0)
        {
            int m = LayerMask.GetMask("Ground", "Platforms", "Tilemap");
            if (m != 0) groundMask = m;
        }

        EnsureGraphicsIsChild();
    }

    void LateUpdate()
    {

        Bounds b = col.bounds;
        float minDown = b.extents.y + 0.25f;
        float rayLen = Mathf.Max(rayDown, minDown);

        Vector2 leftOrigin  = new Vector2(b.min.x + insetX, b.min.y + 0.02f);
        Vector2 rightOrigin = new Vector2(b.max.x - insetX, b.min.y + 0.02f);

        var hitL = Physics2D.Raycast(leftOrigin,  Vector2.down, rayLen, groundMask);
        var hitR = Physics2D.Raycast(rightOrigin, Vector2.down, rayLen, groundMask);

        float targetAngle;
        bool gotSlope;

        if (hitL && hitR)
        {
            Vector2 tangent = (hitR.point - hitL.point).normalized;
            targetAngle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
            gotSlope = true;
        }
        else if (hitL || hitR)
        {
            var h = hitL ? hitL : hitR;
            Vector2 n = h.normal.normalized;
            Vector2 tangent = new Vector2(n.y, -n.x);
            targetAngle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
            gotSlope = true;
        }
        else
        {
            return;
        }

        targetAngle = Mathf.Clamp(targetAngle, -maxTilt, maxTilt);

        float current = graphics.localEulerAngles.z;
        float next = Mathf.MoveTowardsAngle(current, targetAngle, alignSpeedDeg * Time.deltaTime);

        var gPos = graphics.localPosition;
        gPos.y = visualYOffset;
        graphics.localPosition = gPos;
        graphics.localRotation = Quaternion.Euler(0, 0, next);
    }

    // ---------- Helpers ----------

    void AutoAssignGraphics()
    {
        if (graphics != null) return;
        foreach (Transform t in transform)
        {
            if (t.GetComponent<SpriteRenderer>())
            {
                graphics = t;
                return;
            }
        }
        if (transform.childCount > 0) graphics = transform.GetChild(0);
    }

    void EnsureGraphicsIsChild()
    {
        if (graphics == null) return;

        if (graphics == transform)
        {
            var srOnParent = GetComponent<SpriteRenderer>();
            if (srOnParent == null)
            {
                var child = new GameObject("Graphics");
                child.transform.SetParent(transform, false);
                graphics = child.transform;
                return;
            }

            var childGO = new GameObject("Graphics");
            childGO.transform.SetParent(transform, false);
            var srChild = childGO.AddComponent<SpriteRenderer>();
            CopySpriteRenderer(srOnParent, srChild);
            srOnParent.enabled = false;

            graphics = childGO.transform;
        }
    }

    void CopySpriteRenderer(SpriteRenderer src, SpriteRenderer dst)
    {
        dst.sprite = src.sprite;
        dst.color = src.color;
        dst.flipX = src.flipX;
        dst.flipY = src.flipY;
        dst.drawMode = src.drawMode;
        dst.size = src.size;
        dst.sharedMaterial = src.sharedMaterial;
        dst.sortingLayerID = src.sortingLayerID;
        dst.sortingOrder = src.sortingOrder;
        dst.maskInteraction = src.maskInteraction;
        dst.shadowCastingMode = src.shadowCastingMode;
        dst.receiveShadows = src.receiveShadows;
        dst.spriteSortPoint = src.spriteSortPoint;
    }
}
