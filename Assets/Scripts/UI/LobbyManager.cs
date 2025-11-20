using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using TMPro;

public class LobbyManager : MonoBehaviour
{
    [Header("Start Button References")]
    public Button playButton;
    public Image playButtonBackground; 
    public TMP_Text playButtonText;

    [Header("Back Button References")]
    public Button backButton;
    public Image backButtonImage;     
    public TMP_Text backButtonText;   

    [Header("Animation Settings")]
    public float fadeDuration = 0.15f; 

    [Header("State Colors")]
    public Color inactiveColor = new Color(1f, 1f, 1f, 0.3f); 
    public Color inactiveTextColor = Color.white;
    
    public Color readyColor = Color.white;       
    public Color readyTextColor = Color.black;
    
    public Color hoverColor = new Color(0.17f, 0.24f, 0.31f); // Dark Blue
    public Color hoverTextColor = Color.white;

    public Color pressedColor = new Color(0.1f, 0.15f, 0.2f); // 🟢 NEW: Very Dark Blue (Click)
    public Color pressedTextColor = new Color(0.7f, 0.7f, 0.7f); // Grey Text

    [Header("Scene Loading")]
    [SerializeField] private string nextSceneName = "Prologue";
    [SerializeField] private string mainMenuSceneName = "MainMenu"; 

    private Dictionary<string, string> playerPositions = new Dictionary<string, string>();
    private string currentHoveringPlayer = null;
    
    private Coroutine currentFadeRoutine; // Start Button Routine
    private Coroutine backFadeRoutine;    // Back Button Routine
    
    private bool isLoading = false; 

    void Start()
    {
        // --- Setup Start Button ---
        playButton.interactable = false;
        if (playButtonText == null) playButtonText = playButton.GetComponentInChildren<TMP_Text>();
        if (playButtonBackground == null) playButtonBackground = playButton.GetComponent<Image>();
        ApplyVisualsInstant(inactiveColor, inactiveTextColor, "Begin your journey");

        // --- Setup Back Button ---
        if (backButtonImage != null) backButtonImage.color = readyColor;
        if (backButtonText != null) backButtonText.color = readyTextColor;
    }

    public void UpdatePlayerPosition(string playerName, string position)
    {
        playerPositions[playerName] = position;
        CheckPlayButton();
    }

    public void UpdatePlayerSelection(string playerName, string character)
    {
        if (PlayerSelectionManager.Instance != null)
            PlayerSelectionManager.Instance.RegisterSelection(playerName, character);
    }

    private void CheckPlayButton()
    {
        if (isLoading) return; 

        if (!playerPositions.ContainsKey("P1") || !playerPositions.ContainsKey("P2"))
        {
            TransitionToState(inactiveColor, inactiveTextColor, "Begin your journey");
            return;
        }

        string p1 = playerPositions["P1"];
        string p2 = playerPositions["P2"];
        bool isReady = (p1 == "Marie" || p1 == "Mimi") && (p2 == "Marie" || p2 == "Mimi") && (p1 != p2);

        playButton.interactable = isReady;

        if (isReady)
        {
            if (currentHoveringPlayer == null)
                TransitionToState(readyColor, readyTextColor, "Start");
        }
        else
        {
            TransitionToState(inactiveColor, inactiveTextColor, "Begin your journey");
        }
    }

    // --------------------------
    //  START BUTTON ANIMATION
    // --------------------------
    public void HighlightPlayButton(string playerName)
    {
        if (!playButton.interactable || isLoading) return;
        currentHoveringPlayer = playerName;
        TransitionToState(hoverColor, hoverTextColor, "Begin your journey");
    }

    public void UnhighlightPlayButton(string playerName)
    {
        if (isLoading) return;
        if (currentHoveringPlayer == playerName)
        {
            currentHoveringPlayer = null;
            TransitionToState(readyColor, readyTextColor, "Start");
        }
    }

    private void TransitionToState(Color targetBg, Color targetText, string textContent)
    {
        if (playButtonText != null) playButtonText.text = textContent;
        if (currentFadeRoutine != null) StopCoroutine(currentFadeRoutine);
        currentFadeRoutine = StartCoroutine(AnimateColors(targetBg, targetText));
    }

