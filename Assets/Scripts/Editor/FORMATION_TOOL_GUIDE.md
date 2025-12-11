# Formation Setup Tool - User Guide

## Overview
The **Formation Setup Tool** is a comprehensive Unity Editor tool for automating the setup, configuration, and testing of the RTS formation system in Kingdoms at Dusk.

## Opening the Tool
**Menu:** `RTS Tools → Formation Setup Tool`

The tool window opens with 5 main tabs:

---

## Tab 1: Scene Setup

### Purpose
Automatically set up all required formation managers and components in your scene.

### Features

#### Current Scene Status
- Shows whether FormationGroupManager exists
- Shows whether CustomFormationManager exists
- Shows whether FormationSettingsSO is configured

#### One-Click Setup
**"Setup Everything" Button**
- Creates FormationGroupManager GameObject
- Creates CustomFormationManager GameObject
- Creates FormationSettingsSO asset (if enabled)
- Automatically wires all references
- Validates the complete setup

#### Individual Setup
- **Create FormationGroupManager** - Add the group manager component
- **Create CustomFormationManager** - Add the custom formation manager
- **Create Formation Settings Asset** - Generate the ScriptableObject configuration

#### Configuration Options
- **Auto-Create Settings Asset** - Automatically generates FormationSettingsSO during setup
- **Auto-Wire References** - Automatically connects FormationGroupManager to FormationSettingsSO
- **Formation Settings** - Manual assignment field for existing settings asset

#### Validation
**"Validate Setup" Button**
- Checks all managers are present
- Verifies settings asset is configured
- Reports number of custom formations loaded
- Shows detailed status report

### Usage Example
1. Open new scene
2. Click "Setup Everything"
3. All managers and settings created automatically
4. System ready to use!

---

## Tab 2: Preset Templates

### Purpose
Create custom formations based on historical military templates.

### Available Templates

1. **Infantry** - Standard infantry formation with tight grid for maximum combat power
2. **Cavalry** - Wide spread formation optimized for mounted units
3. **Archers** - Staggered ranks allowing clear lines of fire
4. **Phalanx** - Dense rectangular formation with overlapping coverage
5. **ShieldWall** - Tight horizontal line with minimal gaps
6. **SkirmishLine** - Loose spread for harassment tactics
7. **Turtle** - Defensive box with units on all sides
8. **CrescentMoon** - Curved envelopment formation
9. **DoubleEnvelopment** - Pincer movement with strong flanks
10. **Flying V** - Wedge with extended wings for breakthrough

### Parameters
- **Formation Name** - Name for the new custom formation
- **Template** - Which historical template to use
- **Unit Count** - Number of units (5-50)

### Usage
1. Enter a name for your formation
2. Select a template from the dropdown
3. Adjust unit count
4. Click "Create Formation from Template"
5. Formation is saved and ready to use!

### Template Descriptions
Each template has a description box explaining its tactical purpose and shape.

---

## Tab 3: Custom Formation Generator

### Purpose
Programmatically generate formations using mathematical patterns.

### Available Patterns

1. **Grid** - Regular grid pattern with even spacing
2. **Circle** - Units arranged in circular formation
3. **Diamond** - Diamond/rhombus shape
4. **Star** - Star pattern with radiating points
5. **Arrow** - Arrow/chevron pointing forward
6. **Cross** - Cross/plus shape
7. **Checkerboard** - Checkerboard pattern with gaps
8. **Spiral** - Spiral pattern from center outward (golden ratio)
9. **Random** - Random scatter within bounds

### Parameters
- **Formation Name** - Name for the generated formation
- **Pattern** - Mathematical pattern to use
- **Unit Count** - Number of units to generate (5-50)
- **Grid Width** - Width of the generation grid (10-30)
- **Grid Height** - Height of the generation grid (10-30)

### Usage
1. Enter formation name
2. Select a pattern
3. Adjust unit count and grid dimensions
4. Click "Generate Formation"
5. Formation created with mathematical precision!

### Pattern Descriptions
Each pattern shows a description of its shape and characteristics.

---

## Tab 4: Batch Operations

### Purpose
Manage multiple formations at once with import/export capabilities.

### Export Operations

#### Export All Custom Formations
- Exports all formations to a JSON file
- Choose save location
- Preserves all formation data

#### Export Selected Formations
- Export specific formations (currently exports all)

### Import Operations

#### Import Formations from File
- Load formations from exported JSON
- Automatically assigns new IDs to avoid conflicts
- Shows count of imported formations

### Bulk Operations

#### Clear All Custom Formations
- **WARNING:** Deletes all custom formations
- Confirmation dialog prevents accidents
- Cannot be undone

#### Generate Standard Formation Pack
- Creates all 10 template formations at once
- Each with 20 units
- Named "Standard [TemplateName]"
- Great starting point for new projects

### Current Formations List

Shows all custom formations with:
- Formation name
- Unit count
- **Duplicate** button - Clone the formation
- **Delete** button - Remove formation (with confirmation)

### Usage Example
1. Create formations using Templates or Generator
2. Export all to backup file
3. Share JSON file with team
4. Team imports formations
5. Everyone has same formation library!

---

## Tab 5: Testing

### Purpose
Test and validate formations before using them in-game.

### Features

#### Formation Selection
- Lists all custom formations
- Click to select for testing

#### Test Parameters
- **Unit Count** - How many units to preview (1 to formation max)
- **Spacing** - Distance between units (0.5-10)
- **Center Position** - World position for preview

#### Visualization

**"Visualize Formation (Scene View)" Button**
- Displays formation positions in Scene View
- Uses Scene View Gizmos
- Check console for position count

