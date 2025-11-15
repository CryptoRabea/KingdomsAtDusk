# RTS Building Selection & Unit Training System - Setup Guide

## Overview

This system allows players to:
- Click on buildings to select them
- View building details in a UI panel
- Train units from buildings (like barracks, stables, etc.)
- See training progress and queue status

## Components Created

### Core Components
1. **BuildingSelectable** - Makes buildings clickable
2. **BuildingSelectionManager** - Handles building selection input
3. **UnitTrainingQueue** - Manages unit training queue for buildings
4. **BuildingDataSO** (Extended) - Now includes trainable units configuration

### UI Components
1. **BuildingDetailsUI** - Shows selected building details and training options
2. **TrainUnitButton** - Button for training specific unit types

### Events Added
- `BuildingSelectedEvent` - Published when building is clicked
- `BuildingDeselectedEvent` - Published when building is deselected
- `UnitTrainingStartedEvent` - Published when unit training starts
- `UnitTrainingCompletedEvent` - Published when unit training finishes
- `TrainingProgressEvent` - Published during training progress

---

## Setup Instructions

### Step 1: Configure Your Building Prefabs

For each building that should be selectable:

1. Add the **BuildingSelectable** component
2. Configure visual feedback:
   - Enable "Use Color Highlight" for color change on selection
   - Set "Selected Color" (default: cyan)
   - Or assign a "Selection Indicator" GameObject (like a ring)

For buildings that can train units:

1. Add the **UnitTrainingQueue** component
2. Configure settings:
   - Set "Max Queue Size" (default: 5)
   - Optionally set a custom "Spawn Point" (where units appear)
   - If no spawn point is set, units spawn 3 units in front of the building

### Step 2: Setup Building Data (ScriptableObjects)

For buildings that can train units:

1. Open your BuildingDataSO in the Inspector
2. Enable **"Can Train Units"**
3. Add entries to **"Trainable Units"** list:
   - Assign the UnitConfigSO
   - Set resource costs (Wood, Food, Gold, Stone)
   - Set training time in seconds

Example configuration:
```
Building: Barracks
├─ Can Train Units: ✓
└─ Trainable Units:
    ├─ [0] Soldier
    │   ├─ Unit Config: SoldierConfig (UnitConfigSO)
    │   ├─ Wood Cost: 50
    │   ├─ Food Cost: 25
    │   ├─ Training Time: 5s
    └─ [1] Archer
        ├─ Unit Config: ArcherConfig (UnitConfigSO)
        ├─ Wood Cost: 40
        ├─ Gold Cost: 30
        ├─ Training Time: 4s
```

### Step 3: Setup Building Selection Manager

1. Create an empty GameObject (or use GameManager)
2. Add **BuildingSelectionManager** component
3. Configure:
   - **Click Action**: Reference to your Input Action for clicking (e.g., "Player/Click")
   - **Position Action**: Reference to mouse position input (e.g., "Player/Point")
   - **Building Layer**: Set to the layer your buildings are on
   - **Main Camera**: Will auto-find Camera.main if not set

### Step 4: Create the UI

#### A. Create Building Details Panel

