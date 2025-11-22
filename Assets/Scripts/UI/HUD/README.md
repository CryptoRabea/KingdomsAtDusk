# Main HUD Framework

A flexible, modular HUD framework for RTS games that supports multiple layout configurations, including Warcraft 3-style interfaces.

## Overview

The Main HUD Framework provides a centralized system for managing all UI components in your RTS game. It supports:

- **Configurable layouts** - Multiple preset layouts (Warcraft 3, StarCraft, Age of Empires styles)
- **Optional components** - Enable/disable features like inventory, top bar, minimap
- **Developer-friendly** - Easy configuration via ScriptableObjects
- **Runtime flexibility** - Change layouts and toggle components at runtime
- **Performance optimized** - Configurable update rates and efficient rendering

## Architecture

### Core Components

1. **MainHUDFramework** - Central controller managing all HUD components
2. **HUDConfiguration** - ScriptableObject for developer settings
3. **HUDLayoutPreset** - Defines positioning and sizing of UI elements
4. **TopBarUI** - Optional top resource/menu bar (Warcraft 3 style)
5. **InventoryUI** - Optional inventory system for units

### Component Hierarchy

```
MainHUDFramework (Controller)
├── Core Components (Always available)
│   ├── MiniMap
│   ├── UnitDetailsUI
│   ├── BuildingDetailsUI
│   └── BuildingHUD
├── Optional Components (Configurable)
│   ├── TopBarUI (Resources + Menu)
│   ├── InventoryUI (Unit items)
│   ├── ResourceUI (Standalone)
│   ├── HappinessUI
│   ├── NotificationUI
│   └── WallResourcePreviewUI
└── Systems
    └── CursorStateManager
```

## Setup

### 1. Create HUD Configuration

Right-click in Project window:
- `Create > RTS > UI > HUD Configuration`
- Name it (e.g., "DefaultHUDConfig")

Configure settings:
- **Core Components**: Enable minimap, unit details, building details, etc.
- **Optional Components**: Toggle top bar, inventory, notifications
- **Resource Display**: Choose between top bar or standalone panel
- **Performance**: Set update rate (30-60 Hz recommended)

### 2. Create Layout Preset

Right-click in Project window:
- `Create > RTS > UI > HUD Layout Preset`
- Name it (e.g., "Warcraft3Layout")

Configure positions:
- **Anchor positions**: Where each element attaches (TopLeft, BottomRight, etc.)
- **Sizes**: Width and height of each panel
- **Offsets**: Fine-tune positioning

### 3. Add MainHUDFramework to Scene

1. Create empty GameObject named "MainHUD"
2. Add `MainHUDFramework` component
3. Assign your HUDConfiguration
4. Link all UI component references:
   - Minimap panel
   - UnitDetailsUI
   - BuildingDetailsUI
   - etc.

## Configuration Options

### HUDConfiguration Settings

#### Core Components
```csharp
enableMinimap = true           // Show/hide minimap
enableUnitDetails = true        // Show/hide unit info panel
enableBuildingDetails = true    // Show/hide building info panel
enableBuildingHUD = true        // Show/hide building placement UI
```

#### Optional Components
```csharp
enableTopBar = false           // Warcraft 3 style top bar
includeResourcesInTopBar = true // Resources in top bar vs standalone
enableInventory = false         // Unit inventory system
inventoryGridSize = (3, 2)     // 3x2 = 6 inventory slots
```

#### Resource Display
```csharp
showStandaloneResourcePanel = true  // Separate resource panel
showHappiness = true                // Happiness indicator
```

## Layout Presets

### Creating Custom Layouts

The framework includes preset examples:

#### 1. Warcraft 3 Style Layout
```
┌─────────────────────────────────┐
│  Top Bar (Resources + Menu)     │
├────────┬──────────────┬─────────┤
│        │              │         │
│        │              │ Inven-  │
│ Mini   │    Game      │ tory    │
│ map    │    View      │         │
│        │              ├─────────┤
├────────┼──────────────┤ Build   │
│  Unit  │  Building    │ HUD     │
│ Details│  Details     │         │
└────────┴──────────────┴─────────┘
```

Configuration:
- `enableTopBar = true`
- `includeResourcesInTopBar = true`
- `enableInventory = true`
- `minimapAnchor = BottomLeft`
- `inventoryAnchor = BottomRight`

