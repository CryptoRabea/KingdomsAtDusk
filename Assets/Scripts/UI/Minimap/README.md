# Professional High-Performance Minimap System

A modular, high-performance minimap system for RTS games with object pooling, batched updates, and professional click-to-world positioning.

## Features

### Core Features
- ✅ **Real-time World Rendering** - Live view of the game world via RenderTexture
- ✅ **Click-to-World Positioning** - Accurate screen-to-world coordinate conversion with validation
- ✅ **Camera Viewport Indicator** - Shows current camera position on minimap
- ✅ **Building Markers** - Automatic tracking of friendly/enemy buildings
- ✅ **Unit Markers** - Automatic tracking of friendly/enemy units
- ✅ **Smooth Camera Movement** - Configurable animation curves

### Performance Optimizations
- 🚀 **Object Pooling** - Reuses marker GameObjects (reduces GC pressure)
- 🚀 **Batched Updates** - Configurable marker update rates
- 🚀 **Marker Culling** - Hides off-screen markers automatically
- 🚀 **Configurable Update Intervals** - Update markers every N frames
- 🚀 **Max Markers Per Frame** - Limit updates for stable framerate
- 🚀 **Modular Architecture** - Separate managers for different marker types

### Professional Features
- 🎨 **ScriptableObject Configuration** - Easy tuning without code changes
- 🎨 **Separate Marker Managers** - Building and unit markers managed independently
- 🎨 **Debug Statistics** - Real-time performance monitoring
- 🎨 **Pool Statistics** - Track object pool usage
- 🎨 **Verification Tools** - Context menu setup verification

## Architecture

### Components

1. **MiniMapControllerPro** - Main controller managing the minimap
   - Located at: `Assets/Scripts/UI/MiniMapControllerPro.cs`
   - Handles rendering, input, and coordinates marker managers

2. **MinimapConfig** - ScriptableObject configuration
   - Located at: `Assets/Scripts/UI/Minimap/MinimapConfig.cs`
   - Centralizes all minimap settings

3. **MinimapMarkerPool** - Generic object pool for markers
   - Located at: `Assets/Scripts/UI/Minimap/MinimapMarkerPool.cs`
   - Manages marker lifecycle and reuse

4. **MinimapMarkerManager** - Base class for marker managers
   - Located at: `Assets/Scripts/UI/Minimap/MinimapMarkerManager.cs`
   - Provides common marker management functionality

5. **MinimapBuildingMarkerManager** - Building marker manager
   - Located at: `Assets/Scripts/UI/Minimap/MinimapBuildingMarkerManager.cs`
   - Manages building markers with pooling

6. **MinimapUnitMarkerManager** - Unit marker manager
   - Located at: `Assets/Scripts/UI/Minimap/MinimapUnitMarkerManager.cs`
   - Manages unit markers with pooling

## Setup Instructions

### 1. Create Minimap Configuration

1. Right-click in Project window
2. Select `Create > RTS > UI > Minimap Config`
3. Name it "MinimapConfig"
4. Configure settings in Inspector:
   - World bounds
   - Performance settings
   - Visual customization

### 2. Setup Minimap in Scene

1. Add MiniMapControllerPro component to Canvas
2. Assign the MinimapConfig ScriptableObject
3. Create UI hierarchy:
   ```
   Canvas
   └─ MiniMap (Panel)
       ├─ MapBackground (RawImage)
       ├─ ViewportIndicator (Image)
       ├─ BuildingMarkers (Empty GameObject)
       └─ UnitMarkers (Empty GameObject)
   ```

4. Assign references in Inspector:
   - Mini Map Rect: MiniMap RectTransform
   - Mini Map Image: MapBackground RawImage
   - Viewport Indicator: ViewportIndicator RectTransform
   - Building Markers Container: BuildingMarkers RectTransform
   - Unit Markers Container: UnitMarkers RectTransform
   - Camera Controller: RTSCameraController in scene

### 3. Verify Setup

1. Select MiniMapControllerPro in Hierarchy
2. Right-click component header
3. Select "Verify Setup"
4. Check Console for verification results

## Configuration Options

### World Bounds
- `worldMin`: Minimum world coordinates (X, Z)
- `worldMax`: Maximum world coordinates (X, Z)

### Performance Settings
- `markerUpdateInterval`: Update markers every N frames (1-10)
  - **1** = Every frame (high CPU, smooth updates)
  - **2** = Every other frame (balanced)
  - **5** = Every 5 frames (low CPU, slightly choppy)