    private IEnumerator AnimateColors(Color targetBgColor, Color targetTextColor)
    {
        float elapsed = 0f;
        Color startBg = playButtonBackground != null ? playButtonBackground.color : targetBgColor;
        Color startText = playButtonText != null ? playButtonText.color : targetTextColor;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            t = Mathf.SmoothStep(0f, 1f, t); 
            if (playButtonBackground != null) playButtonBackground.color = Color.Lerp(startBg, targetBgColor, t);
            if (playButtonText != null) playButtonText.color = Color.Lerp(startText, targetTextColor, t);
            yield return null;
        }
        if (playButtonBackground != null) playButtonBackground.color = targetBgColor;
        if (playButtonText != null) playButtonText.color = targetTextColor;
    }

    // --------------------------
    //  BACK BUTTON ANIMATION
    // --------------------------
    public void HighlightBackButton()
    {
        if (isLoading) return;
        TransitionBackState(hoverColor, hoverTextColor);
    }

    public void UnhighlightBackButton()
    {
        if (isLoading) return;
        TransitionBackState(readyColor, readyTextColor);
    }

    private void TransitionBackState(Color targetBg, Color targetText)
    {
        if (backFadeRoutine != null) StopCoroutine(backFadeRoutine);
        backFadeRoutine = StartCoroutine(AnimateBackColors(targetBg, targetText));
    }

    private IEnumerator AnimateBackColors(Color targetBgColor, Color targetTextColor)
    {
        float elapsed = 0f;
        Color startBg = backButtonImage != null ? backButtonImage.color : targetBgColor;
        Color startText = backButtonText != null ? backButtonText.color : targetTextColor;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            t = Mathf.SmoothStep(0f, 1f, t); 

            if (backButtonImage != null) 
                backButtonImage.color = Color.Lerp(startBg, targetBgColor, t);

            if (backButtonText != null) 
                backButtonText.color = Color.Lerp(startText, targetTextColor, t);

            yield return null;
        }
        if (backButtonImage != null) backButtonImage.color = targetBgColor;
        if (backButtonText != null) backButtonText.color = targetTextColor;
    }

    // --------------------------
    //  SCENE LOADING & CLICK LOGIC
    // --------------------------

    // 🟢 START BUTTON PRESSED
    public void OnPlayPressed()
    {
        if (!playButton.interactable || isLoading) return;
        isLoading = true;
        StartCoroutine(PressedSequence());
    }

    private IEnumerator PressedSequence()
    {
        // Visual: Turn Dark & say "Loading..."
        TransitionToState(pressedColor, pressedTextColor, "Loading...");
        yield return new WaitForSeconds(fadeDuration);

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            if (SceneFader.instance != null) SceneFader.instance.FadeToScene(nextSceneName);
            else SceneManager.LoadScene(nextSceneName);
        }
    }

    // 🟢 BACK BUTTON PRESSED (New Animation Logic)
    public void BackToMainMenu()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(BackPressedSequence());
    }

    private IEnumerator BackPressedSequence()
    {
        // Visual: Turn Dark
        TransitionBackState(pressedColor, pressedTextColor);
        yield return new WaitForSeconds(fadeDuration);

        if (SceneFader.instance != null) SceneFader.instance.FadeToScene(mainMenuSceneName);
        else SceneManager.LoadScene(mainMenuSceneName);
    }

    // --------------------------

    private void ApplyVisualsInstant(Color bg, Color txt, string content)
    {
        if (playButtonBackground != null) playButtonBackground.color = bg;
        if (playButtonText != null) { playButtonText.color = txt; playButtonText.text = content; }
    }

    public void OnPlayerConfirm(string playerName)
    {
        var cursor = FindObjectsOfType<PlayerCursorController>().FirstOrDefault(c => c.playerName == playerName);
        if (cursor != null && playButton.interactable)
        {
             OnPlayPressed();
        }
    }

    public void OnSubmit(InputAction.CallbackContext context)
    {
        if (!context.performed || !playButton.interactable) return;
        if ((Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame))
        {
            OnPlayPressed();
        }
    }
}