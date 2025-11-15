# Tower System Implementation - Quick Reference Guide

## CRITICAL FILE LOCATIONS

### Core Building System
- **Building Component:** `/home/user/KingdomsAtDusk/Assets/Scripts/RTSBuildingsSystems/Building.cs`
- **Building Data (Config):** `/home/user/KingdomsAtDusk/Assets/Scripts/RTSBuildingsSystems/BuildingDataSO.cs`
- **Placement Manager:** `/home/user/KingdomsAtDusk/Assets/Managers/BuildingManager.cs`
- **Wall Placement:** `/home/user/KingdomsAtDusk/Assets/Scripts/RTSBuildingsSystems/WallPlacementController.cs`
- **Wall Connections:** `/home/user/KingdomsAtDusk/Assets/Scripts/RTSBuildingsSystems/WallConnectionSystem.cs`

### Core Infrastructure
- **Event System:** `/home/user/KingdomsAtDusk/Assets/Scripts/Core/GameEvents.cs`
- **Event Bus:** `/home/user/KingdomsAtDusk/Assets/Scripts/Core/EventBus.cs`
- **Services:** `/home/user/KingdomsAtDusk/Assets/Scripts/Core/IServices.cs`
- **Service Locator:** `/home/user/KingdomsAtDusk/Assets/Scripts/Core/ServiceLocator.cs`

### Managers
- **Resources:** `/home/user/KingdomsAtDusk/Assets/Scripts/Managers/ResourceManager.cs`
- **Happiness:** `/home/user/KingdomsAtDusk/Assets/Scripts/Managers/HappinessManager.cs`

### Combat System
- **Unit Combat:** `/home/user/KingdomsAtDusk/Assets/Scripts/Units/Components/UnitCombat.cs`
- **Unit Health:** `/home/user/KingdomsAtDusk/Assets/Scripts/Units/Components/UnitHealth.cs`

### Asset Data
- **Building Data Assets:** `/home/user/KingdomsAtDusk/Assets/Prefabs/BuildingPrefabs&Data/`
  - `TowerBuildingData.asset` ← Tower config exists!
- **Wall Data Assets:** `/home/user/KingdomsAtDusk/Assets/Prefabs/WallPrefabs&Data/`
  - `WallTowers_1_Data.asset` through `WallTowers_2_Data.asset`

---

## CODE SNIPPETS: HOW TO USE KEY SYSTEMS

### 1. ACCESS RESOURCES SERVICE
```csharp
// Get service
IResourcesService resourceService = ServiceLocator.TryGet<IResourcesService>();

// Check specific resource
int woodAmount = resourceService.GetResource(ResourceType.Wood);

// Check if can afford
var costs = new Dictionary<ResourceType, int> 
{ 
    { ResourceType.Wood, 100 },
    { ResourceType.Stone, 50 }
};
if (resourceService.CanAfford(costs))
{
    resourceService.SpendResources(costs);
}

// Add resources
var gains = new Dictionary<ResourceType, int> 
{ 
    { ResourceType.Food, 10 }
};
resourceService.AddResources(gains);
```

### 2. ACCESS HAPPINESS SERVICE
```csharp
// Get service
IHappinessService happinessService = ServiceLocator.TryGet<IHappinessService>();

// Add building bonus (e.g., tower increases defense happiness)
happinessService.AddBuildingBonus(5f, "Tower Defense");

// Remove when destroyed
happinessService.RemoveBuildingBonus(5f, "Tower Defense");

// Get current happiness
float currentHappiness = happinessService.CurrentHappiness;
```

### 3. SUBSCRIBE TO BUILDING EVENTS
```csharp
// Subscribe to building placement
EventBus.Subscribe<BuildingPlacedEvent>(OnBuildingPlaced);
EventBus.Subscribe<BuildingCompletedEvent>(OnBuildingCompleted);
EventBus.Subscribe<BuildingDestroyedEvent>(OnBuildingDestroyed);

// Event handlers
private void OnBuildingPlaced(BuildingPlacedEvent evt)
{
    Debug.Log($"Building placed at {evt.Position}");
    // If it's a tower, activate tower combat system
    var tower = evt.Building.GetComponent<TowerCombat>();
    if (tower != null)
    {
        tower.Activate();
    }
}

// Always unsubscribe in OnDestroy
private void OnDestroy()
{
    EventBus.Unsubscribe<BuildingPlacedEvent>(OnBuildingPlaced);
    EventBus.Unsubscribe<BuildingCompletedEvent>(OnBuildingCompleted);
    EventBus.Unsubscribe<BuildingDestroyedEvent>(OnBuildingDestroyed);
}
```

