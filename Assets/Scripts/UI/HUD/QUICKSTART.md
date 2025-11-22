# HUD Framework Quick Start Guide

Get your HUD up and running in 5 minutes!

## Step 1: Generate Templates (30 seconds)

In Unity Editor menu:
```
Tools > RTS > Create HUD Templates > All Templates
```

This creates:
- **3 Configuration presets** (Default, Minimal, Full)
- **4 Layout presets** (Warcraft 3, Modern RTS, Age of Empires, Compact)

Files will be in:
- `Assets/Resources/HUD/Configurations/`
- `Assets/Resources/HUD/Layouts/`

## Step 2: Setup Scene (2 minutes)

### Create UI Structure

1. In your main scene, create:
```
Canvas (if you don't have one)
└── MainHUD (Empty GameObject)
    ├── TopBar (Empty GameObject)
    ├── Minimap (Your existing minimap panel)
    ├── UnitDetails (Your existing unit details panel)
    ├── BuildingDetails (Your existing building details panel)
    ├── BuildingHUD (Your existing building HUD panel)
    ├── Inventory (Empty GameObject)
    ├── ResourcePanel (Your existing resource panel)
    ├── HappinessPanel (Your existing happiness panel)
    └── Notifications (Your existing notification panel)
```

### Add MainHUDFramework Component

1. Select the `MainHUD` GameObject
2. Add Component > `MainHUDFramework`
3. Assign your HUD Configuration (e.g., `DefaultHUDConfig`)

### Link Component References

In the MainHUDFramework inspector, drag and drop:
- **Minimap Panel**: Your minimap GameObject
- **Unit Details UI**: Your UnitDetailsUI component
- **Building Details UI**: Your BuildingDetailsUI component
- **Building HUD**: Your BuildingHUD component
- **Resource UI**: Your ResourceUI component (if using standalone)
- **Happiness UI**: Your HappinessUI component
- **Notification UI**: Your NotificationUI component

## Step 3: Configure TopBar (Optional, 1 minute)

If you want the Warcraft 3-style top bar:

1. Select the `TopBar` GameObject
2. Add Component > `TopBarUI`
3. Create UI elements:

### Create TopBar UI (Quick Setup)

```
TopBar (with Image component - dark background)
├── Resources (Horizontal Layout Group)
│   └── ResourceItem (Prefab with TextMeshPro)
├── MenuButtons (Horizontal Layout Group)
│   ├── MenuButton
│   ├── AlliesButton (F11)
│   ├── QuestsButton (F9)
│   └── ChatButton (F12)
├── Clock (TextMeshPro - optional)
└── Population (TextMeshPro)
```

Link these to TopBarUI component.

## Step 4: Configure Inventory (Optional, 1 minute)

If you want unit inventory:

1. Select the `Inventory` GameObject
2. Add Component > `InventoryUI`
3. Create slot structure:

```
Inventory (with Grid Layout Group)
└── SlotsContainer (GridLayoutGroup 3x2)
    └── SlotPrefab (Image + child Image for icon)
```

Set:
- **Grid Size**: 3x2 (in MainHUDFramework config)
- **Slot Prefab**: Your slot prefab
- **Slots Container**: The SlotsContainer transform

## Step 5: Choose Layout & Test (30 seconds)

1. In your HUD Configuration asset:
   - Assign a Layout Preset (e.g., `Warcraft3Layout`)
2. Press Play!

The HUD will automatically arrange itself based on the layout.

## Quick Configuration Comparison

### Want Warcraft 3 Style?
```
Configuration: Warcraft3HUDConfig
Layout: Warcraft3Layout

Settings:
✓ Top Bar (with resources)
✓ Inventory (3x2)
✓ Minimap (bottom-left)
✓ All animations
```

### Want Clean/Minimal?
```
Configuration: MinimalHUDConfig
Layout: ModernRTSLayout

Settings:
✓ No top bar
✗ No inventory
✓ Standalone resources (top-center)
✗ No animations
```

### Want Maximum Info?
```
Configuration: FullHUDConfig
Layout: Warcraft3Layout

Settings:
✓ Everything enabled
✓ Top bar + Inventory
✓ Notifications
✓ All features
```

