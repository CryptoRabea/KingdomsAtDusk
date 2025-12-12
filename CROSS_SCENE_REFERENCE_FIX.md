# Cross-Scene Reference Issue and Solution

## Problem

Unity displays this warning:
```
Cross scene references are not supported: 'SaveLoadMenuSystem' in scene GameScene has an invalid reference to 'SettingsManager' in scene DontDestroyOnLoad.
```

## Why This Happens

Unity cannot serialize references between objects in different scenes. When you have:
- An object in **GameScene** (e.g., `SaveLoadMenuSystem`)
- Trying to reference an object marked with **DontDestroyOnLoad** (e.g., `RTSSettingsManager`)

Unity will:
1. Show a warning in the console
2. Not save the reference when the scene is saved
3. The reference will be null at runtime

## How to Fix

### Option 1: Use ServiceLocator Pattern (Recommended)

Instead of using a serialized field reference, access services through the ServiceLocator at runtime:

```csharp
// ❌ BAD: Direct serialized reference
[SerializeField] private RTSSettingsManager settingsManager;

// ✅ GOOD: Access via ServiceLocator
private void Start()
{
    var settingsService = ServiceLocator.TryGet<ISettingsService>();
    if (settingsService != null)
    {
        // Use the service
    }
}
```

### Option 2: Use Singleton Pattern

If the object is a singleton, access it via its Instance property:

```csharp
// ❌ BAD: Direct serialized reference
[SerializeField] private CustomFormationManager formationManager;

// ✅ GOOD: Access via singleton
var manager = CustomFormationManager.Instance;
if (manager != null)
{
    // Use the manager
}
```

### Option 3: Find at Runtime

Use `FindAnyObjectByType` or `FindFirstObjectByType` at runtime:

```csharp
// ❌ BAD: Direct serialized reference
[SerializeField] private RTSSettingsManager settingsManager;

// ✅ GOOD: Find at runtime
private void Start()
{
    var settingsManager = FindAnyObjectByType<RTSSettingsManager>();
    if (settingsManager != null)
    {
        // Use the manager
    }
}
```

## Fix for SaveLoadMenuSystem

If you see this warning for SaveLoadMenuSystem:

1. **Open the GameScene in Unity Editor**
2. **Select the SaveLoadMenuSystem GameObject**
3. **In the Inspector**, look for any field that references an object from DontDestroyOnLoad
4. **Clear that reference** (set it to None)
5. **Save the scene**

The code should already be using ServiceLocator or runtime finding instead of serialized references, so clearing the reference won't break functionality.

## Verification

After fixing:
1. Open the GameScene
2. Press Play in Unity
3. Check the Console - the warning should be gone
4. Verify functionality still works (settings, formations, etc.)

## Related Files

- `Assets/Scripts/SaveLoad/SaveLoadMenu.cs` - Uses ServiceLocator for settings
- `Assets/Scripts/UI/Settings/SettingsPanel.cs` - Uses ServiceLocator for ISettingsService
- `Assets/Scripts/UI/UnitDetailsUI.cs` - Uses singleton for CustomFormationManager

## Prevention

To prevent this issue in the future:

1. **Never** drag DontDestroyOnLoad objects into scene object fields
2. **Always** use ServiceLocator, Singleton, or runtime finding for cross-scene access
3. **Mark** fields as `[NonSerialized]` or make them private without `[SerializeField]` if they should be found at runtime
