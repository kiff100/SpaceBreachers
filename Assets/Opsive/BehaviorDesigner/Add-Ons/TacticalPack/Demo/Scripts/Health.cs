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
    /// Example IDamageable which adds health to an object.
    /// </summary>
    public class Health : MonoBehaviour, IDamageable
    {
        [Tooltip("The starting amount of health")]
        [SerializeField] protected float m_Amount = 100;

        public bool IsAlive => m_CurrentValue > 0;

        private float m_CurrentValue;
        private MeshRenderer[] m_MeshRenderers;
        private SpriteRenderer[] m_SpriteRenderers;

        /// <summary>
        /// Initializes the current health.
        /// </summary>
        private void Awake()
        {
            m_CurrentValue = m_Amount;
            m_MeshRenderers = GetComponentsInChildren<MeshRenderer>(true);
            m_SpriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        }

        /// <summary>
        /// Take damage. Deactivate if the amount of remaining health is 0.
        /// </summary>
        /// <param name="amount"></param>
        public void Damage(float amount)
        {
            m_CurrentValue = Mathf.Max(m_CurrentValue - amount, 0);
            if (m_CurrentValue == 0) {
                // The scenario manager manages the GameObject active state.
                if (m_MeshRenderers != null) {
                    for (int i = 0; i < m_MeshRenderers.Length; ++i) {
                        m_MeshRenderers[i].enabled = false;
                    }
                }
                if (m_SpriteRenderers != null) {
                    for (int i = 0; i < m_SpriteRenderers.Length; ++i) {
                        m_SpriteRenderers[i].enabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// Sets the current health to the starting health and enables the object.
        /// </summary>
        public void ResetHealth()
        {
            m_CurrentValue = m_Amount;
            if (m_MeshRenderers != null) {
                for (int i = 0; i < m_MeshRenderers.Length; ++i) {
                    m_MeshRenderers[i].enabled = true;
                }
            }
            if (m_SpriteRenderers != null) {
                for (int i = 0; i < m_SpriteRenderers.Length; ++i) {
                    m_SpriteRenderers[i].enabled = true;
                }
            }
        }
    }
}