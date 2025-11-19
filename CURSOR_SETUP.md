# Attack Cursor & Right-Click Attack Setup Guide

This guide explains how to set up the cursor system for RTS-style unit commands.

## Features

✅ **Cursor Changes Based on Context:**
- Normal cursor when idle
- **Select Unit cursor** when hovering over selectable units (UnitSelectable component)
- **Select Building cursor** when hovering over selectable buildings (BuildingSelectable component)
- Move cursor when hovering over ground with units selected
- Attack cursor when hovering over enemies with attack-capable units selected
- Invalid cursor when hovering over enemies with no attack-capable units

✅ **Right-Click Attack Already Implemented:**
- Right-click on enemy = Attack command
- Right-click on ground = Move command
- Double right-click = Forced move (ignores enemies)

## Setup Instructions

### 1. Generate Cursor Textures

Open Unity and go to: **Tools > Generate Cursor Textures**

This will create 6 cursor textures in `Assets/Textures/Cursors/`:
- `CursorNormal.png` - Default cursor (white arrow)
- `CursorMove.png` - Movement cursor (green 4-way arrows)
- `CursorAttack.png` - Attack cursor (red crosshair)
- `CursorInvalid.png` - Invalid cursor (gray X)
- `CursorSelectUnit.png` - Unit selection cursor (cyan hand pointer)
- `CursorSelectBuilding.png` - Building selection cursor (orange house icon)

### 2. Add CursorStateManager to Scene

1. Find your **SelectionManager** GameObject (the one with UnitSelectionManager and RTSCommandHandler)
2. Add the **CursorStateManager** component to it
3. Configure the component:

**Cursor Textures:**
- Drag the generated cursor textures to their respective slots

**References:**
- Selection Manager: Auto-found (or drag your UnitSelectionManager)
- Main Camera: Auto-found (or drag your main camera)
- Ground Layer: Set to your ground layer (e.g., "Ground" or "Terrain")
- Unit Layer: Set to your unit layer (e.g., "Unit")

**Cursor Hotspots:**
- Normal Hotspot: (0, 0) - top-left corner
- Move Hotspot: (16, 16) - center for 32x32 cursor
- Attack Hotspot: (16, 16) - center for 32x32 cursor
- Invalid Hotspot: (16, 16) - center for 32x32 cursor
- Select Unit Hotspot: (16, 16) - center for 32x32 cursor
- Select Building Hotspot: (16, 16) - center for 32x32 cursor

### 3. Verify Layer Setup

Make sure your layers are configured correctly:
- **Enemy layer** - For enemy units
- **Player/PlayerUnit layer** - For player units
- **Ground layer** - For ground/terrain
- **Unit layer** - For all units (both player and enemy)

You can check this in: **Edit > Project Settings > Tags and Layers**

### 4. Test the System

1. **Start Play Mode**
2. **Hover over a selectable unit** (has UnitSelectable component) - cursor should change to cyan hand pointer
3. **Hover over a selectable building** (has BuildingSelectable component) - cursor should change to orange house icon
4. **Select a unit** that can attack (has UnitCombat component)
5. **Hover over ground** - cursor should change to move cursor (green arrows)
6. **Hover over enemy** - cursor should change to attack cursor (red crosshair)
7. **Right-click enemy** - unit should move to attack
8. **Right-click ground** - unit should move to location

## How It Works

### Cursor System (`CursorStateManager.cs`)

The cursor manager runs independently and checks what's under the mouse cursor every frame with the following priority:

1. **Hovering BuildingSelectable component** → Select Building cursor (highest priority)
2. **Hovering UnitSelectable component** → Select Unit cursor
3. **Units selected + hovering enemy + can attack** → Attack cursor
4. **Units selected + hovering enemy + cannot attack** → Invalid cursor
5. **Units selected + hovering ground** → Move cursor
6. **Default/no units selected** → Normal cursor

This priority system ensures that selection cursors always show when hovering selectable objects, regardless of whether you have units selected or not.

