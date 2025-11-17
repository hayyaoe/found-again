using System.Collections.Generic;
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

    // --- THIS IS THE FIX ---
    // We use a List to track ALL players in the trigger, not just one.
    private List<PlayerInteract> playersInRange = new List<PlayerInteract>();

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
        Debug.Log($"--- TRIGGER ENTER: {collision.gameObject.name} entered.", this.gameObject);
        // Check if the object that entered is a Player
        if (collision.CompareTag("Player"))
        {
            if (!hasBeenTriggered)
            {
                // FIRST TIME: Auto-play the cutscene
                hasBeenTriggered = true;
                TriggerDialogue();
                return; // Exit here. We don't need to add them to the list yet.
            }

            // --- THIS IS FOR RE-TRIGGERING ---
            PlayerInteract player = collision.GetComponent<PlayerInteract>();

            // If we found a player script AND they are not already in our list
            if (player != null && !playersInRange.Contains(player))
            {
                // 1. Add this new player to our list
                playersInRange.Add(player);

                // 2. Tell this specific player that they can interact
                player.SetInteractable(this);

                // 3. Show the "E" prompt (it's safe to call this multiple times)
                if (interactPrompt != null)
                {
                    interactPrompt.SetActive(true);
                }
            }
        }
        else
        {
            // ADD THIS LINE
            Debug.LogWarning($"--- {collision.gameObject.name} does NOT have 'Player' tag!", this.gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        Debug.Log($"--- TRIGGER EXIT: {collision.gameObject.name} exited.", this.gameObject);
        // Check if the object that exited is a Player
        if (collision.CompareTag("Player"))
        {
            PlayerInteract player = collision.GetComponent<PlayerInteract>();

            // If this player is in our list
            if (player != null && playersInRange.Contains(player))
            {
                // 1. Tell this specific player they can no longer interact
                player.ClearInteractable();

                // 2. Remove this player from our list
                playersInRange.Remove(player);

                // 3. If the list is now empty (no players left), hide the prompt
                if (playersInRange.Count == 0 && interactPrompt != null)
                {
                    interactPrompt.SetActive(false);
                }
            }
        }
    }

    // This is called by ANY Player's PlayerInteract script
    public void TriggerDialogue()
    {
        if (dialogueManager != null)
        {
            dialogueManager.StartDialogue(cutsceneName);
        }
    }
}