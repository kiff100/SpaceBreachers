using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the on-screen energy meter. Binds to <see cref="ShipStats.Primary"/> (resolving it
/// lazily, since the gameplay scene may load after this persistent UI), updates the fill bar as
/// energy changes, and pulses the bar red when a fire attempt is rejected for low energy.
/// </summary>
public class EnergyBarUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Filled image representing current energy. Should use Image.Type = Filled (Horizontal).")]
    [SerializeField] private Image fillImage;

    [Tooltip("Optional text label, e.g. 'ENERGY' or a numeric readout.")]
    [SerializeField] private TMPro.TMP_Text label;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.8f, 1f);
    [SerializeField] private Color lowColor = new Color(1f, 0.55f, 0.1f);
    [SerializeField] private Color flashColor = new Color(1f, 0.15f, 0.15f);

    [Tooltip("Energy fraction (0..1) at or below which the bar shows the 'low' color.")]
    [Range(0f, 1f)]
    [SerializeField] private float lowEnergyThreshold = 0.2f;

    [Header("Flash Feedback")]
    [Tooltip("Total duration of the red pulse when firing is denied for low energy.")]
    [SerializeField] private float flashDuration = 0.6f;

    [Tooltip("Number of red pulses within the flash duration.")]
    [SerializeField] private int flashPulses = 3;

    private ShipStats boundStats;
    private Coroutine flashRoutine;
    private float displayedFraction;

    private void Start()
    {
        if (fillImage != null)
        {
            fillImage.color = normalColor;
        }
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void Update()
    {
        // Lazily bind once the gameplay ship exists.
        if (boundStats == null)
        {
            if (ShipStats.Primary != null)
            {
                Bind(ShipStats.Primary);
            }
            return;
        }
    }

    private void Bind(ShipStats stats)
    {
        boundStats = stats;
        boundStats.EnergyChanged += OnEnergyChanged;
        boundStats.InsufficientEnergy += OnInsufficientEnergy;
        OnEnergyChanged(boundStats.CurrentEnergy, boundStats.MaxEnergy);
    }

    private void Unbind()
    {
        if (boundStats != null)
        {
            boundStats.EnergyChanged -= OnEnergyChanged;
            boundStats.InsufficientEnergy -= OnInsufficientEnergy;
            boundStats = null;
        }
    }

    private void OnEnergyChanged(float current, float max)
    {
        displayedFraction = max > 0f ? current / max : 0f;

        if (fillImage != null)
        {
            fillImage.fillAmount = displayedFraction;

            // Don't override the color mid-flash; the flash routine restores it afterwards.
            if (flashRoutine == null)
            {
                fillImage.color = displayedFraction <= lowEnergyThreshold ? lowColor : normalColor;
            }
        }

        if (label != null)
        {
            label.text = $"ENERGY {Mathf.RoundToInt(current)}/{Mathf.RoundToInt(max)}";
        }
    }

    private void OnInsufficientEnergy()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }
        flashRoutine = StartCoroutine(FlashRed());
    }

    private IEnumerator FlashRed()
    {
        if (fillImage == null)
        {
            flashRoutine = null;
            yield break;
        }

        Color baseColor = displayedFraction <= lowEnergyThreshold ? lowColor : normalColor;
        int pulses = Mathf.Max(1, flashPulses);
        float half = flashDuration / (pulses * 2f);

        for (int i = 0; i < pulses; i++)
        {
            yield return LerpColor(baseColor, flashColor, half);
            yield return LerpColor(flashColor, baseColor, half);
        }

        fillImage.color = baseColor;
        flashRoutine = null;
    }

    private IEnumerator LerpColor(Color from, Color to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            fillImage.color = Color.Lerp(from, to, duration > 0f ? t / duration : 1f);
            yield return null;
        }
        fillImage.color = to;
    }
}
