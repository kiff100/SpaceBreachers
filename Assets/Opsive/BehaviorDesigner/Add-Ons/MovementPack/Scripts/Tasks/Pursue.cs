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

    [Opsive.Shared.Utility.Description("Pursues the specified target. Pursue will predict where the target is headed based on the Target Distance Prediction value.")]
    [NodeIcon("079b135d5d495e14abd9ad2cb0dfaf9e", "c0515b66e6314734ba5a8f2ec7d6f451")]
    public class Pursue : MovementBase
    {
        [Tooltip("The GameObject that the agent is seeking.")]
        [SerializeField] protected SharedVariable<GameObject> m_Target;
        [Tooltip("Specifies how far to predict the distance ahead of the target. Lower values indicate that less distance should be predicated.")]
        [SerializeField] protected SharedVariable<float> m_DistancePrediction = 20;
        [Tooltip("Specifies the multiplier for predicting the look ahead distance.")]
        [SerializeField] protected SharedVariable<float> m_DistancePredictionMultiplier = 20;

        private Vector3 m_TargetPosition;

        /// <summary>
        /// The task has started.
        /// </summary>
        public override void OnStart()
        {
            base.OnStart();

            if (m_Target == null) {
                Debug.LogError("Error: A target must be set on the Pursue ability.");
                return;
            }

            m_TargetPosition = m_Target.Value.transform.position;
            SetDestination(GetTargetDestination());
        }

        /// <summary>
        /// Returns the target destination.
        /// </summary>
        /// <returns>The target destination.</returns>
        private Vector3 GetTargetDestination()
        {
            // Calculate the current distance to the target and the current speed.
            var distance = (m_Target.Value.transform.position - transform.position).magnitude;
            var velocityMagnitude = m_Pathfinder.Velocity.magnitude;

            var futurePrediction = 0f;
            // Set the future prediction to the max prediction if the speed is too small to give an accurate prediction.
            if (velocityMagnitude <= distance / m_DistancePrediction.Value) {
                futurePrediction = m_DistancePrediction.Value;
            } else {
                futurePrediction = (distance / velocityMagnitude) * m_DistancePredictionMultiplier.Value;
            }

            // Predict the future by taking the velocity of the target and multiply it by the future prediction.
            var lastTargetPosition = m_TargetPosition;
            m_TargetPosition = m_Target.Value.transform.position;
            return m_TargetPosition + (m_TargetPosition - lastTargetPosition) * futurePrediction;
        }

        /// <summary>
        /// Updates the destination.
        /// </summary>
        /// <returns>Success when the agent arrives.</returns>
        public override TaskStatus OnUpdate()
        {
            if (m_Target.Value == null) {
                return TaskStatus.Failure;
            }

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
            m_DistancePrediction = 20;
            m_DistancePredictionMultiplier = 20;
        }
    }
}