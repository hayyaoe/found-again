using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems; // <-- 1. ADD THIS AT THE TOP
using System.Collections.Generic;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pauseMenuUI; // Assign your Pause Menu Panel here

    // --- 2. ADD THIS VARIABLE ---
    [Header("Controller")]
    [Tooltip("The first button to be selected when the menu opens (e.g., Resume button)")]
    [SerializeField] private GameObject firstSelectedButton;

    [Header("Input")]
    private List<PlayerInput> allPlayers = new List<PlayerInput>();
    private List<InputAction> pauseActions = new List<InputAction>();

    public static bool GameIsPaused { get; set; }

    private void Awake()
    {
        pauseMenuUI.SetActive(false);

        // Find all PlayerInput objects already in the scene
        allPlayers = new List<PlayerInput>(FindObjectsOfType<PlayerInput>());

        // Register Pause action for each player
        foreach (var p in allPlayers)
        {
            var action = p.actions["Pause"];
            pauseActions.Add(action);
        }
    }


    private void OnEnable()
    {
        foreach (var action in pauseActions)
            action.performed += TogglePause;
    }

    private void OnDisable()
    {
        foreach (var action in pauseActions)
            action.performed -= TogglePause;
    }

    public void RegisterNewPlayer(PlayerInput playerInput)
    {
        allPlayers.Add(playerInput);

        var action = playerInput.actions["Pause"];
        pauseActions.Add(action);

        action.performed += TogglePause;
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