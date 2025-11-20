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

        // ----------------------------------------------------------
        // 🔥 Load checkpoint data BEFORE spawning any players
        // ----------------------------------------------------------
        bool hasSavedPos = SaveSystem.HasSavedPosition();
        Vector3 savedPos = SaveSystem.LoadCheckpointPosition();

        Transform savedCheckpointTransform = CheckpointLocator.GetSavedCheckpoint();

        // ----------------------------------------------------------

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

            // Devices
            InputDevice[] devices = selection.deviceIds?
                .Select(id => InputSystem.devices.FirstOrDefault(d => d.deviceId == id))
                .Where(d => d != null)
                .ToArray() ?? new InputDevice[0];

            // SPAWN PLAYER
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

            // Register player to other systems
            FindObjectOfType<PauseMenu>()?.RegisterNewPlayer(newPlayerInput);
            FindObjectOfType<DialogueManager>()?.RegisterNewPlayer(newPlayerInput);

            // ----------------------------------------------------------
            // 🔥 Apply correct spawning logic
            // ----------------------------------------------------------

            if (hasSavedPos)
            {
                // Highest priority → saved position
                newPlayerInput.transform.position = savedPos;
            }
            else if (savedCheckpointTransform != null)
            {
                // Second priority → checkpoint transform
                newPlayerInput.transform.position = savedCheckpointTransform.position;
                newPlayerInput.transform.rotation = savedCheckpointTransform.rotation;
            }
            else
            {
                // Fallback → default spawn points
                int spawnIdx = selection.playerIndex;
                if (spawnIdx < spawnPoints.Length && spawnPoints[spawnIdx] != null)
                {
                    newPlayerInput.transform.position = spawnPoints[spawnIdx].position;
                    newPlayerInput.transform.rotation = spawnPoints[spawnIdx].rotation;
                }
                else
                {
                    Debug.LogWarning("Fallback spawn: no spawn point assigned.");
                }
            }

            spawnedPlayers.Add(newPlayerInput.transform);
            Debug.Log($"Spawned {selection.characterName} for {selection.playerName}.");
        }

        // ----------------------------------------------------------
        // CAMERA FOLLOW SETUP
        // ----------------------------------------------------------
        if (cameraMovement != null && spawnedPlayers.Count > 0)
        {
            cameraMovement.SetTargets(spawnedPlayers.ToArray());

            if (SaveSystem.HasSave())
                cameraMovement.SnapToTargets();

            Debug.Log("Camera now following players.");
        }
    }
}