#### 2. Modern RTS Layout
```
┌─────────────────────────────────┐
│  Resources (Center)   Happiness │
├──────────────────────┬──────────┤
│                      │          │
│                      │ Mini     │
│     Game View        │ map      │
│                      │          │
│                      ├──────────┤
├──────────────────────┤ Build    │
│  Unit/Building Info  │ HUD      │
└──────────────────────┴──────────┘
```

Configuration:
- `enableTopBar = false`
- `showStandaloneResourcePanel = true`
- `enableInventory = false`
- `resourcePanelAnchor = TopCenter`
- `minimapAnchor = MiddleRight`

#### 3. Age of Empires Style
```
┌─────────────────────────────────┐
│  Resources    │    Menu          │
├───────────────┴──────────────────┤
│                                  │
│  Minimap │   Game View           │
│  (Left)  │                       │
│          │                       │
├──────────┴───────────────────────┤
│  Unit/Building Controls (Center) │
└──────────────────────────────────┘
```

Configuration:
- `enableTopBar = true` (menu only)
- `includeResourcesInTopBar = false`
- `minimapAnchor = TopLeft`
- `unitDetailsAnchor = BottomCenter`

## Usage Examples

### Basic Setup (In Unity Editor)

1. Create configuration asset
2. Set `enableMinimap = true`, `enableTopBar = false`
3. Create layout preset with default positions
4. Assign to MainHUDFramework
5. Press Play!

### Runtime Configuration

```csharp
// Get HUD framework
MainHUDFramework hud = FindObjectOfType<MainHUDFramework>();

// Toggle components
hud.ToggleComponent("inventory", true);  // Show inventory
hud.ToggleComponent("topbar", false);    // Hide top bar

// Change entire configuration
HUDConfiguration newConfig = Resources.Load<HUDConfiguration>("MinimalHUD");
hud.SetConfiguration(newConfig);

// Change layout
HUDLayoutPreset newLayout = Resources.Load<HUDLayoutPreset>("AoELayout");
hud.ApplyLayoutPreset(newLayout);

// Access specific components
TopBarUI topBar = hud.GetComponent<TopBarUI>();
topBar.Configure(resources: true, menu: true, clock: true, population: true);

InventoryUI inventory = hud.GetComponent<InventoryUI>();
inventory.ConfigureGrid(new Vector2Int(2, 3)); // 2x3 grid
```

### Setting Inventory

```csharp
// Get inventory UI
InventoryUI inventory = hudFramework.GetComponent<InventoryUI>();

// Create inventory data
InventoryData playerInventory = new InventoryData();

// Add items
ItemData sword = new ItemData("Iron Sword", swordSprite, ItemType.Equipment);
ItemData potion = new ItemData("Health Potion", potionSprite, ItemType.Consumable);
potion.stackSize = 5;

playerInventory.AddItem(sword);
playerInventory.AddItem(potion);

// Display inventory
inventory.SetInventory(playerInventory);
```

## Component Details

### TopBarUI

Displays resources and menu buttons at the top of the screen (Warcraft 3 style).

**Features:**
- Dynamic resource display (auto-adapts to new resource types)
- Menu buttons (Menu, Allies, Quests, Chat)
- Optional game clock
- Optional population counter
- Animated resource changes

**Configuration:**
```csharp
topBar.Configure(
    resources: true,      // Show resources
    menu: true,          // Show menu buttons
    clock: false,        // Show game time
    population: true     // Show population (food)
);
```

### InventoryUI

Displays unit inventory with configurable grid layout.

**Features:**
- Flexible grid sizes (2x3, 3x2, 4x2, etc.)
- Item stacking support
- Visual feedback (highlights, colors)
- Drag-and-drop ready (extend InventorySlot)

**Configuration:**
```csharp
inventory.ConfigureGrid(new Vector2Int(3, 2)); // 3 columns, 2 rows
inventory.SetVisible(true);
```

### HUD Layout System

The layout system uses anchor points and offsets for responsive positioning:

**Anchor Positions:**
- `TopLeft`, `TopCenter`, `TopRight`
- `MiddleLeft`, `MiddleCenter`, `MiddleRight`
- `BottomLeft`, `BottomCenter`, `BottomRight`

