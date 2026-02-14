using UnityEngine;

public class MagnetProjectile : MonoBehaviour
{
    public float maxSpeed = 20f;
    public float minSpeed = 5f;
    public float decelerationRate = 2f; // Speed decrease per second
    
    private Rigidbody2D rb;
    private float holdDuration;
    private Vector2 fireDirection = Vector2.right;
    private float currentSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        if (rb == null)
        {
            Debug.LogError("MagnetProjectile requires a Rigidbody2D component!");
            return;
        }

        currentSpeed = Mathf.Lerp(minSpeed, maxSpeed, Mathf.Clamp01(holdDuration));
        rb.linearVelocity = fireDirection * currentSpeed;
        Debug.Log($"Shot fired at speed{currentSpeed:F2}");
    }

    void FixedUpdate()
    {
        currentSpeed -= decelerationRate * Time.deltaTime;
        currentSpeed = Mathf.Max(currentSpeed, 0f); // Clamp to zero
        rb.linearVelocity = fireDirection * currentSpeed;
    }

    public void Fire(float duration, Vector2 direction)
    {
        holdDuration = duration;
        fireDirection = direction;
    }
}
