using UnityEngine;

/// <summary>
/// Contract for the behavior triggered by a single selectable HUD button.
/// The <see cref="InputManager"/> guarantees only one action is active at a time,
/// driving it identically whether triggered by a mouse click or a keyboard digit key.
/// </summary>
public interface IButtonAction
{
    /// <summary>Called when this button becomes the active selection.</summary>
    void OnActivated();

    /// <summary>Called when this button stops being the active selection.</summary>
    void OnDeactivated();

    /// <summary>
    /// When true, the default Fire/turret behavior is suppressed while this action is active,
    /// and the fire-input hooks below are invoked instead.
    /// </summary>
    bool SuppressesFire { get; }

    /// <summary>Invoked when the Fire input is first pressed while this action is active.</summary>
    void OnFirePressed();

    /// <summary>
    /// Invoked every frame while the Fire input is held down and this action is active.
    /// Used by continuous tools such as the laser to sustain their effect.
    /// </summary>
    void OnFireHeld();

    /// <summary>Invoked when the Fire input is released while this action is active.</summary>
    void OnFireReleased();
}
