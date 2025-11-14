# Building Selection & Unit Spawning Setup Guide

This guide explains how to fix building selection and unit spawning issues in your RTS game.

## Recent Changes

The recent revert (commit 0e6a928) simplified the spawn point system by removing rally point functionality. This fix restores the spawn point button functionality that was temporarily disabled.

## Fixed Issues

1. ✅ **Spawn Point Button** - Re-enabled the UI button for setting spawn points
2. ✅ **Spawn Point Mode** - Re-enabled synchronization between UI and BuildingSelectionManager
3. ✅ **Validation Tool** - Added BuildingSystemValidator to diagnose scene configuration issues

## Unity Scene Setup Checklist

### 1. Building Selection Manager Setup

**Add BuildingSelectionManager to your scene:**

1. Create a GameObject named "BuildingSelectionManager" (or add to existing GameManager)
2. Add the `BuildingSelectionManager` component
3. Configure the following in the Inspector:

   **Input Settings:**
   - **Click Action**: Assign Input Action for left mouse button (e.g., "Click" from Player Input Actions)
   - **Right Click Action**: Assign Input Action for right mouse button
   - **Position Action**: Assign Input Action for mouse position

   **Selection Settings:**
   - **Building Layer**: Select the layer your buildings are on (create "Building" layer if needed)
   - **Ground Layer**: Select the layer(s) for ground/terrain (e.g., "Default", "Ground")
   - **Main Camera**: Assign your main camera (or leave empty to auto-find)

   **Debug:**
   - **Enable Debug Logs**: Check this to see selection debug messages in console

### 2. Configure Input Actions

If you don't have Input Actions set up:

1. **Window → Package Manager**: Ensure "Input System" package is installed
2. **Assets → Create → Input Actions**: Create new Input Actions asset
3. Add the following actions:
   - **Click** (Button, Left Mouse Button)
   - **RightClick** (Button, Right Mouse Button)
   - **Position** (Value, Mouse Position)
4. Generate C# class (if needed) and save
5. Reference these actions in BuildingSelectionManager

### 3. Building Setup

For EACH building in your scene:

1. **Add Components:**
   - `Building` component (core building logic)
   - `BuildingSelectable` component (enables selection)
   - `Collider` component (BoxCollider, etc. - required for raycasting)
   - `UnitTrainingQueue` component (if building can train units)

2. **Set Layer:**
   - Change building's layer to "Building" (create this layer if it doesn't exist)
   - When prompted, also apply to children if needed

3. **Configure Building Data:**
   - Assign a `BuildingDataSO` (ScriptableObject) to the Building component
   - If the building trains units, check `canTrainUnits` and add trainable units to the list

4. **Spawn Point (for buildings that train units):**
   - UnitTrainingQueue auto-creates a spawn point if not assigned
   - Or manually create a child GameObject named "SpawnPoint" and assign it

### 4. Building Details UI Setup

For the UI that shows when selecting buildings:

1. **Find or create BuildingDetailsUI:**
   - Should be on a Canvas in your scene
   - Add `BuildingDetailsUI` component if not present

2. **Configure in Inspector:**

   **Panel References:**
   - **Panel Root**: The parent GameObject that contains all UI elements
   - **Building Name Text**: TextMeshPro for building name
   - **Building Description Text**: TextMeshPro for description
   - **Building Icon**: Image for building icon

   **Training Queue Display:**
   - **Training Queue Panel**: Panel showing training progress
   - **Queue Count Text**: Shows number of units in queue
   - **Training Progress Bar**: Image with Image Type: Filled
   - **Current Training Text**: Shows what's currently training

   **Unit Training Buttons:**
   - **Unit Button Container**: Transform to hold train unit buttons
   - **Train Unit Button Prefab**: Prefab for individual unit training button

   **Spawn Point Button:**
   - **Set Spawn Point Button**: Button to toggle spawn point mode
   - **Set Spawn Point Button Text**: Text that shows "Set Spawn Point" / "Cancel"

   **References:**
   - **Selection Manager**: Assign your BuildingSelectionManager (or leave null for auto-find)

### 5. Layers Configuration

Create these layers if they don't exist:

1. **Edit → Project Settings → Tags and Layers**
2. Add layers:
   - **Building** (e.g., Layer 6)
   - **Ground** (e.g., Layer 7) - Optional, can use Default

3. **Assign layers:**
   - All buildings → Building layer
   - Ground/Terrain → Ground layer

## Validation Tool

Run the validation tool to check your setup:

**Method 1 - Menu:**
1. **Tools → RTS → Validate Building System**

**Method 2 - Component:**
1. Add `BuildingSystemValidator` component to any GameObject
2. Check "Run Validation On Start" to run automatically
3. Or right-click component → "Validate Building System"

