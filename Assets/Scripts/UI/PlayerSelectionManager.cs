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

        var deviceIds = playerInput.devices.Select(d => d.deviceId).ToArray();

        var data = new PlayerSelectionData
        {
            playerName = cursor.playerName,
            characterName = characterName,
            playerIndex = playerInput.playerIndex,
            deviceIds = deviceIds
        };

        // ✅ If player already exists, update their selection instead of skipping
        var existing = selectedPlayers.FirstOrDefault(p => p.playerIndex == data.playerIndex);
        if (existing != null)
        {
            existing.characterName = data.characterName;
            existing.deviceIds = data.deviceIds;
            existing.playerName = data.playerName;
        }
        else
        {
            selectedPlayers.Add(data);
        }

        Debug.Log($"✅ Registered/Updated {data.playerName} → {data.characterName}");
    }

    public void RegisterSelection(string playerName, string characterName)
    {
        // Find the PlayerCursorController that matches the player name
        var cursor = FindObjectsOfType<PlayerCursorController>()
            .FirstOrDefault(c => c.playerName == playerName);

        if (cursor == null)
        {
            Debug.LogWarning($"RegisterSelection: Could not find cursor for {playerName}");
            return;
        }

        RegisterPlayer(cursor, characterName);
    }


    public void ClearSelections()
    {
        selectedPlayers.Clear();
    }
}
