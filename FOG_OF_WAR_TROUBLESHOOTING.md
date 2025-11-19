# Fog of War Camera Dimming - Troubleshooting Guide

This guide will help you diagnose and fix issues with the camera-based fog of war dimming effect.

## Quick Diagnostic Steps

### Step 1: Check Console Logs

1. Enter **Play Mode** in Unity
2. Check the **Console** window for messages
3. Look for messages starting with `[FogOfWarCameraEffect]`

**Expected logs when working correctly:**
```
[FogOfWarCameraEffect] Starting initialization...
[FogOfWarCameraEffect] Camera depth mode: Depth
[FogOfWarCameraEffect] Material: FogOfWarCameraEffectMaterial, Shader: KingdomsAtDusk/FogOfWarCameraEffect
[FogOfWarCameraEffect] FogOfWarManager found: FogOfWarManager
[FogOfWarCameraEffect] ✓ Initialization complete! Grid: 1000x1000, Texture: 1000x1000
[FogOfWarCameraEffect] OnRenderImage called - Effect:True, Material:True, Texture:True, Manager:True, Initialized:True
```

### Step 2: Use Debug Context Menu

1. Select your **Main Camera** in the hierarchy (in Play mode)
2. In the Inspector, find the `FogOfWarCameraEffect` component
3. Right-click on the component header
4. Select **"Debug: Print Status"**
5. Check the Console for detailed diagnostic information

### Step 3: Check Material Assignment

1. Select your Main Camera
2. Find the `FogOfWarCameraEffect` component
3. Check that **Fog Effect Material** field is assigned (not "None")
4. Click on the material to select it in Project
5. In material Inspector, verify:
   - **Shader** is set to `KingdomsAtDusk/FogOfWarCameraEffect` or `KingdomsAtDusk/FogOfWarCameraEffectSimple`

## Common Issues and Solutions

### Issue 1: "No material assigned!" Error

**Symptoms:**
- Console shows: `[FogOfWarCameraEffect] No material assigned!`
- No dimming effect visible

**Solution:**
1. Create a new Material:
   - Right-click in Project → Create → Material
   - Name it `FogOfWarCameraEffectMaterial`
2. Set the shader:
   - Select the material
   - In Inspector, click **Shader** dropdown
   - Choose `KingdomsAtDusk → FogOfWarCameraEffect` (or `FogOfWarCameraEffectSimple`)
3. Assign to component:
   - Select Main Camera
   - Find `FogOfWarCameraEffect` component
   - Drag material into **Fog Effect Material** slot

### Issue 2: "No FogOfWarManager found!" Error

**Symptoms:**
- Console shows: `[FogOfWarCameraEffect] No FogOfWarManager found in scene!`
- Camera effect can't initialize

**Solution:**
1. Create FogOfWarManager:
   - Create empty GameObject: Right-click in Hierarchy → Create Empty
   - Name it "FogOfWarManager"
   - Add component: `FogOfWarManager`
2. Configure it:
   - Set **World Bounds** to match your play area
   - Set **Cell Size** (default: 2 is good)
3. Add vision providers to units/buildings
4. Restart play mode

### Issue 3: Shader Compilation Errors

**Symptoms:**
- Console shows shader errors
- Material shows pink/magenta color
- Shader dropdown shows "Error" or "Hidden/InternalErrorShader"

**Solution Option A - Use Simple Shader:**
1. Select your material
2. Change shader to `KingdomsAtDusk/FogOfWarCameraEffectSimple`
3. This shader is simpler and more compatible

**Solution Option B - Fix Shader:**
1. Open `Assets/Shaders/FogOfWarCameraEffect.shader`
2. Check Console for specific errors
3. Common issues:
   - Unity version too old (needs 2020.3+)
   - Built-in RP not enabled
   - Missing depth texture support

### Issue 4: Black Screen

**Symptoms:**
- Entire screen is black when effect is enabled
- Disabling effect shows normal view

**Possible Causes:**

**A) Fog texture is all black:**
1. Use context menu: **Debug: Set Full Visibility**
2. If screen becomes visible, it means fog is working but all areas are unexplored
3. Add VisionProvider components to your units

**B) Shader dim strength too high:**
1. Select Main Camera
2. Find `FogOfWarCameraEffect` component
3. Set **Dim Strength** to 0.3 temporarily
4. Gradually increase until you find good value

**C) World bounds mismatch:**
1. Select FogOfWarManager
2. Check **World Bounds** in config
3. Make sure it covers your actual play area
4. Example: If your map is 500x500, use:
   - Center: (0, 0, 0)
   - Size: (500, 100, 500)

### Issue 5: No Effect Visible

**Symptoms:**
- Camera view looks normal, no dimming
- No errors in console

**Solutions:**

**Check 1: Effect Enabled**
1. Select Main Camera
2. Find `FogOfWarCameraEffect`
3. Ensure **Enable Effect** is ✓ checked

**Check 2: Dim Strength**
1. Check **Dim Strength** slider
2. Set it to 1.0 temporarily to see maximum effect
3. If you see dimming, adjust to 0.7

**Check 3: Test with Hidden Map**
1. Use context menu: **Debug: Set No Visibility**
2. Screen should go dark
3. If it does, fog is working
4. Problem is vision providers not revealing areas

