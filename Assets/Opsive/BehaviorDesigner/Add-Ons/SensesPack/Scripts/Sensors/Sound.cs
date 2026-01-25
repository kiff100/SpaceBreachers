/// ---------------------------------------------
/// Senses Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Sensors
{
    using Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Utility;
    using Opsive.GraphDesigner.Runtime.Variables;
    using UnityEngine;

    /// <summary>
    /// A sensor that detects sound sources within its range. Can be used for hearing-based detection.
    /// </summary>
    public class Sound : Sensor, IGameObjectSensor, IFloatSensor
    {
        [Tooltip("Specifies the type of detection that should be used.")]
        [SerializeField] [DetectionModeList] protected DetectionMode[] m_DetectionModes = new DetectionMode[] { new ObjectDetectionMode() };
        [Tooltip("Is the environment in 2D?")]
        [SerializeField] protected bool m_Use2DPhysics;
        [Tooltip("Specifies the distance that the agent can hear.")]
        [SerializeField] protected SharedVariable<float> m_Radius = 50;
        [Tooltip("The further away a sound source is the less likely the agent will be able to hear it. " +
                 "Set a threshold for the the minimum audibility level that the agent can hear.")]
        [SerializeField] protected SharedVariable<float> m_AudibilityThreshold = 0.05f;
        [Tooltip("The hearing offset relative to the pivot position.")]
        [SerializeField] protected SharedVariable<Vector3> m_PivotOffset;

        /// <summary>
        /// Initializes the sound sensor with the specified GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject that the sensor is attached to.</param>
        public override void Initialize(GameObject gameObject)
        {
            base.Initialize(gameObject);

            if (m_DetectionModes == null) {
                return;
            }

            for (int i = 0; i < m_DetectionModes.Length; ++i) {
                m_DetectionModes[i].Initialize(m_Use2DPhysics);
            }
        }

        /// <summary>
        /// Returns the GameObject that was detected by the sound sensor.
        /// </summary>
        /// <returns>The detected GameObject (can be null).</returns>
        public GameObject GetDetectedObject()
        {
            return GetDetectedSound().Item1;
        }

        /// <summary>
        /// Returns the sound level of the detected object.
        /// </summary>
        /// <returns>The sound level of the detected object (returns float.MinValue if no object was detected).</returns>
        public float GetDetectedAmount()
        {
            return GetDetectedSound().Item2;
        }

        /// <summary>
        /// Returns the detected object and its sound level.
        /// </summary>
        /// <returns>A tuple containing the detected GameObject and its sound level.</returns>
        private (GameObject, float) GetDetectedSound()
        {
            if (m_DetectionModes == null || m_DetectionModes.Length == 0) {
                return (null, float.MinValue);
            }

            GameObject detectedObject = null;
            var loudestAudioLevel = float.MinValue;
            for (int i = 0; i < m_DetectionModes.Length; ++i) {
                m_DetectionModes[i].DetectObjects(m_GameObject.transform.position, m_Radius.Value, (GameObject target) =>
                {
                    var targetSoundLevel = GetSoundLevel(target);
                    if (targetSoundLevel > loudestAudioLevel) {
                        detectedObject = target;
                        loudestAudioLevel = targetSoundLevel;
                    }
                    return true;
                });
            }
            return (detectedObject, loudestAudioLevel);
        }

        /// <summary>
        /// Calculates the sound level of a target object based on its AudioSource components.
        /// </summary>
        /// <param name="target">The target object to check for sound.</param>
        /// <returns>The sound level of the target object (float.MinValue if no sound is detected).</returns>
        public float GetSoundLevel(GameObject target)
        {
            if (target == null) {
                return float.MinValue;
            }

            AudioSource[] colliderAudioSource;
            // Check to see if the hit agent has an audio source and that audio source is playing
            if ((colliderAudioSource = CacheUtility.GetCachedComponentsInChildren<AudioSource>(target)) != null) {
                for (int i = 0; i < colliderAudioSource.Length; ++i) {
                    if (colliderAudioSource[i].isPlaying) {
                        var distance = Vector3.Distance(m_Transform.TransformPoint(m_PivotOffset.Value), colliderAudioSource[i].transform.position);
                        if (distance <= colliderAudioSource[i].maxDistance) {
                            if (colliderAudioSource[i].rolloffMode == AudioRolloffMode.Logarithmic) {
                                // Unity's logarithmic rolloff follows the inverse square law.
                                if (distance <= colliderAudioSource[i].minDistance) {
                                    return colliderAudioSource[i].volume;
                                } else {
                                    var volume = colliderAudioSource[i].volume * (colliderAudioSource[i].minDistance * colliderAudioSource[i].minDistance) / (distance * distance);
                                    return volume;
                                }
                            } else { // Linear.
                                return colliderAudioSource[i].volume * Mathf.Clamp01((distance - colliderAudioSource[i].minDistance) / (colliderAudioSource[i].maxDistance - colliderAudioSource[i].minDistance));
                            }
                        }
                    }
                }
            }
            return float.MinValue;
        }

        /// <summary>
        /// Draws gizmos to visualize the sound sensor's detection area.
        /// </summary>
        /// <param name="transform">The transform of the agent that the sensor is attached to.</param>
        public override void OnDrawGizmos(Transform transform)
        {
#if UNITY_EDITOR
            var originalColor = Gizmos.color;
            Gizmos.color = Editor.BehaviorDesignerSettings.Instance.DefaultGizmosColor;
            var position = transform.TransformPoint(m_PivotOffset.Value);
            Gizmos.DrawWireSphere(position, m_Radius.Value);
            Gizmos.color = originalColor;
#endif
        }
    }
}