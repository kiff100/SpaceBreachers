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

    [Opsive.Shared.Utility.Description("A conditional task that checks if a GameObject-based sensor can detect any objects.  If the compare object is null then the task will return success if any object is returned.")]
    [NodeIcon("0820cfaadd7604d4cb2e2f81cfba0e93", "20c9b7fefb0b522479aa53fb97e6b730")]
    [Shared.Utility.Category("Senses Pack")]
    public class CanDetectObject : Conditional
    {
        [Tooltip("The sense that should be detected.")]
        [SerializeField] protected IGameObjectSensor m_Sensor;
        [Tooltip("The GameObject to compare against.")]
        [SerializeField] protected SharedVariable<GameObject> m_CompareObject;
        [Tooltip("The found GameObject.")]
        [SerializeField][RequireShared] protected SharedVariable<GameObject> m_ReturnedObject;

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
        /// Updates the task by checking if the sensor can detect any objects.
        /// </summary>
        /// <returns>Success if an object was detected.</returns>
        public override TaskStatus OnUpdate()
        {
            if (m_Sensor == null) {
                return TaskStatus.Failure;
            }

            var detectedObject = m_Sensor.GetDetectedObject();
            if (m_ReturnedObject.IsShared) {
                m_ReturnedObject.Value = detectedObject;
            }
            if (m_CompareObject.Value != null) {
                return detectedObject == m_CompareObject.Value ? TaskStatus.Success : TaskStatus.Failure;
            }
            return detectedObject != null ? TaskStatus.Success : TaskStatus.Failure;
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