# Kingdoms at Dusk - Complete Game Systems Analysis

## Executive Summary
The game is built with a modular, service-oriented architecture using C# with Unity 2021.3+ (currently 6000.2.10f1). All systems use the Event Bus for decoupled communication and Service Locator for dependency injection.

---

## CORE INFRASTRUCTURE SYSTEMS

### 1. **Service Locator System**
**Purpose:** Centralized dependency injection and service management

**Files:**
- `/Assets/Scripts/Core/ServiceLocator.cs` (110 lines)

**Key Features:**
- Dictionary-based service registry
- Type-safe generic registration/retrieval
- TryGet() for safe lookups
- Clear() for cleanup
- Runtime initialization with domain reload handling

**Dependencies:** None (Core system)
**External Dependencies:** Unity Engine

**Interfaces Managed:**
- IResourcesService
- IHappinessService
- IPoolService
- ITimeService
- IGameStateService

---

### 2. **Event Bus System**
**Purpose:** Publish-subscribe event system for decoupled communication

**Files:**
- `/Assets/Scripts/Core/EventBus.cs` (80+ lines)
- `/Assets/Scripts/Core/GameEvents.cs` (348 lines - Event definitions)

**Key Features:**
- Static publish/subscribe pattern
- Generic event handling with delegates
- Event subscription management
- Type-safe event publishing

**Event Categories:**
1. Resource Events (8 events):
   - ResourcesChangedEvent
   - ResourcesSpentEvent

2. Happiness Events (2 events):
   - HappinessChangedEvent
   - BuildingBonusChangedEvent

3. Building Events (6 events):
   - BuildingPlacedEvent
   - BuildingCompletedEvent
   - BuildingDestroyedEvent
   - ResourcesGeneratedEvent
   - ConstructionProgressEvent

4. Unit Events (5 events):
   - UnitSpawnedEvent
   - UnitDiedEvent
   - UnitHealthChangedEvent
   - UnitStateChangedEvent

5. Combat Events (2 events):
   - DamageDealtEvent
   - HealingAppliedEvent

6. Wave Events (2 events):
   - WaveStartedEvent
   - WaveCompletedEvent

7. Selection Events (5 events):
   - UnitSelectedEvent
   - UnitDeselectedEvent
   - SelectionChangedEvent
   - BuildingSelectedEvent
   - BuildingDeselectedEvent

8. Unit Training Events (3 events):
   - UnitTrainingStartedEvent
   - UnitTrainingCompletedEvent
   - TrainingProgressEvent

**Dependencies:** None (Core system)
**External Dependencies:** Unity Engine

---

### 3. **Object Pool System**
**Purpose:** Memory-efficient object reuse for frequently created/destroyed objects

**Files:**
- `/Assets/Scripts/Core/ObjectPool.cs`

**Dependencies:** IPoolService interface
**External Dependencies:** Unity Engine

---

## MANAGER SYSTEMS

### 4. **Game Manager System**
**Purpose:** Entry point and service initialization orchestrator

**Files:**
- `/Assets/Scripts/Managers/GameManager.cs` (197 lines)
- Contains: GameManager class + GameStateService implementation

**Manages:**
- Singleton lifecycle (DontDestroyOnLoad)
- Service initialization order
- Game state transitions (MainMenu, Playing, Paused, GameOver, Victory)
- Time scale control

**Initialization Order:**
1. ObjectPool
2. GameStateService
3. ResourceManager
4. HappinessManager

**Dependencies:**
- ResourceManager
- HappinessManager
- ObjectPool
- IGameStateService

**External Dependencies:**
- UnityEngine
- RTS.Core.Services
- RTS.Core.Pooling

---

### 5. **Resource Management System**
**Purpose:** Track and manage player resources (Wood, Food, Gold, Stone)

**Files:**
- `/Assets/Scripts/Managers/ResourceManager.cs` (179 lines)
- `/Assets/Scripts/Core/IServices.cs` (ResourceType enum + IResourcesService interface)

**Key Features:**
- Data-driven design with ResourceType enum
- Dictionary-based dynamic storage
- GetResource(), CanAfford(), SpendResources(), AddResources()
- Event-driven notifications
- Configurable starting amounts
- ResourceCost helper class for building cost definitions

**Supported Resources:**
- Wood
- Food
- Gold
- Stone
(Easily extensible with commented examples for Iron, Mana, Population)

**Starting Amounts (Configurable):**
- Wood: 100
- Food: 100
- Gold: 50
- Stone: 50

**Dependencies:**
- EventBus (publishes ResourcesChangedEvent, ResourcesSpentEvent)

**External Dependencies:**
- UnityEngine
- RTS.Core.Events
- RTS.Core.Services

---

### 6. **Happiness Management System**
**Purpose:** Track citizen morale affecting gameplay

**Files:**
- `/Assets/Scripts/Managers/HappinessManager.cs` (112 lines)

