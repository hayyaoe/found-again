using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class CutscenePlayer : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public string nextScene = "Area 1";   // default after cutscene
    [Header("SFX Settings")]
    [SerializeField] private AudioClip skipSFX;
    [SerializeField] private float sfxVolume = 1f;

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
            if (skipSFX != null)
                SoundFXManager.instance.PlaySoundFXClip(skipSFX, transform, sfxVolume);
            SceneFader.instance.FadeToScene(nextScene);
        }
    }
}
