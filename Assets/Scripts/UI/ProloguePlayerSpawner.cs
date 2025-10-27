// ProloguePlayerSpawner.cs
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class ProloguePlayerSpawner : MonoBehaviour
{
    public GameObject mimiPrefab;  // assign in inspector
    public GameObject mariePrefab; // assign in inspector

    // optional spawn positions per playerIndex, fallback to origin
    public Transform[] spawnPoints;

    void Start()
    {
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

            // Resolve InputDevice[] from stored device ids
            InputDevice[] devices = new InputDevice[0];
            if (selection.deviceIds != null && selection.deviceIds.Length > 0)
            {
                devices = selection.deviceIds
                    .Select(id => InputSystem.devices.FirstOrDefault(d => d.deviceId == id))
                    .Where(d => d != null)
                    .ToArray();
            }

            // Instantiate PlayerInput with the prefab and pair the devices.
            // Parameters: prefab, playerIndex, controlScheme (null = auto), splitScreenIndex (-1 = none), pairWithDevices...
            PlayerInput newPlayerInput = PlayerInput.Instantiate(prefabToSpawn, selection.playerIndex, null, -1, devices);

            if (newPlayerInput == null)
            {
                Debug.LogError($"Failed to instantiate player prefab for {selection.playerName}");
                continue;
            }

            // Move to spawn position if available
            int spawnIdx = selection.playerIndex;
            if (spawnPoints != null && spawnIdx >= 0 && spawnIdx < spawnPoints.Length && spawnPoints[spawnIdx] != null)
            {
                newPlayerInput.transform.position = spawnPoints[spawnIdx].position;
                newPlayerInput.transform.rotation = spawnPoints[spawnIdx].rotation;
            }

            Debug.Log($"Spawned {selection.characterName} for {selection.playerName} with {devices.Length} device(s).");
        }

        // optional cleanup
        // sm.ClearSelections();
    }
}
