using UnityEngine;

public class CutsceneTrigger : MonoBehaviour
{
    [Header("Cutscene To Play")]
    [Tooltip("The ID from your CSV file (e.g., 'cutscene_2')")]
    [SerializeField] private string cutsceneName;

    [Header("References")]
    [Tooltip("Drag the object that has your DialogueManager script on it (e.g., PrologueManager)")]
    [SerializeField] private DialogueManager dialogueManager;

    private bool hasBeenTriggered = false;

    private void Start()
    {
        // Safety check in case you forgot to drag it in
        if (dialogueManager == null)
        {
            dialogueManager = FindObjectOfType<DialogueManager>();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the player entered and the trigger hasn't been used yet
        if (!hasBeenTriggered && collision.CompareTag("Player"))
        {
            // Mark as triggered so it only runs once
            hasBeenTriggered = true;
            
            // Tell the DialogueManager to start this new cutscene
            dialogueManager.StartDialogue(cutsceneName);
            
            // Disable the collider so it doesn't run again
            GetComponent<Collider2D>().enabled = false;
        }
    }
}