using UnityEngine;
using System.Collections;

public class IntroTextController : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public float fadeDuration = 1f;

    private bool hasFadedOut = false;

    void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.alpha = 1f; // start visible
    }

    public void FadeOut()
    {
        if (!hasFadedOut)
            StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        hasFadedOut = true;
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false); // hide after fade
    }
}
