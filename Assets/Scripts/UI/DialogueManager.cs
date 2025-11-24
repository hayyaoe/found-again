using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject dialogueBoxPanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject continuePrompt; // "Press X to continue" text/icon
    [SerializeField] private GameObject nameBoxPanel;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private Image characterLeftSprite;
    [SerializeField] private Image characterRightSprite;

    [Header("Dialogue Content")]
    [SerializeField] public string cutsceneName = "Intro";
    
    [Header("Typing Settings")]
    [SerializeField] private float typingSpeed = 0.04f; 
    private bool isTyping = false; 
    private Coroutine typingCoroutine;

    [Header("Game Start Dependencies")]
    [SerializeField] private ProloguePlayerSpawner playerSpawner;
    [SerializeField] private GameObject gameHUDCanvas;

    [Header("Character Settings")]
    [SerializeField] private Sprite mimiSprite;
    [SerializeField] private Sprite mimiNameBoxSprite;
    [SerializeField] private Sprite wandererSprite; 
    [SerializeField] private Sprite wandererNameBoxSprite; 
    [SerializeField] private Sprite marieOldSprite; 
    [SerializeField] private Sprite marieOldNameBoxSprite; 
    [SerializeField] private Sprite calliSprite; 
    [SerializeField] private Sprite calliNameBoxSprite; 
    [SerializeField] private Sprite defaultNameBoxSprite;
    [SerializeField] private Color defaultNameBoxColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color speakingColor = Color.white;
    [SerializeField] private Color silentColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private RectTransform nameBoxRect;
    private Image nameBoxImage;

    private InputAction continueAction;
    private InputAction skipAction;
    [SerializeField] private InputActionAsset inputActionAsset;
    private List<InputAction> nextActions = new List<InputAction>();
    private List<InputAction> skipActionsList = new List<InputAction>();

    private int currentConversationIndex = 0;
    private bool isWaitingForInput = false;
    private List<DialogueLine> currentLines = new List<DialogueLine>();
    private bool playersHaveSpawned = false;

    private struct DialogueLine
    {
        public string speakerName;
        public string line;
        public CharacterSide characterSide;
    }
    public enum CharacterSide { None, Left, Right }

    private void Awake()
    {
        if (inputActionAsset == null)
        {
            Debug.LogError("Input Action Asset is not assigned in the DialogueManager!", this);
            this.enabled = false;
            return;
        }

        continueAction = inputActionAsset.FindActionMap("Cutscene").FindAction("Next");
        skipAction = inputActionAsset.FindActionMap("Cutscene").FindAction("Skip");

        if (nameBoxPanel != null)
        {
            nameBoxRect = nameBoxPanel.GetComponent<RectTransform>();
            nameBoxImage = nameBoxPanel.GetComponent<Image>();
        }

        // 🟢 FIX: Force hide ALL dialogue UI elements immediately on initialization.
        // This prevents sprites from showing up if they were left active in the Editor scene.
        if (dialogueBoxPanel != null) dialogueBoxPanel.SetActive(false);
        if (nameBoxPanel != null) nameBoxPanel.SetActive(false);
        if (continuePrompt != null) continuePrompt.SetActive(false);
        if (characterLeftSprite != null) characterLeftSprite.gameObject.SetActive(false);
        if (characterRightSprite != null) characterRightSprite.gameObject.SetActive(false);
    }

    public void StartDialogue(string cutsceneID)
    {
        this.enabled = true;
        this.currentConversationIndex = 0;
        this.cutsceneName = cutsceneID;
        
        if (dialogueBoxPanel == null || dialogueText == null || continuePrompt == null)
        {
            Debug.LogError("DIALOGUE MANAGER ERROR: UI elements missing!");
            StartGame(); 
            return;
        }

        LoadDialogueFromDatabase();
        if (currentLines.Count == 0)
        {
            StartGame(); 
            return;
        }

        Time.timeScale = 0f;
        SwitchAllPlayerMaps("Cutscene");

        dialogueBoxPanel.SetActive(true);
        nameBoxPanel.SetActive(false);
        
        // Ensure prompt is visible at start (if you want it visible)
        continuePrompt.SetActive(true);

        characterLeftSprite.sprite = wandererSprite;
        characterLeftSprite.color = silentColor;
        characterLeftSprite.gameObject.SetActive(true);

        characterRightSprite.sprite = mimiSprite;
        characterRightSprite.color = silentColor;
        characterRightSprite.gameObject.SetActive(true);

        StartCoroutine(RunDialogue());
    }

    void Start()
    {
        // 🟢 NOTE: Even though Awake hid them, we keep this logic to ensure game flow is correct
        if (SaveSystem.HasSave())
        {
            Debug.Log("Save found -> skipping cutscene.");
            
            // Ensure they stay hidden
            dialogueBoxPanel.SetActive(false);
            nameBoxPanel.SetActive(false);
            characterLeftSprite.gameObject.SetActive(false);
            characterRightSprite.gameObject.SetActive(false);

            Time.timeScale = 1f;
            SwitchAllPlayerMaps("Player");
            StartCoroutine(SpawnAfterDelay());
            this.enabled = false;
            return;
        }
        
        // If no save, we explicitly start dialogue, which will turn sprites back ON
        StartDialogue(this.cutsceneName);
    }

    private CharacterSide GetSideFromName(string speakerName)
    {
        if (speakerName == "Wanderer" || speakerName == "Marie") return CharacterSide.Left;
        if (speakerName == "Mimi" || speakerName == "Calli") return CharacterSide.Right;
        return CharacterSide.None; 
    }

    private void SwitchAllPlayerMaps(string mapName)
    {
        if (CheckpointManager.instance == null || CheckpointManager.allPlayers == null) return;
        foreach (Movement player in CheckpointManager.allPlayers)
        {
            if (player != null)
            {
                PlayerInput pi = player.GetComponent<PlayerInput>();
                if (pi != null) pi.SwitchCurrentActionMap(mapName);
            }
        }
    }

    private IEnumerator ShowConversation(DialogueLine currentLine)
    {
        isWaitingForInput = false; 
        
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

            if (currentLine.speakerName == "Mimi") { nameBoxImage.sprite = mimiNameBoxSprite; nameBoxImage.color = Color.white; characterRightSprite.sprite = mimiSprite; }
            else if (currentLine.speakerName == "Wanderer") { nameBoxImage.sprite = wandererNameBoxSprite; nameBoxImage.color = Color.white; characterLeftSprite.sprite = wandererSprite; }
            else if (currentLine.speakerName == "Marie") { nameBoxImage.sprite = marieOldNameBoxSprite; nameBoxImage.color = Color.white; characterLeftSprite.sprite = marieOldSprite; }
            else if (currentLine.speakerName == "Calli") { nameBoxImage.sprite = calliNameBoxSprite; nameBoxImage.color = Color.white; characterRightSprite.sprite = calliSprite; }
            else { nameBoxImage.sprite = defaultNameBoxSprite; nameBoxImage.color = defaultNameBoxColor; }
        }

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeLine(currentLine.line));
        yield return null; 
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueText.text = ""; 

        foreach (char letter in line.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        FinishTyping(line);
    }

    private void FinishTyping(string fullLine)
    {
        isTyping = false;
        dialogueText.text = fullLine;
        
        if (continuePrompt != null) continuePrompt.SetActive(true);
        
        isWaitingForInput = true;
    }

    public void OnContinuePressed(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (isTyping)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            FinishTyping(currentLines[currentConversationIndex].line);
        }
        else if (isWaitingForInput)
        {
            NextConversation();
        }
    }

    private void OnEnable()
    {
        continueAction?.Enable();
        skipAction?.Enable();
        continueAction.performed += OnContinuePressed;
        skipAction.performed += OnSkipPressed;
    }

    private void OnDisable()
    {
        if (continueAction != null) continueAction.performed -= OnContinuePressed;
        if (skipAction != null) skipAction.performed -= OnSkipPressed;
        continueAction?.Disable();
        skipAction?.Disable();
        foreach (var a in nextActions) a.performed -= OnContinuePressed;
        foreach (var b in skipActionsList) b.performed -= OnSkipPressed;
    }

    private void StartGame()
    {
        // FIX: Make sure input actions are disabled BEFORE destroying/disabling object 
        if (continueAction != null) continueAction.performed -= OnContinuePressed;
        if (skipAction != null) skipAction.performed -= OnSkipPressed;

        continueAction?.Disable();
        skipAction?.Disable();

        foreach (var a in nextActions) a.performed -= OnContinuePressed;
        foreach (var b in skipActionsList) b.performed -= OnSkipPressed;

        nextActions.Clear();
        skipActionsList.Clear();

        isWaitingForInput = false;
        isTyping = false;
        if (this.enabled == false) return;
        Time.timeScale = 1f;
        dialogueBoxPanel.SetActive(false);
        nameBoxPanel.SetActive(false);
        characterLeftSprite.gameObject.SetActive(false);
        characterRightSprite.gameObject.SetActive(false);
        this.enabled = false;
        SwitchAllPlayerMaps("Player");
        if (!playersHaveSpawned)
        {
            if (playerSpawner != null) { playerSpawner.StartSpawning(); playersHaveSpawned = true; }
            if (gameHUDCanvas != null) gameHUDCanvas.SetActive(true);
        }
    }

    private void LoadDialogueFromDatabase()
    {
        if (DialogueDatabase.instance == null) return;
        List<DialogueDataEntry> data = DialogueDatabase.instance.GetDialogueFor(cutsceneName);
        if (data.Count == 0) return;

        currentLines.Clear();
        foreach (var entry in data)
        {
            currentLines.Add(new DialogueLine
            {
                speakerName = entry.speakerName,
                line = entry.dialogueLine,
                characterSide = GetSideFromName(entry.speakerName)
            });
        }
    }

    private IEnumerator RunDialogue()
    {
        if (currentLines.Count == 0) { StartGame(); yield break; }
        dialogueBoxPanel.SetActive(true);
        StartCoroutine(ShowConversation(currentLines[currentConversationIndex]));
    }

    private void OnSkipPressed(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (this == null || !this.enabled) return;  // SAFETY CHECK
        StartGame();
    }

    private void NextConversation()
    {
        isWaitingForInput = false;
        
        // Keep prompt visible if desired
        // if (continuePrompt != null) continuePrompt.SetActive(false);

        currentConversationIndex++;
        if (currentConversationIndex < currentLines.Count)
            StartCoroutine(ShowConversation(currentLines[currentConversationIndex]));
        else
            StartGame();
    }

    private void SetAnchor(CharacterSide side)
    {
        Vector2 leftPos = new Vector2(50, -20);
        Vector2 rightPos = new Vector2(-50, -20);
        if (side == CharacterSide.Left) { nameBoxRect.anchorMin = new Vector2(0, 1); nameBoxRect.anchorMax = new Vector2(0, 1); nameBoxRect.pivot = new Vector2(0, 1); nameBoxRect.anchoredPosition = leftPos; }
        else if (side == CharacterSide.Right) { nameBoxRect.anchorMin = new Vector2(1, 1); nameBoxRect.anchorMax = new Vector2(1, 1); nameBoxRect.pivot = new Vector2(1, 1); nameBoxRect.anchoredPosition = rightPos; }
        else { nameBoxRect.anchorMin = new Vector2(0, 1); nameBoxRect.anchorMax = new Vector2(0, 1); nameBoxRect.pivot = new Vector2(0, 1); nameBoxRect.anchoredPosition = leftPos; }
    }

    private IEnumerator SpawnAfterDelay()
    {
        yield return null;
        yield return new WaitForSeconds(0.01f);
        if (!playersHaveSpawned) { playerSpawner.StartSpawning(); playersHaveSpawned = true; }
        if (gameHUDCanvas != null) gameHUDCanvas.SetActive(true);
    }

    public void RegisterNewPlayer(PlayerInput player)
    {
        if (dialogueBoxPanel != null && dialogueBoxPanel.activeInHierarchy) player.SwitchCurrentActionMap("Cutscene");
        else player.SwitchCurrentActionMap("Player");
        var next = player.actions["Next"];
        var skip = player.actions["Skip"];
        if (next != null) { nextActions.Add(next); next.performed += OnContinuePressed; }
        if (skip != null) { skipActionsList.Add(skip); skip.performed += OnSkipPressed; }
    }
}