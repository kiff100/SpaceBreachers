/// ---------------------------------------------
/// Tactical Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.TacticalPack.Demo
{
    using Opsive.BehaviorDesigner.AddOns.TacticalPack.Runtime;
    using UnityEngine;

    /// <summary>
    /// Example IAttackAgent which will attack by firing a bullet.
    /// </summary>
    public class Shootable : MonoBehaviour, IAttackAgent
    {
        [Tooltip("Is the perspective 2D?")]
        [SerializeField] protected bool m_Is2D;
        [Tooltip("The prefab bullet.")]
        [SerializeField] protected GameObject m_Bullet;
        [Tooltip("The location that the bullet should be fired from.")]
        [SerializeField] protected GameObject m_FirePosition;
        [Tooltip("The amount of time it takes for the agent to be able to attack again.")]
        [SerializeField] protected float m_RepeatAttackDelay;
        [Tooltip("The closest  distance that the agent is able to attack from.")]
        [SerializeField] protected float m_MinAttackDistance = 5;
        [Tooltip("The furthest distance that the agent is able to attack from.")]
        [SerializeField] protected float m_MaxAttackDistance = 8;
        [Tooltip("The maximum angle that the agent can attack from.")]
        [SerializeField] protected float m_AttackAngle;
        [Tooltip("The speed at which the agent rotates towards the target.")]
        [SerializeField] protected float m_RotationSpeed = 5f;

        private Transform m_Transform;

        public float MinAttackDistance => m_MinAttackDistance;
        public float MaxAttackDistance => m_MaxAttackDistance;
        public float AttackAngleThreshold => m_AttackAngle;


        private float m_LastAttackTime;

        /// <summary>
        /// Initialize the default values.
        /// </summary>
        private void Awake()
        {
            m_Transform = transform;
            m_LastAttackTime = -m_RepeatAttackDelay;
        }

        /// <summary>
        /// Rotates towards the specified direction.
        /// </summary>
        /// <param name="direction">The direction to rotate towards.</param>
        public void RotateTowards(Vector3 direction)
        {
            if (m_Is2D) {
                var forward2D = new Vector2(direction.x, direction.y).normalized;
                var targetAngle = Mathf.Atan2(forward2D.y, forward2D.x) * Mathf.Rad2Deg;
                targetAngle = (270 + targetAngle) % 360;
                var currentAngle = m_Transform.eulerAngles.z;
                var newAngle = Mathf.MoveTowards(currentAngle, currentAngle + Mathf.DeltaAngle(currentAngle, targetAngle), m_RotationSpeed * Time.deltaTime);
                m_Transform.rotation = Quaternion.Euler(0, 0, newAngle);
            } else {
                m_Transform.rotation = Quaternion.Slerp(m_Transform.rotation, Quaternion.LookRotation(direction), m_RotationSpeed * Time.deltaTime);
            }
        }

        /// <summary>
        /// Does the actual attack. 
        /// </summary>
        /// <param name="target">The target to attack.</param>
        /// <param name="targetDamageable">The damagable being attacked.</param>
        public void Attack(Transform target, IDamageable targetDamageable)
        {
            // Don't attack too often.
            if (m_LastAttackTime + m_RepeatAttackDelay > Time.time) {
                return;
            }

            // Attack the target.
            GameObject.Instantiate(m_Bullet, m_FirePosition.transform.position, m_Is2D ? m_Transform.rotation : Quaternion.LookRotation((target.position - m_Transform.position).normalized));
            m_LastAttackTime = Time.time;
        }
    }
}