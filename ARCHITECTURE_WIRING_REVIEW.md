# Kingdoms At Dusk Architecture And Wiring Review

## Purpose

This document explains:

- how the active game scene is wired today
- where the current architecture is brittle or inconsistent
- what is actively wrong versus what is merely hard to maintain
- what the project should move toward next

The review focuses on authored runtime code and active scene/prefab wiring, not third-party packages or sample content.

---

## Executive Summary

The project is functional, but the runtime architecture is mixed and increasingly hard to reason about.

Today the game works through a combination of:

- prefab-wired scene roots
- direct serialized references
- `ServiceLocator`
- static singletons
- global `EventBus`
- ad hoc `FindAnyObjectByType` / `FindFirstObjectByType`
- widespread `Camera.main` fallback
- multiple independent `InputSystem_Actions` instances

That combination is the main problem.

The codebase does not suffer from one catastrophic design flaw. Instead, it suffers from accumulated integration drift:

- systems are individually reasonable
- their composition is inconsistent
- startup order is implicit
- ownership boundaries are blurred
- migrations exist in parallel without a final cutover

The highest-value improvement is not a full rewrite. It is to standardize runtime composition around a single scene composition root and stop letting systems discover each other opportunistically at runtime.

---

## Current Runtime Shape

## Active Scene

The playable runtime centers on:

- `Assets/Scenes/GameScene.unity`

Most important gameplay systems are not scene-local objects authored directly in the scene file. They come from prefab roots.

## Primary Roots

### 1. Managers Root

`Assets/#Prefabs/transfer/ManagersRoot.prefab`

This prefab acts as the main service and coordination hub. It currently hosts:

- `GameManager`
- `ResourceManager`
- `HappinessManager`
- `PopulationManager`
- `ReputationManager`
- `PeasantWorkforceManager`
- `BuildingManager`
- `SelectionManager`
- `FogOfWarManager`
- `SaveLoadSystem`
- `LoadingGameManager`
- `SceneTransitionManager`
- `AudioManager`
- `SettingsManager`
- `FloatingNumbersManager`
- multiple gameplay UI controllers

This means the project already has a de facto composition root. It is just not treated as one consistently.

### 2. Gameplay Root

`Assets/#Prefabs/transfer/GamePlayRoot.prefab`

This prefab hosts world-facing helpers:

- `MainCamera`
- `ObjectPool`
- `PlayAreaBounds`
- minimap/fog camera helpers

This root is more spatial/world-oriented than service-oriented.

### 3. Loading Screen Prefab

`Assets/#Prefabs/UI/LoadingScreen.prefab`

This hosts `LoadingScreenManager`, which is used by transition/bootstrap code.

---

## How Systems Connect Today

## Service Initialization

`Assets/Scripts/Managers/GameManager.cs`

`GameManager` is the closest thing to a runtime orchestrator.

It registers services into `ServiceLocator`, including:

- `IGameStateService`
- `IPoolService`
- `IResourcesService`
- `IHappinessService`
- `IPopulationService`
- `IReputationService`
- `IPeasantWorkforceService`
- `IBuildingService`
- `ISaveLoadService`
- `IFloatingNumberService`
- `IAudioService`
- `ISettingsService`

This is a valid direction, but it is incomplete, because many systems still do not depend on these services through the locator or injected references. They still search the scene directly.

## Input Path

Input is fragmented.

Examples:

- `RTSCameraController` creates its own `InputSystem_Actions`
- `BuildingManager` creates its own `InputSystem_Actions`
- `SaveLoadInputHandler` creates its own `InputSystem_Actions`
- `MainMenuManager` creates its own `InputSystem_Actions`
- unit/building selection systems rely on `InputActionReference`

This means input ownership is split between:

- action references from prefab wiring
- locally created action maps inside components

That is hard to reason about and easy to break when rebinding or changing action maps.

## Selection And Commands

Player command flow is currently split across several components:

- `UnitSelectionManager`
- `RTSCommandHandler`
- `BuildingSelectionManager`
- `UnifiedControlGroupManager`
- `ControlGroupFeedbackUI`
- `CursorStateManager`

This separation is not inherently bad, but responsibility boundaries are not clean.

Current pattern:

- one component decides what is selected
- another decides what right-click means
- others observe selection and mutate UI/cursor state
- building selection and unit selection coordinate by clearing each other

That works, but the cross-clearing and scene lookup approach creates tight coupling.

## Movement And Pathfinding

The active movement stack is still NavMesh-based:

