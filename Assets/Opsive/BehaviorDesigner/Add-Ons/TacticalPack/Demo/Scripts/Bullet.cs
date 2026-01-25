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
    /// Damages the IDamageable upon collision.
    /// </summary>
    public class Bullet : MonoBehaviour
    {
        [Tooltip("The speed of the bullet.")]
        [SerializeField] protected float m_Speed = 5;
        [Tooltip("The amount of damage that the bullet does.")]
        [SerializeField] protected float m_DamageAmount = 10;
        [Tooltip("Amount of time until the bullet should destroy itself if it doesn't hit a target.")]
        [SerializeField] protected float m_SelfDestructTime = 3;

        private Rigidbody m_Rigidbody;
        private Rigidbody2D m_Rigidbody2D;
        private Transform m_Transform;

        /// <summary>
        /// Initializes the default values.
        /// </summary>
        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Rigidbody2D = GetComponent<Rigidbody2D>();
            m_Transform = transform;

            Invoke("SelfDestruct", m_SelfDestructTime);
        }

        /// <summary>
        /// Move in the forward direction.
        /// </summary>
        void FixedUpdate()
        {
            if (m_Rigidbody != null) {
                m_Rigidbody.MovePosition(m_Rigidbody.position + m_Speed * m_Transform.forward * Time.deltaTime);
            }
            if (m_Rigidbody2D != null) {
                m_Rigidbody2D.MovePosition(m_Rigidbody2D.position + m_Speed * new Vector2(m_Transform.up.x, m_Transform.up.y) * Time.deltaTime);
            }
        }

        /// <summary>
        /// Perform any damage to the collided object and destroy itself.
        /// </summary>
        /// <param name="collision">The collision event.</param>
        private void OnCollisionEnter(Collision collision)
        {
            IDamageable damageable;
            if ((damageable = collision.gameObject.GetComponent<IDamageable>()) != null) {
                damageable.Damage(m_DamageAmount);
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Perform any damage to the collided object and destroy itself.
        /// </summary>
        /// <param name="collision">The collision event.</param>
        private void OnCollisionEnter2D(Collision2D collision)
        {
            IDamageable damageable;
            if ((damageable = collision.gameObject.GetComponent<IDamageable>()) != null) {
                damageable.Damage(m_DamageAmount);
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Destroy itself.
        /// </summary>
        private void SelfDestruct()
        {
            Destroy(gameObject);
        }
    }
}