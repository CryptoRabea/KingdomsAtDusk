# Minimap Unit Marker Debug Guide

## Problem
Building markers show correctly on the minimap, but unit markers do not appear.

## Investigation Summary

I've added comprehensive debug logging to help diagnose the issue. The problem is likely one of the following:

### 1. **Fog of War Visibility (Most Likely)**
In `MiniMapController.cs` (lines 558-573), there's fog of war integration that hides enemy unit markers:
- Enemy units are only shown if they're in **currently visible** fog of war areas
- Friendly units are always visible
- Units are detected as "enemy" if they're on the "Enemy" layer

**Potential Issues:**
- Your units might be incorrectly assigned to the "Enemy" layer
- Fog of war might be hiding units even though they should be visible
- The fog of war manager might not be properly initialized

### 2. **Unit Layer Configuration**
Units are classified as enemy/friendly based on their Unity layer:
```csharp
bool isEnemy = unit.layer == LayerMask.NameToLayer("Enemy");
```

**Check:**
- What layer are your units on?
- Do you have an "Enemy" layer defined in your project?
- Are friendly units on a different layer (e.g., "Unit", "Player", "Default")?

### 3. **Which Controller is Active?**
There are two minimap controllers in the project:
- **MiniMapController** (legacy) - Has fog of war integration
- **MiniMapControllerPro** (modern) - More flexible enemy detection

**Check your scene:**
- Which controller component is attached to your minimap GameObject?
- Is it enabled?
- Are the required references set up (containers, config, etc.)?

## Debug Logs Added

I've added detailed logging to both controllers. When you run the game and spawn a unit, you should see:

### Expected Logs:

```
🚀 MiniMapController: UnitSpawnedEvent received for [UnitName] at (x, y, z)
  Unit layer: [LayerNumber], Enemy layer: [EnemyLayerNumber], isEnemy: [true/false]
🎯 MiniMapController: Creating marker for [UnitName], isEnemy=[true/false]
  Created circle marker with color RGBA(...)
  ✓ Marker created at position (x, y), active=true, parent=[ContainerName]
✅ MiniMapController: Making friendly unit [UnitName] visible on minimap
```

### If No Logs Appear:
- The UnitSpawnedEvent is not being fired
- The minimap controller is not subscribed to the event
- The minimap controller is disabled

### If Marker is Created but Not Visible:
Look for fog of war visibility logs:
```
🔍 MiniMapController: Enemy unit [UnitName] visibility changed to false, vision state: Unexplored
```

## Quick Fix Options

### Option 1: Ensure Units Are On Correct Layer
1. Select your unit prefab
2. Set layer to something other than "Enemy" (e.g., "Default", "Unit", or create a "PlayerUnit" layer)
3. Test again

### Option 2: Disable Fog of War for Unit Markers
If you want all units to show regardless of fog of war, modify `UpdateUnitMarkers()` in `MiniMapController.cs`:

```csharp
// TEMPORARY FIX: Show all units regardless of fog of war
kvp.Value.gameObject.SetActive(true);
UpdateMarkerPosition(kvp.Value, kvp.Key.transform.position);
```

### Option 3: Switch to MiniMapControllerPro
The Pro version has more flexible enemy detection methods:
1. Replace MiniMapController with MiniMapControllerPro
2. Create a MinimapConfig ScriptableObject (Right-click > Create > RTS > UI > Minimap Config)
3. Assign the config to the controller
4. Set detection method to "Tag", "Component", or "Auto"

### Option 4: Add MinimapEntity Component
Add a `MinimapEntity` component to your unit prefabs:
```csharp
var entity = unit.GetComponent<MinimapEntity>();
entity.IsEnemy = false; // Set appropriately
```

## Testing Steps

1. **Start the game**
2. **Open the Console** (Ctrl + Shift + C in Unity Editor)
3. **Spawn a unit** (train one or spawn via wave)
4. **Check the logs:**
   - Look for 🚀 "UnitSpawnedEvent received"
   - Look for 🎯 "Creating marker"
   - Look for ✅ "Making friendly unit visible"
   - Look for 🔍 "visibility changed" (fog of war)

5. **Check the Hierarchy:**
   - Expand Canvas > Minimap > UnitMarkers
   - Do you see UnitMarker_[name] GameObjects?
   - Are they active (checkbox enabled)?
   - What is their position/size?

6. **Check Layer Settings:**
   - Select a spawned unit in the hierarchy
   - What layer is it on?
   - Is it the same as the Enemy layer?

## Next Steps

After adding these debug logs, run the game and share what you see in the console. This will help us pinpoint the exact issue.

The most likely scenarios are:
1. **Units on Enemy layer + fog of war hiding them** → Change layer or disable fog of war check
2. **Wrong controller active** → Switch to correct controller
3. **Container not set up** → Assign unitMarkersContainer in inspector
4. **Event not firing** → Check if UnitSpawnedEvent is published when units spawn

## Files Modified

- `Assets/Scripts/UI/Minimap/MinimapUnitMarkerManager.cs` - Added debug logging
- `Assets/Scripts/UI/MiniMapControllerPro.cs` - Added debug logging
- `Assets/Scripts/UI/MiniMapController.cs` - Added debug logging
