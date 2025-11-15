# Quick Setup Guide - Professional Minimap

## Step 1: Create Configuration Asset

1. In Unity, navigate to your Project window
2. Right-click and select: `Create > RTS > UI > Minimap Config`
3. Name it: `DefaultMinimapConfig`
4. Select the asset and configure in Inspector:

### Recommended Settings:
```
World Bounds:
  worldMin: (-1000, -1000)
  worldMax: (1000, 1000)

Render Settings:
  renderWorldMap: true
  renderTextureSize: 512
  minimapCameraHeight: 500
  minimapLayers: Everything (or specific layers)

Performance Settings:
  markerUpdateInterval: 2
  viewportUpdateInterval: 1
  maxMarkersPerFrame: 100
  enableMarkerCulling: true

Building Markers:
  friendlyBuildingColor: Blue
  buildingMarkerSize: 5
  buildingMarkerPoolSize: 50

Unit Markers:
  friendlyUnitColor: Green
  enemyUnitColor: Red
  unitMarkerSize: 3
  unitMarkerPoolSize: 200
```

## Step 2: Setup UI Hierarchy

Create this structure in your Canvas:

```
Canvas
└─ MiniMap (Panel)
    ├─ MapBackground (RawImage)
    │   • Anchor: Bottom-Right
    │   • Size: 200x200
    │   • Color: White
    │
    ├─ ViewportIndicator (Image)
    │   • Anchor: Center
    │   • Size: 20x20
    │   • Color: White (Alpha: 0.3)
    │
    ├─ BuildingMarkers (Empty RectTransform)
    │   • Stretch to fill parent
    │
    └─ UnitMarkers (Empty RectTransform)
        • Stretch to fill parent
```

### Detailed Steps:

1. **Create MiniMap Panel:**
   - Right-click Canvas → UI → Panel
   - Rename to "MiniMap"
   - Anchor to bottom-right corner
   - Set size to 200x200 pixels
   - Position: X = -110, Y = 110

2. **Create MapBackground:**
   - Right-click MiniMap → UI → Raw Image
   - Rename to "MapBackground"
   - Set anchors to stretch (fill parent)
   - Set offsets to 0

3. **Create ViewportIndicator:**
   - Right-click MiniMap → UI → Image
   - Rename to "ViewportIndicator"
   - Set size to 20x20
   - Set color to white with alpha 0.3
   - Add Outline component (optional)

4. **Create Marker Containers:**
   - Right-click MiniMap → Create Empty
   - Rename to "BuildingMarkers"
   - Add RectTransform component
   - Set anchors to stretch (0,0 to 1,1)
   - Set offsets to 0

   - Repeat for "UnitMarkers"

## Step 3: Add MiniMapControllerPro Component

1. Select the MiniMap GameObject
2. Click "Add Component"
3. Search for "MiniMapControllerPro"
4. Add the component

## Step 4: Configure Component References

In the Inspector for MiniMapControllerPro:

### Configuration:
- **Config**: Drag your `DefaultMinimapConfig` asset here

### UI References:
- **Mini Map Rect**: Drag `MiniMap` (the parent panel)
- **Mini Map Image**: Drag `MapBackground` (the RawImage)

### Camera References:
- **Mini Map Camera**: Leave empty (auto-created)
- **Camera Controller**: Drag `RTSCameraController` from scene

### Viewport Indicator:
- **Viewport Indicator**: Drag `ViewportIndicator`

### Marker Containers:
- **Building Markers Container**: Drag `BuildingMarkers`
- **Unit Markers Container**: Drag `UnitMarkers`

### Optional:
- **Building Marker Prefab**: Leave empty (uses default)
- **Unit Marker Prefab**: Leave empty (uses default)

### Performance Monitoring:
- **Show Debug Stats**: Check to see performance logs

## Step 5: Verify Setup

1. Select MiniMap GameObject
2. Right-click on MiniMapControllerPro component header
3. Select "Verify Setup" from context menu
4. Check Console for verification results

Expected output:
```
=== MiniMapControllerPro Setup Verification ===
Config: OK
MiniMap RawImage: OK
RenderTexture: OK (512x512)
MiniMap Camera: OK
Building Manager: OK
Unit Manager: OK
Buildings: 0, Units: 0
Building Markers - Friendly Pool: 0/25, Enemy Pool: 0/25
Unit Markers - Friendly Pool: 0/100, Enemy Pool: 0/100
===========================================
```

## Step 6: Test in Play Mode

1. Enter Play Mode
2. Click anywhere on the minimap
3. Camera should smoothly move to that position
4. Place buildings/spawn units
5. Markers should appear automatically

## Customization

### Custom Marker Prefabs

If you want custom marker visuals:

1. Create a prefab with an Image component
2. Assign to `buildingMarkerPrefab` or `unitMarkerPrefab`
3. The system will use your prefab instead of default squares/circles

### Adjust Performance

Edit the MinimapConfig asset:

- **Lag with many units?**
  - Increase `markerUpdateInterval` to 3-5
  - Set `maxMarkersPerFrame` to 50

- **Markers too small/large?**
  - Adjust `buildingMarkerSize` and `unitMarkerSize`

- **Different world size?**
  - Update `worldMin` and `worldMax`

### Change Colors

In MinimapConfig asset:
- `friendlyBuildingColor`: Color for your buildings
- `enemyBuildingColor`: Color for enemy buildings
- `friendlyUnitColor`: Color for your units
- `enemyUnitColor`: Color for enemy units
- `viewportColor`: Color for camera indicator

## Troubleshooting

### Issue: Minimap shows black screen
**Solution:**
- Check that MiniMap Camera is being created (look in Hierarchy)
- Verify `renderWorldMap` is enabled in config
- Check `minimapLayers` includes your game objects

### Issue: Clicking doesn't move camera
**Solution:**
- Ensure `enableClickToMove` is true in config
- Check that EventSystem exists in scene
- Verify MiniMap GameObject has correct layering (not blocked by other UI)

### Issue: Markers don't appear
**Solution:**
- Check that events are being fired (BuildingPlacedEvent, UnitSpawnedEvent)
- Verify marker containers are assigned
- Enable "Show Debug Stats" to see marker counts

### Issue: Performance drops with many units
**Solution:**
- Increase `markerUpdateInterval` to 3-5
- Set `maxMarkersPerFrame` to 50-100
- Enable `enableMarkerCulling`
- Consider reducing `unitMarkerSize` to 2

## Advanced: Using from Code

```csharp
using RTS.UI;
using UnityEngine;

public class MinimapExample : MonoBehaviour
{
    [SerializeField] private MiniMapControllerPro minimap;

    private void Start()
    {
        // Move camera to specific position
        Vector3 target = new Vector3(500, 0, -300);
        minimap.MoveCameraTo(target);

        // Convert world position to minimap position
        Vector2 minimapPos = minimap.WorldToMinimapScreen(target);
        Debug.Log($"World {target} is at minimap position {minimapPos}");

        // Get performance stats
        string stats = minimap.GetPerformanceStats();
        Debug.Log(stats);
    }
}
```

## Done! 🎉

Your professional high-performance minimap is now ready to use!

For more details, see `README.md` in the same folder.
