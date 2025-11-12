using UnityEngine;
using System.Collections.Generic; // Needed for lists

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager instance;

    public Transform currentCheckpoint { get; private set; }

    // --- NEW ---
    // Static lists to track all players and objects
    private static List<Movement> allPlayers = new List<Movement>();
    private static List<ResettableObject> allResettables = new List<ResettableObject>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- NEW ---
    // Methods for players to register themselves
    public static void RegisterPlayer(Movement player)
    {
        if (!allPlayers.Contains(player))
        {
            allPlayers.Add(player);
        }
    }
    public static void UnregisterPlayer(Movement player)
    {
        if (allPlayers.Contains(player))
        {
            allPlayers.Remove(player);
        }
    }

    // --- NEW ---
    // Methods for objects to register themselves
    public static void RegisterResettable(ResettableObject obj)
    {
        if (!allResettables.Contains(obj))
        {
            allResettables.Add(obj);
        }
    }
    public static void UnregisterResettable(ResettableObject obj)
    {
        if (allResettables.Contains(obj))
        {
            allResettables.Remove(obj);
        }
    }

    public void SetCurrentCheckpoint(Transform newCheckpoint)
    {
        currentCheckpoint = newCheckpoint;
    }

    // --- NEW ---
    // This is the master function that does everything!
    public void TriggerFullRespawn()
    {
        // 1. Reset all objects first
        foreach (ResettableObject obj in allResettables)
        {
            obj.ResetObject();
        }

        // 2. Respawn all players
        foreach (Movement player in allPlayers)
        {
            // We will create this 'DieAndRespawn' method in Movement.cs
            player.DieAndRespawn();
        }
    }
}