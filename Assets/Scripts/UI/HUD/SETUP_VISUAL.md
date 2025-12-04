# HUD Framework Visual Setup Guide

## WHERE TO CREATE THE SCRIPTABLE OBJECTS

### Step 1: Create the Folders

```
Unity Project Window:
Assets
└── Resources                    ← Create this folder (Right-click Assets > Create > Folder)
    └── HUD                      ← Create this folder
        ├── Configurations       ← Create this folder
        └── Layouts             ← Create this folder
```

### Step 2: Create Layout Preset

**WHERE**: In `Assets/Resources/HUD/Layouts/` folder

**HOW**:
1. Click on the `Layouts` folder to select it
2. Right-click in the Project window (in empty space)
3. Look for: **Create > RTS > UI > HUD Layout Preset**
4. Name it: `DefaultLayout`

**Can't find the menu?** Make sure:
- Unity has finished compiling (bottom right corner shows no progress bar)
- You're right-clicking inside the Project window
- The scripts compiled without errors

**Menu Path**:
```
Create >
  RTS >
    UI >
      HUD Configuration        ← Don't use this one yet
      HUD Layout Preset        ← USE THIS ONE!
```

### Step 3: Create HUD Configuration

**WHERE**: In `Assets/Resources/HUD/Configurations/` folder

**HOW**:
1. Click on the `Configurations` folder to select it
2. Right-click in Project window
3. **Create > RTS > UI > HUD Configuration**
4. Name it: `DefaultHUDConfig`

### Step 4: Link Layout to Configuration

**IMPORTANT**: The configuration needs to know which layout to use!

1. **Click** on `DefaultHUDConfig` (in Configurations folder)
2. **Look at Inspector** window (right side of Unity)
3. You'll see a field called "**Layout Preset**"
4. **Drag** the `DefaultLayout` asset from Layouts folder
5. **Drop** it into the "Layout Preset" field

**Visual**:
```
Inspector (DefaultHUDConfig selected):
┌─────────────────────────────────────┐
│ DefaultHUDConfig                    │
├─────────────────────────────────────┤
│ Layout Settings                     │
│   Layout Preset: [DefaultLayout] ←  │  Drag & drop here!
│                                     │
│ Core Components                     │
│   ☑ Enable Minimap                  │
│   ☑ Enable Unit Details             │
│   ☑ Enable Building Details         │
│   ☑ Enable Building HUD             │
│                                     │
│ Optional Components                 │
│   ☐ Enable Top Bar                  │
│   ☐ Enable Inventory                │
│   ☑ Show Standalone Resource Panel  │
└─────────────────────────────────────┘
```

### Step 5: Add to Scene

**WHERE**: In your game scene

**HOW**:
1. **Hierarchy window** (left side)
2. Find your **Canvas** (or create one: Right-click > UI > Canvas)
3. **Right-click** Canvas > Create Empty
4. Name it: `HUDFramework`
5. **Select** HUDFramework
6. **Inspector** > Add Component
7. Type: `MainHUDFramework`
8. Press Enter

**Visual**:
```
Hierarchy:
Canvas
├── EventSystem
└── HUDFramework  ← Your new GameObject
    (MainHUDFramework component attached)
```

### Step 6: Assign Configuration to MainHUDFramework

**CRITICAL STEP** - Don't skip this!

1. **Select** `HUDFramework` in Hierarchy
2. **Look at Inspector**
3. You'll see "Main HUD Framework (Script)"
4. At the top, there's a field: "**Configuration**"
5. **Drag** `DefaultHUDConfig` from Project window
6. **Drop** it into the "Configuration" field

