// PlayerSelectionManager.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class PlayerSelectionData
{
    public string playerName;
    public string characterName; // "Mimi" or "Marie"
    public int playerIndex;
    public int[] deviceIds; // store device ids used by this player
}

public class PlayerSelectionManager : MonoBehaviour
{
    public static PlayerSelectionManager Instance;

    public List<PlayerSelectionData> selectedPlayers = new List<PlayerSelectionData>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Use PlayerInput (not PlayerCursorController) to get device info
    public void RegisterPlayer(PlayerCursorController cursor, string characterName)
    {
        var playerInput = cursor.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogWarning("RegisterPlayer: PlayerInput component missing on cursor prefab.");
            return;
        }

        // collect device ids that are currently paired to that PlayerInput
        var deviceIds = playerInput.devices.Select(d => d.deviceId).ToArray();

        var data = new PlayerSelectionData
        {
            playerName = cursor.playerName,
            characterName = characterName,
            playerIndex = playerInput.playerIndex,
            deviceIds = deviceIds
        };

        // avoid duplicates (based on playerIndex or playerName)
        if (!selectedPlayers.Exists(p => p.playerIndex == data.playerIndex))
            selectedPlayers.Add(data);
    }

    public void ClearSelections()
    {
        selectedPlayers.Clear();
    }
}
