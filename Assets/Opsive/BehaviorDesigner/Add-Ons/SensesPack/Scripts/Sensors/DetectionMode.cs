/// ---------------------------------------------
/// Senses Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Sensors
{
    using Opsive.GraphDesigner.Runtime.Variables;
    using System;
    using UnityEngine;

    /// <summary>
    /// Base class for all detection modes. A detection mode determines how objects are detected within a sensor's range.
    /// </summary>
    public abstract class DetectionMode
    {
        /// <summary>
        /// Initializes the detection mode with the specified physics type.
        /// </summary>
        /// <param name="use2DPhysics">Should 2D physics be used?</param>
        public virtual void Initialize(bool use2DPhysics) { }

        /// <summary>
        /// Detects objects based on the specified position and magnitude.
        /// </summary>
        /// <param name="position">The origin.</param>
        /// <param name="magnitude">The magnitude of the detector.</param>
        /// <param name="onFilterObject">Callback that allows objects to be filtered.</param>
        public abstract void DetectObjects(Vector3 position, float magnitude, Func<GameObject, bool> onFilterObject);
    }

    /// <summary>
    /// Detection mode that detects a specific GameObject.
    /// </summary>
    public class ObjectDetectionMode : DetectionMode
    {
        [Tooltip("The object that should be detected.")]
        [SerializeField] protected SharedVariable<GameObject> m_Object;

        /// <summary>
        /// Detects the specified object using the provided filter callback.
        /// </summary>
        /// <param name="position">The origin position of the detection.</param>
        /// <param name="magnitude">The magnitude of the detection area.</param>
        /// <param name="onFilterObject">Callback that allows the object to be filtered.</param>
        public override void DetectObjects(Vector3 position, float magnitude, Func<GameObject, bool> onFilterObject)
        {
            onFilterObject(m_Object.Value);
        }
    }

    /// <summary>
    /// Detection mode that detects multiple GameObjects.
    /// </summary>
    public class ObjectArrayDetectionMode : DetectionMode
    {
        [Tooltip("The array of objects that should be detected.")]
        [SerializeField] protected SharedVariable<GameObject[]> m_Objects;

        /// <summary>
        /// Detects the specified objects using the provided filter callback.
        /// </summary>
        /// <param name="position">The origin position of the detection.</param>
        /// <param name="magnitude">The magnitude of the detection area.</param>
        /// <param name="onFilterObject">Callback that allows objects to be filtered.</param>
        public override void DetectObjects(Vector3 position, float magnitude, Func<GameObject, bool> onFilterObject)
        {
            for (int i = 0; i < m_Objects.Value.Length; ++i) {
                if (!onFilterObject(m_Objects.Value[i])) {
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Detection mode that detects objects with a specific tag.
    /// </summary>
    public class TagDetectionMode : DetectionMode
    {
        [Tooltip("The tag that should be detected.")]
        [SerializeField] protected SharedVariable<string> m_Tag;

        /// <summary>
        /// Detects objects with the specified tag using the provided filter callback.
        /// </summary>
        /// <param name="position">The origin position of the detection.</param>
        /// <param name="magnitude">The magnitude of the detection area.</param>
        /// <param name="onFilterObject">Callback that allows objects to be filtered.</param>
        public override void DetectObjects(Vector3 position, float magnitude, Func<GameObject, bool> onFilterObject)
        {
            var gameObjects = GameObject.FindGameObjectsWithTag(m_Tag.Value);
            for (int i = 0; i < gameObjects.Length; ++i) {
                if (!onFilterObject(gameObjects[i])) {
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Detection mode that detects objects on specific layers.
    /// </summary>
    public class LayerMaskDetectionMode : DetectionMode
    {
        [Tooltip("The layer mask that should be detected.")]
        [SerializeField] protected SharedVariable<LayerMask> m_LayerMask = (LayerMask)(-1);
        [Tooltip("The maximum number of objects that can be detected at once.")]
        [SerializeField] protected int m_MaxObjectCount = 200;

        private Collider[] m_OverlapColliders;
        private Collider2D[] m_OverlapColliders2D;

        /// <summary>
        /// Initializes the detection mode with the specified physics type.
        /// </summary>
        /// <param name="use2DPhysics">Should 2D physics be used?</param>
        public override void Initialize(bool use2DPhysics)
        {
            if (use2DPhysics) {
                m_OverlapColliders2D = new Collider2D[m_MaxObjectCount];
            } else {
                m_OverlapColliders = new Collider[m_MaxObjectCount];
            }
        }

        /// <summary>
        /// Detects objects on the specified layers using the provided filter callback.
        /// </summary>
        /// <param name="position">The origin position of the detection.</param>
        /// <param name="magnitude">The magnitude of the detection area.</param>
        /// <param name="onFilterObject">Callback that allows objects to be filtered.</param>
        public override void DetectObjects(Vector3 position, float magnitude, Func<GameObject, bool> onFilterObject)
        {
            if (m_OverlapColliders2D != null) {
#if UNITY_6000_0_OR_NEWER
                var count = Physics2D.OverlapCircle(position, magnitude, new ContactFilter2D() { layerMask = m_LayerMask.Value }, m_OverlapColliders2D);
#else
                var count = Physics2D.OverlapCircleNonAlloc(position, magnitude, m_OverlapColliders2D, m_LayerMask.Value);
#endif
                for (int i = 0; i < count; ++i) {
                    if (!onFilterObject(m_OverlapColliders2D[i].gameObject)) {
                        return;
                    }
                }
            } else {
                var count = Physics.OverlapSphereNonAlloc(position, magnitude, m_OverlapColliders, m_LayerMask.Value);
                for (int i = 0; i < count; ++i) {
                    if (!onFilterObject(m_OverlapColliders[i].gameObject)) {
                        return;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Base class for all cast-based detection modes. A cast detection mode uses physics casts to detect objects.
    /// </summary>
    public abstract class CastDetectionMode
    {
        [Tooltip("Should the agent's forward direction be used for the cast direction?")]
        [SerializeField] protected SharedVariable<bool> m_UseAgentForwardDirection;
        [Tooltip("The direction to cast in. Only used if UseAgentForwardDirection is false.")]
        [SerializeField] protected SharedVariable<Vector3> m_Direction = new Vector3(0, 0, 1);
        [Tooltip("The offset of the position.")]
        [SerializeField] protected SharedVariable<Vector3> m_Offset;
        [Tooltip("The distance to cast.")]
        [SerializeField] protected SharedVariable<float> m_Distance = 1;
        [Tooltip("The layer mask to cast against.")]
        [SerializeField] protected SharedVariable<LayerMask> m_LayerMask = (LayerMask)(-1);
        [Tooltip("The maximum number of objects that can be hit by the cast.")]
        [SerializeField] protected int m_MaxHitCount = 200;

        protected RaycastHit[] m_RaycastResults;
        protected RaycastHit2D[] m_RaycastResults2D;

        /// <summary>
        /// Initializes the circle cast detection mode.
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// Initializes the cast detection mode with the specified physics type.
        /// </summary>
        /// <param name="use2DPhysics">Should 2D physics be used?</param>
        protected void Initialize(bool use2DPhysics)
        {
            if (use2DPhysics) {
                m_RaycastResults2D = new RaycastHit2D[m_MaxHitCount];
            } else {
                m_RaycastResults = new RaycastHit[m_MaxHitCount];
            }
        }

        /// <summary>
        /// Detects objects using the cast-based detection method.
        /// </summary>
        /// <param name="position">The origin position of the cast.</param>
        /// <param name="onFilterObject">Callback that allows objects to be filtered.</param>
        public abstract void DetectObjects(Vector3 position, Func<GameObject, bool> onFilterObject);

        /// <summary>
        /// Draws gizmos to visualize the cast detection area.
        /// </summary>
        /// <param name="position">The position to draw the gizmos at.</param>
        public virtual void OnDrawGizmos(Vector3 position) { }
    }

    /// <summary>
    /// Detection mode that uses raycasts to detect objects.
    /// </summary>
    public class RaycastDetectionMode : CastDetectionMode
    {
        [Tooltip("Should 2D physics be used for the raycast?")]
        [SerializeField] protected bool m_Use2DPhysics;

        /// <summary>
        /// Initializes the raycast detection mode.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize(m_Use2DPhysics);
        }

        /// <summary>
        /// Detects objects using a raycast.
        /// </summary>
        /// <param name="position">The origin position of the raycast.</param>
        /// <param name="onFilterObject">Callback that allows objects to be filtered.</param>
        public override void DetectObjects(Vector3 position, Func<GameObject, bool> onFilterObject)
        {
            if (m_RaycastResults != null) {
                var hitCount = Physics.RaycastNonAlloc(position + m_Offset.Value, m_Direction.Value, m_RaycastResults, m_Distance.Value, m_LayerMask.Value, QueryTriggerInteraction.Ignore);
                if (hitCount > 0) {
                    for (int i = 0; i < hitCount; ++i) {
                        if (!onFilterObject(m_RaycastResults[i].transform.gameObject)) {
                            return;
                        }
                    }
                }
            } else {
                var hitCount = Physics2D.RaycastNonAlloc(position + m_Offset.Value, m_Direction.Value, m_RaycastResults2D, m_Distance.Value, m_LayerMask.Value);
                if (hitCount > 0) {
                    for (int i = 0; i < hitCount; ++i) {
                        if (!onFilterObject(m_RaycastResults2D[i].transform.gameObject)) {
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Draws gizmos to visualize the raycast.
        /// </summary>
        /// <param name="position">The position to draw the gizmos at.</param>
        public override void OnDrawGizmos(Vector3 position)
        {
#if UNITY_EDITOR
            var originalColor = Gizmos.color;
            Gizmos.color = Editor.BehaviorDesignerSettings.Instance.DefaultGizmosColor;
            Gizmos.DrawRay(position + m_Offset.Value, m_Direction.Value.normalized * m_Distance.Value);
            Gizmos.color = originalColor;
#endif
        }
    }

    /// <summary>
    /// Detection mode that uses sphere casts to detect objects.
    /// </summary>
    public class SphereCastDetectionMode : CastDetectionMode
    {
        [Tooltip("The radius of the sphere cast.")]
        [SerializeField] protected SharedVariable<float> m_Radius;

        /// <summary>
        /// Initializes the sphere cast detection mode.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize(false);
        }

        /// <summary>
        /// Detects objects using a sphere cast.
        /// </summary>
        /// <param name="position">The origin position of the sphere cast.</param>
        /// <param name="onFilterObject">Callback that allows objects to be filtered.</param>
        public override void DetectObjects(Vector3 position, Func<GameObject, bool> onFilterObject)
        {
            var hitCount = Physics.SphereCastNonAlloc(position + m_Offset.Value, m_Radius.Value, m_Direction.Value, m_RaycastResults, m_Distance.Value, m_LayerMask.Value, QueryTriggerInteraction.Ignore);
            if (hitCount > 0) {
                for (int i = 0; i < hitCount; ++i) {
                    if (!onFilterObject(m_RaycastResults[i].transform.gameObject)) {
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Draws gizmos to visualize the sphere cast.
        /// </summary>
        /// <param name="position">The position to draw the gizmos at.</param>
        public override void OnDrawGizmos(Vector3 position)
        {
#if UNITY_EDITOR
            var originalColor = Gizmos.color;
            Gizmos.color = Editor.BehaviorDesignerSettings.Instance.DefaultGizmosColor;
            Gizmos.DrawWireSphere(position + m_Offset.Value, m_Radius.Value);
            Gizmos.DrawWireSphere(position + m_Offset.Value + m_Direction.Value.normalized * m_Distance.Value, m_Radius.Value);
            Gizmos.DrawLine(position + m_Offset.Value, position + m_Offset.Value + m_Direction.Value.normalized * m_Distance.Value);
            Gizmos.color = originalColor;
#endif
        }
    }

    /// <summary>
    /// Detection mode that uses circle casts to detect objects.
    /// </summary>
    public class CircleCastDetectionMode : CastDetectionMode
    {
        [Tooltip("The radius of the circle cast.")]
        [SerializeField] protected SharedVariable<float> m_Radius;

        /// <summary>
        /// Initializes the circle cast detection mode.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize(true);
        }

        /// <summary>
        /// Detects objects using a circle cast.
        /// </summary>
        /// <param name="position">The origin position of the circle cast.</param>
        /// <param name="onFilterObject">Callback that allows objects to be filtered.</param>
        public override void DetectObjects(Vector3 position, Func<GameObject, bool> onFilterObject)
        {
#if UNITY_6000_0_OR_NEWER
            var hitCount = Physics2D.CircleCast(position + m_Offset.Value, m_Radius.Value, m_Direction.Value, new ContactFilter2D() { layerMask = m_LayerMask.Value }, m_RaycastResults2D, m_Distance.Value);
#else
            var hitCount = Physics2D.CircleCastNonAlloc(position + m_Offset.Value, m_Radius.Value, m_Direction.Value, m_RaycastResults2D, m_Distance.Value, m_LayerMask.Value);
#endif
            if (hitCount > 0) {
                for (int i = 0; i < hitCount; ++i) {
                    if (!onFilterObject(m_RaycastResults[i].transform.gameObject)) {
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Draws gizmos to visualize the circle cast.
        /// </summary>
        /// <param name="position">The position to draw the gizmos at.</param>
        public override void OnDrawGizmos(Vector3 position)
        {
#if UNITY_EDITOR
            var originalColor = Gizmos.color;
            Gizmos.color = Editor.BehaviorDesignerSettings.Instance.DefaultGizmosColor;
            Gizmos.DrawWireSphere(position + m_Offset.Value, m_Radius.Value);
            Gizmos.DrawWireSphere(position + m_Offset.Value + m_Direction.Value.normalized * m_Distance.Value, m_Radius.Value);
            Gizmos.DrawLine(position + m_Offset.Value, position + m_Offset.Value + m_Direction.Value.normalized * m_Distance.Value);
            Gizmos.color = originalColor;
#endif
        }
    }

    /// <summary>
    /// Detection mode that uses capsule casts to detect objects.
    /// </summary>
    public class CapsuleCastDetectionMode : CastDetectionMode
    {
        [Tooltip("The offset of the secondary position for the capsule cast.")]
        [SerializeField] protected SharedVariable<Vector3> m_SecondaryPositionOffset;
        [Tooltip("The radius of the capsule cast.")]
        [SerializeField] protected SharedVariable<float> m_Radius;

        /// <summary>
        /// Initializes the capsule cast detection mode.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize(false);
        }

        /// <summary>
        /// Detects objects using a capsule cast.
        /// </summary>
        /// <param name="position">The origin position of the capsule cast.</param>
        /// <param name="onFilterObject">Callback that allows objects to be filtered.</param>
        public override void DetectObjects(Vector3 position, Func<GameObject, bool> onFilterObject)
        {
            var hitCount = Physics.CapsuleCastNonAlloc(position + m_Offset.Value, position + m_Offset.Value + m_SecondaryPositionOffset.Value, m_Radius.Value, m_Direction.Value, m_RaycastResults, m_Distance.Value, m_LayerMask.Value, QueryTriggerInteraction.Ignore);
            if (hitCount > 0) {
                for (int i = 0; i < hitCount; ++i) {
                    if (!onFilterObject(m_RaycastResults[i].transform.gameObject)) {
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Draws gizmos to visualize the capsule cast.
        /// </summary>
        /// <param name="position">The position to draw the gizmos at.</param>
        public override void OnDrawGizmos(Vector3 position)
        {
#if UNITY_EDITOR
            position += m_Offset.Value;
            var originalColor = Gizmos.color;
            Gizmos.color = Editor.BehaviorDesignerSettings.Instance.DefaultGizmosColor;
            Gizmos.DrawWireSphere(position, m_Radius.Value);
            Gizmos.DrawWireSphere(position + m_SecondaryPositionOffset.Value, m_Radius.Value);
            Gizmos.DrawWireSphere(position + m_Direction.Value.normalized * m_Distance.Value, m_Radius.Value);
            Gizmos.DrawWireSphere(position + m_Direction.Value.normalized * m_Distance.Value + m_SecondaryPositionOffset.Value, m_Radius.Value);
            Gizmos.DrawLine(position, position + m_Direction.Value.normalized * m_Distance.Value);
            Gizmos.DrawLine(position + m_SecondaryPositionOffset.Value, position + m_Direction.Value.normalized * m_Distance.Value + m_SecondaryPositionOffset.Value);
            Gizmos.color = originalColor;
#endif
        }
    }

    /// <summary>
    /// Detection mode that uses box casts to detect objects.
    /// </summary>
    public class BoxCastDetectionMode : CastDetectionMode
    {
        [Tooltip("The size of the box cast.")]
        [SerializeField] protected SharedVariable<Vector3> m_Size;
        [Tooltip("The orientation of the box cast.")]
        [SerializeField] protected SharedVariable<Vector3> m_Orientation;

        /// <summary>
        /// Initializes the box cast detection mode.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize(false);
        }

        /// <summary>
        /// Detects objects using a box cast.
        /// </summary>
        /// <param name="position">The origin position of the box cast.</param>
        /// <param name="onFilterObject">Callback that allows objects to be filtered.</param>
        public override void DetectObjects(Vector3 position, Func<GameObject, bool> onFilterObject)
        {
            var hitCount = Physics.BoxCastNonAlloc(position + m_Offset.Value, m_Size.Value / 2, m_Direction.Value, m_RaycastResults, Quaternion.Euler(m_Orientation.Value), m_Distance.Value, m_LayerMask.Value, QueryTriggerInteraction.Ignore);
            if (hitCount > 0) {
                for (int i = 0; i < hitCount; ++i) {
                    if (!onFilterObject(m_RaycastResults[i].transform.gameObject)) {
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Draws gizmos to visualize the box cast.
        /// </summary>
        /// <param name="position">The position to draw the gizmos at.</param>
        public override void OnDrawGizmos(Vector3 position)
        {
#if UNITY_EDITOR
            position += m_Offset.Value;
            var originalColor = Gizmos.color;
            Gizmos.color = Editor.BehaviorDesignerSettings.Instance.DefaultGizmosColor;
            Gizmos.matrix = Matrix4x4.TRS(position, Quaternion.Euler(m_Orientation.Value), Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, m_Size.Value);
            Gizmos.matrix = Matrix4x4.TRS(position + m_Direction.Value.normalized * m_Distance.Value, Quaternion.Euler(m_Orientation.Value), Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, m_Size.Value);
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawLine(position, position + m_Direction.Value.normalized * m_Distance.Value);
            Gizmos.color = originalColor;
#endif
        }
    }

    /// <summary>
    /// Attribute indicating that a ReorderableList should be used for the DetectionMode list.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class DetectionModeListAttribute : System.Attribute
    {

    }

    /// <summary>
    /// Attribute indicating that a ReorderableList should be used for the CastDetectionMode list.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class CastDetectionModeListAttribute : System.Attribute
    {

    }
}