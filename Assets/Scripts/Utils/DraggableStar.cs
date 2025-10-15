using UnityEngine;

public class DraggableStar : MonoBehaviour
{
    private Camera cam;
    private bool isDragging;
    private Vector3 originalPosition;

    [Header("Movement Limit")]
    public float minX = -3f;
    public float maxX = 3f;

    [Header("Return Settings")]
    public float returnSpeed = 3f;
    private bool isReturning = false;

    void Start()
    {
        cam = Camera.main;
        originalPosition = transform.position;
    }

    void OnMouseDown()
    {
        isDragging = true;
        isReturning = false;
    }

    void OnMouseUp()
    {
        isDragging = false;
        ReturnToStart();
    }

    void Update()
    {
        if (isDragging)
        {
            // ✅ Correct world conversion for 2D camera (z = -10)
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = -cam.transform.position.z; // this is the key!
            mousePos = cam.ScreenToWorldPoint(mousePos);

            // ✅ Limit movement between minX and maxX
            float clampedX = Mathf.Clamp(mousePos.x, minX, maxX);
            transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
        }
        else if (isReturning)
        {
            transform.position = Vector3.MoveTowards(transform.position, originalPosition, returnSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, originalPosition) < 0.01f)
            {
                transform.position = originalPosition;
                isReturning = false;
            }
        }
    }

    public void ReturnToStart()
    {
        if (returnSpeed <= 0)
            transform.position = originalPosition;
        else
            isReturning = true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(minX, transform.position.y, 0),
                        new Vector3(maxX, transform.position.y, 0));
    }
}