**Visual**:
```
Inspector (HUDFramework selected):
┌─────────────────────────────────────┐
│ Main HUD Framework (Script)         │
├─────────────────────────────────────┤
│ Configuration                       │
│   [DefaultHUDConfig]  ←             │  Drag & drop here!
│                                     │
│ Canvas                              │
│   Main Canvas: [None (Canvas)]      │
│   Canvas Scaler: [None]             │
│                                     │
│ Core Components                     │
│   Minimap Panel: [None (Game...)]   │
│   Unit Details UI: [None (Unit...)] │
│   Building Details UI: [None]       │
│   Building HUD: [None]              │
└─────────────────────────────────────┘
```

## YOUR FINAL PROJECT STRUCTURE

```
Assets/
├── Resources/
│   └── HUD/
│       ├── Configurations/
│       │   └── DefaultHUDConfig.asset  ✅ You created this
│       └── Layouts/
│           └── DefaultLayout.asset      ✅ You created this
└── Scripts/
    └── UI/
        └── HUD/
            ├── MainHUDFramework.cs      ✅ Already exists
            ├── HUDConfiguration.cs      ✅ Already exists
            ├── HUDLayoutPreset.cs       ✅ Already exists
            └── ... (other scripts)

Scene Hierarchy:
Canvas
└── HUDFramework                         ✅ You created this
    (MainHUDFramework component)         ✅ You added this
```

## TEST IT NOW

1. **Press Play** ▶️
2. **Check Console** (bottom of Unity)
3. **You should see**:
   ```
   MainHUDFramework: Initializing HUD...
   MainHUDFramework: Minimap = Enabled
   MainHUDFramework: UnitDetails = Enabled
   MainHUDFramework: BuildingDetails = Enabled
   MainHUDFramework: BuildingHUD = Enabled
   MainHUDFramework: TopBar = Disabled
   MainHUDFramework: Inventory = Disabled
   MainHUDFramework: ResourceUI = Enabled
   MainHUDFramework: HUD initialized successfully!
   ```

## TROUBLESHOOTING

### ❌ "Can't find Create > RTS > UI menu"

**Solution**:
1. Check Console for compilation errors
2. Make sure all scripts compiled successfully
3. Wait for Unity to finish compiling (check bottom-right corner)
4. Try: Assets > Reimport All (this forces Unity to recompile)

### ❌ "No HUD configuration assigned" error

**Solution**:
1. Select HUDFramework in Hierarchy
2. Drag DefaultHUDConfig into the Configuration field in Inspector
3. The field should show "DefaultHUDConfig" not "None"

### ❌ Nothing happens when I press Play

**Solution**:
1. Check if HUDFramework GameObject is active (checkbox in Inspector)
2. Check if MainHUDFramework component is enabled
3. Check if Configuration is assigned (not "None")
4. Look for errors in Console

### ❌ "Layout Preset is null" warning

**Solution**:
1. Select DefaultHUDConfig in Project window
2. In Inspector, drag DefaultLayout into "Layout Preset" field
3. Press Ctrl+S to save

## WHAT'S NEXT?

Once you see "HUD initialized successfully!" you can:

### Add Your Existing UI Components

1. **Select** HUDFramework in Hierarchy
2. **In Inspector**, under "Core Components":
   - Drag your **Minimap** GameObject into "Minimap Panel"
   - Drag your **UnitDetailsUI** into "Unit Details UI"
   - Drag your **BuildingDetailsUI** into "Building Details UI"
   - Drag your **BuildingHUD** into "Building HUD"
3. **Press Play** - Your UI will now be positioned automatically!

### Enable Optional Features

1. **Select** DefaultHUDConfig in Project
2. **In Inspector**:
   - Check ☑ "Enable Top Bar" (for Warcraft 3 style resources)
   - Check ☑ "Enable Inventory" (for unit items)
3. **Press Play** - New UI elements will appear!

### Customize the Layout

1. **Select** DefaultLayout in Project
2. **In Inspector**, change:
   - Minimap Anchor: Bottom Left / Top Left / etc.
   - Minimap Size: (200, 200) or whatever you want
   - Same for other elements
3. **Press Play** - UI repositions automatically!

---

**That's it!** You now have a working HUD framework! 🎉
