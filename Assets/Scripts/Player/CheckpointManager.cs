using UnityEngine;
using System.Collections.Generic; // Needed for lists
using System.Collections; // Needed for Coroutines

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager instance;

    public Transform currentCheckpoint { get; private set; }

    // --- NEW ---
    // Static lists to track all players and objects
    public static List<Movement> allPlayers = new List<Movement>();
    private static List<ResettableObject> allResettables = new List<ResettableObject>();

    // Prevents multiple respawn sequences from starting simultaneously
    private bool isRespawning = false;

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

    // --- Player Registration ---
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

    // --- Resettable Object Registration ---
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

    // --- NEW: Coordinated Death & Respawn Logic ---

    // Public entry point called by a dying player.
    public void TriggerGlobalDeath(float vanishDuration)
    {
        if (isRespawning) return;

        // Start the process: Cleanup, Wait, Respawn
        StartCoroutine(CoordinatedRespawn(vanishDuration));
    }

    // Coroutine to handle the full sequence: Vanish -> Wait -> Reset -> Appear
    private IEnumerator CoordinatedRespawn(float delay)
    {
        isRespawning = true;
        Debug.Log($"Starting coordinated respawn sequence. Vanish delay: {delay}s.");

        // PHASE 1: Trigger Local Death/Vanish on ALL Players
        foreach (Movement player in allPlayers)
        {
            // This function (now public in Movement.cs) disables physics and starts the dissolve/vanish animation.
            player.StartLocalDeathCleanup();
        }

        // WAIT: Wait for the dissolve animation to complete
        yield return new WaitForSeconds(delay);

        // PHASE 2: Reset Resettable Objects
        foreach (ResettableObject obj in allResettables)
        {
            obj.ResetObject();
        }

        // PHASE 3: Respawn ALL Players
        foreach (Movement player in allPlayers)
        {
            // This function (now public in Movement.cs) moves the player, re-enables physics, and starts the appear animation.
            player.HandleRespawn();
        }

        isRespawning = false;
        Debug.Log("Coordinated respawn complete.");
    }

    // OLD: This is now just a wrapper that calls the coordinated function.
    public void TriggerFullRespawn()
    {
        // Assuming a standard vanish time if called directly without a death
        TriggerGlobalDeath(1.0f); 
    }
}