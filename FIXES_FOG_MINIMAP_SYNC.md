# Fog of War and Minimap Synchronization Fixes

## Issues Fixed

### 1. Fog of War Not Centered on Revealers
**Problem**: The fog of war was revealing with a Y-axis offset, not properly centered on revealer positions.

**Root Cause**: The minimap camera's orthographic size was only accounting for one dimension (Z/depth), causing misalignment when the world bounds were non-square.

**Fix**: Updated `MiniMapControllerPro.SetupMiniMapCamera()` to calculate orthographic size using the maximum of both world dimensions:
```csharp
miniMapCamera.orthographicSize = Mathf.Max(worldWidth, worldDepth) / 2f;
```

### 2. Minimap Markers Not Accurate for Buildings and Units
**Problem**: Building and unit markers appeared offset from their actual world positions on the minimap.

**Root Cause**: The marker positioning system used configured world bounds (`worldMin`/`worldMax`) but the camera's actual view could be larger, causing coordinate mismatch.

**Fixes**:
- Added `GetCameraViewBounds()` method to calculate the actual world space visible by the minimap camera
- Updated `WorldToMinimapPosition()` in `MinimapMarkerManager` to use actual camera view bounds
- Added `SetCameraViewBounds()` method to allow marker managers to sync with camera setup
- Updated `MiniMapControllerPro.Awake()` to propagate camera bounds to marker managers

### 3. Mouse Click on Minimap Not Accurate
**Problem**: Clicking on the minimap didn't move the camera to the correct world position.

**Root Cause**: Same as issue #2 - the click-to-world conversion used configured bounds instead of actual camera view.

**Fix**: Updated `ScreenToWorldPosition()` to use `GetCameraViewBounds()` for accurate coordinate conversion:
```csharp
Bounds cameraBounds = GetCameraViewBounds();
Vector3 worldPos = new Vector3(
    Mathf.Lerp(cameraBounds.min.x, cameraBounds.max.x, normalizedPos.x),
    cameraController != null ? cameraController.transform.position.y : 0f,
    Mathf.Lerp(cameraBounds.min.z, cameraBounds.max.z, normalizedPos.y)
);
```

### 4. Minimap and Fog of War Not Synchronized
**Problem**: The minimap markers, fog reveals, and mouse clicks were all out of sync.

**Root Cause**: Inconsistent coordinate systems - fog of war uses discrete grid cells while minimap uses continuous coordinates, and the camera view didn't match configured world bounds.

**Solution**: Unified all systems to use the same camera view bounds calculated from the orthographic camera's actual field of view.

## Modified Files

1. **Assets/Scripts/UI/MiniMapControllerPro.cs**
   - Updated `SetupMiniMapCamera()` to properly calculate orthographic size
   - Added `GetCameraViewBounds()` helper method
   - Updated `ScreenToWorldPosition()` to use camera bounds
   - Updated `WorldToMinimapScreen()` to use camera bounds
   - Updated `Awake()` to set camera bounds on marker managers

2. **Assets/Scripts/UI/Minimap/MinimapMarkerManager.cs**
   - Added `cachedCameraBounds` field
   - Added `SetCameraViewBounds()` method
   - Updated `WorldToMinimapPosition()` to use camera bounds when available

## Technical Details

### Coordinate System Alignment

The fix ensures all systems use the same coordinate space:

1. **Minimap Camera**: Orthographic camera looking down (90° X rotation)
   - Vertical extent (world Z): `orthographicSize * 2`
   - Horizontal extent (world X): `orthographicSize * aspect * 2`
   - For square textures: aspect = 1.0

2. **Camera View Bounds**: Calculated from orthographic size
   - Width (X): `orthographicSize * 2`
   - Depth (Z): `orthographicSize * 2`
   - Centered at `config.WorldCenter`

3. **Marker Positioning**: Uses camera view bounds
   - Normalized X: `InverseLerp(bounds.min.x, bounds.max.x, worldPos.x)`
   - Normalized Z: `InverseLerp(bounds.min.z, bounds.max.z, worldPos.z)`

4. **Click Positioning**: Uses camera view bounds
   - World X: `Lerp(bounds.min.x, bounds.max.x, normalizedPos.x)`
   - World Z: `Lerp(bounds.min.z, bounds.max.z, normalizedPos.y)`

### Handling Non-Square Worlds

When the world is not square (e.g., 2000x1000):
- Orthographic size is set to the larger dimension / 2
- This ensures the entire world is visible
- Camera shows: `max(width, depth)` in both dimensions
- Coordinate conversion accounts for the extra visible space

## Testing Recommendations

1. **Verify fog reveals are centered** on units and buildings
2. **Check minimap markers** align with unit/building positions in game world
3. **Test mouse clicks** move camera to correct positions
4. **Test with non-square worlds** (different width/depth values)
5. **Verify synchronization** between all three systems

## Configuration Requirements

Ensure `MinimapConfig` world bounds are properly configured:
- `worldMin` and `worldMax` should match or encompass the fog of war area
- For best results, use the same center point and dimensions as fog of war system
- The camera will automatically adjust to show the larger dimension

## Notes

- All coordinate conversions now use the camera's actual view bounds
- System is resilient to non-square world configurations
- Fallback to config bounds if camera not yet initialized
- Compatible with existing fog of war and minimap systems
