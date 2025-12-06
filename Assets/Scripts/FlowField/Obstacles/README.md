# FlowField Obstacles

## Overview

This folder contains obstacle components that mark buildings and walls as unwalkable in the FlowField cost grid.

## Components

### BuildingFlowFieldObstacle
- Replaces `NavMeshObstacle` for buildings
- Automatically detects building size from colliders/renderers
- Updates FlowField cost grid when placed/destroyed
- No baking required - dynamic updates

**Usage:**
```csharp
// Auto-added by BuildingManager when placing buildings
// Or manually add to building prefabs
gameObject.AddComponent<BuildingFlowFieldObstacle>();
```

### WallFlowFieldObstacle
- Replaces `NavMeshObstacle` for walls
- Matches wall collider bounds
- Updates FlowField cost grid dynamically
- Supports wall placement and destruction

**Usage:**
```csharp
// Auto-added by WallPlacementController when placing walls
// Or manually add to wall prefabs
gameObject.AddComponent<WallFlowFieldObstacle>();
```

## How It Works

1. **On Awake**: Detects obstacle bounds from colliders
2. **On Start**: Registers with FlowFieldManager
3. **Updates Cost Grid**: Marks area as unwalkable
4. **On Destroy**: Unregisters and restores walkability

## Performance

- ✅ **Dynamic**: No NavMesh baking required
- ✅ **Efficient**: Only updates affected grid region
- ✅ **Cached**: FlowField cache invalidation handles updates
- ✅ **Automatic**: No manual intervention needed

## Integration

Works seamlessly with:
- `BuildingManager` - Auto-adds to new buildings
- `WallPlacementController` - Auto-adds to walls
- `FlowFieldManager` - Registers obstacles automatically
- Migration tool - Converts from NavMeshObstacle