The validator will check:
- ✅ BuildingSelectionManager configuration
- ✅ Input actions assigned
- ✅ Layer masks configured
- ✅ Buildings have required components
- ✅ BuildingDetailsUI setup

Check the Console for validation results and fix any errors.

## How to Use

### Selecting Buildings

1. **Left-click** on a building to select it
2. Building highlights and UI panel shows building details
3. **Left-click** on empty ground to deselect

### Spawning Units

1. **Select a building** that can train units
2. **Click train button** for the unit you want
3. Unit is added to training queue
4. When training completes, unit spawns at the spawn point

### Setting Spawn Points

**Method 1 - UI Button:**
1. Select a building
2. Click "Set Spawn Point" button in UI
3. **Left-click** on ground where you want units to spawn
4. Flag appears at spawn point location

**Method 2 - Right-Click (Direct):**
1. Select a building
2. **Right-click** on ground where you want units to spawn
3. Flag appears at spawn point location

## Common Issues & Solutions

### Can't Select Buildings

**Symptoms:** Clicking on buildings does nothing

**Possible Causes:**
1. ❌ BuildingSelectionManager not in scene
   - **Fix:** Add BuildingSelectionManager component to a GameObject
2. ❌ Input Actions not assigned
   - **Fix:** Assign Click, RightClick, and Position actions in inspector
3. ❌ Building layer mask not set
   - **Fix:** Set "Building Layer" in BuildingSelectionManager inspector
4. ❌ Buildings missing BuildingSelectable component
   - **Fix:** Add BuildingSelectable to each building
5. ❌ Buildings missing Collider
   - **Fix:** Add BoxCollider or other collider to buildings
6. ❌ Buildings on wrong layer
   - **Fix:** Set building layer to "Building"

### Can't Spawn Units

**Symptoms:** Train buttons don't work or units don't spawn

**Possible Causes:**
1. ❌ Building missing UnitTrainingQueue component
   - **Fix:** Add UnitTrainingQueue component
2. ❌ Building Data not configured
   - **Fix:** Assign BuildingDataSO and set canTrainUnits = true
3. ❌ No trainable units in Building Data
   - **Fix:** Add TrainableUnitData to buildingData.trainableUnits
4. ❌ Unit prefab not assigned
   - **Fix:** Assign unit prefab in UnitConfig
5. ❌ Insufficient resources
   - **Fix:** Ensure you have enough resources to train the unit

### Spawn Point Button Doesn't Work

**Symptoms:** Clicking "Set Spawn Point" button does nothing

**This has been fixed in this update!** The button functionality was re-enabled.

If still not working:
1. ❌ BuildingSelectionManager not found
   - **Fix:** Ensure BuildingSelectionManager exists in scene
2. ❌ Ground layer not set
   - **Fix:** Set "Ground Layer" in BuildingSelectionManager inspector
3. ❌ No collider on ground
   - **Fix:** Ensure terrain/ground has a collider

## Testing

1. **Run validation tool** (Tools → RTS → Validate Building System)
2. **Enter Play Mode**
3. **Enable Debug Logs** in BuildingSelectionManager inspector
4. **Try selecting a building** - check console for debug messages
5. **Try training a unit** - should see training progress
6. **Try setting spawn point** - right-click or use UI button

## Files Changed

- `Assets/Scripts/UI/BuildingDetailsUI.cs` - Fixed spawn point button functionality
- `Assets/Scripts/RTSBuildingsSystems/BuildingSystemValidator.cs` - NEW validation tool
- `BUILDING_SYSTEM_SETUP.md` - NEW setup guide

## Code Architecture

The building system uses an event-driven architecture:

**Selection Flow:**
1. BuildingSelectionManager → Raycasts → Finds BuildingSelectable
2. BuildingSelectable.Select() → Publishes BuildingSelectedEvent
3. BuildingDetailsUI → Subscribes to event → Shows UI panel
4. TrainUnitButton → Calls UnitTrainingQueue.TryTrainUnit()
5. UnitTrainingQueue → Trains unit → Spawns prefab → Publishes events

**Key Events:**
- `BuildingSelectedEvent` - Building was selected
- `BuildingDeselectedEvent` - Building was deselected
- `UnitTrainingStartedEvent` - Unit added to training queue
- `UnitTrainingCompletedEvent` - Unit finished training
- `UnitSpawnedEvent` - Unit spawned in world
- `TrainingProgressEvent` - Training progress updated

## Need Help?

1. **Run the validation tool** first
2. **Enable debug logs** in BuildingSelectionManager
3. **Check console** for error messages and warnings
4. **Review this setup guide** for missing configuration

If issues persist, check that:
- Unity Input System package is installed
- All required components are present
- Layer masks are configured correctly
- Input actions are assigned
