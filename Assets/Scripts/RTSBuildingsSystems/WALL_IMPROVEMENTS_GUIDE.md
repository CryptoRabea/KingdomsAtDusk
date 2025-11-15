# Wall System Improvements Guide

This guide covers the new improvements to the wall system including collision prevention, NavMesh integration, and stairs for unit traversal.

## Features Overview

### 1. Wall-Building Collision Prevention ✅

Walls can no longer be placed over existing buildings, preventing invalid placement and ensuring clean construction.

**How it works:**
- Uses capsule collision detection along the entire wall path
- Checks for any buildings (including other structures) before allowing placement
- Shows red preview when placement would overlap a building
- Prevents actual placement with a warning message

**Implementation:**
- `WallPlacementController.WouldOverlapBuildings()` - Collision detection method
- Integrated into wall preview and placement validation

### 2. NavMesh Integration for Walls and Buildings ✅

All walls and buildings now block unit navigation properly using Unity's NavMesh system.

**Components:**

#### WallNavMeshObstacle
- Automatically added to wall segments when placed
- Uses NavMeshObstacle with carving enabled
- Dynamically updates NavMesh to block unit paths through walls
- Auto-detects wall dimensions from colliders

**Configuration:**
```csharp
[SerializeField] private bool carveNavMesh = true;
[SerializeField] private bool moveThreshold = true;
[SerializeField] private float carvingMoveThreshold = 0.1f;
[SerializeField] private float carvingTimeToStationary = 0.5f;
```

#### BuildingNavMeshObstacle
- Automatically added to buildings when placed
- Similar to WallNavMeshObstacle but optimized for buildings
- Auto-detects building size from colliders, renderers, or manual configuration

**Configuration:**
```csharp
[SerializeField] private bool carveNavMesh = true;
[SerializeField] private bool autoDetectSize = true;
[SerializeField] private Vector3 manualSize = Vector3.one;
```

**How it works:**
- NavMeshObstacles with carving automatically update the NavMesh at runtime
- Units (using NavMeshAgent) will path around walls and buildings
- No manual NavMesh rebaking required
- Obstacles are removed when buildings/walls are destroyed

### 3. Stairs System for Wall Traversal ✅

Units can now traverse walls using stairs connected with NavMeshLinks.

**Components:**

#### WallStairs
- Handles stairs/ramps for walls
- Uses Unity's NavMeshLink for pathfinding
- Supports bidirectional movement (up and down)
- Auto-generates simple visual ramp or uses custom mesh

**Configuration:**
```csharp
[SerializeField] private float wallHeight = 3f;
[SerializeField] private float stairWidth = 2f;
[SerializeField] private float stairDepth = 3f;
[SerializeField] private bool bidirectional = true;
[SerializeField] private GameObject stairMeshPrefab;
```

**Features:**
- NavMeshLink connects ground to wall top
- Customizable start/end points
- Optional custom stair mesh
- Auto-generated default visual (ramp)
- Gizmo visualization in editor

#### StairPlacementController
- Handles interactive placement of stairs
- Snaps to nearby walls automatically
- Validates placement distance and overlap
- Resource cost system (Wood: 50, Stone: 20)
- Preview system with valid/invalid materials

**Usage:**
1. Call `StartPlacingStairs()` to begin placement mode
2. Move mouse near a wall to snap
3. Click to place when preview is green
4. Right-click or ESC to cancel

## Setup Instructions

### For Walls

1. **WallNavMeshObstacle** is automatically added when walls are placed
   - No manual setup required
   - Component added in `WallPlacementController.PlaceWallSegments()`

2. **Optional**: Customize NavMesh settings on wall prefabs
   - Add `WallNavMeshObstacle` component to wall prefab
   - Adjust carving settings as needed

### For Buildings

1. **BuildingNavMeshObstacle** is automatically added when buildings are placed
   - No manual setup required
   - Component added in `BuildingManager.PlaceBuilding()`

2. **Optional**: Pre-configure on building prefabs
   - Add `BuildingNavMeshObstacle` component
   - Set `autoDetectSize = false` for manual control
   - Adjust size and center manually if needed

### For Stairs

1. **Create Stair Prefab**:
   - Create empty GameObject
   - Add `WallStairs` component
   - Configure height, width, depth
   - (Optional) Add custom stair mesh as child

2. **Setup StairPlacementController**:
   - Add component to scene manager
   - Assign stair prefab
   - Configure materials for preview
   - Set layer masks for ground and walls

3. **Trigger Placement** (example):
   ```csharp
   StairPlacementController stairController;

   void OnStairsButtonClick()
   {
       stairController.StartPlacingStairs();
   }
   ```

## Technical Details

### Collision Detection

**Wall-Building Detection:**
- Uses `Physics.OverlapCapsule()` along wall path
- Capsule radius: 0.5f (adjustable)
- Checks from start pole to end pole
- Ignores: ground layer, terrain, preview objects
- Detects: Buildings, walls, obstacles

