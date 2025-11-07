using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class ProloguePlayerSpawner : MonoBehaviour
{
    public GameObject mimiPrefab;  // assign in inspector
    public GameObject mariePrefab; // assign in inspector

    // optional spawn positions per playerIndex, fallback to origin
    public Transform[] spawnPoints;
    
    // --- REMOVED THE HUD CANVAS ---
    // The DialogueManager will handle enabling this now.

    // --- Start() has been REPLACED ---
    // This function will now be called by DialogueManager when the cutscene ends.
    public void StartSpawning()
    {
        Debug.Log("Dialogue finished, spawning players...");
        
        var sm = PlayerSelectionManager.Instance;
        if (sm == null)
        {
            Debug.LogError("PlayerSelectionManager not found. Make sure it exists in the Lobby and uses DontDestroyOnLoad.");
            return;
        }

        foreach (var selection in sm.selectedPlayers)
        {
            GameObject prefabToSpawn = null;
            switch (selection.characterName)
            {
                case "Mimi": prefabToSpawn = mimiPrefab; break;
                case "Marie": prefabToSpawn = mariePrefab; break;
                default:
                    Debug.LogWarning($"Unknown character: {selection.characterName}. Skipping spawn.");
                    continue;
            }

            InputDevice[] devices = new InputDevice[0];
            if (selection.deviceIds != null && selection.deviceIds.Length > 0)
            {
                devices = selection.deviceIds
                    .Select(id => InputSystem.devices.FirstOrDefault(d => d.deviceId == id))
                    .Where(d => d != null)
                    .ToArray();
            }

            PlayerInput newPlayerInput = PlayerInput.Instantiate(prefabToSpawn, selection.playerIndex, null, -1, devices);

            if (newPlayerInput == null)
            {
                Debug.LogError($"Failed to instantiate player prefab for {selection.playerName}");
                continue;
            }

            int spawnIdx = selection.playerIndex;
            if (spawnPoints != null && spawnIdx >= 0 && spawnIdx < spawnPoints.Length && spawnPoints[spawnIdx] != null)
            {
                newPlayerInput.transform.position = spawnPoints[spawnIdx].position;
                newPlayerInput.transform.rotation = spawnPoints[spawnIdx].rotation;
            }

            Debug.Log($"Spawned {selection.characterName} for {selection.playerName} with {devices.Length} device(s).");
        }
    }
    
}