- `UnitMovement` requires `NavMeshAgent`
- `UnitAIController` depends on `UnitMovement`
- `RTSCommandHandler` issues movement through `UnitMovement.SetDestination`

In parallel, a flow-field migration exists:

- `FlowFieldManager`
- `FlowFieldFollower`
- `FlowFieldFormationController`
- `FlowFieldRTSCommandHandler`
- obstacle adapters
- setup and migration tooling

The important fact is this:

- flow-field exists
- NavMesh remains the gameplay default
- the integration is not complete
- both paths are alive in the codebase

That makes the movement architecture conceptually split.

## Building Placement

`BuildingManager` is doing a lot:

- building placement
- wall placement delegation
- tower/gate wall snapping
- fog visibility checks
- input handling
- placement preview state
- building data validation
- service access

This is one of the largest coupling hotspots in the runtime.

It is functional, but it mixes orchestration, placement policy, input concerns, and placement-mode state in one class.

## Save/Load

Save/load behavior is distributed across:

- `SaveLoadManager`
- `SaveLoadInputHandler`
- `SaveLoadMenu`
- `SceneTransitionManager`
- `GameManager`

The core persistence manager is fine in concept, but orchestration is diffuse:

- quick save/load hotkeys live elsewhere
- menu pause/resume also touches game state and `Time.timeScale`
- scene transitions and auto-load flags span multiple classes

This makes save/load behavior harder to test and reason about than it needs to be.

## UI

UI is partly event-driven and partly lookup-driven.

Good:

- `ResourceUI` listens to `ResourcesChangedEvent`
- detail panels respond to selection events

Risky:

- many UI classes locate managers at runtime
- some data appears on multiple UI controllers
- UI ownership is spread across `UIManager`, `SelectionManager`, `SaveLoadSystem`, and independent prefabs

---

## What Is Actually Wrong

This section focuses on concrete architectural faults, not preferences.

## 1. Runtime Composition Is Inconsistent

The biggest issue is not any single class. It is that the project has too many dependency styles at once.

Today a component may obtain dependencies by:

- serialized field assignment
- `ServiceLocator.TryGet`
- singleton access
- `FindAnyObjectByType`
- `FindFirstObjectByType`
- `Camera.main`

That causes:

- hidden dependencies
- startup-order sensitivity
- scene-specific behavior
- harder prefab reuse
- harder debugging when references are missing

This is the top architectural problem.

## 2. The Real Composition Root Exists But Is Not Enforced

`ManagersRoot.prefab` already behaves like the runtime composition root.

But instead of enforcing explicit wiring there, many systems self-heal by searching the scene in `Awake` or `Start`.

That creates a false sense of safety:

- missing references do not fail early
- systems quietly fall back
- bugs become environment-dependent

A composition root only helps if it is authoritative.

## 3. Input Ownership Is Split Across The Runtime

Several systems instantiate `InputSystem_Actions` themselves while other systems consume `InputActionReference`.

This creates:

- duplicated enable/disable logic
- duplicated subscriptions
- hidden control conflicts
- harder rebinding support
- multiple components believing they own the same action map

The input layer should have one authoritative runtime owner.

## 4. Pathfinding Migration Is Halfway Done

The codebase contains both:

- stable NavMesh gameplay wiring
- incomplete flow-field gameplay replacement

That is not wrong by itself during migration, but it becomes wrong when:

- both paths remain first-class for too long
- code continues to accumulate on both
- ownership of “the real movement system” becomes unclear

Right now the project still thinks in two movement architectures.

## 5. Some Important Systems Exist In Code But Are Not Clearly Wired Into The Active Scene

### GameSceneBootstrap

`Assets/Scripts/Core/GameSceneBootstrap.cs` exists and is designed to manage loading/bootstrap behavior.

But in the inspected active scene wiring, it was not present on `GameScene` or on the transfer prefabs that define the runtime roots.

That means one of two things:

- it is dead or legacy code
- it is intended to be active, but currently is not wired

Either case should be resolved explicitly.

### DayNightCycleManager

The day/night system exists in code, and `DayNightCycleManager` registers `ITimeService` when active.

But:

- `GameManager`'s `dayNightCycleManager` reference is null in the prefab wiring
- the inspected active scene wiring did not show an active `DayNightCycleManager`

So the project currently contains a designed service that may not actually participate in the main playable runtime.

## 6. Error Handling Is Too Quiet

Core systems swallow exceptions without useful reporting, including:

- `EventBus`
- `SaveLoadManager`
- `RTSSettingsManager`
- `ShaderPreloader`

This hurts:

- debugging
- testability
- user-facing reliability

Silent failure is worse than visible failure in most of these places.

