using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuLogic : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene("PlayerSelectMenu");
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
