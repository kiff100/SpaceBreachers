/// ---------------------------------------------
/// Movement Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.MovementPack.Runtime.Tasks
{
    using Opsive.BehaviorDesigner.AddOns.Shared.Runtime.Pathfinding;
    using Opsive.BehaviorDesigner.Runtime.Tasks.Actions;
    using Opsive.BehaviorDesigner.Runtime.Utility;
    using Opsive.GraphDesigner.Runtime.Variables;
    using Opsive.Shared.Utility;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Base class for all Movement Pack tasks.
    /// </summary>
    [HideNameInTaskControl]
    [Category("Movement Pack")]
    public abstract class MovementBase : Action
    {
#if UNITY_EDITOR
        private const string c_PathfinderTypeKey = "Opsive.BehaviorDesigner.AddOns.PathfinderType";
        public static string PathfinderTypeKey => c_PathfinderTypeKey;
#endif

        [Tooltip("Specifies the base pathfinding implementation that should be used.")]
        [SerializeField] [HideInInspector] protected Pathfinder m_Pathfinder;
        [Tooltip("Should the pathfinder be stopped when the task ends?")]
        public SharedVariable<bool> m_StopOnTaskEnd = true;

        public Pathfinder Pathfinder { get => m_Pathfinder; set => m_Pathfinder = value; }

        protected Vector3 Velocity { get => m_Pathfinder.Velocity; }
        protected float RemainingDistance { get => m_Pathfinder.RemainingDistance; }

        /// <summary>
        /// The task has been initialized.
        /// </summary>
        public override void OnAwake()
        {
            base.OnAwake();

            if (m_Pathfinder == null) {
                m_Pathfinder = new NavMeshAgentPathfinder();
                m_Pathfinder.Reset();
            }
            m_Pathfinder.Initialize(gameObject);
        }

        /// <summary>
        /// Starts the task.
        /// </summary>
        public override void OnStart()
        {
            m_Pathfinder.OnStart();
        }

        /// <summary>
        /// Set a new pathfinding destination.
        /// </summary>
        /// <param name="destination">The destination to set.</param>
        /// <returns>True if the destination is valid.</returns>
        protected virtual bool SetDestination(Vector3 destination)
        {
            return m_Pathfinder.SetDesination(destination);
        }

        /// <summary>
        /// Does the agent have a pathfinding path?
        /// </summary>
        /// <returns>True if the agent has a pathfinding path.</returns>
        protected bool HasPath()
        {
            return m_Pathfinder.HasPath();
        }

        /// <summary>
        /// Returns true if the position is a valid pathfinding position.
        /// </summary>
        /// <param name="position">The position to sample. The position will be updated to the valid sampled position.</param>
        /// <returns>True if the position is a valid pathfinding position.</returns>
        protected bool SamplePosition(ref Vector3 position)
        {
            return m_Pathfinder.SamplePosition(ref position);
        }

        /// <summary>
        /// Has the agent arrived at the destination?
        /// </summary>
        /// <returns>True if the agent has arrived at the destination.</returns>
        public bool HasArrived()
        {
            return m_Pathfinder.HasArrived();
        }

        /// <summary>
        /// The task has ended.
        /// </summary>
        public override void OnEnd()
        {
            if (m_StopOnTaskEnd.Value) {
                m_Pathfinder.Stop();
            }
            m_Pathfinder.OnEnd();
        }

        /// <summary>
        /// Specifies the type of reflection that should be used to save the task.
        /// </summary>
        /// <param name="index">The index of the sub-task. This is used for the task set allowing each contained task to have their own save type.</param>
        public override MemberVisibility GetSaveReflectionType(int index) { return MemberVisibility.None; }

        /// <summary>
        /// Returns the current task state.
        /// </summary>
        /// <param name="world">The DOTS world.</param>
        /// <param name="entity">The DOTS entity.</param>
        /// <returns>The current task state.</returns>
        public override object Save(World world, Entity entity)
        {
            if (!m_BehaviorTree.IsNodeActive(true, m_RuntimeIndex)) return null;

            var saveData = new MovementSaveData();
            if (m_Pathfinder.HasPath() && !m_Pathfinder.HasArrived()) {
                saveData.Destination = m_Pathfinder.Destination;
                saveData.HasDestination = true;
            } else {
                saveData.Destination = Vector3.zero;
                saveData.HasDestination = false;
            }

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
            if (saveData == null) {
                return;
            }

            var movementSaveData = (MovementSaveData)saveData;
            if (movementSaveData.HasDestination) {
                m_Pathfinder.SetDesination(movementSaveData.Destination);
            }
        }

        /// <summary>
        /// The behavior tree has been stopped.
        /// </summary>
        /// <param name="paused">Was the behavior tree paused.</param>
        public override void OnBehaviorTreeStopped(bool paused)
        {
            base.OnBehaviorTreeStopped(paused);

            if (m_Pathfinder.HasPath()) {
                m_Pathfinder.Stop();
            }
        }

        /// <summary>
        /// The behavior tree has been destroyed.
        /// </summary>
        public override void OnDestroy()
        {
            m_Pathfinder.Stop();
        }

        /// <summary>
        /// Resets the task values back to their default.
        /// </summary>
        public override void Reset()
        {
#if UNITY_EDITOR
            if (m_Pathfinder == null) {
                var defaultPathfinderType = UnityEditor.EditorPrefs.GetString(c_PathfinderTypeKey);
                if (!string.IsNullOrEmpty(defaultPathfinderType)) {
                    var pathfinderType = TypeUtility.GetType(defaultPathfinderType);
                    if (pathfinderType != null) {
                        m_Pathfinder = System.Activator.CreateInstance(pathfinderType) as Pathfinder;
                    }
                }

                // The NavMeshAgentPathfinder will always be available.
                if (m_Pathfinder == null) {
                    m_Pathfinder = new NavMeshAgentPathfinder();
                }
            }
#endif
            m_Pathfinder.Reset();
            m_StopOnTaskEnd = true;
        }

        /// <summary>
        /// Data structure for saving movement state.
        /// </summary>
        private class MovementSaveData
        {
            [Tooltip("The current destination that the agent is moving towards.")]
            public Vector3 Destination;
            [Tooltip("Indicates if the agent has a valid destination to move towards.")]
            public bool HasDestination;
        }
    }
}