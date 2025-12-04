# Ownership Detection & Drag-to-Move Guide

This guide explains the new features added to the minimap system:
1. **Flexible Ownership Detection** - Multiple ways to detect friendly/enemy entities
2. **Drag-to-Move** - Drag on minimap to move the camera

## Part 1: Ownership Detection

### Problem with Old System

The original system only used **Unity Layers** to detect enemy units:
```csharp
bool isEnemy = unit.layer == LayerMask.NameToLayer("Enemy");
```

**Limitations:**
- ❌ Only supports 2 factions (friendly/enemy)
- ❌ Requires specific layer setup
- ❌ Can't handle neutral units
- ❌ No multiplayer support

### New Detection System

The new system supports **4 detection methods** with automatic fallback:

#### Method 1: Component-Based (Most Flexible) ⭐ RECOMMENDED

Attach `MinimapEntity` component to units/buildings:

```csharp
using RTS.UI.Minimap;

public class Unit : MonoBehaviour
{
    void Start()
    {
        // Add minimap entity component
        var minimapEntity = gameObject.AddComponent<MinimapEntity>();
        minimapEntity.SetOwnership(MinimapEntityOwnership.Friendly);
    }
}
```

**Supports:**
- ✅ Friendly
- ✅ Enemy
- ✅ Neutral
- ✅ Ally
- ✅ Player1, Player2, Player3, Player4 (multiplayer)

**Setup:**
1. Add `MinimapEntity` component to unit prefab
2. Set ownership in Inspector
3. Done! Minimap will automatically detect it

**Example in Inspector:**
```
GameObject: Soldier
└─ MinimapEntity
    ├─ Ownership: Friendly
    ├─ Auto Detect Ownership: false
    └─ Player Id: 0
```

#### Method 2: Tag-Based (Simple)

Use Unity tags:

**Setup:**
1. Create tags: "Friendly", "Enemy", "Neutral", "Ally"
2. Tag your units/buildings
3. Set detection method to "Tag" in MiniMapControllerPro

**Example:**
```csharp
// In unit spawner
GameObject unit = Instantiate(unitPrefab);
unit.tag = "Enemy"; // Minimap will show red marker
```

#### Method 3: Layer-Based (Original Method)

Use Unity layers (backward compatible):

**Setup:**
1. Create layer "Enemy"
2. Set enemy units to Enemy layer
3. Set detection method to "Layer" in MiniMapControllerPro

**Example:**
```csharp
GameObject unit = Instantiate(unitPrefab);
unit.layer = LayerMask.NameToLayer("Enemy"); // Red marker
```

#### Method 4: Auto (Default) ⭐ BEST FOR MOST PROJECTS

Tries all methods automatically:
1. First checks for `MinimapEntity` component
2. If not found, checks tags
3. If no tags, uses layers

**Setup:**
1. Set detection method to "Auto" in MiniMapControllerPro
2. Use any of the above methods on your units
3. System automatically picks the right one!

### Configuring Detection Method

In MiniMapControllerPro Inspector:

```
MiniMapControllerPro
├─ Entity Detection
    └─ Detection Method: Auto  ← Change this
```

**Options:**
- **Auto** - Try all methods (recommended)
- **Component** - Only check MinimapEntity component
- **Tag** - Only check Unity tags
- **Layer** - Only check Unity layers

### Example: Setting Up Units

#### Example 1: Using MinimapEntity Component

```csharp
using UnityEngine;
using RTS.UI.Minimap;

public class UnitSpawner : MonoBehaviour
{
    public GameObject unitPrefab;
    public bool spawnEnemies = false;

    void SpawnUnit()
    {
        GameObject unit = Instantiate(unitPrefab);

        // Add minimap entity
        MinimapEntity entity = unit.AddComponent<MinimapEntity>();

        if (spawnEnemies)
            entity.SetOwnership(MinimapEntityOwnership.Enemy);
        else
            entity.SetOwnership(MinimapEntityOwnership.Friendly);
    }
}
```

