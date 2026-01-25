/// ---------------------------------------------
/// Senses Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Emitters
{
    using System.IO.Pipes;
    using UnityEngine;

    /// <summary>
    /// A component that emits luminance (light intensity) information for use in behavior trees.
    /// This component must be attached to a GameObject that has a Light component.
    /// </summary>
    public class LuminanceEmitter : MonoBehaviour
    {
        private Transform m_Transform;
        private Light m_Light;

        /// <summary>
        /// Initializes the component by getting required references and validating setup.
        /// </summary>
        private void Awake()
        {
            m_Light = GetComponent<Light>();
            if (m_Light == null) {
                Debug.LogWarning("Warning: The LuminanceEmitter component doesn't have a light on the same GameObject. It will be disabled.", this);
                enabled = false;
            }
            m_Transform = transform;
        }

        /// <summary>
        /// Registers this emitter with the LuminanceManager when enabled.
        /// </summary>
        public void OnEnable()
        {
            LuminanceManager.Instance.Register(this);
        }

        /// <summary>
        /// Calculates the luminance (light intensity) that this light source provides to a target object.
        /// Takes into account the light type, distance, angle, and any obstacles between the light and target.
        /// </summary>
        /// <param name="target">The GameObject to calculate luminance for.</param>
        /// <returns>The calculated luminance value (0-1) that the target receives from this light source.</returns>
        public float GetLuminance(GameObject target)
        {
            // Return 0 if the light is off, disabled, or blocked from the target.
            if (m_Light.intensity == 0 || !m_Light.isActiveAndEnabled || !IsLightVisible(target)) {
                return 0;
            }

            var targetPosition = target.transform.position;
            float luminance = 0f;
            // Calculate luminance based on the type of light
            switch (m_Light.type) {
                case LightType.Directional: {
                        // Directional lights provide constant intensity regardless of distance
                        luminance = m_Light.intensity;
                        break;
                    }
                case LightType.Point: {
                        // Point lights attenuate based on distance from the source.
                        var distance = Vector3.Distance(targetPosition, m_Transform.position);
                        if (distance > m_Light.range) {
                            return 0;
                        }
                        var direction = (targetPosition - m_Transform.position).normalized;
                        var attenuation = 1f / (1f + distance * distance);
                        luminance = m_Light.intensity * attenuation;
                        break;
                    }
                case LightType.Spot: {
                        // Spot lights have both distance attenuation and angle-based falloff.
                        var distance = Vector3.Distance(targetPosition, m_Transform.position);
                        if (distance > m_Light.range) {
                            return 0;
                        }
                        var direction = (targetPosition - m_Transform.position).normalized;
                        var angle = Vector3.Angle(direction, m_Transform.forward);
                        // Return 0 if the target is outside of the max spotlight angle.
                        if (angle > m_Light.spotAngle * 0.5f) {
                            return 0;
                        }

                        var angleFactor = Mathf.Max(Vector3.Dot(direction, m_Transform.forward), 0f);
                        if (angleFactor == 0) {
                            return 0;
                        }

                        // Calculate smooth falloff between inner and outer spot angles
                        var angleRatio = 1f - (angle > m_Light.innerSpotAngle ? (angle - m_Light.innerSpotAngle) / m_Light.spotAngle : 0f);
                        var attenuation = 1f / (1f + distance * distance);
                        luminance = m_Light.intensity * attenuation * angleFactor * angleRatio;
                        break;
                    }
#if UNITY_6000_0_OR_NEWER
                case LightType.Rectangle:
#else
                case LightType.Area:
#endif
                case LightType.Disc: {
                        // Area and disc lights provide a simplified approximation of area lighting.
                        var distance = Vector3.Distance(targetPosition, m_Transform.position);
                        if (distance > m_Light.range) {
                            return 0;
                        }
                        var direction = (targetPosition - m_Transform.position).normalized;
                        var attenuation = 1f / (1f + distance * distance);
                        var angleFactor = Mathf.Max(Vector3.Dot(direction, m_Transform.forward), 0f);
                        luminance = m_Light.intensity * attenuation * angleFactor;
                        break;
                    }
            }
            return luminance;
        }

        /// <summary>
        /// Checks if there are any obstacles between the light source and the target object.
        /// </summary>
        /// <param name="target">The GameObject to check visibility for.</param>
        /// <returns>True if the light can reach the target without being blocked.</returns>
        private bool IsLightVisible(GameObject target)
        {
            var targetPosition = target.transform.position;
            // For directional lights, cast from a far distance in the opposite direction
            Vector3 lightPosition = (m_Light.type == LightType.Directional) ? targetPosition - m_Transform.forward * 1000f : m_Transform.position;
            Vector3 direction = (targetPosition - lightPosition).normalized;
            float distance = Vector3.Distance(lightPosition, targetPosition);

            // Check if any objects block the light path, ignoring the target and its children
            if (Physics.Raycast(lightPosition, direction, out RaycastHit hit, distance) && !hit.transform.IsChildOf(target.transform)) {
                return false; // Light is blocked by an object
            }
            return true;
        }

        /// <summary>
        /// Unregisters this emitter from the LuminanceManager when disabled.
        /// </summary>
        public void OnDisable()
        {
            LuminanceManager.Unregister(this);
        }
    }
}