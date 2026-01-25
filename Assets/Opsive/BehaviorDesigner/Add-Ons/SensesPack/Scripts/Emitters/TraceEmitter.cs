/// ---------------------------------------------
/// Senses Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Emitters
{
    using UnityEngine;

    /// <summary>
    /// Creates a trace within the TraceManager. Can be used for a single trace or over a trail.
    /// </summary>
    public class TraceEmitter : MonoBehaviour
    {
        [Tooltip("How often to emit the trace (in seconds).")]
        [SerializeField] protected float m_EmitInterval = 0;
        [Tooltip("The intensity of the trace.")]
        [SerializeField] protected float m_Intensity = 1f;
        [Tooltip("The time in seconds before the trace fully dissipates. Set to 0 for persistant traces.")]
        [SerializeField] protected float m_DissipationTime = 1f;
        [Tooltip("The offset from the transform where the trace should be placed.")]
        [SerializeField] protected Vector3 m_PositionOffset = Vector3.zero;
        [Tooltip("A reference to the particle effect that should be spawned when the trace is emitted.")]
        [SerializeField] protected GameObject m_ParticleEffect;

        private Transform m_Transform;
        private float m_NextEmissionTime;
        private GameObject m_Particle;

        /// <summary>
        /// Initializes the default values.
        /// </summary>
        private void Awake()
        {
            m_Transform = transform;
        }

        /// <summary>
        /// The component has been enabled.
        /// </summary>
        private void OnEnable()
        {
            if (m_ParticleEffect != null) {
                m_Particle = GameObject.Instantiate(m_ParticleEffect, transform.position, transform.rotation, transform);
            }
        }


        /// <summary>
        /// Creates a new trace at the current position.
        /// </summary>
        private void Start()
        {
            if (m_EmitInterval == 0) {
                Emit();
                enabled = false;
            }
        }

        /// <summary>
        /// Emits a scent at the specified interval.
        /// </summary>
        private void Update()
        {
            if (Time.time < m_NextEmissionTime) {
                return;
            }

            TraceManager.Instance.Add(new Trace(transform.position, m_Intensity, m_DissipationTime));
            m_NextEmissionTime = Time.time + m_EmitInterval;
        }

        /// <summary>
        /// Emits the trace.
        /// </summary>
        private void Emit()
        {
            TraceManager.Instance.Add(new Trace(transform.TransformPoint(m_PositionOffset), m_Intensity, m_DissipationTime));
        }

        /// <summary>
        /// The component has been disabled.
        /// </summary>
        private void OnDisable()
        {
            if (m_Particle != null) {
                GameObject.Destroy(m_Particle);
                m_Particle = null;
            }
        }
    }
} 