using UnityEngine;
using System.Collections.Generic;

public class BoatMove : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public Vector2 moveDirection = Vector2.right;

    [Header("Required Players")]
    public int requiredPlayers = 1;

    [Header("Debug")]
    public bool logDebug = true;
    [SerializeField] private GameObject roomZoneToDisable;
    public WaveMovement waveScript;

    [Header("Smoothing")]
    public float accelerationTime = 1f;   // how long until full speed
    private float currentSpeed = 0f; // <-- IMPORTANT: This needs to be reset

    private readonly List<Transform> playersOnBoard = new List<Transform>();
    private readonly Dictionary<Transform, Transform> originalParents = new Dictionary<Transform, Transform>();
    private bool isShuttingDown = false;

    public bool IsMoving => playersOnBoard.Count >= requiredPlayers;
    public static bool AnyPlayerOnBoat = false;


    private void OnDisable()
    {
        isShuttingDown = true;
    }

    private void Awake()
    {
        if (waveScript == null)
            waveScript = GetComponent<WaveMovement>();

        if (moveDirection.sqrMagnitude > 0.01f)
            moveDirection = moveDirection.normalized;
    }

    void Update()
    {
        if (playersOnBoard.Count < requiredPlayers)
        {
            // Decelerate if players leave before reaching the destination
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, Time.deltaTime * (moveSpeed / accelerationTime));
            
            // Re-enable the room zone if needed
            if (roomZoneToDisable != null && roomZoneToDisable.activeSelf == false && currentSpeed < 0.01f)
            {
                roomZoneToDisable.SetActive(true);
                Debug.Log("[BoatTrigger] RoomZone re-enabled due to player exit/deceleration.");
            }
        }
        else
        {
             // Smooth acceleration (0 → moveSpeed)
            currentSpeed = Mathf.MoveTowards(currentSpeed, moveSpeed, Time.deltaTime * (moveSpeed / accelerationTime));

            if (roomZoneToDisable != null)
            {
                roomZoneToDisable.SetActive(false);
                Debug.Log("[BoatTrigger] RoomZone disabled.");
            }
        }


        // Gerakan boat (Only move if there is *some* speed, even during deceleration)
        if (currentSpeed > 0.001f)
        {
            Vector3 oldBase = waveScript.GetBasePosition();
            Vector3 delta = (Vector3)moveDirection * (currentSpeed * Time.deltaTime);
            Vector3 newBase = oldBase + delta;
            waveScript.SetBasePosition(newBase);
        }
    }

    // 🟢 NEW: Public method to reset the boat's movement state
    public void ResetMovementState()
    {
        // 1. Stop the boat instantly
        currentSpeed = 0f;
        
        // 2. Clear players and restore their parenting (if possible)
        foreach(Transform player in playersOnBoard)
        {
             if (originalParents.ContainsKey(player))
            {
                // Restore original parent
                player.SetParent(originalParents[player], true);
            }
        }
        
        // Clear lists
        playersOnBoard.Clear();
        originalParents.Clear();

        // 3. Reset static flag
        AnyPlayerOnBoat = false;

        // 4. Re-enable the room zone
        if (roomZoneToDisable != null)
        {
            roomZoneToDisable.SetActive(true);
            Debug.Log("[BoatMove] RoomZone reset and re-enabled.");
        }

        if (logDebug)
            Debug.Log("[BoatMove] Boat movement state fully reset.");
    }
    
    // Trigger masuk boat
    public void NotifyEnter(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Transform player = other.transform;

        if (!playersOnBoard.Contains(player))
        {
            playersOnBoard.Add(player);

            if (playersOnBoard.Count > 0)
                AnyPlayerOnBoat = true;

            // Simpan parent asli
            if (!originalParents.ContainsKey(player))
                originalParents[player] = player.parent;

            // Parentkan seluruh player object
            player.SetParent(transform, true);

            if (logDebug)
                Debug.Log($"[BoatMove] Parent PLAYER: {player.name} → {name}");

            if (logDebug)
                Debug.Log($"[BoatMove] {player.name} naik boat. OnBoard={playersOnBoard.Count}");
        }
    }

    // Trigger keluar boat
    public void NotifyExit(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Transform player = other.transform;

        playersOnBoard.Remove(player);
        
        if (playersOnBoard.Count == 0)
            AnyPlayerOnBoat = false;

        // If boat is shutting down, DO NOT parent back (Unity would throw error)
        if (isShuttingDown)
        {
            if (logDebug)
                Debug.Log("[BoatMove] Boat is disabling — skip restoring parent.");
            return;
        }

        // Safe: restore parent only if boat is active
        if (originalParents.ContainsKey(player))
        {
            player.SetParent(originalParents[player], true);

            if (logDebug)
                Debug.Log($"[BoatMove] Restore PLAYER parent: {player.name}");
        }

        if (logDebug)
            Debug.Log($"[BoatMove] {player.name} turun boat. OnBoard={playersOnBoard.Count}");
    }
}