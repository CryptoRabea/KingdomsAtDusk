# Comprehensive Codebase Refactoring Report
**Generated:** 2025-11-13  
**Project:** Kingdoms At Dusk  
**Analysis Focus:** Building systems, UI systems, and Manager initialization

---

## Executive Summary

This report identifies 47 specific issues across 6 categories, affecting 20+ files. The most critical issues are:
1. **Two redundant building UI systems** (BuildingUI vs BuildingHUD)
2. **Duplicated cost display code** across 5 files
3. **Tight coupling** between UI and managers
4. **Missing service registration** for BuildingManager
5. **Heavy use of FindObjectOfType** instead of dependency injection

**Estimated Refactoring Effort:** 16-24 hours  
**Priority:** HIGH (affects maintainability and testability)

---

## 1. UNNECESSARY REFERENCES

### Issue 1.1: BuildingUI - Serialized BuildingManager Reference
**Files:** `/Assets/Scripts/UI/BuildingUI.cs`  
**Lines:** 18, 30-38

**Problem:**
```csharp
[SerializeField] private BuildingManager buildingManager;  // Line 18
// Then uses FindAnyObjectByType as fallback (lines 30-38)
```
BuildingManager reference is serialized but has FindAnyObjectByType fallback. This creates confusion - is it required or optional?

**Impact:**
- Serialized references must be manually assigned in Unity Editor
- Easy to forget assignment, leading to FindObjectOfType overhead
- Not using ServiceLocator despite having the infrastructure

**Refactoring Approach:**
1. Remove serialized field
2. Get BuildingManager from ServiceLocator (requires registering it first)
3. Keep FindAnyObjectByType only as emergency fallback with warning

**Code Example:**
```csharp
// BEFORE
[SerializeField] private BuildingManager buildingManager;
if (buildingManager == null)
{
    buildingManager = Object.FindAnyObjectByType<BuildingManager>();
}

// AFTER
private BuildingManager buildingManager;
buildingManager = ServiceLocator.TryGet<IBuildingService>();
if (buildingManager == null)
{
    Debug.LogWarning("BuildingManager not registered in ServiceLocator! Using fallback.");
    buildingManager = Object.FindAnyObjectByType<BuildingManager>();
}
```

---

### Issue 1.2: BuildingHUD - Duplicate BuildingManager Reference
**Files:** `/Assets/Scripts/RTSBuildingsSystems/BuildingHUD.cs`  
**Lines:** 21, 48-56

**Problem:** Exact same issue as BuildingUI - serialized reference with FindAnyObjectByType fallback.

**Impact:** Same as Issue 1.1

**Refactoring Approach:** Same as Issue 1.1

---

### Issue 1.3: BuildingButton - Unnecessary ResourceUI Dependency
**Files:** `/Assets/Scripts/RTSBuildingsSystems/BuildingButton.cs`  
**Lines:** 42, 57, 161-167

**Problem:**
```csharp
private ResourceUI resourceUI;  // Line 42
resourceUI = Object.FindAnyObjectByType<ResourceUI>();  // Line 57
// Used only to get resource icon sprites (lines 161-167)
```
BuildingButton depends on ResourceUI just to get resource icons, creating unnecessary coupling.

**Impact:**
- Tight coupling between UI systems
- FindObjectOfType overhead
- Hard to test BuildingButton without ResourceUI

**Refactoring Approach:**
1. Create `IResourceIconProvider` interface
2. Register ResourceUI as implementation in ServiceLocator
3. Or create static `ResourceUtility.GetResourceIcon(ResourceType)`

**Code Example:**
```csharp
// NEW: Core/IServices.cs
public interface IResourceIconProvider
{
    Sprite GetResourceIcon(ResourceType type);
    Color GetResourceColor(ResourceType type);
}

// UPDATED: UI/ResourceUI.cs
public class ResourceUI : MonoBehaviour, IResourceIconProvider
{
    public Sprite GetResourceIcon(ResourceType type) { /* ... */ }
    public Color GetResourceColor(ResourceType type) { /* ... */ }
}

// UPDATED: BuildingButton.cs
private IResourceIconProvider iconProvider;
iconProvider = ServiceLocator.TryGet<IResourceIconProvider>();
```

---

### Issue 1.4: BuildingDetailsUI - FindObjectOfType for SelectionManager
**Files:** `/Assets/Scripts/UI/BuildingDetailsUI.cs`  
**Lines:** 42, 77

**Problem:**
```csharp
selectionManager = FindObjectOfType<BuildingSelectionManager>();  // Line 77
```
Uses FindObjectOfType in Start(), creating overhead and tight coupling.

**Impact:**
- Performance overhead (FindObjectOfType is slow)
- Tight coupling
- Hard to test

**Refactoring Approach:**
1. Register BuildingSelectionManager as service
2. Or inject via constructor/Initialize method
3. Or use events instead of direct calls

