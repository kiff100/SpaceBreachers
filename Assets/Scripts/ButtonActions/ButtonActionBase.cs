using UnityEngine;

/// <summary>
/// Convenience base class providing no-op defaults for <see cref="IButtonAction"/>,
/// so each concrete button action only overrides the members it actually needs.
/// </summary>
public abstract class ButtonActionBase : IButtonAction
{
    public virtual void OnActivated() { }

    public virtual void OnDeactivated() { }

    public virtual bool SuppressesFire => false;

    public virtual void OnFireReleased() { }
}
