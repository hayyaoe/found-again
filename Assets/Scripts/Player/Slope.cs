using UnityEngine;

public class Slope : MonoBehaviour
{
    public Rigidbody2D.SlideMovement SlideMovement = new Rigidbody2D.SlideMovement();
    public Rigidbody2D.SlideResults SlideResults;

    public float HorizontalSpeed = 2f;

    private Rigidbody2D m_Rigidbody;

    void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Calculate the horizontal velocity from keyboard input.
        var horizontalInput = (Input.GetKey(KeyCode.LeftArrow) ? -1 : 0f) + (Input.GetKey(KeyCode.RightArrow) ? 1f : 0f);
        var velocity = new Vector2(horizontalInput * HorizontalSpeed, 0f);
        

        // Slide the rigidbody.
        SlideResults = m_Rigidbody.Slide(velocity, Time.deltaTime, SlideMovement);
    }
}
