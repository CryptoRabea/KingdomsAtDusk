# Fog of War System - Migration Guide

## Overview

The fog of war system has been refactored to provide a cleaner, more maintainable architecture. This guide will help you migrate from the legacy `csFogWar` system to the modern `FogOfWarManager` system.

## What Changed?

### Old Architecture (Legacy)
- **csFogWar** - Legacy fog manager from AOS Fog of War asset
- **Shadowcaster** - Legacy shadowcasting algorithm
- **FogOfWarView** - Bridged to legacy csFogWar
- Multiple boundary configurations scattered across scripts
- Tight coupling between components

### New Architecture (Modern)
- **FogOfWarManager** - Central coordinator (singleton)
- **GameBoundary** - Single source of truth for world boundaries
- **IFogRenderer** - Interface for rendering implementations
- **VisionProvider** - Component-based vision system
- **FogOfWarView** - Now bridges to FogOfWarManager
- Clean separation of concerns

## Key Benefits

1. **Single Source of Truth**: `GameBoundary` eliminates boundary configuration duplication
2. **Better Architecture**: Clear interfaces and separation of concerns
3. **Easier Testing**: Decoupled components are easier to test
4. **Better Performance**: Modern grid-based system optimized for performance
5. **Type Safety**: Uses float for world units instead of mixed int/float types
6. **Simplified Integration**: VisionProvider component auto-registers with manager

## Migration Steps

### Step 1: Update Scene Setup

#### Remove Legacy Components (Optional)
The legacy `csFogWar` system is now deprecated but still functional if you need it for backward compatibility.

To fully migrate:
1. Remove `csFogWar` component from your scene
2. Remove `Shadowcaster` references

#### Add Modern Components
1. Add `FogOfWarManager` to your scene
2. Configure `GameBoundary` in the FogOfWarConfig:
   - Set center point (world center)
   - Set size (world dimensions)
   - Set cell size (grid resolution)

### Step 2: Update FogOfWarView

`FogOfWarView` has been updated to work with `FogOfWarManager` automatically. No code changes needed!

**Before:**
```csharp
[SerializeField] private csFogWar fogWarSystem; // Legacy
```

**After:**
```csharp
[SerializeField] private FogOfWarManager fogWarManager; // Modern
```

The component will automatically:
- Find `FogOfWarManager` if not assigned
- Add `VisionProvider` components to units/buildings
- Register vision providers with the manager

### Step 3: Update Minimap Integration

`MinimapFogOfWarIntegration` has been updated to use `FogOfWarManager`.

**Before:**
```csharp
[SerializeField] private csFogWar fogWarSystem;
// Manual boundary calculations
```

**After:**
```csharp
[SerializeField] private FogOfWarManager fogWarManager;
// Uses GameBoundary automatically
```

### Step 4: Update Custom Scripts

If you have custom scripts that interact with the fog of war system:

#### Checking Visibility

**Before:**
```csharp
csFogWar fogWar = FindObjectOfType<csFogWar>();
bool isVisible = fogWar.CheckVisibility(worldPos, 0);
```

**After:**
```csharp
FogOfWarManager fogManager = FogOfWarManager.Instance;
bool isVisible = fogManager.IsVisible(worldPos);
bool isExplored = fogManager.IsExplored(worldPos);
VisionState state = fogManager.GetVisionState(worldPos);
```

#### Registering Vision Providers

**Before:**
```csharp
var fogRevealer = new csFogWar.FogRevealer(transform, sightRange, true);
int index = fogWar.AddFogRevealer(fogRevealer);
```

**After:**
```csharp
// Option 1: Let FogOfWarView handle it automatically (recommended)
// Just ensure your unit/building has the right components

// Option 2: Add VisionProvider component manually
VisionProvider visionProvider = gameObject.AddComponent<VisionProvider>();
visionProvider.SetVisionRadius(15f);
visionProvider.SetOwnerId(playerId);
visionProvider.SetActive(true);
// VisionProvider auto-registers with FogOfWarManager
```

#### Accessing Boundaries

**Before:**
```csharp
float worldMinX = fogWar._LevelMidPoint.position.x - (fogWar._UnitScale * fogWar.levelData.levelDimensionX / 2f);
float worldMaxX = fogWar._LevelMidPoint.position.x + (fogWar._UnitScale * fogWar.levelData.levelDimensionX / 2f);
```

