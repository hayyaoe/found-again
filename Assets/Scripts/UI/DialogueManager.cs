using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic; // <-- We need this for Lists
using TMPro;

[RequireComponent(typeof(PlayerInput))]
public class DialogueManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject dialogueBoxPanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject continuePrompt;
    [SerializeField] private GameObject nameBoxPanel;
    [SerializeField] private TextMeshProUGUI characterNameText; 
    [SerializeField] private Image characterLeftSprite; 
    [SerializeField] private Image characterRightSprite;

    // --- REMOVED DIALOGUE CONTENT ---
    // [SerializeField] private DialogueLine[] dialogueLines; // <-- REMOVED THIS

    [Header("Dialogue Content")]
    [SerializeField] private string cutsceneName = "cutscene_1"; // <-- ADD THIS
    
    [Header("Next Scene")]
    [SerializeField] private string nextSceneName = "Prologue";

    [Header("Character Settings")]
    [SerializeField] private Sprite mimiSprite; 
    [SerializeField] private Sprite mimiNameBoxSprite; 
    [SerializeField] private Sprite wandererSprite; 
    [SerializeField] private Sprite wandererNameBoxSprite; 
    
    [SerializeField] private Sprite defaultNameBoxSprite; 
    [SerializeField] private Color defaultNameBoxColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    
    [SerializeField] private Color speakingColor = Color.white;
    [SerializeField] private Color silentColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private RectTransform nameBoxRect;
    private Image nameBoxImage;

    private PlayerInput playerInput;
    private InputAction continueAction;
    private InputAction skipAction;

    private int currentConversationIndex = 0;
    private bool isWaitingForInput = false;

    // --- MODIFIED: This is no longer a [SerializeField] struct ---
    private struct DialogueLine
    {
        public string speakerName;
        public string line;
        public CharacterSide characterSide;
    }
    private List<DialogueLine> currentLines = new List<DialogueLine>(); // <-- NEW LIST

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
        if (dialogueBoxPanel == null || dialogueText == null || continuePrompt == null ||
            nameBoxPanel == null || characterNameText == null ||
            characterLeftSprite == null || characterRightSprite == null)
        {
            Debug.LogError("DIALOGUE MANAGER ERROR: Not all UI elements are assigned in the Inspector!");
            this.enabled = false;
            return;
        }

        // --- NEW: Load dialogue from the database ---
        LoadDialogueFromDatabase();
        // --- END NEW ---

        dialogueBoxPanel.SetActive(false);
        nameBoxPanel.SetActive(false); 
        continuePrompt.SetActive(false);
        
        characterLeftSprite.sprite = wandererSprite;
        characterLeftSprite.color = silentColor;
        characterLeftSprite.gameObject.SetActive(true);

        characterRightSprite.sprite = mimiSprite;
        characterRightSprite.color = silentColor;
        characterRightSprite.gameObject.SetActive(true);

        StartCoroutine(RunDialogue());
    }

    // --- NEW FUNCTION ---
    private void LoadDialogueFromDatabase()
    {
        if (DialogueDatabase.instance == null)
        {
            Debug.LogError("DIALOGUE MANAGER: DialogueDatabase instance is missing! Make sure it's on a persistent object from the MainMenu scene.");
            return;
        }
        
        // Ask the database for all lines matching our cutsceneName
        List<DialogueDataEntry> data = DialogueDatabase.instance.GetDialogueFor(cutsceneName);

        if (data.Count == 0)
        {
            Debug.LogError($"DIALOGUE MANAGER: No dialogue found for cutscene '{cutsceneName}'. Check your CSV and spelling.");
            return;
        }

        // Convert the database entries into the format this script understands
        currentLines.Clear();
        foreach (var entry in data)
        {
            DialogueLine newLine = new DialogueLine
            {
                speakerName = entry.speakerName,
                line = entry.dialogueLine,
                // Infer the character side from the name
                characterSide = GetSideFromName(entry.speakerName) 
            };
            currentLines.Add(newLine);
        }
    }
    
    // --- NEW FUNCTION ---
    private CharacterSide GetSideFromName(string speakerName)
    {
        if (speakerName == "Wanderer")
        {
            return CharacterSide.Left;
        }
        else if (speakerName == "Mimi")
        {
            return CharacterSide.Right;
        }
        
        return CharacterSide.None; // For "Narrator" or other speakers
    }
    // --- END NEW FUNCTIONS ---


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

    // --- MODIFIED: Check the new list 'currentLines' ---
    private IEnumerator RunDialogue()
    {
        if (currentLines.Count == 0)
        {
            StartGame();
            yield break;
        }

        dialogueBoxPanel.SetActive(true);
        StartCoroutine(ShowConversation(currentLines[currentConversationIndex]));
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

    // --- MODIFIED: Check the new list 'currentLines' ---
    private void NextConversation()
    {
        isWaitingForInput = false;
        continuePrompt.SetActive(false);

        currentConversationIndex++;

        if (currentConversationIndex < currentLines.Count)
        {
            StartCoroutine(ShowConversation(currentLines[currentConversationIndex]));
        }
        else
        {
            StartGame();
        }
    }

    private IEnumerator ShowConversation(DialogueLine currentLine)
    {
        nameBoxPanel.SetActive(false); 
        
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

        if (!string.IsNullOrEmpty(currentLine.speakerName) && currentLine.speakerName != "Narrator")
        {
            nameBoxPanel.SetActive(true);
            characterNameText.text = currentLine.speakerName;
            characterNameText.gameObject.SetActive(true);
            
            SetAnchor(currentLine.characterSide);

            if (currentLine.speakerName == "Mimi")
            {
                nameBoxImage.sprite = mimiNameBoxSprite; 
                nameBoxImage.color = Color.white; 
            }
            else if (currentLine.speakerName == "Wanderer")
            {
                nameBoxImage.sprite = wandererNameBoxSprite; 
                nameBoxImage.color = Color.white; 
            }
            else
            {
                nameBoxImage.sprite = defaultNameBoxSprite; 
                nameBoxImage.color = defaultNameBoxColor; 
            }
        }
        
        dialogueText.text = currentLine.line;
        yield return null;
        continuePrompt.SetActive(true);
        isWaitingForInput = true;
    }

    // ... (Your SetAnchor and StartGame methods are perfect and don't need changes) ...
    private void SetAnchor(CharacterSide side)
    {
        Vector2 leftPos = new Vector2(50, 20);
        Vector2 rightPos = new Vector2(-50, 20);
        
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
            // FadeManager.instance.FadeToScene(nextSceneName);
            SceneFader.instance.FadeToScene(nextSceneName);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
        }
    }
}