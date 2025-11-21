using UnityEngine;
using System.Collections.Generic;

public class Checkpoint : MonoBehaviour
{
    public string checkpointID;
    public CheckpointUITrigger uiTrigger;

    private HashSet<Movement> playersPassed = new HashSet<Movement>();

    public void PlayerEntered(Collider2D collision)
    {
        Debug.Log("Trigger entered by: " + collision.name);

        Movement player = collision.GetComponent<Movement>();
        if (player == null)
        {
            Debug.Log("But NO Movement component found on " + collision.name);
            return;
        }

        if (playersPassed.Contains(player))
        {
            Debug.Log(player.name + " already counted!");
            return;
        }

        playersPassed.Add(player);
        Debug.Log(player.name + " passed checkpoint: " + checkpointID);

        if (playersPassed.Count >= 2)
        {
            Debug.Log("Both players detected! Activating.");
            ActivateCheckpoint();
        }
    }

    private void ActivateCheckpoint()
    {
        CheckpointManager.instance.SetCurrentCheckpoint(this.transform);

        SaveSystem.SaveCheckpointPosition(transform.position);
        SaveSystem.SaveCheckpointID(checkpointID);

        Debug.Log("CHECKPOINT ACTIVATED after 2 players passed: " + checkpointID);

        if (uiTrigger != null)
            uiTrigger.ShowSavingUI();
        else
            Debug.LogWarning("No UI Trigger assigned on checkpoint " + checkpointID);
    }
}
