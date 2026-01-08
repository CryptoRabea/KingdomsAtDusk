# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Kingdoms at Dusk** is a Unity 6 (6000.3.2f1) RTS game featuring medieval kingdom building, unit combat, and wave-based survival mechanics. The project uses URP (Universal Render Pipeline) and is structured around a professional service-oriented architecture.

## Development Commands

### Opening the Project
- Open the project in Unity Hub using Unity 6000.3.x
- The solution file is `KingdomsAtDusk.slnx` (Visual Studio 2022+ format)

### Building
- Use Unity's Build Settings (File > Build Settings)
- Target platform configurations are in `ProjectSettings/`
- Build diagnostics available via `BuildDiagnostics` class

### Testing
- Unity Test Framework is included (`com.unity.test-framework`)
- Tests should be placed in `Assets/Tests/` directories
- Run tests via Unity Test Runner (Window > General > Test Runner)

### Code Generation
- Unity auto-generates `.csproj` files - do not manually edit these
- Regenerate project files: Assets > Open C# Project (if sync issues occur)

## Core Architecture

### Service Locator Pattern (`Assets/Scripts/Core/ServiceLocator.cs`)

Central dependency injection system avoiding singleton abuse:

```csharp
// Register a service (typically in manager's Awake/Start)
ServiceLocator.Register<IResourcesService>(this);

// Retrieve a service from anywhere
var resources = ServiceLocator.Get<IResourcesService>();

// Check if registered
if (ServiceLocator.IsRegistered<IResourcesService>()) { }
```

**Key Services:**
- `IResourcesService` - Wood, Food, Gold, Stone management
- `IPopulationService` - Population, housing, peasant allocation
- `IHappinessService` - Morale system
- `IReputationService` - Kingdom fame
- `IBuildingService` - Building placement and management
- `IPoolService` - Object pooling
- `ITimeService` - Game time and day/night cycle
- `ISettingsService` - Game settings

### Event Bus System (`Assets/Scripts/Core/Events/EventBus.cs`)

Decoupled publish-subscribe communication using struct events (zero GC):

```csharp
// Subscribe (typically in OnEnable)
EventBus.Subscribe<ResourcesChangedEvent>(OnResourcesChanged);

// Unsubscribe (typically in OnDisable)
EventBus.Unsubscribe<ResourcesChangedEvent>(OnResourcesChanged);

// Publish
EventBus.Publish(new ResourcesChangedEvent(ResourceType.Wood, 100));
```

**Event Categories:**
- Resources: `ResourcesChangedEvent`, `ResourceNodeDepletedEvent`
- Buildings: `BuildingPlacedEvent`, `BuildingDestroyedEvent`, `ConstructionStartedEvent`
- Units: `UnitSpawnedEvent`, `UnitDiedEvent`, `UnitSelectedEvent`
- Combat: `UnitAttackedEvent`, `DamageTakenEvent`
- Waves: `WaveStartedEvent`, `WaveCompletedEvent`
- Population: `PopulationChangedEvent`, `HappinessChangedEvent`

**Critical:** UI never directly accesses game logic - all updates are event-driven.

## Major Game Systems

### Pathfinding System

**Current Active: Unity NavMesh**
- Units currently use Unity's built-in NavMesh system (`com.unity.ai.navigation`)
- `NavMeshAgent` components on units for pathfinding
- NavMesh baking includes terrain and building obstacles

**Planned Migration: Flow Field Pathfinding (`Assets/Scripts/FlowField/`)**

Flow field system code exists but is not yet integrated. This is a major todo item for future implementation to improve performance with large unit groups:

**Key Components:**
- `FlowFieldManager` - Singleton managing grid and flow field caching
- `FlowFieldGrid` - Grid with cost/integration/flow fields
- `FlowFieldGenerator` - Dijkstra + gradient descent pathfinding
- `FlowFieldFollower` - Unit component sampling flow directions

**Multi-File Understanding Required:**
1. Grid stores cost field (obstacles), integration field (distances from goal), flow field (movement directions)
2. Generator runs Dijkstra to compute integration costs
3. Flow field calculated as gradient of integration field
4. Units sample flow direction at their position using bilinear interpolation
5. Manager caches flow fields for same destination to avoid recomputation

