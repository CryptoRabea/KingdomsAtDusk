# Wall Connection System - Technical Documentation

## Overview
Advanced modular wall system with automatic connections, pole-to-pole placement, and multiple construction modes.

## Architecture
Static grid-based registry with O(1) neighbor lookups. Bitmask state encoding for 16 visual variants. Event-driven updates.

## API Reference

### Main Classes
#### WallConnectionSystem
Location: `Assets/Scripts/RTSBuildingsSystems/WallConnectionSystem.cs`

#### WallPlacementController
Location: `Assets/Scripts/RTSBuildingsSystems/WallPlacementController.cs`

#### WallSegmentConstructor
Location: `Assets/Scripts/RTSBuildingsSystems/WallSegmentConstructor.cs`

#### ConstructionMode
Location: `Assets/Scripts/RTSBuildingsSystems/ConstructionMode.cs`

#### WallResourcePreviewUI
Location: `Assets/Scripts/UI/WallResourcePreviewUI.cs`


## Usage Examples
// Walls automatically use pole-to-pole placement
BuildingManager manager = FindFirstObjectByType<BuildingManager>();
BuildingDataSO wallData = manager.GetBuildingByName("Stone Wall");
manager.StartPlacingBuilding(wallData); // Click twice to place walls

## Configuration
Configure WallPlacementController in scene. Set construction mode on wall prefabs. Assign 16 mesh variants to WallConnectionSystem.

## Best Practices
- Keep gridSize=1.0 across all wall systems
- Create all 16 mesh variants for proper connections
- Use pole-to-pole mode for long walls
- Choose construction mode based on gameplay needs
- Assign workers programmatically for SegmentWithWorkers mode
