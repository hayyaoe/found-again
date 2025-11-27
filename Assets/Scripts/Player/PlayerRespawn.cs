using UnityEngine;
using System.Collections;

public class PlayerRespawn : MonoBehaviour
{
    // [SerializeField] private AudioClip checkpointSound;
    
    // We NO LONGER need a private 'currentCheckpoint' here.
    // The manager will handle it.
    
    private Animator anim;
    private Vector3 startPosition; // Each player still has its own start position
    private CameraBoundaries CameraBoundariesInstance;


    private void Awake()
    {
        anim = GetComponent<Animator>();
        startPosition = transform.position;

        // Automatically find the CameraBoundaries script in the scene
        CameraBoundariesInstance = FindObjectOfType<CameraBoundaries>();
    }

    public void Respawn()
    {
        // --- THIS IS THE FIX ---
        // Ask the CheckpointManager where to go
        // 1. Disable boundaries temporarily
        if (CameraBoundariesInstance != null)
            CameraBoundariesInstance.SetBoundariesActive(false);

        if (CheckpointManager.instance.currentCheckpoint != null)
        {
            // If the manager has a checkpoint, go there
            transform.position = CheckpointManager.instance.currentCheckpoint.position;
        }
        else
        {
            // If not, go back to our own personal start position
            transform.position = startPosition;
        }
        // --- END OF FIX ---

        // 3. Wait for camera to catch up, then enable boundaries
            StartCoroutine(ReenableBoundsAfterDelay(4f));

        if (anim != null)
        {
            anim.ResetTrigger("die");
            anim.Play("Idle");
        }
    }

    // Activate checkpoints 
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.tag == "Checkpoint")
        {
            // --- THIS IS THE FIX ---
            // Tell the manager that we just hit a new checkpoint
            CheckpointManager.instance.SetCurrentCheckpoint(collision.transform);
            
            // SoundManager.instance.PlaySound(checkpointSound); 
            collision.GetComponent<Collider2D>().enabled = false; // Deactivate checkpoint collider 
        } 
    }
    private IEnumerator ReenableBoundsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (CameraBoundariesInstance != null)
            CameraBoundariesInstance.SetBoundariesActive(true);
    }
}