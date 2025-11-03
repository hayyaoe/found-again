using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    public Button playButton;

    // Store each player's current position state
    private Dictionary<string, string> playerPositions = new Dictionary<string, string>();

    void Start()
    {
        playButton.interactable = false;
    }

    public void UpdatePlayerPosition(string playerName, string position)
    {
        // position can be "Marie", "Mimi", "Center", or "Play"
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

    public void OnPlayPressed()
    {
        if (playButton.interactable)
        {
            SceneManager.LoadScene("Prologue");
        }
    }

    public void HighlightPlayButton(string playerName)
    {
        Debug.Log($"{playerName} moved down to Play Button");
        playButton.Select();

        if (playButton.interactable)
        {
            OnPlayPressed();
        }
    }
}
