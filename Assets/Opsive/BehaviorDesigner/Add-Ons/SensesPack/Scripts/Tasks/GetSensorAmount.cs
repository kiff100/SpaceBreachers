/// ---------------------------------------------
/// Movement Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Tasks
{
    using Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Sensors;
    using Opsive.BehaviorDesigner.Runtime.Tasks;
    using Opsive.BehaviorDesigner.Runtime.Tasks.Actions;
    using Opsive.GraphDesigner.Runtime;
    using Opsive.GraphDesigner.Runtime.Variables;
    using UnityEngine;

    [Opsive.Shared.Utility.Description("Retrieves the detected amount from a float-based sensor and stores it in a shared variable.")]
    [NodeIcon("a80c21e051724af4290aa87b473501fb", "e7f529bf2b8e22f4788e1938f4566a5e")]
    [Shared.Utility.Category("Senses Pack")]
    public class GetSensorAmount : Action
    {
        [Tooltip("The sense that should be detected.")]
        [SerializeField] protected IFloatSensor m_Sensor;
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
        /// Updates the task by getting the detected amount from the sensor and storing it in the shared variable.
        /// </summary>
        /// <returns>Success if the amount was retrieved.</returns>
        public override TaskStatus OnUpdate()
        {
            if (m_Sensor == null) {
                return TaskStatus.Failure;
            }

            var detectedAmount = m_Sensor.GetDetectedAmount();
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