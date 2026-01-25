/// ---------------------------------------------
/// Senses Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.SensesPack.Demo
{
    using UnityEngine;

    /// <summary>
    /// Manages multiple light sources to create dynamic lighting scenarios for demonstrating the luminance sensor.
    /// </summary>
    public class LuminanceScenario : MonoBehaviour
    {
        [Tooltip("Array of directional lights for thunderstorm effect")]
        [SerializeField] protected Light[] m_DirectionalLights;
        [Tooltip("Array of spotlights that move in circular patterns")]
        [SerializeField] protected Light[] m_Spotlights;
        [Tooltip("Array of point lights that flicker")]
        [SerializeField] protected Light[] m_PointLights;
        
        [Header("Ambient Light")]
        [Tooltip("Minimum global ambient light intensity.")]
        [SerializeField] protected float m_MinAmbientIntensity = 0.1f;
        [Tooltip("Maximum global ambient light intensity.")]
        [SerializeField] protected float m_MaxAmbientIntensity = 1f;
        [Tooltip("Speed at which ambient light changes.")]
        [SerializeField] protected float m_AmbientChangeSpeed = 0.5f;
        
        [Header("Directional Light")]
        [Tooltip("Minimum intensity during thunderstorm.")]
        [SerializeField] protected float m_MinDirectionalIntensity = 0.2f;
        [Tooltip("Maximum intensity during thunderstorm.")]
        [SerializeField] protected float m_MaxDirectionalIntensity = 1f;
        [Tooltip("How long each thunderstorm lasts.")]
        [SerializeField] protected float m_ThunderstormDuration = 2f;
        [Tooltip("Time between thunderstorms.")]
        [SerializeField] protected float m_ThunderstormCooldown = 5f;
        
        [Header("Spotlight")]
        [Tooltip("Speed of spotlight movement.")]
        [SerializeField] protected float m_SpotlightMoveSpeed = 1f;
        [Tooltip("Radius of spotlight circular movement.")]
        [SerializeField] protected float m_SpotlightMoveRadius = 5f;
        
        [Header("Point Light")]
        [Tooltip("Minimum intensity for point light flicker.")]
        [SerializeField] protected float m_MinPointIntensity = 0.5f;
        [Tooltip("Maximum intensity for point light flicker.")]
        [SerializeField] protected float m_MaxPointIntensity = 2f;
        [Tooltip("Speed of point light flicker.")]
        [SerializeField] protected float m_FlickerSpeed = 10f;

        private float m_InitialAmbientIntensity;
        private float[] m_InitialDirectionalIntensities;
        private float m_ThunderstormTimer;
        private float m_AmbientTargetIntensity;
        private Vector3[] m_SpotlightStartPositions;
        private float[] m_SpotlightAngles;
        private float[] m_PointLightTargetIntensities;

        /// <summary>
        /// Initializes the component and sets up initial states for all light types.
        /// </summary>
        private void Awake()
        {
            m_InitialAmbientIntensity = RenderSettings.ambientIntensity;
            m_AmbientTargetIntensity = m_InitialAmbientIntensity;
            
            if (m_DirectionalLights != null && m_DirectionalLights.Length > 0) {
                m_InitialDirectionalIntensities = new float[m_DirectionalLights.Length];
                for (int i = 0; i < m_DirectionalLights.Length; ++i) {
                    if (m_DirectionalLights[i] != null) {
                        m_InitialDirectionalIntensities[i] = m_DirectionalLights[i].intensity;
                    }
                }
            }
            
            // Spotlight.
            if (m_Spotlights != null && m_Spotlights.Length > 0) {
                m_SpotlightStartPositions = new Vector3[m_Spotlights.Length];
                m_SpotlightAngles = new float[m_Spotlights.Length];
                for (int i = 0; i < m_Spotlights.Length; ++i) {
                    if (m_Spotlights[i] != null) {
                        m_SpotlightStartPositions[i] = m_Spotlights[i].transform.position;
                        m_SpotlightAngles[i] = Random.Range(0f, 360f);
                    }
                }
            }
            
            // Point light.
            if (m_PointLights != null && m_PointLights.Length > 0) {
                m_PointLightTargetIntensities = new float[m_PointLights.Length];
                for (int i = 0; i < m_PointLights.Length; ++i) {
                    if (m_PointLights[i] != null) {
                        // Store initial intensity for each point light
                        m_PointLightTargetIntensities[i] = m_PointLights[i].intensity;
                    }
                }
            }
        }

        /// <summary>
        /// Updates all light effects.
        /// </summary>
        private void Update()
        {
            UpdateAmbientLight();
            UpdateThunderstorm();
            UpdateSpotlights();
            UpdatePointLights();
        }
        
        /// <summary>
        /// Smoothly changes the global ambient light intensity between min and max values.
        /// </summary>
        private void UpdateAmbientLight()
        {
            var currentIntensity = RenderSettings.ambientIntensity;
            var newIntensity = Mathf.MoveTowards(currentIntensity, m_AmbientTargetIntensity, m_AmbientChangeSpeed * Time.deltaTime);
            RenderSettings.ambientIntensity = newIntensity;
            
            if (Mathf.Approximately(currentIntensity, m_AmbientTargetIntensity)) {
                m_AmbientTargetIntensity = Random.Range(m_MinAmbientIntensity, m_MaxAmbientIntensity);
            }
        }
        
        /// <summary>
        /// Simulates a thunderstorm effect by varying directional light intensities.
        /// </summary>
        private void UpdateThunderstorm()
        {
            if (m_DirectionalLights == null || m_DirectionalLights.Length == 0) return;
            
            m_ThunderstormTimer -= Time.deltaTime;
            
            if (m_ThunderstormTimer <= 0 || m_ThunderstormTimer > m_ThunderstormDuration) {
                if (m_ThunderstormTimer <= 0) {
                    // Start new thunderstorm with random duration and cooldown.
                    var duration = Random.Range(m_ThunderstormDuration * 0.5f, m_ThunderstormDuration);
                    var cooldown = Random.Range(m_ThunderstormCooldown * 0.5f, m_ThunderstormCooldown);
                    m_ThunderstormTimer = duration + cooldown;
                }

                // Default to minimum intensity.
                for (int i = 0; i < m_DirectionalLights.Length; ++i) {
                    if (m_DirectionalLights[i] != null) {
                        m_DirectionalLights[i].intensity = m_MinDirectionalIntensity;
                    }
                }
            } else {
                // Add random thunderstorm variations.
                for (int i = 0; i < m_DirectionalLights.Length; ++i) {
                    if (m_DirectionalLights[i] != null) {
                        m_DirectionalLights[i].intensity = Random.Range(m_MinDirectionalIntensity, m_MaxDirectionalIntensity);
                    }
                }
            }
        }
        
        /// <summary>
        /// Moves each spotlight in a circular pattern around its starting position.
        /// </summary>
        private void UpdateSpotlights()
        {
            if (m_Spotlights == null || m_Spotlights.Length == 0) return;
            
            // Move the spotlights in a circle.
            for (int i = 0; i < m_Spotlights.Length; ++i) {
                if (m_Spotlights[i] == null) continue;
                
                m_SpotlightAngles[i] += m_SpotlightMoveSpeed * Time.deltaTime;
                
                var x = Mathf.Cos(m_SpotlightAngles[i]) * m_SpotlightMoveRadius;
                var z = Mathf.Sin(m_SpotlightAngles[i]) * m_SpotlightMoveRadius;
                m_Spotlights[i].transform.position = m_SpotlightStartPositions[i] + new Vector3(x, 0, z);
            }
        }
        
        /// <summary>
        /// Creates a flickering effect for each point light by smoothly varying its intensity.
        /// </summary>
        private void UpdatePointLights()
        {
            if (m_PointLights == null || m_PointLights.Length == 0) return;
            
            for (int i = 0; i < m_PointLights.Length; ++i) {
                if (m_PointLights[i] == null) {
                    continue;
                }
                
                var currentIntensity = m_PointLights[i].intensity;
                var newIntensity = Mathf.MoveTowards(currentIntensity, m_PointLightTargetIntensities[i], m_FlickerSpeed * Time.deltaTime);
                m_PointLights[i].intensity = newIntensity;
                
                if (Mathf.Approximately(currentIntensity, m_PointLightTargetIntensities[i])) {
                    m_PointLightTargetIntensities[i] = Random.Range(m_MinPointIntensity, m_MaxPointIntensity);
                }
            }
        }

        /// <summary>
        /// The component has been disabled.
        /// </summary>
        private void OnDisable()
        {
            // Reset the ambient and directional lights.
            RenderSettings.ambientIntensity = m_InitialAmbientIntensity;

            if (m_DirectionalLights != null && m_DirectionalLights.Length > 0) {
                for (int i = 0; i < m_DirectionalLights.Length; ++i) {
                    if (m_DirectionalLights[i] != null) {
                        m_DirectionalLights[i].intensity = m_InitialDirectionalIntensities[i];
                    }
                }
            }
        }
    }
}