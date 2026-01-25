/// ---------------------------------------------
/// Movement Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Tasks
{
    using Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Emitters;
    using Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Sensors;
    using Opsive.BehaviorDesigner.Runtime.Tasks;
    using Opsive.BehaviorDesigner.Runtime.Tasks.Conditionals;
    using Opsive.GraphDesigner.Runtime;
    using Opsive.GraphDesigner.Runtime.Variables;
    using UnityEngine;

    [Opsive.Shared.Utility.Description("A conditional task that checks if a surface sensor can detect the specified surface. If the compare surface is null then the task will return success if any object is returned.")]
    [NodeIcon("217fc2cb217aef14dafb591f2c5ea979", "5e890eb5f6035e149b3d475dd489a297")]
    [Shared.Utility.Category("Senses Pack")]
    public class CanDetectSurface : Conditional
    {
        [Tooltip("The sense that should be detected.")]
        [SerializeField] protected Surface m_Sensor;
        [Tooltip("The desired surface type.")]
        [SerializeField] protected SharedVariable<SurfaceType> m_CompareSurfaceType;
        [Tooltip("The detected surface type.")]
        [SerializeField] [RequireShared] protected SharedVariable<SurfaceType> m_ReturnedObject;

        /// <summary>
        /// Initializes the default values.
        /// </summary>
        public override void OnAwake()
        {
            base.OnAwake();

            if (m_Sensor == null) {
                Debug.LogError("Error: A sensor must be specified.");
                return;
            }

            m_Sensor.Initialize(m_GameObject);
        }

        /// <summary>
        /// Updates the task by checking if the sensor can detect the desired surface.
        /// </summary>
        /// <returns>Success if the desired surface was detected.</returns>
        public override TaskStatus OnUpdate()
        {
            if (m_Sensor == null) {
                return TaskStatus.Failure;
            }

            var detectedObject = m_Sensor.GetDetectedSurface();
            if (m_ReturnedObject.IsShared) {
                m_ReturnedObject.Value = detectedObject;
            }
            if (m_CompareSurfaceType.Value != null) {
                return detectedObject == m_CompareSurfaceType.Value ? TaskStatus.Success : TaskStatus.Failure;
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