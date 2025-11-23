# Menu, Loading & Package Export Tools - Complete Guide

## Overview

This guide covers three powerful Unity editor tools that were created for your project:

1. **Menu & Loading Screen Setup Tool** - Auto-creates main menu and loading screens
2. **Package Exporter Tool** - Export systems as standalone .unitypackage files
3. **System Extractor Tool** - Auto-detect and extract ALL game systems

## Table of Contents

- [Part 1: Menu & Loading Screen Setup](#part-1-menu--loading-screen-setup)
- [Part 2: Package Exporter](#part-2-package-exporter)
- [Part 3: System Extractor](#part-3-system-extractor)
- [Quick Start Guides](#quick-start-guides)

---

## Part 1: Menu & Loading Screen Setup

### What It Creates

✅ **Main Menu Scene** with:
- Professional UI layout
- New Game, Continue, Settings, Credits, Quit buttons
- Settings panel (ready for customization)
- Credits panel
- Version display
- Fully functional menu system

✅ **Loading Screen** with:
- Smooth progress bar
- Loading tips (customizable)
- Percentage display
- Beautiful background
- DontDestroyOnLoad (persists across scenes)

✅ **Scene Transition Manager**:
- Handles scene loading with progress
- Automatic loading screen display
- Smooth transitions between scenes

### How To Use

#### Option 1: Setup Everything (Recommended)

1. In Unity Editor: **Tools → RTS → Setup → Main Menu & Loading Screen Setup**
2. The tool window opens
3. Configure settings:
   - Main Menu Scene Name (default: "MainMenu")
   - Game Scene Name (default: "GameScene")
   - Customize colors if desired
4. Click **"Setup Everything"**
5. Done! ✅

#### Option 2: Individual Setup

Use these buttons for granular control:
- **Setup Main Menu Only** - Creates just the menu scene
- **Setup Loading Screen Only** - Creates just the loading screen prefab
- **Setup Scene Transition Manager** - Adds transition manager to scene

### What Gets Created

**Files Created:**
```
Assets/Scenes/MainMenu.unity                     - Main menu scene
Assets/Prefabs/UI/LoadingScreen.prefab          - Loading screen prefab
Assets/Scripts/UI/LoadingScreen/                - Loading system scripts
Assets/Scripts/UI/MainMenu/                     - Menu system scripts
```

**Scripts Created:**
- `LoadingScreenManager.cs` - Manages loading screen display
- `SceneTransitionManager.cs` - Handles scene transitions
- `MainMenuManager.cs` - Main menu logic and button handlers

### Customization

#### Customize Main Menu

1. Open `Assets/Scenes/MainMenu.unity`
2. Select `MainMenuCanvas` in Hierarchy
3. Modify:
   - Colors (background, buttons)
   - Text (title, buttons)
   - Layout (positions, sizes)
   - Add logo/images
4. Save scene

#### Customize Loading Screen

1. Open `Assets/Prefabs/UI/LoadingScreen.prefab`
2. Modify:
   - Background image
   - Progress bar colors
   - Loading tips (in LoadingScreenManager component)
   - Text styles
3. Apply changes to prefab

#### Add Loading Tips

1. Find LoadingScreen in scene/prefab
2. Select LoadingScreenManager component
3. Expand "Loading Tips" array
4. Add your tips:
   ```
   "Tip: Build defenses to protect your kingdom"
   "Tip: Gather resources before winter"
   "Tip: Train different unit types for balanced armies"
   ```

### Using In Code

#### Load Game Scene (from Main Menu)

```csharp
// Button onClick handler
SceneTransitionManager.Instance.LoadGameScene();
```

#### Load Main Menu (from Game)

```csharp
// Return to menu
SceneTransitionManager.Instance.LoadMainMenu();
```

#### Custom Scene Loading

```csharp
// Load any scene with loading screen
SceneTransitionManager.Instance.LoadScene("MyCustomScene");
```

#### Manual Loading Screen Control

```csharp
// Show loading screen
LoadingScreenManager.Instance.Show(showTip: true);

// Update progress (0 to 1)
LoadingScreenManager.Instance.SetProgress(0.5f);

// Set custom message
LoadingScreenManager.Instance.SetMessage("Loading assets...");

// Hide when done
LoadingScreenManager.Instance.Hide();
```

### Build Settings

**IMPORTANT:** Add scenes to Build Settings:

1. File → Build Settings
2. Click "Add Open Scenes" or drag scenes:
   - MainMenu.unity (Index 0 - will be startup scene)
   - GameScene.unity (Index 1 - your game)
3. Click "Build" or "Build and Run"

---

## Part 2: Package Exporter

### What It Does

Export any game system or feature as a standalone .unitypackage file that can be imported into other Unity projects.

### How To Use

1. **Tools → RTS → Export → Package Exporter**
2. The tool window opens

### Quick Export (Recommended)

**Export Menu & Loading System:**
- Click **"Export Menu & Loading System"** button
- Choose save location
- Done! Package created with README

**Export All Systems:**
- Click **"Export All Game Systems"** button
- Creates one package with everything
- Includes all dependencies

### Custom Export

1. Select "Predefined System" dropdown:
   - Menu & Loading System
   - Fog of War System
   - Building System
   - Unit System
   - Selection System
   - Resource System
   - Camera System
   - UI System (Complete)
   - Core Services
   - All Systems

2. Or choose "Custom Selection..." and add your own paths

3. Configure package info:
   - Package Name
   - Version
   - Description

4. Click **"Export Package"**

### Predefined Systems Available

| System | What It Includes |
|--------|------------------|
| **Menu & Loading System** | Complete menu, loading screens, scene transitions |
| **Fog of War System** | Fog of war with revealers and visibility |
| **Building System** | Building placement, selection, management |
| **Unit System** | Units, movement, commands |
| **Selection System** | Selection system for units and buildings |
| **Resource System** | Resource management and display |
| **Camera System** | RTS camera with pan/zoom/rotate |
| **UI System** | All UI components and systems |
| **Core Services** | Service locator, event bus, core architecture |
| **All Systems** | Complete game export |

### What Gets Exported

For each package, you get:
- ✅ `.unitypackage` file (importable in any Unity project)
- ✅ README text file with:
  - Installation instructions
  - Included files list
  - Setup guide
  - Dependencies
  - Version info

### Importing Packages

1. Open target Unity project
2. Assets → Import Package → Custom Package
3. Select the .unitypackage file
4. Check all files (or select specific ones)
5. Click Import
6. Follow README instructions

---

## Part 3: System Extractor (Auto-Detect)

### What It Does

**Automatically scans your entire project** and detects all game systems, then exports each one as a separate standalone package.

This is the most powerful tool - it analyzes your codebase and creates modular, reusable packages automatically!

### How To Use

1. **Tools → RTS → Export → System Extractor (Auto-Detect)**
2. The tool window opens
3. It automatically scans and detects all systems
4. Review the detected systems

### Auto-Detected Systems

The tool automatically finds and categorizes:

- ✅ Core Services (Service Locator, Event Bus)
- ✅ Build Initialization (VSync fix, shader preloader)
- ✅ Menu & Loading System
- ✅ Fog of War System
- ✅ Building System
- ✅ Unit System
- ✅ Selection System
- ✅ Resource Management
- ✅ Happiness System
- ✅ Population System
- ✅ Reputation System
- ✅ Wave System
- ✅ RTS Camera System
- ✅ UI System
- ✅ Minimap System
- ✅ Cursor System
- ✅ Input System
- ✅ Editor Tools

### Features

**System Information:**
- System name and description
- All included paths
- File count estimate
- Dependencies on other systems

**Dependency Tracking:**
- Shows which systems depend on others
- Enable "Show Dependencies" checkbox
- Helps determine import order

**Batch Export:**
- Select multiple systems
- Export all at once
- Each gets its own package + README

### Workflow

1. **Scan Project:**
   - Click "Re-Scan Project for Systems" to refresh
   - Tool detects all systems automatically

2. **Select Systems:**
   - Check boxes next to systems you want
   - Or use "Select All" / "Deselect All"

3. **Choose Export Folder:**
   - Click "Browse" to select location
   - Or use default: `[ProjectFolder]/ExportedPackages`

4. **Export:**
   - Click **"Export Selected Systems (N)"**
   - Or click **"Export All Systems"** to export everything

5. **Done!:**
   - Opens export folder automatically
   - Each system has its own .unitypackage + README
   - Master README.txt with import instructions

### What Gets Created

```
ExportedPackages/
├── README.txt                          (Master README)
├── CoreServices_v1.0.0.unitypackage
├── CoreServices_v1.0.0.txt
├── BuildInitialization_v1.0.0.unitypackage
├── BuildInitialization_v1.0.0.txt
├── MenuLoadingSystem_v1.0.0.unitypackage
├── MenuLoadingSystem_v1.0.0.txt
├── FogOfWarSystem_v1.0.0.unitypackage
├── FogOfWarSystem_v1.0.0.txt
├── BuildingSystem_v1.0.0.unitypackage
├── BuildingSystem_v1.0.0.txt
├── ... (and so on for each system)
```

### Recommended Import Order

When importing into a new project, follow this order:

1. ✅ Core Services (required by most systems)
2. ✅ Build Initialization
3. ✅ Input System
4. ✅ Camera System
5. ✅ Selection System
6. ✅ Resource Management
7. ✅ All other systems (any order)

---

## Quick Start Guides

### Quick Start: Setup Main Menu (2 Minutes)

1. Tools → RTS → Setup → Main Menu & Loading Screen Setup
2. Click "Setup Everything"
3. Add MainMenu.unity and GameScene.unity to Build Settings
4. Test in Play Mode
5. Done! ✅

### Quick Start: Export Menu System (1 Minute)

1. Tools → RTS → Export → Package Exporter
2. Click "Export Menu & Loading System"
3. Choose save location
4. Done! ✅

### Quick Start: Extract All Systems (2 Minutes)

1. Tools → RTS → Export → System Extractor
2. Click "Export All Systems"
3. Choose export folder
4. Wait for completion
5. Done! All systems exported as individual packages ✅

### Quick Start: Use In Another Project

1. Create new Unity project
2. Import packages in this order:
   - CoreServices_v1.0.0.unitypackage
   - MenuLoadingSystem_v1.0.0.unitypackage
   - [Other systems as needed]
3. Follow individual README files
4. Configure Build Settings
5. Done! ✅

---

## Advanced Usage

### Custom Loading Screen Backgrounds

```csharp
// Set custom background for specific scenes
LoadingScreenManager manager = LoadingScreenManager.Instance;
manager.SetBackgroundImage(mySprite);
manager.Show();
```

### Scene Loading With Custom Progress

```csharp
IEnumerator LoadSceneManually()
{
    LoadingScreenManager.Instance.Show();

    // Your custom loading logic
    for (int i = 0; i <= 100; i++)
    {
        // Do some loading work...
        LoadingScreenManager.Instance.SetProgress(i / 100f);
        yield return null;
    }

    LoadingScreenManager.Instance.Hide();
}
```

### Export System With Custom Paths

1. Open Package Exporter
2. Select "Custom Selection..." from dropdown
3. Click "Add File or Folder"
4. Browse to your system folder
5. Add all relevant paths
6. Configure package info
7. Export

### Add Custom System To Extractor

Edit `SystemExtractorTool.cs`:

```csharp
// In DetectAllSystems() method, add:
AddSystemIfExists("My Custom System",
    "Description of my system",
    new[] { "Assets/Scripts/MySystem", "Assets/Prefabs/MySystem" },
    new[] { "Core Services" }); // Dependencies
```

---

## Troubleshooting

### Menu Scene Not Loading

- Check Build Settings - MainMenu.unity must be added
- Verify scene name in SceneTransitionManager matches actual scene name

### Loading Screen Not Showing

- Ensure LoadingScreen prefab is instantiated in scene
- Check that LoadingScreenManager component is present
- Verify Canvas sorting order is high (999+)

### Package Export Fails

- Check that all paths exist
- Ensure no compile errors in project
- Try exporting smaller systems first

### Import Errors In New Project

- Import Core Services first (most systems depend on it)
- Check Unity version compatibility
- Follow dependency order in README

### System Not Detected By Extractor

- Click "Re-Scan Project for Systems"
- Check that files exist in expected locations
- Manually add via Package Exporter if needed

---

## File Locations

### Scripts
```
Assets/Scripts/UI/LoadingScreen/
├── LoadingScreenManager.cs
└── SceneTransitionManager.cs

Assets/Scripts/UI/MainMenu/
└── MainMenuManager.cs

Assets/Scripts/Editor/
├── MenuSetupTool.cs
├── PackageExporterTool.cs
└── SystemExtractorTool.cs
```

### Prefabs
```
Assets/Prefabs/UI/
└── LoadingScreen.prefab
```

### Scenes
```
Assets/Scenes/
├── MainMenu.unity
└── GameScene.unity
```

---

## Tips & Best Practices

### Menu System
- ✅ Always add scenes to Build Settings
- ✅ Test transitions in Play Mode before building
- ✅ Customize loading tips for your game
- ✅ Use version text for player feedback

### Package Export
- ✅ Export systems that work well independently
- ✅ Include documentation in README
- ✅ Test imports in clean projects
- ✅ Version your packages (use semantic versioning)

### System Extraction
- ✅ Export all systems early in development
- ✅ Keep packages updated as you develop
- ✅ Document dependencies clearly
- ✅ Test in isolation to ensure modularity

---

## Summary

You now have three powerful tools:

1. **Menu Setup Tool** → Create professional menus in 2 minutes
2. **Package Exporter** → Export any system as reusable package
3. **System Extractor** → Auto-detect and export ALL systems

All tools are accessible via **Tools → RTS** menu in Unity Editor.

Your entire game is now modular and reusable! 🚀

---

## Support

For issues or questions:
1. Check this guide's troubleshooting section
2. Review individual system README files
3. Check Unity console for error messages
4. Verify all dependencies are imported

Happy developing! 🎮