---

### Issue 1.5: BuildingHUDToggle - FindObjectOfType for BuildingHUD
**Files:** `/Assets/Scripts/UI/BuildingHUDToggle.cs`  
**Lines:** 34-41

**Problem:** Same pattern - uses FindObjectOfType to locate BuildingHUD.

**Impact:** Same as Issue 1.4

**Refactoring Approach:** Same as Issue 1.4

---

## 2. DUPLICATED CODE

### Issue 2.1: CRITICAL - GetCostString() Duplicated 5+ Times
**Files:**
- `/Assets/Scripts/UI/BuildingUI.cs` (lines 246-257)
- `/Assets/Scripts/RTSBuildingsSystems/BuildingButton.cs` (lines 246-257)
- `/Assets/Scripts/UI/TrainUnitButton.cs` (lines 122-132)
- `/Assets/Scripts/RTSBuildingsSystems/BuildingDataSO.cs` (lines 138-148)
- `/Assets/Scripts/RTSBuildingsSystems/BuildingTooltip.cs` (lines 79-90)

**Problem:**
Nearly identical cost string formatting logic appears in 5 different files!

**Examples:**
```csharp
// BuildingUI.cs (lines 246-257)
private string GetCostString()
{
    var costs = buildingData.GetCosts();
    var costStrings = new List<string>();
    foreach (var cost in costs)
    {
        costStrings.Add($"{GetResourceIconText(cost.Key)}{cost.Value}");
    }
    return string.Join(" ", costStrings);
}

// TrainUnitButton.cs (lines 122-132) - ALMOST IDENTICAL
private string GetCostString()
{
    var costs = new List<string>();
    if (unitData.woodCost > 0) costs.Add($"🪵 {unitData.woodCost}");
    if (unitData.foodCost > 0) costs.Add($"🍖 {unitData.foodCost}");
    // ... etc
    return string.Join(" ", costs);
}
```

**Impact:**
- **HIGH SEVERITY** - Any change must be made in 5 places
- Inconsistent formatting across different UIs
- 100+ lines of duplicated code
- Maintenance nightmare

**Refactoring Approach:**
Create centralized `ResourceDisplayUtility` class:

```csharp
// NEW: Core/ResourceDisplayUtility.cs
namespace RTS.Core.Utilities
{
    public static class ResourceDisplayUtility
    {
        public static string FormatCosts(Dictionary<ResourceType, int> costs, 
            bool useEmoji = true, 
            bool showNames = true)
        {
            var costStrings = new List<string>();
            foreach (var cost in costs)
            {
                string icon = useEmoji ? GetResourceEmoji(cost.Key) : "";
                string name = showNames ? cost.Key.ToString() : "";
                costStrings.Add($"{icon}{name}: {cost.Value}");
            }
            return string.Join(", ", costStrings);
        }
        
        public static string GetResourceEmoji(ResourceType type)
        {
            return type switch
            {
                ResourceType.Wood => "🪵",
                ResourceType.Food => "🍖",
                ResourceType.Gold => "💰",
                ResourceType.Stone => "🪨",
                _ => ""
            };
        }
        
        public static Color GetResourceColor(ResourceType type)
        {
            return type switch
            {
                ResourceType.Wood => new Color(0.55f, 0.27f, 0.07f),
                ResourceType.Food => new Color(0.9f, 0.8f, 0.2f),
                ResourceType.Gold => new Color(1f, 0.84f, 0f),
                ResourceType.Stone => new Color(0.5f, 0.5f, 0.5f),
                _ => Color.white
            };
        }
    }
}
```

**Then replace all 5 implementations with:**
```csharp
costText.text = ResourceDisplayUtility.FormatCosts(buildingData.GetCosts());
```

**Estimated Savings:** Remove ~100 lines of duplicated code

---

### Issue 2.2: GetResourceIconText() Duplicated
**Files:**
- `/Assets/Scripts/RTSBuildingsSystems/BuildingButton.cs` (lines 262-272)
- `/Assets/Scripts/UI/TrainUnitButton.cs` (inline in GetCostString)

**Problem:** Same emoji mapping logic appears twice.

**Impact:** Moderate - emoji changes must be made in 2 places

**Refactoring Approach:** Merge into `ResourceDisplayUtility.GetResourceEmoji()` (from Issue 2.1)

---

### Issue 2.3: GetResourceColor() - Should Be Centralized
**Files:** `/Assets/Scripts/RTSBuildingsSystems/BuildingButton.cs` (lines 277-287)

**Problem:** Color mapping exists in BuildingButton, but ResourceUI also has color settings.

**Impact:** Inconsistent colors across UI systems

**Refactoring Approach:** Merge into `ResourceDisplayUtility.GetResourceColor()` (from Issue 2.1)

---

