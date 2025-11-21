using UnityEngine;
using System.Collections;

public class CheckpointUITrigger : MonoBehaviour
{
    [Header("Saving UI (Canvas Group)")]
    public CanvasGroup savingCanvas;    
    public float fadeDuration = 0.5f;
    public float displayTime = 5f;

    private Coroutine routine;

    public void ShowSavingUI()
    {
        if (savingCanvas == null)
        {
            Debug.LogWarning("Saving CanvasGroup not assigned!");
            return;
        }

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(SavingRoutine());
    }

    private IEnumerator SavingRoutine()
    {
        yield return FadeCanvas(1f);
        yield return new WaitForSeconds(displayTime);
        yield return FadeCanvas(0f);
    }

    private IEnumerator FadeCanvas(float targetAlpha)
    {
        float start = savingCanvas.alpha;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            savingCanvas.alpha = Mathf.Lerp(start, targetAlpha, t / fadeDuration);
            yield return null;
        }

        savingCanvas.alpha = targetAlpha;
    }
}
