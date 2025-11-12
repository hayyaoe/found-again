using UnityEngine;
using System.Collections.Generic;

public class CameraZoneTrigger : MonoBehaviour
{
    private CameraMovement cameraMovement;
    private HashSet<GameObject> playersInZone = new HashSet<GameObject>();

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private void Start()
    {
        cameraMovement = FindObjectOfType<CameraMovement>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only track objects tagged as "Player"
        if (!other.CompareTag("Player")) return;

        playersInZone.Add(other.gameObject);

        if (debugLog) Debug.Log($"{other.name} entered {gameObject.name} ({playersInZone.Count} players inside)");

        RoomZone zone = GetComponent<RoomZone>();
        if (zone == null) return;

        // --- When both players are inside ---
        if (playersInZone.Count >= 2)
        {
            if (zone.lockCamera)
            {
                cameraMovement.LockToPosition(zone.fixedCameraPosition);
                zone.ActivateBlocker();
                if (debugLog) Debug.Log("📸 Camera locked & blocker activated");
            }
            else
            {
                cameraMovement.UnlockCamera();
                cameraMovement.UpdateBounds(zone.minBounds, zone.maxBounds);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playersInZone.Remove(other.gameObject);

        if (debugLog) Debug.Log($"{other.name} left {gameObject.name} ({playersInZone.Count} players inside)");

        RoomZone zone = GetComponent<RoomZone>();
        if (zone == null) return;

        // --- If any player leaves, unlock camera ---
        if (playersInZone.Count < 1 && zone.lockCamera)
        {
            cameraMovement.UnlockCamera();
            if (debugLog) Debug.Log("📸 Camera unlocked (a player left)");
        }
    }
}
