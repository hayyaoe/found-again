using UnityEngine;

public class OutroCutsceneTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            OutroCutsceneController outro = FindFirstObjectByType<OutroCutsceneController>();
            outro?.PlayOutro();
        }
    }
}
