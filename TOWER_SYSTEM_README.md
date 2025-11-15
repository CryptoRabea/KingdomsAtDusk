# Tower System - Implementation Guide

## Overview

The tower system adds defensive structures that can automatically attack enemies. Three tower types are supported:
- **Arrow Tower**: Fast, single-target attacks
- **Fire Tower**: Area damage with burning effect (DOT)
- **Catapult Tower**: Slow, high damage with area effect

Towers can be built individually and can snap to walls, replacing them when built.

---

## Features

✅ **Three Tower Types**: Arrow, Fire, and Catapult with unique attack patterns
✅ **Automatic Targeting**: Towers automatically find and attack enemies in range
✅ **Wall Snapping**: Towers snap to nearby walls during placement
✅ **Wall Replacement**: Towers replace walls when built on them
✅ **Projectile System**: Different projectile types with visual trajectories
✅ **Damage Over Time**: Fire towers apply burning effects
✅ **Area Damage**: Fire and Catapult towers have AOE damage
✅ **Full Integration**: Works with existing building and resource systems

---

## File Structure

### Core Scripts (Assets/Scripts/RTSBuildingsSystems/)
- `TowerDataSO.cs` - ScriptableObject configuration for towers
- `Tower.cs` - Tower component (extends Building)
- `TowerCombat.cs` - Handles targeting and firing
- `TowerProjectile.cs` - Projectile behavior and damage
- `TowerPlacementHelper.cs` - Wall snapping and replacement logic

### Modified Scripts
- `BuildingManager.cs` - Updated with tower placement support

---

## Setup Instructions

### 1. Create Tower Prefabs

For each tower type (Arrow, Fire, Catapult), create a prefab:

1. Create a GameObject in your scene (e.g., "ArrowTower")
2. Add a **MeshRenderer** or model for the tower visual
3. Add a **Collider** (for building bounds)
4. Add these components:
   - `Building` component
   - `Tower` component
   - `TowerCombat` component
5. Optionally add a child GameObject for the turret (rotates toward targets)
6. Save as a prefab

### 2. Create Tower Data Assets

For each tower, create a TowerDataSO asset:

1. Right-click in Project window → **Create > RTS > TowerData**
2. Name it (e.g., "ArrowTowerData", "FireTowerData", "CatapultTowerData")
3. Configure the settings:

#### Arrow Tower Example
```
Building Name: Arrow Tower
Building Type: Defensive or Military
Tower Type: Arrow

Costs:
  Wood: 100
  Stone: 50

Combat Settings:
  Attack Range: 15
  Attack Damage: 20
  Attack Rate: 2 (attacks per second)
  Target Layers: Enemy

Projectile Settings:
  Projectile Prefab: [Arrow projectile]
  Projectile Speed: 20
  Projectile Spawn Offset: (0, 2, 0)
```

#### Fire Tower Example
```
Building Name: Fire Tower
Tower Type: Fire

Attack Damage: 15
Attack Rate: 1
Has Area Damage: ✓
AOE Radius: 3

DOT Damage: 5 (per second)
DOT Duration: 3 (seconds)
```

#### Catapult Tower Example
```
Building Name: Catapult Tower
Tower Type: Catapult

Attack Damage: 50
Attack Rate: 0.5 (slow)
Has Area Damage: ✓
AOE Radius: 5
Projectile Speed: 10 (slower for arc effect)
```

### 3. Create Projectile Prefabs

For each tower type, create a projectile prefab:

1. Create a GameObject (e.g., "Arrow", "Fireball", "Boulder")
2. Add a visual (MeshRenderer or sprite)
3. Add a **Rigidbody** (if needed)
4. Add a **Collider** with "Is Trigger" enabled
5. Add the `TowerProjectile` component
6. Save as a prefab

Assign the projectile prefab to the tower's TowerDataSO.

### 4. Setup BuildingManager

1. Select your BuildingManager GameObject in the scene
2. In the Inspector, find the **Tower Placement** section:
   - Add a `TowerPlacementHelper` component to the BuildingManager (or create a separate GameObject)
   - Assign the **Tower Placement Helper** reference
   - Enable **Tower Wall Snapping** (optional)
   - Enable **Auto Replace Walls** (optional)

3. Add your tower data assets to the **Building Data Array**

### 5. Layer Setup

Make sure you have an "Enemy" layer set up:
1. Edit → Project Settings → Tags and Layers
2. Add a layer called "Enemy"
3. Set enemy units to this layer
4. In your TowerDataSO assets, set **Target Layers** to include the Enemy layer

---

## Usage

### Building Towers

1. Call `BuildingManager.StartPlacingBuilding(towerData)` from your UI
2. The tower preview will appear
3. **If near a wall**: The tower will snap to the wall position (cyan indicator)
4. Click to place the tower
5. **If snapped to wall**: The wall will be destroyed and replaced by the tower

### Tower Behavior

Once placed and constructed:
- Towers automatically search for enemies within **Attack Range**
- When an enemy is found, the tower rotates to face it (if turret is assigned)
- The tower fires projectiles at the configured **Attack Rate**
- Projectiles deal damage on impact
- **Fire towers** apply burning DOT effects
- **Catapult/Fire towers** deal area damage on impact

