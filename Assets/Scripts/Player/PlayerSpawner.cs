using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpawner : MonoBehaviour
{
    public GameObject player1Prefab;
    public GameObject player2Prefab;
    [SerializeField] private Transform[] spawnPoints;

    private int playerCount = 0;

    public void OnPlayerJoined(PlayerInput playerInput)
    {
        playerCount++;

        // Assign a different prefab/sprite based on join order
        if (playerCount == 1)
        {
            ReplacePlayerPrefab(playerInput, player1Prefab);
        }
        else if (playerCount == 2)
        {
            ReplacePlayerPrefab(playerInput, player2Prefab);
        }
    }

    private void ReplacePlayerPrefab(PlayerInput playerInput, GameObject newPrefab)
    {
        int index = playerCount - 1;
        Vector3 spawnPos = spawnPoints.Length > index ? spawnPoints[index].position : Vector3.zero;

        var newPlayer = Instantiate(newPrefab, spawnPos, Quaternion.identity);

        playerInput.transform.SetParent(newPlayer.transform, false);
    }
}