### Issue 2.4: Update() Loops for Affordability Checking
**Files:**
- `/Assets/Scripts/RTSBuildingsSystems/BuildingButton.cs` (lines 76-101)
- `/Assets/Scripts/UI/TrainUnitButton.cs` (lines 62-66)

**Problem:**
Both scripts have Update() methods that check affordability every frame:
```csharp
// BuildingButton.cs
public void UpdateState(IResourcesService resourceService)
{
    // Called from BuildingHUD.Update() for ALL buttons
    var costs = buildingData.GetCosts();
    bool canAfford = resourceService.CanAfford(costs);
    // Update colors...
}

// TrainUnitButton.cs  
private void Update()
{
    UpdateAffordability();  // Called EVERY FRAME
}
```

**Impact:**
- Performance issue: Checking affordability for 10 buttons = 600 checks/second at 60fps
- Inefficient: Resources only change on events, not every frame
- Duplicated logic

**Refactoring Approach:**
Both scripts already subscribe to `ResourcesChangedEvent`. Move affordability check to event handler only:

```csharp
// BEFORE (called every frame)
private void Update()
{
    UpdateAffordability();
}

// AFTER (called only when resources change)
private void OnEnable()
{
    EventBus.Subscribe<ResourcesChangedEvent>(OnResourcesChanged);
}

private void OnResourcesChanged(ResourcesChangedEvent evt)
{
    UpdateAffordability();  // Only called when resources actually change
}
```

**Estimated Performance Gain:** 99% reduction in affordability checks

---

### Issue 2.5: FindObjectOfType Fallback Pattern
**Files:**
- `/Assets/Scripts/UI/BuildingUI.cs` (30-38)
- `/Assets/Scripts/RTSBuildingsSystems/BuildingHUD.cs` (48-56)
- `/Assets/Scripts/RTSBuildingsSystems/BuildingButton.cs` (57)
- `/Assets/Scripts/UI/BuildingDetailsUI.cs` (77)
- `/Assets/Scripts/UI/BuildingHUDToggle.cs` (36)

**Problem:**
Same pattern repeated 5+ times:
```csharp
if (component == null)
{
    component = Object.FindAnyObjectByType<ComponentType>();
    if (component == null)
    {
        Debug.LogError("Component not found!");
    }
}
```

**Impact:** Code duplication, inconsistent error handling

**Refactoring Approach:**
Add utility method to ServiceLocator:

```csharp
// Core/ServiceLocator.cs
public static T GetOrFind<T>() where T : UnityEngine.Object
{
    // Try to get from services first
    if (services.TryGetValue(typeof(T), out var service))
    {
        return service as T;
    }
    
    // Fallback to FindObjectOfType with warning
    Debug.LogWarning($"Service {typeof(T).Name} not registered. Using FindObjectOfType (slow).");
    return Object.FindAnyObjectByType<T>();
}
```

**Then replace all occurrences with:**
```csharp
buildingManager = ServiceLocator.GetOrFind<BuildingManager>();
```

---

## 3. TIGHT COUPLING

### Issue 3.1: BuildingButton → ResourceUI Coupling
**Files:** `/Assets/Scripts/RTSBuildingsSystems/BuildingButton.cs` (lines 42, 57, 161-167)

**Problem:**
```csharp
private ResourceUI resourceUI;  // Direct dependency
resourceUI = Object.FindAnyObjectByType<ResourceUI>();
var resourceDisplay = resourceUI.GetDisplay(cost.Key);  // Tight coupling
```

**Impact:**
- Cannot test BuildingButton without ResourceUI
- Cannot use different resource icon providers
- Violates Dependency Inversion Principle

**Refactoring Approach:** See Issue 1.3 - create IResourceIconProvider interface

---

### Issue 3.2: BuildingButton → BuildingHUD → BuildingManager (Chain)
**Files:** `/Assets/Scripts/RTSBuildingsSystems/BuildingButton.cs` (line 41)

**Problem:**
```csharp
private BuildingHUD parentHUD;  // Line 41
public void Initialize(BuildingDataSO data, int index, BuildingHUD hud)  // Line 50
{
    parentHUD = hud;  // Stored but barely used
}
```
BuildingButton stores parentHUD just to potentially access BuildingManager. This is unnecessary indirection.

**Impact:**
- Unnecessary coupling
- Confusing dependency chain

**Refactoring Approach:**
BuildingButton doesn't actually need parentHUD. It only needs:
1. BuildingDataSO (already has)
2. Building index (already has)
3. Click callback (can be passed as Action)

```csharp
// AFTER
public void Initialize(BuildingDataSO data, int index, Action<int> onClickCallback)
{
    buildingData = data;
    buildingIndex = index;
    button.onClick.AddListener(() => onClickCallback(index));
}
```

---

### Issue 3.3: BuildingDetailsUI ↔ BuildingSelectionManager Coupling
**Files:** `/Assets/Scripts/UI/BuildingDetailsUI.cs` (lines 42, 77, 106-114, 337-363)

