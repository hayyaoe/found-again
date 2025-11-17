using UnityEngine;

public class CameraSway : MonoBehaviour
{
    [Header("Motion Settings")]
    public float positionAmplitude = 0.05f; // how far it moves
    public float rotationAmplitude = 0.5f;  // how much it rotates
    public float frequency = 1.5f;          // how fast it moves

    private Vector3 initialPos;
    private Quaternion initialRot;

    void Start()
    {
        initialPos = transform.localPosition;
        initialRot = transform.localRotation;
    }

    void Update()
    {
        float time = Time.time * frequency;

        // Sine wave motion
        float offsetX = Mathf.PerlinNoise(time, 0f) - 0.5f;
        float offsetY = Mathf.PerlinNoise(0f, time) - 0.5f;

        // Smooth, natural motion using Perlin noise
        Vector3 posOffset = new Vector3(offsetX, offsetY, 0) * positionAmplitude;
        Vector3 rotOffset = new Vector3(offsetY, offsetX, 0) * rotationAmplitude;

        transform.localPosition = initialPos + posOffset;
        transform.localRotation = initialRot * Quaternion.Euler(rotOffset);
    }
}
