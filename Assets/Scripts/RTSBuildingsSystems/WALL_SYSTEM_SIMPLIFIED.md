# Simplified Wall System - Stronghold Crusader Style

## Overview

This is a **centralized, simplified wall building system** inspired by Stronghold Crusader. Key features:

- ✅ **Single wall prefab** - No need for 16 mesh variants!
- ✅ **Automatic rotation** - Walls rotate based on connections
- ✅ **Drag-to-build** - Place first wall, drag, place to create long segments
- ✅ **Auto-connect** - Walls automatically detect and connect to neighbors
- ✅ **Collider support** - Auto-creates colliders for selection and gameplay
- ✅ **Upgrade system** - Convert walls to towers like Stronghold Crusader
- ✅ **Selection support** - Click to select walls for upgrades

## Quick Start (5 Minutes)

### 1. Create a Wall Prefab

Use the editor tool: **Tools > RTS > Setup Wall Prefab**

1. Click "Create New Wall Prefab GameObject"
2. Choose "Auto-Create Simple Mesh" (for testing)
3. Adjust wall dimensions if needed
4. Click "Setup Wall Prefab"

✅ Done! Your wall prefab is ready with all components.

### 2. Create Building Data

1. Right-click in Project: **Create > RTS > Building Data**
2. Set `Building Type` to `Defensive`
3. Assign your wall prefab to `Building Prefab`
4. Set resource costs (e.g., Wood: 10, Stone: 20)
5. Set construction time

### 3. Add to Building Manager

1. Find `BuildingManager` in your scene
2. Add your wall BuildingDataSO to the `Available Buildings` list

### 4. Test It!

1. Press Play
2. Click the wall button in the Building HUD
3. Click to place first wall
4. Drag mouse to show wall preview
5. Click again to confirm - walls are built!
6. Build more walls nearby - they auto-connect and rotate!

## How It Works

### Wall Connection & Rotation

The system uses **automatic rotation** instead of mesh variants:

```
Standalone (0 connections)
│
└── Rotation: 0°

End (1 connection)
│
├── North → Rotation: 0°
├── East  → Rotation: 90°
├── South → Rotation: 180°
└── West  → Rotation: 270°

Straight (2 opposite connections)
│
├── North-South → Rotation: 0°
└── East-West   → Rotation: 90°

Corner (2 adjacent connections)
│
├── North-East → Rotation: 0°
├── East-South → Rotation: 90°
├── South-West → Rotation: 180°
└── West-North → Rotation: 270°

T-Junction (3 connections)
│
└── Rotates to face the missing direction

Cross (4 connections)
│
└── Rotation: 0° (all directions connected)
```

### Wall Types

The system automatically detects 6 wall types:

| Type | Connections | Description |
|------|-------------|-------------|
| **Standalone** | 0 | Isolated wall segment |
| **End** | 1 | End piece of a wall line |
| **Straight** | 2 (opposite) | Long wall segment (N-S or E-W) |
| **Corner** | 2 (adjacent) | 90° corner piece |
| **T-Junction** | 3 | Three-way intersection |
| **Cross** | 4 | Four-way intersection |

## Components

### WallConnectionSystem

Main component for wall connections and rotation.

**Inspector Settings:**
- `Grid Size` - Grid cell size (default: 1.0)
- `Enable Connections` - Enable/disable auto-connection
- `Wall Mesh` - The mesh object to rotate (auto-found if named "WallMesh")
- `Wall Collider` - Collider for selection (auto-created if enabled)
- `Auto Create Collider` - Automatically add BoxCollider

**Public API:**
```csharp
// Get wall information
int GetConnectionState();          // Bitmask of connections
Vector2Int GetGridPosition();      // Grid position
WallType GetWallType();           // Current wall type
bool IsConnected(WallDirection);  // Check specific direction

// Manual rotation (optional)
void RotateWall(float yRotation); // Rotate by degrees
```

### WallUpgradeSystem

Handles upgrading walls to towers.

**Inspector Settings:**
- `Tower Prefab` - The tower to replace wall with
- `Can Upgrade` - Enable/disable upgrades
- `Wood/Stone/Gold/Food Cost` - Resource costs for upgrade

**Public API:**
```csharp
void UpgradeToTower();           // Perform upgrade
bool IsUpgradeAffordable();      // Check if player can afford
(int, int, int, int) GetUpgradeCost(); // Get costs
```

**Usage:**
1. Select a wall (click on it)
2. Show upgrade UI button
3. Click to upgrade → Wall replaced with tower

### BuildingSelectable

Makes walls selectable for upgrades and inspection.

**Features:**
- Color highlight on selection
- Optional selection indicator
- Event publishing for selection state

## Building Walls

### Free Build Mode

1. **Place Start:** Click to place first pole/wall
2. **Drag:** Move mouse to show wall preview
3. **Place End:** Click again to confirm wall line

