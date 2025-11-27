# Save/Load System Guide

## Overview

The save/load system for **Kingdoms at Dusk** provides comprehensive game state persistence, including:
- **Buildings**: Position, construction status, resource generation timers
- **Units**: Position, health, AI state, targets, movement state
- **Resources**: Wood, Food, Gold, Stone
- **Game State**: Current game state, pause status, time scale
- **Optional Systems**: Population, Reputation, Happiness
- **Fog of War**: Explored, unexplored, and visible areas
- **Camera State**: Position and settings

## Architecture

### Core Components

1. **SaveLoadManager** (`Assets/Scripts/SaveLoad/SaveLoadManager.cs`)
   - Main orchestrator for save/load operations
   - Implements `ISaveLoadService`
   - Handles serialization/deserialization
   - Manages file I/O

2. **AutoSaveSystem** (`Assets/Scripts/SaveLoad/AutoSaveSystem.cs`)
   - Handles automatic saving at intervals
   - Manages auto-save file rotation
   - Save-on-quit functionality

3. **SaveLoadInputHandler** (`Assets/Scripts/SaveLoad/SaveLoadInputHandler.cs`)
   - Keyboard input handling
   - F5: Quick Save
   - F9: Quick Load
   - F10/ESC: Toggle Save/Load Menu

4. **SaveLoadMenu** (`Assets/Scripts/SaveLoad/SaveLoadMenu.cs`)
   - In-game UI for save/load operations
   - Displays save file list
   - Allows manual saves with custom names
   - Load and delete operations

5. **SaveLoadSettings** (`Assets/Scripts/SaveLoad/SaveLoadSettings.cs`)
   - ScriptableObject for configuration
   - Auto-save intervals
   - Max save file counts
   - Compression/encryption settings

6. **SaveData** (`Assets/Scripts/SaveLoad/SaveData.cs`)
   - Serializable data structures
   - Contains all game state data classes

## Setup Instructions

### 1. Create SaveLoadSettings Asset

1. In Unity, right-click in Project window
2. Create > RTS > Save Load Settings
3. Name it "SaveLoadSettings"
4. Configure settings:
   - **Auto-Save Interval**: 300 seconds (5 minutes) default
   - **Max Auto-Saves**: 3 default
   - **Max Manual Saves**: 0 (unlimited) default
   - **Auto-Save On Quit**: True/False

### 2. Add Components to GameManager

1. Select the GameManager GameObject in your scene
2. Drag SaveLoadManager prefab/script onto it (or add component)
3. Assign SaveLoadManager to the GameManager's "Save Load Manager" field
4. Assign the SaveLoadSettings asset to SaveLoadManager

### 3. Add AutoSaveSystem to Scene

1. Create a new GameObject called "AutoSaveSystem"
2. Add AutoSaveSystem component
3. Assign SaveLoadSettings asset

### 4. Create Save/Load Menu UI

**Option A: Use provided prefab (if created)**
- Drag SaveLoadMenuPrefab into scene

**Option B: Create from scratch**

1. Create Canvas if not exists
2. Create Panel (Menu Panel):
   - Add CanvasGroup (optional, for fading)
   - Set anchors/position as desired

3. Add UI elements:
   - **Save Name Input**: TMP_InputField
   - **Save Button**: Button
   - **Load Button**: Button
   - **Delete Button**: Button
   - **Close Button**: Button
   - **Save List Scroll View**: ScrollRect with Content transform
   - **Save List Item Prefab**: Create a prefab with:
     - Background Image
     - Save Name Text (TextMeshProUGUI)
     - Save Date Text (TextMeshProUGUI)
     - Play Time Text (TextMeshProUGUI)
     - Select Button

4. Add SaveLoadMenu component to root panel
5. Assign all UI references in inspector

### 5. Add SaveLoadInputHandler

1. Create GameObject "SaveLoadInputHandler" (or add to GameManager)
2. Add SaveLoadInputHandler component
3. Assign SaveLoadMenu reference
4. Configure key bindings (F5, F9, F10 default)

### 6. Initialize in GameManager

The system is automatically registered in `GameManager.InitializeSaveLoadManager()`:

```csharp
private void InitializeSaveLoadManager()
{
    if (saveLoadManager == null)
    {
        saveLoadManager = FindAnyObjectByType<RTS.SaveLoad.SaveLoadManager>();
        if (saveLoadManager == null)
        {
            Debug.LogWarning("SaveLoadManager not found!");
            return;
        }
    }

    ServiceLocator.Register<ISaveLoadService>(saveLoadManager);
}
```

## Usage

### Keyboard Shortcuts

- **F5**: Quick Save to dedicated slot
- **F9**: Quick Load from quick save slot
- **F10** or **ESC**: Toggle Save/Load Menu

