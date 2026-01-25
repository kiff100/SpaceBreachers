/// ---------------------------------------------
/// Movement Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.MovementPack.Runtime.Tasks
{
    using Opsive.BehaviorDesigner.Runtime.Tasks;
    using Opsive.BehaviorDesigner.Runtime.Utility;
    using Opsive.GraphDesigner.Runtime;
    using Opsive.GraphDesigner.Runtime.Variables;
    using System;
    using Unity.Entities;
    using UnityEngine;

    [Opsive.Shared.Utility.Description("Patrols around the specified waypoints.")]
    [NodeIcon("6f82a4685e473054986cedbfb24bf8bd", "ed44a06209d60e943819270678e69b89")]
    public class Patrol : MovementBase
    {
        [Tooltip("The waypoints that the agent should move between.")]
        [SerializeField] protected SharedVariable<GameObject[]> m_Waypoints;
        [Tooltip("Should the agent start patrolling to the closest waypoint?")]
        [SerializeField] protected SharedVariable<bool> m_ClosestWaypointStart;
        [Tooltip("Should the agent patrol the waypoints randomly?")]
        [SerializeField] protected SharedVariable<bool> m_RandomPatrol;
        [Tooltip("The length of time that the agent should pause when arriving at a waypoint.")]
        [SerializeField] protected SharedVariable<RangeFloat> m_WaypointPauseDuration;

        private int m_Index;
        private float m_NextDestinationTime;

        // Callback when the agent arrives at a waypoint.
        public Action<GameObject> OnWaypointArrival { get; set; }

        /// <summary>
        /// The task has started.
        /// </summary>
        public override void OnStart()
        {
            base.OnStart();

            if (m_RandomPatrol.Value) {
                m_Index = UnityEngine.Random.Range(0, m_Waypoints.Value.Length);
            } else if (m_ClosestWaypointStart.Value) {
                // Move towards the closest waypoint at the start.
                var distance = Mathf.Infinity;
                float localDistance;
                for (int i = 0; i < m_Waypoints.Value.Length; ++i) {
                    if ((localDistance = Vector3.SqrMagnitude(transform.position - m_Waypoints.Value[i].transform.position)) < distance) {
                        distance = localDistance;
                        m_Index = i;
                    }
                }
            }
            m_NextDestinationTime = -1;
            SetDestination(GetTargetDestination());
        }

        /// <summary>
        /// Returns the target destination.
        /// </summary>
        /// <returns>The target destination.</returns>
        private Vector3 GetTargetDestination()
        {
            if (m_Index >= m_Waypoints.Value.Length) {
                return transform.position;
            }
            return m_Waypoints.Value[m_Index].transform.position;
        }

        /// <summary>
        ///  Patrol around the different waypoints specified in the waypoint array.
        /// </summary>
        /// <returns>Always returns a status of running. </returns>
        public override TaskStatus OnUpdate()
        {
            if (m_Waypoints.Value.Length == 0) {
                return TaskStatus.Failure;
            }

            if (HasArrived()) {
                if (m_NextDestinationTime == -1) {
                    m_NextDestinationTime = Time.time + m_WaypointPauseDuration.Value.RandomValue;

                    // Others may be interested when the agent arrives at a waypoint.
                    OnWaypointArrival?.Invoke(m_Waypoints.Value[m_Index]);
                }
                // Wait the required duration before switching waypoints.
                if (m_NextDestinationTime <= Time.time) {
                    if (m_RandomPatrol.Value) {
                        if (m_Waypoints.Value.Length == 1) {
                            m_Index = 0;
                        } else {
                            // Prevent the same waypoint from being selected.
                            var nextIndex = m_Index;
                            while (nextIndex == m_Index) {
                                nextIndex = UnityEngine.Random.Range(0, m_Waypoints.Value.Length);
                            }
                            m_Index = nextIndex;
                        }
                    } else {
                        m_Index = (m_Index + 1) % m_Waypoints.Value.Length;
                    }
                    SetDestination(GetTargetDestination());
                    m_NextDestinationTime = -1;
                }
            }

            return TaskStatus.Running;
        }

        /// <summary>
        /// Returns the current task state.
        /// </summary>
        /// <param name="world">The DOTS world.</param>
        /// <param name="entity">The DOTS entity.</param>
        /// <returns>The current task state.</returns>
        public override object Save(World world, Entity entity)
        {
            var saveData = new object[3];
            saveData[0] = base.Save(world, entity);
            saveData[1] = m_Index;
            saveData[2] = Time.time - m_NextDestinationTime;
            return saveData;
        }

        /// <summary>
        /// Loads the previous task state.
        /// </summary>
        /// <param name="saveData">The previous task state.</param>
        /// <param name="world">The DOTS world.</param>
        /// <param name="entity">The DOTS entity.</param>
        public override void Load(object saveData, World world, Entity entity)
        {
            var saveDataArray = (object[])saveData;
            base.Load(saveDataArray[0], world, entity);
            m_Index = (int)saveDataArray[1];
            m_NextDestinationTime = Time.time - (float)saveDataArray[1];
        }

        /// <summary>
        /// Resets the task values back to their default.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            m_Waypoints = new GameObject[0];
            m_ClosestWaypointStart = false;
            m_RandomPatrol = false;
            m_WaypointPauseDuration = new RangeFloat();
        }
    }
}