# Camera Dimming Setup Guide

This guide explains how to set up the camera-based fog of war dimming effect for Kingdoms at Dusk.

## Overview

The new `FogOfWarCameraEffect` system provides proper camera view dimming for non-visible areas. It works by:

1. Attaching to your main camera as a post-processing effect
2. Reconstructing world positions from camera depth
3. Sampling the fog of war grid to determine visibility
4. Dimming pixels based on their fog state (unexplored/explored/visible)

## Quick Setup (5 Minutes)

### 1. Add Camera Effect Component

1. Select your **Main Camera** in the hierarchy
2. Click **Add Component**
3. Search for `FogOfWarCameraEffect`
4. Add the component

### 2. Create Material

1. In the Project window, right-click and select **Create → Material**
2. Name it `FogOfWarCameraEffectMaterial`
3. In the material inspector, set the **Shader** dropdown to:
   ```
   KingdomsAtDusk → FogOfWarCameraEffect
   ```

### 3. Assign Material

1. Select your Main Camera again
2. Find the `FogOfWarCameraEffect` component
3. Drag the `FogOfWarCameraEffectMaterial` into the **Fog Effect Material** slot

### 4. Configure Settings

In the `FogOfWarCameraEffect` component:

- **Enable Effect**: ✓ (checked)
- **Dim Strength**: 0.7 (adjust to taste)
  - 0.0 = No dimming
  - 0.5 = Moderate dimming
  - 0.7 = Default dimming (recommended)
  - 1.0 = Maximum dimming

The **Fog Manager** field will auto-detect the FogOfWarManager in your scene.

### 5. Test It

1. Enter Play mode
2. You should see:
   - **Unexplored areas**: Completely dark/black
   - **Explored areas**: Partially dimmed (60% by default)
   - **Visible areas**: Fully bright (no dimming)
3. Move units around to reveal new areas
4. Areas they leave should become dimmed

## Troubleshooting

### Black Screen

**Problem**: Entire screen is black

**Solutions**:
1. Check that the shader compiled without errors (check Console)
2. Verify the material shader is set to `KingdomsAtDusk/FogOfWarCameraEffect`
3. Ensure FogOfWarManager exists in the scene and is initialized
4. Try setting Dim Strength to 0.5 temporarily

### No Dimming Effect

**Problem**: No dimming visible

**Solutions**:
1. Check that **Enable Effect** is checked
2. Verify **Fog Effect Material** is assigned
3. Check that **Dim Strength** is > 0
4. Ensure FogOfWarManager is active in the scene
5. Check Console for any initialization errors

### Flickering or Artifacts

**Problem**: Dimming flickers or shows visual artifacts

**Solutions**:
1. Check that your camera has **Depth Texture Mode** enabled (automatically set by script)
2. Ensure the camera's **Clear Flags** is set to Skybox or Solid Color
3. Try adjusting the shader **Target** pragma to 3.5 if on newer Unity versions

### Wrong Areas Dimmed

**Problem**: Dimming appears in wrong locations

**Solutions**:
1. Verify FogOfWarManager **World Bounds** match your actual play area
2. Check that the world bounds Min and Max are correct
3. Ensure units have VisionProvider components with correct Owner ID
4. Check that grid cell size is appropriate for your map size

## Advanced Configuration

### Adjusting Dim Colors

The dimming effect uses black by default. To customize:

1. Open the shader: `Assets/Shaders/FogOfWarCameraEffect.shader`
2. In the fragment shader, modify the dimming calculation:
   ```hlsl
   // Current (dims to black):
   color.rgb *= (1.0 - dimAmount);

   // Alternative (dims to custom color):
   float3 dimColor = float3(0.1, 0.1, 0.2); // Dark blue
   color.rgb = lerp(color.rgb, dimColor, dimAmount);
   ```

### Different Dim Levels for Unexplored vs Explored

Modify `FogOfWarCameraEffect.cs` in the `UpdateFogTexture()` method:

```csharp
switch (state)
{
    case VisionState.Unexplored:
        targetColor = new Color(0, 0, 0, 1f * dimStrength);      // Full dim
        break;
    case VisionState.Explored:
        targetColor = new Color(0, 0, 0, 0.4f * dimStrength);    // Less dim
        break;
    case VisionState.Visible:
        targetColor = new Color(0, 0, 0, 0f);                    // No dim
        break;
}
```

### Performance Optimization

If you experience performance issues:

1. **Reduce grid resolution**: In FogOfWarManager, increase Cell Size (e.g., from 2 to 5)
2. **Lower texture update rate**: In `FogOfWarCameraEffect.cs`, reduce update frequency
3. **Simplify shader**: Remove depth reconstruction and use simpler screen-space mapping

## Comparison: Camera Effect vs Mesh Renderer

| Feature | FogOfWarCameraEffect | FogOfWarRenderer (Legacy) |
|---------|---------------------|---------------------------|
| Camera rotation support | ✓ Excellent | ✗ Limited |
| Camera movement | ✓ Perfect | ✗ Requires height adjustment |
| Performance | ✓ Good | ✓ Good |
| Setup complexity | ✓ Simple | Medium |
| Customization | ✓ Easy | ✓ Easy |
| Works with any camera angle | ✓ Yes | ✗ Top-down only |

**Recommendation**: Use `FogOfWarCameraEffect` for all new projects.

## Integration with Existing Systems

### Works With
- RTSCameraController (rotation, movement, zoom)
- All fog of war features (vision providers, entity visibility)
- Minimap fog of war (independent system)
- Building and unit systems

### Doesn't Interfere With
- Post-processing stack
- Other camera effects
- UI rendering
- Particle systems

## Example Scene Setup

```
Hierarchy:
├── FogOfWarManager
│   └── MinimapFogRenderer (optional)
├── Main Camera
│   └── FogOfWarCameraEffect (component)
├── Units (with VisionProvider)
└── Buildings (with VisionProvider)

Materials:
└── FogOfWarCameraEffectMaterial (using FogOfWarCameraEffect shader)
```

## API Usage

```csharp
// Get reference
var cameraEffect = Camera.main.GetComponent<FogOfWarCameraEffect>();

// Enable/disable
cameraEffect.SetEnabled(true);

// Adjust dim strength at runtime
cameraEffect.SetDimStrength(0.8f);
```

## Next Steps

1. Add VisionProvider components to your units and buildings
2. Add FogOfWarEntityVisibility to enemy units
3. Configure vision radii for different unit types
4. Test with camera rotation and movement
5. Adjust dim strength to your preference

## Support

If you encounter issues:

1. Check the Unity Console for errors
2. Enable Debug Visualization in FogOfWarManager config
3. Use the FogOfWarManager context menu: "Debug: Print Fog of War Status"
4. Verify all components are properly assigned

For more details, see the main README at `Assets/Scripts/FogOfWar/README.md`
