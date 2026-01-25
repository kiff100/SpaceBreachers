/// ---------------------------------------------
/// Movement Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.MovementPack.Runtime.Tasks
{
    using Opsive.BehaviorDesigner.Runtime.Tasks;
    using Opsive.GraphDesigner.Runtime;
    using Opsive.GraphDesigner.Runtime.Variables;
    using UnityEngine;

    [Opsive.Shared.Utility.Description("Moves towards the target destination. Uses the Target GameObject is set, otherwise the Target Position.")]
    [NodeIcon("9cafaa822d368954fb8a6c43fa7d8206", "b217f83765c09674790bf63866775d22")]
    public class Seek : MovementBase
    {
        [Tooltip("The GameObject that the agent is seeking.")]
        [SerializeField] protected SharedVariable<GameObject> m_Target;
        [Tooltip("If destination is null then use the target position.")]
        [SerializeField] protected SharedVariable<Vector3> m_TargetPosition;

        /// <summary>
        /// The task has started.
        /// </summary>
        public override void OnStart()
        {
            base.OnStart();

            SetDestination(GetTargetDestination());
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
        /// Updates the destination.
        /// </summary>
        /// <returns>Success when the agent arrives.</returns>
        public override TaskStatus OnUpdate()
        {
            if (HasArrived()) {
                return TaskStatus.Success;
            }

            SetDestination(GetTargetDestination());

            return TaskStatus.Running;
        }

        /// <summary>
        /// Resets the task values back to their default.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            m_Target = null;
            m_TargetPosition = Vector3.zero;
        }
    }
}