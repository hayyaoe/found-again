using UnityEngine;

public class MovingLiftTrigger : MonoBehaviour
{
    private AutoElevator2D lift;

    void Awake()
    {
        lift = GetComponentInParent<AutoElevator2D>();
        if (lift == null)
            Debug.LogError("LiftTriggerProxy: No AutoElevator2D found in parent!");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (lift != null)
            lift.OnTriggerEnter2D(other);   // Forward event
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (lift != null)
            lift.OnTriggerExit2D(other);    // Forward event
    }
}
