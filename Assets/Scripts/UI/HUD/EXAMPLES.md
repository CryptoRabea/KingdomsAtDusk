# HUD Framework Usage Examples

Real-world code examples for common HUD scenarios.

## Example 1: Basic Game Setup

```csharp
using UnityEngine;
using RTS.UI.HUD;

public class GameManager : MonoBehaviour
{
    void Start()
    {
        // Apply Warcraft 3 style HUD at game start
        HUDController.Instance.ApplyWarcraft3Style();
    }
}
```

## Example 2: Settings Menu Integration

```csharp
using UnityEngine;
using UnityEngine.UI;
using RTS.UI.HUD;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private Toggle minimapToggle;
    [SerializeField] private Toggle inventoryToggle;
    [SerializeField] private Dropdown layoutDropdown;

    void Start()
    {
        // Setup toggles
        minimapToggle.onValueChanged.AddListener(OnMinimapToggled);
        inventoryToggle.onValueChanged.AddListener(OnInventoryToggled);
        layoutDropdown.onValueChanged.AddListener(OnLayoutChanged);
    }

    void OnMinimapToggled(bool enabled)
    {
        HUDController.Instance.SetComponentVisible("minimap", enabled);
    }

    void OnInventoryToggled(bool enabled)
    {
        HUDController.Instance.SetComponentVisible("inventory", enabled);
    }

    void OnLayoutChanged(int index)
    {
        switch (index)
        {
            case 0: HUDController.Instance.ApplyWarcraft3Style(); break;
            case 1: HUDController.Instance.ApplyModernRTSStyle(); break;
            case 2: HUDController.Instance.ApplyAgeOfEmpiresStyle(); break;
            case 3: HUDController.Instance.ApplyMinimalStyle(); break;
        }
    }
}
```

## Example 3: Dynamic Inventory Management

```csharp
using UnityEngine;
using RTS.UI.HUD;

public class UnitInventoryManager : MonoBehaviour
{
    [SerializeField] private Sprite swordIcon;
    [SerializeField] private Sprite potionIcon;
    [SerializeField] private Sprite armorIcon;

    private InventoryData currentInventory;

    void Start()
    {
        currentInventory = new InventoryData();
    }

    public void OnUnitSelected(GameObject unit)
    {
        // Get unit's inventory data
        var unitData = unit.GetComponent<UnitData>();
        if (unitData != null && unitData.hasInventory)
        {
            // Show inventory UI
            HUDController.Instance.SetComponentVisible("inventory", true);

            // Populate inventory
            LoadUnitInventory(unitData);
        }
        else
        {
            // Hide inventory for units without it
            HUDController.Instance.SetComponentVisible("inventory", false);
        }
    }

    void LoadUnitInventory(UnitData unitData)
    {
        currentInventory.Clear();

        foreach (var item in unitData.items)
        {
            ItemData itemData = new ItemData(
                item.name,
                GetItemIcon(item.type),
                item.type
            );
            itemData.stackSize = item.quantity;

            currentInventory.AddItem(itemData);
        }

        // Display in UI
        HUDController.Instance.SetInventory(currentInventory);
    }

    Sprite GetItemIcon(ItemType type)
    {
        switch (type)
        {
            case ItemType.Equipment: return swordIcon;
            case ItemType.Consumable: return potionIcon;
            default: return null;
        }
    }

    public void AddItemToInventory(string itemName, Sprite icon, ItemType type)
    {
        ItemData newItem = new ItemData(itemName, icon, type);
        currentInventory.AddItem(newItem);

        // Update display
        HUDController.Instance.SetInventory(currentInventory);
    }

    public void RemoveItemFromInventory(int slotIndex)
    {
        currentInventory.RemoveItem(slotIndex);

        // Update display
        HUDController.Instance.SetInventory(currentInventory);
    }
}
```

## Example 4: Different HUD for Different Game Modes

