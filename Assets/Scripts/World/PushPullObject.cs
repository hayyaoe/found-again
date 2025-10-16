using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class PushPullObject : MonoBehaviour
{
    [Header("Multi-player Pushing")]
    [Tooltip("How many players are required to move this object?")]
    [SerializeField] public int requiredPlayers = 1;

    [HideInInspector] public bool isBeingPushed = false; // Is the object currently unlocked/movable?
    
    private Rigidbody2D rb;
    private List<PlayerPushPull> pushingPlayers = new List<PlayerPushPull>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        LockObject(); // Start locked/unmovable
    }

    public void AddPusher(PlayerPushPull pusher)
    {
        if (!pushingPlayers.Contains(pusher))
        {
            pushingPlayers.Add(pusher);
            CheckPushState();
        }
    }

    public void RemovePusher(PlayerPushPull pusher)
    {
        if (pushingPlayers.Contains(pusher))
        {
            pushingPlayers.Remove(pusher);
            CheckPushState();
        }
    }

    private void CheckPushState()
    {
        // If we have enough players, unlock the object and tell them
        if (pushingPlayers.Count >= requiredPlayers)
        {
            if (!isBeingPushed)
            {
                isBeingPushed = true;
                UnlockObject();
                // Notify all players they can now push
                foreach (var player in pushingPlayers)
                {
                    player.OnPushSuccessful();
                }
            }
        }
        else // Not enough players
        {
            if (isBeingPushed)
            {
                isBeingPushed = false;
                LockObject();
                // Notify all players they must stop pushing
                foreach (var player in pushingPlayers)
                {
                    player.OnPushFailed();
                }
            }
        }
    }

    private void LockObject()
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        rb.angularVelocity = 0f;
    }

    private void UnlockObject()
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }
}
