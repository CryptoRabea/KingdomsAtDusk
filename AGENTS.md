<!-- UNITY CODE ASSIST INSTRUCTIONS START -->
- Project name: KingdomsAtDusk
- Unity version: Unity 6000.2.10f1
- Active game object:
  - Name: [RUNTIME] Fog_Plane
  - Tag: Untagged
  - Layer: Default

## CRITICAL CODING RULES

### Input System
**NEVER use the old Input System:**
- ❌ DO NOT use `Input.GetKey()`, `Input.GetButton()`, `Input.GetAxis()`
- ❌ DO NOT use `Input.mousePosition`, `Input.GetMouseButton()`
- ✅ ALWAYS use the New Input System via `PlayerInput` component and Input Actions
- ✅ ALWAYS use `InputAction` callbacks and bindings

### Modern Unity APIs
**Object Finding (Unity 6):**
- ❌ DO NOT use `Object.FindObjectsOfType<T>()` (deprecated in Unity 6)
- ✅ ALWAYS use `Object.FindObjectsByType<T>(FindObjectsSortMode sortMode)`
  - Use `FindObjectsSortMode.None` for better performance when sorting is not needed
  - Use `FindObjectsSortMode.InstanceID` only when you need sorted results

### Architecture Patterns
- ✅ Use Service Locator for dependency injection (`ServiceLocator.Get<IService>()`)
- ✅ Use EventBus for decoupled communication (`EventBus.Publish<Event>()`)
- ✅ Use object pooling for frequently spawned objects (`IPoolService`)
- ✅ Follow namespace conventions (see DEVELOPMENT.md)

### Documentation
- See `DEVELOPMENT.md` for comprehensive development guidelines
- See `GAMEPLAY_FEATURES.md` for feature documentation
- Check system-specific README.md files in subsystem folders

<!-- UNITY CODE ASSIST INSTRUCTIONS END -->