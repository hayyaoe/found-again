using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuLogic : MonoBehaviour
{
    public string cutsceneScene = "Cutscene";
    public void PlayGame()
    {
        // SceneManager.LoadScene("PlayerSelectMenu");
        SaveSystem.ClearSave(); // Remove old checkpoint
        PlayerPrefs.DeleteAll();
        
        PlayerPrefs.SetInt("IsNewGame", 1); // ← NEW
        PlayerPrefs.Save();

        SceneFader.instance.FadeToScene("PlayerSelectMenu");
    }

    public void FinalStartAfterSelection()
    {
        // This method will run after players confirm on LobbyManager
        SceneFader.instance.FadeToScene(cutsceneScene);
    }

    public void ContinueGame()
    {
        if (!SaveSystem.HasSave())
        {
            Debug.Log("No save file found!");
            return;
        }

        PlayerPrefs.SetInt("IsNewGame", 0); // ← NEW
        PlayerPrefs.Save();
        
        SceneFader.instance.FadeToScene("PlayerSelectMenu"); // Load your level
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void GoToOptionsMenu()
    {
        SceneManager.LoadScene("OptionsMenu");
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
