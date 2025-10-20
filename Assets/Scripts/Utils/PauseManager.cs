using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public GameObject pauseMenuUI; // Assign your pause menu panel in the Inspector
    public GameObject quitConfirmDialog; // Assign your "Are you sure?" panel

    void Update()
    {
        // Check if the "Cancel" button was pressed
        // On tvOS, this is the Menu button, 'O', or 'B'
        if (Input.GetButtonDown("Cancel"))
        {
            if (quitConfirmDialog.activeInHierarchy)
            {
                // If the "Are you sure?" dialog is open, "Cancel" should close it.
                quitConfirmDialog.SetActive(false);
            }
            else if (pauseMenuUI.activeInHierarchy)
            {
                // If the pause menu is open, "Cancel" should unpause.
                pauseMenuUI.SetActive(false);
                Time.timeScale = 1f; // Unpause the game
            }
            else
            {
                // If the game is running, "Cancel" should open the pause menu.
                pauseMenuUI.SetActive(true);
                Time.timeScale = 0f; // Pause the game
            }
        }
    }

    // You would call this function from a "Quit Game" button
    // in your pause menu.
    public void OnQuitButtonPress()
    {
        // Instead of quitting, show the confirmation dialog.
        quitConfirmDialog.SetActive(true);
    }

    // This function is called by the "Yes" button on your quit dialog
    public void ConfirmQuit()
    {
        // This is how you properly quit a tvOS application in code
        Application.Quit();
    }

    // This function is called by the "No" button on your quit dialog
    public void CancelQuit()
    {
        quitConfirmDialog.SetActive(false);
    }
}