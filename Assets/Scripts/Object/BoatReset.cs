using UnityEngine;

// This component uses the CheckpointManager system to reset the boat's movement state.
// It relies on the BoatMove and WaveMovement scripts being present.
[RequireComponent(typeof(BoatMove))]
// Inherits directly from MonoBehaviour to avoid the mandatory Rigidbody2D requirement.
public class BoatReset : MonoBehaviour
{
    // The BoatMove script holds the core movement logic
    private BoatMove boatMoveScript;
    private WaveMovement waveScript;

    // We manually track the initial position.
    private Vector3 initialBasePosition; 

    private void Awake()
    {
        // 1. Get components
        boatMoveScript = GetComponent<BoatMove>();
        // Ensure WaveMovement reference is available before trying to access it
        if (boatMoveScript.waveScript == null)
        {
             // Assumes WaveMovement is on the same GameObject
             boatMoveScript.waveScript = boatMoveScript.GetComponent<WaveMovement>();
        }
        waveScript = boatMoveScript.waveScript; 

        // 2. FIX: Always save the Transform's position as the initial point.
        // This guarantees we capture where the boat was placed in the editor, solving the (0,0,0) issue.
        initialBasePosition = transform.position;

        // 3. Register this object with the manager using the new method
        CheckpointManager.RegisterBoatReset(this);
        
        // 4. Synchronization: Ensure WaveMovement starts its base calculations from the correct position
        if (waveScript != null)
        {
            waveScript.SetBasePosition(initialBasePosition);
        }
    }
    
    private void OnDestroy()
    {
        // Unregister the object when destroyed
        CheckpointManager.UnregisterBoatReset(this);
    }

    // Public method called by CheckpointManager during a reset sequence.
    public void ResetObject()
    {
        // 1. Reset the WaveMovement's base position
        if (waveScript != null)
        {
            waveScript.SetBasePosition(initialBasePosition);
        }
        
        // 2. Explicitly teleport the boat's transform back to the initial position.
        transform.position = initialBasePosition;

        // 3. Reset the BoatMove's internal speed/state (clears players, stops movement, re-enables RoomZone)
        boatMoveScript.ResetMovementState();
    }
}