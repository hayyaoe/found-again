using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MenuHighlighter : MonoBehaviour
{
    [Header("Highlighter")]
    [SerializeField] private Image highlighterImage;

    [Header("Navigation")]
    [SerializeField] private GameObject firstSelectedButton;

    [Header("Mode")]
    [SerializeField] private bool onlyShowWhenPaused = false;

    [Header("Optional Polish")]
    [SerializeField] private float moveSpeed = 15f;
    // --- PADDING IS NO LONGER NEEDED ---
    // [SerializeField] private float padding = 20f;

    private GameObject lastSelectedObject;
    private RectTransform highlighterRect;

    void Start()
    {
        if (highlighterImage == null)
        {
            Debug.LogError("Highlighter Image is not assigned!", this);
            this.enabled = false;
            return;
        }

        highlighterRect = highlighterImage.GetComponent<RectTransform>();
        highlighterImage.gameObject.SetActive(false);
        highlighterImage.raycastTarget = false;
        
        if (firstSelectedButton != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelectedButton);
        }
    }

    void Update()
    {
        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        if (onlyShowWhenPaused && !PauseMenu.GameIsPaused)
        {
            highlighterImage.gameObject.SetActive(false);
            lastSelectedObject = null;
            return;
        }

        if (currentSelected == null)
        {
            highlighterImage.gameObject.SetActive(false);
            lastSelectedObject = null;
            return;
        }

        if (currentSelected != lastSelectedObject)
        {
            highlighterImage.gameObject.SetActive(true);
            lastSelectedObject = currentSelected;
        }

        // --- THIS IS THE NEW LOGIC ---
        // 1. Get the target component from the selected button
        HighlighterTarget target = currentSelected.GetComponent<HighlighterTarget>();

        // 2. If it has one, use its manual size
        if (target != null)
        {
            MoveAndResize(currentSelected.transform, target.highlightSize);
        }
        else
        {
            // 3. If not, fall back to the old (big) text size
            Debug.LogWarning($"Button '{currentSelected.name}' is missing a HighlighterTarget component.", currentSelected);
            MoveAndResize(currentSelected.transform, currentSelected.GetComponent<RectTransform>().rect.size);
        }
        // --- END OF NEW LOGIC ---
    }

    // --- THIS FUNCTION IS MODIFIED ---
    private void MoveAndResize(Transform targetTransform, Vector2 targetSize)
    {
        if (targetTransform == null) return;

        // The logic is now simpler: just use the values we are given
        if (moveSpeed > 0)
        {
            highlighterRect.position = Vector3.Lerp(highlighterRect.position, targetTransform.position, Time.unscaledDeltaTime * moveSpeed);
            highlighterRect.sizeDelta = Vector2.Lerp(highlighterRect.sizeDelta, targetSize, Time.unscaledDeltaTime * moveSpeed);
        }
        else
        {
            highlighterRect.position = targetTransform.position;
            highlighterRect.sizeDelta = targetSize;
        }
    }
}