**Key Features:**
- Base happiness + building bonuses - tax penalties formula
- Tax level system (0-100%)
- Building happiness bonuses/penalties
- Periodic update system (0.5s intervals)
- Event-driven notifications

**Configurable Parameters:**
- baseHappiness: 50f
- startingTaxLevel: 10f
- happinessUpdateInterval: 0.5f

**Dependencies:**
- EventBus (publishes HappinessChangedEvent, BuildingBonusChangedEvent)
- IHappinessService interface

**External Dependencies:**
- UnityEngine
- RTS.Core.Services
- RTS.Core.Events

---

### 7. **Building Manager System**
**Purpose:** Handle building placement, validation, and lifecycle

**Files:**
- `/Assets/Scripts/Managers/BuildingManager.cs` (612 lines)

**Key Features:**
- Building data source of truth (BuildingDataSO array)
- Preview placement with material feedback (green/red)
- Grid snapping system
- Terrain validation (slope checking, height difference)
- Collision detection (overlap checks)
- Resource cost validation before placement
- Building prefab instantiation
- Placement info UI

**Configuration:**
- Grid size: 1f
- Max height difference: 2f
- Ground samples: 5
- Ground sample points (center + 4 corners)

**Validation Checks:**
1. Building data validation
2. Prefab assignment
3. Building component presence
4. Cost validation
5. Ground suitability
6. Collision detection

**Public API:**
- StartPlacingBuilding(int/BuildingDataSO)
- CancelPlacement()
- IsPlacing property
- CurrentBuildingData property
- GetAllBuildingData()
- CanAffordBuilding()
- GetBuildingCost()
- GetBuildingsByType()
- GetBuildingByName()

**Dependencies:**
- BuildingManager (self)
- IResourcesService
- BuildingDataSO
- Building component
- EventBus (publishes BuildingPlacedEvent)

**External Dependencies:**
- UnityEngine
- UnityEngine.InputSystem
- RTS.Core.Services
- RTS.Core.Events
- RTS.Buildings
- System.Collections.Generic
- System.Linq

---

### 8. **Wave Manager System**
**Purpose:** Enemy spawning and wave progression

**Files:**
- `/Assets/Scripts/Managers/WaveManager.cs` (100+ lines)

**Key Features:**
- Wave configuration via WaveConfigSO
- Procedural wave generation for infinite mode
- Enemy spawning with intervals
- Difficulty scaling
- Active enemy tracking
- Object pooling integration
- Event-driven notifications

**Configuration:**
- baseEnemyCount: 3
- enemiesPerWave: 2
- difficultyScaling: 1.1f
- timeBetweenWaves: 30f
- spawnInterval: 0.5f

**Dependencies:**
- WaveConfigSO (not shown but referenced)
- IPoolService
- EventBus (publishes WaveStartedEvent, WaveCompletedEvent)
- UnitDiedEvent subscription

**External Dependencies:**
- UnityEngine
- RTS.Core.Services
- RTS.Core.Events
- System.Collections.Generic

---

## BUILDING SYSTEMS

### 9. **Core Building System**
**Purpose:** Building component for placement, construction, resource generation

**Files:**
- `/Assets/Scripts/RTSBuildingsSystems/Building.cs` (204 lines)

**Key Features:**
- Construction progression system
- Construction completion callbacks
- Happiness bonus application/removal
- Resource generation timer
- Construction visual toggling
- Event-driven notifications
- SetData() for dynamic assignment

**Construction:**
- Configurable construction time
- Construction visual indicator
- Progress tracking

**Resource Generation:**
- Per-building resource type and amount
- Generation interval
- Automatic resource addition

**Happiness System:**
- Bonuses applied on construction complete
- Bonuses removed on destruction
- Building name tracking for UI

**Configuration:**
- requiresConstruction: true
- constructionTime: 5f
- generatesResources: false (per building)

**Dependencies:**
- BuildingDataSO
- IHappinessService
- IResourcesService
- EventBus

**External Dependencies:**
- UnityEngine
- RTS.Core.Events
- RTS.Core.Services
- System.Collections.Generic

---

### 10. **Building Data System (ScriptableObject)**
**Purpose:** Data-driven building configuration

**Files:**
- `/Assets/Scripts/RTSBuildingsSystems/BuildingDataSO.cs` (195 lines)

**BuildingType Enum:**
- Residential
- Production
- Military
- Economic
- Religious
- Cultural
- Defensive
- Special

**Properties:**
- Identity: buildingName, buildingType, description, icon
- Costs: woodCost, foodCost, goldCost, stoneCost
- Effects: happinessBonus, generatesResources
- Resource Generation: resourceType, resourceAmount, generationInterval
- Construction: constructionTime, buildingPrefab
- Housing: providesHousing, housingCapacity
- Health: maxHealth, repairCostMultiplier
- Unit Training: canTrainUnits, trainableUnits (List<TrainableUnitData>)

