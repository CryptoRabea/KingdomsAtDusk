# Mini-Map Setup Guide

## Overview
The mini-map displays the entire playable area (2000x2000 units) and allows you to click anywhere on it to smoothly move the camera to that location.

## Features
✓ **Real-time world map rendering** - See the actual terrain and world
✓ Click-to-move camera with smooth transitions
✓ Camera viewport indicator (shows current camera position)
✓ Building markers (automatically tracked via events)
✓ Unit markers (friendly and enemy, automatically tracked)
✓ Customizable colors, sizes, and movement speeds
✓ Auto-generated mini-map camera or use your own

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

   **Mini-Map Camera (World Rendering):**
   - `Render World Map`: ✓ Checked (to show actual terrain)
   - `Mini Map Camera`: (Optional) Leave empty for auto-creation
   - `Render Texture Size`: 512 (higher = better quality, more performance cost)
   - `Mini Map Camera Height`: 500 (how high above the world the camera sits)
   - `Mini Map Layers`: Everything (or customize to show only specific layers)

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
   - `Viewport Indicator`: Drag the ViewportIndicator Image object here
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
   - A mini-map in the corner showing the **actual world terrain** from above
   - A white indicator showing where your camera is
   - Building markers appearing when you place buildings
   - Unit markers appearing when units spawn
   - Real-time rendering of the game world

3. **Click anywhere on the mini-map** and watch the camera smoothly move to that location!

4. **Check the Console** for a confirmation message: "Mini-map camera setup complete. Rendering 2000x2000 world area."

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

### Customize World Map Rendering
- **Render Texture Size**:
  - 256: Low quality, fast performance
  - 512: Good balance (default)
  - 1024: High quality, moderate performance
  - 2048+: Very high quality, performance intensive
- **Mini Map Camera Height**: Adjust how high the camera sits (500 is default, increase if objects are cut off)
- **Mini Map Layers**: Use LayerMask to show/hide specific objects on the mini-map:
  - Show only Ground layer for terrain-only view
  - Hide UI layers to prevent clutter
  - Show Enemy layer to see enemy positions

### Disable World Rendering (Markers Only)
If you want a simple mini-map with just markers and no terrain:
- Uncheck `Render World Map`
- Set the MapBackground color to your desired background
- This improves performance significantly

## How It Works

### Real-Time Rendering
The mini-map uses a second camera positioned above the world center:
- Renders from orthographic view (no perspective distortion)
- Positioned at `miniMapCameraHeight` above the world
- Renders to a RenderTexture which is displayed on the RawImage
- Updates every frame automatically

### Render Layers
You can control what appears on the mini-map using Unity's Layer system:
- Create a "MiniMapIgnore" layer for objects you don't want shown
- Assign the `Mini Map Layers` to exclude that layer
- Useful for hiding UI, effects, or decorative objects

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

### World not rendering on mini-map?
- Verify `Render World Map` is checked
- Check the Console for the setup confirmation message
- Ensure `Mini Map Image` (RawImage) is assigned
- Verify your terrain/world objects are on layers included in `Mini Map Layers`
- Check that objects are within the world bounds (-1000 to 1000)

### Mini-map shows black screen?
- Increase `Mini Map Camera Height` (try 1000 if 500 doesn't work)
- Check `Mini Map Layers` - make sure ground/terrain layers are included
- Verify the background color isn't too dark
- Check that your world has visible objects to render

### Mini-map looks distorted or cut off?
- Adjust `World Bounds` to match your actual playable area
- Increase `Mini Map Camera Height` to see taller objects
- Check the orthographic size calculation (should be worldSize.y / 2)

### Performance issues with mini-map?
- Lower `Render Texture Size` (try 256 instead of 512)
- Reduce `Mini Map Layers` to only essential layers
- Consider disabling world rendering (uncheck `Render World Map`) and use markers only
- Reduce marker update frequency if you have many units

## Performance Notes

### Mini-Map Camera Rendering
- The mini-map camera renders every frame by default
- RenderTexture size directly impacts performance (512x512 is a good balance)
- Consider these optimizations:
  - Lower render texture resolution on mobile devices
  - Use fewer layers in the mini-map camera's culling mask
  - Position the mini-map camera to avoid rendering unnecessary objects
  - Use simpler LODs or shaders for objects visible on the mini-map

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
