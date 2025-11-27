# Changelog

All notable changes to the Top-Down Wall Building System package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-11-27

### Added
- Initial release of Top-Down Wall Building System
- Pole-to-pole wall placement with intelligent mesh fitting
- Automatic wall segmentation with perfect end-to-end placement
- Last segment scaling to fill gaps (minimum 30% scale)
- Overlap detection for walls and buildings
- Endpoint and midpoint snapping for wall connections
- Loop closure support (connect back to first pole)
- Real-time resource cost preview
- Visual feedback system (green/red preview materials, line renderer)
- Affordability checking and validation
- Wall connection system with automatic neighbor detection
- NavMesh integration via WallNavMeshObstacle
- Optional wall stairs/ramps with NavMeshLink
- Building lifecycle management and construction timers
- Event system for wall placement, completion, and destruction
- Resource management service interface
- Service locator pattern for dependency injection
- Editor tools for wall prefab setup
- Custom inspector for WallConnectionSystem
- Comprehensive documentation and README
- Full source code with detailed comments

### Components
- **Core Systems**:
  - ServiceLocator - Dependency injection
  - EventBus - Publish-subscribe messaging
  - IResourcesService - Resource management interface
  - GameEvents - Event definitions

- **Wall Systems**:
  - WallPlacementController - Main placement logic
  - WallConnectionSystem - Connection detection and management
  - WallNavMeshObstacle - NavMesh carving for AI pathfinding
  - WallStairs - Traversable wall support
  - Building - Wall lifecycle and construction
  - BuildingDataSO - Wall configuration data

- **UI**:
  - WallResourcePreviewUI - Cost preview display

- **Editor**:
  - WallConnectionSystemEditor - Custom inspector
  - WallPrefabSetupUtility - Prefab setup tool

### Features
- ✅ Pole-to-pole placement workflow
- ✅ Perfect mesh-based fitting (no gaps)
- ✅ Intelligent scaling for last segment
- ✅ Overlap detection (walls + buildings)
- ✅ Endpoint/midpoint snapping
- ✅ Loop closure detection
- ✅ Resource cost calculation
- ✅ Real-time affordability checking
- ✅ Visual preview system
- ✅ Event-driven architecture
- ✅ NavMesh integration
- ✅ Connection system
- ✅ Editor utilities

### Dependencies
- Unity 2021.3+
- Unity Input System 1.4.4+

### Known Limitations
- Walls must be straight (no curves)
- Designed for top-down perspective (adaptable to others)
- No built-in multiplayer support
- No built-in save/load system

### Notes
- Package is fully standalone and self-contained
- Fog of War integration removed (made optional)
- All dependencies on game-specific code removed
- Ready for use in any Unity project
- Namespace: `TopDownWallBuilding.*`

---

## Future Roadmap

### [1.1.0] - Planned
- Sample scenes and examples
- Prefab templates
- Additional editor tools
- Performance profiling and optimization

### [1.2.0] - Planned
- Curved wall support
- Wall upgrade system
- Damage and repair mechanics
- Multiplayer synchronization helpers

### [2.0.0] - Planned
- Visual node editor for wall blueprints
- Advanced connection types (gates, towers)
- Procedural wall decoration system
- Save/load system
