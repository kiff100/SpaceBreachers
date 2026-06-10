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

/// <summary>Laser weapon button (label "3").</summary>
public class LaserButtonAction : ButtonActionBase
{
    public override void OnActivated()
    {
        Debug.Log("Laser selected");
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