---

## Configuration Reference

### TowerDataSO Properties

| Property | Description |
|----------|-------------|
| **Tower Type** | Arrow, Fire, or Catapult |
| **Attack Range** | Maximum distance to detect/attack enemies |
| **Attack Damage** | Damage dealt per hit |
| **Attack Rate** | Attacks per second |
| **Target Layers** | Which layers can be targeted (set to Enemy) |
| **Projectile Prefab** | The projectile GameObject to spawn |
| **Projectile Speed** | How fast the projectile moves |
| **Projectile Spawn Offset** | Height offset for spawning (e.g., 0,2,0) |
| **Has Area Damage** | Enable for Fire/Catapult |
| **AOE Radius** | Area of effect radius |
| **DOT Damage** | Fire tower: damage per second |
| **DOT Duration** | Fire tower: burn duration |
| **Can Replace Walls** | Can this tower be placed on walls? |
| **Wall Snap Distance** | How close to snap to walls |

---

## Code Examples

### Starting Tower Placement (from UI)
```csharp
using RTS.Buildings;
using RTS.Managers;

public class TowerBuildingUI : MonoBehaviour
{
    [SerializeField] private BuildingManager buildingManager;
    [SerializeField] private TowerDataSO arrowTowerData;

    public void OnArrowTowerButtonClicked()
    {
        buildingManager.StartPlacingBuilding(arrowTowerData);
    }
}
```

### Getting Tower Stats
```csharp
var tower = GetComponent<Tower>();
if (tower != null)
{
    Debug.Log($"Tower Type: {tower.TowerData.towerType}");
    Debug.Log($"Attack Range: {tower.Combat.AttackRange}");
    Debug.Log($"Current Target: {tower.Combat.CurrentTarget?.name}");
}
```

### Manually Setting Tower Target
```csharp
var towerCombat = GetComponent<TowerCombat>();
if (towerCombat != null)
{
    towerCombat.SetTarget(enemyTransform);
}
```

---

## Customization

### Custom Projectile Types

To create custom projectile behavior:
1. Extend `TowerProjectile` class
2. Override `Update()` or add custom logic
3. Use the `Initialize()` method to pass custom parameters

### Custom Tower Types

To add more tower types:
1. Add new values to `TowerType` enum in `TowerDataSO.cs`
2. Create TowerDataSO assets with the new type
3. Create appropriate projectile prefabs
4. Implement custom logic in `TowerProjectile` based on tower type

### Visual Effects

Add visual effects:
- **Muzzle Flash**: Instantiate VFX in `TowerCombat.SpawnProjectile()`
- **Impact Effect**: Instantiate VFX in `TowerProjectile.HitTarget()`
- **Burn Effect**: Add particle system to units with `BurnEffect` component

---

## Debugging

### Context Menu Commands

**Tower Component:**
- "Print Tower Stats" - Log tower configuration

**TowerCombat Component:**
- "Find Target Now" - Force target search
- "Fire Once" - Fire a single shot at current target

### Gizmos

When selected in editor:
- **Red sphere**: Attack range
- **Red line**: Line to current target
- **Yellow sphere**: Projectile spawn point
- **Orange sphere** (projectiles): AOE radius

### Common Issues

**Tower not attacking:**
- Check that construction is complete
- Verify enemy is on correct layer
- Check attack range (red sphere gizmo)
- Ensure projectile prefab is assigned

**Tower not snapping to walls:**
- Ensure TowerPlacementHelper is assigned to BuildingManager
- Check "Enable Tower Wall Snapping" is enabled
- Verify walls have WallConnectionSystem component

**Projectiles not dealing damage:**
- Check that TowerProjectile component is on projectile prefab
- Verify enemies have UnitHealth component
- Check target layers match enemy layers

---

## Events

The tower system publishes these events via EventBus:

- `TowerPlacedEvent` - When a tower is successfully placed
- `TowerDestroyedEvent` - When a tower is destroyed
- `BuildingPlacedEvent` - Also published for towers

Subscribe to these for UI updates, sound effects, achievements, etc.

---

## Performance Considerations

- Towers update targets every 0.5 seconds (configurable in TowerCombat)
- Use object pooling for projectiles if spawning many
- Disable turret rotation if not needed (better performance)
- Adjust attack range to balance gameplay and performance

---

## Next Steps

1. Create your tower prefabs and data assets
2. Create projectile prefabs (arrow, fireball, boulder)
3. Setup BuildingManager with TowerPlacementHelper
4. Test tower placement and combat
5. Balance tower stats (damage, range, cost)
6. Add visual effects and polish
7. Create UI buttons for tower placement

---

## Credits

Tower system integrates with:
- Existing Building system (BuildingDataSO, Building, BuildingManager)
- Wall system (WallConnectionSystem, WallPlacementController)
- Combat system (UnitHealth, UnitCombat)
- Event system (EventBus)
- Resource system (ResourceManager)

Enjoy building your tower defense!