**Helper Methods:**
- GetCosts()
- GetTotalCost()
- GetCostString()
- GetRepairCosts()
- GetFullDescription()
- OnValidate() (sync aliases)

**Dependencies:** None (Data class)
**External Dependencies:**
- UnityEngine
- System.Collections.Generic
- RTS.Core.Services
- RTS.Units

---

### 11. **Building Selection System**
**Purpose:** Select buildings and show UI panels

**Files:**
- `/Assets/Scripts/RTSBuildingsSystems/BuildingSelectionManager.cs` (80+ lines)
- `/Assets/Scripts/RTSBuildingsSystems/BuildingSelectable.cs` (80+ lines)

**BuildingSelectionManager Features:**
- Input-driven building selection
- Click and right-click detection
- Raycast-based selection
- UI panel triggering
- Spawn point mode (for setting unit spawn locations)

**BuildingSelectable Features:**
- Selection state tracking
- Visual feedback (color highlighting)
- Selection indicator toggle
- Event-based communication

**Configuration:**
- selectionBoxColor
- selectedColor (cyan default)

**Dependencies:**
- UnityEngine.InputSystem
- EventBus (publishes/subscribes BuildingSelectedEvent, BuildingDeselectedEvent)
- BuildingSelectable component

**External Dependencies:**
- UnityEngine
- UnityEngine.EventSystems
- RTS.Core.Events
- UnityEngine.InputSystem

---

### 12. **Wall Connection System**
**Purpose:** Modular walls with automatic neighbor detection and mesh variants

**Files:**
- `/Assets/Scripts/RTSBuildingsSystems/WallConnectionSystem.cs` (349 lines)

**Key Features:**
- Grid-based wall registry (Vector2Int keys)
- Connection state bitmask (4 directions: N, E, S, W)
- 16 mesh variants (one per connection state)
- Automatic neighbor detection
- Mesh swapping based on connections
- Event-driven updates
- Connection update on building placement/destruction

**Connection Bitmask:**
- North: 1 (0001)
- East: 2 (0010)
- South: 4 (0100)
- West: 8 (1000)

**Configuration:**
- gridSize: 1f
- enableConnections: true
- meshVariants: GameObject[16]

**Public API:**
- GetConnectionState()
- GetGridPosition()
- IsConnected(WallDirection)

**Dependencies:**
- Building component (for building data)
- EventBus (subscribes BuildingPlacedEvent, BuildingDestroyedEvent)
- WallDirection enum

**External Dependencies:**
- UnityEngine
- System.Collections.Generic
- RTS.Core.Events

---

### 13. **Unit Training Queue System**
**Purpose:** Train units from buildings with queue management

**Files:**
- `/Assets/Scripts/RTSBuildingsSystems/UnitTrainingQueue.cs` (80+ lines)

**Key Features:**
- Queue-based training system
- Training progress tracking
- Resource cost validation
- Unit spawning at spawn point
- Training time configuration
- Queue size limit
- Event-driven notifications

**Configuration:**
- maxQueueSize: 5
- spawnPoint: Transform (auto-creates if not set, 3 units in front)

**TrainingQueueEntry Data:**
- unitData: TrainableUnitData
- timeRemaining: float
- totalTime: float
- Progress property: 0-1 float

**Public API:**
- QueueCount property
- IsTraining property
- CurrentTraining property
- Queue property (IReadOnlyCollection)

**Dependencies:**
- Building component
- IResourcesService
- BuildingDataSO
- TrainableUnitData (from BuildingDataSO)
- EventBus

**External Dependencies:**
- UnityEngine
- System.Collections.Generic
- RTS.Core.Events
- RTS.Core.Services
- RTS.Units

---

### 14. **Spawn Point Flag System**
**Purpose:** Visual indicator for unit spawn locations (buildings)

**Files:**
- `/Assets/Scripts/RTSBuildingsSystems/SpawnPointFlag.cs` (215 lines)

**Key Features:**
- Flag visual at spawn point
- Show/hide based on building selection
- Auto-create flag visual if not assigned
- Flag pole and flag mesh generation
- Position synchronization
- Gizmo visualization

**Configuration:**
- flagColor: Color (green default)
- flagHeight: 2f
- flagPoleRadius: 0.05f
- flagSize: 0.5f
- autoCreateFlag: true

**Visual Components:**
- Pole: Cylinder (dark gray)
- Flag: Cube (configurable color)

**Dependencies:**
- BuildingSelectable component
- UnitTrainingQueue component (for spawn point reference)
- EventBus (subscribes BuildingSelectedEvent, BuildingDeselectedEvent)

**External Dependencies:**
- UnityEngine
- RTS.Core.Events

---

## UNIT SYSTEMS

### 15. **Unit AI Controller System**
**Purpose:** Main AI orchestrator using state machine pattern

**Files:**
- `/Assets/Scripts/Units/AI/UnitAIController.cs` (100+ lines)

