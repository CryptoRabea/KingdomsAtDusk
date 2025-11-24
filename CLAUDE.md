# CLAUDE.md - AI Assistant Guide for Kingdoms at Dusk

**Project:** Kingdoms at Dusk
**Type:** Unity Real-Time Strategy (RTS) Game
**Unity Version:** 6000.2.10f1 (Unity 6)
**Last Updated:** 2025-11-24

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Codebase Structure](#codebase-structure)
3. [Architecture & Patterns](#architecture--patterns)
4. [Core Systems](#core-systems)
5. [Development Conventions](#development-conventions)
6. [Common Tasks](#common-tasks)
7. [Testing & Debugging](#testing--debugging)
8. [Known Issues & Technical Debt](#known-issues--technical-debt)
9. [Performance Considerations](#performance-considerations)
10. [Quick Reference](#quick-reference)

---

## Project Overview

### What is Kingdoms at Dusk?

A medieval fantasy RTS game combining base building, resource management, defensive structures, and unit combat. Players build kingdoms, manage resources, construct defenses (walls and towers), and command units in real-time battles.

### Core Features

- **Base Building:** 8 building types (Residential, Production, Military, Economic, Religious, Cultural, Defensive, Special)
- **Resource Management:** 4 resources (Wood, Food, Gold, Stone) with generation and consumption
- **Defensive Systems:** Pole-to-pole wall placement with intelligent segmentation, tower combat
- **Unit Systems:** State-based AI (soldiers, archers, healers) with combat mechanics
- **Kingdom Management:** Happiness/morale system, population, reputation, day/night cycle
- **RTS Features:** Fog of war, minimap, camera controls, multi-unit selection

### Visual Style

Low-poly medieval aesthetic rendered with Universal Render Pipeline (URP)

---

## Codebase Structure

### Root Directory Layout

```
KingdomsAtDusk/
├── Assets/
│   ├── Scripts/                     # 118 C# scripts
│   │   ├── Core/                    # Service Locator, EventBus, Utilities
│   │   ├── Managers/                # Game, Resource, Building, Happiness managers
│   │   ├── RTSBuildingsSystems/     # Building & wall placement systems
│   │   ├── Units/                   # Unit AI, combat, movement, selection
│   │   ├── UI/                      # HUD, minimap, menus, building UI
│   │   ├── Camera/                  # RTS camera controller
│   │   ├── FogOfWar/                # Custom fog of war system
│   │   └── Editor/                  # Custom editor tools
│   ├── Prefabs/                     # Building, unit, wall prefabs & data
│   ├── Scenes/                      # Game scenes
│   └── Settings/                    # URP render pipeline assets
│
├── StandalonePackages/              # 10 modular packages (reusable)
│   ├── building-system/
│   ├── event-system/
│   ├── happiness-system/
│   ├── resource-management/
│   ├── selection-system/
│   ├── service-locator/
│   ├── wall-system/
│   ├── time-system/
│   ├── rts-ui-system/
│   └── object-pooling/
│
├── ProjectSettings/                 # Unity configuration
├── Packages/                        # Unity package dependencies
└── [Documentation]/                 # 93 markdown files
```

### Key File Locations

**Core Infrastructure:**
- Services: `Assets/Scripts/Core/IServices.cs`
- Service Locator: `Assets/Scripts/Core/ServiceLocator.cs`
- Event System: `Assets/Scripts/Core/GameEvents.cs`, `Assets/Scripts/Core/EventBus.cs`

**Managers:**
- Game Manager: `Assets/Scripts/Managers/GameManager.cs`
- Resource Manager: `Assets/Scripts/Managers/ResourceManager.cs`
- Building Manager: `Assets/Scripts/Managers/BuildingManager.cs`
- Happiness Manager: `Assets/Scripts/Managers/HappinessManager.cs`

**Building System:**
- Building Component: `Assets/Scripts/RTSBuildingsSystems/Building.cs`
- Building Data: `Assets/Scripts/RTSBuildingsSystems/BuildingDataSO.cs`
- Wall Placement: `Assets/Scripts/RTSBuildingsSystems/WallPlacementController.cs`
- Wall Connections: `Assets/Scripts/RTSBuildingsSystems/WallConnectionSystem.cs`

**Unit System:**
- Unit AI: `Assets/Scripts/Units/AI/` (State machine)
- Unit Combat: `Assets/Scripts/Units/Components/UnitCombat.cs`
- Unit Health: `Assets/Scripts/Units/Components/UnitHealth.cs`

---

## Architecture & Patterns

### 1. Service Locator Pattern

**Purpose:** Dependency injection without tight coupling

```csharp
// Register services (in GameManager)
ServiceLocator.Register<IResourcesService>(resourceManager);

// Access services
var resources = ServiceLocator.TryGet<IResourcesService>();
if (resources == null)
{
    Debug.LogError("Service not available!");
    return;
}
```

**Available Services:**
- `IResourcesService` - Resource tracking & spending
- `IHappinessService` - Morale management
- `IBuildingService` - Building placement control (⚠️ NOT YET REGISTERED - see Known Issues)
- `IPoolService` - Object pooling
- `IPopulationService` - Population management
- `IReputationService` - Fame/reputation system
- `IPeasantWorkforceService` - Worker allocation
- `IGameStateService` - Game state management
- `ITimeService` - Day/night cycle

### 2. Event Bus / Pub-Sub Pattern

**Purpose:** Decoupled communication between systems

```csharp
// Define events in GameEvents.cs
public struct BuildingPlacedEvent
{
    public GameObject Building;
    public Vector3 Position;
}

// Subscribe (in OnEnable or Start)
EventBus.Subscribe<BuildingPlacedEvent>(OnBuildingPlaced);

// Publish
EventBus.Publish(new BuildingPlacedEvent { Building = gameObject, Position = transform.position });

// ALWAYS unsubscribe (in OnDestroy or OnDisable)
EventBus.Unsubscribe<BuildingPlacedEvent>(OnBuildingPlaced);
```

**Critical Event Categories:**
- Building Events: `BuildingPlacedEvent`, `BuildingCompletedEvent`, `BuildingDestroyedEvent`
- Resource Events: `ResourcesChangedEvent`, `ResourcesSpentEvent`
- Happiness Events: `HappinessChangedEvent`
- Unit Events

### 3. ScriptableObject Data Pattern

**Purpose:** Data-driven design, designer-friendly

```csharp
// Create data assets in Unity Editor
[CreateAssetMenu(fileName = "NewBuilding", menuName = "RTS/BuildingData")]
public class BuildingDataSO : ScriptableObject
{
    public string buildingName;
    public BuildingType buildingType;
    public int woodCost;
    public float constructionTime;
    // ... more configuration
}
```

**Asset Locations:**
- Building Data: `Assets/Prefabs/BuildingPrefabs&Data/`
- Wall Data: `Assets/Prefabs/WallPrefabs&Data/`

### 4. Component-Based Architecture

**Purpose:** Modular, reusable functionality

Units are composed of:
- `UnitMovement` - NavMesh-based pathfinding
- `UnitCombat` - Attack logic
- `UnitHealth` - Damage tracking
- `UnitSelectable` - Selection handling

Buildings use:
- `Building` - Core lifecycle
- `BuildingSelectable` - Selection
- Optional modules (WorkerModules)

### 5. State Machine Pattern (AI)

**Purpose:** Clear AI behavior transitions

```csharp
// AI States: Idle → Moving → Attacking → Retreating → Healing → Dead
public abstract class UnitState
{
    public abstract void Enter();
    public abstract void Update();
    public abstract void Exit();
}
```

**Specialized AI:**
- `SoldierAI` - Melee combat
- `ArcherAI` - Ranged combat with positioning
- `HealerAI` - Support role

### 6. Object Pool Pattern

**Purpose:** Performance optimization

```csharp
// Get from pool (instead of Instantiate)
var instance = poolService.Get(prefab);

// Return to pool (instead of Destroy)
poolService.Return(instance);
```

Used for: minimap markers, projectiles, visual effects

---

## Core Systems

### Building System

**How it Works:**
1. Player selects building from UI
2. `BuildingManager` validates resources and placement
3. Creates preview with valid/invalid visual feedback
4. On placement, instantiates building and starts construction
5. `Building` component manages construction timer
6. On completion, activates resource generation/happiness bonuses

**Key Classes:**
- `Building.cs` - Lifecycle management
- `BuildingDataSO.cs` - Configuration data
- `BuildingManager.cs` - Placement controller

**Integration Points:**
- Deducts resources via `IResourcesService`
- Publishes events via `EventBus`
- Applies happiness bonuses via `IHappinessService`

### Wall System

**Unique Feature:** Pole-to-pole placement

1. Click first pole position
2. Click second pole position
3. System automatically calculates segments between poles
4. Intelligent scaling to fill gaps
5. Overlap detection prevents intersecting walls
6. Connection detection links adjacent walls

**Key Classes:**
- `WallPlacementController.cs` - Pole-to-pole placement
- `WallConnectionSystem.cs` - Connection detection

### Resource System

**Resources:** Wood, Food, Gold, Stone

**Flow:**
1. Buildings generate resources over time
2. `ResourceManager` tracks all resources
3. Construction/training spends resources
4. UI updates on `ResourcesChangedEvent`

**Usage Pattern:**
```csharp
var costs = ResourceCost.Build()
    .Wood(100)
    .Stone(50)
    .Create();

if (resourceService.CanAfford(costs))
{
    resourceService.SpendResources(costs);
}
```

### Unit AI System

**State Machine Flow:**
```
Idle → (enemy detected) → Moving → (in range) → Attacking
  ↑                                                  ↓
  ←──────────────── (health low) ← Retreating ←─────┘
```

**Performance Optimizations:**
- Update interval throttling (0.5s default)
- Max updates per frame limiting
- Layer mask filtering for detection

### Fog of War System

**Grid-based visibility:**
- Three states: Unexplored (black) → Explored (dimmed) → Visible (clear)
- Vision providers on units and buildings
- URP shader-based rendering
- Configurable cell size (2.0 units recommended)

**Performance:** 8-10% CPU at recommended settings

### Combat System

**Components:**
- `UnitHealth` - Damage tracking, death handling
- `UnitCombat` - Attack logic, cooldowns, target selection
- `TowerCombat` - Building-based defense (separate from unit combat)

**Pattern:**
```csharp
// Find targets
Collider[] enemies = Physics.OverlapSphere(position, range, enemyLayers);

// Apply damage
target.GetComponent<UnitHealth>().TakeDamage(damage, attacker);

// Publish event for VFX/audio
EventBus.Publish(new AttackEvent { ... });
```

---

## Development Conventions

### Naming Conventions

**Scripts:**
- Managers: `{Domain}Manager.cs` (e.g., `ResourceManager.cs`)
- Services: `I{Domain}Service` interface (e.g., `IResourcesService`)
- ScriptableObjects: `{Name}SO` or `{Name}DataSO` (e.g., `BuildingDataSO`)
- Components: `Unit{Function}` or `Building{Function}` (e.g., `UnitMovement`)
- Events: `{Subject}{Action}Event` (e.g., `BuildingPlacedEvent`)

**Namespaces:**
```csharp
RTS.Core.Services       // Service interfaces & ServiceLocator
RTS.Core.Events         // Event definitions & EventBus
RTS.Buildings           // Building system
RTS.Units               // Unit system
RTS.Units.AI            // AI subsystem
RTS.Managers            // Manager layer
```

### Code Organization

**Service Registration Order (in GameManager):**
1. Object Pool (core dependency)
2. Game State Service
3. Resource & Happiness systems
4. Optional systems (population, reputation, workforce)
5. Building management

**Event Subscription Pattern:**
```csharp
// Always pair Subscribe in OnEnable/Start with Unsubscribe in OnDestroy/OnDisable
private void OnEnable()
{
    EventBus.Subscribe<ResourcesChangedEvent>(OnResourcesChanged);
}

private void OnDestroy()
{
    EventBus.Unsubscribe<ResourcesChangedEvent>(OnResourcesChanged);
}
```

**Service Access Pattern:**
```csharp
// Always use TryGet and null-check
var service = ServiceLocator.TryGet<IResourcesService>();
if (service == null)
{
    Debug.LogError("ResourcesService not available!");
    return;
}
```

### Performance Patterns

**1. Update Throttling:**
```csharp
// Don't update every frame - use intervals
float updateTimer = 0f;
void Update()
{
    updateTimer += Time.deltaTime;
    if (updateTimer >= updateInterval)
    {
        updateTimer = 0f;
        DoExpensiveOperation();
    }
}
```

**2. Event-Based Updates (Preferred over Update()):**
```csharp
// Instead of checking affordability every frame:
void Update() { CheckAffordability(); } // BAD - 60 checks/second

// Listen to resource change events:
void OnResourcesChanged(ResourcesChangedEvent evt) { CheckAffordability(); } // GOOD - only when needed
```

**3. Delayed Initialization:**
```csharp
// Avoid cascading updates with Invoke
Invoke(nameof(UpdateConnections), 0.05f);
```

### Data-Driven Design Philosophy

- Building types configured in `BuildingDataSO` assets
- AI behavior configured in `AISettingsSO`
- Resource costs defined in data, not hardcoded
- Performance settings exposed via ScriptableObjects

---

## Common Tasks

### Adding a New Building

1. **Create Building Prefab:**
   - Add `Building` component
   - Configure visual model
   - Add collider for placement validation

2. **Create BuildingDataSO Asset:**
   ```
   Right-click → Create → RTS → BuildingData
   ```
   - Set name, type, costs
   - Set construction time
   - Configure resource generation (if any)
   - Assign prefab reference

3. **Add to BuildingManager:**
   - Add asset to BuildingManager's building list in Inspector

4. **Test:**
   - Verify placement
   - Check resource deduction
   - Verify construction timer
   - Check completion effects

### Adding a New Resource Type

⚠️ **WARNING:** Requires changes across multiple systems

1. **Add to ResourceType enum** (`Core/IServices.cs`)
2. **Update ResourceManager** to track new resource
3. **Update UI displays** (ResourceUI, building tooltips)
4. **Update cost builders** (ResourceCost class)
5. **Update all ResourcesChangedEvent handlers**
6. **Add resource icons/sprites**

### Implementing Tower Combat

See `QUICK_REFERENCE.md` for complete code example.

**Quick Steps:**
1. Create `TowerCombat.cs` script
2. Subscribe to `BuildingCompletedEvent`
3. Implement target finding (OverlapSphere)
4. Apply damage via `UnitHealth.TakeDamage()`
5. Publish events for VFX/audio feedback

### Adding a Custom Editor Tool

Examples in `Assets/Scripts/Editor/`:
- `MenuSetupTool.cs` - Menu configuration
- `BuildingSpawnPointEditor.cs` - Visual spawn point placement

Pattern:
```csharp
using UnityEditor;

public class MyEditorTool : EditorWindow
{
    [MenuItem("RTS Tools/My Tool")]
    public static void ShowWindow()
    {
        GetWindow<MyEditorTool>("My Tool");
    }
}
```

### Exporting a Standalone Package

Use `PackageExporterTool` (Window → RTS Tools → Package Exporter)

Structure:
```
StandalonePackages/{package-name}/
├── package.json
├── Runtime/
├── Documentation~/
└── Samples~/
```

---

## Testing & Debugging

### Debug Tools

**Performance Monitor (Press F3 in-game):**
- FPS (current, average, min, max)
- Frame time (ms)
- Memory usage (allocated, reserved, mono heap, GC)
- GPU information
- Shadow settings
- Scene statistics

**Diagnostics:**
- `StartupDiagnostics.cs` - Logs service registration status
- `BuildDiagnostics.cs` - Logs build initialization

**AI Debug Visualization:**
```csharp
// In AISettingsSO asset:
showDebugGizmos: true    // Visual debug in Scene view
logStateChanges: false   // Log AI state transitions
```

**Fog of War Debug:**
```csharp
// In FogOfWarConfig:
enableDebugVisualization: false  // Show grid in editor
```

### Common Debug Patterns

**Check if Service is Registered:**
```csharp
var service = ServiceLocator.TryGet<IResourcesService>();
if (service == null)
{
    Debug.LogError("Service not registered! Check GameManager initialization.");
}
```

**Verify Event Subscriptions:**
```csharp
// Add debug logs to confirm events fire
private void OnBuildingPlaced(BuildingPlacedEvent evt)
{
    Debug.Log($"Building placed: {evt.Building.name} at {evt.Position}");
}
```

**Monitor Resource Changes:**
```csharp
EventBus.Subscribe<ResourcesChangedEvent>(evt =>
{
    Debug.Log($"Resources changed: Wood {evt.WoodDelta}, Food {evt.FoodDelta}");
});
```

### Manual Test Checklist

After major changes:
- [ ] All building buttons show correct costs
- [ ] Affordability updates when resources change
- [ ] Building placement works (valid/invalid preview)
- [ ] Construction timer completes
- [ ] Resource generation works
- [ ] Walls connect properly
- [ ] Unit AI transitions between states
- [ ] Combat damage is applied
- [ ] Fog of war updates
- [ ] Minimap markers appear
- [ ] No errors in console

---

## Known Issues & Technical Debt

### Critical Issues

**1. BuildingManager Not Registered as Service**
- **Impact:** UI components use `FindObjectOfType` fallback (slow)
- **Location:** `Assets/Scripts/Managers/GameManager.cs`
- **Fix:** Register `BuildingManager` as `IBuildingService`
- **Estimated Time:** 1 hour

**2. Redundant UI Systems**
- **Issue:** Both `BuildingUI.cs` and `BuildingHUD.cs` exist (~70% overlap)
- **Impact:** Confusion, duplicated maintenance
- **Fix:** Deprecate `BuildingUI.cs`, use `BuildingHUD.cs` only
- **Estimated Time:** 2-3 hours

**3. Duplicated GetCostString() Method**
- **Issue:** Same cost formatting logic in 5+ files
- **Impact:** Inconsistent displays, maintenance nightmare
- **Fix:** Create central `ResourceDisplayUtility` class
- **Estimated Time:** 2 hours

### Medium Priority Issues

**4. Update() Loops for Affordability Checking**
- **Issue:** Checking affordability every frame instead of on resource change events
- **Impact:** Performance overhead (600 checks/second at 60fps for 10 buttons)
- **Fix:** Move checks to `ResourcesChangedEvent` handler only
- **Estimated Time:** 1 hour

**5. Missing Stone Field in ResourcesSpentEvent**
- **Issue:** Constructor accepts stone parameter but doesn't store it
- **Location:** `Assets/Scripts/Core/GameEvents.cs:24-38`
- **Impact:** Stone costs not tracked in events
- **Fix:** Add `public int Stone;` field and assign in constructor
- **Estimated Time:** 15 minutes

**6. Heavy Use of FindObjectOfType**
- **Issue:** Many scripts use `FindObjectOfType` instead of ServiceLocator
- **Impact:** Performance overhead, tight coupling
- **Fix:** Register managers as services, access via ServiceLocator
- **Estimated Time:** 2 hours

### Technical Debt Summary

**Estimated Total Refactoring:** 16-24 hours

**See:** `CODEBASE_REFACTORING_REPORT.md` for complete analysis (47 issues across 6 categories)

**Recommended Refactoring Order:**
1. Register BuildingManager as service (P0)
2. Merge BuildingUI/HUD systems (P0)
3. Centralize cost display logic (P1)
4. Event-based affordability checks (P1)
5. Fix ResourcesSpentEvent (P2)
6. Reduce FindObjectOfType usage (P2)

---

## Performance Considerations

### Build Configuration

**Initialization Scripts:**
- `BuildInitializer.cs` - Runs before scene load
  - Disables VSync
  - Sets target frame rate (300 FPS)
  - Forces discrete GPU on laptops
  - Enables texture streaming

- `ShaderPreloader.cs` - Prevents runtime shader compilation
  - Pre-warms all shaders
  - Pre-loads critical materials
  - Prevents first-frame black screen

### Quality Settings

**Performance Profiles:**
- **Low:** 30-60 FPS target, minimal shadows
- **Medium:** 60 FPS, soft shadows, basic post-processing
- **High:** 60+ FPS, 4 shadow cascades
- **Ultra:** Maximum quality for high-end GPUs

### Unity Configuration

**Key Settings:**
- Unity Version: 6000.2.10f1
- Render Pipeline: URP 17.2.0
- Scripting Backend: Mono
- Input System: New Input System 1.14.2
- NavMesh: AI Navigation 2.0.9

**Job System (boot.config):**
```
job-worker-count=4          # Should be CPU cores - 1
background-job-worker-count=2
gc-helper-count=1
```

### Optimization Tips for AI Assistants

**When Adding New Features:**

1. **Use Object Pooling** for frequently instantiated objects
2. **Throttle Updates** - Don't run expensive operations every frame
3. **Use Events** instead of polling/Update() loops
4. **Cache References** - Don't call `GetComponent` repeatedly
5. **Layer Masks** - Use for filtering physics queries
6. **Async Operations** - Consider for long-running tasks

**Performance Red Flags:**
- ❌ `FindObjectOfType` in Update()
- ❌ GetComponent() every frame
- ❌ String concatenation in Update()
- ❌ Instantiate/Destroy in tight loops
- ❌ Physics.OverlapSphere without layer mask

---

## Quick Reference

### File Path Quick Access

**Most Frequently Edited:**
```
Core Systems:
  Assets/Scripts/Core/IServices.cs
  Assets/Scripts/Core/GameEvents.cs
  Assets/Scripts/Managers/GameManager.cs

Building System:
  Assets/Scripts/RTSBuildingsSystems/Building.cs
  Assets/Scripts/RTSBuildingsSystems/BuildingDataSO.cs
  Assets/Scripts/Managers/BuildingManager.cs

Unit System:
  Assets/Scripts/Units/AI/SoldierAI.cs
  Assets/Scripts/Units/Components/UnitCombat.cs
  Assets/Scripts/Units/Components/UnitHealth.cs

Resources:
  Assets/Scripts/Managers/ResourceManager.cs
  Assets/Scripts/UI/ResourceUI.cs
```

### Code Snippets

**Access Resources:**
```csharp
var resources = ServiceLocator.TryGet<IResourcesService>();
int wood = resources.GetResource(ResourceType.Wood);
```

**Check Affordability:**
```csharp
var costs = ResourceCost.Build().Wood(100).Stone(50).Create();
if (resources.CanAfford(costs))
{
    resources.SpendResources(costs);
}
```

**Subscribe to Building Events:**
```csharp
EventBus.Subscribe<BuildingCompletedEvent>(OnBuildingCompleted);
// Always unsubscribe in OnDestroy!
```

**Place a Building:**
```csharp
buildingManager.StartPlacingBuilding(buildingIndex);
```

### Essential Documentation Files

- `QUICK_REFERENCE.md` - Code snippets and API usage
- `DOCUMENTATION_INDEX.md` - Index of all 93 documentation files
- `CODEBASE_REFACTORING_REPORT.md` - Known technical debt
- `FOG_OF_WAR_SETUP_GUIDE.md` - Fog of war system setup
- `WALL_SYSTEM_GUIDE.md` - Wall placement implementation
- `BUILD_ISSUES_FIX_GUIDE.md` - Build troubleshooting

---

## Best Practices for AI Assistants

### When Implementing Features

1. **Read Existing Code First**
   - NEVER propose changes to code you haven't read
   - Understand existing patterns before suggesting modifications
   - Check for similar existing implementations

2. **Follow Existing Patterns**
   - Use Service Locator for dependencies
   - Use EventBus for communication between systems
   - Use ScriptableObjects for configuration
   - Follow namespace conventions

3. **Avoid Over-Engineering**
   - Only make changes that are directly requested
   - Don't add "improvements" beyond scope
   - Don't add features, refactoring, or cleanup unless asked
   - Don't add comments/docstrings to unchanged code

4. **Test Thoroughly**
   - Verify building placement works
   - Check resource deduction
   - Ensure events fire correctly
   - Test edge cases (insufficient resources, invalid placement)

5. **Document Complex Logic**
   - Explain non-obvious algorithms (e.g., wall segmentation)
   - Document integration points
   - Note performance implications

### When Reviewing Code

**Check For:**
- ✅ Services accessed via ServiceLocator (not FindObjectOfType)
- ✅ Events subscribed AND unsubscribed
- ✅ Null checks for service access
- ✅ Performance considerations (throttling, pooling, caching)
- ✅ Follows naming conventions
- ✅ Uses existing patterns (don't introduce new patterns unnecessarily)

### When Debugging

**Investigation Steps:**
1. Check if services are registered (GameManager initialization)
2. Verify event subscriptions (are events firing?)
3. Check resource availability
4. Verify layer masks for physics queries
5. Check console for errors/warnings
6. Use Debug tools (F3 performance monitor)

---

## Git Workflow

### Branch Strategy

- **Main Branch:** Stable, production-ready code
- **Feature Branches:** `claude/{feature-name}-{session-id}`
- **Development:** Work on feature branches, merge to main via PR

### Commit Guidelines

**Good Commit Messages:**
```
✅ Add tower combat system with target detection
✅ Fix ResourcesSpentEvent missing Stone field
✅ Refactor BuildingUI to use ServiceLocator
```

**Bad Commit Messages:**
```
❌ Update files
❌ Fix bug
❌ WIP
```

**Pattern:**
- Focus on "why" rather than "what"
- Be concise (1-2 sentences)
- Use imperative mood ("Add feature" not "Added feature")

### Pre-Commit Checklist

- [ ] Code compiles without errors
- [ ] No new warnings in console
- [ ] Manual testing completed
- [ ] Follows existing patterns
- [ ] Documentation updated if needed
- [ ] No commented-out code left behind

---

## Additional Resources

### Documentation Index

The repository contains 93 markdown files with detailed documentation:

**System-Specific Guides:**
- Building System: `BUILDING_SYSTEM_SETUP.md`
- Wall System: `WALL_SYSTEM_GUIDE.md`, `POLE_TO_POLE_WALL_SYSTEM.md`
- Fog of War: `FOG_OF_WAR_SETUP_GUIDE.md`, `FOG_OF_WAR_ARCHITECTURE.md`
- Tower System: `TOWER_SYSTEM_README.md`
- Animation: `Assets/RTSAnimation/ANIMATION_SYSTEM_GUIDE.md`
- Minimap: `Assets/Scripts/UI/Minimap/SETUP_GUIDE.md`

**Architecture & Analysis:**
- `GAME_SYSTEMS_ANALYSIS.md`
- `ACTUAL_CODE_PATTERNS.md`
- `CODEBASE_REFACTORING_REPORT.md`

**Quick References:**
- `QUICK_REFERENCE.md` - Code snippets
- `FILE_REFERENCE_QUICK_GUIDE.md` - File locations

**Troubleshooting:**
- `BUILD_ISSUES_FIX_GUIDE.md`
- `FOG_OF_WAR_TROUBLESHOOTING.md`
- `BUILDING_DETAILS_TROUBLESHOOTING.md`

### Unity Package Documentation

Each standalone package has documentation:
```
StandalonePackages/{package-name}/
├── README.md
└── Documentation~/Documentation.md
```

### External Assets

**Third-Party:**
- AOSFogWar - Fog of war system (customized)
- Polytope Studio - Low-poly environment
- SimpleToon - Toon shading
- StoneKeep - Medieval building assets
- UltimateRtsFantasy - RTS unit assets

---

## Contact & Support

**Issues:** Report at https://github.com/CryptoRabea/KingdomsAtDusk/issues

**Documentation:** All docs in root directory and subsystem folders

**Last Updated:** 2025-11-24

---

**END OF CLAUDE.MD**
