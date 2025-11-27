# Top-Down Wall Building System

## Description
A complete pole-to-pole wall building system designed for top-down games and RTS projects. Features intelligent mesh fitting, automatic segmentation, overlap detection, and wall snapping.

## Category
Gameplay Systems / Building Systems

## Features
- ✅ **Pole-to-Pole Placement**: Click two points to create a wall line
- ✅ **Perfect Mesh Fitting**: Automatically detects mesh dimensions and places segments end-to-end with NO gaps
- ✅ **Intelligent Scaling**: Last segment scales to fill remaining distance perfectly
- ✅ **Overlap Detection**: Prevents walls from overlapping buildings or other walls
- ✅ **Endpoint Snapping**: Automatically snaps to nearby wall endpoints and midpoints
- ✅ **Loop Closure**: Allows closing wall perimeters by connecting back to the first pole
- ✅ **Resource Cost Preview**: Shows segment count and total cost in real-time
- ✅ **Visual Feedback**: Green/red preview materials, line renderer, and affordability indicators
- ✅ **Wall Connection System**: Automatic neighbor detection and connection tracking
- ✅ **NavMesh Integration**: Automatic NavMeshObstacle setup for AI pathfinding
- ✅ **Stair/Ramp Support**: Optional NavMeshLink-based traversal between ground and wall tops
- ✅ **Editor Tools**: Visual editors and utilities for wall prefab setup

## Installation

### Via Unity Package Manager
1. Open Unity Package Manager (Window > Package Manager)
2. Click the '+' button and select 'Add package from disk...'
3. Navigate to the package.json file in this directory

### Via Direct Import
1. Copy the entire package folder to your project's Assets directory
2. Unity will automatically import the scripts

## Requirements
- Unity 2021.3+
- Unity Input System 1.4.4+

## Dependencies
This package is **standalone** and self-contained. It includes all necessary core systems:
- Service Locator (for dependency injection)
- Event Bus (for decoupled communication)
- Resource Management Interface (implement in your game)

### Unity Package Dependencies
- `com.unity.inputsystem`: 1.4.4

## Quick Start

### 1. Implement the Resource Service

The wall system needs a resource service to check costs and spend resources. Implement the `IResourcesService` interface in your game:

```csharp
using TopDownWallBuilding.Core.Services;
using System.Collections.Generic;

public class MyResourceManager : MonoBehaviour, IResourcesService
{
    private Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>
    {
        { ResourceType.Wood, 1000 },
        { ResourceType.Stone, 500 },
        { ResourceType.Gold, 200 },
        { ResourceType.Food, 300 }
    };

    public int GetResource(ResourceType type) => resources.GetValueOrDefault(type, 0);

    public bool CanAfford(Dictionary<ResourceType, int> costs)
    {
        foreach (var cost in costs)
        {
            if (GetResource(cost.Key) < cost.Value)
                return false;
        }
        return true;
    }

    public bool SpendResources(Dictionary<ResourceType, int> costs)
    {
        if (!CanAfford(costs)) return false;

        foreach (var cost in costs)
        {
            resources[cost.Key] -= cost.Value;
        }
        return true;
    }

    public void AddResources(Dictionary<ResourceType, int> amounts)
    {
        foreach (var amount in amounts)
        {
            resources[amount.Key] = resources.GetValueOrDefault(amount.Key, 0) + amount.Value;
        }
    }

    // Legacy properties
    public int Wood => GetResource(ResourceType.Wood);
    public int Food => GetResource(ResourceType.Food);
    public int Gold => GetResource(ResourceType.Gold);
    public int Stone => GetResource(ResourceType.Stone);
}
```

### 2. Register the Service

Register your resource manager at startup:

```csharp
using TopDownWallBuilding.Core.Services;

public class GameInitializer : MonoBehaviour
{
    [SerializeField] private MyResourceManager resourceManager;

    private void Awake()
    {
        ServiceLocator.Register<IResourcesService>(resourceManager);
    }
}
```

### 3. Create a Wall Data Asset

1. Right-click in Project window → Create → Top-Down Wall Building → Wall Data
2. Configure the wall:
   - Name: "Stone Wall"
   - Assign your wall prefab
   - Set resource costs (e.g., Wood: 10, Stone: 5)
   - Set construction time

