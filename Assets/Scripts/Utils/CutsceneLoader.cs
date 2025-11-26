using UnityEngine;
using UnityEngine.SceneManagement;

public class CutsceneSceneLoader : MonoBehaviour
{
    public string sceneToLoad;

    public void LoadNextScene()
    {
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogWarning("Scene name is empty!");
            return;
        }

        if (SceneFader.instance != null) SceneFader.instance.FadeToScene(sceneToLoad);
        else SceneManager.LoadScene(sceneToLoad);
    }
}
