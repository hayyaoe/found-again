using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class PushPullObject : MonoBehaviour
{
    [HideInInspector] public bool isBeingPushed = false;
    private Rigidbody2D rb;

    // Keep track of which players are pushing
    private HashSet<GameObject> pushingPlayers = new HashSet<GameObject>();

    // Tag certain objects as "co-op only"
    [SerializeField] private bool requiresTwoPlayers = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        LockObject(); // Start locked/unmovable
    }

    // Called when a player starts interacting
    public void AddPushingPlayer(GameObject player)
    {
        if (!pushingPlayers.Contains(player))
            pushingPlayers.Add(player);

        UpdatePushState();
    }

    // Called when a player stops interacting
    public void RemovePushingPlayer(GameObject player)
    {
        if (pushingPlayers.Contains(player))
            pushingPlayers.Remove(player);

        UpdatePushState();
    }

    // Check if conditions are met for movement
    private void UpdatePushState()
    {
        if (requiresTwoPlayers)
        {
            if (pushingPlayers.Count >= 2)
                StartPush();
            else
                StopPush();
        }
        else
        {
            if (pushingPlayers.Count > 0)
                StartPush();
            else
                StopPush();
        }
    }

    public void StartPush()
    {
        if (isBeingPushed) return;
        isBeingPushed = true;
        UnlockObject();
    }

    public void StopPush()
    {
        if (!isBeingPushed) return;
        isBeingPushed = false;
        LockObject();
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
