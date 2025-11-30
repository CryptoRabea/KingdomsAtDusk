# Kingdoms at Dusk - Development Guide

**Project**: Kingdoms at Dusk
**Unity Version**: Unity 6000.2.10f1
**Genre**: Real-Time Strategy (RTS)

---

## Table of Contents

1. [Critical Development Rules](#critical-development-rules)
2. [Codebase Structure](#codebase-structure)
3. [Core Architecture](#core-architecture)
4. [Key Systems](#key-systems)
5. [Development Workflows](#development-workflows)
6. [Coding Conventions](#coding-conventions)
7. [Common Patterns](#common-patterns)
8. [Testing & Debugging](#testing--debugging)
9. [Performance Guidelines](#performance-guidelines)

---

## Critical Development Rules

### NEVER Use Old Input System
**CRITICAL**: This project uses the **New Input System**. NEVER use the old input system APIs:
- ❌ DO NOT use `Input.GetKey()`, `Input.GetButton()`, `Input.GetAxis()`
- ❌ DO NOT use `Input.mousePosition`, `Input.GetMouseButton()`
- ✅ ALWAYS use the New Input System via `PlayerInput` component and Input Actions
- ✅ ALWAYS use `InputAction` callbacks and bindings

### Use Modern Unity APIs
**Object Finding**:
- ❌ DO NOT use `Object.FindObjectsOfType<T>()` (deprecated)
- ✅ ALWAYS use `Object.FindObjectsByType<T>(FindObjectsSortMode sortMode)`
  - Use `FindObjectsSortMode.None` for better performance when sorting is not needed
  - Use `FindObjectsSortMode.InstanceID` only when you need sorted results

**Other Deprecations**:
- Check Unity console warnings regularly and update deprecated APIs immediately
- Refer to Unity 6 migration guides for API changes

---

## Codebase Structure

```
KingdomsAtDusk/
├── Assets/
│   ├── Scripts/                        # All C# code (157 scripts)
│   │   ├── Core/                       # Foundation architecture
│   │   │   ├── ServiceLocator.cs       # Dependency injection
│   │   │   ├── EventBus.cs             # Event system
│   │   │   ├── ObjectPool.cs           # Object pooling
│   │   │   └── Utilities/              # Helper utilities
│   │   │
│   │   ├── Managers/                   # Game state & orchestration
│   │   │   ├── GameManager.cs          # Main entry point
│   │   │   ├── ResourceManager.cs      # Resource economy
│   │   │   ├── BuildingManager.cs      # Building registry
│   │   │   ├── WaveManager.cs          # Enemy waves
│   │   │   └── Conditions/             # Victory/defeat conditions
│   │   │
│   │   ├── Units/                      # Unit systems
│   │   │   ├── AI/                     # AI controllers & states
│   │   │   ├── Components/             # Unit components
│   │   │   ├── Selection/              # Selection system
│   │   │   ├── Formation/              # Formation logic
│   │   │   └── Data/                   # Unit ScriptableObjects
│   │   │
│   │   ├── RTSBuildingsSystems/        # Building systems
│   │   │   ├── Building.cs             # Core building component
│   │   │   ├── ConstructionVisuals/    # Build animations
│   │   │   ├── WorkerModules/          # Production modules
│   │   │   └── Editor/                 # Custom editors
│   │   │
│   │   ├── UI/                         # User interface
│   │   │   ├── HUD/                    # Main HUD framework
│   │   │   ├── Minimap/                # Minimap system
│   │   │   ├── MainMenu/               # Menu screens
│   │   │   └── LoadingScreen/          # Loading UI
│   │   │
│   │   ├── Camera/                     # Camera control
│   │   ├── FogOfWar/                   # Vision system
│   │   ├── SaveLoad/                   # Persistence
│   │   └── Debug/                      # Debug tools
│   │
│   ├── Prefabs/                        # Prefabricated GameObjects
│   ├── ScriptableObjects/              # Data containers
│   ├── Scenes/                         # Unity scenes
│   ├── Materials/                      # Materials & shaders
│   ├── Models/                         # 3D models
│   └── Audio/                          # Sound effects & music
│
├── ProjectSettings/                    # Unity project settings
├── Packages/                           # Unity packages
└── UserSettings/                       # User-specific settings
```

---

## Core Architecture

### 1. Service Locator Pattern

**File**: `Assets/Scripts/Core/ServiceLocator.cs`

The Service Locator is the central dependency registry. Use it to access global services without direct coupling.

```csharp
// Register a service (usually in GameManager)
ServiceLocator.Register<IResourcesService>(resourceManager);

// Access a service
var resources = ServiceLocator.Get<IResourcesService>();
resources.AddResource(ResourceType.Gold, 100);

// Safe access (returns null if not found)
if (ServiceLocator.TryGet<IHappinessService>(out var happiness))
{
    happiness.ModifyHappiness(10);
}
```

**Registered Services**:
- `IResourcesService` - Resource management
- `IBuildingService` - Building registry
- `IHappinessService` - Happiness system
- `IPopulationService` - Population tracking
- `IGameStateService` - Game state (Playing, Paused, Victory, etc.)
- `ISaveLoadService` - Save/load system
- `IPoolService` - Object pooling

### 2. Event Bus (Publish-Subscribe)

**File**: `Assets/Scripts/Core/EventBus.cs`

Type-safe event system for decoupled communication between systems.

```csharp
// Define an event (struct, not class)
public struct UnitDiedEvent
{
    public GameObject Unit;
    public int Team;
}

// Subscribe to events
private void OnEnable()
{
    EventBus.Subscribe<UnitDiedEvent>(OnUnitDied);
}

private void OnDisable()
{
    EventBus.Unsubscribe<UnitDiedEvent>(OnUnitDied);
}

private void OnUnitDied(UnitDiedEvent evt)
{
    Debug.Log($"Unit {evt.Unit.name} died!");
}

// Publish events
EventBus.Publish(new UnitDiedEvent
{
    Unit = gameObject,
    Team = team
});
```

**Common Events** (see `Assets/Scripts/Core/Events/`):
- `UnitDiedEvent` - Unit death
- `WaveStartedEvent` - Wave begins
- `WaveCompletedEvent` - Wave cleared
- `ResourceChangedEvent` - Resource modified
- `BuildingDestroyedEvent` - Building destroyed
- `BuildingDamagedEvent` - Building damaged
- `GameSavedEvent` - Game saved
- `GameLoadedEvent` - Game loaded

### 3. State Machine Pattern

**Location**: `Assets/Scripts/Units/AI/States/`

All unit AI uses state machines for behavior management.

```csharp
// Base class
public abstract class UnitState
{
    public abstract void OnEnter();
    public abstract void OnUpdate();
    public abstract void OnExit();
}

// Example concrete state
public class AttackingState : UnitState
{
    private UnitAIController controller;
    private UnitCombat combat;

    public override void OnEnter()
    {
        // Initialize attack behavior
    }

    public override void OnUpdate()
    {
        // Check if target is in range, attack
        if (target == null)
            controller.TransitionToState(new IdleState());
    }

    public override void OnExit()
    {
        // Cleanup
    }
}
```

**Available States**:
- `IdleState` - Waiting for orders
- `MovingState` - Moving to destination
- `AttackingState` - Engaging enemy
- `HealingState` - Healing allies
- `RetreatState` - Fleeing from combat
- `DeadState` - Unit destroyed

### 4. Component-Based Architecture

Units and buildings use composition over inheritance.

**Unit Components**:
- `UnitHealth` - HP management, damage, death
- `UnitMovement` - NavMesh pathfinding
- `UnitCombat` - Attack mechanics
- `UnitSelectable` - Selection system integration

**Building Components**:
- `Building` - Core building logic
- `BuildingHealth` - Building HP and destruction
- `TowerCombat` - Defensive tower attacks
- `WorkerModule` - Production capabilities

---

## Key Systems

### Game Manager Initialization

**File**: `Assets/Scripts/Managers/GameManager.cs`

Entry point that initializes all services in the correct order:

```csharp
void Awake()
{
    // 1. Core services
    InitializeObjectPool();
    InitializeGameState();

    // 2. Economy
    InitializeResources();
    InitializeHappiness();
    InitializePopulation();

    // 3. Buildings
    InitializeBuildings();

    // 4. Persistence
    InitializeSaveLoad();
}
```

### Resource System

**File**: `Assets/Scripts/Managers/ResourceManager.cs`

Manages all game resources (Wood, Food, Gold, Stone).

```csharp
// Get resource manager
var resources = ServiceLocator.Get<IResourcesService>();

// Add resources
resources.AddResource(ResourceType.Wood, 100);

// Check if player can afford
var cost = ResourceCost.Build()
    .Wood(50)
    .Stone(25)
    .Create();

if (resources.CanAfford(cost))
{
    resources.SpendResources(cost);
    // Build structure
}

// Subscribe to changes
EventBus.Subscribe<ResourceChangedEvent>(OnResourceChanged);
```

### Unit AI System

**File**: `Assets/Scripts/Units/AI/UnitAIController.cs`

Core AI controller managing unit behavior via state machines.

**Required Components**:
- `UnitHealth`
- `UnitMovement`
- `UnitCombat`

**AI Types**:
- `SoldierAI` - Basic melee fighter
- `ArcherAI` - Ranged attacker
- `HealerAI` - Support healer
- `BerserkerAI` - Enrage mechanic (low HP = high damage)
- `TankAI` - Taunt ability to pull aggro
- `EnemyArcherAI` - Kiting behavior
- `BossAI` - Multi-phase boss with special abilities

```csharp
// AI controller automatically handles:
// - Target acquisition
// - State transitions
// - Combat engagement
// - Retreat logic
// - Death handling
```

### Building System

**File**: `Assets/Scripts/RTSBuildingsSystems/Building.cs`

**Construction Phases**:
1. **Placement** - Ghost preview
2. **Foundation** - Construction started
3. **InProgress** - Workers building
4. **Complete** - Fully built

```csharp
// Buildings can have:
// - Resource generation (passive income)
// - Unit training (via TrainingModule)
// - Worker allocation (via WorkerModule)
// - Happiness bonuses
// - Health/destruction

// Specialized buildings
public class Stronghold : Building
{
    // Main base - if destroyed, game over
}

public class Tower : Building
{
    // Defensive structure with TowerCombat
}
```

### Selection System

**File**: `Assets/Scripts/Units/Selection/UnitSelectionManager.cs`

Advanced RTS selection with:
- Single click selection
- Box selection (drag)
- Double-click to select all of type
- Hover highlighting
- Control groups (1-0 keys)

```csharp
// Selection manager handles:
// - Raycast detection
// - Selection rendering (highlight)
// - Command issuing (move, attack)
// - Formation assignment
```

### Wave System

**Files**:
- `Assets/Scripts/Managers/WaveManager.cs`
- `Assets/Scripts/Managers/EnemyWaveGenerator.cs`

**Wave Progression**:
- Waves 1-2: Basic enemies
- Wave 3+: Special units introduced
- Wave 10, 20, 30...: Boss waves
- Dynamic scaling: HP +5%, Damage +3% per wave

```csharp
// Wave events
EventBus.Subscribe<WaveStartedEvent>(OnWaveStart);
EventBus.Subscribe<WaveCompletedEvent>(OnWaveComplete);
```

### Victory/Defeat Conditions

**Location**: `Assets/Scripts/Managers/Conditions/`

**Victory Conditions**:
- `SurviveWavesVictory` - Survive X waves
- `DefeatBossVictory` - Kill boss unit

**Defeat Conditions**:
- `StrongholdDestroyedDefeat` - Main base destroyed
- `AllUnitsDeadDefeat` - No units + insufficient resources

```csharp
// Extend base classes for custom conditions
public class CustomVictory : VictoryCondition
{
    public override bool IsCompleted => // logic
    public override float Progress => // 0-1
    public override string GetStatusText() => "Status";
}
```

### Fog of War

**File**: `Assets/Scripts/FogOfWar/`

Grid-based vision system with:
- Vision providers (units, buildings)
- Fog states (Hidden, Explored, Visible)
- Minimap integration

### Save/Load System

**File**: `Assets/Scripts/SaveLoad/SaveLoadManager.cs`

Features:
- JSON serialization
- Compression (GZip)
- Encryption (AES)
- Auto-save
- Quick-save/load

```csharp
var saveLoad = ServiceLocator.Get<ISaveLoadService>();

// Save
saveLoad.SaveGame("save_slot_1");

// Load
saveLoad.LoadGame("save_slot_1");

// Events
EventBus.Subscribe<GameSavedEvent>(OnGameSaved);
EventBus.Subscribe<GameLoadedEvent>(OnGameLoaded);
```

---

## Development Workflows

### Adding a New Unit Type

1. **Create Unit Prefab**:
   - Add GameObject with model
   - Add `UnitHealth`, `UnitMovement`, `UnitCombat`
   - Add `UnitSelectable` for selection
   - Add AI controller (e.g., `SoldierAI`)

2. **Create UnitConfigSO**:
   - Right-click → Create → RTS → Unit Config
   - Set stats (health, damage, speed, range)

3. **Create AISettingsSO** (if needed):
   - Configure detection range, attack behavior

4. **Assign to prefab**:
   - Link UnitConfigSO to components
   - Set layer to appropriate team

5. **Test**:
   - Place in scene or spawn via code
   - Test movement, combat, AI behavior

### Adding a New Building

1. **Create Building Prefab**:
   - Add GameObject with model
   - Add `Building` component
   - Optionally add `BuildingHealth`

2. **Create BuildingDataSO**:
   - Right-click → Create → RTS → Building Data
   - Set construction time, costs, size

3. **Add Modules** (optional):
   - `TrainingModule` - Unit production
   - `WorkerModule` - Worker assignment
   - `ResourceGenerationModule` - Passive income

4. **Register with BuildingManager**:
   - Prefab is automatically discovered
   - Or manually register in BuildingManager

### Creating Custom Events

1. **Define event struct**:
```csharp
public struct CustomEvent
{
    public int Value;
    public GameObject Source;
}
```

2. **Publish**:
```csharp
EventBus.Publish(new CustomEvent
{
    Value = 42,
    Source = gameObject
});
```

3. **Subscribe**:
```csharp
void OnEnable() => EventBus.Subscribe<CustomEvent>(OnCustomEvent);
void OnDisable() => EventBus.Unsubscribe<CustomEvent>(OnCustomEvent);
void OnCustomEvent(CustomEvent evt) { /* handle */ }
```

### Extending Service Locator

1. **Define interface**:
```csharp
public interface IMyService
{
    void DoSomething();
}
```

2. **Implement**:
```csharp
public class MyManager : MonoBehaviour, IMyService
{
    public void DoSomething() { }
}
```

3. **Register in GameManager**:
```csharp
void InitializeMyService()
{
    var service = GetComponent<MyManager>();
    ServiceLocator.Register<IMyService>(service);
}
```

---

## Coding Conventions

### Namespaces

All code must use appropriate namespaces:

```csharp
// Core systems
namespace RTS.Core { }
namespace RTS.Core.Services { }
namespace RTS.Core.Events { }

// Game systems
namespace RTS.Units { }
namespace RTS.Units.AI { }
namespace RTS.Buildings { }
namespace RTS.Managers { }
namespace RTS.UI { }

// Game-specific
namespace KingdomsAtDusk.FogOfWar { }
namespace KingdomsAtDusk.UI { }
```

### Naming Conventions

```csharp
// Interfaces: I-prefix
public interface IResourcesService { }

// Events: Event-suffix (struct)
public struct UnitDiedEvent { }

// ScriptableObjects: SO-suffix
public class UnitConfigSO : ScriptableObject { }

// Private fields: camelCase
private int healthValue;

// Public properties: PascalCase
public int MaxHealth { get; set; }

// Methods: PascalCase
public void TakeDamage(int amount) { }

// Constants: UPPER_CASE
private const int MAX_UNITS = 200;
```

### Code Organization

```csharp
public class ExampleComponent : MonoBehaviour
{
    // 1. Serialized fields
    [SerializeField] private int value;

    // 2. Private fields
    private bool isActive;

    // 3. Properties
    public int Value => value;

    // 4. Unity lifecycle
    private void Awake() { }
    private void OnEnable() { }
    private void Start() { }
    private void Update() { }
    private void OnDisable() { }
    private void OnDestroy() { }

    // 5. Public methods
    public void PublicMethod() { }

    // 6. Private methods
    private void PrivateMethod() { }

    // 7. Event handlers
    private void OnEventReceived(Event evt) { }
}
```

### Comments & Documentation

```csharp
/// <summary>
/// Brief description of class purpose
/// </summary>
public class MyClass
{
    /// <summary>
    /// Deals damage to the target unit
    /// </summary>
    /// <param name="target">The unit to damage</param>
    /// <param name="amount">Amount of damage to deal</param>
    /// <returns>True if target was killed</returns>
    public bool DealDamage(GameObject target, int amount)
    {
        // Implementation
    }
}
```

---

## Common Patterns

### Singleton via Service Locator

❌ **DON'T** use traditional singletons:
```csharp
public class BadManager : MonoBehaviour
{
    public static BadManager Instance; // Avoid this
    void Awake() { Instance = this; }
}
```

✅ **DO** use Service Locator:
```csharp
public class GoodManager : MonoBehaviour, IMyService
{
    void Awake()
    {
        ServiceLocator.Register<IMyService>(this);
    }
}

// Usage
var service = ServiceLocator.Get<IMyService>();
```

### Object Pooling

Always pool frequently spawned objects:

```csharp
var pool = ServiceLocator.Get<IPoolService>();

// Get from pool
GameObject obj = pool.Get(prefab);

// Return to pool
pool.Return(obj);
```

### Resource Costs

Use builder pattern for costs:

```csharp
var cost = ResourceCost.Build()
    .Wood(100)
    .Food(50)
    .Gold(25)
    .Create();
```

### Safe Null Checks

```csharp
// Use null-conditional operators
target?.GetComponent<UnitHealth>()?.TakeDamage(10);

// Use pattern matching
if (target is { } t && t.GetComponent<UnitHealth>() is { } health)
{
    health.TakeDamage(10);
}
```

---

## Testing & Debugging

### Debug Tools

**File**: `Assets/Scripts/Debug/`

- Enable debug overlays in Inspector
- Use `Debug.DrawLine()` for visualization
- Check Unity console for warnings

### Common Issues

**Units not moving**:
- Check NavMesh is baked
- Verify UnitMovement component exists
- Check NavMeshAgent is enabled

**Selection not working**:
- Verify UnitSelectable component
- Check layer masks in SelectionManager
- Ensure colliders exist

**Resources not updating**:
- Check ResourceManager is initialized
- Verify ServiceLocator registration
- Subscribe to ResourceChangedEvent for debugging

### Performance Profiling

1. Open Unity Profiler (Window → Analysis → Profiler)
2. Check CPU usage per frame
3. Monitor GC allocations
4. Optimize hot paths identified in profiler

---

## Performance Guidelines

### DO's

✅ **Cache component references**:
```csharp
private UnitHealth health;
void Awake() { health = GetComponent<UnitHealth>(); }
```

✅ **Use object pooling**:
```csharp
var pool = ServiceLocator.Get<IPoolService>();
GameObject obj = pool.Get(prefab);
```

✅ **Minimize allocations in Update()**:
```csharp
// Good: reuse array
private RaycastHit[] hits = new RaycastHit[10];
void Update()
{
    int count = Physics.RaycastNonAlloc(ray, hits);
}
```

✅ **Use LayerMasks for raycasts**:
```csharp
LayerMask mask = LayerMask.GetMask("Unit", "Building");
Physics.Raycast(ray, out hit, maxDistance, mask);
```

### DON'Ts

❌ **Don't use Find in Update()**:
```csharp
void Update()
{
    var obj = GameObject.Find("Manager"); // SLOW!
}
```

❌ **Don't allocate in loops**:
```csharp
for (int i = 0; i < 1000; i++)
{
    var list = new List<int>(); // Allocates every iteration!
}
```

❌ **Don't use SendMessage**:
```csharp
// Slow and error-prone
gameObject.SendMessage("TakeDamage", 10);

// Use direct calls or events instead
health.TakeDamage(10);
EventBus.Publish(new DamageEvent { Amount = 10 });
```

### NavMesh Optimization

```csharp
// Update paths less frequently for distant units
private float pathUpdateInterval = 0.5f;
private float lastPathUpdate;

void Update()
{
    if (Time.time - lastPathUpdate > pathUpdateInterval)
    {
        UpdatePath();
        lastPathUpdate = Time.time;
    }
}
```

### UI Optimization

- Use Canvas groups for show/hide
- Disable raycasts on non-interactive elements
- Pool UI elements that spawn frequently
- Use sprite atlases to reduce draw calls

---

## Additional Resources

- **Gameplay Features**: See `GAMEPLAY_FEATURES.md` for detailed feature documentation
- **System-Specific Guides**: Check README.md files in subsystem folders:
  - `Assets/Scripts/UI/HUD/README.md` - HUD system
  - `Assets/Scripts/UI/Minimap/README.md` - Minimap
  - `Assets/Scripts/FogOfWar/README.md` - Fog of war
  - `Assets/Scripts/SaveLoad/README.md` - Save/load
  - `Assets/Scripts/RTSBuildingsSystems/WALL_SYSTEM_GUIDE.md` - Wall building

---

**Last Updated**: 2025-11-30
**Maintainer**: Development Team
