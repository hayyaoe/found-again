using UnityEngine;

public class FloatingMotion : MonoBehaviour
{
    public float amplitudeX = 0.5f;  // distance to move on X
    public float amplitudeY = 0.5f;  // distance to move on Y
    public float frequency = 1f;     // how fast to move

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        float offsetX = Mathf.Sin(Time.time * frequency) * amplitudeX;
        float offsetY = Mathf.Cos(Time.time * frequency * 0.7f) * amplitudeY;
        transform.position = startPos + new Vector3(offsetX, offsetY, 0f);
    }
}
