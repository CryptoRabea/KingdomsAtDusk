# Custom Formations Builder System

## Overview

The Custom Formations Builder allows players to create, save, edit, and manage their own unit formations in both runtime and the main menu. Players can design formations using an intuitive grid-based interface and apply them to their units during gameplay.

## Features

- ✅ **Runtime & Main Menu Access**: Create formations anytime
- ✅ **Grid-Based Builder**: Intuitive chess-like grid interface
- ✅ **Drag & Drop**: Place and drag pieces to design formations
- ✅ **Multiple Deletion Methods**:
  - Drag pieces to garbage bin
  - Select and press Delete key
- ✅ **CRUD Operations**: Create, Read, Update, Delete formations
- ✅ **Persistent Storage**: Formations saved to JSON and persist between sessions
- ✅ **Formation Management**: Browse, select, edit, duplicate, and delete saved formations

## System Architecture

### Core Components

1. **CustomFormationData** (`Assets/Scripts/Units/Formation/CustomFormationData.cs`)
   - Stores formation positions as normalized coordinates (-1 to 1)
   - Supports serialization for saving/loading
   - Includes metadata (name, creation date, etc.)

2. **CustomFormationManager** (`Assets/Scripts/Units/Formation/CustomFormationManager.cs`)
   - Singleton manager for all custom formations
   - Handles save/load operations
   - Provides CRUD operations
   - Events for UI updates

3. **FormationBuilderUI** (`Assets/Scripts/UI/FormationBuilderUI.cs`)
   - Main UI controller for the builder panel
   - Handles piece placement, dragging, and deletion
   - Grid-based editing interface
   - Save/load formation editing

4. **FormationPiece** (`Assets/Scripts/UI/FormationPiece.cs`)
   - Draggable piece component
   - Handles left-click selection and dragging
   - Visual feedback for selection state

5. **FormationSelectorUI** (`Assets/Scripts/UI/FormationSelectorUI.cs`)
   - Browse all saved formations
   - Select formations to use
   - Edit, duplicate, or delete existing formations

## Setup Instructions

### 1. Create the Formation Builder UI

In your Unity scene, create the following UI hierarchy:

```
Canvas
├── FormationBuilderPanel (GameObject)
│   ├── GridPanel (RectTransform) - The formation grid
│   ├── GarbageBin (RectTransform) - Drag-to-delete area
│   ├── NameInputField (TMP_InputField) - Formation name
│   ├── PieceCountText (TextMeshProUGUI) - Shows piece count
│   ├── SaveButton (Button)
│   ├── DeleteButton (Button)
│   └── CloseButton (Button)
└── FormationSelectorPanel (GameObject)
    ├── FormationListContainer (RectTransform with Vertical Layout Group)
    ├── CreateNewButton (Button)
    └── CloseButton (Button)
```

### 2. Create the Formation Piece Prefab

1. Create a new GameObject in your scene
2. Add an `Image` component (this will be the visual representation)
3. Add the `FormationPiece` component
4. Configure the visual settings (colors for normal, selected, dragging states)
5. Save as a prefab in your project
6. Delete from scene

### 3. Assign Components

#### FormationBuilderUI
- Assign all UI references in the Inspector
- Set the piece prefab
- Configure grid size (default 400x400)
- Set max pieces (default 50)

#### FormationSelectorUI
- Assign the selector panel
- Assign the formation list container
- Assign buttons
- Link to FormationBuilderUI
- Link to FormationGroupManager

#### UnitDetailsUI
- Assign the new formation buttons:
  - `customFormationButton` - Opens formation selector
  - `createFormationButton` - Opens formation builder
- Link FormationSelectorUI and FormationBuilderUI

### 4. Add CustomFormationManager to Scene

The CustomFormationManager will auto-create itself as a singleton, but you can also manually add it:

1. Create an empty GameObject in your scene
2. Add the `CustomFormationManager` component
3. It will persist between scenes (DontDestroyOnLoad)

## Usage Guide

### For Players

#### Creating a Custom Formation

1. **Open the Builder**
   - Click the "Create Formation" button in the UI
   - Or access from the main menu

2. **Place Pieces**
   - **Right-click** on the grid to place a unit piece
   - Pieces represent unit positions in the formation

3. **Move Pieces**
   - **Left-click and drag** to reposition pieces
   - Drag outside the grid to snap back to original position

4. **Delete Pieces**
   - **Method 1**: Drag piece to the garbage bin
   - **Method 2**: Left-click to select, then press `Delete` key

5. **Name Your Formation**
   - Enter a name in the input field
   - Names must be unique

6. **Save**
   - Click "Save" to save your formation
   - Formations are automatically saved to disk

#### Using a Custom Formation

1. **Open Formation Selector**
   - Click "Custom Formations" button
   - Browse your saved formations