**Key Features:**
- State machine pattern
- Component coordination (Health, Movement, Combat)
- Configuration-driven setup
- Behavior type system
- State type tracking

**AI Behavior Types:**
- Aggressive (assumed, configurable)
- Other types not enumerated in code

**Component Management:**
- UnitHealth
- UnitMovement
- UnitCombat

**Configuration:**
- UnitConfigSO for unit stats
- AISettingsSO for AI behavior settings

**Public API:**
- ChangeState(UnitState)
- CurrentStateType property
- Health/Movement/Combat properties

**Dependencies:**
- UnitHealth component
- UnitMovement component
- UnitCombat component
- UnitConfigSO
- AISettingsSO
- IdleState (initial state)

**External Dependencies:**
- UnityEngine
- RTS.Core.Events
- RTS.Units

---

### 16. **Unit State Machine System**
**Purpose:** Define AI behavior states for units

**Files:**
- `/Assets/Scripts/Units/AI/States/UnitState.cs` (25 lines - Base class)
- `/Assets/Scripts/Units/AI/States/UnitStateType.cs` (Enum)
- `/Assets/Scripts/Units/AI/States/IdleState.cs`
- `/Assets/Scripts/Units/AI/States/MovingState.cs`
- `/Assets/Scripts/Units/AI/States/AttackingState.cs`
- `/Assets/Scripts/Units/AI/States/RetreatState.cs`
- `/Assets/Scripts/Units/AI/States/HealingState.cs`
- `/Assets/Scripts/Units/AI/States/DeadState.cs`

**State Types Enum:**
- Idle
- Moving
- Attacking
- Retreating
- Healing
- Dead

**Base State Pattern:**
- OnEnter() - Setup
- OnUpdate() - Frame updates
- OnExit() - Cleanup
- GetStateType() - Abstract

**Dependencies:**
- UnitAIController
- EventBus

**External Dependencies:**
- UnityEngine

---

### 17. **Unit Movement System**
**Purpose:** NavMesh-based unit movement

**Files:**
- `/Assets/Scripts/Units/Components/UnitMovement.cs` (80+ lines)

**Key Features:**
- NavMeshAgent integration
- SetDestination(Vector3)
- Follow target tracking
- Path updates for moving targets
- Movement speed control
- Stopping distance
- Arrival detection

**Configuration:**
- moveSpeed: 3.5f
- rotationSpeed: 120f
- stoppingDistance: 0.1f
- pathUpdateInterval: 0.5f

**Public Properties:**
- IsMoving
- HasReachedDestination
- Speed
- Velocity

**Dependencies:**
- NavMeshAgent component
- NavMesh system (Unity)

**External Dependencies:**
- UnityEngine
- UnityEngine.AI

---

### 18. **Unit Health System**
**Purpose:** Health tracking, damage, healing

**Files:**
- `/Assets/Scripts/Units/Components/UnitHealth.cs` (80+ lines)

**Key Features:**
- Health tracking (current/max)
- Damage application
- Healing system
- Death state
- Invulnerability flag
- Health percentage calculation
- Event-driven notifications

**Configuration:**
- maxHealth: 100f
- isInvulnerable: false

**Public Properties:**
- MaxHealth
- CurrentHealth
- HealthPercent
- IsDead
- IsInvulnerable

**Methods:**
- TakeDamage(amount, attacker)
- Heal(amount, healer)

**Dependencies:**
- EventBus (publishes UnitHealthChangedEvent, DamageDealtEvent, HealingAppliedEvent)

**External Dependencies:**
- UnityEngine
- RTS.Core.Events

---

### 19. **Unit Combat System**
**Purpose:** Combat mechanics (attacking, damage dealing)

**Files:**
- `/Assets/Scripts/Units/Components/UnitCombat.cs` (80+ lines)

**Key Features:**
- Target management
- Attack range checking
- Attack rate limiting
- Projectile support
- Attack execution
- Range detection

**Configuration:**
- attackRange: 2f
- attackDamage: 10f
- attackRate: 1f (attacks per second)
- projectilePrefab (optional)

**Public Properties:**
- AttackRange
- AttackDamage
- AttackRate
- CurrentTarget
- IsInAttackRange
- CanAttackNow

**Methods:**
- SetTarget(Transform)
- ClearTarget()
- IsTargetInRange(Transform)
- TryAttack()
- CanAttack()
- PerformAttack()

**Dependencies:**
- UnitHealth (for damage application)

**External Dependencies:**
- UnityEngine

---

### 20. **Unit Configuration System (ScriptableObject)**
**Purpose:** Data-driven unit configuration

**Files:**
- `/Assets/Scripts/Units/Data/UnitConfigSO.cs` (40 lines)

**Properties:**
- Identity: unitName, unitPrefab, unitIcon
- Health: maxHealth, canRetreat, retreatThreshold (0-100%)
- Movement: speed
- Combat: attackRange, attackDamage, attackRate
- AI: detectionRange

