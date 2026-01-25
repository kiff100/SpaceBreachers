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

    [Opsive.Shared.Utility.Description("Evade the specified target. Evade will predict where the target is headed based on the Look Ahead Distance. " +
                     "Evade is similar to Flee except Evade uses the target's velocity to predict where to move towards whereas Flee only uses the target's position.")]
    [NodeIcon("8e0f6d1ef072a8d468943f37b3453bd9", "cfa59bcf26297d44e8b774d0568c996c")]
    public class Evade : MovementBase
    {
        [Tooltip("The GameObject that the agent is evading from.")]
        [SerializeField] protected SharedVariable<GameObject> m_Target;
        [Tooltip("The agent has evaded when the magnitude is greater than this value")]
        [SerializeField] protected SharedVariable<float> m_EvadeDistance = 10;
        [Tooltip("The distance to look ahead when evading")]
        [SerializeField] protected SharedVariable<float> m_LookAheadDistance = 5;
        [Tooltip("Specifies how far to predict the distance ahead of the target. Lower values indicate that less distance should be predicated.")]
        [SerializeField] protected SharedVariable<float> m_DistancePrediction = 20;
        [Tooltip("Specifies the multiplier for predicting the look ahead distance.")]
        [SerializeField] protected SharedVariable<float> m_DistancePredictionMultiplier = 20;
        [Tooltip("The maximum number of interations that the position should be set")]
        [SerializeField] [Range(0, 100)] protected int m_MaxInterations = 1;

        private Vector3 m_TargetPosition;

        /// <summary>
        /// The task has started.
        /// </summary>
        public override void OnStart()
        {
            base.OnStart();

            if (m_MaxInterations == 0) {
                Debug.LogWarning("Warning: Max iterations must be greater than 0.");
                m_MaxInterations = 1;
            }

            if (m_Target == null) {
                Debug.LogError("Error: A target must be set on the Evade ability.");
                return;
            }

            m_TargetPosition = m_Target.Value.transform.position;
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

            if (Vector3.Magnitude(transform.position - m_Target.Value.transform.position) > m_EvadeDistance.Value) {
                return TaskStatus.Success;
            }

            return SetTargetDestination() ? TaskStatus.Running : TaskStatus.Failure;
        }

        /// <summary>
        /// Sets the target destination.
        /// </summary>
        /// <returns>True if the target destination is valid.</returns>
        private bool SetTargetDestination()
        {
            var predictedDestination = GetPredictedDestination();
            var validDestination = false;
            var interation = 0;
            do {
                var targetDestination = transform.position + (transform.position - predictedDestination).normalized * m_LookAheadDistance.Value * ((m_MaxInterations - interation) / m_MaxInterations);
                validDestination = SetDestination(targetDestination);
            } while (!validDestination && interation < m_MaxInterations - 1);

            return interation < m_MaxInterations;
        }

        /// <summary>
        /// Returns the predicted target destination.
        /// </summary>
        /// <returns>The predicted target destination.</returns>
        private Vector3 GetPredictedDestination()
        {
            // Calculate the current distance to the target and the current speed.
            var distance = (m_Target.Value.transform.position - transform.position).magnitude;
            var velocityMagnitude = m_Pathfinder.Velocity.magnitude;

            var futurePrediction = 0f;
            // Set the future prediction to max prediction if the speed is too small to give an accurate prediction.
            if (velocityMagnitude <= distance / m_DistancePrediction.Value) {
                futurePrediction = m_DistancePrediction.Value;
            } else {
                futurePrediction = (distance / velocityMagnitude) * m_DistancePredictionMultiplier.Value;
            }

            // Predict the future by taking the velocity of the target and multiply it by the future prediction.
            var prevTargetPosition = m_TargetPosition;
            m_TargetPosition = m_Target.Value.transform.position;
            return m_TargetPosition + (m_TargetPosition - prevTargetPosition) * futurePrediction;
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
            m_EvadeDistance = 10;
            m_LookAheadDistance = 5;
            m_DistancePrediction = 20;
            m_DistancePredictionMultiplier = 20;
            m_MaxInterations = 1;
        }
    }
}