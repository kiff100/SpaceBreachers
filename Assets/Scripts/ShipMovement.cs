using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class ShipMovement : MonoBehaviour
{
    
    private NavMeshAgent agent;
    private float prefabStartingRotationX;
    private float prefabstartingRotationY;
    public float rotationSpeed = 5f; // Rotation speed multiplier

    void Start()
    {
        agent = GetComponentInChildren<NavMeshAgent>();
        agent.updateRotation = false; // Disable automatic rotation
        agent.updateUpAxis = false; // Disable automatic up axis adjustment
    }

    void Update()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            HandleRightClick();
            Debug.Log("Right mouse button clicked, handling movement.");
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
        if (agent.velocity.sqrMagnitude > 0.01f) // Only rotate when moving
        {
            Vector3 direction = agent.velocity.normalized;
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float currentAngle = transform.eulerAngles.z;

            // Smoothly rotate toward the target angle
            float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * rotationSpeed);
            transform.eulerAngles = new Vector3(0, 0, newAngle);
        }
    }
}
