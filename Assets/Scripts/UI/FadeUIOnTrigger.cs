using UnityEngine;
using TMPro;

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
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    private System.Collections.IEnumerator FadeRoutine(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }
}