**Problem:**
BuildingDetailsUI directly manipulates BuildingSelectionManager's spawn point mode:
```csharp
private BuildingSelectionManager selectionManager;  // Line 42
selectionManager.SetSpawnPointMode(isSettingSpawnPoint);  // Line 356

// Also polls state in Update()
if (!selectionManager.IsSpawnPointMode())  // Line 109
{
    isSettingSpawnPoint = false;
}
```

**Impact:**
- Tight coupling between UI and input manager
- Bidirectional dependency (both track state)
- Polling in Update() is inefficient

**Refactoring Approach:**
Use event-driven approach:

```csharp
// NEW: Add to GameEvents.cs
public struct SpawnPointModeChangedEvent
{
    public bool IsEnabled;
    public SpawnPointModeChangedEvent(bool enabled) { IsEnabled = enabled; }
}

// BuildingSelectionManager.cs
public void SetSpawnPointMode(bool enabled)
{
    isSpawnPointMode = enabled;
    EventBus.Publish(new SpawnPointModeChangedEvent(enabled));
}

// BuildingDetailsUI.cs - no more direct reference needed!
private void OnEnable()
{
    EventBus.Subscribe<SpawnPointModeChangedEvent>(OnSpawnPointModeChanged);
}

private void OnSpawnPointModeChanged(SpawnPointModeChangedEvent evt)
{
    isSettingSpawnPoint = evt.IsEnabled;
    UpdateSpawnPointButtonText();
}
```

---

### Issue 3.4: BuildingManager → Mouse/Keyboard Hardware
**Files:** `/Assets/Scripts/Managers/BuildingManager.cs` (lines 44-45, 55-56)

**Problem:**
```csharp
private Mouse mouse;      // Line 44
private Keyboard keyboard;  // Line 45

mouse = Mouse.current;      // Line 55
keyboard = Keyboard.current;  // Line 56
```
Direct dependency on Input System hardware makes testing impossible.

**Impact:**
- Cannot unit test BuildingManager
- Cannot support different input methods
- Violates Single Responsibility Principle

**Refactoring Approach:**
Create input abstraction:

```csharp
// NEW: Core/IInputService.cs
public interface IInputService
{
    bool GetPlaceAction();
    bool GetCancelAction();
    Vector2 GetPointerPosition();
}

// NEW: Managers/UnityInputService.cs
public class UnityInputService : IInputService
{
    private Mouse mouse;
    private Keyboard keyboard;
    
    public bool GetPlaceAction() => mouse?.leftButton.wasPressedThisFrame ?? false;
    public bool GetCancelAction() => 
        mouse?.rightButton.wasPressedThisFrame ?? false || 
        keyboard?.escapeKey.wasPressedThisFrame ?? false;
    public Vector2 GetPointerPosition() => mouse?.position.ReadValue() ?? Vector2.zero;
}

// BuildingManager.cs - now testable!
private IInputService inputService;

private void Start()
{
    inputService = ServiceLocator.TryGet<IInputService>();
}

private void HandlePlacementInput()
{
    if (inputService.GetPlaceAction())
    {
        // Place building
    }
}
```

---

## 4. REDUNDANT SYSTEMS

### Issue 4.1: CRITICAL - BuildingUI vs BuildingHUD Are Redundant
**Files:**
- `/Assets/Scripts/UI/BuildingUI.cs` (entire file)
- `/Assets/Scripts/RTSBuildingsSystems/BuildingHUD.cs` (entire file)

**Problem:**
Two separate scripts that do almost exactly the same thing:

| Feature | BuildingUI | BuildingHUD |
|---------|-----------|-------------|
| Creates building buttons | ✅ Yes | ✅ Yes |
| Gets buildings from BuildingManager | ✅ Yes | ✅ Yes |
| Updates affordability | ✅ Yes | ✅ Yes |
| Handles button clicks | ✅ Yes | ✅ Yes |
| Subscribes to ResourcesChangedEvent | ✅ Yes | ✅ Yes |
| Hotkey support | ❌ No | ✅ Yes |
| Placement info panel | ❌ No | ✅ Yes |
| Custom BuildingButton component | ❌ No (uses simple) | ✅ Yes |

**Analysis:**
- BuildingUI: 265 lines
- BuildingHUD: 408 lines
- ~70% code overlap
- BuildingHUD is more feature-complete

**Impact:**
- **CRITICAL SEVERITY**
- Confusion about which one to use
- Maintenance of duplicate logic
- Wasted development time
- Inconsistent UI behavior

**Refactoring Approach:**
1. **Deprecate BuildingUI.cs** completely
2. **Keep BuildingHUD.cs** as the single building UI system
3. Add feature flag to BuildingHUD to enable/disable advanced features:
   ```csharp
   [SerializeField] private bool enableHotkeys = true;
   [SerializeField] private bool showPlacementInfo = true;
   ```
