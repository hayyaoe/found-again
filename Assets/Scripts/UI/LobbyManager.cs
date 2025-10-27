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
            SceneManager.LoadScene("Prologue"); // your next scene
        }
    }
}
