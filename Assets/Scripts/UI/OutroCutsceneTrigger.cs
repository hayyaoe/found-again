using UnityEngine;
using UnityEngine.InputSystem;

public class OutroCutsceneTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Lock movement
            Movement move = other.GetComponent<Movement>();
            if (move != null) move.enabled = false;

            PlayerInput input = other.GetComponent<PlayerInput>();
            if (input != null) input.enabled = false;

            // Play Outro
            OutroCutsceneController outro = FindFirstObjectByType<OutroCutsceneController>();
            outro?.PlayOutro();
        }
    }

}
