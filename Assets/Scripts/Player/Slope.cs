using UnityEngine;

public class Slope : MonoBehaviour
{
    [Header("References")]
    private Rigidbody2D rb;
    public LayerMask groundLayer;

    [Header("Settings")]
    public float maxSlopeAngle = 45f; // Max climbable angle
    public float slideSpeed = 5f;     // Speed when sliding down

    private bool isGrounded;
    private bool isSliding;
    private Vector2 slopeNormal;

    void Update()
    {
        CheckSlope();
        HandleSlopeSliding();
    }

    void CheckSlope()
    {
        // Cast a ray down to detect slope angle
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1.2f, groundLayer);

        if (hit)
        {
            slopeNormal = hit.normal;
            float angle = Vector2.Angle(hit.normal, Vector2.up);
            isGrounded = true;

            // Check if the slope is too steep
            if (angle > maxSlopeAngle)
                isSliding = true;
            else
                isSliding = false;
        }
        else
        {
            isGrounded = false;
            isSliding = false;
        }
    }

    void HandleSlopeSliding()
    {
        if (isSliding && isGrounded)
        {
            // Calculate the direction to slide down
            Vector2 slideDir = new Vector2(slopeNormal.x, slopeNormal.y) * -1;
            slideDir = Vector2.Perpendicular(slopeNormal).normalized;

            // Apply sliding movement
            rb.linearVelocity = new Vector2(slideDir.x * slideSpeed, rb.linearVelocity.y);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Debug ray
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 1.2f);
    }
}