**After:**
```csharp
GameBoundary boundary = FogOfWarManager.Instance.Boundary;
Vector3 min = boundary.Min;
Vector3 max = boundary.Max;
bool contains = boundary.Contains(worldPos);
Vector2 normalized = boundary.GetNormalizedPosition(worldPos);
```

### Step 5: Update Configuration

#### Sight Ranges

**Before:**
```csharp
public int sightRange = 10; // Grid units (confusing)
```

**After:**
```csharp
public float sightRange = 15f; // World units (clear)
```

All sight ranges are now in world units, making them consistent with Unity's coordinate system.

## Configuration Mapping

| Legacy (csFogWar) | Modern (GameBoundary) |
|-------------------|----------------------|
| `levelMidPoint` | `GameBoundary.Center` |
| `levelDimensionX` | Calculated from `GameBoundary.Width` |
| `levelDimensionY` | Calculated from `GameBoundary.Depth` |
| `unitScale` | `GameBoundary.CellSize` |
| Manual calculation | `GameBoundary.Bounds` |

## Common Issues & Solutions

### Issue: "No FogOfWarManager found"
**Solution:** Ensure you have a `FogOfWarManager` component in your scene.

### Issue: Units not revealing fog
**Solution:**
1. Check that `FogOfWarView` is in the scene
2. Ensure units have correct `ownerId` matching `FogOfWarManager.LocalPlayerId`
3. Verify units are on friendly layers (check `FogOfWarView` layer mask)

### Issue: Minimap markers not updating
**Solution:**
1. Ensure `MinimapFogOfWarIntegration` has reference to `FogOfWarManager`
2. Check that `enemyColorThreshold` is set correctly for detecting enemy markers

### Issue: Vision radius seems wrong
**Solution:** Sight ranges are now in world units, not grid units. Adjust values accordingly.
- Old: `sightRange = 10` (grid units with unitScale=2 → 20 world units)
- New: `visionRadius = 20f` (world units)

## Backward Compatibility

The legacy `csFogWar` system is marked as `[System.Obsolete]` but still functional. You can run both systems in parallel during migration if needed, but this is not recommended for production.

To remove legacy system completely:
1. Delete `/Assets/AOSFogWar/` folder (if not used elsewhere)
2. Remove all `csFogWar` and `Shadowcaster` references
3. Update scripts that use `FischlWorks_FogWar` namespace

## Testing Your Migration

1. **Visual Test**: Run the game and verify fog reveals correctly around units
2. **Minimap Test**: Check that enemy markers hide/show based on fog visibility
3. **Performance Test**: Profile to ensure performance is acceptable
4. **Boundary Test**: Move units to world edges to verify boundaries work correctly

## API Reference

### FogOfWarManager
- `Instance` - Singleton instance
- `IsVisible(Vector3)` - Check if position is currently visible
- `IsExplored(Vector3)` - Check if position has been explored
- `GetVisionState(Vector3)` - Get detailed vision state
- `Boundary` - Access game boundaries
- `ForceUpdate()` - Force immediate vision update
- `RevealAll()` / `HideAll()` - Debug commands

### GameBoundary
- `Center` - World center point
- `Size` - World dimensions
- `Min` / `Max` - Boundary extents
- `Contains(Vector3)` - Check if position is within bounds
- `GetNormalizedPosition(Vector3)` - Get 0-1 normalized position
- `GetWorldPosition(Vector2)` - Convert normalized to world position

### VisionProvider (Component)
- `SetVisionRadius(float)` - Set vision range in world units
- `SetOwnerId(int)` - Set owner/team ID
- `SetActive(bool)` - Enable/disable vision
- Auto-registers with `FogOfWarManager` on enable
- Auto-unregisters on disable/destroy

## Need Help?

If you encounter issues during migration:
1. Check Unity console for error messages
2. Review this guide's "Common Issues" section
3. Ensure all modern components are properly configured
4. Compare your setup to a working example scene

## Future Deprecation Timeline

- **Current**: Legacy system marked `[Obsolete]`, warnings in console
- **Future**: Legacy system may be removed in future updates
- **Recommendation**: Migrate to modern system as soon as possible

---

*Last Updated: 2025-12-01*
*Fog of War System Version: 2.0*
