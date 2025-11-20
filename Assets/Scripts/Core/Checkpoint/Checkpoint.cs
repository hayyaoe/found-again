using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public string checkpointID; // Example: "CP1", "CP2", "BossCheckpoint"

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Movement player = collision.GetComponent<Movement>();
        if (player != null)
        {
            // Tell CheckpointManager
            CheckpointManager.instance.SetCurrentCheckpoint(this.transform);

            // Save progress
            SaveSystem.SaveCheckpointPosition(transform.position);
            SaveSystem.SaveCheckpointID(checkpointID);

            Debug.Log("Checkpoint reached & saved: " + checkpointID);
        }
    }
}
