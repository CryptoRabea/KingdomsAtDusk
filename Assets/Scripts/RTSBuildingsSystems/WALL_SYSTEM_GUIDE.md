# Modular Wall Building System - Setup Guide

This guide explains how to set up and use the modular wall connection system for RTS-style automatic wall connections.

## Overview

The wall system automatically detects adjacent walls and updates their visual appearance to create seamless connections. When you place walls next to each other, they automatically connect to form corners, straight segments, and junctions.

## Features

- **Automatic Connection Detection**: Walls detect neighbors in 4 directions (North, East, South, West)
- **16 Visual States**: Supports all possible connection combinations
- **Grid-Based System**: Uses the same grid system as BuildingManager
- **Event-Driven**: Automatically updates when walls are placed or destroyed
- **Visual Debugging**: Gizmos show connections in Scene view
- **Custom Editor**: Easy-to-use inspector with visual guides

## Quick Start

### 1. Create Wall Mesh Variants

You need to create 16 different mesh variants for all connection states:

| Index | Connections | Description | Example Shape |
|-------|-------------|-------------|---------------|
| 0 | None | Isolated wall post | ⬜ |
| 1 | N | Wall extending North | ╨ |
| 2 | E | Wall extending East | ╞ |
| 3 | N+E | Corner (North-East) | ╚ |
| 4 | S | Wall extending South | ╥ |
| 5 | N+S | Straight (North-South) | ║ |
| 6 | E+S | Corner (East-South) | ╔ |
| 7 | N+E+S | T-junction (North-East-South) | ╠ |
| 8 | W | Wall extending West | ╡ |
| 9 | N+W | Corner (North-West) | ╝ |
| 10 | E+W | Straight (East-West) | ═ |
| 11 | N+E+W | T-junction (North-East-West) | ╩ |
| 12 | S+W | Corner (South-West) | ╗ |
| 13 | N+S+W | T-junction (North-South-West) | ╣ |
| 14 | E+S+W | T-junction (East-South-West) | ╦ |
| 15 | N+E+S+W | 4-way intersection | ╬ |

**Tip**: You can start with simple cube variations to test the system, then create detailed models later.

### 2. Set Up Wall Prefab

1. Create a new GameObject for your wall prefab
2. Add the `Building` component (from RTS.Buildings)
3. Add the `WallConnectionSystem` component
4. Create 16 child GameObjects, each containing a different mesh variant
5. Assign all 16 child objects to the `meshVariants` array in the order shown above
6. Set the `gridSize` to match your BuildingManager's grid size (default: 1)

Example hierarchy:
```
WallPrefab
├── Building (component)
├── WallConnectionSystem (component)
├── Variant_0_None (child GameObject with mesh)
├── Variant_1_N (child GameObject with mesh)
├── Variant_2_E (child GameObject with mesh)
├── Variant_3_NE (child GameObject with mesh)
├── ... (continue for all 16 variants)
└── Variant_15_NESW (child GameObject with mesh)
```

### 3. Create Wall BuildingDataSO

1. Right-click in Project window → Create → RTS → BuildingData
2. Name it "WallData"
3. Configure the settings:
   - Building Name: "Stone Wall"
   - Building Type: **Defensive**
   - Assign your wall prefab to `buildingPrefab`
   - Set costs (e.g., Stone: 10)
   - Set construction time
   - Add icon

### 4. Add to BuildingManager

1. Find the BuildingManager in your scene
2. Add your WallData ScriptableObject to the `buildingDataArray`
3. Make sure grid sizes match between BuildingManager and WallConnectionSystem

## How It Works

### Connection Detection

The system uses a bitmask to represent connections:
- **North** = 1 (binary: 0001)
- **East** = 2 (binary: 0010)
- **South** = 4 (binary: 0100)
- **West** = 8 (binary: 1000)

When walls are adjacent, the system:
1. Converts world positions to grid coordinates
2. Checks all 4 adjacent grid cells
3. Calculates a connection bitmask (0-15)
4. Activates the corresponding mesh variant

### Event System Integration

The wall system uses the existing EventBus:
- **BuildingPlacedEvent**: Updates nearby walls when a new wall is placed
- **BuildingDestroyedEvent**: Updates neighbors when a wall is removed

No modifications to core systems needed!

### Grid Registration

All walls are registered in a static dictionary:
```csharp
Dictionary<Vector2Int, WallConnectionSystem> wallRegistry
```

