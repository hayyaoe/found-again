using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerJoinHandler : MonoBehaviour
{
    public Canvas uiCanvas; // assign in inspector (Canvas object)

    // Called automatically by PlayerInputManager when a player joins
    void OnPlayerJoined(PlayerInput playerInput)
    {
        // parent the spawned player GameObject under the Canvas so RectTransform movement works
        if (uiCanvas != null)
        {
            playerInput.transform.SetParent(uiCanvas.transform, false);
        }

        // set camera for PlayerInput if not set
        if (playerInput.camera == null)
            playerInput.camera = Camera.main;

        // optional: set display name
        var cursor = playerInput.GetComponent<PlayerCursorController>();
        if (cursor != null)
        {
            cursor.playerName = "P" + (playerInput.playerIndex + 1);
        }

        Debug.Log($"Player joined: index {playerInput.playerIndex} parented to Canvas");
    }
}
