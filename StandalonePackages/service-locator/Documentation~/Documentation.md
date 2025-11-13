# Service Locator - Technical Documentation

## Overview
Dependency injection pattern implementation for accessing game services.

## Architecture
Dictionary-based service storage with interface keys. Supports TryGet for optional services.

## API Reference

### Main Classes
#### ServiceLocator
Location: `Assets/Scripts/Core/ServiceLocator.cs`

#### IServices
Location: `Assets/Scripts/Core/IServices.cs`


## Usage Examples
// Register
ServiceLocator.Register<IResourcesService>(resourceManager);

// Get
IResourcesService resources = ServiceLocator.Get<IResourcesService>();

// Try Get (returns null if not found)
var service = ServiceLocator.TryGet<IResourcesService>();

## Configuration
Services are typically registered in Awake() or Start() methods of manager classes.

## Best Practices
- Always code against interfaces, not implementations
- Register services early (Awake/Start)
- Use TryGet for optional services
- Don't abuse - use for truly global services only
