using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    public Button startButton;
    private int confirmedPlayers = 0;

    void Start()
    {
        startButton.interactable = false;
    }

    public void PlayerConfirmed()
    {
        confirmedPlayers++;
        if (confirmedPlayers >= 2)
        {
            startButton.interactable = true;
        }
    }

    public void PlayerCancelled()
    {
        confirmedPlayers--;
    }

    public void OnStartPressed()
    {
        if (confirmedPlayers >= 2)
        {
            // --- THIS IS THE CHANGE ---
            // Use the FadeManager to load the cutscene,
            // which will then load the "Prologue"
            if (FadeManager.instance != null)
            {
                FadeManager.instance.FadeToScene("DialogueCutscene");
            }
            else
            {
                // Fallback in case the FadeManager is missing
                SceneManager.LoadScene("DialogueCutscene");
            }
            // --- END OF CHANGE ---
        }
    }
}
