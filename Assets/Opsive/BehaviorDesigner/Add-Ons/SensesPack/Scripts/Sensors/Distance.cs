/// ---------------------------------------------
/// Senses Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Sensors
{
    using Opsive.GraphDesigner.Runtime.Variables;
    using UnityEngine;

    /// <summary>
    /// A sensor that detects objects within a specified distance. Can be used for proximity-based detection.
    /// </summary>
    public class Distance : Sensor, IGameObjectSensor, IFloatSensor
    {
        [Tooltip("Specifies the type of detection that should be used.")]
        [SerializeField] [DetectionModeList] protected DetectionMode[] m_DetectionModes = new DetectionMode[] { new ObjectDetectionMode() };
        [Tooltip("Is the environment in 2D?")]
        [SerializeField] protected bool m_Use2DPhysics;
        [Tooltip("The distance that the object needs to be within.")]
        [SerializeField] protected SharedVariable<float> m_Distance = 5;
        [Tooltip("If true, the object must be within line of sight to be within distance. For example, if this option is enabled then an object behind a wall will not be within distance even though it may " +
                 "be physically close to the other object.")]
        [SerializeField] protected SharedVariable<bool> m_LineOfSight;
        [Tooltip("The LayerMask of the objects to ignore when performing the line of sight check.")]
        [SerializeField] protected SharedVariable<LayerMask> m_IgnoreLayerMask = (LayerMask)(1 << LayerMask.NameToLayer("Ignore Raycast"));
        [Tooltip("The raycast offset relative to the pivot position.")]
        [SerializeField] protected SharedVariable<Vector3> m_PivotOffset;
        [Tooltip("The target raycast offset relative to the pivot position")]
        [SerializeField] protected SharedVariable<Vector3> m_TargetOffset;
        [Tooltip("Should a debug look ray be drawn to the scene view?")]
        [SerializeField] protected SharedVariable<bool> m_DrawDebugRay;

        /// <summary>
        /// Initializes the distance sensor with the specified GameObject.
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
        /// Returns the GameObject that was detected by the distance sensor.
        /// </summary>
        /// <returns>The detected GameObject (can be null).</returns>
        public GameObject GetDetectedObject()
        {
            return GetDetectedDistance().Item1;
        }

        /// <summary>
        /// Returns the distance to the detected object.
        /// </summary>
        /// <returns>The distance to the detected object (returns float.MaxValue if no object was detected).</returns>
        public float GetDetectedAmount()
        {
            return GetDetectedDistance().Item2;
        }

        /// <summary>
        /// Gets the detected object and its distance.
        /// </summary>
        /// <returns>A tuple containing the detected GameObject and its distance.</returns>
        private (GameObject, float) GetDetectedDistance()
        {
            if (m_DetectionModes == null || m_DetectionModes.Length == 0) {
                return (null, float.MaxValue);
            }

            GameObject detectedObject = null;
            var closestDistance = float.MaxValue;
            for (int i = 0; i < m_DetectionModes.Length; ++i) {
                m_DetectionModes[i].DetectObjects(m_GameObject.transform.position, m_Distance.Value, (GameObject target) =>
                {
                    var targetDistance = GetDistance(target);
                    if (targetDistance < closestDistance) {
                        detectedObject = target;
                        closestDistance = targetDistance;
                    }
                    return true;
                });
            }
            return (detectedObject, closestDistance);
        }

        /// <summary>
        /// Calculates the distance to a target object, taking into account line of sight if enabled.
        /// </summary>
        /// <param name="target">The target object to check distance to.</param>
        /// <returns>The distance to the target object, or float.MaxValue if the target is not within range or line of sight.</returns>
        private float GetDistance(GameObject target)
        {
            if (target == null) {
                return float.MaxValue;
            }

            var position = m_Transform.TransformPoint(m_PivotOffset.Value);
            var targetDistance = (target.transform.position - position).magnitude;
            if (targetDistance < m_Distance.Value) {
                if (m_LineOfSight.Value) {
                    if ((m_Use2DPhysics && Physics2D.Linecast(position, target.transform.TransformPoint(m_TargetOffset.Value), ~m_IgnoreLayerMask.Value).transform != null) ||
                        (!m_Use2DPhysics && Physics.Linecast(position, target.transform.TransformPoint(m_TargetOffset.Value), ~m_IgnoreLayerMask.Value, QueryTriggerInteraction.Ignore))) {
                        return targetDistance;
                    }
                } else {
                    return targetDistance;
                }
            }
            return float.MaxValue;
        }

        /// <summary>
        /// Draws gizmos to visualize the distance sensor's detection area.
        /// </summary>
        /// <param name="transform">The transform of the agent that the sensor is attached to.</param>
        public override void OnDrawGizmos(Transform transform)
        {
#if UNITY_EDITOR
            var originalColor = Gizmos.color;
            Gizmos.color = Editor.BehaviorDesignerSettings.Instance.DefaultGizmosColor;
            var position = transform.TransformPoint(m_PivotOffset.Value);
            Gizmos.DrawWireSphere(position, m_Distance.Value);
            Gizmos.color = originalColor;
#endif
        }
    }
}