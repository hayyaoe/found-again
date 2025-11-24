using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class CutscenePlayer : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public string nextScene = "Area 1";   // default after cutscene

    void Start()
    {
        videoPlayer.loopPointReached += OnFinished;
    }

    void OnFinished(VideoPlayer vp)
    {
        SceneFader.instance.FadeToScene(nextScene);
    }

    void Update()
    {
        if (Input.anyKeyDown)
        {
            SceneFader.instance.FadeToScene(nextScene);
        }
    }
}