**Dependencies:** None (Data class)
**External Dependencies:**
- UnityEngine
- UnityEngine.UIElements

---

### 21. **Unit Selection Manager System**
**Purpose:** Select/deselect units with drag selection

**Files:**
- `/Assets/Scripts/Units/Selection/UnitSelectionManager.cs` (80+ lines)

**Key Features:**
- Click-based single selection
- Drag-based area selection
- Selection box UI
- Input action integration
- Multiple unit tracking
- Event-driven notifications

**Configuration:**
- selectionBoxColor: Color with alpha
- selectableLayer: LayerMask

**Public API:**
- SelectedUnits (IReadOnlyList)
- SelectionCount property

**Dependencies:**
- UnitSelectable component
- EventBus (publishes UnitSelectedEvent, UnitDeselectedEvent, SelectionChangedEvent)
- InputSystem

**External Dependencies:**
- UnityEngine
- UnityEngine.InputSystem
- UnityEngine.UI
- System.Collections.Generic
- RTS.Core.Events

---

### 22. **Unit Selectable Component**
**Purpose:** Mark units as selectable

**Files:**
- Built into UnitSelectionManager interactions

**Key Features:**
- Selection state
- Visual feedback
- Event-based communication

**Dependencies:**
- EventBus

---

### 23. **Unit AI Specialized Classes**
**Purpose:** Unit type-specific AI behavior

**Files:**
- `/Assets/Scripts/Units/AI/Specialized/SoldierAI.cs`
- `/Assets/Scripts/Units/AI/Specialized/ArcherAI.cs`
- `/Assets/Scripts/Units/AI/Specialized/HealerAI.cs`

**Patterns:**
- Inherit from UnitAIController
- Implement specific combat behaviors
- Type-specific logic

---

## UI SYSTEMS

### 24. **Resource UI System**
**Purpose:** Display and update resource counts

**Files:**
- `/Assets/Scripts/UI/ResourceUI.cs` (80+ lines)

**Key Features:**
- Data-driven resource display
- Multiple resource tracking
- Animation on changes
- Color feedback (positive/negative)
- Format string customization
- Auto-create displays option
- Flexible array-based setup

**ResourceDisplay Structure:**
- resourceType: ResourceType
- textComponent: TextMeshProUGUI
- iconImage: Image (optional)
- displayFormat: string
- showName: bool
- animateChanges: bool
- positiveChangeColor: Color
- negativeChangeColor: Color
- colorFadeDuration: float

**Dependencies:**
- IResourcesService
- EventBus (subscribes ResourcesChangedEvent)

**External Dependencies:**
- UnityEngine
- UnityEngine.UI
- TMPro
- RTS.Core.Events
- RTS.Core.Services
- System.Collections.Generic
- System.Linq

---

### 25. **Building Details UI System**
**Purpose:** Show building information and training options

**Files:**
- `/Assets/Scripts/UI/BuildingDetailsUI.cs` (100+ lines)

**Key Features:**
- Building information display
- Training queue visualization
- Unit training buttons
- Spawn point flag button
- Progress bar updates
- Building icon and description
- Dynamic button creation

**UI Components:**
- Panel root (show/hide)
- Building name text
- Building description text
- Building icon image
- Training queue panel
- Queue count text
- Training progress bar
- Current training text
- Unit button container
- Unit button prefab (instantiated)
- Set spawn point button

**Dependencies:**
- BuildingSelectionManager
- Building component
- UnitTrainingQueue
- BuildingSelectable
- EventBus (subscribes BuildingSelectedEvent, BuildingDeselectedEvent, TrainingProgressEvent)
- TrainUnitButton prefab

**External Dependencies:**
- UnityEngine
- UnityEngine.UI
- TMPro
- System.Collections.Generic
- RTS.Buildings
- RTS.Core.Events

---

### 26. **Happiness UI System**
**Purpose:** Display happiness/morale indicator

**Files:**
- `/Assets/Scripts/UI/HappinessUI.cs`

**Dependencies:**
- HappinessManager
- EventBus

---

### 27. **Building HUD System**
**Purpose:** Building placement UI with hotkeys

**Files:**
- `/Assets/Scripts/UI/BuildingHUD.cs` (80+ lines)

**Key Features:**
- Dynamic building button creation
- Hotkey support (b, h, f, t, w, g, c, m)
- Resource availability checking
- Placement info panel
- Building panel toggling
- Input action integration

**Configuration:**
- enableHotkeys: true
- buildingHotkeyStrings: string[]

**Dependencies:**
- BuildingManager
- BuildingButton component
- IResourcesService
- EventBus (subscribes ResourcesChangedEvent)
- InputActionReference

**External Dependencies:**
- RTS.Buildings
- RTS.Core.Events
- RTS.Core.Services
- RTS.Managers
- System.Collections.Generic
- TMPro
- UnityEngine
- UnityEngine.InputSystem
- UnityEngine.UI

