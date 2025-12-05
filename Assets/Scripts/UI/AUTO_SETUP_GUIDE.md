# 🚀 Automated Multi-Unit Selection UI Setup

This guide explains how to use the **automated setup tool** to instantly create and configure the Multi-Unit Selection UI system.

## ✨ One-Click Setup

### Step 1: Run the Setup Tool

In Unity Editor, go to the menu:

```
Tools > RTS > Setup Multi-Unit Selection UI
```

That's it! ✅

The tool will automatically:
- ✅ Create the `UnitIconWithHP` prefab with all components
- ✅ Create the `MultiUnitSelectionPanel` in your scene
- ✅ Set up the Grid Layout for unit icons
- ✅ Configure all references and settings
- ✅ Position the panel at the bottom-left corner
- ✅ Enable object pooling for performance

### Step 2: Test It Out

1. Enter **Play Mode**
2. Select multiple units by:
   - **Shift+Click** to add units to selection
   - **Drag** to create a selection box
   - **Double-click** a unit to select all visible units of that type
3. Watch the unit icons appear with HP bars!

## 🎨 What Gets Created

### UI Prefab Structure
```
UnitIconWithHP.prefab (64x64px)
├── UnitIcon (Image) - Shows unit portrait
└── HPBar (Image) - Dark red background
    └── HPBarFill (Image) - Green fill (changes color based on HP)
```

### Scene Hierarchy
```
Canvas
└── MultiUnitSelectionPanel
    └── UnitIconContainer (GridLayoutGroup)
        └── (Unit icons spawn here at runtime)
```

## 📍 Default Configuration

- **Position**: Bottom-left corner (20px, 20px offset)
- **Grid**: 4 columns × 3 rows
- **Icon Size**: 64×64 pixels
- **Spacing**: 8px between icons
- **Max Icons**: 12 units displayed
- **Show Only When**: 2+ units selected
- **Auto Hide**: When selection is empty

## 🔧 Customization

After setup, you can customize the panel in the Inspector:

### MultiUnitSelectionUI Component
- `Max Icons To Display` - Maximum number of icons (default: 12)
- `Show Only When Multiple` - Only show when 2+ units selected
- `Auto Hide When Empty` - Hide panel when no units selected

### Grid Layout Group
- `Cell Size` - Size of each icon (default: 64×64)
- `Spacing` - Gap between icons (default: 8px)
- `Constraint Count` - Number of columns (default: 4)

### Panel Position
Drag the `MultiUnitSelectionPanel` in the scene to reposition it:
- **Bottom-left** (Warcraft 3 style) - Default
- **Left side** (StarCraft style)
- **Bottom-center**

## 🧹 Cleanup

To remove the UI from your scene:

```
Tools > RTS > Remove Multi-Unit Selection UI
```

This removes the panel from the scene but keeps the prefab.

## 🔌 Integration

The system automatically integrates with:
- **UnitSelectionManager** - Tracks selected units
- **EventBus** - Listens for selection events
- **UnitHealth** - Monitors HP changes
- **UnitAIController** - Gets unit icons from Config

No additional setup required! Everything works out of the box.

## 📦 What's Included

### Components Created
1. **MultiUnitSelectionUI.cs** - Main controller
   - Grid management
   - Object pooling for performance
   - Automatic updates via EventBus

2. **UnitIconWithHP.cs** - Individual unit icons
   - Displays unit portrait
   - Animated HP bar
   - Color-coded health (Green/Yellow/Red)

3. **MultiUnitSelectionUISetup.cs** (Editor) - Automated setup tool
   - One-click prefab creation
   - Scene configuration
   - Reference assignment

### Events Handled
- `UnitSelectedEvent` - Unit added to selection
- `UnitDeselectedEvent` - Unit removed from selection
- `SelectionChangedEvent` - Selection count changed
- `UnitDiedEvent` - Unit destroyed (removes icon)

## 🎮 Features

- ✅ **Automatic Updates** - Icons update when units selected/deselected
- ✅ **Real-time HP Bars** - Shows current health with color coding
- ✅ **Object Pooling** - Reuses icons for performance
- ✅ **Smart Visibility** - Auto-hides when not needed
- ✅ **Grid Layout** - Clean, organized display
- ✅ **Max Selection Limit** - Prevents UI overflow
- ✅ **Event-Driven** - Integrated with game systems

## 🚨 Troubleshooting

### Icons not appearing?
- Verify `UnitSelectionManager` exists in scene
- Check that units have `UnitSelectable` component
- Ensure `UnitAIController` has `Config` assigned with `unitIcon` sprite

### HP bars not updating?
- Verify units have `UnitHealth` component
- Check that `UnitConfigSO` has `maxHealth` set

### Want to reset?
1. Run: `Tools > RTS > Remove Multi-Unit Selection UI`
2. Run: `Tools > RTS > Setup Multi-Unit Selection UI`

## 📖 Manual Setup

If you prefer manual setup, see:
- `MULTIUNIT_SELECTION_SETUP.md` - Detailed manual setup guide

## 💡 Tips

1. **Customize Icons**: Edit the prefab at `Assets/Prefabs/UI/UnitIconWithHP.prefab`
2. **Adjust Colors**: Modify HP bar colors in the `UnitIconWithHP` component
3. **Change Layout**: Adjust Grid Layout settings for different arrangements
4. **Reposition Panel**: Drag in scene or change anchors for different screen positions

---

**That's it!** Your Multi-Unit Selection UI is ready to use. Select multiple units and watch the magic happen! 🎉