- `maxMarkersPerFrame`: Maximum markers to update per frame (0 = unlimited)
  - **0** = Update all (use for <100 markers)
  - **100** = Good for 500+ markers
  - **50** = Good for 1000+ markers

- `enableMarkerCulling`: Hide markers outside visible area
  - Recommended: **true** for 200+ markers

### Visual Settings
- Building marker size, colors
- Unit marker size, colors
- Viewport indicator color
- Background color

### Camera Movement
- `cameraMoveSpeed`: Movement speed multiplier
- `useSmoothing`: Enable smooth movement
- `movementCurve`: Animation curve for movement
- `minMoveDuration`: Minimum movement time
- `maxMoveDuration`: Maximum movement time

## Usage Examples

### Click-to-World Position
```csharp
// Automatically handled by OnPointerClick
// Users click minimap -> Camera moves to world position
```

### Manual Camera Movement
```csharp
MiniMapControllerPro minimap = GetComponent<MiniMapControllerPro>();
Vector3 targetPosition = new Vector3(100, 0, 200);
minimap.MoveCameraTo(targetPosition);
```

### Convert World to Minimap Position
```csharp
Vector3 worldPos = new Vector3(500, 0, -500);
Vector2 minimapPos = minimap.WorldToMinimapScreen(worldPos);
// Returns local position on minimap UI
```

### Convert Screen to World Position
```csharp
Vector2 screenPos = Input.mousePosition;
Vector3 worldPos = minimap.ScreenToWorldPosition(screenPos, eventCamera);
// Returns world position, or Vector3.zero if invalid
```

### Get Performance Statistics
```csharp
string stats = minimap.GetPerformanceStats();
Debug.Log(stats);
// Output: "Buildings: 25, Units: 150"
//         "Building Pool: 25/50"
//         "Unit Pool: 150/200"
```

## Performance Recommendations

### Small Scale (< 100 total markers)
```
markerUpdateInterval: 1
maxMarkersPerFrame: 0
enableMarkerCulling: false
```

### Medium Scale (100-500 markers)
```
markerUpdateInterval: 2
maxMarkersPerFrame: 100
enableMarkerCulling: true
```

### Large Scale (500-1000+ markers)
```
markerUpdateInterval: 3-5
maxMarkersPerFrame: 50-100
enableMarkerCulling: true
unitMarkerSize: 2-3 (smaller markers)
```

## Event Integration

The minimap automatically listens to these events:
- `BuildingPlacedEvent` → Creates building marker
- `BuildingDestroyedEvent` → Removes building marker
- `UnitSpawnedEvent` → Creates unit marker
- `UnitDiedEvent` → Removes unit marker

No additional setup required - markers are created/removed automatically!

## Comparison: Original vs Pro

| Feature | MiniMapController | MiniMapControllerPro |
|---------|------------------|----------------------|
| Object Pooling | ❌ No | ✅ Yes |
| Batched Updates | ❌ No | ✅ Yes |
| Marker Culling | ❌ No | ✅ Yes |
| Configurable Perf | ❌ No | ✅ Yes |
| Modular Architecture | ❌ No | ✅ Yes |
| ScriptableObject Config | ❌ No | ✅ Yes |
| GC Friendly | ⚠️ Medium | ✅ High |
| Max Recommended Markers | ~100 | 1000+ |

## Troubleshooting

### Minimap is black
- Check that render texture is assigned
- Verify camera layers match minimap layers
- Check camera is enabled

### Markers not appearing
- Verify containers are assigned
- Check event system is working
- Enable debug stats to see marker counts

### Poor performance with many units
- Increase `markerUpdateInterval` to 3-5
- Set `maxMarkersPerFrame` to 50-100
- Enable `enableMarkerCulling`
- Reduce `unitMarkerSize`

### Click not working
- Check `enableClickToMove` is true
- Verify EventSystem exists in scene
- Check minimap has PointerClick event configured

## Migration from Original MiniMapController

1. Replace MiniMapController with MiniMapControllerPro
2. Create MinimapConfig asset
3. Configure settings in MinimapConfig
4. Reassign UI references
5. Test and tune performance settings

Both controllers can coexist - you can use Pro for new scenes and keep the original for existing ones.

## License

Part of Kingdoms at Dusk RTS Game