### 4. Setup the Wall Placement Controller

1. Create an empty GameObject in your scene (e.g., "WallPlacementSystem")
2. Add the `WallPlacementController` component
3. Configure settings:
   - **Main Camera**: Assign your main camera
   - **Ground Layer**: Set the layer mask for ground detection
   - **Preview Materials**: Assign green (valid) and red (invalid) materials
   - **Wall Length Axis**: Choose X, Y, or Z based on your wall mesh orientation

### 5. Start Placing Walls

Call this from a UI button or input handler:

```csharp
using TopDownWallBuilding.WallSystems;

public class BuildingUI : MonoBehaviour
{
    [SerializeField] private WallPlacementController wallPlacer;
    [SerializeField] private BuildingDataSO stoneWallData;

    public void OnWallButtonClicked()
    {
        wallPlacer.StartPlacingWalls(stoneWallData);
    }
}
```

## How It Works

### Pole-to-Pole Placement
1. User clicks to place first pole
2. User clicks to place second pole
3. System calculates wall segments between poles:
   - Detects wall mesh length automatically
   - Places full-size segments end-to-end
   - Scales last segment to fill remaining distance perfectly (min 30% scale)
4. Walls are instantiated with proper scaling

### Overlap Detection
- Uses capsule collision detection along wall path
- Allows endpoint connections (walls can touch at ends)
- Blocks body overlaps (walls can't pass through each other)
- Prevents building over existing buildings

### Wall Snapping
- Detects nearby wall endpoints within snap distance (default: 2m)
- Snaps cursor to wall endpoints and midpoints
- Shows cyan preview when snapped
- Allows closing wall loops by snapping back to first pole

### Resource Cost Calculation
- Calculates number of segments needed
- Multiplies single wall cost by segment count
- Shows real-time preview with green/red affordability indicator
- Prevents placement if resources insufficient

## Configuration

### Wall Placement Controller Settings

**References:**
- `mainCamera`: Camera used for raycasting
- `groundLayer`: Layer mask for ground detection

**Visual Settings:**
- `validPreviewMaterial`: Green material shown when placement is valid
- `invalidPreviewMaterial`: Red material shown when placement is invalid
- `linePreviewRenderer`: Line renderer between poles (auto-created if null)
- `lineWidth`: Width of preview line (default: 0.2)
- `validLineColor`: Green line color
- `invalidLineColor`: Red line color

**Placement Settings:**
- `useGridSnapping`: Enable grid snapping (default: false)
- `gridSize`: Grid cell size if snapping enabled (default: 1.0)
- `wallSnapDistance`: Distance to snap to nearby walls (default: 2.0)
- `autoCompleteOnSnap`: Auto-cancel placement when closing a loop (default: true)
- `minParallelOverlap`: Minimum overlap to consider collinear walls overlapping (default: 0.5)

**Mesh-Based Placement:**
- `minScaleFactor`: Minimum scale for last segment (default: 0.3 = 30%)
- `useAutoMeshSize`: Auto-detect mesh length from bounds (default: true)
- `wallLengthAxis`: Axis representing wall length (X, Y, or Z - default: X)

**Pole Settings:**
- `polePrefab`: Optional visual pole prefab (uses cylinder if null)
- `poleHeight`: Height of pole markers (default: 2.0)

## Events

The system publishes events you can subscribe to:

```csharp
using TopDownWallBuilding.Core.Events;

// Wall placed
EventBus.Subscribe<BuildingPlacedEvent>(OnWallPlaced);

// Wall construction completed
EventBus.Subscribe<BuildingCompletedEvent>(OnWallCompleted);

// Wall destroyed
EventBus.Subscribe<BuildingDestroyedEvent>(OnWallDestroyed);

// Placement failed
EventBus.Subscribe<BuildingPlacementFailedEvent>(OnPlacementFailed);

// Resources spent
EventBus.Subscribe<ResourcesSpentEvent>(OnResourcesSpent);

// Remember to unsubscribe!
private void OnDestroy()
{
    EventBus.Unsubscribe<BuildingPlacedEvent>(OnWallPlaced);
    // ... unsubscribe all
}
```

## API Reference

### WallPlacementController

```csharp
// Start wall placement mode
public void StartPlacingWalls(BuildingDataSO wallData)

// Cancel current placement
public void CancelWallPlacement()

// Check if currently placing walls
public bool IsPlacingWalls { get; }

// Get total cost for current preview
public Dictionary<ResourceType, int> GetTotalCost()

// Get number of segments for current preview
public int GetRequiredSegments()
```

### BuildingDataSO

```csharp
// Get costs as dictionary
public Dictionary<ResourceType, int> GetCosts()

// Get formatted cost string for UI
public string GetCostString() // Returns "Wood: 100, Stone: 50"
```

### WallConnectionSystem

```csharp
// Update connections with nearby walls
public void UpdateConnections()

// Get number of connected walls
public int GetConnectionCount()

// Get list of connected walls
public List<WallConnectionSystem> GetConnectedWalls()

// Check if connected to specific wall
public bool IsConnectedTo(WallConnectionSystem otherWall)

// Get direction to connected wall
public Vector3 GetConnectionDirection(WallConnectionSystem otherWall)

// Static methods
public static List<WallConnectionSystem> GetAllWalls()
public static void ClearAllWalls()
```

## Editor Tools

### Wall Prefab Setup Utility
Access via: **Tools → TopDownWallBuilding → Setup Wall Prefab**

Helps you quickly set up wall prefabs with proper components:
- Adds required components (Building, WallConnectionSystem)
- Creates mesh variant containers
- Configures connection settings

### Wall Connection System Editor
Custom inspector for WallConnectionSystem with:
- Visual connection state guide
- Runtime connection info
- Force update button
- Connection distance visualization

## Best Practices

1. **Wall Mesh Setup**:
   - Orient wall mesh along X-axis for best results
   - Set `wallLengthAxis` to match your mesh orientation
   - Ensure mesh pivot is centered

2. **Resource Costs**:
   - Balance costs to make wall-building strategic
   - Consider wall length when setting costs

3. **Prefab Structure**:
   ```
   WallPrefab
   ├── Building (component)
   ├── WallConnectionSystem (component)
   ├── WallNavMeshObstacle (component)
   ├── Collider (auto-disabled after placement)
   └── Visual Mesh
   ```

4. **Performance**:
   - Wall colliders are disabled after placement (NavMeshObstacle handles blocking)
   - Connection updates are throttled to prevent cascading
   - Preview objects are pooled and reused

5. **NavMesh Integration**:
   - WallNavMeshObstacle automatically carves NavMesh
   - Units will path around walls
   - Add WallStairs for traversable walls

## Troubleshooting

**Walls have gaps between segments:**
- Ensure `useAutoMeshSize` is enabled
- Check `wallLengthAxis` matches your mesh orientation
- Verify mesh pivot is centered

**Walls overlap when they shouldn't:**
- Increase `minParallelOverlap` value
- Check collider setup on wall prefabs

**Can't place walls:**
- Verify IResourcesService is registered
- Check ground layer mask is correct
- Ensure preview materials are assigned

**Walls don't block units:**
- Add WallNavMeshObstacle component to prefab
- Verify NavMesh is baked after wall placement
- Check NavMesh carving settings

## Examples

### Creating a Stone Wall System
```csharp
// 1. Create resource manager
public class GameResources : MonoBehaviour, IResourcesService { ... }

// 2. Register at startup
ServiceLocator.Register<IResourcesService>(GetComponent<GameResources>());

// 3. Create UI button
public void BuildStoneWall()
{
    wallPlacer.StartPlacingWalls(stoneWallData);
}
```

### Listening to Wall Events
```csharp
private void OnEnable()
{
    EventBus.Subscribe<BuildingCompletedEvent>(OnWallBuilt);
}

private void OnWallBuilt(BuildingCompletedEvent evt)
{
    Debug.Log($"Wall completed: {evt.BuildingName}");
    // Play sound, show VFX, etc.
}
```

## Support

For issues, questions, or feature requests, please check the documentation or contact support.

## License

See LICENSE file for details.

## Version History

### 1.0.0 (Current)
- Initial release
- Pole-to-pole wall placement
- Perfect mesh fitting
- Overlap detection
- Wall snapping
- Resource cost system
- NavMesh integration
- Editor tools