**Validation Flow:**
1. User places first pole
2. User moves second pole
3. System checks:
   - Wall-to-wall overlap (`WouldOverlapExistingWall()`)
   - Building overlap (`WouldOverlapBuildings()`)
   - Resource availability
4. Preview shows red if any check fails
5. Placement blocked if invalid

### NavMesh System

**Obstacle Carving:**
- Uses Unity's NavMeshObstacle component
- Carving mode: Dynamic
- Updates NavMesh at runtime
- Performance: Optimized for stationary obstacles

**Important:**
- Requires pre-baked NavMesh in scene
- Works with existing NavMeshAgent components
- Compatible with Unity's NavMesh system
- No custom pathfinding required

**NavMeshLink for Stairs:**
- Connects two NavMesh surfaces
- Bidirectional by default
- Width configurable
- Auto-updates when stair position changes

### Performance Considerations

1. **NavMeshObstacle Carving**:
   - Only carves when stationary (configurable)
   - Low overhead for static buildings/walls
   - May impact performance with 100+ obstacles

2. **Collision Detection**:
   - Single capsule cast per wall segment
   - Efficient for typical wall lengths
   - No continuous checks after placement

3. **NavMeshLinks**:
   - Minimal overhead
   - Pre-calculated connections
   - No runtime path recalculation

## Code Integration

### WallPlacementController Changes

**Added Methods:**
- `WouldOverlapBuildings()` - Building collision detection
- Modified `UpdateWallPreview()` - Added building check
- Modified `PlaceWallSegments()` - Added NavMeshObstacle and building check

**Code snippets:**
```csharp
// Check for building overlaps
bool overlapsBuilding = WouldOverlapBuildings(firstPolePosition, secondPolePos);

// Add NavMeshObstacle to walls
if (newWall.GetComponent<WallNavMeshObstacle>() == null)
{
    newWall.AddComponent<WallNavMeshObstacle>();
}
```

### BuildingManager Changes

**Added Code:**
```csharp
// Add NavMeshObstacle to buildings
if (newBuilding.GetComponent<BuildingNavMeshObstacle>() == null)
{
    newBuilding.AddComponent<BuildingNavMeshObstacle>();
}
```

## Troubleshooting

### Walls still overlap buildings
- Ensure groundLayer is properly configured
- Check if buildings have colliders
- Verify Building component is attached
- Check capsuleRadius in WouldOverlapBuildings (increase if needed)

### Units walk through walls
- Verify NavMesh is baked in scene
- Check NavMeshObstacle is added to walls
- Ensure carving is enabled
- Confirm wall has collider

### Stairs don't work
- Verify NavMesh covers both start and end points
- Check NavMeshLink is enabled
- Ensure bidirectional is true
- Verify wall height matches stair configuration

### Performance issues
- Reduce number of NavMeshObstacles
- Set carveOnlyStationary = true
- Use manual size instead of auto-detect
- Bake static obstacles into NavMesh

## Future Enhancements

Potential improvements for the system:

1. **Automatic Stair Placement**: Add stairs automatically when walls are placed
2. **Stair Variants**: Multiple stair types (ladder, ramp, full stairs)
3. **Wall Segments**: Pre-placed stairs in wall segments
4. **Patrol Points**: Add patrol waypoints on walls for archers
5. **Dynamic NavMesh**: Full NavMeshSurface runtime baking for large-scale changes
6. **Optimization**: Pool NavMeshObstacles for better performance
7. **Visual Feedback**: Show NavMesh paths in debug mode

## API Reference

### WallNavMeshObstacle
```csharp
public class WallNavMeshObstacle : MonoBehaviour
{
    // Public properties
    [SerializeField] bool carveNavMesh
    [SerializeField] bool moveThreshold
    [SerializeField] float carvingMoveThreshold
    [SerializeField] float carvingTimeToStationary
}
```

### BuildingNavMeshObstacle
```csharp
public class BuildingNavMeshObstacle : MonoBehaviour
{
    // Public properties
    [SerializeField] bool carveNavMesh
    [SerializeField] bool autoDetectSize
    [SerializeField] Vector3 manualSize
    [SerializeField] Vector3 manualCenter
}
```

### WallStairs
```csharp
public class WallStairs : MonoBehaviour
{
    // Public methods
    void UpdateStairConfiguration(float newWallHeight, float newDepth)
    void SetCustomPoints(Vector3 startPoint, Vector3 endPoint)

    // Public properties
    [SerializeField] float wallHeight
    [SerializeField] float stairWidth
    [SerializeField] float stairDepth
    [SerializeField] bool bidirectional
    [SerializeField] GameObject stairMeshPrefab
}
```

### StairPlacementController
```csharp
public class StairPlacementController : MonoBehaviour
{
    // Public methods
    void StartPlacingStairs()
    void CancelStairPlacement()

    // Public properties
    bool IsPlacingStairs { get; }
}
```

## Credits

Implementation Date: 2025-11-15
Features: Wall-Building Collision Prevention, NavMesh Integration, Stairs System