---

### 28. **Building Button Component**
**Purpose:** Individual building placement button

**Files:**
- `/Assets/Scripts/RTSBuildingsSystems/BuildingButton.cs`

**Dependencies:**
- BuildingManager
- BuildingDataSO
- EventBus

---

### 29. **Building UI Component**
**Purpose:** Building UI wrapper

**Files:**
- `/Assets/Scripts/UI/BuildingUI.cs`

---

### 30. **Building UI Toggle**
**Purpose:** Show/hide building UI

**Files:**
- `/Assets/Scripts/UI/BuildingHUDToggle.cs`

---

### 31. **Building Tooltip System**
**Purpose:** Hover tooltips for buildings

**Files:**
- `/Assets/Scripts/RTSBuildingsSystems/BuildingTooltip.cs`

---

### 32. **Train Unit Button Component**
**Purpose:** Button for training individual units

**Files:**
- `/Assets/Scripts/UI/TrainUnitButton.cs`

**Dependencies:**
- UnitTrainingQueue
- TrainableUnitData
- IResourcesService

---

## CAMERA SYSTEM

### 33. **RTS Camera Controller**
**Purpose:** Camera movement and zoom for RTS gameplay

**Files:**
- `/Assets/Scripts/Camera/RTSCameraController.cs` (100+ lines)

**Key Features:**
- WASD/Arrow key movement
- Mouse edge scrolling
- Scroll wheel zoom
- Q/E rotation
- Middle mouse drag panning
- Position clamping
- Orthographic/perspective support
- Input System integration

**Configuration:**
- moveSpeed: 15f
- dragSpeed: 0.5f
- edgeScrollSpeed: 20f
- panBorderThickness: 10f
- zoomSpeed: 50f
- minZoom: 15f
- maxZoom: 80f
- rotationSpeed: 60f
- useEdgeScroll: true
- isCamInverted: false
- minPosition/maxPosition: Vector2

**Input Bindings:**
- Move (WASD/Arrows)
- Zoom (Mouse scroll)
- Rotate (Q/E)

**Dependencies:**
- InputSystem_Actions class

**External Dependencies:**
- UnityEngine
- UnityEngine.InputSystem

---

## ANIMATION SYSTEM

### 34. **Unit Animation Controller**
**Purpose:** Synchronize animations with unit movement/combat

**Files:**
- `/Assets/RTSAnimation/UnitAnimationController.cs` (12KB)

**Key Features:**
- Event-driven animation updates
- Parameter hashing for performance
- Automatic state synchronization
- Movement speed sync
- Combat animation triggers
- Death animation handling
- Fully integrated with EventBus

**Configuration:**
- AnimationConfigSO reference

**Dependencies:**
- Animator component
- EventBus
- AnimationConfigSO

**Integration Points:**
- UnitMovement → Speed parameter
- UnitCombat → Attack trigger
- UnitHealth → Death trigger
- UnitAIController → State changes

---

### 35. **Animation Configuration System (ScriptableObject)**
**Purpose:** Designer-friendly animation settings

**Files:**
- `/Assets/RTSAnimation/AnimationConfigSO.cs` (2KB)

**Properties:**
- Movement thresholds
- Animation transition speeds
- Timing configurations
- Audio/effect triggers

---

### 36. **Unit Animation Advanced**
**Purpose:** Advanced animation features (IK, layers)

**Files:**
- `/Assets/RTSAnimation/UnitAnimationAdvanced.cs` (6.8KB)

**Key Features:**
- Look-at IK for targeting
- Hand IK for weapon positioning
- Animation layer management
- Blend tree transitions

---

### 37. **Unit Animation Events**
**Purpose:** Audio and particle effect handling

**Files:**
- `/Assets/RTSAnimation/UnitAnimationEvents.cs` (5.4KB)

**Key Features:**
- Footstep sounds
- Attack sounds
- Death sounds
- Particle effect spawning
- Event-driven timing

---

### 38. **Animation Setup Helper**
**Purpose:** Editor utilities for animation setup

**Files:**
- `/Assets/RTSAnimation/AnimationSetupHelper.cs` (11KB)

**Key Features:**
- Editor menu integration
- Automatic component addition
- Animator validation
- Configuration creation

---

## INPUT SYSTEM

### 39. **Input System Actions**
**Purpose:** Centralized input action configuration

**Files:**
- `/Assets/Scripts/InputSystem_Actions.cs`

**Action Maps:**
- Player inputs (movement, zoom, rotation, etc.)
- UI interactions

**Dependencies:**
- Unity.InputSystem package

---

## EDITOR TOOLS

### 40. **Wall Connection System Editor**
**Purpose:** Editor tools for wall mesh setup

**Files:**
- `/Assets/Scripts/RTSBuildingsSystems/Editor/WallConnectionSystemEditor.cs`

**Features:**
- Mesh variant visualization
- Connection debugging
- Setup validation

