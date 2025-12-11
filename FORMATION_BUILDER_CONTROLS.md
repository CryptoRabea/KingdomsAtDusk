# Formation Builder Controls

## Creating the Builder

Run: **Tools > RTS > Create Formation Builder UI**

This automatically creates a Formation Builder with all features:
- Draggable title bar
- Resizable edges/corners
- Zoomable grid
- Custom cursor support
- Zoom controls

## Player Controls

### Moving the Panel (Drag)
- **Click and drag the title bar** to move the panel anywhere on screen
- Works exactly like Windows dialog boxes

### Resizing the Panel
- **Drag corners** to resize diagonally
- **Drag edges** to resize horizontally or vertically
- Min size: 400x400
- Max size: 1200x900
- Works like Windows window resizing

### Zooming the Grid
Three ways to zoom:
1. **Mouse Wheel**: Scroll up = zoom in, scroll down = zoom out
2. **Keyboard**: Ctrl + Plus = zoom in, Ctrl + Minus = zoom out
3. **Buttons**: Click + or - buttons on right side of panel

Zoom range: 50% to 300%
Current zoom level shown in bottom left

### Grid Interaction
- **Hover over cell**: Cursor changes to hover cursor, cell highlights
- **Click empty cell**: Places unit (shows select cursor)
- **Click filled cell**: Removes unit (shows deselect cursor)
- **Mouse leaves cell**: Cursor resets to default

### Custom Cursors (Optional)
To add custom cursor textures:
1. Select the Canvas object in hierarchy
2. Find CustomCursorController component
3. Assign your cursor textures:
   - **Default Cursor**: Normal pointer
   - **Hover Cursor**: When hovering over cells
   - **Select Cursor**: When placing a unit
   - **Deselect Cursor**: When removing a unit
4. Set Cursor Hotspot (usually 0,0 for top-left)

If no textures assigned, uses default system cursor.

## Technical Details

### Components Added Automatically
- **DraggablePanel**: Makes panel draggable by title bar
- **ResizablePanel**: Adds resize handles at edges/corners
- **ZoomableScrollRect**: Mouse wheel zoom on grid
- **CustomCursorController**: Manages cursor changes

### Scripts Location
- `Assets/Scripts/UI/DraggablePanel.cs`
- `Assets/Scripts/UI/ResizablePanel.cs`
- `Assets/Scripts/UI/ZoomableScrollRect.cs`
- `Assets/Scripts/UI/CustomCursorController.cs`
- `Assets/Scripts/UI/FormationGridCell.cs` (updated)

### Cursor Texture Requirements
- Format: PNG or TGA
- Recommended size: 32x32 or 64x64
- Must enable "Read/Write" in import settings
- Set texture type to "Cursor" in import settings

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| Mouse Wheel | Zoom in/out |
| Ctrl + Plus/Equals | Zoom in |
| Ctrl + Minus | Zoom out |

## Tips

1. **Smooth Dragging**: Click and hold title bar, then drag
2. **Corner Resize**: Best for maintaining aspect ratio
3. **Edge Resize**: Best for one-directional resize
4. **Zoom Before Placing**: Zoom in for precise unit placement
5. **Zoom Out**: See the whole formation at once
6. **Custom Cursors**: Use 32x32 textures for crisp cursors at all resolutions

## Creating Custom Cursor Textures

Example cursor designs:
- **Hover**: Highlighted outline or glow
- **Select**: Plus sign or add icon
- **Deselect**: X or trash icon

Tools: Photoshop, GIMP, Paint.NET, or any image editor

Export as PNG with transparency for best results.
