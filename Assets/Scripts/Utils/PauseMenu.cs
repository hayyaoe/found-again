using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI; 
using System.Collections.Generic;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pauseMenuUI; 

    [Header("Controller")]
    [Tooltip("The first button to be selected when the menu opens (e.g., Resume button)")]
    [SerializeField] private GameObject firstSelectedButton; 

    [Header("Navigation Links")] 
    [SerializeField] private Slider sfxSlider; 
    [SerializeField] private Slider musicSlider; // 🟢 NEW
    [SerializeField] private Slider masterSlider; // 🟢 NEW

    [Header("Input")]
    private List<PlayerInput> allPlayers = new List<PlayerInput>();
    private List<InputAction> pauseActions = new List<InputAction>();

    public static bool GameIsPaused { get; set; }

    private void Awake()
    {
        pauseMenuUI.SetActive(false);

        allPlayers = new List<PlayerInput>(FindObjectsOfType<PlayerInput>());

        foreach (var p in allPlayers)
        {
            var action = p.actions["Pause"];
            pauseActions.Add(action);
        }
    }

    private void Start()
    {
        SetupNavigation();
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

    private void SetupNavigation()
    {
        // Ensure we have references
        if (firstSelectedButton == null || sfxSlider == null || musicSlider == null || masterSlider == null) 
            return;

        Selectable resumeSelectable = firstSelectedButton.GetComponent<Selectable>();
        Selectable sfxSelectable = sfxSlider.GetComponent<Selectable>();
        Selectable musicSelectable = musicSlider.GetComponent<Selectable>();
        Selectable masterSelectable = masterSlider.GetComponent<Selectable>();

        if (resumeSelectable != null && sfxSelectable != null && musicSelectable != null && masterSelectable != null)
        {
            // 1. Resume <-> SFX
            Navigation resumeNav = resumeSelectable.navigation;
            resumeNav.mode = Navigation.Mode.Explicit; 
            resumeNav.selectOnUp = sfxSelectable; 
            // resumeNav.selectOnDown = ... (Restart button handles this usually)
            resumeSelectable.navigation = resumeNav;

            Navigation sfxNav = sfxSelectable.navigation;
            sfxNav.mode = Navigation.Mode.Explicit;
            sfxNav.selectOnDown = resumeSelectable;
            sfxNav.selectOnUp = musicSelectable; // Go up to Music
            sfxNav.selectOnLeft = null;
            sfxNav.selectOnRight = null;
            sfxSelectable.navigation = sfxNav;

            // 2. SFX <-> Music
            Navigation musicNav = musicSelectable.navigation;
            musicNav.mode = Navigation.Mode.Explicit;
            musicNav.selectOnDown = sfxSelectable; // Go down to SFX
            musicNav.selectOnUp = masterSelectable; // Go up to Master
            musicNav.selectOnLeft = null;
            musicNav.selectOnRight = null;
            musicSelectable.navigation = musicNav;

            // 3. Music <-> Master
            Navigation masterNav = masterSelectable.navigation;
            masterNav.mode = Navigation.Mode.Explicit;
            masterNav.selectOnDown = musicSelectable; // Go down to Music
            // masterNav.selectOnUp = null; (Top of the menu)
            masterNav.selectOnLeft = null;
            masterNav.selectOnRight = null;
            masterSelectable.navigation = masterNav;
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

        EventSystem.current.SetSelectedGameObject(null);
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f; 
        GameIsPaused = true;
        Debug.Log("Game Paused");

        EventSystem.current.SetSelectedGameObject(firstSelectedButton);
    }

    public void LoadMenu()
    {
        Time.timeScale = 1f; 
        GameIsPaused = false; 
        EventSystem.current.SetSelectedGameObject(null); 
        SceneManager.LoadScene("MainMenu");
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f; 
        EventSystem.current.SetSelectedGameObject(null); 

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