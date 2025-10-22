using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    // This 'static instance' lets any script easily find this manager
    public static CheckpointManager instance;

    // This is the one, shared checkpoint for ALL players
    public Transform currentCheckpoint { get; private set; }

    private void Awake()
    {
        // This is the "Singleton" pattern. It ensures there is only
        // one instance of this manager.
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Optional: keeps checkpoint between levels
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // A public method so players can update the checkpoint
    public void SetCurrentCheckpoint(Transform newCheckpoint)
    {
        currentCheckpoint = newCheckpoint;
    }
}