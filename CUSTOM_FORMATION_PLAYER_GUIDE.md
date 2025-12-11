# Custom Formation Player Guide

## Quick Setup (3 Steps)

### 1. Create Formation Settings SO
1. Right-click in Project window
2. Create > RTS > Formation Settings
3. Name it "FormationSettings"
4. In Inspector, customize the `availableFormations` list if desired

### 2. Create Formation Builder UI
1. Go to: **Tools > RTS > Create Formation Builder UI**
2. Click "Create Formation Builder UI" button
3. Done! The UI panel is created and hidden by default

### 3. Run Auto Setup
1. Go to: **Tools > RTS > Formation System Auto Setup**
2. Click "Auto Setup Formation System"
3. This will:
   - Find/create all managers
   - Connect FormationBuilder to UnitDetailsUI
   - Assign FormationSettings SO
   - Clean up duplicates
   - Validate everything

## How Players Create Custom Formations

### Opening the Builder
Players can open the formation builder by:
1. Selecting units
2. Opening the formations dropdown in UnitDetailsUI
3. Clicking "Customize Formation" (if you have FormationBuilderUI)

### Creating a Formation
1. **Grid appears** - 20x20 grid of cells
2. **Click cells** to place unit positions (circles appear)
3. **Click again** to remove positions
4. **Enter name** in the text field at top
5. **Click Save Formation** button
6. Formation is saved and can be used immediately

### Using Custom Formations
- Custom formations appear in the dropdown after preset formations
- Select a custom formation to apply it to selected units
- Formations are saved to: `Application.persistentDataPath/customFormations.json`

## Formation Builder Controls

- **Click cell**: Toggle unit position on/off
- **Clear All**: Remove all placed units
- **Save Formation**: Save current layout with name
- **Close**: Cancel and close builder
- **Units count**: Shows number of units in formation

## Technical Details

### Components Required
- **CustomFormationManager**: Manages saved formations (singleton)
- **FormationGroupManager**: Manages current formation for selected units (singleton)
- **UnitDetailsUI**: Shows formation dropdown
- **FormationBuilderUI**: Grid-based formation creator (optional, for custom formations)

### Flow
1. Player opens FormationBuilderUI
2. Player clicks grid cells to create pattern
3. Player saves formation
4. CustomFormationManager saves to disk
5. Formation appears in dropdown
6. Player selects formation from dropdown
7. FormationGroupManager applies formation to units

### Files Created
- Grid cell prefab: `Assets/Prefabs/FormationGridCell.prefab`
- Saved formations: `{persistentDataPath}/customFormations.json`

## Troubleshooting

### Dropdown is empty
- Make sure FormationSettings SO is created and assigned to UnitDetailsUI
- Run the Auto Setup tool

### "Customize Formation" doesn't appear
- FormationBuilderUI must exist in scene
- Run "Create Formation Builder UI" tool

### Custom formations don't save
- Check console for errors
- Make sure CustomFormationManager exists in scene
- Run Auto Setup tool

### Cross-scene reference errors
- Run Auto Setup tool - it removes invalid references
- Make sure you're using singleton access, not serialized references
