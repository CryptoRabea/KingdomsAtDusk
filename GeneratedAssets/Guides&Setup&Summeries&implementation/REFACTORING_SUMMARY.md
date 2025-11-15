# Refactoring Summary - Quick Reference

## Top 5 Critical Issues

### 🔴 CRITICAL #1: Two Redundant Building UI Systems
**Files:** `BuildingUI.cs` and `BuildingHUD.cs`  
**Problem:** 70% code overlap, maintaining duplicate logic  
**Fix:** Delete BuildingUI.cs, keep BuildingHUD.cs  
**Time:** 3 hours

### 🔴 CRITICAL #2: BuildingManager Not in ServiceLocator  
**Files:** `GameManager.cs`, `BuildingManager.cs`  
**Problem:** Forces all UI to use FindObjectOfType fallbacks  
**Fix:** Register as IBuildingService in GameManager  
**Time:** 1 hour

### 🔴 CRITICAL #3: Cost Display Code Duplicated 5× Times
**Files:** BuildingUI, BuildingButton, TrainUnitButton, BuildingDataSO, BuildingTooltip  
**Problem:** Same formatting logic in 5 files (~100 duplicate lines)  
**Fix:** Create ResourceDisplayUtility static class  
**Time:** 2 hours

### 🟡 HIGH #4: Event-Based Affordability Checks Missing
**Files:** `BuildingButton.cs`, `TrainUnitButton.cs`  
**Problem:** Checking affordability every frame (600+ checks/sec)  
**Fix:** Use ResourcesChangedEvent, only check when needed  
**Time:** 1 hour

### 🟡 HIGH #5: ResourcesSpentEvent Missing Stone Field
**Files:** `GameEvents.cs`  
**Problem:** Constructor accepts stone but doesn't store it (bug)  
**Fix:** Add `public int Stone;` field  
**Time:** 15 minutes

---

## Issue Count by Category

| Category | Issues | Lines Affected |
|----------|--------|----------------|
| 1. Unnecessary References | 5 | 50+ |
| 2. Duplicated Code | 5 | 200+ |
| 3. Tight Coupling | 4 | 100+ |
| 4. Redundant Systems | 3 | 600+ |
| 5. Dead Code | 4 | 20+ |
| 6. Missing Centralization | 5 | N/A |
| **TOTAL** | **26** | **970+** |

---

## Refactoring Roadmap

### Phase 1: Foundation (Week 1) - 4 hours
- [ ] Register BuildingManager in ServiceLocator
- [ ] Merge BuildingUI into BuildingHUD
- [ ] Test all building placement flows

### Phase 2: Core Systems (Week 2) - 5.5 hours  
- [ ] Create ResourceDisplayUtility
- [ ] Replace all duplicated cost display code
- [ ] Decouple BuildingDetailsUI from SelectionManager
- [ ] Move affordability checks to events
- [ ] Cache building data arrays

### Phase 3: Cleanup (Week 3) - 4.25 hours
- [ ] Remove unnecessary references
- [ ] Centralize resource icon management  
- [ ] Fix ResourcesSpentEvent stone field
- [ ] Clean up commented code

### Phase 4: Architecture (Week 4) - 8.75 hours
- [ ] Create IInputService abstraction
- [ ] Implement InputManager
- [ ] Remove BuildingDataSO alias properties

**Total Time: ~22 hours**

---

## Quick Wins (< 30 minutes each)

1. ✅ Fix ResourcesSpentEvent.Stone field (15 min)
2. ✅ Cache building data in UI scripts (30 min)  
3. ✅ Remove commented code (15 min)
4. ✅ Fix BuildingTooltip property usage (15 min)

---

## Files Requiring Major Changes

| File | Changes | Priority |
|------|---------|----------|
| `BuildingUI.cs` | **DELETE** | P0 |
| `BuildingHUD.cs` | Keep, enhance | P0 |
| `GameManager.cs` | Add BuildingManager registration | P0 |
| `BuildingButton.cs` | Remove redundant code | P1 |
| `TrainUnitButton.cs` | Use utility class | P1 |
| `BuildingDetailsUI.cs` | Decouple from SelectionManager | P1 |
| `BuildingDataSO.cs` | Clean up aliases | P3 |

---

## New Files to Create

1. `Core/Utilities/ResourceDisplayUtility.cs` - Central cost formatting
2. `Core/IServices.cs` - Add IBuildingService interface
3. `Managers/UnityInputService.cs` - Input abstraction (Phase 4)
4. `Managers/InputManager.cs` - Central input (Phase 4)

---

## Performance Improvements

| Optimization | Before | After | Gain |
|--------------|--------|-------|------|
| Affordability checks | 600/sec | 1-5/sec | 99% |
| FindObjectOfType calls | 5 per Start() | 0 | 100% |
| Building data array allocations | 3+ per action | 1 cached | 67% |
| Code size (duplicate removal) | ~970 lines | ~470 lines | 50% |

---

## Architecture Improvements

### Before
```
BuildingUI ──FindObjectOfType──> BuildingManager
BuildingHUD ──FindObjectOfType──> BuildingManager  
BuildingButton ──FindObjectOfType──> ResourceUI
BuildingDetailsUI ──FindObjectOfType──> SelectionManager
```

### After  
```
ServiceLocator
├── IBuildingService (BuildingManager)
├── IResourcesService (ResourceManager)  
├── IResourceIconProvider (ResourceUI)
└── IInputService (InputManager)

UI Scripts ──ServiceLocator.Get<>──> Services
```

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Breaking existing scenes | HIGH | HIGH | Test all scenes, use [Obsolete] |
| UI not updating | MEDIUM | HIGH | Thorough event testing |
| Performance regression | LOW | MEDIUM | Profile before/after |
| Merge conflicts | MEDIUM | LOW | Work in feature branch |

---

## Success Criteria

✅ No FindObjectOfType warnings in console  
✅ All building costs display consistently  
✅ Building placement works in all scenarios  
✅ UI updates only when resources change  
✅ No duplicate systems (BuildingUI deleted)  
✅ All services registered in ServiceLocator  
✅ Code coverage > 60% for new utilities  

---

## Questions to Answer Before Starting

1. Are there any scenes still using BuildingUI that need migration?
2. Should we maintain backwards compatibility or clean break?
3. Do we need to support external mods/plugins?
4. What's the testing strategy (manual, automated, both)?

---

For full details, see: `CODEBASE_REFACTORING_REPORT.md`
