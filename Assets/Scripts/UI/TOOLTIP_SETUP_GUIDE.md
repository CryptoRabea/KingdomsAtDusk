# Universal Tooltip System - Setup Guide

This guide explains how to set up the new universal tooltip system for buildings, units, towers, and walls.

## Overview

The tooltip system consists of:
1. **UniversalTooltip** - Main tooltip UI component
2. **TooltipData** - Data structure for tooltip content
3. **BuildingButton** - Shows icon only, displays tooltip on hover
4. **TrainUnitButton** - Unit training button with tooltip support
5. **BuildingSO / UnitConfigSO** - Scriptable objects with optional stats

## Features

- ✅ Icon-only buttons (no text clutter)
- ✅ Tooltip appears on hover at fixed position above HUD
- ✅ Shows building/unit name, icon, description
- ✅ Displays costs with resource icons
- ✅ Optional stats: HP, Defence, Attack Damage, Attack Range, Attack Speed, Construction/Training Time
- ✅ All stats configurable in Inspector per building/unit

---

## Part 1: Create Tooltip UI in Unity

### Step 1: Create Tooltip Panel

1. In your Canvas, create a new Panel GameObject named `UniversalTooltip`
2. Set the following RectTransform properties:
   - Anchors: Middle Center
   - Position: (0, 200, 0) - This places it above the HUD
   - Size: (400, 300) - Adjust as needed
3. Add a semi-transparent background (e.g., dark gray with 90% opacity)

### Step 2: Add Title and Icon

1. Create a child TextMeshProUGUI named `Title`
   - Set font size: 24
   - Set alignment: Center
   - Position at top of panel

2. Create a child Image named `Icon`
   - Position next to title
   - Size: (64, 64)

### Step 3: Add Description

1. Create a child TextMeshProUGUI named `Description`
   - Set font size: 14
   - Enable word wrapping
   - Position below title

### Step 4: Create Costs Container

1. Create an empty GameObject named `CostsContainer`
   - Add Horizontal Layout Group component
   - Set spacing: 10
   - Position below description

2. Create a prefab for cost items:
   - Create a GameObject named `CostItemPrefab`
   - Add Horizontal Layout Group
   - Add child Image named `Icon` (for resource icon)
   - Add child TextMeshProUGUI named `Text` (for amount)
   - Save as prefab and delete from scene

### Step 5: Create Stats Container

1. Create an empty GameObject named `StatsContainer`
   - Add Vertical Layout Group component
   - Set spacing: 5
   - Position below costs

2. Add TextMeshProUGUI children for each stat:
   - `ConstructionTimeText`
   - `HPText`
   - `DefenceText`
   - `AttackDamageText`
   - `AttackRangeText`
   - `AttackSpeedText`
   - Set font size: 12-14
   - Alignment: Left

### Step 6: Configure UniversalTooltip Component

1. Add the `UniversalTooltip` component to the tooltip panel
2. Assign all references:
   - Tooltip Panel: The root panel GameObject
   - Tooltip Rect: The RectTransform of the panel
   - Title Text: Title TextMeshProUGUI
   - Icon Image: Icon Image component
   - Description Text: Description TextMeshProUGUI
   - Costs Container: CostsContainer GameObject
   - Cost Item Prefab: Your CostItemPrefab
   - Stats Container: StatsContainer GameObject
   - All stat TextMeshProUGUI references
3. Assign resource icons (Wood, Food, Gold, Stone sprites)
4. Set Fixed Position: (0, 200) - Adjust to position above your HUD
5. Enable "Use Fixed Position"

---

## Part 2: Configure BuildingHUD

1. Open your BuildingHUD GameObject in the Inspector
2. Find the `BuildingHUD` component
3. Assign the `UniversalTooltip` reference to the `Building Tooltip` field

The BuildingHUD will automatically pass this tooltip reference to all building buttons!

---

## Part 3: Configure Building Buttons

### For Icon-Only Display:

1. Select your BuildingButton prefab
2. In the BuildingButton component:
   - Enable `Show Icon Only` ✅
   - Enable `Show Tooltip On Hover` ✅
3. The nameText, costText, and costContainer will be automatically hidden

### Building Button Hierarchy (for icon-only mode):

```
BuildingButton (Button component + BuildingButton script)
├── Icon (Image) - Required
└── HotkeyText (TextMeshProUGUI) - Optional, shows [1], [2], etc.
```

That's it! The tooltip will automatically show on hover.

---

## Part 4: Configure BuildingSO Stats

1. Open any BuildingSO asset in the Inspector
2. Scroll to the **Combat Stats (Optional - For Tooltips)** section
3. Enable the stats you want to show:
   - ☑️ Show HP - Check to display HP in tooltip
   - ☑️ Show Defence - Check to display Defence stat
   - ☑️ Show Attack Damage - Check to display Attack stat
   - ☑️ Show Attack Range - Check to display Range stat
   - ☑️ Show Attack Speed - Check to display Attack Speed
