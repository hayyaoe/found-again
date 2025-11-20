using UnityEngine;

public class BoatTriggerForwarder : MonoBehaviour
{
    private BoatMove parentBoat;
    [SerializeField] private bool logDebug = true;

    void Start()
    {
        parentBoat = GetComponentInParent<BoatMove>();

        if (logDebug)
        {
            if (parentBoat == null)
                Debug.LogWarning($"[BoatTriggerForwarder] NO BoatMove found in parents of {name}");
            else
                Debug.Log($"[BoatTriggerForwarder] Found BoatMove on {parentBoat.name} for trigger {name}");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (logDebug)
            Debug.Log($"[BoatTriggerForwarder] OnTriggerEnter2D on {name} with {other.name} (tag={other.tag})");

        parentBoat?.NotifyEnter(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (logDebug)
            Debug.Log($"[BoatTriggerForwarder] OnTriggerExit2D on {name} with {other.name} (tag={other.tag})");

        parentBoat?.NotifyExit(other);
    }
}
