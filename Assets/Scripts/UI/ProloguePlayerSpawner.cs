using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class ProloguePlayerSpawner : MonoBehaviour
{
    public GameObject mimiPrefab;
    public GameObject mariePrefab;
    public Transform[] spawnPoints;

    private CameraMovement cameraMovement;

    private void Awake()
    {
        cameraMovement = FindObjectOfType<CameraMovement>();
    }

    public void StartSpawning()
    {
        Debug.Log("Dialogue finished, spawning players...");

        var sm = PlayerSelectionManager.Instance;
        if (sm == null)
        {
            Debug.LogError("PlayerSelectionManager not found.");
            return;
        }

        var spawnedPlayers = new System.Collections.Generic.List<Transform>();

        // 🔥 Get saved checkpoint (if any)
        Transform savedCheckpoint = CheckpointLocator.GetSavedCheckpoint();

        foreach (var selection in sm.selectedPlayers)
        {
            GameObject prefabToSpawn = selection.characterName switch
            {
                "Mimi" => mimiPrefab,
                "Marie" => mariePrefab,
                _ => null
            };

            if (prefabToSpawn == null)
            {
                Debug.LogWarning($"Unknown character: {selection.characterName}. Skipping spawn.");
                continue;
            }

            InputDevice[] devices = selection.deviceIds?
                .Select(id => InputSystem.devices.FirstOrDefault(d => d.deviceId == id))
                .Where(d => d != null)
                .ToArray() ?? new InputDevice[0];

            PlayerInput newPlayerInput = PlayerInput.Instantiate(
                prefabToSpawn, 
                selection.playerIndex, 
                null, 
                -1, 
                devices
            );

            if (newPlayerInput == null)
            {
                Debug.LogError($"Failed to instantiate player prefab for {selection.playerName}");
                continue;
            }

            // 🟢 If we have a saved checkpoint, spawn there
            if (savedCheckpoint != null)
            {
                newPlayerInput.transform.position = savedCheckpoint.position;
                newPlayerInput.transform.rotation = savedCheckpoint.rotation;
            }
            else
            {
                // 🟡 No checkpoint → use regular spawn points
                int spawnIdx = selection.playerIndex;
                if (spawnPoints != null && spawnIdx < spawnPoints.Length && spawnPoints[spawnIdx] != null)
                {
                    newPlayerInput.transform.position = spawnPoints[spawnIdx].position;
                    newPlayerInput.transform.rotation = spawnPoints[spawnIdx].rotation;
                }
            }

            spawnedPlayers.Add(newPlayerInput.transform);
            Debug.Log($"Spawned {selection.characterName} for {selection.playerName}.");
        }

        // Update camera
        if (cameraMovement != null && spawnedPlayers.Count > 0)
        {
            cameraMovement.SetTargets(spawnedPlayers.ToArray());

            // 🔥 If loading from checkpoint, instantly move camera to players
            if (SaveSystem.HasSave())
                cameraMovement.SnapToTargets();

            Debug.Log("Camera now following both players.");
        }
    }
}
