using UnityEngine;
using System.Collections;

[RequireComponent(typeof(DialogueManager))]
public class IntroCutsceneStarter : MonoBehaviour
{
    private DialogueManager dialogueManager;

    private void Awake()
    {
        dialogueManager = GetComponent<DialogueManager>();
    }

    // After the scene fades in, this will run
    private IEnumerator Start()
    {
        // Wait for one frame to let everything initialize
        yield return null; 
        
        // Get the intro cutscene name from the DialogueManager's Inspector
        string introCutsceneName = dialogueManager.cutsceneName;
        
        // Manually start the intro cutscene
        dialogueManager.StartDialogue(introCutsceneName);
    }
}