4. Set the values for enabled stats
5. Construction time is always shown automatically

### Example Configuration:

**Barracks Building:**
- Show HP: ✅ (maxHealth: 500)
- Show Defence: ✅ (defence: 10)
- Show Attack Damage: ❌
- Show Attack Range: ❌
- Show Attack Speed: ❌

**Defensive Tower:**
- Show HP: ✅ (maxHealth: 800)
- Show Defence: ✅ (defence: 20)
- Show Attack Damage: ✅ (attackDamage: 50)
- Show Attack Range: ✅ (attackRange: 15)
- Show Attack Speed: ✅ (attackSpeed: 0.5)

---

## Part 5: Configure UnitConfigSO Stats

1. Open any UnitConfigSO asset in the Inspector
2. Add a description in the `Description` field
3. Scroll to the **Tooltip Display (Optional)** section
4. Enable/disable stats to show:
   - ☑️ Show HP (defaults to ON)
   - ☑️ Show Defence
   - ☑️ Show Attack Damage (defaults to ON)
   - ☑️ Show Attack Range (defaults to ON)
   - ☑️ Show Attack Speed (defaults to ON)

The system automatically reads the existing stats from UnitConfigSO:
- `maxHealth` → HP
- `defence` → Defence
- `attackDamage` → Attack
- `attackRange` → Range
- `attackRate` → Attack Speed

---

## Part 6: Unit Training Buttons (Optional)

If you want tooltips on unit training buttons:

1. Find the BuildingDetailsUI or where unit buttons are created
2. Pass the tooltip reference to TrainUnitButton.Initialize():
   ```csharp
   trainUnitButton.Initialize(unitData, trainingQueue, tooltipReference);
   ```
3. Or call SetTooltip() after initialization:
   ```csharp
   trainUnitButton.SetTooltip(tooltipReference);
   ```

---

## Tooltip Display Logic

### What Shows in Tooltip:

**Always Shown:**
- Building/Unit name (title)
- Icon
- Description
- Resource costs (Wood, Food, Gold, Stone) with icons
- Construction/Training time

**Conditionally Shown (based on SO settings):**
- HP (if showHP = true)
- Defence (if showDefence = true)
- Attack Damage (if showAttackDamage = true)
- Attack Range (if showAttackRange = true)
- Attack Speed (if showAttackSpeed = true)

---

## Positioning System

The tooltip uses a **fixed position** by default, appearing at the same location for all buttons (above the HUD).

### To Customize Position:

1. Select the UniversalTooltip component
2. Adjust `Fixed Position` (default: X=0, Y=200)
   - Positive Y moves it up
   - Negative Y moves it down
3. Keep `Use Fixed Position` enabled

### For Dynamic Positioning (follows mouse):

1. Disable `Use Fixed Position`
2. The tooltip will follow the mouse cursor

---

## Testing

1. Enter Play Mode
2. Hover over any building button
3. Tooltip should appear above the HUD showing:
   - Building name and icon
   - Description
   - Resource costs with colored icons
   - Construction time
   - Any enabled stats

---

## Troubleshooting

### Tooltip doesn't appear on hover:
- ✅ Check that UniversalTooltip reference is assigned in BuildingHUD
- ✅ Verify BuildingButton has `Show Tooltip On Hover` enabled
- ✅ Ensure tooltip panel is active in hierarchy

### Stats don't show:
- ✅ Check that the stat toggle is enabled in BuildingSO/UnitConfigSO
- ✅ Verify the stat value is set (e.g., attackDamage > 0)
- ✅ Check that the TextMeshProUGUI reference is assigned in UniversalTooltip

### Resource icons are wrong color:
- ✅ Assign proper sprites to Wood Icon, Food Icon, Gold Icon, Stone Icon
- ✅ If using colored squares, the system auto-colors them

### Tooltip position is wrong:
- ✅ Adjust `Fixed Position` in UniversalTooltip component
- ✅ Make sure tooltip RectTransform uses Middle Center anchors

---

## Advanced: Extending for Towers and Walls

The system is ready for towers and walls. Just:

1. Add similar optional stats to your TowerSO/WallSO scriptable objects
2. Create a `TooltipData.FromTower()` or `TooltipData.FromWall()` method
3. Use the same UniversalTooltip component

Example:
```csharp
public static TooltipData FromTower(TowerDataSO towerData)
{
    return new TooltipData
    {
        title = towerData.towerName,
        description = towerData.description,
        icon = towerData.icon,
        costs = towerData.GetCosts(),

        showConstructionTime = true,
        constructionTime = towerData.constructionTime,

        showHP = towerData.showHP,
        maxHP = towerData.maxHealth,
        // ... etc
    };
}
```

---

## Summary

✅ Icon-only building buttons (clean UI)
✅ Comprehensive tooltips on hover
✅ Fixed position above HUD (adjustable)
✅ All stats optional and configurable
✅ Works for buildings, units, towers, walls
✅ Easy to extend and customize

Happy building! 🏰⚔️