```csharp
using UnityEngine;
using RTS.UI.HUD;

public class GameModeManager : MonoBehaviour
{
    public enum GameMode
    {
        Campaign,
        Skirmish,
        Multiplayer,
        Tutorial
    }

    public void StartGameMode(GameMode mode)
    {
        switch (mode)
        {
            case GameMode.Campaign:
                // Full HUD with story elements
                HUDController.Instance.ApplyConfiguration("FullHUDConfig");
                HUDController.Instance.ApplyLayout("Warcraft3Layout");
                HUDController.Instance.SetComponentVisible("inventory", true);
                break;

            case GameMode.Skirmish:
                // Standard HUD
                HUDController.Instance.ApplyConfiguration("DefaultHUDConfig");
                HUDController.Instance.ApplyLayout("ModernRTSLayout");
                break;

            case GameMode.Multiplayer:
                // Minimal HUD for competitive play
                HUDController.Instance.ApplyMinimalStyle();
                HUDController.Instance.SetComponentVisible("notifications", false);
                break;

            case GameMode.Tutorial:
                // Simplified HUD for learning
                HUDController.Instance.ApplyConfiguration("MinimalHUDConfig");
                HUDController.Instance.ApplyLayout("CompactLayout");
                break;
        }
    }
}
```

## Example 5: Context-Sensitive Top Bar

```csharp
using UnityEngine;
using RTS.UI.HUD;

public class TopBarController : MonoBehaviour
{
    void Start()
    {
        // Configure top bar for different contexts
        ConfigureTopBarForGameplay();
    }

    public void ConfigureTopBarForGameplay()
    {
        HUDController.Instance.ConfigureTopBar(
            showResources: true,
            showMenu: true,
            showClock: true,
            showPopulation: true
        );
    }

    public void ConfigureTopBarForCutscene()
    {
        // Hide everything during cutscenes
        HUDController.Instance.ToggleAllUI();
    }

    public void ConfigureTopBarForPause()
    {
        // Show only menu during pause
        HUDController.Instance.ConfigureTopBar(
            showResources: false,
            showMenu: true,
            showClock: false,
            showPopulation: false
        );
    }
}
```

## Example 6: Resolution/Aspect Ratio Adaptation

```csharp
using UnityEngine;
using RTS.UI.HUD;

public class HUDResolutionAdapter : MonoBehaviour
{
    private int lastScreenWidth;
    private int lastScreenHeight;

    void Start()
    {
        AdaptToCurrentResolution();
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
    }

    void Update()
    {
        // Check for resolution changes
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            AdaptToCurrentResolution();
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
        }
    }

    void AdaptToCurrentResolution()
    {
        float aspectRatio = (float)Screen.width / Screen.height;

        if (aspectRatio >= 1.7f) // 16:9 or wider
        {
            HUDController.Instance.ApplyLayout("Warcraft3Layout");
        }
        else if (aspectRatio >= 1.5f) // 16:10
        {
            HUDController.Instance.ApplyLayout("ModernRTSLayout");
        }
        else // 4:3 or narrower
        {
            HUDController.Instance.ApplyLayout("CompactLayout");
        }

        // Also adapt for small screens
        if (Screen.width < 1280)
        {
            HUDController.Instance.ApplyLayout("CompactLayout");
        }
    }
}
```

## Example 7: Hotkey System

```csharp
using UnityEngine;
using RTS.UI.HUD;

public class HUDHotkeys : MonoBehaviour
{
    void Update()
    {
        // F1 - Toggle all UI
        if (Input.GetKeyDown(KeyCode.F1))
        {
            HUDController.Instance.ToggleAllUI();
        }

        // F2 - Toggle minimap
        if (Input.GetKeyDown(KeyCode.F2))
        {
            HUDController.Instance.ToggleMinimap();
        }

        // F3 - Cycle through layouts
        if (Input.GetKeyDown(KeyCode.F3))
        {
            CycleLayout();
        }

        // I - Toggle inventory
        if (Input.GetKeyDown(KeyCode.I))
        {
            HUDController.Instance.ToggleInventory();
        }

        // Ctrl+1/2/3 - Quick presets
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                HUDController.Instance.ApplyWarcraft3Style();
            else if (Input.GetKeyDown(KeyCode.Alpha2))
                HUDController.Instance.ApplyModernRTSStyle();
            else if (Input.GetKeyDown(KeyCode.Alpha3))
                HUDController.Instance.ApplyMinimalStyle();
        }
    }

    private int currentLayoutIndex = 0;
    private string[] layouts = { "Warcraft3Layout", "ModernRTSLayout", "AgeOfEmpiresLayout", "CompactLayout" };

    void CycleLayout()
    {
        currentLayoutIndex = (currentLayoutIndex + 1) % layouts.Length;
        HUDController.Instance.ApplyLayout(layouts[currentLayoutIndex]);
        Debug.Log($"Switched to: {layouts[currentLayoutIndex]}");
    }
}
```

