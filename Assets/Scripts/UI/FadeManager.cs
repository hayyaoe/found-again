using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class FadeManager : MonoBehaviour
{
    public static FadeManager instance;

    [Header("Fade Settings")]
    [SerializeField] private GameObject fadePanelPrefab; // Your 'Fade.prefab'
    [SerializeField] private float fadeDuration = 1.0f;

    private Image fadeImage;
    private bool isFading = false;

    private void Awake()
    {
        // --- Create the Singleton ---
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // This is the key to making it persist
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // --- Create a Canvas ---
        GameObject canvasObj = new GameObject("FadeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        // --- THIS IS THE FIX ---
        // Set the sorting order to the absolute maximum to ensure
        // it is on top of ALL other UI, including your Main Menu.
        canvas.sortingOrder = 32767;
        // --- END OF FIX ---

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.transform.SetParent(this.transform);

        // --- Instantiate the Fade Panel ---
        if (fadePanelPrefab != null)
        {
            GameObject panelObject = Instantiate(fadePanelPrefab, canvas.transform);
            fadeImage = panelObject.GetComponent<Image>();
            
            Animator prefabAnimator = panelObject.GetComponent<Animator>();
            if (prefabAnimator != null)
            {
                prefabAnimator.enabled = false;
            }

            if (fadeImage != null)
            {
                fadeImage.enabled = true;
            }

            // Force the panel to stretch
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Start transparent
            fadeImage.color = new Color(0f, 0f, 0f, 0f);
        }
        else
        {
            Debug.LogError("Fade Panel Prefab is not assigned in FadeManager!");
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void FadeToScene(string sceneName)
    {
        if (!isFading)
        {
            StartCoroutine(FadeOutAndLoad(sceneName));
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(FadeIn());
    }

    
    private IEnumerator FadeOutAndLoad(string sceneName)
    {
        isFading = true;
        
        float timer = 0f;
        while (timer < fadeDuration)
        {
            float alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            fadeImage.color = new Color(0f, 0f, 0f, alpha);
            timer += Time.unscaledDeltaTime; 
            yield return null;
        }
        fadeImage.color = new Color(0f, 0f, 0f, 1f); 

        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeIn()
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            fadeImage.color = new Color(0f, 0f, 0f, alpha);
            timer += Time.unscaledDeltaTime;
            yield return null;
        }
        fadeImage.color = new Color(0f, 0f, 0f, 0f); 

        isFading = false;
    }
}