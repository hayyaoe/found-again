using UnityEngine;
using System.Collections;

public class CheckpointUITrigger : MonoBehaviour
{
    [Header("Saving UI (Canvas Group)")]
    public CanvasGroup savingCanvas;   // assign your Saving canvas group here
    public float fadeDuration = 0.5f;
    public float displayTime = 5f;

    private bool isShowing = false;
    private Coroutine routine;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Set the checkpoint in CheckpointManager
        if (CheckpointManager.instance != null)
            CheckpointManager.instance.SetCurrentCheckpoint(transform);

        // Show saving UI
        ShowSavingUI();
    }

    private void ShowSavingUI()
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
        // Fade in
        yield return FadeCanvas(1f);

        // Hold for 5 seconds
        yield return new WaitForSeconds(displayTime);

        // Fade out
        yield return FadeCanvas(0f);
    }

    private IEnumerator FadeCanvas(float targetAlpha)
    {
        float start = savingCanvas.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            savingCanvas.alpha = Mathf.Lerp(start, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        savingCanvas.alpha = targetAlpha;
    }
}