## Example 8: Save/Load HUD Preferences

```csharp
using UnityEngine;
using RTS.UI.HUD;

public class HUDPreferences : MonoBehaviour
{
    private const string PREF_CONFIG = "HUDConfig";
    private const string PREF_LAYOUT = "HUDLayout";
    private const string PREF_MINIMAP = "HUDMinimap";
    private const string PREF_INVENTORY = "HUDInventory";
    private const string PREF_TOPBAR = "HUDTopBar";

    void Start()
    {
        LoadPreferences();
    }

    void OnApplicationQuit()
    {
        SavePreferences();
    }

    public void SavePreferences()
    {
        var framework = FindObjectOfType<MainHUDFramework>();
        if (framework == null) return;

        var config = framework.GetConfiguration();

        // Save configuration name
        PlayerPrefs.SetString(PREF_CONFIG, config.name);

        // Save layout name
        if (config.layoutPreset != null)
        {
            PlayerPrefs.SetString(PREF_LAYOUT, config.layoutPreset.name);
        }

        // Save component states
        PlayerPrefs.SetInt(PREF_MINIMAP, config.enableMinimap ? 1 : 0);
        PlayerPrefs.SetInt(PREF_INVENTORY, config.enableInventory ? 1 : 0);
        PlayerPrefs.SetInt(PREF_TOPBAR, config.enableTopBar ? 1 : 0);

        PlayerPrefs.Save();
        Debug.Log("HUD preferences saved!");
    }

    public void LoadPreferences()
    {
        if (!PlayerPrefs.HasKey(PREF_CONFIG))
        {
            Debug.Log("No saved HUD preferences, using defaults");
            return;
        }

        // Load configuration
        string configName = PlayerPrefs.GetString(PREF_CONFIG);
        HUDController.Instance.ApplyConfiguration(configName);

        // Load layout
        if (PlayerPrefs.HasKey(PREF_LAYOUT))
        {
            string layoutName = PlayerPrefs.GetString(PREF_LAYOUT);
            HUDController.Instance.ApplyLayout(layoutName);
        }

        // Load component states
        if (PlayerPrefs.HasKey(PREF_MINIMAP))
        {
            bool minimap = PlayerPrefs.GetInt(PREF_MINIMAP) == 1;
            HUDController.Instance.SetComponentVisible("minimap", minimap);
        }

        if (PlayerPrefs.HasKey(PREF_INVENTORY))
        {
            bool inventory = PlayerPrefs.GetInt(PREF_INVENTORY) == 1;
            HUDController.Instance.SetComponentVisible("inventory", inventory);
        }

        if (PlayerPrefs.HasKey(PREF_TOPBAR))
        {
            bool topbar = PlayerPrefs.GetInt(PREF_TOPBAR) == 1;
            HUDController.Instance.SetComponentVisible("topbar", topbar);
        }

        Debug.Log("HUD preferences loaded!");
    }

    public void ResetToDefaults()
    {
        PlayerPrefs.DeleteKey(PREF_CONFIG);
        PlayerPrefs.DeleteKey(PREF_LAYOUT);
        PlayerPrefs.DeleteKey(PREF_MINIMAP);
        PlayerPrefs.DeleteKey(PREF_INVENTORY);
        PlayerPrefs.DeleteKey(PREF_TOPBAR);
        PlayerPrefs.Save();

        // Apply default configuration
        HUDController.Instance.ApplyConfiguration("DefaultHUDConfig");
        Debug.Log("HUD preferences reset to defaults!");
    }
}
```

