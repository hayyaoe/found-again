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

    private IEnumerator Start()
    {
        // Wait one frame
        yield return null;

        // 🚫 If save exists → DO NOT play cutscene
        if (SaveSystem.HasSave())
        {
            Debug.Log("Save found → IntroCutsceneStarter will NOT play the cutscene.");
            yield break;
        }

        // ▶ If no save → play intro normally
        string introCutsceneName = dialogueManager.cutsceneName;
        dialogueManager.StartDialogue(introCutsceneName);
    }
}
