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
        if (whiteFade != null) SetAlpha(whiteFade, 0f);
        if (logoImage != null) SetAlpha(logoImage, 0f);
    }

    public void PlayOutro()
    {
        if (!hasPlayed)
        {
            hasPlayed = true;

            BoatMove.OutroRunning = true;

            StartCoroutine(OutroSequence());
        }
    }

    private IEnumerator OutroSequence()
    {
        // 1) Fade to white
        if (whiteFade != null)
            yield return Fade(whiteFade, 0f, 1f, fadeToWhiteDuration);

        // ✅ Freeze AFTER screen is fully white
        Time.timeScale = 0f;

        // 2) Fade in logo
        if (logoImage != null)
            yield return Fade(logoImage, 0f, 1f, logoFadeInDuration);

        // 3) Hold logo
        yield return new WaitForSecondsRealtime(logoHoldDuration);

        // 🟢 NEW: Clear Save Data so "Continue" disappears
        Debug.Log("Outro finished. Clearing save data.");
        
        // Remove checkpoint file
        SaveSystem.ClearSave(); 

        // Reset "IsNewGame" pref so Main Menu logic knows we are fresh
        PlayerPrefs.SetInt("IsNewGame", 1); 
        PlayerPrefs.Save();

        // 4) Ganti ke main menu / credit / scene lain
        // Use Fader if available, otherwise direct load
        if (SceneFader.instance != null)
        {
            SceneFader.instance.FadeToScene(nextSceneName);
        }
        else
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private IEnumerator Fade(Image img, float from, float to, float time)
    {
        float t = 0;
        Color c = img.color;

        while (t < time)
        {
            // t += Time.deltaTime;
            t += Time.unscaledDeltaTime;
            float lerp = Mathf.Clamp01(t / time);
            c.a = Mathf.Lerp(from, to, lerp);
            img.color = c;
            yield return null;
        }
        // Ensure final value
        c.a = to;
        img.color = c;
    }

    private void SetAlpha(Image img, float alpha)
    {
        var c = img.color;
        c.a = alpha;
        img.color = c;
    }
}