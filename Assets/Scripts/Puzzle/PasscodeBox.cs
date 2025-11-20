using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PasscodeBox : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private TextMeshProUGUI displayText;
    [SerializeField] private int passcodeValue = 0;
    [SerializeField] private int minValue = 0;
    [SerializeField] private int maxValue = 9;

    [Header("UI References")]
    [SerializeField] private GameObject leftArrow;
    [SerializeField] private GameObject rightArrow;

    [Header("Correct State")]
    [SerializeField] private SpriteRenderer bottomSpriteRenderer;
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite lightSprite;


    private bool isActive = false;
    private PlayerInput currentPlayerInput;
    private InputAction changeNumberAction;
    private InputAction exitAction;
    private PasscodeManager manager;

    private void Start()
    {
        UpdateDisplay();
        SetArrowVisibility(false);
    }

    public void AssignManager(PasscodeManager passcodeManager)
    {
        manager = passcodeManager;
    }

    public void BeginInteraction(PlayerInput playerInput)
    {
        if (isActive) return;

        if(manager != null && manager.IsSolved)
        {
            Debug.Log("Puzzle already solved - cannot interact anymore.");
            return;
        }

        isActive = true;
        currentPlayerInput = playerInput;

        playerInput.SwitchCurrentActionMap("Passcode");

        changeNumberAction = playerInput.currentActionMap.FindAction("ChangeNumber");
        exitAction = playerInput.currentActionMap.FindAction("Exit");

        changeNumberAction.performed += OnChangeNumber;
        exitAction.performed += OnExit;

        SetArrowVisibility(true);
    }

    private void EndInteraction()
    {
        isActive = false;

        if (changeNumberAction != null)
            changeNumberAction.performed -= OnChangeNumber;
        if (exitAction != null)
            exitAction.performed -= OnExit;

        SetArrowVisibility(false);

        if (currentPlayerInput != null)
        {
            currentPlayerInput.SwitchCurrentActionMap("Player");
            currentPlayerInput = null;
        }
    }

    public void OnChangeNumber(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();

        if (input.y > 0.5f)
            passcodeValue = Mathf.Min(passcodeValue + 1, maxValue);
        else if (input.y < -0.5f)
            passcodeValue = Mathf.Max(passcodeValue - 1, minValue);

        UpdateDisplay();

        // Notify manager each time the number changes
        manager?.CheckPasscode();
    }

    public void OnExit(InputAction.CallbackContext context)
    {
        EndInteraction();
    }

    private void UpdateDisplay()
    {
        displayText.text = passcodeValue.ToString("0");
    }

    private void SetArrowVisibility(bool visible)
    {
        if (leftArrow != null) leftArrow.SetActive(visible);
        if (rightArrow != null) rightArrow.SetActive(visible);
    }

    public int GetValue() => passcodeValue;

    public void SetLightState(bool isCorrect)
    {
        if (bottomSpriteRenderer != null)
            bottomSpriteRenderer.sprite = isCorrect ? lightSprite : defaultSprite;
    }

    public void ForceEndInteraction()
    {
        if(isActive)
            EndInteraction();
    }
}
