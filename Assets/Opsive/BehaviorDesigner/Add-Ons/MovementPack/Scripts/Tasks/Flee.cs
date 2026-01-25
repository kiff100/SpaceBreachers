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

    [Opsive.Shared.Utility.Description("Flee from the target specified. Flee will predict where the target is headed based on the Look Ahead Distance.")]
    [NodeIcon("1ec2f15c8d735df4db1b25bd175908d5", "c3bcfb3ed7a811f42bd18b1316442621")]
    public class Flee : MovementBase
    {
        [Tooltip("The GameObject that the agent is fleeing from.")]
        [SerializeField] protected SharedVariable<GameObject> m_Target;
        [Tooltip("The agent has fleed when the magnitude is greater than this value.")]
        [SerializeField] protected SharedVariable<float> m_FleedDistance = 20;
        [Tooltip("The distance to look ahead when fleeing.")]
        [SerializeField] protected SharedVariable<float> m_LookAheadDistance = 5;

        private bool m_HasMoved;

        /// <summary>
        /// The task has started.
        /// </summary>
        public override void OnStart()
        {
            base.OnStart();

            if (m_Target == null) {
                Debug.LogError("Error: A target must be set on the Flee ability.");
                return;
            }

            m_HasMoved = false;
            SetDestination(GetTargetDestination());
        }

        /// <summary>
        /// Returns the target destination.
        /// </summary>
        /// <returns>The target destination.</returns>
        private Vector3 GetTargetDestination()
        {
            return transform.position + (transform.position - m_Target.Value.transform.position).normalized * m_LookAheadDistance.Value;
        }

        /// <summary>
        /// Updates the flee destination.
        /// </summary>
        /// <returns>Success when the agent has fleed.</returns>
        public override TaskStatus OnUpdate()
        {
            if (m_Target.Value == null) {
                return TaskStatus.Failure;
            }

            if (Vector3.Magnitude(transform.position - m_Target.Value.transform.position) > m_FleedDistance.Value) {
                return TaskStatus.Success;
            }

            if (HasArrived()) {
                if (!m_HasMoved) {
                    return TaskStatus.Failure;
                }
                if (!SetDestination(GetTargetDestination())) {
                    return TaskStatus.Failure;
                }
                m_HasMoved = false;
            } else {
                // If the agent is stuck the task shouldn't indefinitely return a status of running.
                var velocityMagnitude = m_Pathfinder.Velocity.sqrMagnitude;
                if (m_HasMoved && velocityMagnitude <= 0f) {
                    return TaskStatus.Failure;
                }
                m_HasMoved = velocityMagnitude > 0f;
            }

            return TaskStatus.Running;
        }

        /// <summary>
        /// Sets the destination. Returns false if the destination isn't valid.
        /// </summary>
        /// <param name="destination">The target destination.</param>
        /// <returns>True if the destination was successfully set.</returns>
        protected override bool SetDestination(Vector3 destination)
        {
            if (!SamplePosition(ref destination)) {
                return false;
            }
            return base.SetDestination(destination);
        }

        /// <summary>
        /// Resets the task values back to their default.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            m_Target = null;
            m_FleedDistance = 20;
            m_LookAheadDistance = 5;
        }
    }
}