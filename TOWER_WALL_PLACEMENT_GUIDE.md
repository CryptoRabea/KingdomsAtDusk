# Tower Wall Placement System - Complete Guide

## Overview

The enhanced Tower Wall Placement System allows towers to be placed on walls with perfect rotation alignment and intelligent multi-segment wall replacement. This system ensures towers integrate seamlessly with your wall defenses.

## Key Features

### 1. **Perfect Rotation Alignment**
- Towers automatically align their rotation with the wall direction
- Rotation is extracted from the wall's Y-axis (yaw) only
- Ensures towers face the correct direction along the wall

### 2. **Multi-Segment Wall Replacement**
- Towers can replace multiple wall segments based on their size
- System automatically detects connected wall segments within tower length
- Maintains wall connections for segments outside the tower footprint

### 3. **Modular Configuration**
- Configurable tower sizes for different tower types
- Optional position adjustment for non-perfectly aligned towers
- Fallback to single-segment replacement when needed

## Configuration

### Tower Data Settings (TowerDataSO)

Open your TowerDataSO asset in the Inspector to configure these settings:

#### Wall Replacement Settings

| Property | Type | Description | Default |
|----------|------|-------------|---------|
| **canReplaceWalls** | bool | Enable/disable wall placement for this tower | true |
| **wallSnapDistance** | float | Detection radius for nearby walls (in units) | 2.0 |
| **towerWallLength** | float | Length of tower along the wall axis (in units) | 2.0 |
| **replaceMultipleSegments** | bool | Auto-replace multiple wall segments if tower covers them | true |
| **allowPositionAdjustment** | bool | Allow tower position adjustment for better alignment | true |
| **maxPositionAdjustment** | float | Maximum position offset when adjusting (in units) | 1.0 |

### Recommended Configuration by Tower Size

#### Small Towers (Arrow Towers)
```
towerWallLength: 2.0
replaceMultipleSegments: false  // Single segment replacement
allowPositionAdjustment: false
```

#### Medium Towers (Ballista Towers)
```
towerWallLength: 4.0
replaceMultipleSegments: true   // Can cover 2 wall segments
allowPositionAdjustment: true
maxPositionAdjustment: 0.5
```

#### Large Towers (Catapult Towers)
```
towerWallLength: 6.0
replaceMultipleSegments: true   // Can cover 3+ wall segments
allowPositionAdjustment: true
maxPositionAdjustment: 1.0
```

## How It Works

### 1. Wall Detection
When placing a tower:
1. System searches for walls within `wallSnapDistance`
2. Finds the nearest wall segment to cursor position
3. Calculates perfect rotation from wall direction

### 2. Multi-Segment Analysis
If `replaceMultipleSegments` is enabled:
1. Calculates tower half-length: `towerWallLength / 2`
2. Traverses connected walls in both directions along wall line
3. Collects all wall segments within tower footprint
4. Calculates geometric center of collected segments

### 3. Position Adjustment
If `allowPositionAdjustment` is enabled:
1. System calculates optimal center position for tower
2. Checks if adjustment distance ≤ `maxPositionAdjustment`
3. If within limit: uses adjusted position
4. If exceeds limit: uses nearest wall position (fallback)

### 4. Rotation Application
- Extracts wall rotation (removes pitch/roll, keeps yaw)
- Applies rotation to tower preview during placement
- Tower is instantiated with wall-aligned rotation

### 5. Wall Replacement
When tower is placed:
1. All wall segments in tower footprint are destroyed
2. External wall connections are preserved
3. Tower inherits wall connections via `WallConnectionSystem`
4. Tower maintains wall continuity for adjacent segments

## Technical Implementation

### Key Files Modified

1. **TowerDataSO.cs** (`Assets/Scripts/RTSBuildingsSystems/`)
   - Added tower size and configuration properties
   - Extends BuildingDataSO with wall placement settings

2. **TowerPlacementHelper.cs** (`Assets/Scripts/RTSBuildingsSystems/`)
   - `TrySnapToWall()` - Returns position, rotation, and multiple walls
   - `CalculateWallRotation()` - Extracts Y-axis rotation from wall
   - `FindWallSegmentsToCover()` - Detects multi-segment coverage
   - `TraverseWallDirection()` - Walks connected walls
   - `ReplaceWallWithTower()` - Handles multi-segment replacement

3. **BuildingManager.cs** (`Assets/Scripts/Managers/`)
   - Updated to use `List<GameObject> wallsToReplace`
   - Stores and applies `towerSnappedRotation`
   - Destroys all covered wall segments
   - Updates collision detection for multi-segment

### Data Structures

#### WallReplacementData
```csharp
public class WallReplacementData
{
    public List<GameObject> originalWalls;           // All walls being replaced
    public Vector3 position;                         // Calculated optimal position
    public Quaternion rotation;                      // Wall rotation for alignment
    public List<WallConnectionSystem> connectedWalls; // External connections to maintain
}
```

## Usage Examples

### Example 1: Basic Tower Placement
```
1. Select tower from build menu
2. Move cursor over wall
3. Tower snaps to wall with correct rotation
4. Click to place - replaces 1 wall segment
```

