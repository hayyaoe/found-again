using UnityEngine;
using TMPro;

public class CharacterSelectButton : MonoBehaviour
{
    public string characterName;
    public bool isTaken = false;
    public string takenByPlayer;
    public SpriteRenderer characterImage;
    public TMP_Text playerNameText;

    private PlayerCursorController selectedBy;

    public void SelectCharacter(PlayerCursorController player)
    {
        if (isTaken) return;

        isTaken = true;
        takenByPlayer = player.playerName;
        player.hasConfirmed = true;

        // Grey out the character
        characterImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        playerNameText.text = player.playerName;

        // ✅ Save selection persistently
        PlayerSelectionManager.Instance.RegisterPlayer(player, characterName);

        // Notify the lobby manager
        FindObjectOfType<LobbyManager>().PlayerConfirmed();
    }


    // ✅ Called when a player cancels their selection
    public void UnselectCharacter()
    {
        if (!isTaken) return;

        Debug.Log($"{takenByPlayer} unselected {characterName}");

        isTaken = false;
        takenByPlayer = null;
        selectedBy = null;

        // Restore visuals
        characterImage.color = Color.white;
        playerNameText.text = characterName;

        FindObjectOfType<LobbyManager>().PlayerCancelled();
    }

    // (Optional helper)
    public bool IsSelectedBy(PlayerCursorController player)
    {
        return selectedBy == player;
    }
}