#### Example 2: Using Tags

```csharp
void SpawnUnit()
{
    GameObject unit = Instantiate(unitPrefab);
    unit.tag = spawnEnemies ? "Enemy" : "Friendly";
}
```

#### Example 3: Multiplayer with Player IDs

```csharp
public class MultiplayerUnit : MonoBehaviour
{
    public int ownerId; // Set this from network

    void Start()
    {
        MinimapEntity entity = gameObject.AddComponent<MinimapEntity>();
        entity.SetPlayerId(ownerId);

        // Color will be:
        // Player 0 = Friendly (green)
        // Player 1 = Player1 (blue)
        // Player 2 = Player2 (red)
        // etc.
    }
}
```

### Example: Neutral Units

```csharp
public class WildAnimal : MonoBehaviour
{
    void Start()
    {
        MinimapEntity entity = gameObject.AddComponent<MinimapEntity>();
        entity.SetOwnership(MinimapEntityOwnership.Neutral);
        // Will show as gray marker
    }
}
```

## Part 2: Drag-to-Move Camera

### Overview

Now you can **drag on the minimap** to move the camera, not just click!

### Setup Drag-to-Move

#### Option 1: Simple Setup (Uses MiniMapControllerPro)

The existing click functionality works. To add drag support:

1. Select your MiniMap GameObject
2. Add Component: `MinimapDragHandler`
3. Configure in Inspector:

```
MinimapDragHandler
├─ References
│   ├─ Minimap Controller: Auto-detected
│   └─ Minimap Rect: Auto-detected
├─ Drag Settings
│   ├─ Enable Drag: ✓
│   ├─ Drag Threshold: 5 (prevents accidental drags)
│   ├─ Continuous Drag: ✓ (move while dragging)
│   ├─ Show Drag Feedback: ✓
│   └─ Drag Cursor: (optional custom cursor)
```

#### Option 2: Advanced Setup (Viewport Indicator Only)

If you only want to drag the viewport indicator:

1. Select the `ViewportIndicator` GameObject
2. Add Component: `MinimapDragHandler`
3. Enable drag

**Difference:**
- On MiniMap root: Drag anywhere to move camera
- On ViewportIndicator: Only drag the indicator itself

### Drag Settings Explained

**Enable Drag**
- ✓ Enabled: Can drag to move camera
- ☐ Disabled: Only click to move (original behavior)

**Drag Threshold** (pixels)
- Distance mouse must move before drag starts
- Prevents accidental camera movement
- **5 pixels** = Good default
- **10 pixels** = Requires more deliberate drag
- **1 pixel** = Very sensitive

**Continuous Drag**
- ✓ Enabled: Camera moves smoothly while dragging
- ☐ Disabled: Camera moves only when drag ends

**Show Drag Feedback**
- ✓ Enabled: Visual feedback while dragging
- Future: Could show highlight or cursor change

**Drag Cursor**
- Optional: Custom cursor texture during drag
- Leave empty for default cursor

### Usage Examples

#### Example 1: Enable/Disable Drag at Runtime

```csharp
using UnityEngine;
using RTS.UI.Minimap;

public class MinimapControls : MonoBehaviour
{
    public MinimapDragHandler dragHandler;

    void Update()
    {
        // Hold Shift to enable drag
        if (Input.GetKey(KeyCode.LeftShift))
            dragHandler.SetDragEnabled(true);
        else
            dragHandler.SetDragEnabled(false);
    }
}
```

#### Example 2: Check if User is Dragging

```csharp
public class GameController : MonoBehaviour
{
    public MinimapDragHandler dragHandler;

    void Update()
    {
        if (dragHandler.IsDragging())
        {
            // Disable other input while dragging minimap
            DisableUnitSelection();
        }
    }
}
```

#### Example 3: Custom Drag Behavior

You can access the drag handler to customize behavior:

