# 🚀 Automated Multi-Unit Selection UI Setup

This guide explains how to use the **automated setup tool** to instantly integrate the Multi-Unit Selection UI system with your existing UnitDetailsUI panel.

## ✨ One-Click Setup

### Step 1: Run the Setup Tool

In Unity Editor, go to the menu:

```
Tools > RTS > Setup Multi-Unit Selection UI
```

That's it! ✅

The tool will automatically:
- ✅ Find your existing `UnitDetailsUI` panel
- ✅ Create the `UnitIconWithHP` prefab with all components
- ✅ Wrap your single-unit stats in a container
- ✅ Create a multi-unit selection container with grid layout
- ✅ Configure all references and integrate everything
- ✅ Enable smart switching between modes

### Step 2: Test It Out

1. Enter **Play Mode**
2. Select **1 unit**: See normal detailed stats (portrait, health, speed, attack, etc.)
3. Select **2+ units**: Stats are replaced with a grid of unit icons with HP bars
4. **Formation buttons remain visible** in both modes!

## 🎯 How It Works

### Single Unit Selected (1 unit)
- Shows detailed stats (health, speed, attack damage, attack speed, attack range)
- Displays unit portrait
- Shows health bar
- Formation controls visible

### Multiple Units Selected (2+ units)
- **Replaces stats section** with grid of unit icons
- Each icon shows:
  - Unit portrait
  - Real-time HP bar (Green → Yellow → Red)
- **Formation buttons stay visible** for controlling the group
- Grid supports up to 12 units (4 columns × 3 rows)

## 📦 What Gets Created

### UI Component Structure
```
UnitDetailsUI (existing panel)
├── Unit Portrait (existing)
├── Unit Name (existing)
├── SingleUnitStatsContainer (NEW - wraps your stats)
│   └── Stats Container (your existing stats UI)
│       ├── Health Text
│       ├── Speed Text
│       ├── Attack Damage Text
│       ├── Attack Speed Text
│       └── Attack Range Text
├── MultiUnitSelectionContainer (NEW - hidden by default)
│   ├── MultiUnitSelectionUI component
│   └── UnitIconGrid (GridLayoutGroup)
│       └── (Unit icons spawn here at runtime)
└── Formation Controls (existing - always visible)
    ├── Formation Dropdown
    ├── Custom Formation Button
    └── Create Formation Button
```

### Prefab Created
```
UnitIconWithHP.prefab (64x64px)
├── UnitIcon (Image) - Shows unit portrait
└── HPBar (Image) - Dark red background
    └── HPBarFill (Image) - Green fill (changes color based on HP)
```

## 🎨 Default Configuration

- **Grid**: 4 columns × 3 rows = 12 max icons
- **Icon Size**: 64×64 pixels
- **Spacing**: 8px between icons
- **HP Colors**:
  - Green (>60% HP)
  - Yellow (30-60% HP)
  - Red (<30% HP)
- **Mode Switching**: Automatic based on selection count

## 🔧 Customization

After setup, you can customize in the Inspector:

### UnitDetailsUI Component
New fields added:
- `Single Unit Stats Container` - Container shown for 1 unit
- `Multi Unit Selection Container` - Container shown for 2+ units
- `Multi Unit Selection UI` - Reference to the grid controller

### MultiUnitSelectionUI Component
- `Unit Icon Container` - Where icons spawn (auto-assigned)
- `Unit Icon Prefab` - The UnitIconWithHP prefab (auto-assigned)
- `Max Icons To Display` - Maximum number of icons (default: 12)
- `Grid Layout Group` - Grid layout reference (auto-assigned)

### Adjust Grid Layout
Select `UnitDetailsUI > MultiUnitSelectionContainer > UnitIconGrid`:
- `Cell Size` - Size of each icon (default: 64×64)
- `Spacing` - Gap between icons (default: 8px)
- `Constraint Count` - Number of columns (default: 4)
- `Padding` - Padding around the grid (default: 5px)

