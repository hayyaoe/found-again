using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PlayerCursorController : MonoBehaviour
{
    public string playerName;
    public bool hasConfirmed = false;

    private RectTransform cursorTransform;
    private Vector2 moveInput;
    private Camera mainCam;

    // 👉 Add this:
    private CharacterSelectButton currentCharacter;

    void Awake()
    {
        cursorTransform = GetComponent<RectTransform>();
        mainCam = Camera.main;
    }

    void Start()
    {
        if (mainCam == null)
            mainCam = Camera.main;

        var playerInput = GetComponent<PlayerInput>();
        if (playerInput.camera == null)
            playerInput.camera = mainCam;

        // ✅ Ensure the cursor is inside the main UI Canvas
        AttachToCanvas();
    }

    private void AttachToCanvas()
    {
        Canvas canvas = FindObjectOfType<Canvas>();

        if (canvas != null)
        {
            transform.SetParent(canvas.transform, false);

            // Reset position if needed
            RectTransform rect = GetComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero; 
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;

            // Optional: spawn at a random starting position so cursors don’t overlap
            rect.anchoredPosition = new Vector2(Random.Range(-300, 300), Random.Range(-200, 200));

            Debug.Log($"{playerName} cursor attached to canvas.");
        }
        else
        {
            Debug.LogWarning("⚠️ No Canvas found for cursor!");
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    void Update()
    {
        // Always allow movement
        cursorTransform.anchoredPosition += moveInput * 400 * Time.deltaTime;
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        if (context.performed && !hasConfirmed)
        {
            TrySelectCharacter();
        }
    }

    public void OnCancel(InputAction.CallbackContext context)
    {
        if (context.performed && hasConfirmed)
        {
            CancelSelection();
        }
    }

    void TrySelectCharacter()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = mainCam.WorldToScreenPoint(transform.position)
        };

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            CharacterSelectButton character = result.gameObject.GetComponent<CharacterSelectButton>();
            if (character != null)
            {
                character.SelectCharacter(this);
                currentCharacter = character; // ✅ remember which button was selected
                hasConfirmed = true;
                break;
            }
        }
    }

    void CancelSelection()
    {
        if (currentCharacter != null)
        {
            currentCharacter.UnselectCharacter(); // ✅ notify button to reset visuals
            currentCharacter = null;
        }

        hasConfirmed = false;
        Debug.Log($"{playerName} cancelled selection.");
    }
}