**Planned Benefits:**
- Better performance for large unit groups moving to same destination
- More natural group movement and formations
- Flow field caching reduces pathfinding computation
- Bilinear interpolation for smooth movement
- Multi-goal support for formations

**Note:** When migrating to flow fields, units will need `FlowFieldFollower` component instead of `NavMeshAgent`.

### Unit AI State Machine (`Assets/Scripts/Units/AI/`)

**Architecture:**
- `UnitAIController` - Coordinates Health, Movement, Combat components
- `UnitState` - Abstract base for state pattern
- Concrete states: `IdleState`, `MovingState`, `AttackingState`, `RetreatState`, `DeadState`, `ReturningToOriginState`

**State Transitions:**
```
Idle → Moving (move command) → Attacking (enemy in range) → Retreat (low health)
Attacking → ReturningToOrigin (max chase distance exceeded) → Idle (returned)
```

**AI Behavior Types:**
- `Aggressive` - Targets nearest enemy
- `Defensive` - Targets weakest enemy (lowest health)
- `Support` - Heals allied units

**Aggro System:**
- Units track aggro origin position
- Max chase distance prevents infinite pursuit
- Return-to-origin state brings unit back after chase limit
- Forced moves (player commands) override AI behavior

### Selection & Command System (`Assets/Scripts/Units/Selection/`)

**Multi-System Pipeline:**
1. `UnitSelectionManager` - Box selection, double/triple-click selection, control groups (Ctrl+1-9)
2. `RTSCommandHandler` - Right-click move/attack commands
3. `FormationManager` - Calculates unit positions for formations (Box, Line, Wedge, etc.)
4. `NavMeshAgent` - Currently handles pathfinding and movement (will be replaced by flow fields eventually)

**Performance Optimizations:**
- Cached unit references (avoids FindObjectsByType every frame)
- NonAlloc raycasts
- UI detection to prevent command issues
- Max selection limits

### Building System (`Assets/Scripts/RTSBuildingsSystems/`)

**Core Components:**
- `Building` - Base component with construction progress, resource generation, happiness bonuses
- `BuildingHealth` - Damage and destruction
- `BuildingSelectionManager` - Separate selection system from units

**Special Building Types:**
- **Wall System** - Auto-connects wall segments, supports gates and stairs
- **Towers** - Defensive structures with unit spawning
- **Stronghold** - Main base building (defeat condition if destroyed)
- **Campfire** - Happiness bonus with area-of-effect
- **Gates** - Automatic opening/closing with animation

**Worker System:**
- `BuildingWorkerModule` - Peasant allocation for construction
- `PeasantWorkforceManager` - Global peasant distribution
- Workers can be in gathering mode or auto-resource generation mode

**Construction Visuals:**
- `FadeInConstructionVisual` - Alpha fade
- `GroundUpConstructionVisual` - Vertical build animation
- `ScaffoldingConstructionVisual` - Scaffolding appearance
- `ParticleAssemblyConstructionVisual` - Particle effects

### Fog of War (`Assets/Scripts/FogOfWar/`)

**Implementation:**
- `FogOfWarGrid` - Grid-based vision (Unexplored/Explored/Visible states)
- `FogOfWarView` - Integration with RTS via EventBus
- `FogRevealerConfig` - Per-entity sight range configuration

**Automatic Integration:**
- Listens to `UnitSpawnedEvent` and `BuildingPlacedEvent`
- Automatically registers units/buildings as fog revealers
- Circular reveal using square grid for efficient updates
- Sight ranges configurable per unit/building type via ScriptableObjects

### Formation System (`Assets/Scripts/Units/Formation/`)

**Formation Types:**
- Box, Line, Column, Wedge, Circle
- Custom formations via `CustomFormationData`

**Formation Flow:**
1. Player selects units and right-clicks destination
2. `RTSCommandHandler` calls `FormationManager.CalculateFormationPositions()`
3. Formation positions validated for NavMesh walkability
4. NavMeshAgents move units to their formation positions
5. Units maintain formation shape during movement

## Namespace Organization

