using UnityEngine;
using TMPro;
using System.Collections;

public class FadeUIOnTrigger : MonoBehaviour
{
    [Header("UI Target")]
    public CanvasGroup canvasGroup; // Assign your Text's Canvas Group

    [Header("Fade Settings")]
    public float fadeDuration = 1f; // seconds

    private Coroutine fadeCoroutine;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            FadeTo(1f); // fade in
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            FadeTo(0f); // fade out
    }

    private void FadeTo(float targetAlpha)
    {
        if (CoroutineRunner.Instance == null)
            return; // Runner destroyed, avoid error

        if (fadeCoroutine != null)
        {
            // Make sure the runner still exists before stopping
            if (CoroutineRunner.Instance.gameObject != null)
                CoroutineRunner.Instance.StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = CoroutineRunner.Instance.Run(FadeRoutine(targetAlpha));
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        if (canvasGroup == null)
            yield break;

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            if (canvasGroup == null)   // safety check
                yield break;

            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);

            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = targetAlpha;
    }
}
