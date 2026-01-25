/// ---------------------------------------------
/// Tactical Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.TacticalPack.Demo
{
    using UnityEngine;
    using Opsive.BehaviorDesigner.AddOns.Shared.Demo;
    using System.Collections;

    /// <summary>
    /// Moves the NavMeshPathfindingAgent between the specified destinations in a patrol pattern.
    /// </summary>
    public class NavMeshMover : MonoBehaviour
    {
        [Tooltip("The amount of delay until the agent starts to move.")]
        [SerializeField] protected float m_Delay;
        [Tooltip("The positions the agent should patrol between.")]
        [SerializeField] protected Vector3[] m_Destinations;
        [Tooltip("The minimum distance to destination before moving to next waypoint.")]
        [SerializeField] protected float m_ArrivalDistance = 1.0f;

        private NavMeshPathfindingAgent m_PathfindingAgent;
        private Vector3 m_StartPosition;
        private Coroutine m_MoveCoroutine;
        private int m_CurrentDestinationIndex = 0;

        /// <summary>
        /// The agent has been enabled.
        /// </summary>
        public void OnEnable()
        {
            if (m_PathfindingAgent == null) {
                return;
            }

            StartMove();
        }

        /// <summary>
        /// Sets the NavMeshPathfindingAgent.
        /// </summary>
        public void Start()
        {
            m_PathfindingAgent = GetComponent<NavMeshPathfindingAgent>();
            m_StartPosition = transform.position;

            StartMove();
        }

        /// <summary>
        /// Starts to move the agent.
        /// </summary>
        private void StartMove()
        {
            if (m_MoveCoroutine != null) {
                StopCoroutine(m_MoveCoroutine);
                m_MoveCoroutine = null;
            }
            m_PathfindingAgent.Warp(m_StartPosition);
            m_MoveCoroutine = StartCoroutine(Move());
        }

        /// <summary>
        /// Moves the agent between patrol destinations.
        /// </summary>
        private IEnumerator Move()
        {
            yield return new WaitForSeconds(m_Delay);

            if (m_Destinations == null || m_Destinations.Length == 0) {
                m_MoveCoroutine = null;
                yield break;
            }

            if (m_Destinations.Length == 1) {
                m_PathfindingAgent.SetDestination(m_Destinations[0]);
                m_MoveCoroutine = null;
                yield break;
            }

            // Start patrolling between destinations.
            while (true) {
                var currentDestination = m_Destinations[m_CurrentDestinationIndex];
                m_PathfindingAgent.SetDestination(currentDestination);

                yield return new WaitUntil(() => Vector3.Distance(transform.position, currentDestination) <= m_ArrivalDistance);

                m_CurrentDestinationIndex = (m_CurrentDestinationIndex + 1) % m_Destinations.Length;
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}