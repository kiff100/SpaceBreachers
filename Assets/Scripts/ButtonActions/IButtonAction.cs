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
    /// and <see cref="OnFireReleased"/> is invoked on the Fire release instead.
    /// </summary>
    bool SuppressesFire { get; }

    /// <summary>Invoked when the Fire input is released while this action is active.</summary>
    void OnFireReleased();
}
