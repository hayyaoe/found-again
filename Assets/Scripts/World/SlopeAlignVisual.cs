using UnityEngine;

public class SlopeAlignVisual : MonoBehaviour
{
    [Header("Target")]
    public Transform graphics;           // child sprite/visual
    public Collider2D col;               // collider objek (boleh auto-find)
    public LayerMask groundMask = ~0;    // layer tanah

    [Header("Ray Sample")]
    public float rayDown = 0.6f;         // panjang ray ke bawah
    public float insetX = 0.05f;         // masuk sedikit dari tepi collider

    [Header("Smoothing")]
    public float alignSpeedDeg = 540f;   // kecepatan rotasi visual
    [Range(0, 70)] public float maxTilt = 50f;

    [Header("Stick (opsional)")]
    public float visualYOffset = 0f;     // kalau mau geser sprite turun dikit biar ilusi nempel

    void Reset()
    {
        col = GetComponent<Collider2D>();
        if (graphics == null && transform.childCount > 0)
            graphics = transform.GetChild(0);
    }

    void LateUpdate()
    {
        if (graphics == null || col == null) return;

        Bounds b = col.bounds;
        Vector2 leftOrigin  = new Vector2(b.min.x + insetX, b.min.y + 0.02f);
        Vector2 rightOrigin = new Vector2(b.max.x - insetX, b.min.y + 0.02f);

        // dua ray ke bawah
        var hitL = Physics2D.Raycast(leftOrigin, Vector2.down, rayDown, groundMask);
        var hitR = Physics2D.Raycast(rightOrigin, Vector2.down, rayDown, groundMask);

        float targetAngle = 0f;
        bool gotSlope = false;

        if (hitL && hitR)
        {
            // tangent dari dua titik tanah
            Vector2 tangent = (hitR.point - hitL.point).normalized;
            targetAngle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
            gotSlope = true;
        }
        else if (hitL || hitR)
        {
            // fallback: pakai normal -> tangent = (n.y, -n.x)
            var n = (hitL ? hitL.normal : hitR.normal).normalized;
            Vector2 tangent = new Vector2(n.y, -n.x);
            targetAngle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
            gotSlope = true;
        }

        if (!gotSlope) targetAngle = 0f; // di udara/datar tak terdeteksi

        // clamp & smooth
        targetAngle = Mathf.Clamp(targetAngle, -maxTilt, maxTilt);
        float current = graphics.eulerAngles.z;
        float next = Mathf.MoveTowardsAngle(current, targetAngle, alignSpeedDeg * Time.deltaTime);

        // apply
        var gPos = graphics.localPosition;
        gPos.y = visualYOffset; // biar ilusi nempel (opsional)
        graphics.localPosition = gPos;
        graphics.rotation = Quaternion.Euler(0, 0, next);
    }
}
