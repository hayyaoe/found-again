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
    [SerializeField] private GameObject nameBoxPanel;
    [SerializeField] private TextMeshProUGUI characterNameText; // Make sure this is a child of NameBoxPanel
    [SerializeField] private Image characterLeftSprite; 
    [SerializeField] private Image characterRightSprite;

    [Header("Dialogue Content")]
    [SerializeField] private DialogueLine[] dialogueLines;

    [Header("Next Scene")]
    [SerializeField] private string nextSceneName = "Prologue";

    [Header("Character Settings")]
    [SerializeField] private Sprite mimiSprite; 
    [SerializeField] private Sprite mimiNameBoxSprite; 
    [SerializeField] private Sprite marieSprite; 
    [SerializeField] private Sprite marieNameBoxSprite; 
    
    [SerializeField] private Sprite defaultNameBoxSprite; 
    [SerializeField] private Color defaultNameBoxColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    
    // --- NEW OPACITY SETTINGS ---
    [SerializeField] private Color speakingColor = Color.white;
    [SerializeField] private Color silentColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private RectTransform nameBoxRect;
    private Image nameBoxImage;

    private PlayerInput playerInput;
    private InputAction continueAction;
    private InputAction skipAction;

    private int currentConversationIndex = 0;
    private bool isWaitingForInput = false;

    [System.Serializable]
    public struct DialogueLine
    {
        public string speakerName;
        [TextArea(3, 10)]
        public string line;
        public CharacterSide characterSide;
    }

    public enum CharacterSide
    {
        None,
        Left,
        Right
    }

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        continueAction = playerInput.actions["Next"]; 
        skipAction = playerInput.actions["Skip"];

        if (nameBoxPanel != null)
        {
            nameBoxRect = nameBoxPanel.GetComponent<RectTransform>();
            nameBoxImage = nameBoxPanel.GetComponent<Image>();
        }
    }

    void Start()
    {
        // --- MODIFIED START ---
        // We now check for characterNameText again
        if (dialogueBoxPanel == null || dialogueText == null || continuePrompt == null ||
            nameBoxPanel == null || characterNameText == null || // Re-added check
            characterLeftSprite == null || characterRightSprite == null)
        {
            Debug.LogError("DIALOGUE MANAGER ERROR: Not all UI elements are assigned in the Inspector!");
            this.enabled = false;
            return;
        }

        if (dialogueLines.Length == 0)
        {
             Debug.LogWarning("DIALOGUE MANAGER: No dialogue lines have been added.");
        }

        dialogueBoxPanel.SetActive(false);
        nameBoxPanel.SetActive(false); 
        continuePrompt.SetActive(false);
        
        // --- NEW CHARACTER SETUP ---
        // Set up characters from the start, but make them silent
        characterLeftSprite.sprite = marieSprite; // Marie is on the left
        characterLeftSprite.color = silentColor;
        characterLeftSprite.gameObject.SetActive(true);

        characterRightSprite.sprite = mimiSprite; // Mimi is on the right
        characterRightSprite.color = silentColor;
        characterRightSprite.gameObject.SetActive(true);
        // --- END NEW CHARACTER SETUP ---

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
        if (dialogueLines.Length == 0)
        {
            StartGame();
            yield break;
        }

        dialogueBoxPanel.SetActive(true);
        StartCoroutine(ShowConversation(dialogueLines[currentConversationIndex]));
    }

    private void OnContinuePressed(InputAction.CallbackContext context)
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

        if (currentConversationIndex < dialogueLines.Length)
        {
            StartCoroutine(ShowConversation(dialogueLines[currentConversationIndex]));
        }
        else
        {
            StartGame();
        }
    }

    private IEnumerator ShowConversation(DialogueLine currentLine)
    {
        nameBoxPanel.SetActive(false); 
        
        // --- THIS IS THE NEW LOGIC ---
        // Set opacity based on who is talking
        switch (currentLine.characterSide)
        {
            case CharacterSide.Left:
                characterLeftSprite.color = speakingColor;
                characterRightSprite.color = silentColor;
                break;
            case CharacterSide.Right:
                characterLeftSprite.color = silentColor;
                characterRightSprite.color = speakingColor;
                break;
            case CharacterSide.None:
                characterLeftSprite.color = silentColor;
                characterRightSprite.color = silentColor;
                break;
        }

        // Set name box and text
        if (!string.IsNullOrEmpty(currentLine.speakerName) && currentLine.speakerName != "Narrator")
        {
            nameBoxPanel.SetActive(true);
            
            // --- FIX: ALWAYS SET THE TEXT ---
            characterNameText.text = currentLine.speakerName;
            characterNameText.gameObject.SetActive(true);
            // --- END OF FIX ---
            
            SetAnchor(currentLine.characterSide);

            if (currentLine.speakerName == "Mimi")
            {
                nameBoxImage.sprite = mimiNameBoxSprite; 
                nameBoxImage.color = Color.white; 
            }
            else if (currentLine.speakerName == "Marie")
            {
                nameBoxImage.sprite = marieNameBoxSprite; 
                nameBoxImage.color = Color.white; 
            }
            else // For any other speaker (e.g., "Grana")
            {
                nameBoxImage.sprite = defaultNameBoxSprite; 
                nameBoxImage.color = defaultNameBoxColor; 
            }
        }
        
        // --- END OF NEW LOGIC ---

        dialogueText.text = currentLine.line;
        yield return null;
        continuePrompt.SetActive(true);
        isWaitingForInput = true;
    }

    private void SetAnchor(CharacterSide side)
    {
        // Adjust these pixel values to get your perfect padding
        Vector2 leftPos = new Vector2(50, 20);  // 50px from left, 20px down from top
        Vector2 rightPos = new Vector2(-50, 20); // 50px from right, 20px down from top
        
        if (side == CharacterSide.Left)
        {
            nameBoxRect.anchorMin = new Vector2(0, 1);
            nameBoxRect.anchorMax = new Vector2(0, 1);
            nameBoxRect.pivot = new Vector2(0, 1);
            nameBoxRect.anchoredPosition = leftPos;
        }
        else if (side == CharacterSide.Right)
        {
            nameBoxRect.anchorMin = new Vector2(1, 1);
            nameBoxRect.anchorMax = new Vector2(1, 1);
            nameBoxRect.pivot = new Vector2(1, 1);
            nameBoxRect.anchoredPosition = rightPos;
        }
        else
        {
            // Default (same as left)
            nameBoxRect.anchorMin = new Vector2(0, 1);
            nameBoxRect.anchorMax = new Vector2(0, 1);
            nameBoxRect.pivot = new Vector2(0, 1);
            nameBoxRect.anchoredPosition = leftPos;
        }
    }

    private void StartGame()
    {
        isWaitingForInput = false;

        if (this.enabled == false) return;
        this.enabled = false;

        dialogueBoxPanel.SetActive(false);
        nameBoxPanel.SetActive(false);
        characterLeftSprite.gameObject.SetActive(false);
        characterRightSprite.gameObject.SetActive(false);

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