# Building Spawn Points & Rally Points

This document explains how the spawn point and rally point system works for buildings that train units.

## Overview

- **Spawn Point**: Fixed position (child of building prefab) where units appear when training completes
- **Rally Point**: Player-configurable position where units automatically move to after spawning

## Spawn Points

### What is a Spawn Point?

A spawn point is a fixed location attached to a building prefab where newly trained units will appear. Each building that can train units should have a spawn point configured.

### Adding Spawn Points to Buildings

#### Method 1: Using Unity Menu (Recommended)

1. **For a Single Building:**
   - Select the building prefab or instance in the scene/project
   - Go to menu: `RTS > Building Tools > Add Spawn Point to Selected Building`
   - A spawn point will be created 3 units in front of the building
   - Adjust the position as needed in the Scene view

2. **For All Buildings (Batch):**
   - Go to menu: `RTS > Building Tools > Batch Add Spawn Points to All Building Prefabs`
   - This will automatically add spawn points to all building prefabs in `Assets/Prefabs/BuildingPrefabs&Data` that:
     - Have a `UnitTrainingQueue` component
     - Don't already have a spawn point

#### Method 2: Manual Setup

1. Open your building prefab in the editor
2. Create a new empty GameObject as a child of the building
3. Name it "SpawnPoint"
4. Add the `BuildingSpawnPoint` component to it
5. Position it where you want units to spawn (usually 2-4 units away from the building)

### Positioning Guidelines

**Good Spawn Point Positions:**
- Outside the building's collision bounds
- On a walkable NavMesh area
- 2-4 units away from the building center
- Accessible from multiple directions

**Bad Spawn Point Positions:**
- Inside the building geometry
- On non-walkable terrain
- Too close to the building (units may get stuck)
- In other buildings' collision zones

### Spawn Point Inspector Features

When you select a spawn point in the editor, the inspector shows:

- **Help information** about spawn point placement
- **World position** display
- **Quick position presets**: Front, Back, Left, Right buttons to snap to common positions

### Backward Compatibility

If a building prefab doesn't have a `BuildingSpawnPoint` component, the system will automatically create a spawn point 3 units in front of the building at runtime. However, it's recommended to add spawn points to prefabs for better control.

## Rally Points

### What is a Rally Point?

A rally point is a player-set destination where units will automatically move after spawning. This allows players to direct newly trained units to a specific location without manually selecting them.

### Setting Rally Points

There are **two ways** to set a rally point:

#### Method 1: Right-Click (Quick Method)

1. Select a building that can train units
2. **Right-click** on the ground where you want units to gather
3. A flag will appear at that location
4. All units trained in this building will move to the rally point after spawning

#### Method 2: Building Details UI Button

1. Select a building that can train units
2. The Building Details panel appears
3. Click the **"Set Rally Point"** button
4. Click on the ground where you want the rally point
5. Click "Cancel" to exit rally point mode

### Rally Point Features

- **Visual Indicator**: A flag appears at the rally point location
- **Only Visible When Selected**: The flag only shows when the building is selected
- **Per-Building**: Each building has its own rally point
- **Persistent**: Rally points remain set until changed or the building is destroyed
- **Automatic Movement**: Units automatically move to the rally point after spawning

### Clearing Rally Points

Rally points can be cleared by:
- Setting a new rally point (replaces the old one)
- Destroying the building

## Technical Details

### Components

- **`BuildingSpawnPoint`**: Component that marks spawn point location on building prefabs
  - Located at: `Assets/Scripts/RTSBuildingsSystems/BuildingSpawnPoint.cs`
  - Provides visual gizmos in the editor (cyan wire sphere)

- **`UnitTrainingQueue`**: Manages unit training and spawn/rally point logic
  - Located at: `Assets/Scripts/RTSBuildingsSystems/UnitTrainingQueue.cs`
  - Automatically finds `BuildingSpawnPoint` in children
  - Creates fallback spawn point if none exists

- **`RallyPointFlag`**: Visual indicator for rally points
  - Located at: `Assets/Scripts/RTSBuildingsSystems/RallyPointFlag.cs`
  - Shows/hides based on building selection

- **`BuildingDetailsUI`**: UI panel for building interaction
  - Located at: `Assets/Scripts/UI/BuildingDetailsUI.cs`
  - Contains "Set Rally Point" button

### Editor Tools

- **`BuildingSpawnPointEditor`**: Editor utilities for adding spawn points
  - Located at: `Assets/Scripts/RTSBuildingsSystems/Editor/BuildingSpawnPointEditor.cs`
  - Provides menu items: `RTS > Building Tools`

### Workflow

1. **Building Placed** → Building instantiated with `BuildingSpawnPoint` child
2. **Unit Training Starts** → Player queues unit for training
3. **Training Completes** → Unit spawns at `BuildingSpawnPoint` position
4. **Rally Point Check** → If rally point is set, unit moves there automatically
5. **Visual Feedback** → Rally point flag visible when building is selected

## Debug Visualization

### In Editor (Gizmos)

When a building with `UnitTrainingQueue` is selected:
- **Blue sphere**: Spawn point location
- **Green sphere**: Rally point location (if set)
- **Yellow line**: Path from spawn point to rally point

When a `BuildingSpawnPoint` is selected:
- **Cyan wire sphere**: Spawn point indicator
- **Yellow line**: Connection to parent building

### In Play Mode

- **Cyan sphere**: Spawn point (always visible when building selected)
- **Green flag**: Rally point (only visible when building selected)

## Troubleshooting

### Units Not Spawning

1. Check if building has `UnitTrainingQueue` component
2. Verify spawn point exists (check for `BuildingSpawnPoint` in children)
3. Ensure spawn point is on a valid NavMesh area
4. Check console for warnings about missing spawn point

### Units Spawning Inside Building

1. Select the building prefab
2. Select the SpawnPoint child object
3. Move it outside the building's collision bounds
4. Verify it's on walkable NavMesh (blue areas in NavMesh view)

### Rally Point Not Working

1. Ensure building is selected before setting rally point
2. Check that right-click is targeting the ground layer
3. Verify `BuildingSelectionManager` exists in the scene
4. Check console for errors about missing components

### Rally Point Flag Not Visible

1. Make sure the building is selected
2. Check if `RallyPointFlag` component exists on the building
3. Verify rally point has been set (green sphere in gizmos)
4. Check if flag is obscured by terrain or other objects

## Examples

### Example: Barracks Spawn Point Setup

```
BarracksPrefab
├── Building (Building component)
├── UnitTrainingQueue (UnitTrainingQueue component)
├── RallyPointFlag (RallyPointFlag component)
├── Model (3D mesh)
└── SpawnPoint (BuildingSpawnPoint component)
    └── Position: (0, 0, 3)  // 3 units in front
```

### Example: Rally Point Usage

1. Player builds a Barracks
2. Player selects the Barracks
3. Player right-clicks on a position near the enemy base
4. A flag appears at that position
5. Player queues 5 soldiers for training
6. As each soldier finishes training:
   - Soldier spawns at the spawn point (3 units in front of Barracks)
   - Soldier automatically moves to the rally point near enemy base

## Best Practices

1. **Always add spawn points to building prefabs** (don't rely on auto-generation)
2. **Position spawn points on NavMesh** to prevent units getting stuck
3. **Test spawn points in play mode** to verify units can path correctly
4. **Use rally points for strategic positioning** (e.g., defensive positions, gathering points)
5. **Update rally points dynamically** during gameplay as strategy changes
