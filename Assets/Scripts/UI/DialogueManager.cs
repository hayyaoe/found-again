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
    [SerializeField] private GameObject continuePrompt;
    [SerializeField] private GameObject nameBoxPanel;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private Image characterLeftSprite;
    [SerializeField] private Image characterRightSprite;

    [Header("Dialogue Content")]
    [SerializeField] public string cutsceneName = "Intro";

    [Header("Game Start Dependencies")]
    [SerializeField] private ProloguePlayerSpawner playerSpawner;
    [SerializeField] private GameObject gameHUDCanvas;

    [Header("Character Settings")]
    [SerializeField] private Sprite mimiSprite;
    [SerializeField] private Sprite mimiNameBoxSprite;

    // --- Renamed 'marie' to 'wanderer' to be clear ---
    [SerializeField] private Sprite wandererSprite; // Your original WandererHD.png
    [SerializeField] private Sprite wandererNameBoxSprite; // Your original WandererLabel.png

    // --- ADDED NEW CHARACTERS ---
    [SerializeField] private Sprite marieOldSprite; // Drag OldMarieHD.jpg here
    [SerializeField] private Sprite marieOldNameBoxSprite; // (Optional) Drag a name label for her here
    [SerializeField] private Sprite calliSprite; // Drag CalliHD.jpg here
    [SerializeField] private Sprite calliNameBoxSprite; // (Optional) Drag a name label for her here
                                                        // --- END OF NEW CHARACTERS ---

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
    }

    public void StartDialogue(string cutsceneID)
    {
        // 1. Wake up this component and activate its input
        this.enabled = true;

        // Reset the conversation index to the beginning
        this.currentConversationIndex = 0;

        // 2. Set the new cutscene name
        this.cutsceneName = cutsceneID;
        

        // 3. Check if UI is assigned
        if (dialogueBoxPanel == null || dialogueText == null || continuePrompt == null ||
            nameBoxPanel == null || characterNameText == null ||
            characterLeftSprite == null || characterRightSprite == null)
        {
            Debug.LogError("DIALOGUE MANAGER ERROR: Not all UI elements are assigned in the Inspector!");
            StartGame(); // Fail safe
            return;
        }

        // 4. Load the new dialogue
        LoadDialogueFromDatabase();
        if (currentLines.Count == 0)
        {
            StartGame(); // No dialogue found, just start
            return;
        }

        // 5. Pause the game and show the dialogue
        Time.timeScale = 0f;
        SwitchAllPlayerMaps("Cutscene");
        // PauseMenu.GameIsPaused = true; 

        dialogueBoxPanel.SetActive(true);
        nameBoxPanel.SetActive(false);
        continuePrompt.SetActive(false);

        // (Your character setup logic)
        characterLeftSprite.sprite = wandererSprite;
        characterLeftSprite.color = silentColor;
        characterLeftSprite.gameObject.SetActive(true);

        characterRightSprite.sprite = mimiSprite;
        characterRightSprite.color = silentColor;
        characterRightSprite.gameObject.SetActive(true);

        // 6. Start the first line of dialogue
        StartCoroutine(RunDialogue());
    }

    void Start()
    {
        if (SaveSystem.HasSave())
        {
            Debug.Log("Save found → skipping cutscene and spawning players after scene loads.");

            // Hide UI, unpause, etc.
            dialogueBoxPanel.SetActive(false);
            nameBoxPanel.SetActive(false);
            characterLeftSprite.gameObject.SetActive(false);
            characterRightSprite.gameObject.SetActive(false);

            Time.timeScale = 1f;
            SwitchAllPlayerMaps("Player");

            // 🔥 Wait for checkpoints to initialize
            StartCoroutine(SpawnAfterDelay());

            this.enabled = false;
            return;
        }


        // No save → Play intro normally
        StartDialogue(this.cutsceneName);
    }


    // --- UPDATED GetSideFromName ---
    private CharacterSide GetSideFromName(string speakerName)
    {
        // This function decides which side of the screen a character is on.
        if (speakerName == "Wanderer")
        {
            return CharacterSide.Left;
        }
        else if (speakerName == "Marie") // This is Old Marie
        {
            return CharacterSide.Left;
        }
        else if (speakerName == "Mimi")
        {
            return CharacterSide.Right;
        }
        else if (speakerName == "Calli")
        {
            return CharacterSide.Right;
        }

        return CharacterSide.None; // For "Narrator" or other speakers
    }

    private void SwitchAllPlayerMaps(string mapName)
    {
        if (CheckpointManager.instance == null || CheckpointManager.allPlayers == null)
        {
            Debug.LogWarning("DialogueManager: CheckpointManager or player list not found. Cannot switch player maps.");
            return;
        }

        // Loop through all players found by the CheckpointManager
        foreach (Movement player in CheckpointManager.allPlayers)
        {
            if (player != null)
            {
                PlayerInput pi = player.GetComponent<PlayerInput>();
                if (pi != null)
                {
                    pi.SwitchCurrentActionMap(mapName);
                }
            }
        }
    }

    // private void SetAllPlayerInput(bool enabled)
    // {
    //     if (CheckpointManager.instance == null || CheckpointManager.allPlayers == null)
    //     {
    //         Debug.LogWarning("DialogueManager: CheckpointManager or player list not found. Cannot toggle player input.");
    //         return;
    //     }

    //     // Loop through all players found by the CheckpointManager
    //     foreach (Movement player in CheckpointManager.allPlayers)
    //     {
    //         if (player != null)
    //         {
    //             PlayerInput pi = player.GetComponent<PlayerInput>();
    //             if (pi != null)
    //             {
    //                 if (enabled)
    //                 {
    //                     pi.ActivateInput();
    //                 }
    //                 else
    //                 {
    //                     pi.DeactivateInput();
    //                 }
    //             }
    //         }
    //     }
    // }

    // --- UPDATED ShowConversation ---
    private IEnumerator ShowConversation(DialogueLine currentLine)
    {
        nameBoxPanel.SetActive(false);

        // Set character sprites and opacity
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
            characterNameText.text = currentLine.speakerName;
            characterNameText.gameObject.SetActive(true);

            SetAnchor(currentLine.characterSide);

            // --- THIS LOGIC IS NOW EXPANDED ---
            if (currentLine.speakerName == "Mimi")
            {
                nameBoxImage.sprite = mimiNameBoxSprite;
                nameBoxImage.color = Color.white;
                characterRightSprite.sprite = mimiSprite; // Ensure correct sprite is showing
            }
            else if (currentLine.speakerName == "Wanderer")
            {
                nameBoxImage.sprite = wandererNameBoxSprite;
                nameBoxImage.color = Color.white;
                characterLeftSprite.sprite = wandererSprite; // Ensure correct sprite is showing
            }
            else if (currentLine.speakerName == "Marie") // This is Old Marie
            {
                nameBoxImage.sprite = marieOldNameBoxSprite;
                nameBoxImage.color = Color.white;
                characterLeftSprite.sprite = marieOldSprite; // Show Old Marie sprite
            }
            else if (currentLine.speakerName == "Calli")
            {
                nameBoxImage.sprite = calliNameBoxSprite;
                nameBoxImage.color = Color.white;
                characterRightSprite.sprite = calliSprite; // Show Calli sprite
            }
            else // For any other speaker
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

    // --- All other functions (StartGame, LoadDialogue, Awake, etc.) ---
    // --- are unchanged and correct. ---

    // (Pasting the rest of your script for completeness)
    private void OnEnable()
    {
        continueAction?.Enable();
        skipAction?.Enable();

        continueAction.performed += OnContinuePressed;
        skipAction.performed += OnSkipPressed;
    }

    private void OnDisable()
    {
        // --- OLD SYSTEM CLEANUP (if still used) ---
        if (continueAction != null)
            continueAction.performed -= OnContinuePressed;

        if (skipAction != null)
            skipAction.performed -= OnSkipPressed;

        continueAction?.Disable();
        skipAction?.Disable();


        // --- NEW MULTIPLAYER SYSTEM CLEANUP ---
        foreach (var a in nextActions)
            a.performed -= OnContinuePressed;

        foreach (var b in skipActionsList)
            b.performed -= OnSkipPressed;
    }

    private void StartGame()
    {
        isWaitingForInput = false;

        if (this.enabled == false) return;

        Time.timeScale = 1f;
        // PauseMenu.GameIsPaused = false; 

        dialogueBoxPanel.SetActive(false);
        nameBoxPanel.SetActive(false);
        characterLeftSprite.gameObject.SetActive(false);
        characterRightSprite.gameObject.SetActive(false);

        // // Restore player controls
        // if (playerInput != null)
        // {
        //     playerInput.SwitchCurrentActionMap("Player");
        // }

        this.enabled = false;

        SwitchAllPlayerMaps("Player");

        if (!playersHaveSpawned)
        {
            if (playerSpawner != null)
            {
                playerSpawner.StartSpawning();
                playersHaveSpawned = true;
            }
            else
            {
                Debug.LogError("PlayerSpawner is not assigned in the DialogueManager Inspector!", this);
            }

            if (gameHUDCanvas != null)
            {
                gameHUDCanvas.SetActive(true);
            }
            else
            {
                Debug.LogWarning("Game HUD Canvas is not assigned in the DialogueManager Inspector.", this);
            }
        }
    }

    private void LoadDialogueFromDatabase()
    {
        if (DialogueDatabase.instance == null)
        {
            Debug.LogError("DIALOGUE MANAGER: DialogueDatabase instance is missing! Make sure it's on a persistent object from the MainMenu scene.");
            return;
        }

        List<DialogueDataEntry> data = DialogueDatabase.instance.GetDialogueFor(cutsceneName);

        if (data.Count == 0)
        {
            Debug.LogError($"DIALOGUE MANAGER: No dialogue found for cutscene '{cutsceneName}'. Check your CSV and spelling.");
            return;
        }

        currentLines.Clear();
        foreach (var entry in data)
        {
            DialogueLine newLine = new DialogueLine
            {
                speakerName = entry.speakerName,
                line = entry.dialogueLine,
                characterSide = GetSideFromName(entry.speakerName)
            };
            currentLines.Add(newLine);
        }
    }

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

        if (currentConversationIndex < currentLines.Count)
        {
            StartCoroutine(ShowConversation(currentLines[currentConversationIndex]));
        }
        else
        {
            StartGame();
        }
    }

    private void SetAnchor(CharacterSide side)
    {
        Vector2 leftPos = new Vector2(50, -20);
        Vector2 rightPos = new Vector2(-50, -20);

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

    private IEnumerator SpawnAfterDelay()
    {
        yield return null;
        yield return new WaitForSeconds(0.01f);

        if (!playersHaveSpawned)
        {
            playerSpawner.StartSpawning();
            playersHaveSpawned = true;
        }
        else
        {
            Debug.Log("Players already spawned — skipping spawn.");
        }


        if (gameHUDCanvas != null)
            gameHUDCanvas.SetActive(true);

        Debug.Log("Players spawned AFTER delay — checkpoint loading should now work.");
    }

    public void RegisterNewPlayer(PlayerInput player)
    {
        // If dialogue is active → use Cutscene map
        if (dialogueBoxPanel != null && dialogueBoxPanel.activeInHierarchy)
            player.SwitchCurrentActionMap("Cutscene");
        else
            player.SwitchCurrentActionMap("Player");

        // Bind actions if the cutscene map is available
        var next = player.actions["Next"];
        var skip = player.actions["Skip"];

        if (next != null)
        {
            nextActions.Add(next);
            next.performed += OnContinuePressed;
        }

        if (skip != null)
        {
            skipActionsList.Add(skip);
            skip.performed += OnSkipPressed;
        }
    }
}