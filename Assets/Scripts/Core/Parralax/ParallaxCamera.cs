using UnityEngine;

[ExecuteInEditMode]
public class ParallaxCamera : MonoBehaviour
{
    public static event System.Action<float> OnCameraTranslate;

    private float oldPosition;

    void Start()
    {
        oldPosition = transform.position.x;
    }

    void Update()
    {
        if (transform.position.x != oldPosition)
        {
            float delta = oldPosition - transform.position.x;
            
            // Fire event for all listeners
            OnCameraTranslate?.Invoke(delta);

            oldPosition = transform.position.x;
        }
    }
}
