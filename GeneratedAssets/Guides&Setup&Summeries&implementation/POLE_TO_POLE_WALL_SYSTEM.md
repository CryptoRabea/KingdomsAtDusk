# Pole-to-Pole Wall System Guide

## Overview

The new wall system allows you to build walls by placing poles. First, you place a pole with a click, then a second pole appears under your cursor showing a straight line preview from the first pole to the cursor position. The system counts the resources needed for all wall segments between the poles and places them automatically when you confirm.

## Features

- **Pole-Based Placement**: Click to place first pole, click again to place all wall segments
- **Line Preview**: Visual line showing where walls will be placed
- **Resource Counting**: Real-time calculation of resources needed for all segments
- **Automatic Connection**: Walls automatically connect to adjacent walls using the WallConnectionSystem
- **Multiple Construction Modes**:
  - **Instant**: Walls are built instantly (no delay)
  - **Timed**: Walls take time to build automatically (no workers needed)
  - **Segment Without Workers**: Each segment builds one at a time automatically
  - **Segment With Workers**: Each segment requires a worker assignment (one worker per segment)

## Components

### 1. WallPlacementController

Located at: `/Assets/Scripts/RTSBuildingsSystems/WallPlacementController.cs`

Handles the pole-to-pole placement logic with preview and resource counting.

**Key Features:**
- Manages first and second pole placement
- Creates line preview between poles
- Calculates wall segments needed
- Counts total resource cost
- Places all wall segments on confirmation

**Public Methods:**
- `StartPlacingWalls(BuildingDataSO wallData)` - Start wall placement mode
- `CancelWallPlacement()` - Cancel current wall placement
- `IsPlacingWalls` - Check if currently placing walls
- `GetTotalCost()` - Get resource cost for current wall segments
- `GetRequiredSegments()` - Get number of segments to be placed

### 2. ConstructionMode Enum

Located at: `/Assets/Scripts/RTSBuildingsSystems/ConstructionMode.cs`

Defines different construction modes:
- `Instant` - Building constructed instantly
- `Timed` - Building takes time to construct automatically
- `SegmentWithoutWorkers` - Segments build one at a time automatically
- `SegmentWithWorkers` - Segments require worker assignment

### 3. WallSegmentConstructor

Located at: `/Assets/Scripts/RTSBuildingsSystems/WallSegmentConstructor.cs`

Handles segment-based construction for walls with different modes.

**Key Features:**
- Supports all four construction modes
- Visual progress indication
- Worker assignment system for SegmentWithWorkers mode
- Automatic completion detection

**Public Methods:**
- `SetConstructionMode(ConstructionMode mode)` - Change construction mode
- `GetConstructionMode()` - Get current construction mode
- `AssignWorkerToSegment(int segmentIndex, GameObject worker)` - Assign worker to segment
- `RemoveWorkerFromSegment(int segmentIndex)` - Remove worker from segment
- `GetTotalProgress()` - Get overall construction progress (0-1)
- `GetCompletedSegmentsCount()` - Get number of completed segments
- `IsConstructionComplete()` - Check if construction is done

### 4. WallResourcePreviewUI

Located at: `/Assets/Scripts/UI/WallResourcePreviewUI.cs`

Displays resource cost preview during wall placement.

**Features:**
- Shows segment count
- Displays resource costs (Wood, Food, Gold, Stone)
- Color-codes based on affordability (green/red)
- Follows cursor or fixed position

## Setup Instructions

### Step 1: Update BuildingManager

1. Open your scene and locate the BuildingManager GameObject
2. In the Inspector, find the **Wall Placement** section
3. Assign the **WallPlacementController** reference:
   - Either drag an existing WallPlacementController GameObject
   - Or create a new GameObject with the WallPlacementController component
4. Set **Use Wall Placement For Walls** to `true`

### Step 2: Setup WallPlacementController

1. Create a new GameObject in your scene: `WallPlacementController`
2. Add the `WallPlacementController` component
3. Configure the following settings:

**References:**
- **Main Camera**: Assign your main camera
- **Ground Layer**: Set to your ground/terrain layer

**Visual Settings:**
- **Valid Preview Material**: Green semi-transparent material
- **Invalid Preview Material**: Red semi-transparent material
- **Line Preview Renderer**: Will be created automatically if not assigned
- **Line Width**: 0.2 (default)
- **Valid Line Color**: Green
- **Invalid Line Color**: Red

**Placement Settings:**
- **Grid Size**: 1 (must match WallConnectionSystem gridSize)
- **Use Grid Snapping**: true

**Pole Settings:**
- **Pole Prefab**: Optional pole visual (will create simple cylinder if not set)
- **Pole Height**: 2.0

### Step 3: Setup Wall Prefabs

