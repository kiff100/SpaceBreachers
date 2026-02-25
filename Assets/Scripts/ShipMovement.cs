using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class ShipMovement : MonoBehaviour
{
    private NavMeshAgent agent;
    private Rigidbody2D rb;
    public float rotationSpeed = 5f; // Rotation speed multiplier
    public float velocityDamping = 0.95f; // Physics damping for smooth movement

    void Start()
    {
        agent = GetComponentInChildren<NavMeshAgent>();
        agent.updateRotation = false; // Disable automatic rotation
        agent.updateUpAxis = false; // Disable automatic up axis adjustment
        agent.updatePosition = false; // Disable automatic position update; we'll use physics instead

        rb = GetComponentInParent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("ShipMovement requires a Rigidbody2D component on the parent or self!");
            return;
        }

        // Configure rigidbody for physics-based movement
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 0.5f;
    }

    void Update()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            HandleRightClick();
            Debug.Log("Right mouse button clicked, handling movement.");
        }
    }

    void FixedUpdate()
    {
        // Update agent position to match rigidbody position
        agent.nextPosition = rb.position;

        // Apply desired velocity as force for physics-based movement
        if (agent.hasPath && !agent.pathPending)
        {
            Vector2 desiredVelocity = agent.desiredVelocity;
            Vector2 velocityDifference = desiredVelocity - rb.linearVelocity;
            rb.AddForce(velocityDifference * rb.mass, ForceMode2D.Force);
        }

        RotateTowardMovement();
    }

    private void HandleRightClick()
    {
        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        mouseScreenPos.z = -Camera.main.transform.position.z;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        agent.SetDestination(mouseWorldPos);
        Debug.Log($"Right-clicked at {mouseWorldPos}, moving ship to that location.");
    }

    private void RotateTowardMovement()
    {
        if (rb.linearVelocity.sqrMagnitude > 0.01f) // Only rotate when moving
        {
            Vector2 direction = rb.linearVelocity.normalized;
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float currentAngle = transform.eulerAngles.z;

            // Smoothly rotate toward the target angle
            float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * rotationSpeed);
            transform.eulerAngles = new Vector3(0, 0, newAngle);
        }
    }
}
