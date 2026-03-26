using UnityEngine;
using UnityEngine.AI;

public class BreacherSoldier : MonoBehaviour
{
    [SerializeField] private NavMeshAgent navMeshAgent;
    [SerializeField] private Transform playerShip;
    [SerializeField] private Transform targetShip;

    void Start()
    {
        // Initialize NavMeshAgent
        if (navMeshAgent == null)
        {
            navMeshAgent = GetComponent<NavMeshAgent>();

            // If not found on this object, check children
            if (navMeshAgent == null)
            {
                navMeshAgent = GetComponentInChildren<NavMeshAgent>();
                if (navMeshAgent != null)
                {
                    Debug.Log("NavMeshAgent found on child object");
                }
            }
            else { 
                Debug.Log("NavMeshAgent found on BreacherSoldier object");
            }
        }

        if (navMeshAgent == null)
        {
            Debug.LogError($"NavMeshAgent not found on BreacherSoldier '{gameObject.name}' or its children! Please add a NavMeshAgent component to this prefab.");
            return;
        }

        // Enable the agent
        navMeshAgent.enabled = true;
        Debug.Log($"NavMeshAgent initialized on {gameObject.name}");

        // Ensure agent is on NavMesh
        if (!navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.Warp(transform.position);
            Debug.Log($"BreacherSoldier warped to position: {transform.position}");
        }
    }

    void Update()
    {
        // Monitor agent state
        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            if (navMeshAgent.hasPath && navMeshAgent.remainingDistance > 0.1f)
            {
                // Agent is moving towards destination
            }
        }
    }

    public void SetTargetShip(Transform target)
    {
        if (target == null)
        {
            Debug.LogError("Cannot set null target ship");
            return;
        }

        targetShip = target;

        // Move to target ship
        navMeshAgent = this.GetComponent<NavMeshAgent>(); // Ensure agent is enabled before setting destination
        if (navMeshAgent == null)
        {
            Debug.LogError("NavMeshAgent is null!");
            return;
        }

        if (targetShip == null)
        {
            Debug.LogError("Target ship is null!");
            return;
        }

        if (!navMeshAgent.enabled)
        {
            navMeshAgent.enabled = true;
        }

        navMeshAgent.SetDestination(targetShip.position);
        Debug.Log($"BreacherSoldier moving to target: {targetShip.gameObject.name} at position {targetShip.position}");
    }

    public void SetPlayerShip(Transform player)
    {
        playerShip = player;
    }

    public bool IsMoving()
    {
        return navMeshAgent != null && navMeshAgent.hasPath && navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance;
    }
}
