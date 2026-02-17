using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class ShipMovement : MonoBehaviour
{
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
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
    }

    private void HandleRightClick()
    {
        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        mouseScreenPos.z = -Camera.main.transform.position.z;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        agent.SetDestination(mouseWorldPos);
        Debug.Log($"Right-clicked at {mouseWorldPos}, moving ship to that location.");
    }
}