4. Update all scenes to use BuildingHUD
5. Delete BuildingUI.cs

**Migration Steps:**
1. Find all scenes/prefabs using BuildingUI
2. Replace with BuildingHUD
3. Test thoroughly
4. Delete BuildingUI.cs and BuildingButtonSimple class

**Estimated Time:** 2-3 hours

---

### Issue 4.2: BuildingButtonSimple vs BuildingButton
**Files:**
- `/Assets/Scripts/UI/BuildingUI.cs` (lines 260-264) - defines BuildingButtonSimple
- `/Assets/Scripts/RTSBuildingsSystems/BuildingButton.cs` (entire file)

**Problem:**
BuildingUI defines a simple `BuildingButtonSimple` class (5 lines), while there's a full-featured `BuildingButton` component (378 lines).

```csharp
// BuildingUI.cs (lines 260-264)
public class BuildingButtonSimple : MonoBehaviour
{
    public int buildingIndex;
    public BuildingDataSO buildingData;
}
```

**Impact:** Redundant, less-featured implementation

**Refactoring Approach:**
Removed automatically when BuildingUI is deleted (Issue 4.1)

---

### Issue 4.3: Repeated GetAllBuildingData() Calls
**Files:**
- `/Assets/Scripts/UI/BuildingUI.cs` (lines 60, 172, 204)
- `/Assets/Scripts/RTSBuildingsSystems/BuildingHUD.cs` (lines 110, 214, 244)

**Problem:**
BuildingManager.GetAllBuildingData() is called multiple times per frame:
```csharp
// Called in CreateBuildingButtons()
BuildingDataSO[] availableBuildings = buildingManager.GetAllBuildingData();

// Called again in OnBuildingButtonClicked()
BuildingDataSO[] availableBuildings = buildingManager.GetAllBuildingData();

// Called again in UpdateAllButtons()
BuildingDataSO[] availableBuildings = buildingManager.GetAllBuildingData();
```

**Impact:**
- Unnecessary array allocations
- Repeated method calls
- Poor performance with many buildings

**Refactoring Approach:**
Cache the array:
```csharp
private BuildingDataSO[] cachedBuildingData;

private void Start()
{
    cachedBuildingData = buildingManager.GetAllBuildingData();
    CreateBuildingButtons();
}

public void RefreshButtons()
{
    cachedBuildingData = buildingManager.GetAllBuildingData();
    CreateBuildingButtons();
}

private void OnBuildingButtonClicked(int buildingIndex)
{
    if (buildingIndex >= cachedBuildingData.Length) return;
    BuildingDataSO buildingData = cachedBuildingData[buildingIndex];
    // ...
}
```

---

## 5. DEAD CODE

### Issue 5.1: BuildingDataSO - Deprecated Alias Properties
**Files:** `/Assets/Scripts/RTSBuildingsSystems/BuildingDataSO.cs` (lines 81, 85)

**Problem:**
```csharp
public float resourceGenerationRate = 5f; // Alias for generationInterval (backwards compatible)
public float buildTime = 5f; // Alias for constructionTime (backwards compatible)
```
Alias properties that duplicate data for "backwards compatibility" but aren't actually used.

**Impact:**
- Confusing for developers
- Potential for bugs if wrong property is modified
- Extra 8 bytes per BuildingDataSO instance

**Refactoring Approach:**
1. Search codebase for usage of `resourceGenerationRate` and `buildTime`
2. Replace all with `generationInterval` and `constructionTime`
3. Delete alias properties
4. Use `[Obsolete]` attribute if external code might use them:
   ```csharp
   [Obsolete("Use generationInterval instead")]
   public float resourceGenerationRate => generationInterval;
   
   [Obsolete("Use constructionTime instead")]  
   public float buildTime => constructionTime;
   ```

---

### Issue 5.2: ResourcesSpentEvent Missing Stone Field
**Files:** `/Assets/Scripts/Core/GameEvents.cs` (lines 24-38)

**Problem:**
```csharp
public struct ResourcesSpentEvent
{
    public int Wood;
    public int Food;
    public int Gold;
    // Missing: public int Stone;
    public bool Success;

    public ResourcesSpentEvent(int wood, int food, int gold, int stone, bool success)
    {
        Wood = wood;
        Food = food;
        Gold = gold;
        // stone parameter is ignored!
        Success = success;
    }
}
```
Constructor accepts `stone` parameter but doesn't store it.

**Impact:**
- Bug: Stone costs aren't tracked in events
- Misleading API

**Refactoring Approach:**
```csharp
public struct ResourcesSpentEvent
{
    public int Wood;
    public int Food;
    public int Gold;
    public int Stone;  // ADD THIS
    public bool Success;

    public ResourcesSpentEvent(int wood, int food, int gold, int stone, bool success)
    {
        Wood = wood;
        Food = food;
        Gold = gold;
        Stone = stone;  // ADD THIS
        Success = success;
    }
}
```

