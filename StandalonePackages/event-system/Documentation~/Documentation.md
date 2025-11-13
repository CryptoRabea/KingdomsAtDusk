# Event System - Technical Documentation

## Overview
Type-safe event bus for decoupled communication between systems.

## Architecture
Dictionary-based event routing with Action delegates. Generic type parameter ensures type safety.

## API Reference

### Main Classes
#### EventBus
Location: `Assets/Scripts/Core/EventBus.cs`

#### Events
Location: `Assets/Scripts/Core/Events.cs`


## Usage Examples
// Subscribe
EventBus.Subscribe<BuildingPlacedEvent>(OnBuildingPlaced);

// Publish
EventBus.Publish(new BuildingPlacedEvent(building, position));

// Unsubscribe
EventBus.Unsubscribe<BuildingPlacedEvent>(OnBuildingPlaced);

## Configuration
No configuration needed. Add event definitions to Events.cs.

## Best Practices
- Always unsubscribe in OnDestroy to prevent memory leaks
- Keep event data immutable
- Name events with past tense (BuildingPlaced, not PlaceBuilding)
- Don't use events for immediate responses - use direct calls
