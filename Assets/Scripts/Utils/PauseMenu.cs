using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement; // Needed to reload or change scenes

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pauseMenuUI; // Assign your Pause Menu Panel here

    [Header("Input")]
    [SerializeField] private PlayerInput playerInput; // Assign one of your player's PlayerInput component
    private InputAction pauseAction;

    // A static variable can be checked from any other script (e.g., to stop player movement)
    public static bool GameIsPaused { get; set; }

    private void Awake()
    {
        // Find the "Pause" action from the Action Asset assigned to the PlayerInput component
        if (playerInput != null)
        {
            pauseAction = playerInput.actions["Pause"];
        }
        else
        {
            Debug.LogError("PlayerInput is not assigned in the PauseMenu script!");
        }

        // Make sure the pause menu is hidden when the game starts
        pauseMenuUI.SetActive(false);
    }

    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.performed += TogglePause;
        }
    }

    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.performed -= TogglePause;
        }
    }

    private void TogglePause(InputAction.CallbackContext context)
    {
        if (GameIsPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f; // Resumes the flow of time
        GameIsPaused = false;
        Debug.Log("Game Resumed");
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f; // Freezes the flow of time
        GameIsPaused = true;
        Debug.Log("Game Paused");
    }

    // This function can be called by a "Main Menu" button
    public void LoadMenu()
    {
        Time.timeScale = 1f; // Important to unfreeze time before leaving the scene
        // SceneManager.LoadScene("MainMenu"); // Replace "MainMenu" with your menu scene's name
        Debug.Log("Loading Main Menu...");
        SceneManager.LoadScene("MainMenu");
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f; // Unpause before restarting

        if (CheckpointManager.instance != null)
        {
            CheckpointManager.instance.TriggerFullRespawn();
            Debug.Log("Respawning all players and resetting objects...");
        }
        else
        {
            Debug.LogWarning("CheckpointManager instance not found! Reloading scene instead.");
            // Fallback: reload scene if manager not found
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
        }

        GameIsPaused = false;
        pauseMenuUI.SetActive(false);
    }

    // This function can be called by a "Quit" button
    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
}