---

### Issue 5.3: Commented Out Code
**Files:**
- `/Assets/Scripts/UI/BuildingUI.cs` (line 55)
- `/Assets/Scripts/UI/BuildingUI.cs` (line 244)

**Problem:**
```csharp
// Debug.LogError("BuildingUI: Missing references!");  // Line 55
// TODO: Show UI notification with required resources  // Line 244
```

**Impact:** Code clutter

**Refactoring Approach:**
1. Remove commented Debug.LogError (replace with actual error if needed)
2. Convert TODO to GitHub issue or implement it

---

### Issue 5.4: BuildingButton - Unused State Variables
**Files:** `/Assets/Scripts/RTSBuildingsSystems/BuildingButton.cs` (lines 45-48)

**Problem:**
```csharp
private Color currentColor;     // Used ✅
private bool isAffordable;      // Used ✅
private bool isSelected;        // Set but not effectively used ❓
private bool isHovered;         // Set but not effectively used ❓
```

**Analysis:**
- `isSelected` is set in OnClick() but only affects color
- `isHovered` is set in OnPointerEnter/Exit but only affects temporary color
- Neither prevents double-selection or provides external state checking

**Impact:** Minor - extra 2 bytes per button, slight confusion

**Refactoring Approach:**
If not needed for external queries, make them local variables:
```csharp
public void OnPointerEnter()
{
    bool isHovered = true;  // Local variable
    // ...
}
```

---

## 6. MISSING CENTRALIZATION

### Issue 6.1: CRITICAL - BuildingManager Not Registered in ServiceLocator
**Files:**
- `/Assets/Scripts/Managers/GameManager.cs` (entire initialization)
- `/Assets/Scripts/Managers/BuildingManager.cs`

**Problem:**
GameManager registers ResourceManager and HappinessManager as services, but NOT BuildingManager:

```csharp
// GameManager.cs
private void InitializeServices()
{
    InitializeObjectPool();
    InitializeResourceManager();    // ✅ Registered
    InitializeHappinessManager();   // ✅ Registered
    // BuildingManager is NEVER registered! ❌
}
```

Yet BuildingUI and BuildingHUD both need BuildingManager, so they use FindObjectOfType as fallback!

**Impact:**
- **CRITICAL SEVERITY**
- Inconsistent service architecture
- Unnecessary FindObjectOfType calls
- Other systems can't easily access BuildingManager

**Refactoring Approach:**

```csharp
// 1. Create interface for BuildingManager
// Core/IServices.cs
public interface IBuildingService
{
    BuildingDataSO[] GetAllBuildingData();
    bool CanAffordBuilding(BuildingDataSO buildingData);
    void StartPlacingBuilding(int buildingIndex);
    void CancelPlacement();
    bool IsPlacing { get; }
}

// 2. Implement interface
// Managers/BuildingManager.cs
public class BuildingManager : MonoBehaviour, IBuildingService
{
    // ... existing code ...
}

// 3. Register in GameManager
// Managers/GameManager.cs
[SerializeField] private BuildingManager buildingManager;

private void InitializeServices()
{
    InitializeObjectPool();
    InitializeResourceManager();
    InitializeHappinessManager();
    InitializeBuildingManager();  // NEW!
}

private void InitializeBuildingManager()
{
    if (buildingManager == null)
    {
        Debug.LogError("BuildingManager not assigned in GameManager!");
        return;
    }
    ServiceLocator.Register<IBuildingService>(buildingManager);
}

// 4. Update all consumers
// UI/BuildingUI.cs, UI/BuildingHUD.cs
private IBuildingService buildingService;

private void Start()
{
    buildingService = ServiceLocator.Get<IBuildingService>();
    CreateBuildingButtons();
}
```

**Estimated Time:** 1-2 hours  
**Priority:** CRITICAL - blocks other refactoring

---

### Issue 6.2: No Central ResourceDisplayUtility
**Files:** Multiple (see Issue 2.1)

**Problem:**
Cost display logic is duplicated across 5+ files without any central utility.

**Impact:**
- Already covered in Issue 2.1
- Missing from Core architecture

**Refactoring Approach:**
Create `Core/Utilities/ResourceDisplayUtility.cs` as shown in Issue 2.1

---

### Issue 6.3: Resource Icon Management Not Centralized
**Files:**
- `/Assets/Scripts/UI/ResourceUI.cs` (has GetDisplay but not widely used)
- `/Assets/Scripts/RTSBuildingsSystems/BuildingButton.cs` (hardcoded colors and emoji)

**Problem:**
ResourceUI has capability to provide resource icons via GetDisplay(), but:
1. Not registered as service
2. Not used by most systems
3. Each system implements own fallbacks

