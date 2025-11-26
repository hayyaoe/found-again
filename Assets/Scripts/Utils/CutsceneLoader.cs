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

        SceneManager.LoadScene(sceneToLoad);
    }
}
