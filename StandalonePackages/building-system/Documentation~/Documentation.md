# Building System - Technical Documentation

## Overview
Complete RTS building system with placement, construction, and data-driven configuration.

## Architecture
Data-driven architecture using ScriptableObjects. Event-driven placement and construction.

## API Reference

### Main Classes
#### BuildingManager
Location: `Assets/Scripts/Managers/BuildingManager.cs`

#### Building
Location: `Assets/Scripts/RTSBuildingsSystems/Building.cs`

#### BuildingDataSO
Location: `Assets/Scripts/RTSBuildingsSystems/BuildingDataSO.cs`

#### BuildingButton
Location: `Assets/Scripts/RTSBuildingsSystems/BuildingButton.cs`

#### BuildingSelectable
Location: `Assets/Scripts/RTSBuildingsSystems/BuildingSelectable.cs`

#### BuildingSelectionManager
Location: `Assets/Scripts/RTSBuildingsSystems/BuildingSelectionManager.cs`


## Usage Examples
BuildingManager manager = FindFirstObjectByType<BuildingManager>();
BuildingDataSO wallData = manager.GetBuildingByName("Stone Wall");
manager.StartPlacingBuilding(wallData);

## Configuration
Create BuildingDataSO assets for each building type. Configure costs, construction time, and bonuses.

## Best Practices
- Use BuildingDataSO as source of truth
- Keep grid size consistent across systems
- Always check resource affordability before placement
- Use event system for placement/destruction notifications