### Adjust Icon Size
Edit the prefab at `Assets/Prefabs/UI/UnitIconWithHP.prefab`:
- Change RectTransform size
- Adjust HP bar height
- Modify colors

## 🔌 Integration Details

### EventBus Integration
The system listens to these events:
- `UnitSelectedEvent` - When a unit is selected
- `UnitDeselectedEvent` - When a unit is deselected
- `SelectionChangedEvent` - Triggers mode switching (1 unit vs 2+ units)
- `UnitDiedEvent` - Removes dead unit icons

### Mode Switching Logic
```
Selection Count = 0  → Hide entire panel
Selection Count = 1  → Show single unit stats (hide multi-unit grid)
Selection Count ≥ 2  → Hide single unit stats (show multi-unit grid)
Formation buttons    → Always visible when panel is shown
```

## 🧹 Cleanup

To remove the integration and restore original UnitDetailsUI:

```
Tools > RTS > Remove Multi-Unit Selection UI
```

This removes the containers and restores your original UI structure.

## 📋 Requirements

- **UnitDetailsUI** component must exist in your scene
- **UnitSelectionManager** must be present
- Units need:
  - `UnitSelectable` component
  - `UnitAIController` with `Config` assigned
  - `UnitConfigSO` with `unitIcon` sprite
  - `UnitHealth` component (optional, for HP bars)

## 🚨 Troubleshooting

### Setup says "UnitDetailsUI not found"
- Ensure UnitDetailsUI component exists in your scene
- The component must be on an active GameObject
- Run the setup while in a game scene (not main menu)

### Icons not appearing when selecting multiple units
- Check that `MultiUnitSelectionContainer` is assigned in UnitDetailsUI Inspector
- Verify that `UnitIconPrefab` is assigned in MultiUnitSelectionUI component
- Ensure `UnitSelectionManager` exists in the scene

### Stats not switching back to single unit
- Verify `SingleUnitStatsContainer` is assigned in UnitDetailsUI Inspector
- Check Console for any errors during mode switching

### HP bars not updating
- Verify units have `UnitHealth` component
- Check that `UnitConfigSO` has `maxHealth` > 0
- Ensure `unitIcon` sprite is assigned in `UnitConfigSO`

### Formation buttons disappeared
- Formation buttons should remain outside the containers
- If missing, manually drag them out of SingleUnitStatsContainer
- They should be direct children of UnitDetailsUI panel

## 💡 Tips & Best Practices

1. **Organize Your UI**: After setup, you can manually reorganize elements in the Inspector
2. **Customize Colors**: Edit HP bar colors in the `UnitIconWithHP` component
3. **Adjust Grid**: Change column count for different layouts (2-6 columns recommended)
4. **Test Both Modes**: Make sure both single and multi-selection look good
5. **Keep Formation Buttons**: Don't accidentally move them into containers

## 🎮 Features

- ✅ **Smart Mode Switching** - Automatic based on selection count
- ✅ **Formation Preservation** - Buttons stay visible in both modes
- ✅ **Real-time HP** - Live updates with color coding
- ✅ **Object Pooling** - Reuses icons for performance
- ✅ **Event-Driven** - Fully integrated with EventBus
- ✅ **Clean Integration** - Works with existing UnitDetailsUI

## 📖 Manual Setup Alternative

If you prefer manual setup, see:
- `MULTIUNIT_SELECTION_SETUP.md` - Detailed manual setup guide

## 🎯 What's Different from Standalone?

Previously, MultiUnitSelectionUI was a standalone panel. Now it's **integrated**:

### Old Approach (Standalone)
- Separate panel for multi-unit selection
- Shows alongside UnitDetailsUI
- Takes up additional screen space

### New Approach (Integrated) ✅
- **Replaces stats section** when 2+ units selected
- Uses existing UnitDetailsUI panel space
- Formation buttons stay visible
- Cleaner, more efficient UI

---

**That's it!** Your Multi-Unit Selection UI is now fully integrated with UnitDetailsUI. Select units and watch it intelligently switch between single and multi-unit modes! 🎉