### Programmatic Usage

```csharp
using RTS.Core.Services;
using RTS.SaveLoad;

// Get service
var saveLoadService = ServiceLocator.TryGet<ISaveLoadService>();

// Manual save
bool success = saveLoadService.SaveGame("MySaveName");

// Load game
bool loaded = saveLoadService.LoadGame("MySaveName");

// Quick save
saveLoadService.QuickSave();

// Quick load
saveLoadService.QuickLoad();

// Get all saves
string[] saves = saveLoadService.GetAllSaves();

// Get save info
SaveFileInfo info = saveLoadService.GetSaveInfo("MySaveName");

// Delete save
saveLoadService.DeleteSave("MySaveName");
```

### Auto-Save

Auto-save is configured via SaveLoadSettings:

- **Enable Auto-Save**: Toggle on/off
- **Auto-Save Interval**: Time between auto-saves (default 300s)
- **Max Auto-Saves**: Number of auto-save files to keep (default 3)
- **Auto-Save On Quit**: Save when application quits

Auto-saves are named: `AutoSave_00`, `AutoSave_01`, `AutoSave_02`, etc.

Oldest auto-saves are automatically deleted when limit is reached.

## Saved Data Structure

### Game State
- Current game state (Playing, Paused, etc.)
- Time scale
- Play time

### Resources
- Wood, Food, Gold, Stone amounts

### Buildings
For each building:
- Instance ID (for cross-referencing)
- Building type (BuildingDataSO name)
- Position, rotation, scale
- Construction status and progress
- Layer, tag
- Team/ownership

### Units
For each unit:
- Instance ID
- Unit type (UnitConfigSO name)
- Position, rotation, scale
- Current health / max health
- Movement state (destination, speed, is moving)
- Combat state (target, damage, range, rate)
- AI state (current state, behavior type, aggro origin)
- Layer, tag
- Team/ownership

### Fog of War
- Grid dimensions
- Cell size
- Vision state for each cell (Unexplored, Explored, Visible)

### Camera
- Position, rotation
- Field of view / orthographic size

## File Structure

Save files are stored in:
```
{Application.persistentDataPath}/Saves/
```

Default file format:
```
SaveName.sav
```

Files are stored as JSON (optionally compressed/encrypted).

## Events

The system publishes events via EventBus:

```csharp
// When game is saved
EventBus.Publish(new GameSavedEvent(saveName, isAutoSave, isQuickSave));

// When game is loaded
EventBus.Publish(new GameLoadedEvent(saveName));
```

Subscribe in other systems:

```csharp
private void OnEnable()
{
    EventBus.Subscribe<GameSavedEvent>(OnGameSaved);
    EventBus.Subscribe<GameLoadedEvent>(OnGameLoaded);
}

private void OnDisable()
{
    EventBus.Unsubscribe<GameSavedEvent>(OnGameSaved);
    EventBus.Unsubscribe<GameLoadedEvent>(OnGameLoaded);
}

private void OnGameSaved(GameSavedEvent evt)
{
    Debug.Log($"Game saved: {evt.SaveName}");
}

private void OnGameLoaded(GameLoadedEvent evt)
{
    Debug.Log($"Game loaded: {evt.SaveName}");
}
```

## Advanced Configuration

### Compression

Enable in SaveLoadSettings:
```csharp
useCompression = true;
```

Currently uses simple compression (can be upgraded to GZip).

### Encryption

Enable in SaveLoadSettings:
```csharp
useEncryption = true;
```

Currently uses basic obfuscation (can be upgraded to AES).

### Custom Save Location

Modify SaveLoadSettings:
```csharp
saveDirectory = "CustomFolder";
```

### Notifications

Configure in SaveLoadSettings:
```csharp
showSaveNotifications = true;
notificationDuration = 2f;
```

## Extending the System

### Adding New Saveable Data

1. Add data class to `SaveData.cs`:
```csharp
[System.Serializable]
public class CustomSystemData
{
    public int customValue;
    public string customString;
}
```

2. Add field to `GameSaveData`:
```csharp
public CustomSystemData customSystem;
```

3. Collect data in `SaveLoadManager.CollectGameState()`:
```csharp
private CustomSystemData CollectCustomSystemData()
{
    // Collect your data here
    return new CustomSystemData { ... };
}
```

4. Restore data in `SaveLoadManager.RestoreGameState()`:
```csharp
private void RestoreCustomSystemData(CustomSystemData data)
{
    // Restore your data here
}
```

### Custom Serialization

Override serialization in SaveLoadManager:

```csharp
private string SerializeSaveData(GameSaveData data)
{
    // Use custom serializer (e.g., BinaryFormatter, Newtonsoft.Json, etc.)
    return customSerializer.Serialize(data);
}

private GameSaveData DeserializeSaveData(string json)
{
    return customSerializer.Deserialize<GameSaveData>(json);
}
```

## Fog of War Integration

**Note**: Full fog of war serialization requires access to `FogOfWarManager.grid` field.

**Option 1: Make grid public**
```csharp
public class FogOfWarManager : MonoBehaviour
{
    public FogOfWarGrid grid { get; private set; } // Expose for save/load
}
```

**Option 2: Add save/load methods to FogOfWarManager**
```csharp
public FogOfWarData SaveState()
{
    return new FogOfWarData(grid);
}

public void LoadState(FogOfWarData data)
{
    // Restore grid state
}
```

Then use in SaveLoadManager:
```csharp
private FogOfWarData CollectFogOfWarData()
{
    var fowManager = FindAnyObjectByType<FogOfWarManager>();
    return fowManager?.SaveState();
}
```

## Troubleshooting

### Save file not found
- Check persistentDataPath: `Debug.Log(Application.persistentDataPath);`
- Verify save directory exists
- Check file permissions

### Load fails with "Service not available"
- Ensure SaveLoadManager is registered in GameManager
- Check initialization order in GameManager.InitializeServices()

### Entities not loading correctly
- Verify BuildingDataSO/UnitConfigSO names match saved data
- Ensure prefabs are in Resources folder or use AssetDatabase path
- Check instance ID cross-referencing for targets

### Auto-save not working
- Verify AutoSaveSystem component exists in scene
- Check SaveLoadSettings.enableAutoSave is true
- Ensure game is not paused (auto-save skips paused state)

### Menu not opening
- Check SaveLoadMenu GameObject is in scene
- Verify UI references are assigned in inspector
- Check SaveLoadInputHandler has menu reference

## Performance Considerations

### Save Performance
- Saving is synchronous and may cause frame drop
- Consider async save in Update() over multiple frames
- Compress large save files for faster I/O

### Load Performance
- Loading clears and recreates all entities
- May take 1-3 seconds for large saves
- Show loading screen during load operations

### Memory Usage
- Save files are loaded entirely into memory
- Large saves (1000+ entities) may use 10-50MB
- Consider streaming for very large worlds

## Future Enhancements

- [ ] Async save/load operations
- [ ] Save game thumbnails/screenshots
- [ ] Cloud save support
- [ ] Save file versioning/migration
- [ ] Incremental saves (delta compression)
- [ ] Save integrity validation (checksums)
- [ ] Multiplayer save synchronization
- [ ] Save file export/import
- [ ] Debug save viewer tool

## API Reference

### ISaveLoadService

```csharp
bool SaveGame(string saveName, bool isAutoSave = false, bool isQuickSave = false)
bool LoadGame(string saveName)
bool QuickSave()
bool QuickLoad()
bool DeleteSave(string saveName)
string[] GetAllSaves()
SaveFileInfo GetSaveInfo(string saveName)
bool SaveExists(string saveName)
```

### SaveFileInfo

```csharp
public string fileName;
public string saveName;
public string saveDate;
public float playTime;
public string gameVersion;
public long fileSize;
public bool isAutoSave;
public bool isQuickSave;
```

## Examples

### Custom Save Button

```csharp
using RTS.Core.Services;
using UnityEngine;
using UnityEngine.UI;

public class CustomSaveButton : MonoBehaviour
{
    [SerializeField] private Button saveButton;

    private void Start()
    {
        saveButton.onClick.AddListener(OnSaveClicked);
    }

    private void OnSaveClicked()
    {
        var saveService = ServiceLocator.TryGet<ISaveLoadService>();
        if (saveService != null)
        {
            string saveName = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            saveService.SaveGame(saveName);
        }
    }
}
```

### Save Progress Tracker

```csharp
using RTS.Core.Events;
using UnityEngine;

public class SaveProgressTracker : MonoBehaviour
{
    private void OnEnable()
    {
        EventBus.Subscribe<GameSavedEvent>(OnGameSaved);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<GameSavedEvent>(OnGameSaved);
    }

    private void OnGameSaved(GameSavedEvent evt)
    {
        if (evt.IsAutoSave)
        {
            Debug.Log("Auto-save completed");
        }
        else if (evt.IsQuickSave)
        {
            Debug.Log("Quick save completed (F5)");
        }
        else
        {
            Debug.Log($"Manual save completed: {evt.SaveName}");
        }
    }
}
```

---

**Last Updated**: 2025-11-27
**Version**: 1.0
**Author**: Save/Load System for Kingdoms at Dusk