2. **Select a Formation**
   - Click on a formation to apply it to selected units
   - Units will immediately reshape into the custom formation

3. **Edit or Delete**
   - Click "Edit" to modify an existing formation
   - Click "Delete" to remove a formation
   - Click "Duplicate" to create a copy

### For Developers

#### Accessing Custom Formations in Code

```csharp
// Get the manager instance
CustomFormationManager manager = CustomFormationManager.Instance;

// Create a new formation
CustomFormationData formation = manager.CreateFormation("My Formation");

// Add positions (normalized coordinates -1 to 1)
formation.AddPosition(new Vector2(0, 0));    // Center
formation.AddPosition(new Vector2(-0.5f, 0.5f)); // Top-left
formation.AddPosition(new Vector2(0.5f, 0.5f));  // Top-right

// Save the formation
manager.UpdateFormation(formation);

// Apply to units via FormationGroupManager
FormationGroupManager groupManager = FindObjectOfType<FormationGroupManager>();
groupManager.SetCustomFormation(formation.id);
```

#### Calculating World Positions

```csharp
// Get formation positions for units
CustomFormationData formation = manager.GetFormation(formationId);
List<Vector3> positions = FormationManager.CalculateCustomFormationPositions(
    centerPosition,
    unitCount,
    formation,
    spacing: 2.5f,
    facingDirection: Vector3.forward
);
```

#### Events

Subscribe to formation changes:

```csharp
CustomFormationManager.Instance.OnFormationsChanged += (formations) =>
{
    Debug.Log($"Formations updated: {formations.Count} formations");
};

CustomFormationManager.Instance.OnFormationAdded += (formation) =>
{
    Debug.Log($"New formation added: {formation.name}");
};
```

## Data Storage

### File Location

Custom formations are saved to:
```
{Application.persistentDataPath}/CustomFormations.json
```

**Platform-specific paths:**
- **Windows**: `%USERPROFILE%\AppData\LocalLow\{CompanyName}\{ProductName}\CustomFormations.json`
- **Mac**: `~/Library/Application Support/{CompanyName}/{ProductName}/CustomFormations.json`
- **Linux**: `~/.config/unity3d/{CompanyName}/{ProductName}/CustomFormations.json`

### Data Format

Formations are saved as JSON:

```json
{
  "formations": [
    {
      "id": "unique-guid",
      "name": "V-Formation",
      "positions": [
        {"position": {"x": 0.0, "y": 0.0}},
        {"position": {"x": -0.5, "y": -0.5}},
        {"position": {"x": 0.5, "y": -0.5}}
      ],
      "createdDate": "2025-12-05T...",
      "modifiedDate": "2025-12-05T..."
    }
  ]
}
```

### Import/Export

```csharp
// Export formations
CustomFormationManager.Instance.ExportFormations("path/to/file.json");

// Import formations (merge with existing)
CustomFormationManager.Instance.ImportFormations("path/to/file.json", merge: true);
```

## Keyboard Shortcuts

- **Right-click**: Place new piece on grid
- **Left-click**: Select piece or drag piece
- **Delete**: Delete selected piece
- **Esc**: Deselect piece (future feature)

## Configuration

### Grid Settings

In `FormationBuilderUI`:
- `gridSize`: Size of the formation grid (default: 400x400)
- `pieceSize`: Size of each piece (default: 30)
- `maxPieces`: Maximum pieces per formation (default: 50)
- `gridLines`: Number of grid lines for visual aid (default: 8)

### Formation Spacing

Formations scale based on the spacing multiplier passed to `CalculateCustomFormationPositions()`. The default spacing from `FormationSettingsSO` is used.

## Integration with Existing System

The custom formation system seamlessly integrates with the existing formation system:

1. **FormationType.None** is used to indicate a custom formation is active
2. **FormationGroupManager** handles both preset and custom formations
3. **FormationManager** provides calculation methods for both types
4. **RTSCommandHandler** automatically uses the active formation (preset or custom)

## Troubleshooting

### Formations Not Saving
- Check console for errors
- Verify write permissions to `Application.persistentDataPath`
- Check that `CustomFormationManager` exists in scene

### Pieces Not Dragging
- Ensure `FormationPiece` component is attached
- Check that Canvas has a GraphicRaycaster
- Verify EventSystem exists in scene

### Formations Not Applying to Units
- Check that `FormationGroupManager` is assigned
- Verify units have `UnitAIController` and `UnitMovement` components
- Check console for formation calculation errors

## Future Enhancements

Potential improvements:
- Grid snapping options
- Symmetry tools (mirror, rotate)
- Formation templates/presets
- Visual preview of formation in selector
- Formation tags/categories
- Cloud save support
- Formation sharing between players

## Credits

Created for KingdomsAtDuskU_6.3 RTS Game
Formation system integration with existing RTS framework

---

**For questions or issues, please check the Unity console for detailed error messages and stack traces.**
