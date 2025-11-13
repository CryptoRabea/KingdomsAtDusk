# Building Selection System - Technical Documentation

## Overview
Building selection and highlighting system with camera integration.

## Architecture
Raycast-based selection with input system integration.

## API Reference

### Main Classes
#### BuildingSelectable
Location: `Assets/Scripts/RTSBuildingsSystems/BuildingSelectable.cs`

#### BuildingSelectionManager
Location: `Assets/Scripts/RTSBuildingsSystems/BuildingSelectionManager.cs`


## Usage Examples
// Buildings with BuildingSelectable can be clicked
// Manager automatically handles selection and events

## Configuration
Assign camera and configure input in BuildingSelectionManager.

## Best Practices
- Add BuildingSelectable to all interactive buildings
- Subscribe to BuildingSelectedEvent for UI updates
