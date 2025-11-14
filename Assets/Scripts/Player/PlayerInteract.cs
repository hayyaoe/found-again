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
        // If we are near an NPC, tell it to trigger its dialogue
        if (npc != null)
        {
            // Only allow interaction if the game isn't already paused
            if (!PauseMenu.GameIsPaused)
            {
                npc.TriggerDialogue();
            }
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