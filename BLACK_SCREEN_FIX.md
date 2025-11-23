# Black Screen / Crash Fix

## What Changed

I've updated the build scripts to prevent the black screen and crash issues:

### 1. **BuildInitializer.cs** - Updated
- ✅ **Shader warmup DISABLED by default** (was causing freezing)
- ✅ Added error handling with try-catch blocks
- ✅ More defensive initialization
- ✅ Better logging

### 2. **ShaderPreloader.cs** - Updated
- ✅ **Shader warmup DISABLED by default** (major cause of freezing)
- ✅ **Dummy object creation DISABLED** (can cause issues without proper camera)
- ✅ Added error handling
- ✅ Only preloads critical materials (safe operation)

### 3. **StartupDiagnostics.cs** - NEW
- ✅ Emergency diagnostic logger
- ✅ Writes detailed logs to help identify issues
- ✅ Tracks every stage of startup
- ✅ Logs saved to disk for analysis

## The Problem

The black screen was caused by **`Shader.WarmupAllShaders()`** blocking the main thread:
- This function can take several seconds on some systems
- During this time, the screen stays black and Unity appears frozen
- It can cause "Not Responding" errors in Windows

## The Solution

**Shader warmup is now DISABLED by default.** The game will now:
- Start immediately without freezing
- Compile shaders on-demand (may have small stutters first time)
- Run at full FPS without the 20 FPS issue (VSync still disabled)

## Testing the Fix

### Step 1: Build Your Game
1. File → Build Settings → Build
2. Choose output folder
3. Build the game

### Step 2: Run and Check Logs
1. Run your game's `.exe`
2. If you still see a black screen, **wait 10 seconds** - the diagnostic logger is working
3. Close the game
4. Find the startup log at: `C:\Users\[YourName]\AppData\LocalLow\[YourCompany]\[YourGame]\Logs\startup_diagnostic_[timestamp].txt`

### Step 3: Check the Log File
The log will show exactly where initialization failed:

**Good Startup (Everything Working):**
```
=== STARTUP DIAGNOSTICS ===
=== ASSEMBLIES LOADED ===
=== BEFORE SPLASH SCREEN ===
=== BEFORE SCENE LOAD ===
Graphics Device: NVIDIA GeForce RTX 3060
=== AFTER SCENE LOAD ===
Main Camera found: Main Camera
[0.52s] StartupDiagnostics.Awake()
[0.53s] StartupDiagnostics.Start()
[1.53s] Still running... FPS: 60.2
[2.53s] Still running... FPS: 60.5
[3.53s] 3 seconds elapsed - checking system state...
=== IF YOU SEE THIS, THE GAME IS RUNNING ===
```

**Bad Startup (Frozen/Crashed):**
```
=== STARTUP DIAGNOSTICS ===
=== ASSEMBLIES LOADED ===
=== BEFORE SCENE LOAD ===
Graphics Device: Intel(R) UHD Graphics 620   <-- Using integrated GPU!
[stops here - game froze]
```

## If Still Black Screen

### Option 1: Check the Log First
1. Run game, wait 10 seconds, close it
2. Check the log file (path above)
3. Look for where it stops - that's where the problem is
4. Share the log with me for analysis

### Option 2: Verify Camera Setup
The log will tell you if Camera.main is found. If not:
1. Open your main scene in Unity
2. Make sure you have a Camera with tag "MainCamera"
3. Camera component should be enabled
4. Camera's culling mask should include Default layer

### Option 3: Check for Missing References
If the log shows errors about missing components:
1. Check GameManager in your scene
2. Verify all serialized fields are assigned
3. Look for "Missing Reference" warnings in Unity Editor

### Option 4: Disable All Preloaders
If you added ShaderPreloader to your scene:
1. Select the ShaderPreloader GameObject
2. In Inspector, **disable the component** (uncheck the checkbox)
3. Rebuild and test

### Option 5: Check Graphics Settings
From the log, verify:
- You're using your discrete GPU (NVIDIA/AMD), not Intel
- VSync Count is 0
- Screen resolution is correct (not 0x0)

## Windows Graphics Settings (For Laptops)

If the log shows "Intel" GPU instead of NVIDIA/AMD:

1. **Windows Settings:**
   - Settings → Display → Graphics Settings
   - Click "Browse"
   - Select your game's .exe
   - Click "Options" → Select "High performance"
   - Click "Save"

2. **NVIDIA Control Panel:**
   - Right-click desktop → "NVIDIA Control Panel"
   - Manage 3D Settings → Program Settings
   - Add your game's .exe
   - Select "High-performance NVIDIA processor"
   - Click "Apply"

## Quick Fixes Summary

1. ✅ **Shader warmup disabled** - No more freezing
2. ✅ **Error handling added** - Won't crash on errors
3. ✅ **Diagnostic logging** - Can identify exact problem
4. ✅ **VSync still disabled** - Still fixes 20 FPS issue
5. ✅ **Safe initialization** - Minimal chance of issues

## Re-enabling Shader Warmup (Advanced)

**Only do this if the game works but you have texture/shader issues:**

1. Open Unity Editor
2. Find "BuildInitializer" in your scene (it's created automatically at runtime)
3. Or create a prefab with BuildInitializer component
4. In Inspector: Check "Warmup All Shaders"
5. Rebuild and test

**Warning:** This may bring back the freezing issue on some systems.

## Debug Controls (In Build)

- **Press 'D'** - Show diagnostics overlay (if BuildDiagnostics in scene)
- **Press 'P'** - Show FPS counter
- **Check log file** - Always available at the path mentioned above

## Expected Behavior After Fix

### On Startup:
1. Game launches quickly (1-2 seconds)
2. Scene loads and camera renders immediately
3. No black screen or freezing
4. Smooth 60+ FPS

### If You See:
- ✅ **Game window with scene visible** - SUCCESS!
- ✅ **Cursor and FPS counter visible** - SUCCESS!
- ❌ **Black screen for more than 3 seconds** - Check the log file
- ❌ **"Not Responding" dialog** - Shader warmup issue (should be fixed now)

## What To Share If Still Broken

1. The startup diagnostic log file
2. Player.log file from: `C:\Users\[YourName]\AppData\LocalLow\[YourCompany]\[YourGame]\Player.log`
3. Description of what you see (black screen, crash, freeze, etc.)
4. How long you waited before closing

The diagnostic log will tell us exactly what's happening!