The system automatically:
- Calculates required wall segments
- Shows preview with cost
- Checks affordability (green = can build, red = can't)
- Places all segments at once
- Connects to nearby walls

### Shapes You Can Build

```
Line (Straight)
═══════════

Square
╔═══╗
║   ║
╚═══╝

Triangle
  ╔═╗
 ╔╝ ╚╗
╔╝   ╚╗

Rectangle
╔═══════╗
║       ║
╚═══════╝

Complex (with towers at corners)
╔T═══T╗
║     ║
T     T
║     ║
╚T═══T╝
```

## Upgrading to Towers

Stronghold Crusader style tower upgrade:

1. Build walls
2. Select a wall segment
3. Click "Upgrade to Tower"
4. Wall is replaced with tower
5. Neighboring walls update connections

**Best Practices:**
- Upgrade corner walls to create defensive bastions
- Upgrade every 3-4 segments for archer coverage
- Keep end pieces as walls (cheaper than towers)

## Inspector Features

### Runtime Info (Play Mode)

The custom inspector shows:

- **Grid Position** - Current grid coordinates
- **Wall Type** - Detected type (Straight, Corner, etc.)
- **Connection State** - Visual diagram of connections
- **Connection Buttons** - Green = connected, Red = not connected

### Manual Controls

- **Rotate 90°** - Manually rotate wall mesh
- **Force Update** - Recalculate connections

## Collider System

Walls automatically get colliders for:

1. **Selection** - Click to select
2. **Gameplay** - Units can't walk through
3. **Raycasting** - Detecting clicks and interactions

**Auto-Setup:**
- BoxCollider added in Awake()
- Default size: 1m wide × 2m tall × 0.5m thick
- Centered at wall position

## Event System

Walls integrate with the event bus:

```csharp
// Published events
BuildingPlacedEvent    // When wall is built
BuildingDestroyedEvent // When wall is destroyed
BuildingSelectedEvent  // When wall is selected
ResourcesSpentEvent    // When resources spent on walls
NotificationEvent      // UI notifications
```

## Troubleshooting

### Walls not connecting?
- Check `Enable Connections` is true
- Verify walls are on same grid alignment
- Use "Force Update" button in inspector

### Walls not rotating?
- Check `Wall Mesh` is assigned
- Verify mesh is child GameObject named "WallMesh"
- Check mesh orientation (should face North initially)

### Can't select walls?
- Ensure `BuildingSelectable` component is attached
- Check collider exists on wall
- Verify correct layer mask on `BuildingSelectionManager`

### Upgrade not working?
- Assign `Tower Prefab` in WallUpgradeSystem
- Check resource costs
- Verify player has enough resources

## Migration from Old System

If you have walls using the old 16-variant system:

1. ✅ Keep your existing wall prefabs
2. ✅ Open wall prefab for editing
3. ✅ Delete the "Variants" container with 16 meshes
4. ✅ Create single "WallMesh" child object
5. ✅ Reassign in WallConnectionSystem inspector
6. ✅ Add WallUpgradeSystem component
7. ✅ Add BuildingSelectable component
8. ✅ Done! Old walls work with new system

## Best Practices

### Prefab Setup
- Name wall mesh GameObject "WallMesh" for auto-detection
- Orient mesh facing North (positive Z)
- Keep mesh as child of wall prefab root
- Use simple materials for better performance

### Gameplay
- Grid size should match terrain tile size
- Place walls on flat terrain for best results
- Use affordable costs for rapid wall building
- Balance upgrade costs vs. tower placement costs

### Performance
- Single mesh per wall = better performance
- Auto-collider setup reduces setup time
- Grid-based lookup = O(1) neighbor detection

## API Reference

### Enums

```csharp
public enum WallDirection {
    North = 1,
    East = 2,
    South = 4,
    West = 8
}

public enum WallType {
    Standalone,  // 0 connections
    End,         // 1 connection
    Straight,    // 2 opposite
    Corner,      // 2 adjacent
    TJunction,   // 3 connections
    Cross        // 4 connections
}

public enum NotificationType {
    Info,
    Success,
    Warning,
    Error
}
```

### Connection Bitmask

Walls use bitmask for efficient connection storage:

```csharp
North = 1   (0001)
East  = 2   (0010)
South = 4   (0100)
West  = 8   (1000)

Examples:
0  = No connections     (0000)
3  = North + East       (0011)
5  = North + South      (0101)
10 = East + West        (1010)
15 = All directions     (1111)
```

## Support

For issues or questions:
1. Check this documentation
2. Review inspector tooltips
3. Check Unity Console for warnings
4. Use debug buttons in inspector

## Summary

The simplified wall system provides:
✅ Easy setup (5 minutes)
✅ One mesh instead of 16
✅ Automatic rotation
✅ Drag-to-build
✅ Auto-connect
✅ Tower upgrades
✅ Full selection support

Build amazing fortifications like Stronghold Crusader! 🏰
