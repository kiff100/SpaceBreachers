/// ---------------------------------------------
/// Senses Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.SensesPack.Demo
{
    using Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Emitters;
    using UnityEngine;

    /// <summary>
    /// Adjusts the color of renderer materials based on the current scene temperature.
    /// </summary>
    public class TemperatureVisualizer : MonoBehaviour
    {
        [Tooltip("The gradient that defines how colors change based on temperature.")]
        [SerializeField] protected Gradient m_TemperatureGradiant;
        [Tooltip("The minimum temperature value that maps to the start of the gradient.")]
        [SerializeField] protected float m_MinTemperature = 0f;
        [Tooltip("The maximum temperature value that maps to the end of the gradient.")]
        [SerializeField] protected float m_MaxTemperature = 100f;
        [Tooltip("The renderers whose materials will be affected by the temperature.")]
        [SerializeField] protected Renderer[] m_AffectedRenderers;

        /// <summary>
        /// Update the renderer colors based on the current temperature.
        /// </summary>
        private void Update()
        {
            if (SceneTemperature.Instance == null || m_AffectedRenderers == null) {
                return;
            }

            var temperature = SceneTemperature.Instance.Evaluate();
            var normalizedTemperature = Mathf.InverseLerp(m_MinTemperature, m_MaxTemperature, temperature);
            var color = m_TemperatureGradiant.Evaluate(normalizedTemperature);

            for (int i = 0; i < m_AffectedRenderers.Length; ++i) {
                if (m_AffectedRenderers[i] == null) {
                    continue;
                }

                m_AffectedRenderers[i].material.color = color;
            }
        }
    }
}