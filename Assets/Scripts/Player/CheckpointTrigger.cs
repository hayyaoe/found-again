using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    public Checkpoint checkpoint; // assign parent Checkpoint object

    private void OnTriggerEnter2D(Collider2D collision)
    {
        checkpoint.PlayerEntered(collision);
    }
}
