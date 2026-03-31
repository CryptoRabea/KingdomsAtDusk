# Findings

## Initial
- `GameScene` uses prefab roots for most runtime systems instead of scene-local manager objects.
- `ManagersRoot.prefab` contains the main service/control hub.
- `GamePlayRoot.prefab` contains camera, pool, and play-area world helpers.

## Wiring
- `ManagersRoot.prefab` hosts `GameManager`, `BuildingManager`, selection systems, save/load input/menu, audio, settings, fog-of-war, and multiple UI controllers.
- `GamePlayRoot.prefab` hosts `MainCamera`, `ObjectPool`, and `PlayAreaBounds`.
- `GameManager` explicitly serializes most service references and registers them into `ServiceLocator`, but several systems still bypass that and discover dependencies with `FindAnyObjectByType`, `FindFirstObjectByType`, or `Camera.main`.
- Several prefab fields are intentionally left unassigned (`mainCamera: {fileID: 0}` on multiple components), relying on runtime fallback discovery.

## Architecture Risks
- Mixed architecture: direct serialized refs, `ServiceLocator`, static singletons, global `EventBus`, and runtime object discovery all coexist.
- Input ownership is fragmented: multiple systems instantiate `InputSystem_Actions` independently instead of reading from a single input composition/root.
- Scene bootstrap is inconsistent: `GameSceneBootstrap` exists in code but was not found wired into the active `GameScene`/transfer prefabs.
- Day/night code exists, but `GameManager`'s `dayNightCycleManager` reference is null in prefab and no active `DayNightCycleManager` was found in the inspected scene wiring.
- Flow field pathfinding is implemented in parallel with NavMesh movement, but active gameplay wiring still routes through `UnitMovement`/`NavMeshAgent`.

## Code Quality Risks
- `FindAnyObjectByType`/`FindFirstObjectByType`/`Camera.main` are used broadly across runtime code, creating fragile startup ordering and hidden dependencies.
- Many exceptions are swallowed silently in core systems (`EventBus`, `SaveLoadManager`, `RTSSettingsManager`, `ShaderPreloader`), reducing debuggability.
- Some systems duplicate responsibility:
  - `ResourceUI` is attached under more than one manager/UI object.
  - Selection logic is split between `UnitSelectionManager`, `RTSCommandHandler`, `BuildingSelectionManager`, `UnifiedControlGroupManager`, and cursor/UI observers.
  - Save/load is spread across `SaveLoadManager`, `SaveLoadInputHandler`, `SaveLoadMenu`, `SceneTransitionManager`, and `GameManager`.
- There are many TODO placeholders in authored systems, but the larger issue is incomplete integration rather than isolated TODO comments.
