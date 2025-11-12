using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    private PlayerInput playerInput;
    private PasscodeBox nearbyBox;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out PasscodeBox box))
            nearbyBox = box;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent(out PasscodeBox box) && nearbyBox == box)
            nearbyBox = null;
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed && nearbyBox != null)
        {
            // Switch to passcode mode
            nearbyBox.BeginInteraction(playerInput);
        }
    }
}
