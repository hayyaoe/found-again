using UnityEngine;

public class InteractableDialogue : MonoBehaviour
{
    [Header("Cutscene To Play")]
    [Tooltip("The ID from your CSV file (e.g., 'cutscene_2')")]
    [SerializeField] private string cutsceneName;

    [Header("References")]
    [Tooltip("The UI GameObject for the 'E' button prompt")]
    [SerializeField] private GameObject interactPrompt;
    
    [Tooltip("The DialogueManager that is persistent (e.g., on PrologueManager)")]
    [SerializeField] private DialogueManager dialogueManager;

    private bool hasBeenTriggered = false; // Tracks if we've played the auto-cutscene
    private PlayerInteract playerInRange; // Tracks the player who is near

    private void Start()
    {
        // Hide the prompt at the start
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(false);
        }
        
        // Safety check to find the persistent manager
        if (dialogueManager == null)
        {
            dialogueManager = FindObjectOfType<DialogueManager>();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Store the player who entered
            playerInRange = collision.GetComponent<PlayerInteract>();

            if (!hasBeenTriggered)
            {
                // FIRST TIME: Auto-play the cutscene
                hasBeenTriggered = true;
                TriggerDialogue();
            }
            else
            {
                // SUBSEQUENT TIMES: Show the "E" prompt
                if (interactPrompt != null)
                {
                    interactPrompt.SetActive(true);
                }
                
                // Tell the player's script that 'this' is now interactable
                if (playerInRange != null)
                {
                    playerInRange.SetInteractable(this);
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Hide the "E" prompt
            if (interactPrompt != null)
            {
                interactPrompt.SetActive(false);
            }
            
            // Tell the player's script that we are no longer interactable
            if (playerInRange != null)
            {
                playerInRange.ClearInteractable();
            }

            // Clear the player reference
            playerInRange = null;
        }
    }

    // This is called by the PlayerInteraction script
    public void TriggerDialogue()
    {
        if (dialogueManager != null)
        {
            dialogueManager.StartDialogue(cutsceneName);
        }
    }
}