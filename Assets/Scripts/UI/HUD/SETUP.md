# HUD Framework - STEP-BY-STEP SETUP GUIDE

Follow these exact steps to get the HUD working.

## STEP 1: Create Folders (30 seconds)

1. In Unity Project window, create these folders:
   ```
   Assets/Resources/HUD/Configurations/
   Assets/Resources/HUD/Layouts/
   ```

2. Create the folders manually:
   - Right-click in Assets
   - Create > Folder > "Resources"
   - Inside Resources, Create > Folder > "HUD"
   - Inside HUD, Create > Folder > "Configurations"
   - Inside HUD, Create > Folder > "Layouts"

## STEP 2: Create Layout Preset (1 minute)

1. **Right-click** in `Assets/Resources/HUD/Layouts/` folder
2. **Create > RTS > UI > HUD Layout Preset**
3. **Name it**: `DefaultLayout`
4. **Click on it** to see Inspector
5. **Leave default values** (or customize later)

**That's it for the layout!** You just created your first layout preset.

## STEP 3: Create HUD Configuration (1 minute)

1. **Right-click** in `Assets/Resources/HUD/Configurations/` folder
2. **Create > RTS > UI > HUD Configuration**
3. **Name it**: `DefaultHUDConfig`
4. **Click on it** to see Inspector
5. **Drag and drop** the `DefaultLayout` asset into the "Layout Preset" field
6. **Configure what you want enabled:**
   - ✅ Enable Minimap
   - ✅ Enable Unit Details
   - ✅ Enable Building Details
   - ✅ Enable Building HUD
   - ⬜ Enable Top Bar (turn OFF for now - optional)
   - ⬜ Enable Inventory (turn OFF for now - optional)
   - ✅ Show Standalone Resource Panel
   - ✅ Show Happiness

## STEP 4: Add MainHUDFramework to Scene (2 minutes)

### Option A: If you already have a Canvas with UI:

1. **Select your Canvas** in Hierarchy
2. **Create empty child GameObject** (Right-click Canvas > Create Empty)
3. **Name it**: `HUDFramework`
4. **Add Component**: `Main HUD Framework`
5. **In Inspector**, assign the `DefaultHUDConfig` to the "Configuration" field

### Option B: If you DON'T have UI yet:

1. **In Hierarchy**: Right-click > UI > Canvas
2. **Name it**: `MainCanvas`
3. **Create empty child** under Canvas (Right-click Canvas > Create Empty)
4. **Name it**: `HUDFramework`
5. **Add Component**: `Main HUD Framework`
6. **In Inspector**, assign `DefaultHUDConfig` to "Configuration" field

## STEP 5: Link Your Existing UI Components (3 minutes)

Now you need to tell MainHUDFramework where your existing UI components are.

**In the MainHUDFramework Inspector**, you'll see:

### Canvas Section:
- **Main Canvas**: Drag your Canvas here
- **Canvas Scaler**: Drag the CanvasScaler component from your Canvas

### Core Components Section:
- **Minimap Panel**: Drag your minimap GameObject here (if you have one)
- **Unit Details UI**: Drag your UnitDetailsUI component here (if you have one)
- **Building Details UI**: Drag your BuildingDetailsUI component here (if you have one)
- **Building HUD**: Drag your BuildingHUD component here (if you have one)

### Optional Components Section:
- Leave these empty for now (or drag if you have them)

### Don't have these components yet?
**That's OK!** Leave them empty. The framework will just skip those components.

## STEP 6: Test It (30 seconds)

1. **Press Play**
2. **Check Console** - You should see:
   ```
   MainHUDFramework: Initializing HUD...
   MainHUDFramework: Minimap = Enabled
   MainHUDFramework: UnitDetails = Enabled
   etc.
   ```

## What if I get errors?

### Error: "No HUD configuration assigned"
- **Fix**: Select the HUDFramework GameObject
- In Inspector, drag `DefaultHUDConfig` into the Configuration field

### Error: Components not showing
- **Fix**: Make sure you assigned the component references in Step 5
- Or set those features to disabled in the HUDConfiguration

### Error: Can't find "Create > RTS > UI > HUD Layout Preset"
- **Fix**: The menu items only appear AFTER Unity compiles the scripts
- Wait a few seconds for Unity to finish compiling
- If still not there, right-click > Refresh

## QUICK TEST - Minimal Setup

If you just want to see it work RIGHT NOW:

1. Create `Assets/Resources/HUD/Layouts/` folder
2. Create `Assets/Resources/HUD/Configurations/` folder
3. Right-click Layouts > Create > RTS > UI > HUD Layout Preset > Name: `TestLayout`
4. Right-click Configurations > Create > RTS > UI > HUD Configuration > Name: `TestConfig`
5. Click TestConfig, drag TestLayout into "Layout Preset" field
6. **Turn OFF everything** in TestConfig except:
   - Enable Notifications = ✅ (if you have NotificationUI)
7. Create empty GameObject in scene > Add MainHUDFramework component
8. Drag TestConfig into Configuration field
9. Press Play

You should see in console: "MainHUDFramework: Initializing HUD..."

## Visual Guide - What Goes Where

```
Scene Hierarchy:
├── Canvas
│   └── HUDFramework (MainHUDFramework component)
│       ├── Minimap (your existing minimap)
│       ├── UnitDetails (your existing UnitDetailsUI)
│       ├── BuildingDetails (your existing BuildingDetailsUI)
│       ├── BuildingHUD (your existing BuildingHUD)
│       └── ResourcePanel (your existing ResourceUI)

Project Folders:
Assets/
├── Resources/
│   └── HUD/
│       ├── Configurations/
│       │   └── DefaultHUDConfig.asset  ← YOU CREATE THIS
│       └── Layouts/
│           └── DefaultLayout.asset     ← YOU CREATE THIS
└── Scripts/
    └── UI/
        └── HUD/
            ├── MainHUDFramework.cs     ← Already exists
            ├── HUDConfiguration.cs     ← Already exists
            └── HUDLayoutPreset.cs      ← Already exists
```

## Example: Creating Warcraft 3 Style

1. Create layout: `Warcraft3Layout`
2. Set these values in Inspector:
   - Minimap Anchor: **Bottom Left**
   - Minimap Size: **(200, 200)**
   - Minimap Offset: **(10, 10)**
   - Unit Details Anchor: **Bottom Center**
   - Unit Details Size: **(400, 150)**
   - Building HUD Anchor: **Bottom Right**
   - Building HUD Size: **(200, 200)**
3. Create config: `Warcraft3Config`
4. Enable Top Bar and Inventory in config
5. Drag `Warcraft3Layout` into config's Layout Preset field
6. Done!

## Common Mistakes

❌ **Forgetting to create Resources/HUD folders** - ScriptableObjects must be in Resources
❌ **Not assigning Layout Preset to Configuration** - Configuration needs a layout!
❌ **Not assigning Configuration to MainHUDFramework** - Framework needs a config!
❌ **Enabling components you don't have** - Turn off features you haven't built yet

## What You Should See

When it's working:
1. ✅ No errors in Console
2. ✅ Console shows: "MainHUDFramework: Initializing HUD..."
3. ✅ Console shows: "MainHUDFramework: HUD initialized successfully!"
4. ✅ Your existing UI components are positioned (if you assigned them)

---

**Next**: Once basic setup works, you can:
- Create more layouts (Modern RTS, Compact, etc.)
- Enable TopBar and Inventory
- Customize positions in layout presets
- Add hotkeys with HUDController
