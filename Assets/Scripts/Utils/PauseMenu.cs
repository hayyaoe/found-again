using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems; // <-- 1. ADD THIS AT THE TOP

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pauseMenuUI; // Assign your Pause Menu Panel here

    // --- 2. ADD THIS VARIABLE ---
    [Header("Controller")]
    [Tooltip("The first button to be selected when the menu opens (e.g., Resume button)")]
    [SerializeField] private GameObject firstSelectedButton;

    [Header("Input")]
    [SerializeField] private PlayerInput playerInput; // Assign one of your player's PlayerInput component
    private InputAction pauseAction;

    public static bool GameIsPaused { get; set; }

    private void Awake()
    {
        if (playerInput != null)
        {
            pauseAction = playerInput.actions["Pause"];
        }
        else
        {
            Debug.LogError("PlayerInput is not assigned in the PauseMenu script!");
        }
        
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
        Time.timeScale = 1f; 
        GameIsPaused = false;
        Debug.Log("Game Resumed");

        // --- 3. ADD THIS LINE ---
        // Clear the selected button so the controller doesn't get stuck
        EventSystem.current.SetSelectedGameObject(null);
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f; 
        GameIsPaused = true;
        Debug.Log("Game Paused");

        // --- 4. ADD THIS LINE ---
        // Force the controller to select the "Resume" button
        EventSystem.current.SetSelectedGameObject(firstSelectedButton);
    }

    public void LoadMenu()
    {
        Time.timeScale = 1f; 
        GameIsPaused = false; // Set this just in case
        EventSystem.current.SetSelectedGameObject(null); // Clear selection
        SceneManager.LoadScene("MainMenu");
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f; 
        EventSystem.current.SetSelectedGameObject(null); // Clear selection

        if (CheckpointManager.instance != null)
        {
            CheckpointManager.instance.TriggerFullRespawn();
            Debug.Log("Respawning all players and resetting objects...");
        }
        else
        {
            Debug.LogWarning("CheckpointManager instance not found! Reloading scene instead.");
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
        }

        GameIsPaused = false;
        pauseMenuUI.SetActive(false);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
}