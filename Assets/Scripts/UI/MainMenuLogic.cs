using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuLogic : MonoBehaviour
{
    public void PlayGame()
    {
        // SceneManager.LoadScene("PlayerSelectMenu");
        SaveSystem.ClearSave(); // Remove old checkpoint
        PlayerPrefs.DeleteAll();
        SceneFader.instance.FadeToScene("PlayerSelectMenu");
    }

    public void ContinueGame()
    {
        if (!SaveSystem.HasSave())
        {
            Debug.Log("No save file found!");
            return;
        }

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
