using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;

[RequireComponent(typeof(PlayerInput))]
public class DialogueManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject dialogueBoxPanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject continuePrompt;

    [Header("Dialogue Content")]
    [TextArea(3, 10)]
    [SerializeField] private string[] conversations;

    [Header("Next Scene")]
    [SerializeField] private string nextSceneName = "Prologue";

    private PlayerInput playerInput;
    private InputAction continueAction;
    private InputAction skipAction;

    private int currentConversationIndex = 0;
    private bool isWaitingForInput = false;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        continueAction = playerInput.actions["Next"];
        skipAction = playerInput.actions["Skip"];
    }

    void Start()
    {
        // --- NEW NULL CHECKS ---
        // Check if all UI elements are assigned before we start.
        if (dialogueBoxPanel == null || dialogueText == null || continuePrompt == null)
        {
            Debug.LogError("DIALOGUE MANAGER ERROR: Not all UI elements are assigned in the Inspector!");
            
            // Disable this script to prevent further errors
            this.enabled = false; 
            return;
        }
        
        if (conversations.Length == 0)
        {
             Debug.LogWarning("DIALOGUE MANAGER: No conversations have been added.");
        }
        // --- END OF NULL CHECKS ---

        dialogueBoxPanel.SetActive(false);
        continuePrompt.SetActive(false);
        StartCoroutine(RunDialogue());
    }

    private void OnEnable()
    {
        continueAction.performed += OnContinuePressed;
        skipAction.performed += OnSkipPressed;
    }

    private void OnDisable()
    {
        continueAction.performed -= OnContinuePressed;
        skipAction.performed -= OnSkipPressed;
    }

    private IEnumerator RunDialogue()
    {
        if (conversations.Length == 0)
        {
            StartGame(); // No dialogue, just skip to the game
            yield break;
        }

        // Show the first line
        dialogueBoxPanel.SetActive(true);
        StartCoroutine(ShowConversation(conversations[currentConversationIndex]));
    }

    public void OnContinuePressed(InputAction.CallbackContext context)
    {
        if (isWaitingForInput)
        {
            NextConversation();
        }
    }

    private void OnSkipPressed(InputAction.CallbackContext context)
    {
        StartGame();
    }

    private void NextConversation()
    {
        isWaitingForInput = false;
        continuePrompt.SetActive(false);
        
        currentConversationIndex++;

        if (currentConversationIndex < conversations.Length)
        {
            StartCoroutine(ShowConversation(conversations[currentConversationIndex]));
        }
        else
        {
            StartGame();
        }
    }

    private IEnumerator ShowConversation(string line)
    {
        dialogueText.text = line;
        
        // We removed the delays, but we still need to wait one frame
        // to prevent the input from firing on the same frame.
        yield return null; 
        
        continuePrompt.SetActive(true);
        isWaitingForInput = true;
    }

    private void StartGame()
    {
        isWaitingForInput = false;
        
        // Prevent script from running again
        if (this.enabled == false) return; 
        this.enabled = false; 
        
        dialogueBoxPanel.SetActive(false);
        
        if (FadeManager.instance != null)
        {
            FadeManager.instance.FadeToScene(nextSceneName);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
        }
    }
}