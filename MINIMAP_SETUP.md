# Mini-Map Setup Guide

## Overview
The mini-map displays the entire playable area (2000x2000 units) and allows you to click anywhere on it to smoothly move the camera to that location.

## Features
✓ Click-to-move camera with smooth transitions
✓ Camera viewport indicator (shows current camera position)
✓ Building markers (automatically tracked via events)
✓ Unit markers (friendly and enemy, automatically tracked)
✓ Customizable colors, sizes, and movement speeds

## Unity Setup Instructions

### 1. Create the Mini-Map UI

1. **Open your main game scene** in Unity

2. **Create the UI hierarchy:**
   - Right-click in the Hierarchy → UI → Canvas (if you don't already have one)
   - Right-click on Canvas → UI → Panel → Rename to "MiniMap"
   - Configure the MiniMap panel:
     - Position it in the corner of the screen (e.g., bottom-right)
     - Set the size (recommended: 200x200 pixels)
     - Set the background color to semi-transparent black (e.g., RGBA: 0, 0, 0, 150)

3. **Create the map background:**
   - Right-click on MiniMap → UI → Raw Image → Rename to "MapBackground"
   - Set it to fill the parent panel
   - Set the color to a dark gray or terrain color

4. **Create the viewport indicator:**
   - Right-click on MiniMap → UI → Image → Rename to "ViewportIndicator"
   - Set the size to ~20x20 pixels
   - Set the color to white with transparency (RGBA: 255, 255, 255, 80)
   - This will show where your camera is currently looking

### 2. Attach the Script

1. **Add the MiniMapController script:**
   - Select the "MiniMap" panel in the Hierarchy
   - In the Inspector, click "Add Component"
   - Search for "MiniMapController" and add it

2. **Configure the script references:**

   **Mini-Map Setup:**
   - `Mini Map Rect`: Drag the MiniMap panel here
   - `Mini Map Image`: Drag the MapBackground RawImage here

   **Camera Reference:**
   - `Camera Controller`: Drag your Main Camera (with RTSCameraController) here
   - Or leave empty, it will auto-find it

   **World Bounds:**
   - `World Min`: (-1000, -1000) - Already set by default
   - `World Max`: (1000, 1000) - Already set by default

   **Camera Movement:**
   - `Camera Move Speed`: 2 (adjust to taste - higher = faster)
   - `Use Smoothing`: ✓ Checked
   - `Movement Curve`: Default EaseInOut curve (you can customize in the curve editor)

   **Camera Viewport Indicator:**
   - `Viewport Indicator`: Drag the ViewportIndicator Image here
   - `Viewport Image`: Drag the ViewportIndicator Image here
   - `Viewport Color`: White with alpha ~0.3

   **Building Markers:**
   - `Building Marker Prefab`: (Optional) Leave empty for auto-generated markers
   - `Building Markers Container`: (Optional) Leave empty for auto-creation
   - `Friendly Building Color`: Blue
   - `Building Marker Size`: 5

   **Unit Markers:**
   - `Unit Marker Prefab`: (Optional) Leave empty for auto-generated markers
   - `Unit Markers Container`: (Optional) Leave empty for auto-creation
   - `Friendly Unit Color`: Green
   - `Enemy Unit Color`: Red
   - `Unit Marker Size`: 3

### 3. Optional: Create Custom Marker Prefabs

If you want custom-styled markers instead of auto-generated ones:

**For Building Markers:**
1. Create a new GameObject in the scene
2. Add an Image component
3. Set your desired sprite/color/size
4. Make it a prefab by dragging to the Project window
5. Assign to `Building Marker Prefab` field

**For Unit Markers:**
1. Same as building markers but smaller
2. Assign to `Unit Marker Prefab` field

### 4. Test It!

1. **Enter Play Mode**
2. **You should see:**
   - A mini-map in the corner showing the entire playable area
   - A white indicator showing where your camera is
   - Building markers appearing when you place buildings
   - Unit markers appearing when units spawn

3. **Click anywhere on the mini-map** and watch the camera smoothly move to that location!

## Customization Options

### Adjust Camera Movement Speed
- Increase `Camera Move Speed` for faster transitions (try 3-5)
- Decrease for slower, more cinematic movement (try 1-1.5)

### Change Movement Curve
- Click on the `Movement Curve` to open the curve editor
- Modify for different easing effects:
  - Linear: Constant speed
  - EaseIn: Slow start, fast end
  - EaseOut: Fast start, slow end
  - EaseInOut: Smooth acceleration and deceleration (default)

### Customize Colors
- `Friendly Building Color`: Change to match your faction color
- `Friendly Unit Color`: Different color from buildings for clarity
- `Enemy Unit Color`: Red is traditional, but use whatever fits your theme

### Adjust Marker Sizes
- `Building Marker Size`: Make buildings more visible (try 7-10 for larger markers)
- `Unit Marker Size`: Keep smaller than buildings (try 2-4)

## How It Works

### Event-Driven Architecture
The mini-map automatically tracks buildings and units through the EventBus system:

- **BuildingPlacedEvent** → Creates a building marker
- **BuildingDestroyedEvent** → Removes the building marker
- **UnitSpawnedEvent** → Creates a unit marker (with color based on layer)
- **UnitDiedEvent** → Removes the unit marker

No manual tracking required! Just place buildings and spawn units normally.

### Click-to-Move
When you click on the mini-map:
1. The script converts your click position to world coordinates
2. Starts a smooth camera movement coroutine
3. Uses the animation curve for natural easing
4. Automatically stops if you click again (new target)

### Viewport Indicator
- Updates every frame in `Update()`
- Shows where the camera is currently positioned
- Useful for spatial awareness during gameplay

## API Reference

### Public Methods

```csharp
// Manually move camera to a specific world position
miniMapController.MoveCameraTo(new Vector3(500, 0, 500));

// Clear all markers (useful for level transitions)
miniMapController.ClearAllMarkers();
```

### Access the Component

```csharp
MiniMapController miniMap = FindObjectOfType<MiniMapController>();
```

## Troubleshooting

### Mini-map not showing?
- Check that the Canvas is in "Screen Space - Overlay" mode
- Ensure the MiniMap panel is a child of the Canvas
- Verify the panel's RectTransform is positioned on screen

### Click not working?
- Make sure the MiniMap panel has a Graphic component (Image or RawImage)
- Check that "Raycast Target" is enabled on the image
- Verify the EventSystem exists in the scene

### Markers not appearing?
- Check the Console for any errors
- Verify buildings are publishing BuildingPlacedEvent
- Verify units are publishing UnitSpawnedEvent
- Check that marker containers are children of the mini-map rect

### Camera not moving smoothly?
- Increase or decrease `Camera Move Speed`
- Adjust the `Movement Curve` for different easing
- Ensure `Use Smoothing` is checked

### Viewport indicator not visible?
- Increase the alpha on `Viewport Color`
- Make the viewport indicator larger
- Ensure it's on top of other UI elements (last in hierarchy)

## Performance Notes

- Markers update every frame, which is fine for small-medium unit counts
- For large numbers of units (1000+), consider updating positions less frequently
- The script uses efficient Dictionary lookups for marker management
- Destroyed units/buildings are automatically cleaned up

## Future Enhancements

Possible improvements you could add:

1. **Fog of War**: Hide unexplored areas
2. **Terrain Rendering**: Show actual terrain texture on mini-map
3. **Strategic Icons**: Different icons for different building types
4. **Ping System**: Click to ping location for multiplayer
5. **Zoom Controls**: Zoom the mini-map itself
6. **Filter Toggles**: Show/hide buildings or units
7. **Height Indication**: Color-code by terrain height

Enjoy your new mini-map! 🗺️