### 4. SUBSCRIBE TO RESOURCE EVENTS
```csharp
// Listen for resource changes
EventBus.Subscribe<ResourcesChangedEvent>(OnResourcesChanged);
EventBus.Subscribe<ResourcesSpentEvent>(OnResourcesSpent);

private void OnResourcesChanged(ResourcesChangedEvent evt)
{
    if (evt.WoodDelta != 0) Debug.Log($"Wood changed by {evt.WoodDelta}");
    if (evt.StoneDelta != 0) Debug.Log($"Stone changed by {evt.StoneDelta}");
}

private void OnResourcesSpent(ResourcesSpentEvent evt)
{
    if (evt.Success)
        Debug.Log($"Spent: Wood {evt.Wood}, Stone {evt.Stone}, Gold {evt.Gold}, Food {evt.Food}");
    else
        Debug.Log("Not enough resources!");
}
```

### 5. PLACE A BUILDING (TOWER)
```csharp
// Get building manager
BuildingManager buildingManager = GetComponent<BuildingManager>();

// Load tower building data asset
BuildingDataSO towerData = Resources.Load<BuildingDataSO>("Prefabs/BuildingPrefabs&Data/TowerBuildingData");

// Start placement mode
buildingManager.StartPlacingBuilding(towerData);

// Or by index if already configured in inspector
buildingManager.StartPlacingBuilding(3); // assumes tower is at index 3
```

### 6. GET BUILDING DATA
```csharp
// Get all buildings
var allBuildings = buildingManager.GetAllBuildingData();

// Get by type
var defensiveBuildings = buildingManager.GetBuildingsByType(BuildingType.Defensive);

// Get by name
BuildingDataSO tower = buildingManager.GetBuildingByName("Tower");

// Check affordability
bool canAfford = buildingManager.CanAffordBuilding(tower);

// Get costs
var costs = buildingManager.GetBuildingCost(tower);
```

### 7. CREATE A WALL
```csharp
// Get wall placement controller
WallPlacementController wallController = GetComponent<WallPlacementController>();

// Load wall data
BuildingDataSO wallData = Resources.Load<BuildingDataSO>("Prefabs/WallPrefabs&Data/Wall_1_Data");

// Start wall placement (pole-to-pole mode)
wallController.StartPlacingWalls(wallData);

// User clicks: first pole, then second pole to complete segment
// Walls auto-segment to fit distance with scaling
```

### 8. GET ALL WALLS IN SCENE
```csharp
// Static method to get all walls
List<WallConnectionSystem> allWalls = WallConnectionSystem.GetAllWalls();

foreach (var wall in allWalls)
{
    // Check connections
    int connectionCount = wall.GetConnectionCount();
    var neighbors = wall.GetConnectedWalls();
    
    // Get connection direction to a specific wall
    Vector3 direction = wall.GetConnectionDirection(neighbors[0]);
}
```

### 9. IMPLEMENT TOWER COMBAT (Template)
```csharp
using UnityEngine;
using RTS.Core.Events;
using RTS.Core.Services;

public class TowerCombat : MonoBehaviour
{
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private float attackDamage = 25f;
    [SerializeField] private float attackRate = 1f;
    [SerializeField] private LayerMask enemyLayers;
    
    private float lastAttackTime = -999f;
    private Transform currentTarget;
    private bool isActive = false;
    
    private void Start()
    {
        // Subscribe to events
        EventBus.Subscribe<BuildingCompletedEvent>(OnBuildingCompleted);
    }
    
    private void OnBuildingCompleted(BuildingCompletedEvent evt)
    {
        // Activate when construction finishes
        if (evt.Building == gameObject)
        {
            Activate();
        }
    }
    
    private void Update()
    {
        if (!isActive) return;
        
        // Find target
        if (currentTarget == null || !IsInRange(currentTarget))
        {
            FindNewTarget();
        }
        
        // Attack if target found and cooldown ready
        if (currentTarget != null && CanAttack())
        {
            AttackTarget();
        }
    }
    
    private void FindNewTarget()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, attackRange, enemyLayers);
        
        Transform bestTarget = null;
        float bestDistance = float.MaxValue;
        
        foreach (var enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTarget = enemy.transform;
            }
        }
        
        currentTarget = bestTarget;
    }
    
    private bool IsInRange(Transform target)
    {
        return Vector3.Distance(transform.position, target.position) <= attackRange;
    }
    
    private bool CanAttack()
    {
        return Time.time >= lastAttackTime + (1f / attackRate);
    }
    
    private void AttackTarget()
    {
        lastAttackTime = Time.time;
        
        // Apply damage
        var unitHealth = currentTarget.GetComponent<UnitHealth>();
        if (unitHealth != null)
        {
            unitHealth.TakeDamage(attackDamage, gameObject);
        }
        
        // Publish event for visuals/audio
        EventBus.Publish(new TowerFiredEvent(gameObject, currentTarget));
    }
    
    public void Activate()
    {
        isActive = true;
        Debug.Log("Tower activated!");
    }
    
    public void Deactivate()
    {
        isActive = false;
        currentTarget = null;
    }
    
    private void OnDestroy()
    {
        EventBus.Unsubscribe<BuildingCompletedEvent>(OnBuildingCompleted);
    }
}

// Add this event to GameEvents.cs
public struct TowerFiredEvent
{
    public GameObject Tower { get; }
    public Transform Target { get; }
    
    public TowerFiredEvent(GameObject tower, Transform target)
    {
        Tower = tower;
        Target = target;
    }
}
```

