using System;
using UnityEngine;

/// <summary>
/// Generic health pool that implements <see cref="IDamageable"/>. Attach to any object
/// that should take damage from the laser (enemies, asteroids, obstacles). The cargo ship
/// should NOT have this component, since the laser explicitly excludes its own ship.
/// </summary>
public class Health : MonoBehaviour, IDamageable
{
    [Tooltip("Maximum (and starting) health for this object.")]
    [SerializeField] private float maxHealth = 100f;

    [Tooltip("If true, the GameObject is destroyed when health reaches zero.")]
    [SerializeField] private bool destroyOnDeath = true;

    private float currentHealth;
    private bool isDead;

    /// <summary>Raised whenever health changes. Args: (current, max).</summary>
    public event Action<float, float> HealthChanged;

    /// <summary>Raised once when health reaches zero.</summary>
    public event Action Died;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsAlive => !isDead && currentHealth > 0f;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount, Vector3 hitPoint)
    {
        if (isDead || amount <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        HealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        Died?.Invoke();

        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }
}
