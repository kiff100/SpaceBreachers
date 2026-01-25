/// ---------------------------------------------
/// Movement Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Tasks
{
    using Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Sensors;
    using Opsive.BehaviorDesigner.Runtime.Tasks;
    using Opsive.BehaviorDesigner.Runtime.Tasks.Conditionals;
    using Opsive.GraphDesigner.Runtime;
    using Opsive.GraphDesigner.Runtime.Variables;
    using UnityEngine;

    [Opsive.Shared.Utility.Description("A conditional task that checks if a detected amount from a float-based sensor is within a specified range.")]
    [NodeIcon("62cf5eb42708803499769e3858eeed2f", "f0b7471e9adc1bb4b91d015446de30b8")]
    [Shared.Utility.Category("Senses Pack")]
    public class WithinRange : Conditional
    {
        [Tooltip("The sense that should be detected.")]
        [SerializeField] protected IFloatSensor m_Sensor;
        [Tooltip("The minimum value of the sensor.")]
        [SerializeField] protected SharedVariable<float> m_MinimumRange;
        [Tooltip("The maximum value of the sensor.")]
        [SerializeField] protected SharedVariable<float> m_MaximumRange = 1000;
        [Tooltip("The shared variable to store the detected amount.")]
        [SerializeField] [RequireShared] protected SharedVariable<float> m_ReturnedAmount;

        /// <summary>
        /// Initializes the default values.
        /// </summary>
        public override void OnAwake()
        {
            base.OnAwake();

            if (m_Sensor == null || m_Sensor is not Sensor) {
                Debug.LogError("Error: A sensor must be specified.");
                return;
            }

            (m_Sensor as Sensor).Initialize(m_GameObject);
        }

        /// <summary>
        /// Updates the task by checking if the detected amount from the sensor is within the specified range.
        /// </summary>
        /// <returns>Success if the amount is within range.</returns>
        public override TaskStatus OnUpdate()
        {
            if (m_Sensor == null) {
                return TaskStatus.Failure;
            }

            // The detected amount needs to be within the specified range.
            var detectedAmount = m_Sensor.GetDetectedAmount();
            if (detectedAmount <= m_MinimumRange.Value) {
                return TaskStatus.Failure;
            }
            if (detectedAmount > m_MaximumRange.Value) {
                return TaskStatus.Failure;
            }
            if (m_ReturnedAmount.IsShared) {
                m_ReturnedAmount.Value = detectedAmount;
            }
            return TaskStatus.Success;
        }

        /// <summary>
        /// Draws gizmos to visualize the sensor's detection area.
        /// </summary>
        protected override void OnDrawGizmos()
        {
            (m_Sensor as Sensor)?.OnDrawGizmos(gameObject.transform);
        }
    }
}