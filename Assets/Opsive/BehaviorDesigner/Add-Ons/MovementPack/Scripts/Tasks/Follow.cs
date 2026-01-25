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
    using UnityEngine;

    [Opsive.Shared.Utility.Description("Follows the specified target from a distance.")]
    [NodeIcon("0221132e8633eb840a68588d030942d2", "12095e7f978bf294c915462d18637030")]
    public class Follow : MovementBase
    {
        [Tooltip("The GameObject that the agent is following.")]
        [SerializeField] protected SharedVariable<GameObject> m_Target;
        [Tooltip("Start moving towards the target if the target is further than the specified distance")]
        [SerializeField] protected SharedVariable<RangeFloat> m_FollowRange = new RangeFloat(1, 3);

        /// <summary>
        /// The task has started.
        /// </summary>
        public override void OnStart()
        {
            base.OnStart();

            if (m_Target == null) {
                Debug.LogError("Error: A target must be set on the Follow ability.");
                return;
            }
        }

        /// <summary>
        /// Follows the target.
        /// </summary>
        /// <returns>Always returns running as the agent should continue to follow the target.</returns>
        public override TaskStatus OnUpdate()
        {
            if (m_Target.Value == null) {
                return TaskStatus.Failure;
            }

            var direction = (m_Target.Value.transform.position - transform.position);
            var distance = direction.magnitude;
            if (distance > m_FollowRange.Value.Max) {
                // Move towards the target if the agent is too far away.
                SetDestination(m_Target.Value.transform.position);
            } else if (distance < m_FollowRange.Value.Min) {
                // Move away from the target if the agent is too close.
                SetDestination(m_Target.Value.transform.position + direction.normalized * m_FollowRange.Value.Min);
            } else {
                // Stop moving if the agent is within the follow range.
                m_Pathfinder.Stop();
            }

            return TaskStatus.Running;
        }

        /// <summary>
        /// Resets the task values back to their default.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            m_Target = null;
            m_FollowRange = new RangeFloat(1, 3);
        }
    }
}