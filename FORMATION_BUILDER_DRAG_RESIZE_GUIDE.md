# Formation Builder - Drag & Resize Guide

## Overview

The Formation Builder panel is **automatically created** with drag and resize functionality, allowing it to behave like a Windows window. You can move it anywhere on screen and resize it to your preference while designing formations.

## Automatic Setup

### Creating the Formation Builder

Drag and resize functionality is **built-in** when you use the automatic setup:

1. **Open Unity Editor**
2. **Go to:** `Tools > RTS > Create Formation Builder UI`
3. **Click:** "Create Formation Builder UI"
4. **Done!** Drag and resize are automatically enabled

### What's Included

The builder is created with these features pre-configured:
- ✅ Draggable title bar at the top
- ✅ Resizable edges and corners
- ✅ Size limits (600x600 min, 1400x1000 max)
- ✅ Zoom with mouse wheel
- ✅ Custom cursor support

## Usage (In Play Mode)

### Dragging the Panel

**To move the panel:**
1. Click and hold the **dark gray title bar** at the top
2. Drag to move the panel
3. Release to drop

### Resizing the Panel

**Resize from edges:**
- Hover near any edge (left, right, top, bottom)
- Click and drag to resize that dimension

**Resize from corners:**
- Hover near any corner
- Click and drag to resize both width and height

**Size limits:**
- Minimum: 600x600 pixels
- Maximum: 1400x1000 pixels

## If Drag/Resize Doesn't Work

If the panel was created before this fix, you may need to recreate it or manually fix the references:

### Option 1: Recreate (Easiest)

1. Delete the old FormationBuilderPanel
2. Run `Tools > RTS > Create Formation Builder UI` again
3. The new panel will have drag/resize working correctly

### Option 2: Manual Fix

If drag doesn't work:
1. Find the **TitleBar** GameObject under FormationBuilderPanel
2. Select the **DraggablePanel** component
3. Set these references:
   - Panel Rect Transform: **ContentPanel** (parent)
   - Drag Handle Rect: **TitleBar** (itself)
   - Canvas: Your main Canvas

If resize doesn't work:
1. Find the **ContentPanel** GameObject
2. Select the **ResizablePanel** component
3. Set these values:
   - Panel Rect Transform: **ContentPanel** (itself)
   - Min Size: X=600, Y=600
   - Max Size: X=1400, Y=1000
   - Resize Handle Size: 20

## Troubleshooting

### Panel Won't Drag
**Check:**
- Title bar has a DraggablePanel component
- DraggablePanel.panelRectTransform is set to ContentPanel
- DraggablePanel.dragHandleRect is set to TitleBar
- Title bar has an Image component (required for mouse events)

### Panel Won't Resize
**Check:**
- ContentPanel has a ResizablePanel component
- ResizablePanel.panelRectTransform is set to ContentPanel
- Resize Handle Size is at least 15-20
- You're clicking very close to the edge (within 20 pixels)

### Panel Jumps When Resizing
**Check:**
- ContentPanel anchor is center (0.5, 0.5)
- ContentPanel pivot is center (0.5, 0.5)

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| **Mouse Wheel** | Zoom grid in/out |
| **Ctrl + Plus** | Zoom in |
| **Ctrl + Minus** | Zoom out |

## Components Reference

### DraggablePanel
Location: On **TitleBar** GameObject

Makes the panel draggable by clicking the title bar.

**Required Settings:**
- Panel Rect Transform: The panel to move (ContentPanel)
- Drag Handle Rect: Area to click (TitleBar)
- Canvas: Parent canvas (auto-finds)

### ResizablePanel
Location: On **ContentPanel** GameObject

Makes the panel resizable from edges and corners.

**Required Settings:**
- Panel Rect Transform: Panel to resize (ContentPanel)
- Min Size: Minimum dimensions (600x600)
- Max Size: Maximum dimensions (1400x1000)
- Resize Handle Size: Edge grab distance (20px)

## Related Files

- `Assets/Scripts/UI/DraggablePanel.cs` - Drag functionality
- `Assets/Scripts/UI/ResizablePanel.cs` - Resize functionality
- `Assets/Scripts/Editor/FormationBuilderUISetup.cs` - Auto-setup tool (FIXED)
- `Assets/Scripts/UI/FormationBuilderUI.cs` - Main UI controller

## Recent Fixes

**✅ Fixed in this update:**
- FormationBuilderUISetup now properly wires DraggablePanel references
- FormationBuilderUISetup now properly wires ResizablePanel references
- Drag and resize now work immediately after creating the builder

Before this fix, the components were added but their references weren't set, causing drag/resize to not work.
