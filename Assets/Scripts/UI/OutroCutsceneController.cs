using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class OutroCutsceneController : MonoBehaviour
{
    [Header("UI References")]
    public Image whiteFade;
    public Image logoImage;

    [Header("Timing")]
    public float fadeToWhiteDuration = 1.5f;
    public float logoFadeInDuration = 1.5f;
    public float logoHoldDuration = 2f;
    public string nextSceneName = "MainMenu";

    public bool hasPlayed = false;  // Biar nggak kepanggil dua kali

    private void Awake()
    {
        SetAlpha(whiteFade, 0f);
        SetAlpha(logoImage, 0f);
    }

    public void PlayOutro()
    {
        if (!hasPlayed)
        {
            hasPlayed = true;
            StartCoroutine(OutroSequence());
        }
    }

    private IEnumerator OutroSequence()
    {
        // 1) Fade to white
        yield return Fade(whiteFade, 0f, 1f, fadeToWhiteDuration);

        // 2) Fade in logo
        yield return Fade(logoImage, 0f, 1f, logoFadeInDuration);

        // 3) Hold logo
        yield return new WaitForSeconds(logoHoldDuration);

        // 4) Ganti ke main menu / credit / scene lain
        SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator Fade(Image img, float from, float to, float time)
    {
        float t = 0;
        Color c = img.color;

        while (t < time)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / time);
            c.a = Mathf.Lerp(from, to, lerp);
            img.color = c;
            yield return null;
        }
    }

    private void SetAlpha(Image img, float alpha)
    {
        var c = img.color;
        c.a = alpha;
        img.color = c;
    }
}
