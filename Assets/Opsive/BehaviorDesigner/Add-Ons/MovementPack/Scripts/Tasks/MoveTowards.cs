/// ---------------------------------------------
/// Movement Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.MovementPack.Runtime.Tasks
{
    using Opsive.BehaviorDesigner.Runtime.Tasks;
    using Opsive.BehaviorDesigner.Runtime.Tasks.Actions;
    using Opsive.GraphDesigner.Runtime;
    using Opsive.GraphDesigner.Runtime.Variables;
    using UnityEngine;

    [Opsive.Shared.Utility.Description("Move towards the target position using Vector3.MoveTowards. The agent can pass through walls with this task. " +
                     "The position can either be specified by a transform or position. If the transform is used then the position will not be used.")]
    [NodeIcon("9bff476371f650649ab78c6a1242cfba", "938b825eb3c15594d985af1d0a93439e")]
    public class MoveTowards : Action
    {
        [Tooltip("The speed of the agent")]
        [SerializeField] protected SharedVariable<float> m_Speed = 5;
        [Tooltip("The agent has arrived when the magnitude is less than this value.")]
        [SerializeField] protected SharedVariable<float> m_ArrivedDistance = 0.1f;
        [Tooltip("Should the local y value be ignored?")]
        [SerializeField] protected SharedVariable<bool> m_IgnoreYDirection = true;
        [Tooltip("Should the agent be looking at the target position?")]
        [SerializeField] protected SharedVariable<bool> m_LookAtTarget = true;
        [Tooltip("Specifies the max rotation delta if lookAtTarget is enabled.")]
        [SerializeField] protected SharedVariable<float> m_MaxLookAtRotationDelta = 1;
        [Tooltip("The GameObject that the agent should move towards.")]
        [SerializeField] protected SharedVariable<GameObject> m_Target;
        [Tooltip("If target is null then use the target position")]
        [SerializeField] protected SharedVariable<Vector3> m_TargetPosition;

        /// <summary>
        /// Move towards the target.
        /// </summary>
        /// <returns>Success when the agent has arrived at the target.</returns>
        public override TaskStatus OnUpdate()
        {
            var targetPosition = GetTargetDestination();
            // Return a task status of success once we've reached the target
            var direction = targetPosition - transform.position;
            if (m_IgnoreYDirection.Value) {
                direction = Vector3.ProjectOnPlane(direction, transform.up);
            }
            if (Vector3.Magnitude(direction) < m_ArrivedDistance.Value) {
                return TaskStatus.Success;
            }

            // The agent hasn't arrived - keep moving towards the target.
            transform.position = Vector3.MoveTowards(transform.position, transform.position + direction, m_Speed.Value * Time.deltaTime);
            if (m_LookAtTarget.Value && direction.sqrMagnitude > 0.01f) {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), m_MaxLookAtRotationDelta.Value);
            }
            return TaskStatus.Running;
        }

        /// <summary>
        /// Returns the target destination.
        /// </summary>
        /// <returns>The target destination.</returns>
        private Vector3 GetTargetDestination()
        {
            return m_Target.Value != null ? m_Target.Value.transform.position : m_TargetPosition.Value;
        }

        /// <summary>
        /// Resets the task values back to their default.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            m_Speed = 5;
            m_ArrivedDistance = 0.1f;
            m_IgnoreYDirection = true;
            m_LookAtTarget = true;
            m_MaxLookAtRotationDelta = 1;
            m_Target = null;
            m_TargetPosition = Vector3.zero;
        }
    }
}