## 7. Several Systems Have Too Much Responsibility

The main hotspots are:

- `BuildingManager`
- `UnitSelectionManager`
- `BuildingSelectionManager`
- `SaveLoadMenu`
- `RTSCameraController`

These classes are not just “big”. They combine concerns that should evolve independently.

That increases:

- coupling
- regression risk
- inspector complexity
- prefab fragility

## 8. UI Ownership Is Diffuse

There is no single clean UI composition layer.

Instead:

- some UI lives under `UIManager`
- some under selection objects
- some under save/load systems
- some under dedicated prefabs

This makes it harder to answer:

- who owns a panel
- who is allowed to open/close it
- which system is the source of truth

---

## What Is Better Than It Looks

The project does have strong pieces worth preserving.

## 1. `GameManager` Already Acts As A Useful Orchestrator

It is imperfect, but it is a strong starting point for formalizing service composition.

## 2. Event-Driven UI Is Present

Several UI systems already respond to events rather than polling all state directly.

That is the right direction.

## 3. ScriptableObject-Driven Building Data Is Good

Using `BuildingDataSO` and related data objects is a strong design choice. The issue is not the data model. The issue is runtime composition around it.

## 4. The Flow-Field Migration Work Is Valuable

The project has already done meaningful work toward higher-scale RTS pathfinding. That should not be discarded. It just needs a deliberate cutover plan.

---

## Recommended Target Architecture

Do not rewrite everything. Move toward a clearer version of what the project already is.

## A. Formalize A Single Composition Root

Make `ManagersRoot` the authoritative runtime composition root.

That means:

- all critical runtime dependencies are serialized and validated there
- missing references fail loudly in editor validation
- gameplay systems stop discovering each other opportunistically

Preferred rule:

- if a dependency is required, wire it explicitly
- if optional, represent it explicitly as optional
- do not silently search the scene unless there is a very good reason

## B. Standardize Dependency Style

Choose one primary style:

- serialized references for scene/prefab-local relationships
- `ServiceLocator` only for true runtime services

Use `ServiceLocator` for:

- resources
- game state
- audio
- settings
- save/load
- pooling
- time

Do not use it for:

- neighboring visual/UI/controller references that belong in prefab wiring

Reduce:

- `FindAnyObjectByType`
- `FindFirstObjectByType`
- `Camera.main`

to rare fallback cases or editor tools only.

## C. Create A Single Input Gateway

Add one runtime input owner, for example:

- `PlayerInputRouter`
- or `GameInputContext`

Responsibilities:

- own `InputSystem_Actions`
- enable/disable action maps
- expose typed actions/events to gameplay systems
- coordinate menu/gameplay mode switching

Other systems should subscribe to the gateway, not instantiate their own input maps.

## D. Split Selection From Command Issuing More Cleanly

Target split:

- `SelectionService` or selection root: source of truth for current selection
- `CommandService` or command root: interprets player intent
- per-domain responders: units, buildings, rally points, etc.

Selection systems should not need to manually clear each other through scene lookups.

Prefer:

- one selection state model
- separate views/controllers for unit and building selection presentation

## E. Decide The Movement Future Explicitly

Do one of these, deliberately:

### Option 1. Stay On NavMesh For Now

- keep NavMesh as the only supported runtime path
- move flow-field code behind an experimental flag
- stop integrating both at once until migration resumes

### Option 2. Commit To Flow-Field Migration

- define a cutover branch/plan
- identify all active `NavMeshAgent` dependencies
- convert command issuance and AI movement in a bounded sequence
- remove dual-runtime ambiguity after parity is achieved

The bad state is not “NavMesh”. The bad state is “NavMesh plus a semi-active alternate runtime path”.

## F. Break Up Large MonoBehaviours

Priority extraction targets:

### BuildingManager

Split into:

- placement session controller
- placement validation service
- preview renderer/controller
- specialized wall/gate/tower placement strategies

### SaveLoadMenu

Split into:

- menu state controller
- save list presenter
- quit/return actions controller
- pause integration adapter

### RTSCameraController

Split into:

- movement controller
- zoom controller
- edge-scroll policy
- viewport/input gating

This is not for style. It will directly reduce regression risk.

## G. Add Runtime Validation

Add a validation layer that runs in editor and optionally on boot:

- required references present
- no duplicate singleton-like managers
- expected services registered
- input action references assigned
- scene root prefabs intact
- only one active command path

This will catch most current wiring fragility early.

## H. Make Error Handling Observable

Replace silent catches with structured reporting:

- `Debug.LogError` in editor/dev
- explicit result objects where appropriate
- clear failure reasons for save/load/settings/bootstrap

This is especially important for:

- persistence
- configuration loading
- runtime initialization

---

## Recommended Phased Plan

## Phase 1: Stabilize Wiring

Goal: reduce fragility without changing gameplay behavior.

Actions:

1. Treat `ManagersRoot` as the official composition root.
2. Audit all required references and assign them explicitly.
3. Remove low-value `FindAnyObjectByType` fallbacks from core runtime components.
4. Add validation scripts for missing references and duplicate managers.
5. Decide whether `GameSceneBootstrap` is active or dead and act accordingly.
6. Decide whether day/night is active in `GameScene` and wire or remove accordingly.

Expected benefit:

- fewer startup-order bugs
- clearer scene ownership
- easier debugging

## Phase 2: Consolidate Input And Selection

Goal: simplify player interaction architecture.

Actions:

1. Create one input gateway for gameplay.
2. Stop constructing `InputSystem_Actions` inside unrelated controllers.
3. Centralize selection state.
4. Separate selection state from command interpretation.
5. Make UI observe selection state instead of discovering managers ad hoc.

Expected benefit:

- fewer interaction conflicts
- simpler rebinding and pause/menu behavior
- easier future feature work

## Phase 3: Resolve Movement Architecture

Goal: remove dual-path ambiguity.

Actions:

1. Choose NavMesh-only for now or commit to flow-field migration.
2. If staying on NavMesh, mark flow-field runtime as experimental.
3. If migrating, define the conversion sequence:
   - command layer
   - unit movement
   - AI state integration
   - obstacle updates
   - formation parity
4. Remove whichever runtime path is no longer primary.

Expected benefit:

- clearer performance strategy
- less architectural drift
- lower maintenance cost

## Phase 4: Break Up Heavy Managers

Goal: reduce code fragility and improve ownership clarity.

Actions:

1. Refactor `BuildingManager`
2. Refactor `SaveLoadMenu`
3. Refactor `RTSCameraController`
4. Reduce duplicate UI responsibilities

Expected benefit:

- smaller blast radius per change
- easier testing
- cleaner prefab wiring

---

## Priority Defects To Address First

These are the most important practical issues.

### P1. Missing Or Ambiguous Runtime Wiring

- verify and fix `GameSceneBootstrap`
- verify and fix `DayNightCycleManager`
- eliminate null-required references that rely on runtime discovery

### P1. Dependency Style Drift

- reduce scene lookups in core runtime scripts
- standardize service vs serialized dependency rules

### P1. Input Fragmentation

- move toward one gameplay input owner

### P1. Movement Architecture Split

- make an explicit call on NavMesh vs flow-field primary runtime

### P2. Silent Failure

- stop swallowing core exceptions silently

### P2. Oversized MonoBehaviours

- begin with `BuildingManager` and `SaveLoadMenu`

---

## File-Level Notes

## Strong Candidates For Immediate Review

- `Assets/Scripts/Managers/GameManager.cs`
- `Assets/Scripts/Managers/BuildingManager.cs`
- `Assets/Scripts/Units/Selection/UnitSelectionManager.cs`
- `Assets/Scripts/Units/Selection/RTSCommandHandler.cs`
- `Assets/Scripts/RTSBuildingsSystems/BuildingSelectionManager.cs`
- `Assets/Scripts/Camera/RTSCameraController.cs`
- `Assets/Scripts/SaveLoad/SaveLoadManager.cs`
- `Assets/Scripts/SaveLoad/SaveLoadMenu.cs`
- `Assets/Scripts/SaveLoad/SaveLoadInputHandler.cs`
- `Assets/Scripts/FlowField/Integration/FlowFieldRTSCommandHandler.cs`
- `Assets/Scripts/Core/GameSceneBootstrap.cs`
- `Assets/Scripts/DayNightCycle/DayNightCycleManager.cs`

## Files That Represent Good Direction

- `Assets/Scripts/Core/ServiceLocator.cs`
- `Assets/Scripts/Core/EventBus.cs`
- `Assets/Scripts/Managers/ResourceManager.cs`
- `Assets/Scripts/UI/ResourceUI.cs`
- ScriptableObject data assets for buildings and units

These are not perfect, but they represent reusable patterns worth refining instead of discarding.

---

## Final Recommendation

Do not attack this as a giant cleanup.

Treat it as a runtime-composition cleanup with three decisions:

1. what owns wiring
2. what owns input
3. what owns movement going forward

If those three are made explicit, most of the rest of the codebase becomes much easier to improve safely.
