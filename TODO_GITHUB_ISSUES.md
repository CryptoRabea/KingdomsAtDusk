# GitHub Issues to Create

The following features have been identified as TODO items in the codebase. These fields were previously uncommented but are not yet implemented. They should be created as GitHub issues for future implementation.

---

## Issue 1: Implement Variable Obstacle Cost for FlowField

**Title**: Implement variable obstacle cost for FlowField obstacles

**Labels**: enhancement, pathfinding

**Description**:
Currently, FlowField obstacles (buildings and walls) use a hardcoded `UNWALKABLE_COST` value. We should implement support for variable obstacle costs to allow for more nuanced pathfinding behavior.

**Files Affected**:
- `Assets/Scripts/FlowField/Obstacles/BuildingFlowFieldObstacle.cs` (line 20-21)
- `Assets/Scripts/FlowField/Obstacles/WallFlowFieldObstacle.cs` (line 18-19)

**Implementation Details**:
- Add `obstacleCost` serialized field to both obstacle types
- Modify the cost field update logic to use the custom cost value
- Allow designers to configure different cost values (e.g., difficult terrain vs impassable)
- Ensure the FlowFieldManager properly handles variable costs

**Acceptance Criteria**:
- [ ] `obstacleCost` field is exposed in Inspector
- [ ] Custom cost values are properly applied to the cost grid
- [ ] Units pathfind correctly with variable cost obstacles
- [ ] Documentation updated to explain cost system

---

## Issue 2: Implement Aim-While-Moving for Archers

**Title**: Add aim-while-moving capability for archer units

**Labels**: enhancement, animation, combat

**Description**:
Archer units should support the ability to aim at targets while moving, creating more dynamic combat animations and gameplay.

**Files Affected**:
- `Assets/Scripts/RTSAnimation/ArcherAnimationController.cs` (line 21-22)

**Implementation Details**:
- Add `allowAimWhileMoving` boolean field
- Implement animation blending between movement and aim states
- Ensure upper body (aiming) can animate independently from lower body (movement)
- Add configurable settings for aim accuracy penalties while moving
- Update animation controller to support simultaneous movement/combat layers

**Acceptance Criteria**:
- [ ] Archers can aim and track targets while moving
- [ ] Animation blending looks natural
- [ ] Performance impact is minimal (LOD-friendly)
- [ ] Optional accuracy penalty can be configured
- [ ] Works with 8-way directional movement system

---

## Issue 3: Implement Resource Production Bonus System

**Title**: Add worker-based resource production bonus multiplier

**Labels**: enhancement, economy, workers

**Description**:
Implement a production bonus system where assigned workers can multiply resource generation rates for buildings.

**Files Affected**:
- `Assets/Scripts/RTSBuildingsSystems/WorkerModules/ResourceWorkerModule.cs` (line 19-20, 217-218)

**Implementation Details**:
- Add `resourceProductionBonus` multiplier field
- Create `SetResourceProductionMultiplier()` method on Building class
- Apply bonus when workers are assigned to resource buildings
- Remove bonus when workers are unassigned
- Update UI to show production rate with/without workers
- Balance testing to ensure reasonable bonus values

**Current Code Reference**:
```csharp
// Line 217-218 (currently commented):
// buildingComponent.SetResourceProductionMultiplier(apply ? resourceProductionBonus : 1f);
```

**Acceptance Criteria**:
- [ ] Production bonus is configurable per building type
- [ ] Bonus is correctly applied/removed with worker assignment
- [ ] UI displays current production rate including bonuses
- [ ] Building class has public method to set production multiplier
- [ ] System is performant with many buildings/workers

---

## Issue 4: Add Debug Logging Best Practices

**Title**: Establish debug logging guidelines and cleanup existing silent exceptions

**Labels**: technical-debt, logging, debugging

**Description**:
Multiple locations in the codebase silently catch and ignore exceptions, making debugging difficult. Establish logging best practices and improve exception handling.

**Files Affected**:
- `Assets/Scripts/Core/BuildInitializer.cs` (line 128-131)
- `Assets/Scripts/Core/EventBus.cs` (line 64-66)
- `Assets/Scripts/Core/ShaderPreloader.cs` (line 88-90)
- `Assets/Scripts/SaveLoad/SaveLoadMenu.cs` (line 464-466)

**Current Issues**:
- Critical initialization errors fail silently
- Event handler exceptions are swallowed without notification
- No logging for shader warmup failures
- Save/load errors are not reported

**Recommended Approach**:
1. Add logging for all caught exceptions at minimum
2. Use different log levels (Error, Warning, Info) appropriately
3. Consider adding a centralized logging system
4. Add conditional compilation for debug vs release builds
5. Follow the CLAUDE.md guideline to remove debug logs after issues are fixed

**Example Implementation**:
```csharp
// Instead of:
catch (System.Exception)
{
}

// Use:
catch (System.Exception ex)
{
    Debug.LogError($"[BuildInitializer] Initialization failed: {ex.Message}");
}
```

**Acceptance Criteria**:
- [ ] All exception handlers have appropriate logging
- [ ] Logging guidelines added to CLAUDE.md
- [ ] Critical errors are logged as `LogError`
- [ ] Non-critical issues use `LogWarning`
- [ ] Performance impact is minimal
- [ ] Conditional compilation for debug-only logs

---

## Priority Recommendations

1. **High Priority**: Issue #4 (Debug Logging) - Improves debuggability across the entire project
2. **Medium Priority**: Issue #3 (Production Bonus) - Partially implemented, just needs completion
3. **Low Priority**: Issue #1 (Variable Costs) - Nice to have, not critical for current gameplay
4. **Low Priority**: Issue #2 (Aim While Moving) - Polish feature, complex implementation

---

**Created**: 2025-12-12
**Author**: Claude Code Review