1. Open your wall prefab
2. Ensure it has these components:
   - `Building` component
   - `WallConnectionSystem` component
   - `WallSegmentConstructor` component (NEW!)

3. Configure the `WallSegmentConstructor`:
   - **Construction Mode**: Choose from dropdown
     - `Instant` - Walls built immediately
     - `Timed` - Walls build over time (5 seconds default)
     - `SegmentWithoutWorkers` - Each segment builds sequentially
     - `SegmentWithWorkers` - Requires worker assignment
   - **Segment Construction Time**: Time per segment (default 5 seconds)
   - **Show Construction Progress**: Enable visual progress
   - **Construction Visual**: Optional GameObject shown during construction
   - **Incomplete Color**: Gray color for building segments
   - **Complete Color**: White color for completed segments

### Step 4: Setup UI (Optional)

1. Create a Canvas GameObject if you don't have one
2. Add a Panel GameObject as a child
3. Add the `WallResourcePreviewUI` component to the Canvas or Panel
4. Configure:
   - **Wall Placement Controller**: Assign your WallPlacementController
   - **Canvas**: Assign your Canvas
   - **Preview Panel**: Create a panel with the following children:
     - TextMeshProUGUI for segment count
     - TextMeshProUGUI for each resource (Wood, Food, Gold, Stone)
     - Icons for each resource
   - **Follow Cursor**: true to follow mouse, false for fixed position
   - **Screen Offset**: Adjust offset from cursor

## Usage

### Basic Wall Placement

1. **Start Placement**: Click the wall button in your building UI
   - The system automatically detects if it's a wall and uses pole-to-pole mode

2. **Place First Pole**: Click on the ground where you want the wall to start
   - A pole appears at the clicked position

3. **Preview Second Pole**: Move your cursor to where you want the wall to end
   - A line preview shows from first pole to cursor
   - Wall segment previews appear along the line
   - Resource cost is displayed in real-time

4. **Confirm Placement**: Click to place all wall segments
   - Resources are deducted
   - All wall segments are placed instantly
   - Walls automatically connect to adjacent walls

5. **Cancel**: Press ESC or right-click to cancel
   - First cancel goes back to first pole placement
   - Second cancel exits wall placement mode

### Construction Modes

#### Instant Mode
- Walls appear fully built immediately
- No construction time
- Best for testing or creative mode

#### Timed Mode
- Walls take time to construct automatically
- No workers needed
- Visual progress indication
- Set `segmentConstructionTime` to control build speed

#### Segment Without Workers
- Each segment builds one at a time
- Automatic progression
- No worker assignment needed
- Good for showing construction progress without worker management

#### Segment With Workers
- Each segment requires a worker to be assigned
- One worker per segment maximum
- Can assign multiple workers to different segments
- Workers must be assigned via script:

```csharp
// Example: Assign a worker to a wall segment
WallSegmentConstructor wallConstructor = wallObject.GetComponent<WallSegmentConstructor>();
GameObject worker = GetAvailableWorker();
bool success = wallConstructor.AssignWorkerToSegment(0, worker);

// Remove worker from segment
wallConstructor.RemoveWorkerFromSegment(0);

// Check worker assignment
GameObject assignedWorker = wallConstructor.GetAssignedWorker(0);
```

## Integration with Existing Systems

### BuildingManager Integration

The system integrates seamlessly with BuildingManager:
- Automatically detects walls (BuildingType.Defensive + WallConnectionSystem component)
- Switches to pole-to-pole mode for walls
- Uses regular placement for non-wall buildings
- Can be toggled on/off with `useWallPlacementForWalls` flag

### WallConnectionSystem Integration

Walls placed with the pole-to-pole system automatically:
- Register in the wall registry
- Detect adjacent walls
- Update visual connections
- Connect seamlessly with existing walls

### Resource System Integration

The system fully integrates with the resource system:
- Calculates total cost for all segments
- Checks affordability in real-time
- Spends resources only when placement is confirmed
- Publishes ResourcesSpentEvent for UI updates

### Event System Integration

Events published:
- `BuildingPlacedEvent` - For each wall segment placed
- `ResourcesSpentEvent` - When resources are spent or placement fails
- `BuildingCompletedEvent` - When construction completes

## Configuration Tips

### Adjusting Grid Size

Make sure these values match:
- `WallPlacementController.gridSize` = 1.0
- `WallConnectionSystem.gridSize` = 1.0
- `BuildingManager.gridSize` = 1.0

### Performance

For large walls (100+ segments):
- Use `Instant` or `Timed` mode for better performance
- Avoid `SegmentWithWorkers` mode for very long walls
- Consider breaking walls into smaller sections

### Visual Customization

**Line Preview:**
- Adjust `lineWidth` for thicker/thinner lines
- Change `validLineColor` and `invalidLineColor` for different visual feedback