**Impact:**
- Inconsistent resource icon display
- Duplicated color/emoji definitions

**Refactoring Approach:**

```csharp
// 1. Extend ResourceUI to be central registry
// UI/ResourceUI.cs
public class ResourceUI : MonoBehaviour, IResourceIconProvider
{
    [SerializeField] private ResourceIconMapping[] iconMappings;
    
    [System.Serializable]
    public class ResourceIconMapping
    {
        public ResourceType type;
        public Sprite icon;
        public Color color;
        public string emoji;
    }
    
    private void Start()
    {
        // Register as service
        ServiceLocator.Register<IResourceIconProvider>(this);
    }
    
    public Sprite GetResourceIcon(ResourceType type)
    {
        return iconMappings.FirstOrDefault(m => m.type == type)?.icon;
    }
    
    public Color GetResourceColor(ResourceType type)
    {
        return iconMappings.FirstOrDefault(m => m.type == type)?.color ?? Color.white;
    }
    
    public string GetResourceEmoji(ResourceType type)
    {
        return iconMappings.FirstOrDefault(m => m.type == type)?.emoji ?? "";
    }
}

// 2. Create interface
// Core/IServices.cs
public interface IResourceIconProvider
{
    Sprite GetResourceIcon(ResourceType type);
    Color GetResourceColor(ResourceType type);
    string GetResourceEmoji(ResourceType type);
}

// 3. Update ResourceDisplayUtility to use service
// Core/Utilities/ResourceDisplayUtility.cs
public static string FormatCosts(Dictionary<ResourceType, int> costs)
{
    var iconProvider = ServiceLocator.TryGet<IResourceIconProvider>();
    // Use iconProvider if available, fallback to hardcoded emoji
}
```

---

### Issue 6.4: No Base Class for Event Subscription Pattern
**Files:** Multiple UI scripts

**Problem:**
Many scripts repeat the same OnEnable/OnDisable pattern:
```csharp
private void OnEnable()
{
    EventBus.Subscribe<Event1>(OnEvent1);
    EventBus.Subscribe<Event2>(OnEvent2);
}

private void OnDisable()
{
    EventBus.Unsubscribe<Event1>(OnEvent1);
    EventBus.Unsubscribe<Event2>(OnEvent2);
}
```

This pattern appears in:
- BuildingUI
- BuildingHUD
- BuildingDetailsUI
- ResourceUI
- TrainUnitButton (via Update instead)

**Impact:**
- Code duplication
- Easy to forget Unsubscribe (memory leaks)
- Boilerplate code

**Refactoring Approach:**

```csharp
// NEW: Core/MonoBehaviourWithEvents.cs
public abstract class MonoBehaviourWithEvents : MonoBehaviour
{
    private List<Delegate> registeredHandlers = new List<Delegate>();
    
    protected void SubscribeEvent<T>(Action<T> handler) where T : struct
    {
        EventBus.Subscribe<T>(handler);
        registeredHandlers.Add(handler);
    }
    
    protected virtual void OnEnable()
    {
        RegisterEvents();
    }
    
    protected virtual void OnDisable()
    {
        UnregisterEvents();
    }
    
    protected abstract void RegisterEvents();
    
    private void UnregisterEvents()
    {
        foreach (var handler in registeredHandlers)
        {
            var handlerType = handler.GetType();
            var eventType = handlerType.GetGenericArguments()[0];
            // Unsubscribe using reflection
        }
        registeredHandlers.Clear();
    }
}

// USAGE
public class BuildingUI : MonoBehaviourWithEvents
{
    protected override void RegisterEvents()
    {
        SubscribeEvent<ResourcesChangedEvent>(OnResourcesChanged);
    }
    
    private void OnResourcesChanged(ResourcesChangedEvent evt)
    {
        UpdateAllButtons();
    }
}
```

**Note:** This might be over-engineering. Alternative: Just document the pattern and use IDE code snippets.

---

### Issue 6.5: No Central Input Management
**Files:**
- `/Assets/Scripts/Managers/BuildingManager.cs` (handles placement input)
- `/Assets/Scripts/RTSBuildingsSystems/BuildingSelectionManager.cs` (handles selection input)
- `/Assets/Scripts/RTSBuildingsSystems/BuildingHUD.cs` (handles hotkey input)

**Problem:**
Three different managers handle input directly:
- BuildingManager: Left/right click for placement
- BuildingSelectionManager: Left/right click for selection
- BuildingHUD: Keyboard hotkeys

**Input Conflicts:**
- What if player is placing AND selecting?
- What if they press hotkey during placement?
- No input priority system

**Impact:**
- Potential input conflicts
- No way to disable input globally
- Hard to add new input methods (gamepad, touch)
- Cannot implement input rebinding

**Refactoring Approach:**

