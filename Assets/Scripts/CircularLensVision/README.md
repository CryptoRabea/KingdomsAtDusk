# Circular Lens Vision System

A high-performance x-ray vision system for Unity that allows seeing units through obstacles (trees, vegetation, buildings) within a circular radius, while maintaining game performance.

## Features

✅ **Circular lens-based x-ray vision** - See units through obstacles within a defined radius
✅ **Performance optimized** - Spatial partitioning, update throttling, and efficient object tracking
✅ **URP shader-based** - Leverages Universal Render Pipeline for modern rendering
✅ **Smooth transitions** - Fade in/out effects for lens activation
✅ **Fully integrated** - Works with existing fog of war and unit systems
✅ **Configurable** - Easy-to-use configuration assets and inspector controls
✅ **Debug tools** - Runtime debugging and performance monitoring

---

## Quick Start Guide

### 1. Basic Setup

#### Add CircularLensVision to Camera
1. Select your Main Camera or RTS Camera in the scene
2. Add Component → `Circular Lens Vision` → `CircularLensVision`
3. Configure the lens radius (default: 20m)
4. Set the center mode:
   - **Camera**: Follows camera position (projected to ground)
   - **Selected Unit**: Follows a specific transform
   - **Custom Position**: Manually controlled

#### Add Integration Component
1. Create an empty GameObject named "LensVisionManager"
2. Add Component → `LensVisionIntegration`
3. Enable auto-setup options:
   - ✅ Auto Setup Units
   - ✅ Auto Setup Buildings
   - ✅ Auto Setup Obstacles
4. Configure obstacle tags (e.g., "Tree", "Obstacle", "Vegetation")

#### Setup Shaders
1. Navigate to `Assets/Shaders/CircularLensVision/`
2. Create two materials:
   - **UnitXRayMaterial**: Use shader `CircularLensVision/XRayVisionUnit`
   - **ObstacleTransparentMaterial**: Use shader `CircularLensVision/TransparentObstacle`
3. Assign these materials to the LensVisionIntegration component (optional)

### 2. Manual Target Setup

If you prefer manual control over which objects participate in lens vision:

#### For Units
```csharp
// Add LensVisionTarget component to unit prefab
var target = unitPrefab.AddComponent<LensVisionTarget>();
target.SetXRayColor(new Color(0.3f, 0.7f, 1f, 0.8f)); // Cyan x-ray color
```

#### For Obstacles
```csharp
// Add LensVisionTarget component to obstacle
var target = obstacle.AddComponent<LensVisionTarget>();
target.SetTransparencyAmount(0.3f); // 30% transparent
```

### 3. Runtime Control

```csharp
// Get reference to lens controller
var lensController = FindObjectOfType<CircularLensVision>();

// Change radius
lensController.SetLensRadius(30f);

// Enable/disable lens
lensController.IsActive = true;

// Set lens center (when using CustomPosition mode)
lensController.SetLensCenter(new Vector3(10, 0, 10));

// Follow a specific unit (when using SelectedUnit mode)
lensController.SetTargetTransform(selectedUnit.transform);
```

---

## Components

### CircularLensVision
Main controller that manages the lens vision system.

**Key Settings:**
- **Lens Radius**: Size of the circular vision area
- **Center Mode**: How the lens center is determined
- **Obstacle Layers**: Which layers count as obstacles
- **Unit Layers**: Which layers count as units
- **Update Interval**: How often to update (lower = more responsive, higher = better performance)
- **Use Spatial Partitioning**: Enable grid-based optimization for many objects

### LensVisionTarget
Component attached to units and obstacles that participate in lens vision.

**Key Settings:**
- **Target Type**: Unit or Obstacle
- **Auto Register**: Automatically register with CircularLensVision on start
- **Fade Speed**: How quickly the lens effect fades in/out
- **X-Ray Color**: Color tint for units when behind obstacles (units only)
- **Transparency Amount**: How transparent obstacles become (obstacles only)

### LensVisionIntegration
Automatically sets up lens vision on spawned units and placed buildings.

**Key Settings:**
- **Auto Setup Units**: Automatically add LensVisionTarget to spawned units
- **Auto Setup Buildings**: Automatically add LensVisionTarget to placed buildings
- **Auto Setup Obstacles**: Automatically add LensVisionTarget to obstacles with specific tags
- **Player Unit X-Ray Color**: X-ray color for player units
- **Enemy Unit X-Ray Color**: X-ray color for enemy units

### LensVisionConfig (ScriptableObject)
Configuration asset for centralized settings.

**Create via:** `Assets > Create > Circular Lens Vision > Config`

### LensVisionDebug
Debug and testing tool with runtime controls and performance monitoring.

**Keyboard Controls (default):**
- **R**: Increase lens radius
- **F**: Decrease lens radius
- **T**: Toggle lens active/inactive

---

## Shaders

### XRayVisionUnit
Applied to units to enable x-ray vision when behind obstacles.

**Features:**
- Normal rendering pass (when visible)
- X-ray rendering pass (when occluded)
- Rim lighting effect for x-ray highlight
- Configurable x-ray color and intensity

**Shader Properties:**
- `_XRayColor`: Highlight color for x-ray effect
- `_XRayIntensity`: Intensity of x-ray effect
- `_RimPower`: Power of rim lighting
- `_StencilRef`: Stencil buffer reference value

### TransparentObstacle
Applied to obstacles to make them semi-transparent in lens range.

**Features:**
- Normal opaque rendering pass (when not in lens)
- Transparent rendering pass with stencil write (when in lens)
- Smooth fade between states

