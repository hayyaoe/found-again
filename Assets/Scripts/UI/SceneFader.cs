using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneFader : MonoBehaviour
{
    public static SceneFader instance;

    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1f;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject); 
            return;
        }

        // Make sure the image is black to start
        fadeImage.color = new Color(0f, 0f, 0f, 1f);
        // And block raycasts
        fadeImage.raycastTarget = true;
        
        StartCoroutine(FadeIn());
    }

    public void FadeToScene(string sceneName)
    {
        StartCoroutine(FadeOutAndLoad(sceneName));
    }

    private IEnumerator FadeIn()
    {
        // Fade from black (alpha 1) to clear (alpha 0)
        float timer = 0f;
        while (timer < fadeDuration)
        {
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            fadeImage.color = new Color(0f, 0f, 0f, alpha);
            timer += Time.unscaledDeltaTime; 
            yield return null;
        }
        fadeImage.color = new Color(0f, 0f, 0f, 0f); // Ensure it's clear
        fadeImage.raycastTarget = false; // Allow clicks to go through
    }

    private IEnumerator FadeOutAndLoad(string sceneName)
    {
        fadeImage.raycastTarget = true; // Block clicks
        
        // --- THIS LINE WAS THE BUG ---
        // Fade from clear (alpha 0) to black (alpha 1)
        float timer = 0f;
        while (timer < fadeDuration)
        {
            // It was Lerp(0f, 0f, ...), now it's Lerp(0f, 1f, ...)
            float alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration); // <-- FIXED
            fadeImage.color = new Color(0f, 0f, 0f, alpha);
            timer += Time.unscaledDeltaTime; 
            yield return null;
        }
        fadeImage.color = new Color(0f, 0f, 0f, 1f); // Ensure it's black

        // Load the new scene
        SceneManager.LoadScene(sceneName);
    }
}