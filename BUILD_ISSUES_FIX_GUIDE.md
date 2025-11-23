# Build Issues Fix Guide

## Overview
This guide addresses common Unity build issues that cause:
- **Low FPS in builds** (20 FPS with low CPU/GPU usage)
- **Black screens** or **missing textures**
- **Build crashes**
- **Performance degradation** from editor to build

## Problems Identified

### 1. VSync and Frame Rate Limiting
**Symptom:** Game runs at 180 FPS in editor but only 20 FPS in build with low resource usage.

**Cause:** Unity's default VSync settings in builds can lock the frame rate to unusual values depending on your monitor's refresh rate and GPU driver settings. On some systems, VSync can cause severe frame limiting.

**Solution:** The `BuildInitializer` script automatically:
- Disables VSync (`QualitySettings.vSyncCount = 0`)
- Sets target frame rate to 300 (allows hardware to determine actual FPS)
- Configures quality settings for optimal performance

### 2. Shader Compilation Issues
**Symptom:** Black screens, missing textures, or materials not rendering correctly in builds.

**Cause:** Shaders aren't pre-compiled in builds, causing:
- First-frame rendering failures (black screen)
- Texture loading delays (missing textures)
- Runtime shader compilation stutters

**Solution:** The `ShaderPreloader` script:
- Pre-warms all shaders before gameplay
- Pre-loads critical materials
- Forces shader compilation during initialization

### 3. GPU Selection on Laptops
**Symptom:** Poor performance on laptops with both integrated and discrete GPUs.

**Cause:** Windows may default to the integrated GPU (Intel HD Graphics) instead of the discrete GPU (NVIDIA/AMD) for Unity builds.

**Solution:**
- The `BuildInitializer` configures graphics settings to favor discrete GPU
- Add your game's .exe to Windows Graphics Settings (see instructions below)

### 4. Texture Streaming Issues
**Symptom:** Missing textures or low-quality textures in builds.

**Cause:** Unity's texture streaming system may not be properly configured for builds.

**Solution:** The `BuildInitializer` script:
- Enables texture streaming
- Sets appropriate memory budget (512MB)
- Prevents texture quality reduction

## Scripts Created

### 1. BuildInitializer.cs
**Location:** `Assets/Scripts/Core/BuildInitializer.cs`

**Features:**
- Automatically runs before scene load via `[RuntimeInitializeOnLoadMethod]`
- Configures VSync, frame rate, and quality settings
- Optimizes for discrete GPU usage
- Enables texture streaming
- Logs system information for debugging

**Configuration:** Select the `BuildInitializer` GameObject in your scene to adjust:
- `disableVSyncInBuild` - Disable VSync (recommended: true)
- `targetFrameRate` - Max frame rate (recommended: 300)
- `warmupAllShaders` - Warmup shaders (recommended: true)
- `forceDiscreteGPU` - Configure for discrete GPU (recommended: true)
- `enableDebugLogs` - Show initialization logs (recommended: true for testing)

### 2. ShaderPreloader.cs
**Location:** `Assets/Scripts/Core/ShaderPreloader.cs`

**Features:**
- Pre-warms all shaders in the build
- Pre-loads critical materials (e.g., fog of war material)
- Creates temporary render objects to force material initialization
- Cleans up after preloading

**Configuration:**
- `criticalMaterials` - Drag your fog of war material and other critical materials here
- `preloadDuration` - How long to wait for preloading (recommended: 2 seconds)
- `enableDebugLogs` - Show preload logs

## Setup Instructions

### Automatic Setup (Recommended)
The `BuildInitializer` script automatically runs in all builds - **no manual setup required!**

However, for the `ShaderPreloader` to work optimally:

1. **Add ShaderPreloader to your main scene:**
   - Create an empty GameObject called "ShaderPreloader"
   - Add the `ShaderPreloader` component
   - Assign critical materials (especially fog of war material) to the `criticalMaterials` array

2. **Find your Fog of War material:**
   - Navigate to your fog of war assets folder
   - Find the material used for the fog plane
   - Drag it into the `ShaderPreloader` component's `criticalMaterials` array

### Manual GPU Selection (For Laptops)
If you still experience low performance on a laptop with dual GPUs:

**Windows 10/11:**
1. Open "Settings" → "System" → "Display"
2. Scroll down and click "Graphics settings"
3. Click "Browse" and select your game's `.exe` file
4. Click "Options" → Select "High performance"
5. Click "Save"

**NVIDIA Control Panel:**
1. Right-click desktop → "NVIDIA Control Panel"
2. Navigate to "Manage 3D Settings" → "Program Settings"
3. Click "Add" → Find your game's `.exe`
4. Set "OpenGL rendering GPU" to your NVIDIA GPU
5. Click "Apply"

### Build Settings Recommendations

**Graphics API:**
- Windows: DirectX11 (default) or DirectX12
- Avoid OpenGL on Windows (worse performance)

