# Project Overview
- Game Title: Space Breachers
- High-Level Concept: Space exploration/combat game with a focus on ship-to-ship boarding and weapon systems.
- Players: Single player
- Target Platform: PC (StandaloneWindows64)
- Render Pipeline: URP (PC_RPAsset)
- Input System: New Input System

# Game Mechanics
## Core Gameplay Loop
The player controls a cargo ship or interacts with it via a HUD. The laser weapon is one of the tools available to the player to damage enemies or clear obstacles.

## Controls and Input Methods
- **Selection**: Keyboard digit keys (1-9) or UI clicks to select active tools.
- **Laser (Key 3)**: Once selected, holding the "Fire" button (Left Mouse Button) activates the laser.
- **Aiming**: The laser fires from the ship towards the mouse position in world space.

# UI
- **HUD (CanvasOverlay)**: Located in `PersistentUI.unity`.
- **Energy Meter**: A new horizontal bar at the top of the screen displaying current energy / max energy.
- **Visual Feedback**: The meter pulses red when the player attempts to fire below the 20% energy threshold.

# Key Asset & Context
- `CargoShip`: The main vessel where the laser originates.
- `LaserWeapon.cs`: New component to handle raycasting, damage falloff, and visuals.
- `ShipStats.cs`: New component to manage energy levels and regeneration.
- `Health.cs`: New component/interface for damageable objects.
- `IButtonAction`: Interface to be updated to support continuous fire logic.

# Implementation Steps
## 1. Damage & Stats Foundation
- **Description**: Create the core health and energy systems.
- **Files**:
    - Create `Assets/Scripts/IDamageable.cs` (Interface).
    - Create `Assets/Scripts/Health.cs` (Component implementing `IDamageable`).
    - Create `Assets/Scripts/ShipStats.cs` (Component for energy management, including regeneration and drain).
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

## 2. Update Input & Action System
- **Description**: Enhance `IButtonAction` and `InputManager` to support "Fire Pressed" and "Update" events for continuous actions like a laser.
- **Files**:
    - Modify `Assets/Scripts/ButtonActions/IButtonAction.cs` and `ButtonActionBase.cs`.
    - Modify `Assets/Scripts/InputManager.cs` to call `OnFirePressed` and `Update` on the active action.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

## 3. Laser Weapon Logic
- **Description**: Implement the laser firing logic, raycasting, damage falloff (Damage per second), and visual `LineRenderer`. Includes "sputter" flickering effect when energy is depleted.
- **Files**:
    - Create `Assets/Scripts/LaserWeapon.cs`.
    - Update `Assets/Scripts/ButtonActions/WeaponButtonActions.cs` (`LaserButtonAction`) to trigger the `LaserWeapon`.
- **Assigned role**: developer
- **Dependencies**: Step 1, Step 2
- **Parallelizable**: No

## 4. UI Implementation
- **Description**: Add the energy meter to the HUD and connect it to the ship's energy state. Implement red pulse feedback.
- **Files**:
    - Modify `Assets/Scenes/PersistentUI.unity`.
    - Create `Assets/Scripts/UI/EnergyBarUI.cs`.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: Yes

## 5. Scene Setup & Integration
- **Description**: Attach new components to the `CargoShip` and `EnemyShip` prefabs/objects. Configure settings (max distance, damage, energy costs).
- **Files**:
    - Modify `Assets/Scenes/CollectionTest.unity` (or relevant prefabs).
- **Assigned role**: developer
- **Dependencies**: Step 3, Step 4
- **Parallelizable**: No

# Verification & Testing
- **Laser Activation**: Verify selecting slot 3 enables laser mode.
- **Firing**: Verify holding Fire displays the laser line and drains energy.
- **Energy Drain**: Verify laser sputters (flickers) and turns off when energy hits 0.
- **Energy Regeneration**: Verify energy refills over time when not firing.
- **Threshold Check**: Verify laser cannot fire below 20% energy and UI pulses red.
- **Damage**: Verify objects with `Health` component take damage per second.
- **Damage Falloff**: Verify damage is lower at the maximum range of the laser.
- **Friendly Fire**: Verify the `CargoShip` itself is never damaged by its own laser.