1. Create a UI Canvas (if you don't have one)
2. Create a Panel GameObject named "BuildingDetailsPanel"
3. Add the **BuildingDetailsUI** component
4. Design your UI with these elements:

**Required UI Elements:**
- Panel Root (the main panel GameObject)
- Building Name Text (TextMeshProUGUI)
- Building Description Text (TextMeshProUGUI)
- Building Icon (Image)
- Training Queue Panel (GameObject)
- Queue Count Text (TextMeshProUGUI)
- Training Progress Bar (Image with Fill type)
- Current Training Text (TextMeshProUGUI)
- Unit Button Container (Transform/VerticalLayout)

5. Assign all references in the BuildingDetailsUI component

#### B. Create Train Unit Button Prefab

1. Create a UI Button GameObject
2. Add **TrainUnitButton** component
3. Design the button with:
   - Button component (auto-found)
   - Unit Icon (Image)
   - Unit Name Text (TextMeshProUGUI)
   - Cost Text (TextMeshProUGUI)
   - Training Time Text (TextMeshProUGUI)

4. Assign all references
5. Save as a Prefab
6. Assign this prefab to BuildingDetailsUI's "Train Unit Button Prefab" field

### Step 5: Configure Layers

Make sure your buildings are on a specific layer (e.g., "Buildings"):

1. Create a layer named "Buildings" in Project Settings
2. Assign your building prefabs to this layer
3. Set the BuildingSelectionManager's "Building Layer" to include this layer

### Step 6: Setup Input Actions

Make sure you have Input Actions configured:

1. Open your InputActions asset
2. Ensure you have:
   - A "Click" action (Button type)
   - A "Point" action (Value/Vector2 type)

---

## Usage Example

### Creating a Barracks That Trains Soldiers

1. **Create Building Data:**
   ```
   Right-click in Project > Create > RTS > BuildingData
   Name: "BarracksData"
   ```

2. **Configure Barracks:**
   - Building Name: "Barracks"
   - Building Type: Military
   - Can Train Units: ✓
   - Add Trainable Unit:
     - Unit Config: SoldierConfig
     - Wood Cost: 50
     - Food Cost: 25
     - Training Time: 5

3. **Setup Building Prefab:**
   - Add Building component (already exists)
   - Add BuildingSelectable component
   - Add UnitTrainingQueue component
   - Set layer to "Buildings"

4. **Place in Scene:**
   - Drag prefab to scene
   - Click on it during play mode
   - UI panel appears with train button
   - Click "Train Soldier" button
   - Unit trains and spawns after 5 seconds

---

## How It Works

### Selection Flow
1. Player clicks on building
2. BuildingSelectionManager raycasts to find building
3. BuildingSelectable.Select() is called
4. BuildingSelectedEvent is published
5. BuildingDetailsUI receives event and shows panel

### Training Flow
1. Player clicks "Train Unit" button
2. TrainUnitButton calls UnitTrainingQueue.TryTrainUnit()
3. System checks resources and queue capacity
4. Resources are spent
5. Unit added to training queue
6. UnitTrainingStartedEvent published
7. Training progresses over time (TrainingProgressEvent published)
8. When complete, unit spawns at spawn point
9. UnitTrainingCompletedEvent and UnitSpawnedEvent published

---

## Testing Checklist

- [ ] Can click on buildings to select them
- [ ] Visual feedback appears (color change or indicator)
- [ ] BuildingDetailsUI panel shows when building selected
- [ ] Train unit buttons appear for military buildings
- [ ] Clicking train button starts unit training
- [ ] Training progress bar fills over time
- [ ] Queue count updates correctly
- [ ] Resources are spent when training starts
- [ ] Button becomes unclickable when can't afford
- [ ] Unit spawns at correct position when training completes
- [ ] Multiple units can be queued
- [ ] Clicking empty space deselects building

---

## Troubleshooting

### Buildings not selectable
- Check building has BuildingSelectable component
- Check building is on correct layer
- Check BuildingSelectionManager has correct layer mask
- Check Input Actions are properly configured

### No UI showing
- Check BuildingDetailsUI is active in scene
- Check all UI references are assigned
- Check EventBus is working (check other events)

### Units not spawning
- Check UnitConfigSO has unitPrefab assigned
- Check building has UnitTrainingQueue component
- Check spawn point position (use Gizmos in Scene view)

### Resources not being spent
- Check ResourceManager is registered in ServiceLocator
- Check TrainableUnitData has costs configured

---

## Future Enhancements

Possible additions you could make:
- Cancel training button (with partial refund)
- Training speed upgrades
- Multiple training queues per building
- Rally point system (units move to rally point after spawning)
- Hotkeys for training specific units
- Training sounds and visual effects
- Queue management (reorder, remove specific units)

---

## Example Scene Setup

```
GameManager (GameObject)
├─ BuildingSelectionManager
├─ UnitSelectionManager
└─ ... other managers

Canvas (GameObject)
├─ ResourceUI
├─ BuildingHUD
└─ BuildingDetailsPanel
    └─ BuildingDetailsUI

Buildings (in scene)
├─ Barracks (can train: Soldier, Archer)
│   ├─ Building
│   ├─ BuildingSelectable
│   └─ UnitTrainingQueue
├─ Stables (can train: Cavalry)
│   ├─ Building
│   ├─ BuildingSelectable
│   └─ UnitTrainingQueue
└─ TownHall (no training)
    ├─ Building
    └─ BuildingSelectable
```

---

## API Reference

### UnitTrainingQueue

**Public Methods:**
- `bool TryTrainUnit(TrainableUnitData unitData)` - Add unit to training queue
- `void CancelCurrentTraining(bool refund = true)` - Cancel current training
- `void ClearQueue()` - Clear entire queue

**Public Properties:**
- `int QueueCount` - Total units in queue (including current)
- `bool IsTraining` - Is currently training a unit
- `TrainingQueueEntry CurrentTraining` - Current training entry (null if none)

### BuildingSelectable

**Public Methods:**
- `void Select()` - Select this building
- `void Deselect()` - Deselect this building

**Public Properties:**
- `bool IsSelected` - Is building currently selected

---

Enjoy your new RTS building and unit training system! 🏰⚔️
