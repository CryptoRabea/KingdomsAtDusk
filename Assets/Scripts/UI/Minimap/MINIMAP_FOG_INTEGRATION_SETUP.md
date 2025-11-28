# Minimap Fog of War Integration Setup

This guide explains how to integrate the minimap with the fog of war system so that enemy markers only appear in visible areas.

## Problem

By default, enemy unit and building markers show on the minimap everywhere, even in unexplored or explored (but not currently visible) areas. This breaks the fog of war gameplay mechanic.

## Solution

The `MinimapMarkerFogIntegration` component filters minimap markers based on fog of war visibility state:

- **Friendly units/buildings**: Always visible on minimap
- **Enemy units/buildings**: Only visible in areas with active vision
- **Explored areas**: Configurable via `hideInExplored` setting

## Setup Instructions

### 1. Add the Component

1. Find your `MiniMapControllerPro` GameObject in the scene
2. Add the `MinimapMarkerFogIntegration` component to it
   - Click "Add Component" in the Inspector
   - Search for "MinimapMarkerFogIntegration"
   - Or manually add `RTS.UI.Minimap.MinimapMarkerFogIntegration`

### 2. Configure Settings

In the Inspector for `MinimapMarkerFogIntegration`:

#### **Settings**
- **Hide In Explored**: ✓ (Checked - recommended)
  - When checked: Enemy markers only show in actively visible areas
  - When unchecked: Enemy markers show in explored areas too (less realistic)

- **Update Interval**: 2 (default)
  - How many frames between visibility updates
  - Higher = better performance, lower = more responsive
  - Range: 1-10

#### **Enemy Detection**
- **Enemy Color Threshold**: 0.2 (default)
  - How to detect enemy markers by color
  - Enemy markers are typically red (high R value)
  - Range: 0-1

#### **Debug** (optional)
- **Show Debug Logs**: Unchecked (unless debugging)
- **Show Visibility Stats**: Unchecked (unless debugging)

### 3. Verify Fog of War System

Make sure your scene has:
- `FogOfWarManager` GameObject with the `FogOfWarManager` component
- `FogOfWarRenderer` component (for world fog rendering)
- `FogOfWarMinimapRenderer` component (for minimap fog overlay)

### 4. Test

1. **Play the scene**
2. **Place enemy units** in different locations
3. **Verify behavior**:
   - Enemy markers should NOT appear in unexplored (black) areas
   - Enemy markers should NOT appear in explored (dimmed) areas if "Hide In Explored" is checked
   - Enemy markers SHOULD appear in currently visible (clear) areas
   - Friendly markers should ALWAYS appear

## How It Works

1. Every N frames (based on `updateInterval`), the script scans all minimap markers
2. For each marker:
   - Checks if it's an enemy marker (by color)
   - Converts minimap position to world position
   - Queries `FogOfWarManager` for the vision state at that position
   - Shows/hides the marker based on visibility rules

3. Visibility rules:
   - **VisionState.Visible**: Show marker
   - **VisionState.Explored**: Show only if `hideInExplored` is false
   - **VisionState.Unexplored**: Always hide

## Performance

- Uses object pooling (markers are deactivated, not destroyed)
- Batched updates (configurable interval)
- Cached visibility states (only updates when changed)
- Typical overhead: <1% CPU with 200+ markers

## Troubleshooting

### Enemy markers still showing everywhere

**Possible causes:**
1. `MinimapMarkerFogIntegration` component not attached
   - **Fix**: Add the component to `MiniMapControllerPro` GameObject

2. `FogOfWarManager` not found in scene
   - **Fix**: Check console for warnings, ensure FogOfWarManager is in the scene

3. Enemy color detection not working
   - **Fix**: Adjust `enemyColorThreshold` or verify enemy markers are red

### Markers flickering

**Cause**: Update interval too high or minimap markers being recreated frequently

**Fix**:
- Lower `updateInterval` to 1 or 2
- Ensure minimap is using object pooling correctly

### Performance issues

**Cause**: Too many markers updating too frequently

**Fix**:
- Increase `updateInterval` to 3-5
- Reduce number of units/buildings
- Check fog of war update interval in `FogOfWarConfig`

## Integration with Existing Systems

This script works alongside:
- `MinimapFogOfWarIntegration` (for old csFogWar system) - disable if using FogOfWarManager
- `FogOfWarEntityVisibility` (for hiding 3D models in fog)
- `FogOfWarMinimapRenderer` (for fog overlay on minimap)

**Note**: If you have both `MinimapFogOfWarIntegration` (old system) and `MinimapMarkerFogIntegration` (new system), disable the one you're not using to avoid conflicts.

## API

### Public Methods

```csharp
// Change hide in explored setting at runtime
minimapFogIntegration.SetHideInExplored(true);

// Force immediate visibility update
minimapFogIntegration.ForceUpdateVisibility();

// Clear visibility cache (when resetting minimap)
minimapFogIntegration.ClearVisibilityCache();
```

### Context Menu (Editor Only)

Right-click the component in Inspector:
- **Force Update Visibility**: Manually trigger update
- **Print Visibility Stats**: Show marker counts in console
- **Toggle Hide In Explored**: Toggle setting for testing

## Related Files

- **Script**: `Assets/Scripts/UI/Minimap/MinimapMarkerFogIntegration.cs`
- **Minimap Controller**: `Assets/Scripts/UI/MiniMapControllerPro.cs`
- **Fog of War Manager**: `Assets/Scripts/FogOfWar/FogOfWarManager.cs`
- **Entity Visibility**: `Assets/Scripts/FogOfWar/FogOfWarEntityVisibility.cs`

## See Also

- `FOG_OF_WAR_QUICKSTART.md` - Fog of war system setup
- `Assets/Scripts/UI/Minimap/SETUP_GUIDE.md` - Minimap setup guide
- `FOGOFWAR_SETUP_GUIDE.md` - Complete fog of war guide