```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using RTS.UI.Minimap;

public class CustomMinimapDrag : MonoBehaviour, IDragHandler
{
    public MiniMapControllerPro minimap;

    public void OnDrag(PointerEventData eventData)
    {
        // Custom drag logic
        Vector3 worldPos = minimap.ScreenToWorldPosition(
            eventData.position,
            eventData.pressEventCamera
        );

        if (worldPos != Vector3.zero)
        {
            // Only move camera if shift is held
            if (Input.GetKey(KeyCode.LeftShift))
            {
                minimap.MoveCameraTo(worldPos);
            }
        }
    }
}
```

## Combining Both Features

### Example: Full Setup

1. **Add MinimapEntity to all units:**
```csharp
// In your unit prefab
public class Unit : MonoBehaviour
{
    public bool isEnemy = false;

    void Start()
    {
        var entity = gameObject.AddComponent<MinimapEntity>();
        entity.SetOwnership(isEnemy ?
            MinimapEntityOwnership.Enemy :
            MinimapEntityOwnership.Friendly);
    }
}
```

2. **Setup minimap:**
```
MiniMapControllerPro
├─ Entity Detection
│   └─ Detection Method: Auto
```

3. **Add drag support:**
```
MiniMap GameObject
└─ MinimapDragHandler
    ├─ Enable Drag: ✓
    └─ Continuous Drag: ✓
```

## Migration Guide

### From Old System (Layer-based)

**Before:**
```csharp
unit.layer = LayerMask.NameToLayer("Enemy");
```

**After (Backward Compatible):**
```csharp
// Still works! No changes needed
unit.layer = LayerMask.NameToLayer("Enemy");

// Or upgrade to component-based:
unit.AddComponent<MinimapEntity>().SetOwnership(MinimapEntityOwnership.Enemy);
```

### Adding Drag to Existing Minimap

1. Select MiniMap GameObject
2. Add Component → MinimapDragHandler
3. Done! Drag now works

## Troubleshooting

### Units showing wrong color

**Check:**
1. Detection method in MiniMapControllerPro
2. Unit has correct layer/tag/component
3. Use context menu "Detect Ownership" on MinimapEntity

**Debug:**
```csharp
var entity = unit.GetComponent<MinimapEntity>();
if (entity != null)
{
    Debug.Log($"Unit ownership: {entity.GetOwnership()}");
}
```

### Drag not working

**Check:**
1. MinimapDragHandler is attached
2. "Enable Drag" is checked
3. EventSystem exists in scene
4. MiniMap is not blocked by other UI

**Debug:**
```csharp
dragHandler.SetDragEnabled(true);
Debug.Log($"Is dragging: {dragHandler.IsDragging()}");
```

### Neutral units not showing

**Solution:**
Neutral units use gray color by default. Add to MinimapConfig:
```csharp
// In MinimapEntityDetector.GetColorForOwnership
case MinimapEntityOwnership.Neutral:
    return Color.gray; // Or customize this
```

## Best Practices

### For Single Player Games
- Use **Component** or **Tag** detection
- Simple: Friendly vs Enemy

### For Multiplayer Games
- Use **Component** with Player IDs
- Support 4+ players with different colors

### For Large Battles
- Use **Layer** detection (fastest)
- Pre-set layers on prefabs

### For Strategy Games with Diplomacy
- Use **Component** detection
- Support Friendly, Enemy, Neutral, Ally

## Summary

### Ownership Detection
- ✅ 4 detection methods (Component, Tag, Layer, Auto)
- ✅ Supports 8+ factions/teams
- ✅ Backward compatible
- ✅ Multiplayer ready

### Drag-to-Move
- ✅ Drag anywhere on minimap
- ✅ Configurable threshold
- ✅ Continuous or end-of-drag movement
- ✅ Easy to enable/disable

## Next Steps

1. Choose detection method for your game
2. Add MinimapEntity to unit prefabs (if using component method)
3. Add MinimapDragHandler to minimap
4. Test and enjoy! 🎮
