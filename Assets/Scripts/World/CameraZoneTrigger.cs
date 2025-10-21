using UnityEngine;

public class CameraZoneTrigger : MonoBehaviour
{
    private CameraMovement cameraMovement;

    private void Start()
    {
        cameraMovement = FindObjectOfType<CameraMovement>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        RoomZone zone = other.GetComponent<RoomZone>();
        if (zone != null)
        {
            if (zone.lockCamera)
            {
                // Lock camera when entering this zone
                cameraMovement.LockToPosition(zone.fixedCameraPosition);
                zone.ActivateBlocker();
            }
            else
            {
                // Update bounds when entering a normal room
                cameraMovement.UnlockCamera();
                cameraMovement.UpdateBounds(zone.minBounds, zone.maxBounds);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        RoomZone zone = other.GetComponent<RoomZone>();
        if (zone != null && zone.lockCamera)
        {
            // Unlock camera when leaving the zone
            cameraMovement.UnlockCamera();
        }
    }
}
