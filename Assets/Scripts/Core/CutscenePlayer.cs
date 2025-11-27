using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class CutscenePlayer : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public string nextScene = "Area 1";

    [Header("Skip Settings")]
    [SerializeField] private AudioClip skipSFX;
    [SerializeField] private float skipVolume = 0.3f;

    private bool isSkipping = false;

    void Start()
    {
        videoPlayer.loopPointReached += OnFinished;
    }

    void OnFinished(VideoPlayer vp)
    {
        if (isSkipping) return; // prevent double triggers
        isSkipping = true;

        SceneFader.instance.FadeToScene(nextScene);
    }

    void Update()
    {
        if (!isSkipping && Input.anyKeyDown)
        {
            isSkipping = true;

            // 🎵 Play skip SFX
            if (SoundFXManager.instance != null && skipSFX != null)
                SoundFXManager.instance.PlaySoundFXClip(skipSFX, transform, skipVolume);

            // Fade out to next scene
            SceneFader.instance.FadeToScene(nextScene);
        }
    }
}
