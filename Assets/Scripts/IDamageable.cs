using UnityEngine;

/// <summary>
/// Contract for anything that can receive damage (enemies, asteroids, obstacles).
/// The laser applies damage-per-frame via <see cref="TakeDamage"/>.
/// </summary>
public interface IDamageable
{
    /// <summary>Applies a positive amount of damage to this object.</summary>
    /// <param name="amount">Damage to apply this call (already scaled by Time.deltaTime for continuous sources).</param>
    /// <param name="hitPoint">World-space point where the damage was applied (for feedback/VFX).</param>
    void TakeDamage(float amount, Vector3 hitPoint);

    /// <summary>True while this object is still alive and can take damage.</summary>
    bool IsAlive { get; }
}