### Example 2: Large Tower Multi-Segment
```
1. Select large tower (towerWallLength = 6.0)
2. Move cursor to center of 3 connected wall segments
3. System detects all 3 segments within tower footprint
4. Tower preview centers on all 3 segments
5. Click to place - replaces all 3 wall segments
6. Adjacent walls maintain connections to tower
```

### Example 3: Position Adjustment
```
1. Tower with allowPositionAdjustment = true
2. Cursor near wall, but slightly offset
3. System adjusts position to geometric center of walls
4. Adjustment is within maxPositionAdjustment (1.0)
5. Tower placed at optimized position
```

## Debugging

### Visual Indicators

The system provides visual feedback during placement:

- **Cyan Wireframe Sphere**: Snap indicator at tower position
- **Vertical Line**: Shows tower height reference
- **Green Preview**: Valid placement
- **Red Preview**: Invalid placement

### Debug Information

Enable Gizmos in Scene view to see:
- Wall snap detection radius
- Connected wall segments
- Tower rotation alignment

### Common Issues

#### Tower Won't Snap to Wall
**Cause**: `wallSnapDistance` too small or `canReplaceWalls` is false
**Solution**: Increase `wallSnapDistance` in TowerDataSO or enable `canReplaceWalls`

#### Wrong Number of Segments Replaced
**Cause**: `towerWallLength` doesn't match actual tower size
**Solution**: Adjust `towerWallLength` to match tower's actual length

#### Tower Position Offset from Walls
**Cause**: Position adjustment disabled or exceeded max adjustment
**Solution**: Enable `allowPositionAdjustment` or increase `maxPositionAdjustment`

#### Tower Rotation Wrong
**Cause**: Wall rotation has non-zero X or Z components
**Solution**: System automatically removes pitch/roll - check wall prefab rotation

## Best Practices

### 1. Measure Tower Sizes Accurately
- Use Unity's measurement tools to determine actual tower length
- Set `towerWallLength` to match the tower's footprint along the wall

### 2. Configure by Tower Type
- Small towers: Single segment, no adjustment
- Medium towers: Multi-segment with limited adjustment
- Large towers: Multi-segment with flexible adjustment

### 3. Test Wall Connections
- Place towers on straight walls, corners, and T-junctions
- Verify wall connections are maintained for adjacent segments
- Check that walls can still connect to tower

### 4. Balance Snapping
- Use larger `wallSnapDistance` for easier placement
- Use smaller distance for precise control
- Recommended range: 1.5 - 3.0 units

### 5. Performance Considerations
- Multi-segment detection uses wall traversal (efficient)
- Disable `replaceMultipleSegments` for small towers
- System automatically handles edge cases (corners, endpoints)

## API Reference

### TowerPlacementHelper Methods

#### TrySnapToWall (Full Signature)
```csharp
public bool TrySnapToWall(
    Vector3 position,
    TowerDataSO towerData,
    out Vector3 outPosition,
    out Quaternion outRotation,
    out List<GameObject> outWalls
)
```

Returns true if tower can snap to wall, outputs position, rotation, and all walls to replace.

#### TrySnapToWall (Legacy Signature)
```csharp
public bool TrySnapToWall(
    Vector3 position,
    TowerDataSO towerData,
    out Vector3 outPosition,
    out GameObject outWall
)
```

Backward compatible - returns single wall only.

#### ReplaceWallWithTower (Multi-Segment)
```csharp
public WallReplacementData ReplaceWallWithTower(
    List<GameObject> walls,
    TowerDataSO towerData
)
```

Creates replacement data for multiple wall segments.

#### ReplaceWallWithTower (Single-Segment)
```csharp
public WallReplacementData ReplaceWallWithTower(
    GameObject wall,
    TowerDataSO towerData
)
```

Backward compatible - single wall replacement.

## Advanced Scenarios

### Scenario 1: Corner Wall Towers
- System detects if wall has >2 connections (corner piece)
- `CanReplaceWall()` can prevent replacement of corners
- Configure per-tower via validation logic

### Scenario 2: Custom Wall Sizes
- Works with any wall segment size
- Uses wall's actual mesh bounds
- `towerWallLength` is independent of wall segment size

### Scenario 3: Mixed Wall Types
- System works with different wall prefabs
- Relies on `WallConnectionSystem` component
- Tower inherits connections regardless of wall type

## Changelog

### Version 1.0 (Current)
- Added perfect rotation alignment
- Implemented multi-segment wall replacement
- Added modular configuration system
- Position adjustment for better alignment
- Backward compatibility maintained

## Future Enhancements

Potential improvements:
- Auto-calculate `towerWallLength` from tower mesh bounds
- Visual preview showing which walls will be replaced
- Resource refund for replaced wall segments
- Custom snap points for irregular towers
- Support for curved wall sections

## Support

For issues or questions:
1. Check `TowerPlacementHelper.cs:236-253` for debug visualization
2. Review `TowerDataSO` settings in Inspector
3. Verify wall has `WallConnectionSystem` component
4. Enable Gizmos to see snap indicators

## Related Systems

- **Wall Placement System**: See `WALL_SYSTEM_GUIDE.md`
- **Gate Placement**: Similar system in `GatePlacementHelper.cs`
- **Building Manager**: Core placement in `BuildingManager.cs`
- **Wall Connections**: See `WallConnectionSystem.cs`
