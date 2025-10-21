
using UnityEngine;

public class RoomZone : MonoBehaviour
{
    [Header("CameraBounds")]
    public Vector2 minBounds;
    public Vector2 maxBounds;

    [Header("Lock Settings")]
    public bool lockCamera = false;
    public Vector3 fixedCameraPosition;

    [Header("Exit Blocker")]
    public GameObject blocker;

    private void OnDrawGismos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube((minBounds + maxBounds) / 2f, maxBounds - minBounds);
    }

    public void ActivateBlocker()
    {
        if (blocker != null)
            blocker.SetActive(true);
    }
}