**Layout Properties:**
- **Size**: Fixed pixel dimensions (Vector2)
- **Offset**: Distance from anchor point (Vector2)
- **Anchor**: Which corner/edge to attach to

Example:
```csharp
minimapAnchor = BottomLeft     // Attach to bottom-left corner
minimapSize = (200, 200)       // 200x200 pixels
minimapOffset = (10, 10)       // 10 pixels from left, 10 from bottom
```

## Best Practices

### 1. Configuration Organization

Create multiple configurations for different scenarios:
- `DefaultHUD` - Standard gameplay
- `MinimalHUD` - Competitive/clean UI
- `FullHUD` - Maximum information
- `TutorialHUD` - Simplified for learning

### 2. Layout Presets

Create presets for different resolutions and aspect ratios:
- `Layout_16x9_1920x1080`
- `Layout_16x10_1920x1200`
- `Layout_4x3_1024x768`

### 3. Performance Optimization

```csharp
// In HUDConfiguration
hudUpdateRate = 30;           // 30 Hz for most UI (saves CPU)
enableAnimations = false;     // Disable in low-end builds
```

### 4. Modular Design

Keep components independent:
- TopBarUI doesn't depend on InventoryUI
- Each component can function standalone
- MainHUDFramework only coordinates, doesn't control logic

### 5. Event-Driven Updates

All UI components use EventBus for updates:
- No polling or Update() loops where possible
- Reduces CPU usage significantly
- Keeps UI responsive to game events

## Extending the Framework

### Adding New Components

1. Create your component (e.g., `QuestLogUI`)
2. Add it to MainHUDFramework:
```csharp
[Header("Custom Components")]
[SerializeField] private QuestLogUI questLogUI;
```

3. Add to configuration:
```csharp
// In HUDConfiguration.cs
public bool enableQuestLog = false;
```

4. Initialize in MainHUDFramework:
```csharp
SetComponentActive(questLogUI?.gameObject, configuration.enableQuestLog, "QuestLog");
```

5. Add layout properties in HUDLayoutPreset:
```csharp
public AnchorPosition questLogAnchor = AnchorPosition.TopLeft;
public Vector2 questLogSize = new Vector2(300, 400);
```

### Custom Layout Presets

Create your own presets via code:
```csharp
HUDLayoutPreset CreateCustomLayout()
{
    var preset = ScriptableObject.CreateInstance<HUDLayoutPreset>();
    preset.presetName = "My Custom Layout";
    preset.minimapAnchor = HUDLayoutPreset.AnchorPosition.TopRight;
    preset.minimapSize = new Vector2(250, 250);
    // ... configure other properties
    return preset;
}
```

## Troubleshooting

### Issue: Components not showing
- Check that component is enabled in HUDConfiguration
- Verify component reference is assigned in MainHUDFramework
- Ensure Canvas is active

### Issue: Layout looks wrong
- Verify CanvasScaler settings (recommended: Scale with Screen Size)
- Check anchor points in layout preset
- Ensure reference resolution matches your target (e.g., 1920x1080)

### Issue: Performance problems
- Reduce `hudUpdateRate` in configuration
- Disable `enableAnimations`
- Use object pooling for dynamic UI elements

### Issue: Resources not showing in top bar
- Enable `includeResourcesInTopBar` in configuration
- Assign resource prefab to TopBarUI
- Check ResourcesService is providing data

## File Structure

```
Assets/Scripts/UI/HUD/
├── MainHUDFramework.cs         # Main controller
├── HUDConfiguration.cs         # Configuration SO
├── HUDLayoutPreset.cs          # Layout SO
├── TopBarUI.cs                 # Top bar component
├── InventoryUI.cs              # Inventory component
└── README.md                   # This file

Assets/Resources/HUD/
├── Configurations/
│   ├── DefaultHUD.asset
│   ├── MinimalHUD.asset
│   └── FullHUD.asset
└── Layouts/
    ├── Warcraft3Layout.asset
    ├── ModernRTSLayout.asset
    └── AoELayout.asset
```

## Version History

- **1.0.0** - Initial framework release
  - Core HUD management system
  - TopBarUI and InventoryUI components
  - Layout preset system
  - Configuration-based setup

## Credits

Developed for Kingdoms at Dusk RTS game.
Inspired by classic RTS interfaces: Warcraft 3, StarCraft, Age of Empires.

## License

Part of the Kingdoms at Dusk project.
