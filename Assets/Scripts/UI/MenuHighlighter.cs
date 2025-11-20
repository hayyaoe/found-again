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

    [Header("Save Data Logic")]
    [Tooltip("The button to disable/hide if no save is found.")]
    [SerializeField] private Button continueButton;
    [Tooltip("The button to select by default if Continue is disabled.")]
    [SerializeField] private GameObject newGameButton;
    [Tooltip("The Quit button to move up if Continue is disabled.")]
    [SerializeField] private GameObject quitButton;

    [Header("Layout Adjustment")]
    [Tooltip("How many units to move the buttons up when Continue is hidden.")]
    [SerializeField] private float buttonMoveUpAmount = 10f;

    [Header("Mode")]
    [SerializeField] private bool onlyShowWhenPaused = false;

    [Header("Optional Polish")]
    [SerializeField] private float moveSpeed = 15f;

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

        // --- NEW LOGIC START ---
        // Check if we have a save file
        if (SaveSystem.HasSave())
        {
            // Save exists: Ensure Continue is visible and interactable
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
                continueButton.interactable = true;
            }
            // We do NOT move buttons here, assuming the scene is set up
            // with the buttons in their "Correct" positions for having a Continue button.
        }
        else
        {
            // No save: Hide the Continue button
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(false);
            }

            // Move the other buttons up to fill the empty space
            ShiftButtonUp(newGameButton);
            ShiftButtonUp(quitButton);

            // If the "Continue" button was set as the starting button, switch to "New Game"
            if (continueButton != null && firstSelectedButton == continueButton.gameObject)
            {
                if (newGameButton != null)
                {
                    firstSelectedButton = newGameButton;
                }
            }
        }
        // --- NEW LOGIC END ---
        
        if (firstSelectedButton != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelectedButton);
        }
    }

    private void ShiftButtonUp(GameObject buttonObj)
    {
        if (buttonObj != null)
        {
            RectTransform rect = buttonObj.GetComponent<RectTransform>();
            if (rect != null)
            {
                Vector2 newPos = rect.anchoredPosition;
                newPos.y += buttonMoveUpAmount; // Add to Y to move UP
                rect.anchoredPosition = newPos;
            }
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

        // 1. Get the target component from the selected button
        HighlighterTarget target = currentSelected.GetComponent<HighlighterTarget>();

        // 2. If it has one, use its manual size
        if (target != null)
        {
            MoveAndResize(currentSelected.transform, target.highlightSize);
        }
        else
        {
            // 3. If not, fall back to the rect transform size
            MoveAndResize(currentSelected.transform, currentSelected.GetComponent<RectTransform>().rect.size);
        }
    }

    private void MoveAndResize(Transform targetTransform, Vector2 targetSize)
    {
        if (targetTransform == null) return;

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