### 10. RESOURCE COST HELPER
```csharp
// Easy way to define resource costs
var towerCost = ResourceCost.Build()
    .Wood(200)
    .Stone(150)
    .Gold(50)
    .Create();

var buildTime = 10f; // seconds

// Or manually
var costs = new Dictionary<ResourceType, int>
{
    { ResourceType.Wood, 200 },
    { ResourceType.Stone, 150 },
    { ResourceType.Gold, 50 }
};
```

---

## BUILDING DATA ASSET STRUCTURE (What to Configure)

When creating a new `BuildingDataSO` for a tower:

```
Tower BuildingData
├── Identity
│   ├── buildingName: "Tower"
│   ├── buildingType: Defensive      ← Important!
│   ├── description: "Defensive tower that attacks enemies"
│   └── icon: [sprite]
│
├── Costs
│   ├── woodCost: 200
│   ├── stoneCost: 150
│   ├── goldCost: 50
│   └── foodCost: 0
│
├── Effects
│   ├── happinessBonus: 2.0f          ← Defense morale boost
│   ├── generatesResources: false     ← Towers don't generate resources
│   └── [other resource options]
│
├── Construction
│   ├── constructionTime: 10f         ← Time to build
│   └── buildingPrefab: [WatchTower prefab]
│
├── Health & Repair
│   ├── maxHealth: 150
│   └── repairCostMultiplier: 0.5f    ← 50% of build cost to repair
│
└── Unit Training
    ├── canTrainUnits: false          ← Towers don't train units
    └── trainableUnits: []
```

---

## KEY CLASSES & NAMESPACES

```csharp
// Building system
using RTS.Buildings;
- Building
- BuildingDataSO
- BuildingType enum
- WallPlacementController
- WallConnectionSystem

// Infrastructure
using RTS.Core.Services;
- IResourcesService
- IHappinessService
- IBuildingService
- ResourceType enum
- ServiceLocator
- ResourceCost

using RTS.Core.Events;
- BuildingPlacedEvent
- BuildingCompletedEvent
- BuildingDestroyedEvent
- ResourcesChangedEvent
- ResourcesSpentEvent
- [all other game events]

// Units & Combat
using RTS.Units;
- UnitCombat
- UnitHealth
- UnitSelectable

// Managers
using RTS.Managers;
- BuildingManager
- ResourceManager
```

---

## COMMON PATTERNS

### Pattern 1: Building-aware System
```csharp
// Listen for when a specific type of building is placed
EventBus.Subscribe<BuildingPlacedEvent>(evt => 
{
    var building = evt.Building.GetComponent<Building>();
    if (building != null && building.Data.buildingType == BuildingType.Defensive)
    {
        // React to defensive building placement
    }
});
```

### Pattern 2: Resource-dependent Action
```csharp
// Only allow action if resources available
var cost = tower.GetCosts();
if (resourceService.CanAfford(cost))
{
    resourceService.SpendResources(cost);
    // Proceed with action
}
```

### Pattern 3: Cascading Updates (avoid!)
```csharp
// DON'T do this - will cascade:
void OnBuildingPlaced(BuildingPlacedEvent evt)
{
    EventBus.Publish(new SomeEvent());  // Avoid triggering cascades
}

// DO use batch flag or delayed updates
Invoke(nameof(UpdateConnections), 0.05f);  // ← See WallConnectionSystem
```

### Pattern 4: Service Access Pattern
```csharp
// Always use try-get for optional services
IResourcesService resources = ServiceLocator.TryGet<IResourcesService>();
if (resources == null)
{
    Debug.LogError("ResourceService not available!");
    return;
}
```

---

## TOWER SYSTEM CHECKLIST

- [ ] Create `TowerCombat.cs` script (or similar)
- [ ] Configure tower BuildingData asset (set costs, construction time, stats)
- [ ] Verify tower prefab has `Building` component
- [ ] Add tower detection in `TowerCombat` for when building completes
- [ ] Implement target finding (OverlapSphere or array of enemies)
- [ ] Implement attack logic (apply damage via UnitHealth)
- [ ] Test tower placement (use existing BuildingManager)
- [ ] Test tower attacks (verify damage taken by units)
- [ ] Add visual feedback (particle effects, sound, etc.)
- [ ] Add tower placement restrictions (near walls, valid terrain, etc.)
- [ ] Configure tower radius/range visuals
- [ ] Test tower removal (cleanup target reference, etc.)

