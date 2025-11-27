using UnityEngine;

public class CameraBoundaries : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Camera cam;
    [SerializeField] private float wallThickness = 2f;
    [SerializeField] private bool confinePlayersToCamera = true;

    private BoxCollider2D leftWall;
    private BoxCollider2D rightWall;
    private BoxCollider2D topWall;
    private BoxCollider2D bottomWall;

    private float camHeight;
    private float camWidth;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        CreateBoundaries();
    }

    private void LateUpdate()
    {
        if (confinePlayersToCamera && cam != null)
        {
            UpdateBoundariesToCamera();
        }
    }

    private void CreateBoundaries()
    {
        GameObject wallsParent = new GameObject("CameraWalls");
        wallsParent.transform.parent = transform;
        wallsParent.transform.localPosition = Vector3.zero;

        leftWall = CreateWall(wallsParent, "LeftWall");
        rightWall = CreateWall(wallsParent, "RightWall");
        // topWall = CreateWall(wallsParent, "TopWall");
        // bottomWall = CreateWall(wallsParent, "BottomWall");
    }

    private BoxCollider2D CreateWall(GameObject parent, string name)
    {
        GameObject wall = new GameObject(name);
        wall.transform.parent = parent.transform;
        wall.layer = LayerMask.NameToLayer("Default"); 
        
        BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
        // Frictionless so players slide against the screen edge
        col.sharedMaterial = new PhysicsMaterial2D { friction = 0f, bounciness = 0f };
        return col;
    }

    private void UpdateBoundariesToCamera()
    {
        camHeight = 2f * cam.orthographicSize;
        camWidth = camHeight * cam.aspect;

        Vector2 camPos = cam.transform.position;

        // Left Wall
        leftWall.size = new Vector2(wallThickness, camHeight + 2f); 
        leftWall.transform.position = new Vector3(camPos.x - (camWidth / 2f) - (wallThickness / 2f), camPos.y, 0);

        // Right Wall
        rightWall.size = new Vector2(wallThickness, camHeight + 2f);
        rightWall.transform.position = new Vector3(camPos.x + (camWidth / 2f) + (wallThickness / 2f), camPos.y, 0);

        // // Top Wall
        // topWall.size = new Vector2(camWidth + 2f, wallThickness);
        // topWall.transform.position = new Vector3(camPos.x, camPos.y + (camHeight / 2f) + (wallThickness / 2f), 0);

        // // Bottom Wall
        // bottomWall.size = new Vector2(camWidth + 2f, wallThickness);
        // bottomWall.transform.position = new Vector3(camPos.x, camPos.y - (camHeight / 2f) - (wallThickness / 2f), 0);
    }

    public void SetBoundariesActive(bool active)
    {
        if (leftWall != null) leftWall.enabled = active;
        if (rightWall != null) rightWall.enabled = active;
    }

}