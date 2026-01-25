/// ---------------------------------------------
/// Senses Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Sensors
{
    using UnityEngine;
    using Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Emitters;

    /// <summary>
    /// A sensor that detects the type of surface that the agent is in contact with.
    /// </summary>
    public class Surface : Sensor
    {
        [Tooltip("Specifies the type of detection that should be used.")]
        [SerializeField] [CastDetectionModeList] protected CastDetectionMode[] m_DetectionModes;

        /// <summary>
        /// Initializes the surface sensor with the specified GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject that the sensor is attached to.</param>
        public override void Initialize(GameObject gameObject)
        {
            base.Initialize(gameObject);

            for (int i = 0; i < m_DetectionModes.Length; ++i) {
                m_DetectionModes[i].Initialize();
            }
        }

        /// <summary>
        /// Gets the type of surface that was detected by the sensor.
        /// </summary>
        /// <returns>The detected surface type, or null if no surface was detected.</returns>
        public SurfaceType GetDetectedSurface()
        {
            SurfaceType detectedSurface = null;
            for (int i = 0; i < m_DetectionModes.Length; ++i) {
                m_DetectionModes[i].DetectObjects(m_GameObject.transform.position, (GameObject filterObject) =>
                {
                    var surfaceType = SurfaceManager.GetSurfaceType(filterObject);
                    if (surfaceType != null) {
                        detectedSurface = surfaceType;
                        return true;
                    }
                    return false;
                });
            }
            return detectedSurface;
        }

        /// <summary>
        /// Draws gizmos to visualize the surface sensor's detection area.
        /// </summary>
        /// <param name="transform">The transform of the agent that the sensor is attached to.</param>
        public override void OnDrawGizmos(Transform transform)
        {
            if (m_DetectionModes == null) {
                return;
            }

            for (int i = 0; i < m_DetectionModes.Length; ++i) {
                if (m_DetectionModes[i] == null) {
                    continue;
                }
                m_DetectionModes[i].OnDrawGizmos(transform.position);
            }
        }
    }
}