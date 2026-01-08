# Kingdoms at Dusk - Development Roadmap

This roadmap outlines the planned development path for the game. Items are organized by priority and development phase.

## Phase 1: Core Gameplay Systems ✅ (Mostly Complete)

### Essential RTS Mechanics
- [x] Unit selection (box select, double-click, control groups)
- [x] Unit movement and pathfinding (NavMesh)
- [x] Unit combat system
- [x] Building placement and construction
- [x] Resource management (Wood, Food, Gold, Stone)
- [x] Population and housing system
- [x] Basic AI for units (aggro, retreat, return to origin)

### UI Systems
- [x] Resource UI display
- [x] Minimap
- [x] Building tooltips
- [x] Unit selection feedback
- [x] HUD and collapsible panels
- [x] Floating damage numbers

### Game State
- [x] Victory/Defeat conditions
- [x] Wave spawning system
- [x] Save/Load system

## Phase 2: Current Focus - Polish & Refinement 🔄

### Recently Completed
- [x] Fog of War implementation
- [x] Fog of War circular reveals with square grid
- [x] Minimap marker positioning fixes
- [x] Enemy spawner building

### Active Work
- [ ] **Gate selection bug fixes** (branch: `claude/fix-gate-selection-bug-01Eq1FMnLx3w1zof4Nrqx1ub`)
- [ ] Code cleanup and optimization
- [ ] Testing and bug fixes

### Known Issues to Address
Based on code comments (TODO/FIXME markers):
- BiomeManager: Needs completion/refinement
- GameConfigSO: Has 2 TODO items
- ControlGroupFeedbackUI: 2 items needing work
- WorkerGatheringAI: 1 TODO item
- BuildingGroupManager: 1 TODO item
- WallFlowFieldObstacle: 1 TODO item
- BuildingFlowFieldObstacle: 1 TODO item
- ArcherAnimationController: 1 TODO item
- WallConnectionSystem: 1 TODO item
- FormationSetupTool: 1 TODO item
- ResourceWorkerModule: 1 TODO item
- MinimapEntityDetector: 1 TODO item

## Phase 3: Major Feature Additions

### Flow Field Migration 🎯 **HIGH PRIORITY**
**Goal:** Replace NavMesh with Flow Field pathfinding for better performance

**Tasks:**
1. **Integration Planning**
   - Review existing FlowFieldManager, FlowFieldGrid, FlowFieldGenerator code
   - Plan migration strategy (gradual vs full replacement)
   - Identify all NavMeshAgent dependencies

2. **Unit Migration**
   - Replace `NavMeshAgent` component with `FlowFieldFollower`
   - Update `UnitMovement` to use flow fields
   - Test single unit movement
   - Test group movement

3. **Command System Integration**
   - Update `RTSCommandHandler` to use FlowFieldManager
   - Integrate with `FormationManager`
   - Test formation movement with flow fields

4. **AI System Updates**
   - Update all AI states to use flow field movement
   - Test aggro/chase/retreat behaviors
   - Ensure return-to-origin works correctly

5. **Performance Testing**
   - Benchmark large unit groups (50, 100, 200+ units)
   - Compare with NavMesh performance
   - Optimize flow field caching

6. **Building/Obstacle Integration**
   - Connect `BuildingFlowFieldObstacle` and `WallFlowFieldObstacle`
   - Ensure dynamic obstacle updates
   - Test pathfinding around buildings

**Estimated Complexity:** High (multiple systems affected)
**Benefits:** Major performance improvement for large battles

### Animal System Expansion
- [ ] Complete BiomeManager implementation
- [ ] Expand animal behaviors and interactions
- [ ] Hunting mechanics for food gathering
- [ ] Animal migration patterns

### Advanced Building Features
- [ ] Building upgrades system
- [ ] Building damage visual states
- [ ] More building types (markets, temples, etc.)
- [ ] Building special abilities/effects

### Enhanced Combat
- [ ] Unit abilities/special attacks
- [ ] Different damage types (pierce, slash, blunt)
- [ ] Armor system
- [ ] Morale system affecting combat performance
- [ ] Siege weapons

### Advanced AI
- [ ] Enemy AI behaviors (raiding, sieging)
- [ ] Defensive AI for towers and walls
- [ ] Squad-based AI tactics
- [ ] AI difficulty levels

## Phase 4: Content & Polish

### Campaign Mode
- [ ] Story missions
- [ ] Progressive difficulty
- [ ] Unlockable units and buildings
- [ ] Tutorial system

### Visual Polish
- [ ] Particle effects for combat
- [ ] Building destruction animations
- [ ] Weather effects integration (already have Enviro 3)
- [ ] Unit variety and visual customization

### Audio
- [ ] Complete audio implementation for all units
- [ ] Building construction sounds
- [ ] Ambient battle sounds
- [ ] Music system with dynamic layers

### UI/UX Improvements
- [ ] Complete ControlGroupFeedbackUI improvements
- [ ] Tech tree visualization
- [ ] Better unit formation controls
- [ ] In-game statistics and graphs
- [ ] Replay system

## Phase 5: Optimization & Release Preparation

### Performance
- [ ] Object pooling optimization
- [ ] LOD system for units and buildings
- [ ] Occlusion culling optimization
- [ ] Memory profiling and leak fixes
- [ ] Build size optimization

### Testing
- [ ] Full playthrough testing
- [ ] Balance testing
- [ ] Performance testing on target hardware
- [ ] Bug fixing pass

### Release
- [ ] Steam integration
- [ ] Achievements
- [ ] Cloud saves
- [ ] Localization support
- [ ] Final polish pass

## Future Considerations (Post-1.0)

### Multiplayer
- [ ] Network architecture planning
- [ ] 1v1 multiplayer
- [ ] Co-op wave defense
- [ ] Ranked matchmaking

### Modding Support
- [ ] Mod tools
- [ ] Custom unit/building support
- [ ] Custom maps
- [ ] Workshop integration

### Expansions
- [ ] New factions
- [ ] New biomes
- [ ] New units and buildings
- [ ] New campaign chapters

## Development Principles

1. **Performance First:** Always consider performance impact, especially for RTS-scale features
2. **Data-Driven:** Use ScriptableObjects for configuration to enable easy iteration
3. **Event-Driven UI:** Maintain separation between game logic and UI via EventBus
4. **Testability:** Use ServiceLocator pattern to enable testing
5. **Incremental Development:** Ship features incrementally, test thoroughly before moving on

## Priority Matrix

**Must Have (P0):**
- Flow Field Migration
- Bug fixes for known issues
- Performance optimization for large battles

**Should Have (P1):**
- Animal system completion
- Advanced combat features
- Campaign mode basics

**Nice to Have (P2):**
- Advanced AI behaviors
- Visual polish and effects
- Multiplayer support

**Future (P3):**
- Modding support
- Expansions
- Additional game modes

---

**Last Updated:** 2026-01-03
**Current Phase:** Phase 2 - Polish & Refinement
**Next Major Milestone:** Flow Field Migration (Phase 3)
