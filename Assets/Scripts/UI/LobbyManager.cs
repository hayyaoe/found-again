using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.Collections;


public class LobbyManager : MonoBehaviour
{
    public Button playButton;
    public TMPro.TMP_Text playButtonText; // Assign in Inspector (child TMP_Text of play button)

    [Header("Scene Loading")]
    [SerializeField] private string nextSceneName = "Prologue"; // <- set this in the Inspector

    // Store each player's current position state
    private Dictionary<string, string> playerPositions = new Dictionary<string, string>();

    // Track which player is currently hovering
    private string currentHoveringPlayer = null;

    void Start()
    {
        playButton.interactable = false;

        if (playButtonText == null)
            playButtonText = playButton.GetComponentInChildren<TMPro.TMP_Text>();
    }

    public void UpdatePlayerPosition(string playerName, string position)
    {
        playerPositions[playerName] = position;
        CheckPlayButton();
    }

    public void UpdatePlayerSelection(string playerName, string character)
    {
        Debug.Log($"{playerName} selected {character}");

        if (PlayerSelectionManager.Instance != null)
        {
            PlayerSelectionManager.Instance.RegisterSelection(playerName, character);
        }
    }

    private void CheckPlayButton()
    {
        // Need both players registered
        if (!playerPositions.ContainsKey("P1") || !playerPositions.ContainsKey("P2"))
        {
            playButton.interactable = false;
            return;
        }

        // Get current positions
        string p1 = playerPositions["P1"];
        string p2 = playerPositions["P2"];

        // ✅ Both must be on valid character spots
        bool p1Valid = (p1 == "Marie" || p1 == "Mimi");
        bool p2Valid = (p2 == "Marie" || p2 == "Mimi");

        // ❌ Disable if both are on the same character
        bool sameCharacter = (p1 == p2 && p1Valid && p2Valid);

        // ✅ Enable only if both on valid characters AND not on the same one
        playButton.interactable = p1Valid && p2Valid && !sameCharacter;

        // Optional: Debug info
        Debug.Log($"CheckPlayButton → P1: {p1}, P2: {p2}, " +
                $"BothValid: {p1Valid && p2Valid}, SameChar: {sameCharacter}, " +
                $"Interactable: {playButton.interactable}");
    }

    // called when a player moves down to Play
    public void HighlightPlayButton(string playerName)
    {
        if (!playButton.interactable) return;

        currentHoveringPlayer = playerName;
        playButton.Select();

        // determine input device type
        var cursor = FindObjectsOfType<PlayerInput>()
            .FirstOrDefault(p => p.gameObject.name.Contains(playerName));

        string text = "Press Enter to Start"; // default

        if (cursor != null)
        {
            bool usesGamepad = cursor.devices.Any(d => d is Gamepad);
            if (usesGamepad)
                text = "Press X to Start";
        }

        playButtonText.text = text;

        Debug.Log($"{playerName} hovering over Play ({text})");
    }

    // called when player moves away from Play
    public void UnhighlightPlayButton(string playerName)
    {
        if (currentHoveringPlayer == playerName)
        {
            currentHoveringPlayer = null;
            playButtonText.text = "Play";
        }
    }

    // called when actual confirm/submit button pressed (not hover)
    public void OnPlayPressed()
    {
        if (!playButton.interactable) return;
        StartCoroutine(DelayedLoad());
    }

    private IEnumerator DelayedLoad()
    {
        yield return null; // wait one frame for selections to finalize

        // NOTE: make sure the scene name exists in Build Settings (File → Build Settings → Scenes In Build)
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            // If you use a fader singleton, call it; else fall back to direct load.
            if (SceneFader.instance != null)
                SceneFader.instance.FadeToScene(nextSceneName);
            else
                SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogError("nextSceneName is empty. Please set it in the Inspector.");
        }
    }


    // external call by PlayerCursorController when confirm input pressed
    public void OnPlayerConfirm(string playerName)
    {
        if (currentHoveringPlayer == playerName && playButton.interactable)
        {
            // 🔄 Force re-register latest selection before loading
            var cursor = FindObjectsOfType<PlayerCursorController>()
                .FirstOrDefault(c => c.playerName == playerName);
            if (cursor != null)
            {
                string selectedCharacter = cursor.GetCurrentCharacter(); // ✅ get directly from PlayerCursorController
                if (!string.IsNullOrEmpty(selectedCharacter))
                    UpdatePlayerSelection(playerName, selectedCharacter);
            }
            OnPlayPressed();
        }
    }


    public void OnSubmit(InputAction.CallbackContext context)
    {
        if (!context.performed || !playButton.interactable)
            return;

        // Check if Enter key pressed
        if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            OnPlayPressed();
            return;
        }

        // Check if gamepad "South" button (X / A) pressed
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            // --- END OF CHANGE ---
            OnPlayPressed();
            return;
        }
    }
}