## Common Scenarios

### Scenario 1: "I want Warcraft 3 style exactly"

```csharp
// In MainHUDFramework
configuration = Resources.Load<HUDConfiguration>("HUD/Configurations/Warcraft3HUDConfig");
```

Done! Everything else is automatic.

### Scenario 2: "I want top bar OR standalone resources, not both"

Option A (Top bar with resources):
```
enableTopBar = true
includeResourcesInTopBar = true
showStandaloneResourcePanel = false  // Auto-set
```

Option B (Standalone resources):
```
enableTopBar = false
showStandaloneResourcePanel = true
```

### Scenario 3: "I want to switch layouts at runtime"

```csharp
// Get HUD framework
MainHUDFramework hud = FindObjectOfType<MainHUDFramework>();

// Switch to compact layout
HUDLayoutPreset compact = Resources.Load<HUDLayoutPreset>("HUD/Layouts/CompactLayout");
hud.ApplyLayoutPreset(compact);
```

### Scenario 4: "I want to toggle inventory in-game"

```csharp
MainHUDFramework hud = FindObjectOfType<MainHUDFramework>();
hud.ToggleComponent("inventory", true);  // Show
hud.ToggleComponent("inventory", false); // Hide
```

## Prefab Setup (Recommended)

### Resource Item Prefab (for TopBarUI)

```
ResourceItem (GameObject)
├── Image (background)
├── NameText (TextMeshProUGUI)
├── AmountText (TextMeshProUGUI)
└── Icon (Image - optional)
```

### Inventory Slot Prefab

```
InventorySlot (GameObject)
├── Background (Image - slot background)
├── ItemIcon (Image - hidden by default)
├── StackText (TextMeshProUGUI - for stack count)
└── HighlightBorder (Image - optional)
```

## Testing Your Setup

### Test Checklist

1. ✓ Press Play - HUD appears
2. ✓ Select unit - Unit details show
3. ✓ Select building - Building details show
4. ✓ Resources update - UI reflects changes
5. ✓ Open building menu - Building HUD appears

### Debug Mode

In MainHUDFramework inspector (while playing):
- Right-click component > "Reload Configuration"
- Watch console for initialization logs
- Each component logs "Enabled/Disabled" status

## Troubleshooting

**Nothing shows up?**
- Check Canvas has CanvasScaler
- Verify configuration asset is assigned
- Ensure component references are linked

**Wrong positions?**
- Assign a layout preset
- Check Canvas reference resolution (1920x1080 recommended)

**Resources not in top bar?**
- Set `includeResourcesInTopBar = true`
- Assign ResourceItemPrefab to TopBarUI
- Check TopBarUI has ResourceContainer assigned

**Performance issues?**
- Lower `hudUpdateRate` to 15-20
- Disable `enableAnimations`
- Use Compact layout

## Next Steps

Once basic setup works:

1. **Customize layouts** - Adjust positions/sizes in layout presets
2. **Create new configurations** - For different game modes
3. **Add custom components** - Extend the framework
4. **Polish visuals** - Add backgrounds, borders, effects
5. **Add hotkeys** - Toggle UI elements with keys

## Example: Complete Setup Script

```csharp
using UnityEngine;
using RTS.UI.HUD;

public class HUDSetup : MonoBehaviour
{
    void Start()
    {
        // Get framework
        var hud = FindObjectOfType<MainHUDFramework>();

        // Load Warcraft 3 style
        var config = Resources.Load<HUDConfiguration>("HUD/Configurations/Warcraft3HUDConfig");
        var layout = Resources.Load<HUDLayoutPreset>("HUD/Layouts/Warcraft3Layout");

        // Apply
        hud.SetConfiguration(config);
        hud.ApplyLayoutPreset(layout);

        Debug.Log("HUD configured as Warcraft 3 style!");
    }
}
```

## Resources

- Full documentation: `README.md`
- Create templates: `Tools > RTS > Create HUD Templates`
- Example configurations: `Assets/Resources/HUD/Configurations/`
- Example layouts: `Assets/Resources/HUD/Layouts/`

---

**Time to fully functional HUD: ~5 minutes**

Happy building! 🎮
