using UnityEngine;

public class DraggableStar : MonoBehaviour
{
    private Camera cam;
    private bool isDragging;

    void Start() => cam = Camera.main;

    void OnMouseDown() => isDragging = true;

    void OnMouseUp() => isDragging = false;

    void Update()
    {
        if (isDragging)
        {
            Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            transform.position = new Vector3(mousePos.x, transform.position.y, 0);
        }
    }
}
