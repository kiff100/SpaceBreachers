/// ---------------------------------------------
/// Senses Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.SensesPack.Demo
{
    using UnityEngine;

    /// <summary>
    /// Plays a footstep sound at a specified interval when the player is moving.
    /// </summary>
    public class FootstepAudio : MonoBehaviour
    {
        [Tooltip("The AudioClip that should be played.")]
        [SerializeField] protected AudioClip m_FootstepClip;
        [Tooltip("How often to play the footstep sound (in seconds).")]
        [SerializeField] protected float m_FootstepInterval = 0.5f;
        [Tooltip("The minimum velocity required to consider the player as moving.")]
        [SerializeField] protected float m_MinVelocity = 0.5f;

        private Transform m_Transform;
        private AudioSource m_AudioSource;
        private float m_NextFootstepTime;
        private Vector3 m_PreviousPosition;

        /// <summary>
        /// Initializes the default values.
        /// </summary>
        private void Start()
        {
            m_Transform = transform;
            m_AudioSource = gameObject.GetComponent<AudioSource>();
            m_PreviousPosition = transform.position;
        }

        /// <summary>
        /// Plays a footstep sound if the player is moving.
        /// </summary>
        private void Update()
        {
            if (Time.time < m_NextFootstepTime) {
                return;
            }

            // Play footstep sound if the player is moving.
            var currentPosition = transform.position;
            if (Vector3.Distance(currentPosition, m_PreviousPosition) / Time.deltaTime > m_MinVelocity) {
                m_AudioSource.PlayOneShot(m_FootstepClip);
                m_NextFootstepTime = Time.time + m_FootstepInterval;
            }
            m_PreviousPosition = currentPosition;
        }
    }
}