**Pole Visual:**
- Assign custom `polePrefab` for unique pole appearance
- Adjust `poleHeight` to match your wall height

**Construction Progress:**
- Customize `incompleteColor` and `completeColor` in WallSegmentConstructor
- Toggle `showConstructionProgress` on/off

## Troubleshooting

### Walls Not Placing

**Issue**: Clicking doesn't place walls
- **Check**: Make sure `WallPlacementController` is assigned in BuildingManager
- **Check**: Verify `useWallPlacementForWalls` is enabled
- **Check**: Ensure wall prefab has `WallConnectionSystem` component

### Resource Cost Not Showing

**Issue**: Resource preview UI not displaying
- **Check**: WallResourcePreviewUI is properly configured
- **Check**: Wall Placement Controller reference is assigned
- **Check**: Preview panel is enabled in hierarchy

### Walls Not Connecting

**Issue**: Walls don't connect to adjacent walls
- **Check**: All walls have same `gridSize` value
- **Check**: `WallConnectionSystem` is enabled on wall prefabs
- **Check**: Wall positions are properly snapped to grid

### Construction Not Working

**Issue**: Walls not building/completing
- **Check**: `WallSegmentConstructor` is attached to wall prefab
- **Check**: Construction mode is set correctly
- **Check**: For SegmentWithWorkers mode, workers are assigned
- **Check**: `segmentConstructionTime` is not too high

### Line Preview Not Showing

**Issue**: No line between poles
- **Check**: LineRenderer is created and enabled
- **Check**: Valid/invalid materials are assigned
- **Check**: Line colors are not transparent

## API Reference

### WallPlacementController

```csharp
// Start placing walls
public void StartPlacingWalls(BuildingDataSO wallData)

// Cancel placement
public void CancelWallPlacement()

// Check placement status
public bool IsPlacingWalls { get; }

// Get cost information
public Dictionary<ResourceType, int> GetTotalCost()
public int GetRequiredSegments()
```

### WallSegmentConstructor

```csharp
// Construction mode
public void SetConstructionMode(ConstructionMode mode)
public ConstructionMode GetConstructionMode()

// Worker assignment (SegmentWithWorkers mode only)
public bool AssignWorkerToSegment(int segmentIndex, GameObject worker)
public bool RemoveWorkerFromSegment(int segmentIndex)
public GameObject GetAssignedWorker(int segmentIndex)

// Progress tracking
public float GetTotalProgress()
public int GetCompletedSegmentsCount()
public int GetTotalSegmentsCount()
public bool IsConstructionComplete()
```

### BuildingManager

```csharp
// Start placing any building (automatically detects walls)
public void StartPlacingBuilding(BuildingDataSO buildingData)

// Cancel any placement
public void CancelPlacement()

// Check placement status (includes wall placement)
public bool IsPlacing { get; }
```

## Examples

### Example 1: Basic Wall Placement

```csharp
// Get building manager
BuildingManager buildingManager = Object.FindFirstObjectByType<BuildingManager>();

// Get wall data
BuildingDataSO wallData = buildingManager.GetBuildingByName("Stone Wall");

// Start placing walls
buildingManager.StartPlacingBuilding(wallData);

// User clicks twice to place walls
```

### Example 2: Programmatic Wall Construction with Workers

```csharp
// Place walls programmatically
WallPlacementController wallController = Object.FindFirstObjectByType<WallPlacementController>();
BuildingManager buildingManager = Object.FindFirstObjectByType<BuildingManager>();
BuildingDataSO wallData = buildingManager.GetBuildingByName("Stone Wall");

wallController.StartPlacingWalls(wallData);

// After walls are placed, assign workers
GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
foreach (GameObject wall in walls)
{
    WallSegmentConstructor constructor = wall.GetComponent<WallSegmentConstructor>();
    if (constructor != null && constructor.GetConstructionMode() == ConstructionMode.SegmentWithWorkers)
    {
        GameObject worker = GetAvailableWorker();
        if (worker != null)
        {
            constructor.AssignWorkerToSegment(0, worker);
        }
    }
}
```

### Example 3: Change Construction Mode at Runtime

```csharp
// Switch all walls to instant construction
GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
foreach (GameObject wall in walls)
{
    WallSegmentConstructor constructor = wall.GetComponent<WallSegmentConstructor>();
    if (constructor != null)
    {
        constructor.SetConstructionMode(ConstructionMode.Instant);
    }
}
```

## Future Enhancements

Possible additions:
- Curved wall support
- Multi-height walls
- Wall gates and doors
- Wall upgrade system
- Damage and repair system
- Wall defense bonuses

## Credits

- Wall Connection System: Original modular wall system
- Pole-to-Pole Placement: New placement system
- Construction Modes: Flexible building mechanics
- Resource Integration: Seamless resource management
