using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    // [SerializeField] private AudioClip checkpointSound;
    private Transform currentCheckpoint;
    private Animator anim;

    public void Respawn()
    {
        transform.position = currentCheckpoint.position;

        anim.ResetTrigger("die");
        anim.Play("Idle");
    }

    // Activate checkpoints 
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.tag == "Checkpoint")
        {
            currentCheckpoint = collision.transform;
            // SoundManager.instance.PlaySound(checkpointSound); 
            collision.GetComponent<Collider2D>().enabled = false; // Deatviate checkpoint collider 
        } 
    }
}