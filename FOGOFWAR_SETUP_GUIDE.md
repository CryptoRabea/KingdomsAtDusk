# Fog of War Setup Guide

## Quick Diagnosis

If fog of war isn't working, follow these steps:

### 1. Add Diagnostics Script
1. Create an empty GameObject in your scene
2. Add the `FogOfWarDiagnostics` component to it
3. Right-click the component in Inspector → **"Run Full Diagnostics"**
4. Check the Console for detailed error messages

### 2. Required Components in Scene

Your scene MUST have these components:

#### FogOfWarManager
- **Location**: Should be on a GameObject (e.g., "GameManagers" or "FogOfWarSystem")
- **Settings**:
  - `World Bounds`: Should match your playable area (default: 2000x2000)
  - `Cell Size`: Default 2 units (smaller = more precise but slower)
  - `Local Player Id`: Should be 0 for single player
  - `Fog Renderer`: Assign the FogOfWarRenderer component (optional for game view fog)
  - `Minimap Renderer`: Assign the FogOfWarMinimapRenderer component

#### FogOfWarAutoIntegrator
- **Location**: Same GameObject as FogOfWarManager or separate
- **Purpose**: Automatically adds vision to spawned units
- **Settings**:
  - `Auto Add Vision To Units`: ✓ Enabled
  - `Auto Add Vision To Buildings`: ✓ Enabled
  - `Auto Add Visibility Control`: ✓ Enabled

#### FogOfWarMinimapRenderer
- **Location**: On the minimap UI GameObject
- **Settings**:
  - `Enable Minimap Fog`: ✓ Enabled
  - `Fog Overlay`: Assign the RawImage that will show the fog overlay
  - `Texture Size`: 512 (default, can reduce for performance)

#### MiniMapController
- **Location**: Should already exist for your minimap
- **Purpose**: Now includes fog of war filtering for enemy markers

### 3. Required Layers

Make sure these layers exist in **Project Settings → Tags and Layers**:

- `Enemy` - Used to identify enemy units
- `Default` - Default layer for friendly units

### 4. Unit Setup

#### Player Units
Each player unit needs:
- `VisionProvider` component (auto-added by FogOfWarAutoIntegrator)
  - `Owner Id`: 0 (player)
  - `Vision Radius`: Auto-detected from unit config or set manually
- `MinimapEntity` component (should already have this)
  - `Ownership`: Friendly

#### Enemy Units
Each enemy unit needs:
- `VisionProvider` component (auto-added by FogOfWarAutoIntegrator)
  - `Owner Id`: 1 (enemy)
  - `Vision Radius`: Auto-detected from unit config
- `FogOfWarEntityVisibility` component (auto-added)
  - `Is Player Owned`: ✗ Unchecked
  - `Hide In Explored`: ✓ Checked (standard RTS behavior)
- `MinimapEntity` component
  - `Ownership`: Enemy
- **Must be on "Enemy" layer**

### 5. Common Issues

#### Issue: Minimap is completely dark
**Cause**: No vision providers or they aren't registered
**Fix**:
1. Run diagnostics to check vision provider count
2. Make sure FogOfWarAutoIntegrator is in scene and enabled
3. Check that player units have `VisionProvider` with `OwnerId = 0`

#### Issue: Can't see any enemies even when close
**Cause**: FogOfWarEntityVisibility hiding all enemies
**Fix**:
1. Check that FogOfWarManager is initialized (see Console logs)
2. Verify vision providers are registering (enable debug logging)
3. Check that enemy units are on "Enemy" layer
4. Verify `FogOfWarEntityVisibility.isPlayerOwned = false` for enemies

#### Issue: Enemies visible on minimap everywhere
**Cause**: Fog of war filtering not working in MiniMapController
**Fix**:
1. Make sure you have the updated MiniMapController with fog filtering
2. Check that enemies are on "Enemy" layer
3. Verify FogOfWarManager.Instance exists and Grid is initialized

#### Issue: Fog of war working but minimap shows nothing
**Cause**: Fog overlay blocking everything
**Fix**:
1. Check FogOfWarMinimapRenderer is getting vision updates
2. Verify fog overlay RawImage is assigned
3. Check fog overlay alpha values (should be 0.85 for unexplored, not 1.0)

### 6. Debug Commands

#### In Inspector
Right-click on `FogOfWarManager` component:
- **Debug: Print Fog of War Status** - Shows current state
- **Debug: Reveal All** - Reveals entire map (cheat/test)
- **Debug: Hide All** - Hides entire map (reset)

Right-click on `FogOfWarDiagnostics` component:
- **Run Full Diagnostics** - Complete system check
- **List All Vision Providers** - Shows all units providing vision
- **Test Fog at Camera Position** - Check fog state where camera is looking

### 7. Expected Behavior

When working correctly:

1. **At Start**:
   - Entire minimap should be dark (but not completely black - alpha 0.85)
   - Areas around player units gradually become visible
   - Enemy units hidden

2. **As Player Units Move**:
   - Fog clears in circular areas around units (vision radius)
   - Unexplored areas: Dark overlay on minimap
   - Explored areas: Light overlay, terrain visible
   - Visible areas: Clear, all units shown

3. **Enemy Visibility**:
   - **In Game View**: Enemies only visible when in your vision
   - **On Minimap**:
     - Enemy units only shown in currently visible areas
     - Enemy buildings persist in explored areas (until fog returns)

### 8. Performance Notes

- Grid size: ~1000x1000 cells = ~2-3 MB memory, <1ms per frame
- Reduce `Texture Size` in FogOfWarMinimapRenderer if minimap lags
- Increase `Cell Size` in FogOfWarConfig for better performance (less precision)
- Adjust `Update Interval` to update fog less frequently (default 0.1s)

## Still Not Working?

Check the Console for these messages:
- `[FogOfWarManager] Starting initialization...` - Should appear at start
- `[FogOfWarManager] ✓ Initialization complete` - Confirms setup
- `[FogOfWarAutoIntegrator] ✓ Added VisionProvider` - Confirms units are being set up
- `[VisionProvider] Registered vision provider` - Confirms registration

If you don't see these messages, the system isn't initializing. Check that:
1. FogOfWarManager GameObject is active
2. FogOfWarAutoIntegrator is enabled
3. No errors in Console preventing initialization
