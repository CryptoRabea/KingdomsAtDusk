# RTS UI System - Technical Documentation

## Overview
Complete UI system for RTS games including building buttons, resource displays, and detail panels.

## Architecture
Event-driven UI updates. TextMeshPro for text rendering.

## API Reference

### Main Classes
#### BuildingButton
Location: `Assets/Scripts/RTSBuildingsSystems/BuildingButton.cs`

#### BuildingHUD
Location: `Assets/Scripts/RTSBuildingsSystems/BuildingHUD.cs`

#### BuildingDetailsUI
Location: `Assets/Scripts/UI/BuildingDetailsUI.cs`

#### BuildingTooltip
Location: `Assets/Scripts/RTSBuildingsSystems/BuildingTooltip.cs`


## Usage Examples
// UI updates automatically via events
// Use BuildingButton for building placement
// BuildingDetailsUI shows selected building info

## Configuration
Use setup tools in Tools > RTS menu for automatic configuration.

## Best Practices
- Subscribe to events for dynamic UI updates
- Use TextMeshPro for all text
- Keep UI responsive with event-driven updates