---

### 41. **Wall Prefab Setup Utility**
**Purpose:** Automated wall prefab creation

**Files:**
- `/Assets/Scripts/RTSBuildingsSystems/Editor/WallPrefabSetupUtility.cs`

**Features:**
- Mesh variant assignment
- Collider setup
- Component configuration

---

### 42. **Building HUD Setup Editor**
**Purpose:** Building UI panel setup helper

**Files:**
- `/Assets/Scripts/Editor/BuildingHUDSetup.cs`

**Features:**
- Quick UI creation
- Component assignment
- Button prefab setup

---

### 43. **Building Training UI Setup Editor**
**Purpose:** Training queue UI helper

**Files:**
- `/Assets/Scripts/Editor/BuildingTrainingUISetup.cs`

**Features:**
- UI panel configuration
- Progress bar setup
- Button container setup

---

## DEBUG & DIAGNOSTIC SYSTEMS

### 44. **Building Details Diagnostic**
**Purpose:** Debug building selection issues

**Files:**
- `/Assets/Scripts/Debug/BuildingDetailsDiagnostic.cs`

---

### 45. **Building Selection Debugger**
**Purpose:** Debug building selection system

**Files:**
- `/Assets/Scripts/Debug/BuildingSelectionDebugger.cs`

---

## EXTERNAL PACKAGES (from package.json)

### Core Packages:
- com.unity.render-pipelines.universal: 17.2.0 (URP)
- com.unity.inputsystem: 1.14.2
- com.unity.ui: 2.0.0 (UI)
- com.unity.ugui: 2.0.0 (UI Graphics)
- com.unity.timeline: 1.8.9
- com.unity.visualscripting: 1.9.8

### AI & Navigation:
- com.unity.ai.navigation: 2.0.9

### Adaptive Performance:
- com.unity.adaptiveperformance: 5.1.6

### Editor & Tools:
- com.unity.ide.rider: 3.0.38
- com.unity.ide.visualstudio: 2.0.25
- com.unity.collab-proxy: 2.10.0
- com.unity.ai.assistant: 1.0.0-pre.12
- com.unity.ai.generators: 1.0.0-pre.20

### Testing & Profiling:
- com.unity.test-framework: 1.6.0
- com.unity.multiplayer.center: 1.0.0
- com.unity.memoryprofiler: 1.1.9

### 2D Support:
- com.unity.2d.enhancers: 1.0.0
- com.unity.2d.sprite: 1.0.0

### Module Packages:
- All standard Unity modules (physics, audio, animation, etc.)

---

## ASSET ORGANIZATION

### Scripts Folder Structure:
```
/Assets/Scripts/
├── Camera/
│   └── RTSCameraController.cs
├── Core/
│   ├── EventBus.cs
│   ├── GameEvents.cs
│   ├── IServices.cs
│   ├── ObjectPool.cs
│   └── ServiceLocator.cs
├── Debug/
│   ├── BuildingDetailsDiagnostic.cs
│   └── BuildingSelectionDebugger.cs
├── Editor/
│   ├── BuildingHUDSetup.cs
│   └── BuildingTrainingUISetup.cs
├── Managers/
│   ├── BuildingManager.cs
│   ├── GameManager.cs
│   ├── HappinessManager.cs
│   ├── ResourceManager.cs
│   └── WaveManager.cs
├── RTSBuildingsSystems/
│   ├── Building.cs
│   ├── BuildingButton.cs
│   ├── BuildingDataSO.cs
│   ├── BuildingHUD.cs
│   ├── BuildingSelectable.cs
│   ├── BuildingSelectionManager.cs
│   ├── BuildingTooltip.cs
│   ├── SpawnPointFlag.cs
│   ├── UnitTrainingQueue.cs
│   ├── WallConnectionSystem.cs
│   └── Editor/
│       ├── WallConnectionSystemEditor.cs
│       └── WallPrefabSetupUtility.cs
├── UI/
│   ├── BuildingDetailsUI.cs
│   ├── BuildingHUDToggle.cs
│   ├── BuildingUI.cs
│   ├── HappinessUI.cs
│   ├── NotificationUI.cs
│   ├── ResourceUI.cs
│   └── TrainUnitButton.cs
├── Units/
│   ├── AI/
│   │   ├── Specialized/
│   │   │   ├── ArcherAI.cs
│   │   │   ├── HealerAI.cs
│   │   │   └── SoldierAI.cs
│   │   ├── States/
│   │   │   ├── AttackingState.cs
│   │   │   ├── DeadState.cs
│   │   │   ├── HealingState.cs
│   │   │   ├── IdleState.cs
│   │   │   ├── MovingState.cs
│   │   │   ├── RetreatState.cs
│   │   │   ├── UnitState.cs
│   │   │   └── UnitStateType.cs
│   │   ├── AISettingsSO.cs
│   │   └── UnitAIController.cs
│   ├── Components/
│   │   ├── UnitCombat.cs
│   │   ├── UnitHealth.cs
│   │   ├── UnitMovement.cs
│   │   └── UnitSelectable.cs
│   ├── Data/
│   │   └── UnitConfigSO.cs
│   ├── Selection/
│   │   ├── RTSCommandHandler.cs
│   │   └── UnitSelectionManager.cs
│   └── RTS (deprecated?) /
│       └── UnitAIController.cs (legacy?)
├── RTSModularCamera.cs
└── InputSystem_Actions.cs

/Assets/RTSAnimation/
├── UnitAnimationController.cs
├── AnimationConfigSO.cs
├── UnitAnimationAdvanced.cs
├── UnitAnimationEvents.cs
├── AnimationSetupHelper.cs
├── ANIMATION_SYSTEM_GUIDE.md
├── QUICK_REFERENCE.md
└── README.md
```

