using UnityEngine;

public class TriggerTest2D : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[TriggerTest2D] ENTER on {name} with {other.name}");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log($"[TriggerTest2D] EXIT on {name} with {other.name}");
    }
}