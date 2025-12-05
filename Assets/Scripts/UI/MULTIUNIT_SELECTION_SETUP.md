# Multi-Unit Selection with Icons and HP Bars - Setup Guide

This guide explains how to set up the multi-unit selection UI that displays selected units as icons with HP bars (similar to Warcraft 3, StarCraft, etc.).

## Components Overview

### 1. UnitIconWithHP.cs
- Represents a single unit icon with HP bar
- Displays unit portrait and current health
- Updates HP bar color based on health percentage (green > yellow > red)

### 2. MultiUnitSelectionUI.cs
- Manages the grid of unit icons
- Automatically updates when units are selected/deselected
- Works with UnitSelectionManager to track selected units
- Uses object pooling for performance

### 3. UnitDetailsUI.cs (Updated)
- Continues to show detailed stats for the first selected unit
- Works alongside MultiUnitSelectionUI

## Unity Setup Instructions

### Step 1: Create the Unit Icon Prefab

1. **Create a new UI GameObject** in your scene hierarchy:
   - Right-click in Hierarchy → UI → Image
   - Name it "UnitIconWithHP"

2. **Set up the icon structure**:
   ```
   UnitIconWithHP (Image)
   ├── UnitIcon (Image) - The unit portrait
   └── HPBar (Image) - Background for HP bar
       └── HPBarFill (Image) - The actual HP bar fill
   ```

3. **Configure the components**:
   - **UnitIconWithHP (Root)**:
     - Add the `UnitIconWithHP` script
     - Set size (e.g., 64x64 or 80x80 pixels)
     - Optional: Add border/frame image

   - **UnitIcon**:
     - Image component
     - Preserve Aspect: True (recommended)
     - Size: Fill parent or slightly smaller for padding

   - **HPBar** (Background):
     - Position at bottom of icon
     - Height: 4-8 pixels
     - Color: Dark color (e.g., dark red or black)

   - **HPBarFill**:
     - Position to fill HPBar
     - Color: Green (will change automatically)
     - Image Type: Filled (Horizontal) OR Simple

4. **Assign references in UnitIconWithHP script**:
   - Unit Icon → UnitIcon Image
   - HP Bar Fill → HPBarFill Image
   - HP Bar Background → HPBar Image

5. **Save as Prefab**:
   - Drag the UnitIconWithHP GameObject to your Prefabs folder
   - Delete from scene (we'll spawn them dynamically)

### Step 2: Set Up the Multi-Unit Selection Panel

1. **Create the container panel**:
   - In your Canvas, create a new Panel
   - Name it "MultiUnitSelectionPanel"
   - Position it where you want the unit icons (e.g., bottom-left, like in the screenshot)

2. **Add Grid Layout**:
   - Add a child GameObject (Empty or Panel)
   - Name it "UnitIconContainer"
   - Add `GridLayoutGroup` component:
     - Cell Size: Match your UnitIconWithHP size (e.g., 64x64)
     - Spacing: 4-8 pixels
     - Start Corner: Upper Left (or as preferred)
     - Start Axis: Horizontal
     - Child Alignment: Upper Left
     - Constraint: Fixed Column Count (e.g., 4 or 5)

3. **Add the MultiUnitSelectionUI script**:
   - Add `MultiUnitSelectionUI` script to MultiUnitSelectionPanel
   - Configure:
     - Multi Unit Panel: Assign MultiUnitSelectionPanel itself
     - Unit Icon Container: Assign the UnitIconContainer
     - Unit Icon Prefab: Assign your UnitIconWithHP prefab
     - Max Icons To Display: 12 (or as preferred)
     - Show Only When Multiple: Check this if you want icons only when 2+ units selected

### Step 3: Test in Play Mode

1. **Start the game**
2. **Select multiple units** using:
   - Shift+Click to add units to selection
   - Drag box selection
   - Double-click to select all visible units of same type

3. **Verify**:
   - Unit icons appear in the grid
   - HP bars show current health
   - Colors change based on health (green/yellow/red)
   - Icons update when units take damage
   - Icons disappear when units die

## Customization Options

### Appearance

- **Icon Size**: Adjust in GridLayoutGroup cell size
- **Grid Layout**: Change columns, spacing, start position
- **HP Bar Colors**: Modify in UnitIconWithHP inspector
  - Healthy Color (default: green, >60% HP)
  - Damaged Color (default: yellow, 30-60% HP)
  - Critical Color (default: red, <30% HP)

### Behavior

- **Max Icons**: Change `maxIconsToDisplay` in MultiUnitSelectionUI
- **Show Threshold**:
  - `showOnlyWhenMultiple = true`: Only show when 2+ units selected
  - `showOnlyWhenMultiple = false`: Show even for 1 unit
- **Auto Hide**: `autoHideWhenEmpty = true` to hide when no selection

## Integration with Existing UI

The multi-unit selection UI works alongside your existing UI:

- **UnitDetailsUI**: Shows detailed stats for the first selected unit
- **MultiUnitSelectionUI**: Shows all selected units as icons
- Both update automatically via EventBus

## Example Layouts

### Layout 1: Bottom-Left (Warcraft 3 style)
```
Position: Anchored bottom-left
Grid: 4 columns x 3 rows
Icon Size: 64x64
Spacing: 4px
```

### Layout 2: Left Side (StarCraft style)
```
Position: Anchored left center
Grid: 2 columns x 6 rows
Icon Size: 48x48
Spacing: 2px
```

### Layout 3: Bottom Center
```
Position: Anchored bottom center
Grid: 6 columns x 2 rows
Icon Size: 56x56
Spacing: 8px
```

## Troubleshooting

### Icons not appearing
- Check that MultiUnitSelectionUI has all references assigned
- Verify Unit Icon Prefab has UnitIconWithHP component
- Check that Canvas is set up correctly

### HP bars not updating
- Verify units have UnitHealth component
- Check that units have UnitAIController with Config assigned
- Ensure UnitConfigSO has unitIcon sprite assigned

### Performance issues
- Reduce maxIconsToDisplay
- Consider disabling Update() in UnitIconWithHP and update via events instead
- Use object pooling (already implemented)

## Events Used

The system listens to these EventBus events:
- `UnitSelectedEvent` - When a unit is selected
- `UnitDeselectedEvent` - When a unit is deselected
- `SelectionChangedEvent` - When selection count changes
- `UnitDiedEvent` - When a unit dies

## Further Enhancements

Possible future improvements:
- Click on unit icon to center camera on that unit
- Right-click on icon to remove from selection
- Show unit type indicator or badge
- Add selection count text
- Highlight current primary unit
- Show unit abilities or buffs