This allows O(1) lookup for neighbor detection.

## Advanced Usage

### Custom Connection Logic

You can extend the system to support:
- Different wall types (stone, wood, metal)
- Wall heights (allow connections only to same height)
- Gate segments (special variants at specific indices)
- Diagonal connections (8 directions instead of 4)

### Optimizing Mesh Variants

Instead of 16 separate meshes, you can:
1. Use a single mesh with 16 submeshes
2. Use shader-based connections (vertex colors/UVs)
3. Procedurally generate connections at runtime

### Debugging

Use the custom inspector features:
- **Connection Diagram**: Visual representation of current state
- **Runtime Info**: Shows grid position and active connections
- **Force Update Button**: Manually trigger connection recalculation
- **Scene Gizmos**: Green lines show active connections

Debug context menu options (right-click component):
- Force Update Connections
- Print Connection State
- Print All Walls

## Troubleshooting

### Walls Not Connecting

1. **Check grid size**: WallConnectionSystem.gridSize must match BuildingManager.gridSize
2. **Verify prefab setup**: Ensure Building component is present
3. **Check mesh variants**: All 16 slots must be assigned
4. **Grid alignment**: Walls must be exactly on grid points

### Wrong Visual Showing

1. **Mesh variant order**: Ensure meshVariants array is in correct order (0-15)
2. **Multiple active**: Only one variant should be active at a time
3. **Update timing**: Walls update on Start() and when neighbors change

### Performance Issues

1. **Static dictionary**: Only one registry for all walls (very efficient)
2. **Event filtering**: Only updates when relevant buildings change
3. **Lazy updates**: Only recalculates when connections actually change

## Example Code

### Manually Update a Wall
```csharp
var wall = wallObject.GetComponent<WallConnectionSystem>();
wall.UpdateConnections();
```

### Check Wall Connections
```csharp
var wall = wallObject.GetComponent<WallConnectionSystem>();
bool connectedNorth = wall.IsConnected(WallDirection.North);
int connectionState = wall.GetConnectionState();
Vector2Int gridPos = wall.GetGridPosition();
```

### Get All Walls in Range
```csharp
// This is handled automatically by the system,
// but you can access the static registry if needed
// Note: The registry is private for encapsulation
```

## Integration with Existing Systems

### Building System
- ✅ Uses existing BuildingDataSO
- ✅ Uses existing Building component
- ✅ Uses existing EventBus
- ✅ Uses existing grid system
- ✅ No modifications to BuildingManager needed

### Resource System
- ✅ Wall costs managed by BuildingDataSO
- ✅ Resources spent through BuildingManager

### UI System
- ✅ Walls appear in building menu automatically
- ✅ Uses standard building placement flow

## Best Practices

1. **Test with Simple Meshes First**: Use colored cubes to verify logic before creating detailed models
2. **Consistent Grid Size**: Keep all grid-related values consistent across systems
3. **Variant Naming**: Name child objects clearly (e.g., "Variant_5_NS" for North-South straight)
4. **Prefab Variants**: Create separate prefabs for different wall materials/types
5. **Layer Setup**: Ensure walls are on appropriate collision layers

## Next Steps

1. Create your 16 mesh variants in your 3D modeling software
2. Set up the wall prefab with all variants
3. Create the WallData ScriptableObject
4. Test placement in your scene
5. Iterate on visual appearance and placement feel

## API Reference

### WallConnectionSystem

**Public Methods:**
- `UpdateConnections()` - Manually trigger connection update
- `GetConnectionState()` - Returns current bitmask (0-15)
- `GetGridPosition()` - Returns grid coordinates
- `IsConnected(WallDirection)` - Check if connected in specific direction

**Properties:**
- `gridSize` - Grid cell size (must match BuildingManager)
- `enableConnections` - Toggle connection system on/off
- `meshVariants` - Array of 16 mesh variant GameObjects

**Events:**
- Subscribes to `BuildingPlacedEvent`
- Subscribes to `BuildingDestroyedEvent`

### WallDirection Enum
```csharp
public enum WallDirection
{
    North = 1,
    East = 2,
    South = 4,
    West = 8
}
```

## License & Credits

This wall system is designed for the Kingdoms At Dusk RTS game.
Uses the existing RTS.Buildings and RTS.Core.Events namespaces.

---

**Version**: 1.0
**Last Updated**: 2025-11-13
**Author**: Claude (Anthropic)
