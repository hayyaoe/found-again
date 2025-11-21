using UnityEngine;
using System.Collections;

public class MiniFragmentTrigger : MonoBehaviour
{
    [Header("Fragment UI (Canvas Group)")]
    public CanvasGroup fragmentCanvas; 
    public float fadeDuration = 0.5f;
    public float displayTime = 5f;

    private Coroutine routine;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        ShowFragmentObtainedUI();
    }

    private void ShowFragmentObtainedUI()
    {
        if (fragmentCanvas == null)
        {
            Debug.LogWarning("Fragment CanvasGroup not assigned!");
            return;
        }

        // Stop running coroutine if any — this MUST stop from the CoroutineRunner, not here
        if (routine != null)
        {
            CoroutineRunner.Instance.StopCoroutine(routine);
        }

        // Start coroutine safely from always-active object
        routine = CoroutineRunner.Instance.StartCoroutine(SavingRoutine());
    }

    private IEnumerator SavingRoutine()
    {
        yield return FadeCanvas(1f);             // Fade-in
        yield return new WaitForSeconds(displayTime);
        yield return FadeCanvas(0f);             // Fade-out
    }

    private IEnumerator FadeCanvas(float targetAlpha)
    {
        float start = fragmentCanvas.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fragmentCanvas.alpha = Mathf.Lerp(start, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        fragmentCanvas.alpha = targetAlpha;
    }
}
