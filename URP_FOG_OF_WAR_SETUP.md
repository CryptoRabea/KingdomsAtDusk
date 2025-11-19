# Fog of War Setup Guide for URP (Universal Render Pipeline)

This guide is specifically for Unity projects using the **Universal Render Pipeline (URP)**.

## Important Note

The `FogOfWarCameraEffect` component **does NOT work with URP** because it uses `OnRenderImage`, which only works in Built-in RP.

For URP, you must use the **Renderer Feature** approach instead.

## Quick Setup (URP)

### Step 1: Create Fog Material

1. Right-click in Project window → **Create → Material**
2. Name it `FogOfWarMaterial`
3. Set shader to `KingdomsAtDusk/FogOfWarURP`

### Step 2: Add Renderer Feature to URP Asset

1. Locate your **URP Renderer asset** (usually in `Assets/Settings` or similar):
   - If you don't know where it is:
     - Go to **Edit → Project Settings → Graphics**
     - Find **Scriptable Render Pipeline Settings**
     - Click on the asset to select it
     - In Inspector, find **Renderer List**
     - Click on a renderer (e.g., "ForwardRenderer")

2. With the Renderer asset selected:
   - In Inspector, scroll to bottom
   - Click **Add Renderer Feature**
   - Select **Fog Of War Renderer Feature**

3. Configure the Renderer Feature:
   - **Fog Material**: Drag your `FogOfWarMaterial` here
   - **Render Pass Event**: `Before Rendering Post Processing` (recommended)
   - **Dim Strength**: 0.7 (adjust to taste)

### Step 3: Setup Fog of War Manager

1. Create empty GameObject: **Right-click in Hierarchy → Create Empty**
2. Name it `FogOfWarManager`
3. Add component: `FogOfWarManager`
4. Configure settings:
   - **World Bounds**: Match your play area (e.g., Center: 0,0,0, Size: 2000,100,2000)
   - **Cell Size**: 2 (default is good)
   - **Update Interval**: 0.1 (default)

### Step 4: Add Vision Providers

Add `VisionProvider` component to:
- All player units
- All player buildings

Configure each VisionProvider:
- **Owner ID**: 0 (for player)
- **Vision Radius**: 15-30 (depending on unit type)
- **Is Active**: ✓ (checked)

### Step 5: Test It!

1. **Enter Play Mode**
2. The screen should start mostly dark (unexplored)
3. Areas around your units should be visible
4. Move units to reveal new areas
5. Previously seen areas should be dimmed (explored state)

## Troubleshooting

### "No Fog Effect Visible"

**Check 1: Material Assigned**
- Select your URP Renderer asset
- Find the Fog Of War Renderer Feature
- Verify **Fog Material** is assigned (not "None")

**Check 2: Shader Correct**
- Select the fog material
- Verify shader is `KingdomsAtDusk/FogOfWarURP`
- If shader shows error, check Console for compilation errors

**Check 3: Depth Texture Enabled**
- Select your URP Renderer asset
- Find **Depth Texture** setting
- Set to **On** or **Force** (if option exists)

**Check 4: FogOfWarManager Initialized**
- Check Console for initialization messages
- Look for: `[FogOfWarRenderPass] Initialized!`
- If missing, FogOfWarManager may not be set up correctly

### "Screen is Completely Black"

**Solution 1: Test Visibility**
- Select FogOfWarManager in Hierarchy
- Right-click component → **Debug: Reveal All**
- If screen becomes visible, fog is working
- Problem is: no vision providers or they're not revealing areas

**Solution 2: Check Dim Strength**
- Select your URP Renderer asset
- Find Fog Of War Renderer Feature
- Set **Dim Strength** to 0.3 temporarily
- Gradually increase to find good value

**Solution 3: Verify World Bounds**
- Select FogOfWarManager
- Check **World Bounds** covers your entire play area
- Example: If map is 1000x1000, set:
  - Center: (0, 0, 0)
  - Size: (1000, 100, 1000)

### "Shader Compilation Errors"

**Solution 1: Check URP Package**
- Go to **Window → Package Manager**
- Find **Universal RP**
- Verify it's installed and up to date
- Recommended: URP 10.0+ (Unity 2020.3+)

**Solution 2: Use Simple Shader**
- If depth reconstruction fails, use simpler approach
- We'll create a screen-space version if needed

**Solution 3: Check Unity Version**
- URP shader requires Unity 2020.3 or newer
- If older version, let me know and I'll create compatible version

### "Fog Appears in Wrong Locations"

**Check World Bounds Match**
1. Select FogOfWarManager
2. Note the **World Bounds** values
3. In Scene view, verify bounds cover your map:
   - The cyan wire cube should cover entire playable area
   - If not visible, enable Gizmos in Scene view

**Check Unit Positions**
1. Select a unit in your scene
2. Check its Transform position
3. Verify position is within World Bounds min/max

### "Performance Issues / Lag"

**Solution 1: Increase Cell Size**
- Select FogOfWarManager
- Increase **Cell Size** from 2 to 5 or 10
- Larger cells = better performance, less precision

**Solution 2: Reduce Update Frequency**
- Select FogOfWarManager
- Increase **Update Interval** from 0.1 to 0.2 or 0.3

