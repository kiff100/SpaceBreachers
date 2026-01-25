/// ---------------------------------------------
/// Movement Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Tasks
{
#if BEHAVIOR_DESIGNER_MOVEMENT_PACK
    using Opsive.BehaviorDesigner.AddOns.MovementPack.Runtime.Tasks;
    using Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Emitters;
    using Opsive.BehaviorDesigner.Runtime.Tasks;
    using Opsive.GraphDesigner.Runtime;
    using Opsive.GraphDesigner.Runtime.Variables;
    using UnityEngine;

    [Opsive.Shared.Utility.Description("Allows an agent to follow a trail of traces, useful for scent-based or trail-based navigation. Requires the Movement Pack for Behavior Designer Pro.")]
    [NodeIcon("6252c826bade96646bb2667fc31bc6cc", "2ff95816f6239074380224d140a02690")]
    [Shared.Utility.Category("Senses Pack")]
    public class FollowTraceTrail : MovementBase
    {
        [Tooltip("The maximum range at which traces can be detected.")]
        [SerializeField] protected SharedVariable<float> m_Range = 2f;
        [Tooltip("The minimum intensity required to consider a trace.")]
        [SerializeField] protected SharedVariable<float> m_MinIntensity = 0.01f;
        [Tooltip("The offset to sample the trace position.")]
        [SerializeField] protected SharedVariable<Vector3> m_PositionOffset = new Vector3(0, 0, 1);

        private Vector3 m_LastTargetPosition;

        /// <summary>
        /// Initializes the task.
        /// </summary>
        public override void OnStart()
        {
            base.OnStart();

            m_LastTargetPosition = m_Transform.position;
        }

        /// <summary>
        /// Updates the task.
        /// </summary>
        /// <returns>Success if the agent is following the trail, Failure if no trail is found.</returns>
        public override TaskStatus OnUpdate()
        {
            var (intensity, tracePosition) = TraceManager.Instance.GetIntensityPositionAt(m_Transform.TransformPoint(m_PositionOffset.Value), m_Range.Value);
            if (intensity > m_MinIntensity.Value) {
                if (HasArrived()) {
                    return TaskStatus.Success;
                }
                SetDestination(tracePosition);
                m_LastTargetPosition = tracePosition;
                return TaskStatus.Running;
            }

            return m_LastTargetPosition == m_Transform.position ? TaskStatus.Failure : TaskStatus.Success;
        }
    }
#else
    /// <summary>
    /// A task that allows an agent to follow a trail of traces, useful for scent-based or trail-based navigation.
    /// Requires the Movement Pack for Behavior Designer Pro.
    /// </summary>
    public class FollowTraceTrail : Opsive.BehaviorDesigner.Runtime.Tasks.Actions.Action
    {
        /// <summary>
        /// Initializes the task and logs a warning if the Movement Pack is not installed.
        /// </summary>
        public override void OnAwake()
        {
            base.OnAwake();

            UnityEngine.Debug.LogWarning("The Follow Trace Trail task requires the Movement Pack for Behavior Designer Pro: https://assetstore.unity.com/packages/tools/behavior-ai/movement-pack-for-behavior-designer-pro-310243?aid=1100lGdc");
        }
    }
#endif
}