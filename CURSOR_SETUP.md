# Attack Cursor & Right-Click Attack Setup Guide

This guide explains how to set up the cursor system for RTS-style unit commands.

## Features

✅ **Cursor Changes Based on Context:**
- Normal cursor when no units selected
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

This will create 4 cursor textures in `Assets/Textures/Cursors/`:
- `CursorNormal.png` - Default cursor (white arrow)
- `CursorMove.png` - Movement cursor (green 4-way arrows)
- `CursorAttack.png` - Attack cursor (red crosshair)
- `CursorInvalid.png` - Invalid cursor (gray X)

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

### 3. Verify Layer Setup

Make sure your layers are configured correctly:
- **Enemy layer** - For enemy units
- **Player/PlayerUnit layer** - For player units
- **Ground layer** - For ground/terrain
- **Unit layer** - For all units (both player and enemy)

You can check this in: **Edit > Project Settings > Tags and Layers**

### 4. Test the System

1. **Start Play Mode**
2. **Select a unit** that can attack (has UnitCombat component)
3. **Hover over ground** - cursor should change to move cursor
4. **Hover over enemy** - cursor should change to attack cursor
5. **Right-click enemy** - unit should move to attack
6. **Right-click ground** - unit should move to location

## How It Works

### Cursor System (`CursorStateManager.cs`)

The cursor manager runs independently and checks what's under the mouse cursor every frame:

1. **No units selected** → Normal cursor
2. **Units selected + hovering ground** → Move cursor
3. **Units selected + hovering enemy + can attack** → Attack cursor
4. **Units selected + hovering enemy + cannot attack** → Invalid cursor
5. **Units selected + hovering friendly** → Normal cursor

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
        └── CursorInvalid.png
```

## Related Systems

- **UnitSelectionManager** (`Assets/Scripts/Units/Selection/UnitSelectionManager.cs:379`) - Handles unit selection
- **RTSCommandHandler** (`Assets/Scripts/Units/Selection/RTSCommandHandler.cs:192`) - Issues attack commands
- **UnitCombat** (`Assets/Scripts/Units/Components/UnitCombat.cs:37`) - Executes attacks
- **UnitAIController** - Manages AI state transitions during combat

## Notes

- The cursor system works independently of the command system
- Attack commands were already implemented in RTSCommandHandler
- The cursor provides visual feedback for what will happen on right-click
- Units with no UnitCombat component will show invalid cursor on enemies