**Solution 3: Adjust Render Pass Event**
- Select URP Renderer asset
- Find Fog Of War Renderer Feature
- Try different **Render Pass Event**:
  - `After Rendering Opaques` (earliest, best performance)
  - `Before Rendering Post Processing` (default)
  - `After Rendering Post Processing` (latest)

## Complete Setup Checklist

Use this checklist to verify everything is configured:

- [ ] URP is installed and configured in project
- [ ] Created fog material with `FogOfWarURP` shader
- [ ] Added Fog Of War Renderer Feature to URP Renderer asset
- [ ] Assigned fog material to renderer feature
- [ ] Created FogOfWarManager GameObject
- [ ] Configured World Bounds to cover play area
- [ ] Added VisionProvider to player units/buildings
- [ ] Set Owner ID to 0 on vision providers
- [ ] Set Vision Radius > 0 on vision providers
- [ ] Depth Texture enabled in URP Renderer (if needed)
- [ ] Tested in Play Mode

## Debug Commands

### FogOfWarManager Debug Menu

Right-click on FogOfWarManager component:
- **Debug: Print Fog of War Status** - Show system status
- **Debug: Reveal All** - Make entire map visible (test)
- **Debug: Hide All** - Make entire map dark (test)

### Check Initialization

Look for these Console messages when entering Play Mode:

```
[FogOfWarManager] Starting initialization...
[FogOfWarManager] Grid created: 1000x1000 cells
[FogOfWarManager] ✓ Initialization complete with X vision providers
[FogOfWarRenderPass] Initialized! Grid: 1000x1000
```

If you don't see these, something is not initialized correctly.

## Finding Your URP Renderer Asset

If you can't find your URP Renderer asset:

**Method 1: Project Settings**
1. **Edit → Project Settings → Graphics**
2. Find **Scriptable Render Pipeline Settings**
3. The asset shown here is your URP asset
4. Click it to select
5. In Inspector, find **Renderer List**
6. Click on a renderer to select it

**Method 2: Search**
1. In Project window, search: `t:UniversalRendererData`
2. Select the renderer(s) found
3. Add the feature to the active one

**Method 3: Common Locations**
- `Assets/Settings/`
- `Assets/Rendering/`
- `Assets/UniversalRenderPipelineAsset_Renderer.asset`

## Adjusting Dim Strength

The **Dim Strength** setting controls how dark fogged areas appear:

- **0.0** = No dimming (fog disabled)
- **0.3** = Light dimming (subtle effect)
- **0.5** = Moderate dimming
- **0.7** = Default dimming (recommended for most games)
- **1.0** = Maximum dimming (very dark)

Adjust in the Renderer Feature settings, not on the material.

## Advanced Configuration

### Different Dimming for Unexplored vs Explored

Edit `FogOfWarRenderPass.cs` in the `UpdateFogTexture()` method:

```csharp
switch (state)
{
    case VisionState.Unexplored:
        targetColor = new Color(0, 0, 0, 1f);    // Fully dark
        break;
    case VisionState.Explored:
        targetColor = new Color(0, 0, 0, 0.4f);  // Less dark
        break;
    case VisionState.Visible:
        targetColor = new Color(0, 0, 0, 0f);    // Fully visible
        break;
}
```

### Custom Fog Color

Edit the URP shader to dim to a color instead of black:

```hlsl
// Instead of:
color.rgb *= (1.0 - dimAmount);

// Use:
half3 fogColor = half3(0.1, 0.1, 0.15);  // Dark blue
color.rgb = lerp(color.rgb, fogColor, dimAmount);
```

## Still Not Working?

If you've followed all steps and it's still not working:

1. **Check Console** for any error messages
2. **Use Debug Menu** to test (Reveal All / Hide All)
3. **Verify URP is active**:
   - Project Settings → Graphics → Pipeline Asset should be set
   - Not "None" (that's Built-in RP)
4. **Check Unity version** - needs 2020.3+ for URP shader
5. **Provide these details for help**:
   - Unity version
   - URP version
   - Console output
   - Screenshots of renderer feature settings

## Comparison: Renderer Feature vs Camera Effect

| Feature | FogOfWarRendererFeature (URP) | FogOfWarCameraEffect (Built-in) |
|---------|-------------------------------|----------------------------------|
| Works in URP | ✓ Yes | ✗ No (OnRenderImage not called) |
| Works in Built-in RP | ✗ No | ✓ Yes |
| Setup complexity | Medium | Simple |
| Performance | ✓ Excellent | ✓ Good |
| Flexibility | ✓ High | Medium |

**For URP projects:** Always use `FogOfWarRendererFeature`
**For Built-in RP projects:** Use `FogOfWarCameraEffect`

## Next Steps

Once fog of war dimming is working:

1. Fine-tune **Dim Strength** to your preference
2. Add `FogOfWarEntityVisibility` to enemy units to hide them in fog
3. Adjust vision radii for different unit types
4. Test with camera rotation and movement
5. Configure minimap fog (optional)

## Summary

For URP, you need:
- ✓ `FogOfWarRendererFeature` (not FogOfWarCameraEffect!)
- ✓ Material with `FogOfWarURP` shader
- ✓ Renderer Feature added to URP Renderer asset
- ✓ FogOfWarManager in scene
- ✓ VisionProviders on units/buildings

That's it! The fog effect will now properly dim your camera view in URP.