**"Generate Test Units" Button**
- Creates actual GameObjects in scene
- Uses cubes as placeholder units
- Creates parent GameObject for organization
- Automatically frames in Scene View
- Can be selected and manipulated

#### Validation

**"Validate All Formations" Button**
- Checks all formations for errors
- Reports valid vs invalid count
- Lists specific issues found:
  - Formations with no positions
  - Formations with no name
  - Other data integrity issues

### Usage Example
1. Select a formation from the list
2. Adjust spacing and unit count
3. Click "Generate Test Units"
4. Inspect formation in Scene View
5. Delete test parent when done
6. Iterate on formation design

---

## Advanced Features

### Auto-Reference System
The tool automatically:
- Finds existing FormationSettingsSO assets
- Wires FormationGroupManager to settings
- Connects managers to scene hierarchy
- Uses Unity's Undo system (Ctrl+Z works!)

### Persistent Preferences
The tool remembers:
- Grid width/height settings
- Preview spacing
- Last used values

### Error Handling
- Validates all operations before execution
- Shows clear error messages
- Prevents duplicate managers
- Confirms destructive operations

### Integration with Formation System
All created formations:
- Automatically saved to persistent storage
- Immediately available in game UI
- Compatible with all formation features
- Support quick-access list

---

## Common Workflows

### Setting Up a New Scene
1. Open Scene Setup tab
2. Click "Setup Everything"
3. Done! System ready to use

### Creating a Formation Library
1. Go to Batch Operations
2. Click "Generate Standard Formation Pack"
3. Customize individual formations as needed
4. Export all to backup file

### Testing Custom Formations
1. Create formation in Templates or Generator
2. Go to Testing tab
3. Select the formation
4. Generate test units
5. Adjust and iterate

### Sharing Formations with Team
1. Create formations
2. Export all to JSON
3. Commit JSON to version control
4. Team imports JSON file
5. Everyone has same formations

---

## Troubleshooting

### "CustomFormationManager not found"
**Solution:** Go to Scene Setup tab and click "Setup Everything"

### "No formations to test"
**Solution:** Create formations first using Templates or Generator tabs

### Formations not appearing in game
**Solution:**
1. Go to Batch Operations tab
2. Check formations are listed
3. Validate Setup in Scene Setup tab
4. Restart Unity if needed

### Import fails
**Solution:**
- Check JSON file is valid
- Ensure file contains "formations" array
- Re-export from working setup

---

## Technical Details

### File Locations

**Custom Formations Storage:**
```
Windows: %USERPROFILE%\AppData\LocalLow\{Company}\{Game}\CustomFormations.json
Mac: ~/Library/Application Support/{Company}/{Game}/CustomFormations.json
Linux: ~/.config/unity3d/{Company}/{Game}/CustomFormations.json
```

**Formation Settings Asset:**
Created in location you choose via save dialog (typically `Assets/Prefabs/`)

### Coordinate System

All formations use normalized coordinates:
- Range: -1 to 1 on both X and Y axes
- Center: (0, 0)
- Converted to world space using spacing multiplier
- Facing direction determines orientation

### Unit Count Handling

- Templates/Generator: Creates exact number specified
- Testing: Can preview subset of total formation
- Game: Uses all positions up to selected unit count

---

## Keyboard Shortcuts

While tool window is focused:
- **Ctrl+Z** - Undo last operation
- **Ctrl+Y** - Redo

---

## Best Practices

1. **Always validate setup** before creating formations
2. **Export regularly** to avoid losing work
3. **Name formations descriptively** (e.g., "Cavalry Charge V2" not "Formation 1")
4. **Test formations** before using in production
5. **Use Standard Pack** as a starting point
6. **Keep backups** of exported JSON files

---

## Tips & Tricks

### Quick Formation Variants
1. Create base formation
2. Use Duplicate in Batch Operations
3. Modify the duplicate
4. Repeat for multiple variants

### Mathematical Patterns
- **Spiral** creates organic, natural-looking formations
- **Checkerboard** good for spread-out ranged units
- **Star** visually impressive for special abilities

### Template Combinations
- Start with a template
- Export to JSON
- Edit JSON manually for fine control
- Import back

### Testing Workflow
1. Generate test units
2. Take screenshot
3. Delete test parent
4. Repeat with different spacing
5. Compare screenshots

---

## API Reference (For Programmers)

### Creating Formations Programmatically

```csharp
// Get manager
var manager = CustomFormationManager.Instance;

// Create positions
var positions = new List<FormationPosition>();
positions.Add(new FormationPosition(0, 0));
positions.Add(new FormationPosition(-0.5f, 0.5f));
// ... add more

// Create formation
var formation = new CustomFormationData("My Formation", positions);

// Save
manager.AddFormation(formation);
```

### Accessing in Game

```csharp
// Get formation by ID
var formation = CustomFormationManager.Instance.GetFormation(formationId);

// Calculate world positions
var worldPositions = formation.CalculateWorldPositions(
    centerPosition,
    spacing,
    facingDirection
);

// Apply to units
for (int i = 0; i < units.Count && i < worldPositions.Count; i++)
{
    units[i].SetDestination(worldPositions[i]);
}
```

---

## Version History

**v1.0**
- Initial release
- 5 tabs with full functionality
- 10 preset templates
- 9 generation patterns
- Import/export support
- Testing and validation

---

## Support

For issues or feature requests:
1. Check this guide first
2. Validate setup in Scene Setup tab
3. Check Unity console for errors
4. Review formation system documentation in CLAUDE.md

---

**Happy Formation Building!**
