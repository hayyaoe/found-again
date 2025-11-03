using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class LobbyManager : MonoBehaviour
{
    public Button playButton;
    public TMPro.TMP_Text playButtonText; // Assign in Inspector (child TMP_Text of play button)

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

        // ✅ Enable play button if both are on character spots (Marie/Mimi)
        bool bothOnCharacter =
            (p1 == "Marie" || p1 == "Mimi") &&
            (p2 == "Marie" || p2 == "Mimi");

        playButton.interactable = bothOnCharacter;
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

        Debug.Log("✅ Play button pressed. Loading Prologue...");
        SceneManager.LoadScene("Prologue");
    }

    // external call by PlayerCursorController when confirm input pressed
    public void OnPlayerConfirm(string playerName)
    {
        if (currentHoveringPlayer == playerName && playButton.interactable)
        {
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
            OnPlayPressed();
            return;
        }
    }
}