## Example 9: Animated HUD Transitions

```csharp
using UnityEngine;
using System.Collections;
using RTS.UI.HUD;

public class HUDAnimator : MonoBehaviour
{
    public void SwitchToWarcraft3StyleAnimated()
    {
        StartCoroutine(AnimatedStyleSwitch("Warcraft3HUDConfig", "Warcraft3Layout"));
    }

    IEnumerator AnimatedStyleSwitch(string configName, string layoutName)
    {
        var framework = FindObjectOfType<MainHUDFramework>();
        if (framework == null) yield break;

        // Fade out
        CanvasGroup canvasGroup = framework.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = framework.gameObject.AddComponent<CanvasGroup>();
        }

        float fadeTime = 0.3f;
        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - (elapsed / fadeTime);
            yield return null;
        }

        // Switch configuration and layout
        HUDController.Instance.ApplyConfiguration(configName);
        HUDController.Instance.ApplyLayout(layoutName);

        // Small delay
        yield return new WaitForSeconds(0.1f);

        // Fade in
        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = elapsed / fadeTime;
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }
}
```

## Example 10: Performance Monitoring

```csharp
using UnityEngine;
using RTS.UI.HUD;

public class HUDPerformanceMonitor : MonoBehaviour
{
    private float averageFPS;
    private int frameCount;

    void Update()
    {
        // Monitor FPS
        frameCount++;
        averageFPS += (Time.deltaTime - averageFPS) / frameCount;

        // Adaptive quality
        if (frameCount % 100 == 0) // Check every 100 frames
        {
            float fps = 1f / averageFPS;

            if (fps < 30f)
            {
                // Performance is poor, switch to minimal HUD
                OptimizeHUD();
            }
        }
    }

    void OptimizeHUD()
    {
        Debug.LogWarning("Low FPS detected, optimizing HUD...");

        var framework = FindObjectOfType<MainHUDFramework>();
        if (framework == null) return;

        var config = framework.GetConfiguration();

        // Reduce update rate
        config.hudUpdateRate = 15;

        // Disable animations
        config.enableAnimations = false;

        // Switch to compact layout
        HUDController.Instance.ApplyLayout("CompactLayout");

        Debug.Log("HUD optimized for performance");
    }
}
```

## Best Practices Summary

1. **Use HUDController for runtime changes** - Don't access MainHUDFramework directly
2. **Save user preferences** - Remember player's HUD choices
3. **Adapt to resolution** - Use different layouts for different aspect ratios
4. **Context-sensitive HUD** - Show/hide elements based on game state
5. **Performance monitoring** - Adapt HUD complexity based on FPS
6. **Smooth transitions** - Fade HUD when switching layouts
7. **Hotkeys** - Provide keyboard shortcuts for HUD toggles
8. **Testing** - Use F3 hotkey to cycle layouts during development

## Common Patterns

### Pattern 1: Show/Hide Based on Selection
```csharp
void OnUnitSelected(GameObject unit)
{
    bool hasInventory = unit.GetComponent<UnitInventory>() != null;
    HUDController.Instance.SetComponentVisible("inventory", hasInventory);
}
```

### Pattern 2: Progressive UI Unlock
```csharp
void UnlockHUDFeature(string feature)
{
    switch (feature)
    {
        case "inventory":
            HUDController.Instance.SetComponentVisible("inventory", true);
            break;
        case "minimap":
            HUDController.Instance.SetComponentVisible("minimap", true);
            break;
    }
}
```

### Pattern 3: Context Stacks
```csharp
Stack<HUDState> hudStateStack = new Stack<HUDState>();

void PushHUDState(HUDState newState)
{
    hudStateStack.Push(GetCurrentState());
    ApplyHUDState(newState);
}

void PopHUDState()
{
    if (hudStateStack.Count > 0)
    {
        ApplyHUDState(hudStateStack.Pop());
    }
}
```

---

These examples cover most common use cases. Adapt them to your specific needs!