**Check 4: Vision Providers**
1. Select a unit in your scene
2. Check if it has `VisionProvider` component
3. Check **Owner Id** is 0 (for player)
4. Check **Vision Radius** is > 0
5. Check **Is Active** is ✓ checked

### Issue 6: Flickering or Artifacts

**Symptoms:**
- Fog effect flickers
- Visual artifacts or glitches
- Depth-related issues

**Solutions:**

**Solution 1: Use Simple Shader**
- Change material shader to `FogOfWarCameraEffectSimple`
- This uses a more robust depth reconstruction method

**Solution 2: Check Camera Settings**
1. Select Main Camera
2. In Inspector:
   - **Clear Flags**: Set to "Skybox" or "Solid Color"
   - **Depth Texture Mode**: Should be "Depth" (set automatically)
   - **Allow MSAA**: Try disabling if flickering

**Solution 3: Adjust Near/Far Planes**
1. Select Main Camera
2. Try adjusting:
   - **Near Clipping Plane**: 0.3 minimum
   - **Far Clipping Plane**: Match your world size

### Issue 7: Wrong Areas Dimmed

**Symptoms:**
- Dimming appears in incorrect locations
- Fog doesn't match unit positions

**Solutions:**

**Check 1: World Bounds**
1. Select FogOfWarManager
2. Verify **World Bounds** matches your actual play area:
   ```
   Center: (0, 0, 0)  // Center of your map
   Size: (2000, 100, 2000)  // Width, Height, Depth
   ```
3. The bounds should cover ALL units and buildings

**Check 2: Grid Visualization**
1. Select FogOfWarManager
2. Enable **Enable Debug Visualization**
3. In Scene view, you should see grid lines
4. Verify grid covers your play area

**Check 3: Unit Positions**
1. Select a unit
2. Check Transform Position in Inspector
3. Verify it's within World Bounds min/max

## Debug Tools

### Context Menu Commands

Right-click on `FogOfWarCameraEffect` component header:

- **Debug: Print Status** - Print diagnostic info
- **Debug: Force Reinitialize** - Restart the system
- **Debug: Set Full Visibility** - Reveal entire map (test)
- **Debug: Set No Visibility** - Hide entire map (test)

### Debug Flags

In `FogOfWarCameraEffect` component:

- **Enable Debug Logging** - Detailed console logs
- **Visualize Depth** - Show depth buffer (debug)
- **Visualize Fog Texture** - Show fog texture directly

### Testing Steps

1. **Test Full Visibility:**
   - Use "Debug: Set Full Visibility"
   - Entire map should be bright
   - This tests if dimming system works

2. **Test No Visibility:**
   - Use "Debug: Set No Visibility"
   - Entire map should be dark
   - This tests if dimming applies correctly

3. **Test Vision Providers:**
   - Add units with VisionProvider
   - Move units around
   - Areas around units should brighten
   - This tests vision integration

## Performance Issues

If the effect is causing performance problems:

### Solution 1: Reduce Grid Resolution
1. Select FogOfWarManager
2. Increase **Cell Size** from 2 to 5 or 10
3. Larger cells = better performance, less precision

### Solution 2: Reduce Update Frequency
1. Select FogOfWarManager
2. Increase **Update Interval** from 0.1 to 0.2 or 0.3
3. Updates less often = better performance

### Solution 3: Simplify Shader
- Use `FogOfWarCameraEffectSimple` shader
- It has fewer calculations

## Still Not Working?

### Collect Debug Information

1. Enter Play mode
2. Select Main Camera
3. Use "Debug: Print Status"
4. Copy all console output
5. Select FogOfWarManager
6. Use its "Debug: Print Fog of War Status"
7. Copy console output

### Check These Files Exist

- `Assets/Scripts/FogOfWar/FogOfWarCameraEffect.cs`
- `Assets/Scripts/FogOfWar/FogOfWarManager.cs`
- `Assets/Shaders/FogOfWarCameraEffect.shader`
- `Assets/Shaders/FogOfWarCameraEffectSimple.shader` (new)

### Minimal Working Setup

Try this minimal setup to isolate the issue:

1. **New scene**
2. **Add FogOfWarManager:**
   - Empty GameObject named "FogOfWarManager"
   - Component: `FogOfWarManager`
   - World Bounds: Center (0,0,0), Size (100, 10, 100)
3. **Setup Main Camera:**
   - Component: `FogOfWarCameraEffect`
   - Material with `FogOfWarCameraEffectSimple` shader
   - Enable Effect: ✓
   - Dim Strength: 0.7
4. **Add test object:**
   - Cube at (0, 0, 0)
   - Component: `VisionProvider`
   - Owner ID: 0
   - Vision Radius: 20
5. **Enter Play Mode**
6. Use "Debug: Set No Visibility" - should go dark
7. Use "Debug: Set Full Visibility" - should be bright

If this works, your setup is correct. Issue is with scene configuration.

## Getting Help

When asking for help, provide:
1. Unity version
2. Render pipeline (Built-in, URP, HDRP)
3. Console output from "Debug: Print Status"
4. Any error messages
5. Screenshots of:
   - FogOfWarCameraEffect inspector
   - FogOfWarManager inspector
   - Material inspector
6. Description of what you see vs. what you expect
