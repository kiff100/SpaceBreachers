/// ---------------------------------------------
/// Senses Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Sensors
{
    using Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Emitters;
    using Opsive.GraphDesigner.Runtime.Variables;
    using UnityEngine;

    /// <summary>
    /// A sensor that detects the intensity of traces at the agent's position. Can be used for scent-based or trail-based detection.
    /// </summary>
    public class Tracer : Sensor, IFloatSensor
    {
        [Tooltip("The maximum range at which traces can be detected.")]
        [SerializeField] protected SharedVariable<float> m_Range = 2;
        [Tooltip("The hearing offset relative to the pivot position.")]
        [SerializeField] protected SharedVariable<Vector3> m_PivotOffset;

        /// <summary>
        /// Returns the intensity of traces at the agent's current position.
        /// </summary>
        /// <returns>The intensity of traces at the agent's position.</returns>
        public float GetDetectedAmount()
        {
            return TraceManager.Instance.GetIntensityAt(m_GameObject.transform.TransformPoint(m_PivotOffset.Value), m_Range.Value);
        }

        /// <summary>
        /// Draws gizmos to visualize the sensor's detection area or range.
        /// </summary>
        /// <param name="transform">The transform of the agent that the sensor is attached to.</param>
        public override void OnDrawGizmos(Transform transform)
        {
#if UNITY_EDITOR
            var originalColor = Gizmos.color;
            Gizmos.color = Editor.BehaviorDesignerSettings.Instance.DefaultGizmosColor;
            var position = transform.TransformPoint(m_PivotOffset.Value);
            Gizmos.DrawWireSphere(position, m_Range.Value);
            Gizmos.color = originalColor;
#endif
        }
    }
}