```
RTS.Core.Services     - ServiceLocator and service interfaces
RTS.Core.Events       - EventBus and event structs
RTS.Buildings         - Building components and data
RTS.Units             - Unit components (Health, Movement, Combat, Selectable)
RTS.Units.AI          - AI controller and state machines
RTS.Units.Formation   - Formation calculation
RTS.Managers          - Global managers (Resources, Population, etc.)
FlowField.Core        - Flow field grid and generation
FlowField.Movement    - Unit movement via flow fields
KingdomsAtDusk.FogOfWar - Fog of war system
```

## Component-Based Design

**Units are composed of:**
- `UnitHealth` - HP, damage, death
- `UnitMovement` - Movement speed, rotation
- `UnitCombat` - Attack damage, range, cooldown
- `UnitSelectable` - Selection system integration
- `UnitAIController` - AI behavior coordination
- `NavMeshAgent` - Pathfinding and movement (Unity component)

**Buildings are composed of:**
- `Building` - Construction, resources, happiness
- `BuildingHealth` - HP and destruction
- `BuildingSelectable` - Selection system
- `BuildingNavMeshObstacle` - Pathfinding obstacles

## Data-Driven Configuration

Use ScriptableObjects extensively:

- `UnitConfigSO` - Unit stats, AI behavior, animations
- `BuildingDataSO` - Building costs, construction time, benefits
- `AISettingsSO` - AI behavior parameters
- `FormationConfigSO` - Formation spacing and validation
- `CampfireDataSO`, `GateDataSO` - Specialized building data

**Creating New Units/Buildings:**
1. Create ScriptableObject asset (Right-click > Create > RTS > Unit/Building Config)
2. Configure stats and references
3. Reference in spawner or building placement system

## Common Patterns

### Adding a New Event
```csharp
// 1. Define struct in Assets/Scripts/Core/Events/
namespace RTS.Core.Events
{
    public struct MyNewEvent
    {
        public int SomeData;
        public MyNewEvent(int data) { SomeData = data; }
    }
}

// 2. Publish from game logic
EventBus.Publish(new MyNewEvent(42));

// 3. Subscribe in UI or other systems
EventBus.Subscribe<MyNewEvent>(OnMyNewEvent);
```

### Accessing Services
```csharp
// Always use ServiceLocator, never FindObjectOfType
var resources = ServiceLocator.Get<IResourcesService>();
resources.AddResource(ResourceType.Wood, 50);
```

### Unit State Machine Extension
```csharp
// Create new state inheriting from UnitState
public class MyCustomState : UnitState
{
    public override void Enter() { }
    public override void Execute() { }
    public override void Exit() { }
}

// Transition in UnitAIController or other state
aiController.ChangeState(new MyCustomState(aiController));
```

## Performance Considerations

- Use `NonAlloc` variants for Physics raycasts/overlaps
- Cache component references in `Awake()`/`Start()`
- Avoid `FindObjectOfType` or `FindObjectsByType` in `Update()`
- Use object pooling via `IPoolService` for frequently spawned objects
- Events are struct-based for zero GC allocation
- Flow field caching reduces pathfinding computation

## Third-Party Integrations

**Major Assets:**
- Malbers Animal Controller - Animal AI and animations
- Enviro 3 - Sky, weather, day/night cycle
- The Vegetation Engine - Foliage rendering
- Volumetric Fog 2 - Atmospheric fog

**Key Unity Packages:**
- Input System (`com.unity.inputsystem`) - New input system
- Cinemachine (`com.unity.cinemachine`) - Camera control
- AI Navigation (`com.unity.ai.navigation`) - NavMesh (fallback for single units)
- Universal RP (`com.unity.render-pipelines.universal`) - Rendering pipeline

## File Locations

- Game logic: `Assets/Scripts/`
- UI: `Assets/Scripts/UI/`
- Managers: `Assets/Scripts/Managers/`
- Editor tools: `Assets/Scripts/Editor/`
- Unit prefabs: `Assets/Prefabs/Units/`
- Building prefabs: `Assets/Prefabs/Buildings/`
- ScriptableObject configs: `Assets/Data/`

## Major Todo Items

- **Flow Field Migration** - Migrate from NavMesh to flow field pathfinding system for better large-group performance

## Git Workflow

This project uses standard Git workflow:
- Main branch: `main`
- Feature branches follow convention: `feature/description` or `claude/description`
- Recent work focuses on fog of war improvements
