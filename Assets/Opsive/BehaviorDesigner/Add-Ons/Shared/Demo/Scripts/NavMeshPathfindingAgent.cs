/// ---------------------------------------------
/// Shared Add-On for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.Shared.Demo
{
    using UnityEngine;
    using UnityEngine.AI;

    /// <summary>
    /// Implements IPathfindingAgent for the NavMeshAgent.
    /// </summary>
    public class NavMeshPathfindingAgent : MonoBehaviour, IPathfindingAgent
    {
        private NavMeshAgent m_NavMeshAgent;

        /// <summary>
        /// Initializes the default values.
        /// </summary>
        private void Awake()
        {
            m_NavMeshAgent = GetComponent<NavMeshAgent>();
        }

        /// <summary>
        /// Warps the pathfinding implementation.
        /// </summary>
        /// <param name="position">The target position.</param>
        public void Warp(Vector3 position)
        {
            m_NavMeshAgent.Warp(position);
        }

        /// <summary>
        /// Sets the target destination.
        /// </summary>
        /// <param name="position">The position that should be set.</param>
        public void SetDestination(Vector3 position)
        {
            m_NavMeshAgent.SetDestination(position);
        }
    }
}