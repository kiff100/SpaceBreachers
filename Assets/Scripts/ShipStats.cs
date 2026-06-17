using System;
using UnityEngine;

/// <summary>
/// Tracks a ship's energy pool. Energy is drained by powered tools (e.g. the laser) and
/// regenerates over time. Exposes events so UI (the energy meter) and weapons can react
/// without holding direct cross-scene references.
/// </summary>
public class ShipStats : MonoBehaviour
{
    [Header("Energy Pool")]
    [Tooltip("Maximum (and starting) energy.")]
    [SerializeField] private float maxEnergy = 100f;

    [Header("Regeneration")]
    [Tooltip("Energy restored per second while regenerating.")]
    [SerializeField] private float energyRegenPerSecond = 15f;

    [Tooltip("Seconds to wait after the last energy use before regeneration resumes.")]
    [SerializeField] private float regenDelay = 1f;

    [Tooltip("If true, this instance is exposed as ShipStats.Primary for UI binding.")]
    [SerializeField] private bool registerAsPrimary = true;

    private float currentEnergy;
    private float lastDrainTime = -999f;

    /// <summary>The primary ship's stats, used by cross-scene UI for binding.</summary>
    public static ShipStats Primary { get; private set; }

    /// <summary>Raised whenever energy changes. Args: (current, max).</summary>
    public event Action<float, float> EnergyChanged;

    /// <summary>Raised when something tried to use energy it didn't have (UI flashes in response).</summary>
    public event Action InsufficientEnergy;

    /// <summary>Raised when energy is fully depleted to zero.</summary>
    public event Action EnergyDepleted;

    public float MaxEnergy => maxEnergy;
    public float CurrentEnergy => currentEnergy;

    /// <summary>Current energy as a 0..1 fraction of max.</summary>
    public float EnergyFraction => maxEnergy > 0f ? currentEnergy / maxEnergy : 0f;

    private void Awake()
    {
        currentEnergy = maxEnergy;
        if (registerAsPrimary || Primary == null)
        {
            Primary = this;
        }
    }

    private void OnDestroy()
    {
        if (Primary == this)
        {
            Primary = null;
        }
    }

    private void Start()
    {
        EnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }

    private void Update()
    {
        if (currentEnergy < maxEnergy &&
            energyRegenPerSecond > 0f &&
            Time.time - lastDrainTime >= regenDelay)
        {
            ModifyEnergy(energyRegenPerSecond * Time.deltaTime);
        }
    }

    /// <summary>Returns true if at least <paramref name="amount"/> energy is available.</summary>
    public bool HasEnergy(float amount)
    {
        return currentEnergy >= amount;
    }

    /// <summary>Returns true if current energy is at or above the given 0..1 fraction of max.</summary>
    public bool HasEnergyFraction(float fraction)
    {
        return EnergyFraction >= fraction;
    }

    /// <summary>
    /// Drains energy (clamped to what's available). Resets the regen delay timer.
    /// Returns true if any energy remains afterwards.
    /// </summary>
    public bool DrainEnergy(float amount)
    {
        if (amount <= 0f)
        {
            return currentEnergy > 0f;
        }

        lastDrainTime = Time.time;
        ModifyEnergy(-amount);
        return currentEnergy > 0f;
    }

    /// <summary>Notifies listeners (UI) that there wasn't enough energy to perform an action.</summary>
    public void SignalInsufficientEnergy()
    {
        InsufficientEnergy?.Invoke();
    }

    private void ModifyEnergy(float delta)
    {
        float newValue = Mathf.Clamp(currentEnergy + delta, 0f, maxEnergy);
        if (Mathf.Approximately(newValue, currentEnergy))
        {
            return;
        }

        bool wasAboveZero = currentEnergy > 0f;
        currentEnergy = newValue;
        EnergyChanged?.Invoke(currentEnergy, maxEnergy);

        if (wasAboveZero && currentEnergy <= 0f)
        {
            EnergyDepleted?.Invoke();
        }
    }
}
