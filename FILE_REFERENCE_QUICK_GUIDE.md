# Quick File Reference Table

## Core Architecture Files

| Category | File Path | Purpose | Key Class |
|----------|-----------|---------|-----------|
| **Camera/Rendering** | `Camera/RTSCameraController.cs` | Main camera control, zoom, pan, rotation | `RTSCameraController` |
| **Event System** | `Core/EventBus.cs` | Publish-subscribe event system | `EventBus` (static) |
| **Event Definitions** | `Core/GameEvents.cs` | All game event struct definitions | Various event structs |
| **Services** | `Core/IServices.cs` | Service interfaces and enums | Resource, Happiness, Building, etc. |
| **Game Manager** | `Managers/GameManager.cs` | Service initialization and game lifecycle | `GameManager` |

## Building System Files

| File | Purpose | Key Class |
|------|---------|-----------|
| `RTSBuildingsSystems/Building.cs` | Core building component | `Building` |
| `RTSBuildingsSystems/BuildingDataSO.cs` | Building configuration ScriptableObject | `BuildingDataSO` |
| `Managers/BuildingManager.cs` | Building placement, management (IBuildingService) | `BuildingManager` |
| `RTSBuildingsSystems/BuildingSelectable.cs` | Building selection UI | `BuildingSelectable` |
| `RTSBuildingsSystems/Tower.cs` | Tower-specific logic | `Tower` |
| `RTSBuildingsSystems/TowerDataSO.cs` | Tower configuration | `TowerDataSO` |

## Unit System Files

| File | Purpose | Key Class |
|------|---------|-----------|
| `Units/Components/UnitHealth.cs` | Health, damage, death | `UnitHealth` |
| `Units/Components/UnitMovement.cs` | NavMesh movement control | `UnitMovement` |
| `Units/Components/UnitCombat.cs` | Combat mechanics | `UnitCombat` |
| `Units/Components/UnitSelectable.cs` | Selection highlight | `UnitSelectable` |
| `Units/Data/UnitConfigSO.cs` | Unit stats configuration | `UnitConfigSO` |
| `Units/AI/UnitAIController.cs` | AI state machine controller | `UnitAIController` |
| `Units/AI/States/UnitState.cs` | Base AI state class | `UnitState` |
| `Units/AI/States/IdleState.cs` | Idle AI state | `IdleState` |
| `Units/AI/States/MovingState.cs` | Movement AI state | `MovingState` |
| `Units/AI/States/AttackingState.cs` | Combat AI state | `AttackingState` |

## Minimap System Files

| File | Purpose | Key Class |
|------|---------|-----------|
| `UI/MiniMapController.cs` | Main minimap controller | `MiniMapController` |
| `UI/Minimap/IMinimapEntity.cs` | Entity interface for minimap | `IMinimapEntity` interface |
| `UI/Minimap/MinimapEntity.cs` | Minimap entity component | `MinimapEntity` |
| `UI/Minimap/MinimapConfig.cs` | Minimap configuration SO | `MinimapConfig` |
| `UI/Minimap/MinimapMarkerManager.cs` | Base marker manager (abstract) | `MinimapMarkerManager` |
| `UI/Minimap/MinimapUnitMarkerManager.cs` | Unit marker management | `MinimapUnitMarkerManager` |
| `UI/Minimap/MinimapBuildingMarkerManager.cs` | Building marker management | `MinimapBuildingMarkerManager` |
| `UI/Minimap/MinimapMarkerPool.cs` | Object pool for markers | `MinimapMarkerPool` |

## Selection System Files

| File | Purpose | Key Class |
|------|---------|-----------|
| `Units/Selection/UnitSelectionManager.cs` | Unit selection management | `UnitSelectionManager` |
| `Units/Selection/UnitSelection3D.cs` | 3D unit selection raycast | `UnitSelection3D` |
| `Units/Selection/RTSCommandHandler.cs` | Command execution | `RTSCommandHandler` |
| `RTSBuildingsSystems/BuildingSelectionManager.cs` | Building selection | `BuildingSelectionManager` |

## UI Files

| File | Purpose | Key Class |
|------|---------|-----------|
| `UI/BuildingDetailsUI.cs` | Building info display | `BuildingDetailsUI` |
| `UI/ResourceUI.cs` | Resource display | `ResourceUI` |
| `UI/BuildingHUDToggle.cs` | Building HUD toggle | `BuildingHUDToggle` |
| `UI/TrainUnitButton.cs` | Unit training button | `TrainUnitButton` |