```csharp
// NEW: Managers/InputManager.cs
public class InputManager : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    
    public event Action<Vector2> OnLeftClick;
    public event Action<Vector2> OnRightClick;
    public event Action<Key> OnHotkeyPressed;
    
    private void OnEnable()
    {
        // Set up input actions
        var clickAction = inputActions.FindAction("Click");
        clickAction.performed += ctx => HandleClick(ctx);
    }
    
    private void HandleClick(InputAction.CallbackContext ctx)
    {
        var position = Mouse.current.position.ReadValue();
        
        // Priority system
        if (ctx.action.name == "Click")
        {
            OnLeftClick?.Invoke(position);
        }
    }
}

// THEN: Managers subscribe to InputManager instead of handling raw input
// BuildingManager.cs
private void Start()
{
    var inputManager = ServiceLocator.Get<InputManager>();
    inputManager.OnLeftClick += HandleLeftClick;
    inputManager.OnRightClick += HandleRightClick;
}
```

---

## 7. ADDITIONAL RECOMMENDATIONS

### Rec 7.1: Create IBuildingUIService Interface
**Rationale:** BuildingUI/HUD should be accessible as a service for other systems to show/hide the panel.

```csharp
public interface IBuildingUIService
{
    void Show();
    void Hide();
    void RefreshButtons();
    bool IsVisible { get; }
}
```

---

### Rec 7.2: Add Building Placement State Machine
**Current:** BuildingManager uses boolean flags for state  
**Better:** Use enum-based state machine

```csharp
public enum BuildingPlacementState
{
    Idle,
    SelectingBuilding,
    PlacingBuilding,
    ConfirmingPlacement,
    Canceled
}
```

---

### Rec 7.3: Extract Building Cost Validation
**Current:** Scattered across multiple scripts  
**Better:** Single responsibility class

```csharp
public class BuildingCostValidator
{
    private IResourcesService resourceService;
    
    public ValidationResult CanAfford(BuildingDataSO building)
    {
        // Returns detailed result with missing resources
    }
}
```

---

## REFACTORING PRIORITY MATRIX

| Issue | Impact | Effort | Priority | Time Est. |
|-------|--------|--------|----------|-----------|
| 4.1: Merge BuildingUI/HUD | HIGH | Medium | P0 | 3h |
| 6.1: Register BuildingManager | HIGH | Low | P0 | 1h |
| 2.1: Centralize cost display | HIGH | Medium | P1 | 2h |
| 3.3: Decouple UI from SelectionManager | MEDIUM | Medium | P1 | 2h |
| 2.4: Event-based affordability | MEDIUM | Low | P1 | 1h |
| 4.3: Cache building data | MEDIUM | Low | P1 | 0.5h |
| 1.1-1.5: Remove unnecessary refs | MEDIUM | Low | P2 | 2h |
| 6.3: Central resource icons | MEDIUM | Medium | P2 | 2h |
| 5.2: Fix ResourcesSpentEvent | HIGH | Low | P2 | 0.25h |
| 3.4: Input abstraction | MEDIUM | High | P3 | 4h |
| 6.5: InputManager | LOW | High | P3 | 4h |
| 5.1: Remove alias properties | LOW | Low | P3 | 0.5h |
| 5.3: Clean commented code | LOW | Low | P3 | 0.25h |

**Total Estimated Time:** ~22 hours

**Recommended Order:**
1. **Week 1 (P0):** Issues 6.1, 4.1 (4 hours)
2. **Week 2 (P1):** Issues 2.1, 3.3, 2.4, 4.3 (5.5 hours)  
3. **Week 3 (P2):** Issues 1.1-1.5, 6.3, 5.2 (4.25 hours)
4. **Week 4 (P3):** Issues 3.4, 6.5, 5.1, 5.3 (8.75 hours)

---

## TESTING RECOMMENDATIONS

After refactoring, implement:

1. **Unit Tests** for:
   - ResourceDisplayUtility
   - BuildingCostValidator
   - ServiceLocator.GetOrFind()

2. **Integration Tests** for:
   - Building placement flow
   - UI affordability updates
   - Event subscription/unsubscription

3. **Manual Test Checklist**:
   - [ ] All building buttons show correct costs
   - [ ] Affordability updates when resources change
   - [ ] Building placement works
   - [ ] Hotkeys work
   - [ ] Building selection shows details panel
   - [ ] Spawn point setting works
   - [ ] No FindObjectOfType warnings in console

---

## CONCLUSION

This refactoring will:
- **Remove ~500 lines of duplicated code**
- **Eliminate 1 redundant system** (BuildingUI)
- **Fix 5 architectural issues** (coupling, missing services)
- **Improve performance** (event-based updates, cached data)
- **Improve testability** (interfaces, dependency injection)

**Next Steps:**
1. Review and prioritize issues
2. Create GitHub issues for P0/P1 items
3. Start with Issue 6.1 (critical foundation)
4. Test thoroughly after each change

---

**Report End**