### Attack System (`RTSCommandHandler.cs`)

Right-click handling:
- **Single right-click on enemy** → `IssueAttackCommand()` - units attack the target
- **Single right-click on ground** → `IssueMoveCommand()` - units move to position
- **Double right-click** → `IssueForcedMoveCommand()` - units move ignoring enemies

### Combat System (`UnitCombat.cs`)

When attack command is issued:
1. `SetTarget(enemy)` - sets the enemy as target
2. `FollowTarget(enemy)` - unit movement follows the enemy
3. `TryAttack()` - called by AI when in range to perform attack

## Customization

### Custom Cursor Textures

You can replace the generated cursors with your own:
1. Create/import cursor images (recommended: 32x32 PNG with transparency)
2. In the Inspector for the image:
   - Set **Texture Type** to **Cursor**
   - Enable **Alpha Is Transparency**
   - Set **Filter Mode** to **Point** for pixel art or **Bilinear** for smooth
3. Drag to CursorStateManager slots

### Cursor Hotspot

The hotspot determines which pixel of the cursor image is the "point" position:
- `(0, 0)` = Top-left corner (good for arrow cursors)
- `(16, 16)` = Center of a 32x32 cursor (good for crosshairs)
- Adjust based on your cursor design

### Attack Range Display

To show attack range preview when hovering enemies:
- Modify `CursorStateManager.UpdateCursorState()` at line ~97
- Add visual feedback (circle, range indicator, etc.)

## Troubleshooting

### Cursor doesn't change
- Check that CursorStateManager is enabled
- Verify cursor textures are assigned
- Check that layers are set correctly
- Ensure units have UnitCombat component for attack cursor

### Right-click attack doesn't work
- Check RTSCommandHandler is enabled
- Verify enemy is on "Enemy" layer
- Ensure units have UnitCombat component
- Check that UnitMovement component exists

### Units don't attack
- Verify UnitCombat component is enabled
- Check attack range settings
- Ensure target has UnitHealth component
- Check AI state machine is running

## File Locations

```
Assets/
├── Scripts/
│   ├── UI/
│   │   ├── CursorStateManager.cs          # Cursor management
│   │   └── Editor/
│   │       └── CursorTextureGenerator.cs  # Cursor texture generator tool
│   └── Units/
│       ├── Selection/
│       │   ├── UnitSelectionManager.cs    # Unit selection
│       │   └── RTSCommandHandler.cs       # Right-click commands
│       └── Components/
│           └── UnitCombat.cs              # Combat system
└── Textures/
    └── Cursors/                           # Generated cursor textures
        ├── CursorNormal.png
        ├── CursorMove.png
        ├── CursorAttack.png
        ├── CursorInvalid.png
        ├── CursorSelectUnit.png
        └── CursorSelectBuilding.png
```

## Related Systems

- **UnitSelectionManager** (`Assets/Scripts/Units/Selection/UnitSelectionManager.cs:379`) - Handles unit selection
- **RTSCommandHandler** (`Assets/Scripts/Units/Selection/RTSCommandHandler.cs:192`) - Issues attack commands
- **UnitCombat** (`Assets/Scripts/Units/Components/UnitCombat.cs:37`) - Executes attacks
- **UnitSelectable** (`Assets/Scripts/Units/Components/UnitSelectable.cs:12`) - Component for selectable units
- **BuildingSelectable** (`Assets/Scripts/RTSBuildingsSystems/BuildingSelectable.cs:12`) - Component for selectable buildings
- **UnitAIController** - Manages AI state transitions during combat

## Notes

- The cursor system works independently of the command system
- Attack commands were already implemented in RTSCommandHandler
- The cursor provides visual feedback for what will happen on right-click or selection
- Selection cursors (unit/building) show when hovering objects with UnitSelectable or BuildingSelectable components
- Selection cursors have highest priority and show even when units are selected
- Units with no UnitCombat component will show invalid cursor on enemies
- Any GameObject with UnitSelectable or BuildingSelectable component will trigger the appropriate selection cursor