---

# World Coordinate System

## Position Mapping

```
World Position          Minimap Position
X: -1000 to 1000   →   Left to Right
Y: Terrain height  →   Height (not visible on minimap)
Z: -1000 to 1000   →   Bottom to Top (displayed as Y in UI)
```

## Access Patterns

### Get Unit Position
```csharp
UnitAIController unit = GetUnit();
Vector3 position = unit.transform.position;
// OR
Vector3 position = unit.Movement.transform.position;
```

### Get Building Position
```csharp
Building building = GetBuilding();
Vector3 position = building.transform.position;
Vector3 position = building.Data; // Get config data
```

### Get Ownership
```csharp
// Layer-based
bool isEnemy = unit.gameObject.layer == LayerMask.NameToLayer("Enemy");

// Tag-based
bool isFriendly = unit.gameObject.CompareTag("Friendly");

// IMinimapEntity-based (if component exists)
if (unit.TryGetComponent<MinimapEntity>(out var entity))
{
    var ownership = entity.GetOwnership();
}
```

---

# Event Subscription Pattern

## Standard Subscription (in OnEnable)
```csharp
private void OnEnable()
{
    EventBus.Subscribe<UnitSpawnedEvent>(OnUnitSpawned);
    EventBus.Subscribe<UnitDiedEvent>(OnUnitDied);
    EventBus.Subscribe<BuildingPlacedEvent>(OnBuildingPlaced);
}

private void OnDisable()
{
    EventBus.Unsubscribe<UnitSpawnedEvent>(OnUnitSpawned);
    EventBus.Unsubscribe<UnitDiedEvent>(OnUnitDied);
    EventBus.Unsubscribe<BuildingPlacedEvent>(OnBuildingPlaced);
}
```

## Event Handler Signature
```csharp
private void OnUnitSpawned(UnitSpawnedEvent evt)
{
    GameObject unit = evt.Unit;
    Vector3 position = evt.Position;
    // Handle event
}
```

---

# Finding Objects in Scene

## Find All Units
```csharp
var allUnits = FindObjectsByType<UnitAIController>(FindObjectsSortMode.None);
```

## Find All Buildings
```csharp
var allBuildings = FindObjectsByType<Building>(FindObjectsSortMode.None);
```

## Find Specific Unit Component
```csharp
var health = unit.GetComponent<UnitHealth>();
var movement = unit.GetComponent<UnitMovement>();
var combat = unit.GetComponent<UnitCombat>();
var selectable = unit.GetComponent<UnitSelectable>();
```

---

# Layer and Tag Configuration

## Required Layers
- "Enemy" - Enemy units and buildings
- "Default" - Friendly units and buildings (assumed)
- "Terrain" - Terrain colliders (excluded from placement checks)

## Recommended Tags
- "Friendly" - Player units
- "Enemy" - Enemy units
- "Neutral" - Neutral entities
- "Unit" - All moveable units
- "Building" - All static buildings

---

# Service Locator Access Pattern

## Register Service (in GameManager)
```csharp
ServiceLocator.Register<IResourcesService>(resourceManager);
ServiceLocator.Register<IBuildingService>(buildingManager);
```

## Retrieve Service
```csharp
var resourceService = ServiceLocator.TryGet<IResourcesService>();
var buildingService = ServiceLocator.TryGet<IBuildingService>();

if (resourceService != null)
{
    bool canAfford = resourceService.CanAfford(costs);
    resourceService.SpendResources(costs);
}
```

---

# Performance Considerations for Fog of War

## Minimap Optimization (Already Implemented)
- Object pooling for markers
- Batched update system (max 100 markers/frame)
- Marker culling for off-screen entities
- Separate render texture with configurable resolution

## Recommended FOW Optimizations
- Use spatial partitioning for vision range checks
- Cache visibility calculations (don't recalculate every frame)
- Use layers to hide/show entities instead of disabling
- Batch shader updates for fogged materials
- Consider using a height-based visibility grid

## Building Detection Performance
```csharp
// GOOD: Only iterate when needed
if (needsUpdate)
{
    foreach (var building in FindObjectsByType<Building>())
    {
        // Process building
    }
}

// BETTER: Cache references
private List<Building> cachedBuildings;

private void CacheBuildingReferences()
{
    cachedBuildings = new List<Building>(FindObjectsByType<Building>());
}

private void ProcessBuildings()
{
    foreach (var building in cachedBuildings)
    {
        // Process cached reference
    }
}
```

