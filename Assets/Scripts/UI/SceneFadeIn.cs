using UnityEngine;
using UnityEngine.UI; // Needed for UI
using System.Collections;

public class SceneFadeIn : MonoBehaviour
{
    [Header("Transition")]
    [Tooltip("Drag your 'FadePanel' prefab here.")]
    public GameObject fadePanelPrefab;
    public float fadeDuration = 1.0f;

    private Image fadeImage;

    void Start()
    {
        // 1. Create the fade panel
        GameObject panelObject = Instantiate(fadePanelPrefab, transform);
        fadeImage = panelObject.GetComponent<Image>();
        
        // 2. Make sure it starts fully black (opaque)
        fadeImage.color = new Color(0, 0, 0, 1);
        
        // 3. Start the fade-in coroutine
        StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        // --- This is the Fade In animation ---
        float timer = 0f;
        while (timer < fadeDuration)
        {
            // Animate the alpha value from 1 to 0
            float alpha = Mathf.Lerp(1, 0, timer / fadeDuration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            
            timer += Time.deltaTime;
            yield return null; // Wait for the next frame
        }
        
        // Ensure it's fully transparent
        fadeImage.color = new Color(0, 0, 0, 0);
        
        // 4. Destroy the panel so it doesn't block UI
        Destroy(fadeImage.gameObject);
    }
}