---

## DEPENDENCY GRAPH

### Tier 0 (No Dependencies):
- EventBus
- GameEvents
- ServiceLocator
- IServices (interfaces)
- ObjectPool

### Tier 1 (Core Infrastructure):
- ResourceManager (depends: EventBus, IServices)
- HappinessManager (depends: EventBus, IServices)
- GameManager (depends: Tier 1 managers)

### Tier 2 (Building Systems):
- Building (depends: EventBus, Services, BuildingDataSO)
- BuildingDataSO (depends: IServices, UnitConfig)
- BuildingManager (depends: EventBus, Services, BuildingDataSO)
- WallConnectionSystem (depends: EventBus, Building)
- BuildingSelectable (depends: EventBus)
- BuildingSelectionManager (depends: EventBus, BuildingSelectable)
- UnitTrainingQueue (depends: Services, EventBus, Building, UnitConfig)
- SpawnPointFlag (depends: EventBus, BuildingSelectable, UnitTrainingQueue)

### Tier 3 (Unit Systems):
- UnitConfigSO (no dependencies)
- AISettingsSO (no dependencies)
- UnitHealth (depends: EventBus)
- UnitMovement (depends: NavMesh)
- UnitCombat (depends: UnitHealth)
- UnitAIController (depends: UnitHealth, UnitMovement, UnitCombat, EventBus, Configs)
- UnitStates (depend: UnitAIController, EventBus)
- SpecializedAI (depend: UnitAIController)

### Tier 4 (UI Systems):
- ResourceUI (depends: Services, EventBus)
- HappinessUI (depends: Services, EventBus)
- BuildingDetailsUI (depends: Building, UnitTrainingQueue, EventBus, BuildingSelectionManager)
- BuildingHUD (depends: BuildingManager, Services, EventBus)
- BuildingButton (depends: BuildingManager, Services, EventBus)
- TrainUnitButton (depends: UnitTrainingQueue, Services)
- BuildingTooltip (depends: BuildingDataSO)
- NotificationUI (depends: EventBus)
- BuildingHUDToggle

### Tier 5 (Camera & Other):
- RTSCameraController (depends: InputSystem)
- WaveManager (depends: Services, EventBus)

### Tier 6 (Animation System):
- UnitAnimationController (depends: EventBus, AnimationConfigSO, Animator)
- UnitAnimationAdvanced (depends: Animator)
- UnitAnimationEvents (depends: EventBus)
- AnimationConfigSO (no dependencies)

---

## COMMUNICATION PATTERNS

### Event-Driven Communication:
- EventBus (publish/subscribe) for all cross-system communication
- 25+ event types covering all game systems
- Decoupled architecture - systems don't directly reference each other

### Service-Based Communication:
- ServiceLocator (dependency injection)
- 5 main services: Resource, Happiness, Pool, Time, GameState
- Services available globally after registration in GameManager

### Direct Dependencies:
- UI systems depend on managers
- Building systems depend on BuildingDataSO
- Unit systems depend on UnitConfigSO
- Animation depends on Animator components

---

## KEY ARCHITECTURAL PATTERNS

1. **Service Locator Pattern**: Centralized dependency injection
2. **Event Bus Pattern**: Decoupled publish-subscribe communication
3. **State Machine Pattern**: AI behavior management
4. **Component Pattern**: Modular unit behavior (Health, Movement, Combat)
5. **ScriptableObject Architecture**: Data-driven configuration
6. **Object Pooling Pattern**: Memory-efficient object reuse
7. **Factory Pattern**: Building/Unit creation via prefabs and data
8. **Observer Pattern**: Event subscriptions throughout

---

## TOTAL SYSTEMS COUNT: 45+

- Core Infrastructure: 5 systems
- Managers: 4 systems
- Building Systems: 6 systems
- Unit Systems: 9 systems
- UI Systems: 9 systems
- Camera System: 1 system
- Animation System: 5 systems
- Input System: 1 system
- Editor Tools: 3 systems
- Debug Systems: 2 systems

