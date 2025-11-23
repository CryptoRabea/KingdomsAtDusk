# Build Issues Fix - Summary

## Problem
Game runs at 180 FPS in Unity Editor but experiences multiple issues in builds:
- Only 20 FPS with low CPU/GPU usage (<20%)
- Black screens or missing textures
- Occasional crashes
- Severe performance degradation

## Root Causes Identified

### 1. VSync and Frame Rate Limiting
Unity's default VSync settings in builds can lock frame rates to unusual values (20 FPS, 30 FPS) depending on monitor refresh rate and GPU drivers.

### 2. Shader Compilation Issues
Shaders not pre-compiled in builds cause:
- Black screens on first render
- Missing textures
- Runtime compilation stutters

### 3. GPU Selection (Laptops)
Windows may default to integrated GPU (Intel) instead of discrete GPU (NVIDIA/AMD).

### 4. Texture Streaming Not Configured
Missing or incorrect texture streaming settings cause texture loading failures.

## Solution: Scripts Created

### 1. `BuildInitializer.cs` ✓
**Location:** `Assets/Scripts/Core/BuildInitializer.cs`

**Fixes:**
- ✓ Disables VSync (QualitySettings.vSyncCount = 0)
- ✓ Sets target frame rate to 300 (unlimited performance)
- ✓ Configures quality settings for discrete GPU
- ✓ Enables texture streaming with 512MB budget
- ✓ Sets shadow quality and other graphics settings
- ✓ Logs system info for debugging

**Key Feature:** Runs automatically via `[RuntimeInitializeOnLoadMethod]` - no manual setup required!

### 2. `ShaderPreloader.cs` ✓
**Location:** `Assets/Scripts/Core/ShaderPreloader.cs`

**Fixes:**
- ✓ Pre-warms all shaders before gameplay
- ✓ Pre-loads critical materials (fog of war, etc.)
- ✓ Forces shader compilation during startup
- ✓ Creates temporary render objects to initialize materials
- ✓ Prevents black screens and missing textures

**Setup Required:** Add to scene and assign critical materials in Inspector.

### 3. `BuildDiagnostics.cs` ✓
**Location:** `Assets/Scripts/Core/BuildDiagnostics.cs`

**Features:**
- ✓ Press 'D' in build to show detailed diagnostics
- ✓ Shows FPS, frame time, GPU info, quality settings
- ✓ Detects common issues (VSync on, low FPS, wrong GPU, etc.)
- ✓ Provides real-time performance monitoring
- ✓ Helps identify problems in builds

**Setup Required:** Add to scene (optional, for debugging).

### 4. `BuildSetupMenu.cs` ✓
**Location:** `Assets/Scripts/Editor/BuildSetupMenu.cs`

**Features:**
- ✓ Menu: Tools → RTS → Build Setup
- ✓ "Add ShaderPreloader to Scene" - Automatically adds and configures
- ✓ "Add Build Diagnostics to Scene" - Adds diagnostics tool
- ✓ "Setup All Build Optimizations" - One-click setup
- ✓ "Configure Build Settings" - Sets optimal Unity player settings

## Quick Setup (2 Minutes)

### Option A: Automatic (Recommended)
1. In Unity Editor: `Tools → RTS → Build Setup → Setup All Build Optimizations`
2. Click "Yes" to confirm
3. Done! Build your game.

### Option B: Manual
1. Add `ShaderPreloader` to your scene (Tools → RTS → Build Setup → Add ShaderPreloader)
2. Assign fog of war material in Inspector
3. (Optional) Add `BuildDiagnostics` for debugging
4. Build your game

**Note:** `BuildInitializer` runs automatically - no setup needed!

## Testing Your Fix

1. **Build your game:**
   - File → Build Settings → Build
   - Choose a folder and build

2. **Run the .exe and test:**
   - Press `P` to show FPS counter
   - Press `D` to show diagnostics (if BuildDiagnostics is in scene)
   - Check that FPS is now 60+ instead of 20

3. **Check console logs:**
   ```
   [BuildInitializer] === Build Initializer Starting ===
   [BuildInitializer] VSync disabled
   [BuildInitializer] Target frame rate set to: 300
   [BuildInitializer] Graphics Device: [Your GPU]
   [ShaderPreloader] === Shader Preloader Starting ===
   ```

## Expected Results

### Before Fix:
- ❌ 20 FPS in build
- ❌ Black screens / missing textures
- ❌ Low CPU/GPU usage
- ❌ Possible crashes

### After Fix:
- ✅ 60-180+ FPS (depending on hardware)
- ✅ All textures load correctly
- ✅ No black screens
- ✅ Smooth performance
- ✅ Proper GPU utilization

## Still Having Issues?

### If FPS is still low (30-40):
1. Press `D` in build to show diagnostics
2. Check "Graphics Device" - should show your discrete GPU (NVIDIA/AMD)
3. If showing Intel GPU: Configure Windows Graphics Settings (see BUILD_ISSUES_FIX_GUIDE.md)

### If textures are missing:
1. Add fog of war material to ShaderPreloader's `criticalMaterials`
2. Increase `preloadDuration` to 3-5 seconds
3. Check that all materials use shaders included in build

### If build crashes:
1. Check Unity logs in `%AppData%\..\LocalLow\[YourCompany]\[YourGame]\`
2. Look for shader compilation errors
3. Verify all assets exist in build

## Additional Tools

### Editor Menu (Tools → RTS → Build Setup):
- **Add ShaderPreloader to Scene** - Quick setup
- **Add Build Diagnostics to Scene** - Add debugging tool
- **Setup All Build Optimizations** - One-click setup
- **Configure Build Settings** - Set optimal Unity settings
- **Show Build Guide** - Open detailed guide

### In-Build Controls:
- **P key** - Toggle FPS counter
- **D key** - Toggle diagnostics (if BuildDiagnostics in scene)

## Documentation

See `BUILD_ISSUES_FIX_GUIDE.md` for:
- Detailed explanation of each issue
- Step-by-step troubleshooting
- Advanced optimizations
- Manual GPU selection instructions
- Quality settings recommendations

## Files Created

1. `Assets/Scripts/Core/BuildInitializer.cs` - Main fix script (auto-runs)
2. `Assets/Scripts/Core/ShaderPreloader.cs` - Shader/texture fix
3. `Assets/Scripts/Core/BuildDiagnostics.cs` - Diagnostics tool
4. `Assets/Scripts/Editor/BuildSetupMenu.cs` - Editor menu items
5. `BUILD_ISSUES_FIX_GUIDE.md` - Detailed guide
6. `BUILD_FIX_SUMMARY.md` - This file

## Key Points

✅ **BuildInitializer runs automatically** - no manual setup needed!
✅ **ShaderPreloader** needs to be added to scene and configured
✅ **One-click setup** via Tools → RTS → Build Setup menu
✅ **Press 'D' in build** to diagnose any remaining issues
✅ **Comprehensive solution** for all common build issues

Your build should now run smoothly at high FPS! 🚀
