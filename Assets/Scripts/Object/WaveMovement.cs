using UnityEngine;

public class WaveMovement : MonoBehaviour
{
    [Header("Wave Motion")]
    public float amplitude = 0.5f;
    public float frequency = 1f;

    [Header("Boat Tilt")]
    public float tiltAmplitude = 8f;

    private Vector3 basePos;

    void Start()
    {
        basePos = transform.position;
    }

    void Update()
    {
        float t = Time.time * frequency;

        float waveY = Mathf.Sin(t) * amplitude;
        float tiltAngle = Mathf.Cos(t) * tiltAmplitude;

        transform.position = basePos + new Vector3(0f, waveY, 0f);
        transform.rotation = Quaternion.Euler(0f, 0f, tiltAngle);
    }

    public void SetBasePosition(Vector3 newBase)
    {
        basePos = newBase;
    }

    public Vector3 GetBasePosition()
    {
        return basePos;
    }
}
