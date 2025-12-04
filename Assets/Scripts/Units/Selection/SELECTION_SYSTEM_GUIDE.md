# Advanced Unit Selection System Guide

## Overview

KingdomsAtDusk features a comprehensive RTS-style unit selection system with multiple selection methods, group management, and extensive customization options.

## Table of Contents

1. [Features](#features)
2. [Components](#components)
3. [Setup Guide](#setup-guide)
4. [Usage](#usage)
5. [Configuration](#configuration)
6. [Events](#events)
7. [Advanced Features](#advanced-features)

---

## Features

### Core Selection Features

✅ **Single-Click Selection**
- Click on a unit to select it
- Shift+Click to add/remove units from selection
- Automatically handles overlapping units (nearest/furthest priority)

✅ **Drag Box Selection** (2D Screen-Based)
- Drag mouse to create a selection rectangle
- All units within the box are selected
- Visual feedback with customizable selection box

✅ **Double-Click Selection**
- Double-click anywhere to select ALL visible units on screen
- Configurable double-click timing
- Respects max selection limits and type filters

✅ **Mouse Hover Highlighting**
- Units highlight when mouse hovers over them
- Customizable hover color
- Separate visual feedback from selection

✅ **Drag Highlighting**
- Units highlight during drag selection (before release)
- Provides real-time feedback of what will be selected
- Can be enabled/disabled

### Advanced Features

✅ **Max Selection Limit**
- Configurable maximum number of units that can be selected
- Optional distance-based sorting (nearest units selected first)
- Prevents overwhelming selection counts

✅ **Unit Type Filtering**
- Filter selection by unit type: Owned, Friendly, Neutral, Enemy
- Configurable per-type inclusion/exclusion
- Based on Unity layers

✅ **Group/Squad Selection**
- Save selections to groups (Ctrl+1-9)
- Recall groups (1-9)
- Double-tap to recall and center camera on group
- 10 groups available (0-9)

✅ **3D World Cube Selection** (Alternative)
- Selection using 3D world-space box instead of screen rectangle
- Three detection methods: Overlap, Bounds, Position
- Visual 3D box with LineRenderer

✅ **Overlap Priority**
- When multiple units overlap, select nearest or furthest
- Configurable priority setting
- Uses raycast distance sorting

---

## Components

### 1. UnitSelectionManager (Enhanced)

**Location:** `Assets/Scripts/Units/Selection/UnitSelectionManager.cs`

**Purpose:** Main selection manager with all features

**Key Features:**
- 2D screen-based selection
- Double-click selection
- Hover highlighting
- Max selection limits
- Type filtering
- Distance sorting
- Drag highlighting

### 2. UnitSelectable

**Location:** `Assets/Scripts/Units/Components/UnitSelectable.cs`

**Purpose:** Makes individual units selectable

**Key Features:**
- Visual selection feedback (color + indicator)
- Hover highlighting support
- Event publishing
- MaterialPropertyBlock optimization (no material instances)

### 3. UnitGroupManager

**Location:** `Assets/Scripts/Units/Selection/UnitGroupManager.cs`

**Purpose:** Group/squad selection and recall

**Key Features:**
- Save selections to groups (Ctrl+Number)
- Recall groups (Number key)
- Double-tap to center camera
- Automatic cleanup of dead units
- 10 groups (0-9)

### 4. UnitSelection3D (Alternative)

**Location:** `Assets/Scripts/Units/Selection/UnitSelection3D.cs`

**Purpose:** 3D world-space box selection

**Key Features:**
- 3D cube selection in world space
- Three detection methods (Overlap, Bounds, Position)
- Visual 3D box with LineRenderer
- Ground-plane based dragging

### 5. RTSCommandHandler

**Location:** `Assets/Scripts/Units/Selection/RTSCommandHandler.cs`

**Purpose:** Handles RTS commands (move, attack)

**Key Features:**
- Right-click to move
- Right-click on enemy to attack
- Visual move markers
- Formation support ready

---

## Setup Guide

### Quick Setup (Drag & Drop)

1. **Add UnitSelectionManager to Scene**
   ```
   Create empty GameObject → "SelectionManager"
   Add Component → UnitSelectionManager
   ```

2. **Configure UnitSelectionManager**
   - Assign Input Actions (Click, Position)
   - Set Selectable Layer
   - Assign Selection Box UI (Image component)
   - Configure behavior options

3. **Add UnitSelectable to Units**
   ```
   Select your unit prefab
   Add Component → UnitSelectable
   Assign selection indicator GameObject (optional)
   Configure colors
   ```

4. **Add UnitGroupManager (Optional)**
   ```
   Add to "SelectionManager" GameObject
   Add Component → UnitGroupManager
   Assign UnitSelectionManager reference
   ```

5. **Add RTSCommandHandler**
   ```
   Add to "SelectionManager" GameObject
   Add Component → RTSCommandHandler
   Assign references
   Set Ground and Unit layers
   ```

### Alternative: 3D Selection Setup

1. **Use UnitSelection3D Instead**
   ```
   Add Component → UnitSelection3D (instead of UnitSelectionManager)
   Configure ground layer
   Set box height
   Choose detection method
   ```

---

## Usage

### Player Controls

#### Selection
- **Left Click:** Select single unit (Shift to add/remove)
- **Left Drag:** Box selection
- **Double Click:** Select all visible units
- **Mouse Hover:** Highlight unit under cursor

#### Groups
- **Ctrl+1-9:** Save current selection to group
- **1-9:** Recall group
- **Double-tap 1-9:** Recall group and center camera

#### Commands
- **Right Click (Ground):** Move selected units
- **Right Click (Enemy):** Attack with selected units

### Code Examples

#### Subscribe to Selection Events

```csharp
using RTS.Core.Events;
using RTS.Units;

void OnEnable()
{
    EventBus.Subscribe<SelectionChangedEvent>(OnSelectionChanged);
    EventBus.Subscribe<UnitGroupRecalledEvent>(OnGroupRecalled);
}

void OnDisable()
{
    EventBus.Unsubscribe<SelectionChangedEvent>(OnSelectionChanged);
    EventBus.Unsubscribe<UnitGroupRecalledEvent>(OnGroupRecalled);
}

void OnSelectionChanged(SelectionChangedEvent evt)
{
    Debug.Log($"Selection changed: {evt.SelectionCount} units selected");
}

void OnGroupRecalled(UnitGroupRecalledEvent evt)
{
    Debug.Log($"Group {evt.GroupNumber} recalled with {evt.UnitCount} units");
    if (evt.WasDoubleTap)
    {
        Debug.Log("Camera centered on group");
    }
}
```

#### Programmatic Selection

```csharp
// Get reference to selection manager
UnitSelectionManager selectionManager = FindFirstObjectByType<UnitSelectionManager>();

// Get selected units
foreach (var unit in selectionManager.SelectedUnits)
{
    Debug.Log($"Selected unit: {unit.name}");
}

// Move selected units
selectionManager.MoveSelectedUnits(targetPosition);
```

#### Group Management

```csharp
// Get reference to group manager
UnitGroupManager groupManager = FindFirstObjectByType<UnitGroupManager>();

// Save current selection to group 1
groupManager.SaveGroup(1);

// Recall group 3
groupManager.RecallGroup(3);

// Get units in group 5
var unitsInGroup = groupManager.GetGroup(5);

// Clear group 2
groupManager.ClearGroup(2);

// Clear all groups
groupManager.ClearAllGroups();
```

---

## Configuration

### UnitSelectionManager Settings

#### Selection Behavior
- **Enable Max Selection:** Limit number of selected units
- **Max Selection Count:** Maximum units (default: 50)
- **Sort By Distance:** Sort by distance from drag start
- **Enable Double Click:** Enable double-click to select all visible
- **Double Click Time:** Time window for double-click (default: 0.3s)
- **Overlap Priority:** Nearest or Furthest when units overlap

#### Unit Type Filter
- **Enable Type Filter:** Enable unit type filtering
- **Select Owned:** Include player-owned units
- **Select Friendly:** Include friendly units
- **Select Neutral:** Include neutral units
- **Select Enemy:** Include enemy units

#### Drag Selection
- **Selection Box UI:** Reference to UI Image for selection box
- **Selection Box Color:** Color of selection box
- **Drag Threshold:** Minimum pixels to count as drag (default: 5)
- **Highlight During Drag:** Highlight units during drag

#### Hover Highlighting
- **Enable Hover Highlight:** Enable mouse hover highlighting
- **Hover Color:** Color for hover highlight

### UnitSelectable Settings

- **Selection Indicator:** GameObject to show when selected (e.g., ring)
- **Use Color Highlight:** Enable color-based highlighting
- **Selected Color:** Color when unit is selected (default: green)
- **Hover Indicator:** GameObject to show when hovered (optional)

### UnitGroupManager Settings

- **Number of Groups:** How many groups (default: 10 for 0-9)
- **Enable Double Tap Center:** Center camera on double-tap recall
- **Double Tap Time:** Time window for double-tap (default: 0.3s)
- **Clear Empty Groups:** Auto-remove null/dead units
- **Show Debug Messages:** Enable console logging

### UnitSelection3D Settings

- **Detection Type:** Overlap, Bounds, or Position
- **Box Height:** Height of 3D selection box (default: 50)
- **Ground Layer:** Layer for ground raycasting
- **Box Color:** Color of 3D selection box

---

## Events

### Available Events

All events are in the `RTS.Core.Events` namespace.

#### Selection Events

```csharp
// Published when selection count changes
public struct SelectionChangedEvent
{
    public int SelectionCount;
}

// Published when a unit is selected
public struct UnitSelectedEvent
{
    public GameObject Unit;
}

// Published when a unit is deselected
public struct UnitDeselectedEvent
{
    public GameObject Unit;
}

// Published when all visible units are selected (double-click)
public struct AllVisibleUnitsSelectedEvent
{
    public int UnitCount;
}

// Published when unit is hovered
public struct UnitHoveredEvent
{
    public GameObject Unit;
    public bool IsHovered;
}
```

#### Group Events

```csharp
// Published when a group is saved
public struct UnitGroupSavedEvent
{
    public int GroupNumber;
    public int UnitCount;
}

// Published when a group is recalled
public struct UnitGroupRecalledEvent
{
    public int GroupNumber;
    public int UnitCount;
    public bool WasDoubleTap;
}
```

---

## Advanced Features

### Unit Type Filtering

The system determines unit type based on Unity layers:

- **Owned:** "Player" or "PlayerUnit" layer
- **Enemy:** "Enemy" or "EnemyUnit" layer
- **Friendly:** "Friendly" or "Ally" layer
- **Neutral:** "Neutral" layer

**To configure:**
1. Set your unit GameObjects to appropriate layers
2. Enable "Enable Type Filter" in UnitSelectionManager
3. Check which types you want to be selectable

### Distance Sorting

When enabled, units are sorted by distance before selection limit is applied:

- **Drag Selection:** Distance from drag start position
- **Double-Click:** Distance from camera
- **3D Selection:** Distance from box start corner

This ensures nearest units are prioritized when max selection limit is reached.

### Overlap Priority

When clicking and multiple units are under the cursor:

- **Nearest:** Selects the closest unit to camera
- **Furthest:** Selects the furthest unit from camera

Useful for selecting units behind walls or in tight formations.

### 3D Detection Methods

**Overlap (Physics.OverlapBox)**
- Uses physics collision detection
- Most accurate but requires colliders
- Best performance for large scenes

**Bounds (Renderer.bounds)**
- Checks if renderer bounds intersect box
- Good for units with irregular shapes
- Slower than Overlap

**Position (Transform.position)**
- Only checks unit's position point
- Fastest method
- May miss large units if center is outside box

### Performance Tips

1. **Use MaterialPropertyBlock:** Already implemented in UnitSelectable - no material instances created
2. **Limit max selection:** Set reasonable limits (20-100 units)
3. **Use Overlap detection:** For 3D selection, Overlap is fastest
4. **Layer masks:** Properly set up layer masks to reduce raycasts
5. **Disable features:** Turn off hover highlighting if not needed

---

## Troubleshooting

### Selection Not Working

1. **Check Input Actions:** Ensure Click and Position actions are assigned
2. **Check Layers:** Units must be on the Selectable Layer
3. **Check Colliders:** Units need colliders for raycast detection
4. **Check Camera:** MainCamera must be assigned

### Hover Highlighting Not Working

1. **Enable Hover Highlight:** Check "Enable Hover Highlight" is true
2. **Not Dragging:** Hover only works when not dragging
3. **Material Support:** Ensure unit materials support color properties

### Groups Not Working

1. **UnitGroupManager Added:** Ensure component is in scene
2. **Keyboard Input:** Requires Unity's Input System with Keyboard
3. **Selection Manager Reference:** Assign UnitSelectionManager reference

### 3D Selection Not Working

1. **Ground Layer Set:** Must have ground layer for raycasting
2. **Detection Method:** Try different detection methods
3. **Box Height:** Ensure box height covers your units

---

## Integration with Existing Systems

The selection system integrates seamlessly with:

- **UnitMovement:** Selected units can be moved
- **UnitCombat:** Selected units can attack
- **UnitAIController:** AI states respond to selection
- **RTSCameraController:** Camera centering for groups
- **EventBus:** All events published to event bus
- **ServiceLocator:** Can be registered as a service if needed

---

## Future Enhancements

Potential additions to consider:

- [ ] Custom selection shapes (circle, polygon)
- [ ] Lasso selection tool
- [ ] Selection persistence across scenes
- [ ] Selection filters (by unit type, health, etc.)
- [ ] Formation-aware selection
- [ ] Multi-select modifiers (Ctrl=add, Alt=remove)
- [ ] Selection history (undo/redo)
- [ ] Smart selection (same type, nearby units)

---

## Credits

**Developed for:** KingdomsAtDusk RTS Game
**Engine:** Unity 6
**Architecture:** Event-driven, Service-based
**Input System:** Unity's New Input System

---

## License

Part of the KingdomsAtDusk project. See main project license for details.
