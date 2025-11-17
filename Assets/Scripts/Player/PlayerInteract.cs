using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteract : MonoBehaviour
{
    // This will hold the 'grandma' object when we are close to it
    private InteractableDialogue npc;

    private PlayerInput playerInput;
    private InputAction interactAction;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogError("PlayerInteraction script needs a PlayerInput component!", this);
            return;
        }

        // Find the "Interact" action from your Input Action Asset
        interactAction = playerInput.actions["CutsceneInteract"];

        // --- ADD THIS CHECK ---
        if (interactAction == null)
        {
            Debug.LogError($"--- COULD NOT FIND ACTION NAMED 'CutsceneInteract' on {this.gameObject.name}!", this.gameObject);
        }
        else
        {
            Debug.Log($"--- Successfully found action 'CutsceneInteract' for {this.gameObject.name}.", this.gameObject);
        }
        // --- END OF CHECK ---
    }

    public void OnEnable()
    {
        if (interactAction != null)
        {
            interactAction.performed += OnInteractPressed;
        }
    }

    public void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.performed -= OnInteractPressed;
        }
    }

    // This is called when you press the "Interact" button
    private void OnInteractPressed(InputAction.CallbackContext context)
    {
        Debug.Log($"--- INTERACT PRESSED on {this.gameObject.name} ---", this.gameObject);
        // If we are near an NPC, tell it to trigger its dialogue
        if (npc != null)
        {
            // Only allow interaction if the game isn't already paused
            if (!PauseMenu.GameIsPaused)
            {
                npc.TriggerDialogue();
            }
        }
        else
        {
            // --- ADD THIS LINE ---
            Debug.LogWarning($"--- {this.gameObject.name} pressed Interact, but npc is NULL.", this.gameObject);
        }
    }

    // These two functions are called by the NPC's trigger
    public void SetInteractable(InteractableDialogue interactableNpc)
    {
        npc = interactableNpc;
    }

    public void ClearInteractable()
    {
        npc = null;
    }
}