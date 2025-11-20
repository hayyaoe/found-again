using UnityEngine;

public class CheckpointLocator : MonoBehaviour
{
    public static Transform GetSavedCheckpoint()
    {
        string id = SaveSystem.LoadCheckpointID();
        if (string.IsNullOrEmpty(id))
            return null;  // No save

        Checkpoint[] all = GameObject.FindObjectsOfType<Checkpoint>();

        foreach (var cp in all)
        {
            if (cp.checkpointID == id)
                return cp.transform;
        }

        return null; // Saved checkpoint not found
    }
}