**Shader Properties:**
- `_TransparentColor`: Color tint when transparent
- `_TransparencyAmount`: How transparent the obstacle becomes
- `_FadeSpeed`: Speed of fade transition
- `_StencilRef`: Stencil buffer reference value

---

## Performance Optimization

### Spatial Partitioning
Enable `useSpatialPartitioning` in CircularLensVision for scenes with many objects. This uses a grid-based system to quickly find objects in lens range without checking every object.

**Recommended Settings:**
- Grid Cell Size: 10m (adjust based on lens radius)
- Max Objects Per Frame: 50 (increase if targets don't activate fast enough)

### Update Interval
Increase `updateInterval` to reduce CPU usage. Default is 0.1 seconds (10 updates/second).

**Recommended Settings:**
- Fast-paced action: 0.05s (20 updates/sec)
- Standard RTS: 0.1s (10 updates/sec)
- Slower gameplay: 0.2s (5 updates/sec)

### Layer Masks
Configure `obstacleLayers` and `unitLayers` to only check relevant objects. This significantly improves performance by skipping unnecessary collision checks.

### Material Instancing
The system uses MaterialPropertyBlock to avoid creating material instances for each object, which improves performance and reduces memory usage.

---

## Integration with Existing Systems

### Fog of War Integration
The system automatically works with your existing fog of war (csFogWar.cs). Units hidden by fog of war will not show x-ray effects.

The LensVisionIntegration component subscribes to EventBus events:
- `UnitSpawnedEvent`: Automatically adds LensVisionTarget to new units
- `BuildingPlacedEvent`: Automatically adds LensVisionTarget to new buildings
- `UnitDespawnedEvent`, `BuildingDestroyedEvent`: Automatically cleans up

### Unit Selection Integration
To follow a selected unit with the lens:

```csharp
public class UnitSelectionHandler : MonoBehaviour
{
    private CircularLensVision lensVision;

    private void Start()
    {
        lensVision = FindObjectOfType<CircularLensVision>();
    }

    private void OnUnitSelected(GameObject selectedUnit)
    {
        if (lensVision != null)
        {
            lensVision.SetTargetTransform(selectedUnit.transform);
        }
    }
}
```

---

## Troubleshooting

### Units not showing x-ray effect
1. Ensure unit has `LensVisionTarget` component with Type = Unit
2. Check that unit is within lens radius
3. Verify unit is behind an obstacle (use debug visualization)
4. Ensure unit and obstacle have colliders for Physics.OverlapSphere
5. Check that stencil reference values match (default: 1)

### Obstacles not becoming transparent
1. Ensure obstacle has `LensVisionTarget` component with Type = Obstacle
2. Check that obstacle is within lens radius
3. Verify obstacle layer is included in `obstacleLayers` mask
4. Check material is using TransparentObstacle shader

### Performance issues
1. Enable spatial partitioning
2. Increase update interval
3. Reduce max objects per frame
4. Reduce lens radius
5. Use more restrictive layer masks
6. Check for many small colliders (consolidate with compound colliders)

### Shaders not working
1. Verify project uses Universal Render Pipeline (URP)
2. Check shader compilation errors in console
3. Ensure materials are using the correct shaders
4. Try reimporting shaders

---

## Advanced Usage

### Custom Lens Shapes
To create non-circular lens shapes, extend CircularLensVision and override `FindTargetsWithBruteForce()`:

```csharp
public class RectangularLensVision : CircularLensVision
{
    public Vector2 lensSize = new Vector2(20, 30);

    private void FindTargetsWithBruteForce()
    {
        Collider[] colliders = Physics.OverlapBox(
            currentLensCenter,
            lensSize / 2f,
            Quaternion.identity
        );

        // Process colliders...
    }
}
```

### Multiple Lens Controllers
You can have multiple lens controllers in a scene (e.g., one per player unit). Each LensVisionTarget will register with all controllers automatically.

### Dynamic Material Switching
For more complex visual effects, assign custom lens materials:

```csharp
var target = unit.GetComponent<LensVisionTarget>();

// Assign custom lens materials
target.lensMaterials = new Material[] { customXRayMaterial };

// Force refresh
target.RefreshLensState();
```

---

## API Reference

### CircularLensVision

#### Properties
- `float LensRadius`: Current lens radius
- `Vector3 LensCenter`: Current lens center position
- `bool IsActive`: Whether lens is currently active

#### Methods
- `void SetLensRadius(float radius)`: Change lens radius
- `void SetLensCenter(Vector3 position)`: Set lens center (CustomPosition mode)
- `void SetTargetTransform(Transform target)`: Set target to follow (SelectedUnit mode)
- `void RegisterTarget(LensVisionTarget target)`: Manually register target
- `void UnregisterTarget(LensVisionTarget target)`: Manually unregister target

### LensVisionTarget

#### Properties
- `TargetType Type`: Whether this is a Unit or Obstacle
- `bool IsLensActive`: Whether lens effect is currently active

#### Methods
- `void SetLensActive(bool active)`: Activate/deactivate lens effect
- `void SetXRayColor(Color color)`: Change x-ray color (units only)
- `void SetTransparencyAmount(float amount)`: Change transparency (obstacles only)
- `void RefreshLensState()`: Force refresh of materials and state
- `void SetLensController(CircularLensVision controller)`: Manually set controller

---

## Credits

Created for KingdomsAtDuskU_6.3
Compatible with Unity 2023+ and Universal Render Pipeline 17.3.0+

## Support

For issues or questions, please check the troubleshooting section or contact the development team.
