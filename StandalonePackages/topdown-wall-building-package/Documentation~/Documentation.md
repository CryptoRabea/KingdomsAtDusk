# Top-Down Wall Building System - Complete Documentation

## Table of Contents
1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Core Components](#core-components)
4. [Integration Guide](#integration-guide)
5. [Advanced Usage](#advanced-usage)
6. [Performance Optimization](#performance-optimization)
7. [FAQ](#faq)

## Overview

The Top-Down Wall Building System provides a complete solution for pole-to-pole wall placement in Unity games. It handles mesh fitting, overlap detection, resource costs, and NavMesh integration automatically.

### Key Concepts

**Pole-to-Pole Placement**: Users click two points (poles) and the system automatically fills the space between them with wall segments.

**Perfect Mesh Fitting**: The system detects your wall mesh dimensions and places segments end-to-end with no gaps. The last segment scales to fill any remaining space perfectly.

**Smart Overlap Detection**: Walls can connect at endpoints but cannot overlap buildings or other wall segments.

## Architecture

### Core Systems

```
TopDownWallBuilding
├── Core
│   ├── ServiceLocator - Dependency injection
│   ├── EventBus - Pub/sub messaging
│   ├── IServices - Resource service interface
│   └── GameEvents - Event definitions
├── WallSystems
│   ├── WallPlacementController - Main placement logic
│   ├── WallConnectionSystem - Wall neighbor detection
│   ├── WallNavMeshObstacle - AI pathfinding blocker
│   ├── WallStairs - Traversable wall support
│   ├── Building - Wall lifecycle management
│   └── BuildingDataSO - Wall configuration data
├── UI
│   └── WallResourcePreviewUI - Cost preview display
└── Editor
    ├── WallConnectionSystemEditor - Custom inspector
    └── WallPrefabSetupUtility - Prefab setup tool
```

### Data Flow

1. **Initialization**: User clicks wall button → `StartPlacingWalls()` called
2. **First Pole**: User clicks ground → First pole placed
3. **Preview**: User moves mouse → Segment previews calculated and shown
4. **Second Pole**: User clicks ground → Segments instantiated
5. **Cleanup**: Previews destroyed → Mode continues or exits

## Core Components

### WallPlacementController

Main component that handles the pole-to-pole placement workflow.

**Responsibilities:**
- Mouse input handling
- Pole placement
- Segment calculation
- Overlap detection
- Preview rendering
- Resource checking

**Key Methods:**
```csharp
// Start placement mode with specified wall data
StartPlacingWalls(BuildingDataSO wallData)

// Cancel current placement
CancelWallPlacement()

// Internal: Calculate wall segments between two points
CalculateWallSegmentsWithScaling(Vector3 start, Vector3 end)

// Internal: Check if placement would overlap
WouldOverlapExistingWall(Vector3 start, Vector3 end)
```

### WallConnectionSystem

Handles automatic detection and management of wall-to-wall connections.

**Features:**
- Distance-based connection detection
- No grid required (free placement)
- Automatic updates when walls placed/destroyed
- Prevents cascading update loops

**Usage:**
```csharp
// Update this wall's connections
wallSystem.UpdateConnections();

// Get connected walls
List<WallConnectionSystem> connected = wallSystem.GetConnectedWalls();

// Check if connected to specific wall
bool isConnected = wallSystem.IsConnectedTo(otherWall);
```

### BuildingDataSO

ScriptableObject that stores wall configuration data.

**Properties:**
- `buildingName`: Display name
- `description`: Description text
- `buildingPrefab`: Wall prefab reference
- `woodCost`, `stoneCost`, `foodCost`, `goldCost`: Resource costs
- `constructionTime`: Time to build in seconds

**Methods:**
```csharp
// Get costs as dictionary
Dictionary<ResourceType, int> costs = wallData.GetCosts();

// Get formatted string for UI
string costText = wallData.GetCostString(); // "Wood: 10, Stone: 5"
```

## Integration Guide

### Step 1: Implement Resource Service

Create a class that implements `IResourcesService`:

```csharp
using TopDownWallBuilding.Core.Services;
using System.Collections.Generic;
using UnityEngine;

public class MyResourceManager : MonoBehaviour, IResourcesService
{
    // Storage
    private Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>();

    // Initialize starting resources
    private void Awake()
    {
        resources[ResourceType.Wood] = 1000;
        resources[ResourceType.Stone] = 500;
        resources[ResourceType.Gold] = 200;
        resources[ResourceType.Food] = 300;
    }

    // IResourcesService implementation
    public int GetResource(ResourceType type)
    {
        return resources.GetValueOrDefault(type, 0);
    }

    public bool CanAfford(Dictionary<ResourceType, int> costs)
    {
        foreach (var cost in costs)
        {
            if (GetResource(cost.Key) < cost.Value)
                return false;
        }
        return true;
    }

    public bool SpendResources(Dictionary<ResourceType, int> costs)
    {
        if (!CanAfford(costs))
            return false;

        foreach (var cost in costs)
        {
            resources[cost.Key] -= cost.Value;
        }

        // Publish event to update UI
        TopDownWallBuilding.Core.Events.EventBus.Publish(
            new TopDownWallBuilding.Core.Events.ResourcesChangedEvent(
                -costs.GetValueOrDefault(ResourceType.Wood, 0),
                -costs.GetValueOrDefault(ResourceType.Food, 0),
                -costs.GetValueOrDefault(ResourceType.Gold, 0),
                -costs.GetValueOrDefault(ResourceType.Stone, 0)
            )
        );

        return true;
    }

    public void AddResources(Dictionary<ResourceType, int> amounts)
    {
        foreach (var amount in amounts)
        {
            resources[amount.Key] = resources.GetValueOrDefault(amount.Key, 0) + amount.Value;
        }
    }

    // Legacy properties for backward compatibility
    public int Wood => GetResource(ResourceType.Wood);
    public int Food => GetResource(ResourceType.Food);
    public int Gold => GetResource(ResourceType.Gold);
    public int Stone => GetResource(ResourceType.Stone);
}
```

### Step 2: Register Service

Register your service during game initialization:

```csharp
using TopDownWallBuilding.Core.Services;
using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    [SerializeField] private MyResourceManager resourceManager;

    private void Awake()
    {
        // Register the resource service
        ServiceLocator.Register<IResourcesService>(resourceManager);

        Debug.Log("Resource service registered!");
    }
}
```

### Step 3: Create Wall Prefab

1. Create a GameObject with your wall mesh
2. Add components:
   - `Building` - Lifecycle management
   - `WallConnectionSystem` - Connection detection
   - `WallNavMeshObstacle` - NavMesh blocking
   - `Collider` - For overlap detection (will be disabled after placement)

3. Orient mesh:
   - Wall length should be along X-axis (or configure `wallLengthAxis`)
   - Pivot should be centered
   - Scale should be (1, 1, 1) if possible

4. Save as prefab

### Step 4: Create Wall Data

1. Right-click in Project → Create → Top-Down Wall Building → Wall Data
2. Name it (e.g., "StoneWallData")
3. Configure:
   - Assign wall prefab
   - Set costs (e.g., Wood: 10, Stone: 5)
   - Set construction time (e.g., 2.0 seconds)

### Step 5: Setup Scene

1. Create empty GameObject named "WallPlacementSystem"
2. Add `WallPlacementController` component
3. Configure settings:
   - Assign main camera
   - Set ground layer mask
   - Create and assign preview materials (green/red)
   - Set wall length axis to match your prefab

### Step 6: Create UI

```csharp
using TopDownWallBuilding.WallSystems;
using UnityEngine;
using UnityEngine.UI;

public class BuildingUI : MonoBehaviour
{
    [SerializeField] private WallPlacementController wallPlacer;
    [SerializeField] private BuildingDataSO stoneWallData;
    [SerializeField] private Button buildWallButton;

    private void Start()
    {
        buildWallButton.onClick.AddListener(OnBuildWallClicked);
    }

    private void OnBuildWallClicked()
    {
        wallPlacer.StartPlacingWalls(stoneWallData);
    }
}
```

## Advanced Usage

### Custom Wall Types

Create multiple wall data assets for different wall types:

```csharp
// Wooden Palisade - cheap, fast
WoodWallData: Wood=5, constructionTime=1s

// Stone Wall - expensive, durable
StoneWallData: Wood=10, Stone=15, constructionTime=5s

// Reinforced Wall - very expensive
ReinforcedWallData: Wood=20, Stone=30, Gold=5, constructionTime=10s
```

### Listening to Events

Subscribe to wall events for custom logic:

```csharp
using TopDownWallBuilding.Core.Events;
using UnityEngine;

public class WallEventHandler : MonoBehaviour
{
    private void OnEnable()
    {
        EventBus.Subscribe<BuildingPlacedEvent>(OnWallPlaced);
        EventBus.Subscribe<BuildingCompletedEvent>(OnWallCompleted);
        EventBus.Subscribe<BuildingDestroyedEvent>(OnWallDestroyed);
        EventBus.Subscribe<BuildingPlacementFailedEvent>(OnPlacementFailed);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<BuildingPlacedEvent>(OnWallPlaced);
        EventBus.Unsubscribe<BuildingCompletedEvent>(OnWallCompleted);
        EventBus.Unsubscribe<BuildingDestroyedEvent>(OnWallDestroyed);
        EventBus.Unsubscribe<BuildingPlacementFailedEvent>(OnPlacementFailed);
    }

    private void OnWallPlaced(BuildingPlacedEvent evt)
    {
        Debug.Log($"Wall placed at {evt.Position}");
        // Play placement sound
    }

    private void OnWallCompleted(BuildingCompletedEvent evt)
    {
        Debug.Log($"Wall construction completed: {evt.BuildingName}");
        // Play completion sound, show VFX
    }

    private void OnWallDestroyed(BuildingDestroyedEvent evt)
    {
        Debug.Log($"Wall destroyed: {evt.BuildingName}");
        // Play destruction sound
    }

    private void OnPlacementFailed(BuildingPlacementFailedEvent evt)
    {
        Debug.LogWarning($"Placement failed: {evt.Reason}");
        // Show error message to player
    }
}
```

### Programmatic Wall Placement

Place walls from code without user input:

```csharp
// NOT RECOMMENDED - WallPlacementController is designed for interactive placement
// For programmatic placement, instantiate prefabs directly:

Vector3 start = new Vector3(0, 0, 0);
Vector3 end = new Vector3(10, 0, 0);
Vector3 direction = (end - start).normalized;
float distance = Vector3.Distance(start, end);

// Calculate segments
int segmentCount = Mathf.FloorToInt(distance / wallMeshLength);
for (int i = 0; i < segmentCount; i++)
{
    Vector3 pos = start + direction * (i * wallMeshLength + wallMeshLength * 0.5f);
    Quaternion rot = Quaternion.LookRotation(direction);
    GameObject wall = Instantiate(wallPrefab, pos, rot);
}
```

## Performance Optimization

### Wall Connection Updates

The system prevents cascading updates:
- Updates are batched
- Delayed initialization (0.1s)
- Recursive update prevention

### Preview Rendering

Previews are optimized:
- VisionProvider components destroyed
- Colliders disabled
- SharedMaterial used (no instantiation)
- Previews pooled and reused

### NavMesh Carving

WallNavMeshObstacle settings:
- `carveOnlyStationary`: true (only carve when stationary)
- `carvingMoveThreshold`: 0.1m
- `carvingTimeToStationary`: 0.5s

### Recommended Settings

For best performance:
- Keep `connectionDistance` reasonable (1.5-2.0m)
- Use `minParallelOverlap` of 0.5m or higher
- Disable colliders on placed walls (done automatically)
- Use object pooling for wall prefabs if building/destroying frequently

## FAQ

**Q: Walls have gaps between segments**
A: Enable `useAutoMeshSize` and verify `wallLengthAxis` matches your mesh orientation.

**Q: Last segment is too small**
A: Increase `minScaleFactor` to allow smaller segments, or adjust wall mesh size.

**Q: Walls overlap when connecting**
A: This is expected at endpoints. Increase `minParallelOverlap` to prevent collinear overlaps.

**Q: Can I use this for 3D games?**
A: Yes! The system works in any perspective. Just configure your camera and ground layer appropriately.

**Q: How do I change wall prefab dynamically?**
A: Create multiple BuildingDataSO assets and call `StartPlacingWalls()` with different data.

**Q: Can walls be curved?**
A: No, this system uses straight segments. For curves, place multiple short walls at angles.

**Q: How do I destroy walls?**
A: Simply destroy the wall GameObject. The system will publish `BuildingDestroyedEvent`.

**Q: Can I upgrade walls?**
A: Not built-in. Implement by destroying old wall and placing new one at same position.

**Q: Does this work with multiplayer?**
A: Not out of the box. You'll need to sync wall placement over network.

**Q: How do I save/load walls?**
A: Serialize wall positions, rotations, and scales. Recreate on load.

**Q: Can I change costs at runtime?**
A: Yes! Modify the BuildingDataSO fields directly.

## Support

For additional help, check the README.md or contact support.
