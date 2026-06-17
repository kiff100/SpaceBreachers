using UnityEngine;

// Each HUD button gets its own action class so behavior can grow independently.
// These currently signal selection; add gameplay logic in the overrides below.

/// <summary>Tool selection button (label "1").</summary>
public class ToolSelectButtonAction : ButtonActionBase
{
    public override void OnActivated()
    {
        Debug.Log("Tool select activated");
    }
}

/// <summary>
/// Laser weapon button (label "3"). While active it suppresses the default turret fire and
/// instead drives the cargo ship's <see cref="LaserWeapon"/>: holding Fire sustains the beam,
/// releasing Fire (or deselecting the tool) stops it.
/// </summary>
public class LaserButtonAction : ButtonActionBase
{
    private readonly LaserWeapon laserWeapon;

    public LaserButtonAction(Transform targetShip)
    {
        if (targetShip != null)
        {
            laserWeapon = targetShip.GetComponentInChildren<LaserWeapon>();
            if (laserWeapon == null)
            {
                Debug.LogWarning("LaserButtonAction: no LaserWeapon found on the target ship.");
            }
        }
    }

    public override bool SuppressesFire => true;

    public override void OnActivated()
    {
        Debug.Log("Laser selected");
    }

    public override void OnDeactivated()
    {
        // Stop the beam if the tool is switched away while firing.
        laserWeapon?.StopFiring();
    }

    public override void OnFirePressed()
    {
        laserWeapon?.TryBeginFire();
    }

    public override void OnFireHeld()
    {
        laserWeapon?.TickFire();
    }

    public override void OnFireReleased()
    {
        laserWeapon?.StopFiring();
    }
}

/// <summary>Drone deployment button (label "4").</summary>
public class DroneButtonAction : ButtonActionBase
{
    public override void OnActivated()
    {
        Debug.Log("Drone selected");
    }
}

/// <summary>Spear weapon button (label "5").</summary>
public class SpearButtonAction : ButtonActionBase
{
    public override void OnActivated()
    {
        Debug.Log("Spear selected");
    }
}

/// <summary>Warp button (label "6").</summary>
public class WarpButtonAction : ButtonActionBase
{
    public override void OnActivated()
    {
        Debug.Log("Warp selected");
    }
}
