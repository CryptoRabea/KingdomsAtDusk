# Resource Management System - Technical Documentation

## Overview
Complete resource management system with support for multiple resource types (Wood, Food, Gold, Stone). Data-driven design makes adding new resources trivial.

## Architecture
Dictionary-based resource storage with event notifications. Supports dynamic resource types through enum extension.

## API Reference

### Main Classes
#### IServices
Location: `Assets/Scripts/Core/IServices.cs`

#### ResourceManager
Location: `Assets/Scripts/Managers/ResourceManager.cs`


## Usage Examples
// Get resources
IResourcesService resources = ServiceLocator.Get<IResourcesService>();
int wood = resources.GetResource(ResourceType.Wood);

// Spend resources
var costs = ResourceCost.Build().Wood(100).Stone(50).Create();
bool success = resources.SpendResources(costs);

## Configuration
Configure starting resources in ResourceManager component inspector.

## Best Practices
- Always use IResourcesService interface, never ResourceManager directly
- Use ResourceCost.Build() for clean cost definitions
- Subscribe to ResourcesChangedEvent for UI updates
- Check CanAfford() before attempting to spend resources
