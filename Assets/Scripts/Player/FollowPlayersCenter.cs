using UnityEngine;

public class FollowPlayersCenter : MonoBehaviour
{
    public Vector3 offset;

    private Transform player1;
    private Transform player2;

    void LateUpdate()
    {
        // If players not assigned, find them in the scene
        if (player1 == null || player2 == null)
        {
            var players = GameObject.FindGameObjectsWithTag("Player");

            if (players.Length >= 2)
            {
                player1 = players[0].transform;
                player2 = players[1].transform;
            }
            else
            {
                return; // still waiting for 2 players to spawn
            }
        }

        // Follow midpoint between players
        Vector3 center = (player1.position + player2.position) * 0.5f;
        transform.position = center + offset;
    }
}
