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
    /// A sensor that detects objects within line of sight and field of view. Can be used for vision-based detection.
    /// </summary>
    public class Visibility : Sensor, IGameObjectSensor, IFloatSensor
    {
        [Tooltip("Specifies the type of detection that should be used.")]
        [SerializeField] [DetectionModeList] protected DetectionMode[] m_DetectionModes = new DetectionMode[] { new ObjectDetectionMode() };
        [Tooltip("Is the environment in 2D?")]
        [SerializeField] protected bool m_Use2DPhysics;
        [Tooltip("The LayerMask of the objects to ignore when performing the line of sight check.")]
        [SerializeField] protected LayerMask m_IgnoreLayerMask;
        [Tooltip("The field of view angle of the agent (in degrees). The x represents the local horizontal angle and the y represents the local vertical angle for a 3D scene.")]
        [SerializeField] protected SharedVariable<Vector2> m_FieldOfViewAngle = new Vector2(90, 135);
        [Tooltip("The distance that the agent can see.")]
        [SerializeField] protected SharedVariable<float> m_Distance = 10;
        [Tooltip("The raycast offset relative to the pivot position.")]
        [SerializeField] protected SharedVariable<Vector3> m_PivotOffset = new Vector3(0, 1.8f, 0);
        [Tooltip("The target raycast offset relative to the pivot position.")]
        [SerializeField] protected SharedVariable<Vector3> m_TargetOffset = new Vector3(0, 0.5f, 0);
        [Tooltip("The offset to apply to 2D angles.")]
        [SerializeField] protected SharedVariable<float> m_AngleOffset2D;
        [Tooltip("Should the target bone be used?")]
        [SerializeField] protected SharedVariable<bool> m_UseTargetBone;
        [Tooltip("The target's bone if the target is a humanoid.")]
        [SerializeField] protected SharedVariable<HumanBodyBones> m_TargetBone;
        [Tooltip("Should the agent's layer be disabled before visibility check is executed?")]
        [SerializeField] protected SharedVariable<bool> m_DisableAgentColliderLayer;
        [Tooltip("Should a debug look ray be drawn to the scene view?")]
        [SerializeField] protected SharedVariable<bool> m_DrawDebugRay;

        private GameObject[] m_ColliderGameObjects;
        private int[] m_ColliderLayers;
        private static int s_IgnoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");

        /// <summary>
        /// Initializes the visibility sensor with the specified GameObject.
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
        /// Returns the GameObject that was detected by the visibility sensor.
        /// </summary>
        /// <returns>The detected GameObject (can be null).</returns>
        public GameObject GetDetectedObject()
        {
            return GetDetectedVisibility().Item1;
        }

        /// <summary>
        /// Returns the distance to the detected object.
        /// </summary>
        /// <returns>The distance to the detected object (returns float.MaxValue if no object was detected).</returns>
        public float GetDetectedAmount()
        {
            return GetDetectedVisibility().Item2;
        }

        /// <summary>
        /// Gets the detected object and its distance.
        /// </summary>
        /// <returns>A tuple containing the detected GameObject and its distance.</returns>
        private (GameObject, float) GetDetectedVisibility()
        {
            if (m_DetectionModes == null || m_DetectionModes.Length == 0) {
                return (null, float.MaxValue);
            }

            // The collider layers on the agent can be set to ignore raycast to prevent them from interferring with the raycast checks.
            if (m_DisableAgentColliderLayer.Value) {
                if (m_ColliderGameObjects == null) {
                    if (m_Use2DPhysics) {
                        var colliders = m_GameObject.GetComponentsInChildren<Collider2D>();
                        m_ColliderGameObjects = new GameObject[colliders.Length];
                        for (int i = 0; i < m_ColliderGameObjects.Length; ++i) {
                            m_ColliderGameObjects[i] = colliders[i].gameObject;
                        }
                    } else {
                        var colliders = m_GameObject.GetComponentsInChildren<Collider>();
                        m_ColliderGameObjects = new GameObject[colliders.Length];
                        for (int i = 0; i < m_ColliderGameObjects.Length; ++i) {
                            m_ColliderGameObjects[i] = colliders[i].gameObject;
                        }
                    }
                    m_ColliderLayers = new int[m_ColliderGameObjects.Length];
                }

                // Change the layer. Remember the previous layer so it can be reset after the check has completed.
                for (int i = 0; i < m_ColliderGameObjects.Length; ++i) {
                    m_ColliderLayers[i] = m_ColliderGameObjects[i].layer;
                    m_ColliderGameObjects[i].layer = s_IgnoreRaycastLayer;
                }
            }

            GameObject detectedObject = null;
            var closestDistance = float.MaxValue;
            for (int i = 0; i < m_DetectionModes.Length; ++i) {
                m_DetectionModes[i].DetectObjects(m_GameObject.transform.position, m_Distance.Value, (GameObject target) =>
                {
                    var distance = GetVisibility(target);
                    if (distance < closestDistance) {
                        detectedObject = target;
                        closestDistance = distance;
                    }
                    return true;
                });
            }

            if (m_DisableAgentColliderLayer.Value) {
                for (int i = 0; i < m_ColliderGameObjects.Length; ++i) {
                    m_ColliderGameObjects[i].layer = m_ColliderLayers[i];
                }
            }

            return (detectedObject, closestDistance);
        }

        /// <summary>
        /// Calculates the visibility angle to a target object.
        /// </summary>
        /// <param name="target">The target object to check visibility to.</param>
        /// <returns>The distance to the target (float.MaxValue if the target is not visible) .</returns>
        private float GetVisibility(GameObject target)
        {
            if (target == null) {
                return float.MaxValue;
            }

            if (m_UseTargetBone.Value) {
                Animator animator;
                if ((animator = CacheUtility.GetCachedParentComponent<Animator>(target)) != null) {
                    var bone = animator.GetBoneTransform(m_TargetBone.Value);
                    if (bone != null) {
                        target = bone.gameObject;
                    }
                }
            }

            // The target object needs to be within the field of view of the current object.
            var position = m_Transform.TransformPoint(m_PivotOffset.Value);
            var targetPosition = target.transform.TransformPoint(m_TargetOffset.Value);
            var direction = Quaternion.Inverse(Quaternion.LookRotation(Vector3.forward, m_Transform.up)) * (targetPosition - position); // InverseTransformDirection.
            var distance = direction.magnitude;
            if (distance > m_Distance.Value) {
#if UNITY_EDITOR
                if (m_DrawDebugRay.Value) {
                    Debug.DrawLine(position, targetPosition, Color.red);
                }
#endif
                return float.MaxValue;
            }

            float angle;
            if (m_Use2DPhysics) {
                var eulerAngles = m_Transform.eulerAngles;
                eulerAngles.z -= m_AngleOffset2D.Value;
                angle = Vector3.Angle(new Vector3(direction.x, direction.y, 0).normalized, Quaternion.Euler(eulerAngles) * Vector3.up);
                if (angle > m_FieldOfViewAngle.Value.x * 0.5f) {
#if UNITY_EDITOR
                    if (m_DrawDebugRay.Value) {
                        Debug.DrawLine(position, targetPosition, Color.magenta);
                    }
#endif
                    return float.MaxValue;
                }
            } else {
                // Calculate horizontal angle.
                var horizontalDirection = new Vector3(direction.x, 0, direction.z).normalized;
                angle = Vector3.Angle(horizontalDirection, m_Transform.forward);
                if (angle > m_FieldOfViewAngle.Value.x * 0.5f) {
#if UNITY_EDITOR
                    if (m_DrawDebugRay.Value) {
                        Debug.DrawLine(position, targetPosition, Color.magenta);
                    }
#endif
                    return float.MaxValue;
                }

                // Calculate vertical angle by comparing the direction vector with its horizontal projection.
                var verticalAngle = Vector3.Angle(direction, horizontalDirection);
                if (verticalAngle > m_FieldOfViewAngle.Value.y * 0.5f) {
#if UNITY_EDITOR
                    if (m_DrawDebugRay.Value) {
                        Debug.DrawLine(position, targetPosition, Color.cyan);
                    }
#endif
                    return float.MaxValue;
                }
            }

            // The hit object needs to be within view of the current agent.
            var hitTransform = LineOfSight(position, target);
            if (hitTransform != null) {
                if (IsAncestor(target.transform, hitTransform)) {
#if UNITY_EDITOR
                    if (m_DrawDebugRay.Value) {
                        Debug.DrawLine(position, targetPosition, Color.green);
                    }
#endif
                    return distance;
#if UNITY_EDITOR
                } else {
                    if (m_DrawDebugRay.Value) {
                        Debug.DrawLine(position, targetPosition, Color.yellow);
                    }
#endif
                }
            } else if (CacheUtility.GetCachedComponent<Collider>(target) == null && CacheUtility.GetCachedComponent<Collider2D>(target) == null || CacheUtility.GetCachedComponent<CharacterController>(target) != null) {
                // If LineOfSight doesn't hit anything then that the target object doesn't have a collider and there is nothing in the way.
                if (target.activeSelf) {
                    return distance;
                }
            }

            return float.MaxValue;
        }

        /// <summary>
        /// Performs a line of sight check between two points.
        /// </summary>
        /// <param name="position">The starting position of the line of sight check.</param>
        /// <param name="targetObject">The target object to check line of sight to.</param>
        /// <returns>The Transform of the first object hit by the line of sight check, or null if nothing was hit.</returns>
        private Transform LineOfSight(Vector3 position, GameObject targetObject)
        {
            if (m_Use2DPhysics) {
                RaycastHit2D hit;
                if ((hit = Physics2D.Linecast(position, targetObject.transform.TransformPoint(m_TargetOffset.Value), ~m_IgnoreLayerMask.value)).transform != null) {
                    return hit.transform;
                }
            } else {
                RaycastHit hit;
                if (Physics.Linecast(position, targetObject.transform.TransformPoint(m_TargetOffset.Value), out hit, ~m_IgnoreLayerMask.value, QueryTriggerInteraction.Ignore)) {
                    return hit.transform;
                }
            }
            return null;
        }

        /// <summary>
        /// Checks if one Transform is an ancestor or descendant of another.
        /// </summary>
        /// <param name="target">The target Transform to check.</param>
        /// <param name="hitTransform">The hit Transform to check.</param>
        /// <returns>True if one Transform is an ancestor or descendant of the other.</returns>
        private static bool IsAncestor(Transform target, Transform hitTransform)
        {
            return hitTransform.IsChildOf(target) || target.IsChildOf(hitTransform);
        }

        /// <summary>
        /// Draws gizmos to visualize the visibility sensor's detection area.
        /// </summary>
        /// <param name="transform">The transform of the agent that the sensor is attached to.</param>
        public override void OnDrawGizmos(Transform transform)
        {
#if UNITY_EDITOR
            var originalColor = UnityEditor.Handles.color;
            UnityEditor.Handles.color = Editor.BehaviorDesignerSettings.Instance.DefaultGizmosColor;

            // Draw the field of view cone
            var pivotPosition = transform.TransformPoint(m_PivotOffset.Value);
            var forward = m_Use2DPhysics ? transform.up : transform.forward;
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            var up = Vector3.Cross(forward, right).normalized;

            // Draw a cone with an arc to visualize the field of view.
            var halfFOV = m_FieldOfViewAngle.Value.x * 0.5f + (m_Use2DPhysics ? m_AngleOffset2D.Value : 0);
            var beginDirection = Quaternion.AngleAxis(-halfFOV, (m_Use2DPhysics ? transform.forward : transform.up)) * (m_Use2DPhysics ? transform.up : transform.forward);
            UnityEditor.Handles.DrawWireArc(pivotPosition, (m_Use2DPhysics ? transform.forward : transform.up), beginDirection, m_FieldOfViewAngle.Value.x, m_Distance.Value);
            var endDirection = Quaternion.AngleAxis(halfFOV, m_Use2DPhysics ? transform.forward : transform.up) * (m_Use2DPhysics ? transform.up : transform.forward);
            UnityEditor.Handles.DrawLine(pivotPosition, pivotPosition + beginDirection * m_Distance.Value);
            UnityEditor.Handles.DrawLine(pivotPosition, pivotPosition + endDirection * m_Distance.Value);

            if (!m_Use2DPhysics) {
                // 3D requires another cone.
                halfFOV = m_FieldOfViewAngle.Value.y * 0.5f + m_AngleOffset2D.Value;
                var verticalBeginDirection = Quaternion.AngleAxis(-halfFOV, transform.right) * transform.forward;
                var verticalEndDirection = Quaternion.AngleAxis(halfFOV, transform.right) * transform.forward;
                UnityEditor.Handles.DrawLine(pivotPosition, pivotPosition + verticalBeginDirection * m_Distance.Value);
                UnityEditor.Handles.DrawLine(pivotPosition, pivotPosition + verticalEndDirection * m_Distance.Value);
                UnityEditor.Handles.DrawWireArc(pivotPosition, transform.right, verticalBeginDirection, m_FieldOfViewAngle.Value.y, m_Distance.Value);
            }
            UnityEditor.Handles.color = originalColor;
#endif
        }
    }
}