**Quality Settings:**
Create a dedicated "Build" quality preset:
1. Edit → Project Settings → Quality
2. Add a new quality level called "Build"
3. Configure:
   - VSync Count: Don't Sync (0)
   - Shadow Quality: High
   - Shadow Resolution: High
   - Shadow Distance: 150
   - Pixel Light Count: 4
   - Texture Quality: Full Res
   - Anisotropic Textures: Per Texture

**Player Settings:**
1. Edit → Project Settings → Player
2. Under "Other Settings":
   - Color Space: Linear (for better graphics)
   - Auto Graphics API: Unchecked
   - Graphics APIs: DirectX11 first, then DirectX12
3. Under "Resolution and Presentation":
   - Fullscreen Mode: Fullscreen Window (better than Exclusive)
   - Default Screen Width/Height: Your target resolution
   - Run In Background: Checked (if needed)

## Testing Your Build

### 1. Build and Test
1. Build your game (File → Build Settings → Build)
2. Run the `.exe`
3. Press `P` to toggle the FPS counter
4. Check console logs (if you kept debug logs enabled)

### 2. What to Look For
**Console logs should show:**
```
[BuildInitializer] === Build Initializer Starting ===
[BuildInitializer] VSync disabled
[BuildInitializer] Target frame rate set to: 300
[BuildInitializer] Graphics settings configured for discrete GPU usage
[BuildInitializer] Texture streaming optimized
[BuildInitializer] Screen: 1920x1080 @ 60Hz
[BuildInitializer] Graphics Device: NVIDIA GeForce RTX 3060
[BuildInitializer] === Build Initialization Complete ===
[ShaderPreloader] === Shader Preloader Starting ===
[ShaderPreloader] Warming up all shaders...
[ShaderPreloader] === Shader Preloader Complete ===
```

**Performance indicators:**
- FPS should be much higher than 20 (aim for 60+ minimum)
- No black screens on startup
- All textures should load correctly
- Smooth gameplay without stutters

### 3. Troubleshooting

**Still getting 20-30 FPS?**
- Check Windows Graphics Settings (see "Manual GPU Selection" above)
- Verify the `BuildInitializer` logs show your discrete GPU
- Try setting `Application.targetFrameRate = -1` for unlimited FPS
- Check if any laptop power-saving modes are active

**Black screens or missing textures?**
- Ensure `ShaderPreloader` is in your scene
- Add the fog of war material to `criticalMaterials`
- Increase `preloadDuration` to 3-5 seconds
- Check if shader graphs are properly included in the build

**Build crashes?**
- Check Unity logs in `%AppData%\..\LocalLow\[YourCompany]\[YourGame]\`
- Look for shader compilation errors
- Verify all referenced assets exist in the build
- Check for null reference exceptions in initialization scripts

## Advanced Optimizations

### For Very Low FPS (Under 30)
Add this to the end of `BuildInitializer.InitializeBuildSettings()`:

```csharp
// Force maximum performance mode
QualitySettings.SetQualityLevel(QualitySettings.names.Length - 1, true);
Application.targetFrameRate = -1; // Unlimited
QualitySettings.maxQueuedFrames = 2; // Reduce input lag
```

### For Missing Fog of War in Builds
Ensure the fog of war material is using a shader that's included in the build:
1. Select the fog material in Project window
2. Check the shader name in Inspector
3. Verify this shader exists in your project
4. If using Shader Graph, ensure it's named correctly
5. Add the shader name to `ShaderPreloader.WarmupAllShaders()` if needed

### For Texture Quality Issues
```csharp
// Add to BuildInitializer.InitializeBuildSettings()
QualitySettings.globalTextureMipmapLimit = 0; // Full resolution
QualitySettings.streamingMipmapsAddAllCameras = true;
```

## Performance Monitoring

The FPS counter (press `P` key) shows real-time performance. Compare:
- **Editor:** ~180 FPS (your baseline)
- **Build (before fix):** ~20 FPS (problem state)
- **Build (after fix):** Should be 60-180+ FPS depending on hardware

## Summary of Changes

1. ✅ Created `BuildInitializer.cs` - Fixes VSync, frame rate, GPU selection, and quality settings
2. ✅ Created `ShaderPreloader.cs` - Fixes shader compilation and texture loading issues
3. ✅ Automatic initialization via `[RuntimeInitializeOnLoadMethod]` - No manual setup needed
4. ✅ Debug logging to help diagnose issues
5. ✅ Public API for runtime adjustments

## Next Steps

1. **Build your game** and test with these scripts active
2. **Monitor the console** for initialization logs
3. **Check FPS** with the built-in counter (press `P`)
4. **Adjust settings** in the inspectors if needed
5. **Configure GPU selection** on laptops if still needed
6. **Add fog of war material** to ShaderPreloader if you have texture issues

Your build should now run at much higher FPS without black screens or missing textures!
