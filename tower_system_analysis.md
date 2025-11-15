# Building and Wall System Analysis - Kingdoms at Dusk

## 1. BUILDING SYSTEM ARCHITECTURE

### Core Components

#### 1.1 Building.cs - Main Building Component
**Location:** `/home/user/KingdomsAtDusk/Assets/Scripts/RTSBuildingsSystems/Building.cs`

**Key Features:**
- Handles construction lifecycle (incomplete → complete)
- Manages resource generation for productive buildings
- Applies happiness bonuses/penalties
- Publishes events for building placement, completion, and destruction
- Works with BuildingDataSO for configuration

**Construction Mechanics:**
```csharp
- requiresConstruction: bool - whether building needs construction phase
- constructionTime: float - how long construction takes (seconds)
- constructionProgress: tracks current progress (0 to constructionTime)
- isConstructed: bool - completion state
```

**Resource Generation:**
```csharp
- generatesResources: enabled/disabled per building type
- resourceType: Wood, Food, Gold, or Stone
- resourceAmount: amount generated per interval
- generationInterval: time between resource generation (seconds)
```

#### 1.2 BuildingDataSO.cs - Building Configuration Data
**Location:** `/home/user/KingdomsAtDusk/Assets/Scripts/RTSBuildingsSystems/BuildingDataSO.cs`

**Data Structure:**
```csharp
public class BuildingDataSO : ScriptableObject
{
    // Identity
    public string buildingName;
    public BuildingType buildingType; // Residential, Production, Military, Economic, Religious, Cultural, Defensive, Special
    public string description;
    public Sprite icon;

    // Costs (4 resource types: Wood, Food, Gold, Stone)
    public int woodCost, foodCost, goldCost, stoneCost;

    // Effects
    public float happinessBonus;
    public bool generatesResources;
    public ResourceType resourceType;
    public int resourceAmount;
    public float generationInterval;

    // Construction
    public float constructionTime;
    public GameObject buildingPrefab;

    // Housing/Population
    public bool providesHousing;
    public int housingCapacity;

    // Health & Repair
    public int maxHealth;
    public float repairCostMultiplier;

    // Unit Training
    public bool canTrainUnits;
    public List<TrainableUnitData> trainableUnits;
}
```

**Building Types Enum:**
```csharp
public enum BuildingType
{
    Residential,    // Housing/population
    Production,     // Resource generation
    Military,       // Barracks, towers, walls
    Economic,       // Markets, banks
    Religious,      // Temples, churches
    Cultural,       // Libraries, monuments
    Defensive,      // Walls, towers
    Special         // Unique buildings
}
```

#### 1.3 BuildingManager.cs - Building Placement Controller
**Location:** `/home/user/KingdomsAtDusk/Assets/Scripts/Managers/BuildingManager.cs`

**Placement Logic:**
- Manages preview buildings during placement
- Validates placement (collision, terrain slope, resources)
- Spawns actual buildings when confirmed
- Handles wall placement redirect to WallPlacementController

**Key Methods:**
```csharp
public void StartPlacingBuilding(BuildingDataSO buildingData)
- Starts building placement mode
- Creates preview from prefab
- Detects if building is a wall and redirects to wall system

private bool IsValidPlacement(Vector3 position)
- Checks for collisions with other buildings/objects
- Validates terrain is suitable (flat enough)
- Returns true only if valid

private void PlaceBuilding()
- Deducts resources from player
- Instantiates actual building prefab
- Publishes BuildingPlacedEvent
```

**Placement Validation:**
```csharp
- Grid snapping (optional, configurable)
- Height difference checking (maxHeightDifference = 2f default)
- Collision detection using OverlapBox
- Terrain validation via raycast sampling
```

---

## 2. WALL SYSTEM ARCHITECTURE

### Core Components

#### 2.1 WallPlacementController.cs - Wall Pole-to-Pole Placement
**Location:** `/home/user/KingdomsAtDusk/Assets/Scripts/RTSBuildingsSystems/WallPlacementController.cs`

**Unique Features:**
- **Pole-to-pole wall placement**: Click two points, walls fill the gap
- **Mesh-based perfect fitting**: No gaps, no overlaps between segments
- **Adaptive scaling**: Last segment scales to fit exact distance
- **Wall snapping**: Connect new walls to existing walls
- **Automatic wall loop completion**: Auto-closes when snapped to starting point

**Placement Workflow:**
1. Call `StartPlacingWalls(BuildingDataSO wallData)`
2. First click: Place first pole
3. Drag mouse: Preview wall segments and cost
4. Second click: Place all wall segments, continue chaining
5. Right-click or ESC: Cancel

**Key Class: PlacedWallSegment**
```csharp
private struct PlacedWallSegment
{
    public Vector3 center;          // Center position of segment
    public float length;            // World-space length along wall axis
    public Quaternion rotation;     // Rotation of wall

    // Methods to get start/end positions
    public Vector3 GetStartPosition(WallLengthAxis axis);
    public Vector3 GetEndPosition(WallLengthAxis axis);
}
```

**Wall Mesh Sizing:**
```csharp
- wallLengthAxis: Which axis represents wall length (X, Y, or Z)
- DetectWallMeshLength(): Auto-detects actual mesh dimensions
- Calculates how many full segments fit + remaining distance
- Last segment scales to fill remaining distance perfectly
```

**Wall Segment Calculation:**
```csharp
private List<WallSegmentData> CalculateWallSegmentsWithScaling(Vector3 start, Vector3 end)
- Calculates how many full-size segments fit
- Scales final segment to exact remaining distance
- Returns list of positions, scales, and lengths
```

**Overlap Prevention:**
```csharp
private bool WouldOverlapExistingWall(Vector3 start, Vector3 end)
- Checks endpoint connections (allowed)
- Detects 2D segment intersections
- Checks collinear overlapping
- Allows parallel walls with tiny gap (minParallelOverlap = 0.5f)
```

**Input Handling:**
```csharp
- Left click: Place pole or segments
- Right click: Cancel placement
- ESC: Cancel placement
- Mouse position tracked via raycast to ground layer
```

#### 2.2 WallConnectionSystem.cs - Wall Connection Detection
**Location:** `/home/user/KingdomsAtDusk/Assets/Scripts/RTSBuildingsSystems/WallConnectionSystem.cs`

**Features:**
- **Automatic neighbor detection**: Finds nearby walls within connection distance
- **Static registry**: All walls registered in `allWalls` static list
- **Visual updates**: Can activate mesh variants based on connections
- **Cascading update prevention**: Uses batch mode to prevent recursive updates

**Connection Mechanics:**
```csharp
- connectionDistance: Max distance to detect neighbors (1.5f default)
- enableConnections: Toggle connections per wall
- allWalls: Static list of ALL wall instances
- connectedWalls: List of neighbors for THIS wall
```

**Key Methods:**
```csharp
public void UpdateConnections()
- Finds all walls within connectionDistance
- Updates visual mesh based on connections
- Prevents recursive updates via isUpdating flag

public int GetConnectionCount()
- Returns number of connected neighbors

public List<WallConnectionSystem> GetConnectedWalls()
- Returns list of neighbor walls

public static List<WallConnectionSystem> GetAllWalls()
- Returns all walls currently in scene
```

**Event Integration:**
- Subscribes to `BuildingPlacedEvent` to update neighbors
- Subscribes to `BuildingDestroyedEvent` for cleanup
- Updates nearby walls when changes occur

---

## 3. EXISTING DEFENSIVE/COMBAT STRUCTURES

### Current Defense Elements

#### 3.1 Wall Towers
**Pre-configured Tower Data Assets:**
- `WallTowers_1_Data.asset` - Tower variant 1
- `WallTowers_2_Data.asset` - Tower variant 2
- `WallTowers_D1_Data.asset` - Door variant 1
- `WallTowers_D2_Data.asset` - Door variant 2
- `WallTowers_DoorC1_Data.asset` - Door variant with closure 1
- `WallTowers_DoorC2.asset` - Door variant with closure 2

**Wall Segments:**
- `Wall_1_Data.asset` - Wall type 1
- `Wall_2_Data.asset` - Wall type 2

**Location:** `/home/user/KingdomsAtDusk/Assets/Prefabs/WallPrefabs&Data/`

#### 3.2 Combat System (UnitCombat.cs)
**Location:** `/home/user/KingdomsAtDusk/Assets/Scripts/Units/Components/UnitCombat.cs`

**Current Combat Mechanics:**
```csharp
public class UnitCombat : MonoBehaviour
{
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRate = 1f; // attacks per second
    [SerializeField] private LayerMask targetLayers;
    
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private GameObject projectilePrefab;
}
```

**Available Methods:**
```csharp
public void SetTarget(Transform target)
public void ClearTarget()
public bool IsTargetInRange(Transform target)
public bool TryAttack()
public bool CanAttack()
```

#### 3.3 Existing Tower Building Data
**Location:** `/home/user/KingdomsAtDusk/Assets/Prefabs/BuildingPrefabs&Data/TowerBuildingData.asset`

- Pre-configured tower data exists
- Available for defensive building placement

#### 3.4 Barracks (Military Building)
**Location:** `/home/user/KingdomsAtDusk/Assets/Prefabs/BuildingPrefabs&Data/`
- `BaraksBuildingData.asset`
- Can train units (uses TrainableUnitData)

---

## 4. RESOURCE AND EVENT SYSTEMS

### 4.1 Resource System
**Location:** `/home/user/KingdomsAtDusk/Assets/Scripts/Managers/ResourceManager.cs`

**Resource Types (Enum):**
```csharp
public enum ResourceType
{
    Wood,   // Building materials
    Food,   // Population sustenance
    Gold,   // Currency/trade
    Stone,  // Construction/defensive buildings
    // Future: Iron, Mana, Population
}
```

**Interface (IResourcesService):**
```csharp
public interface IResourcesService
{
    int GetResource(ResourceType type);
    bool CanAfford(Dictionary<ResourceType, int> costs);
    bool SpendResources(Dictionary<ResourceType, int> costs);
    void AddResources(Dictionary<ResourceType, int> amounts);
    
    // Legacy compatibility
    int Wood { get; }
    int Food { get; }
    int Gold { get; }
    int Stone { get; }
}
```

**Starting Resources (Configurable):**
```csharp
startingWood = 100
startingFood = 100
startingGold = 50
startingStone = 50
```

### 4.2 Event System
**Location:** `/home/user/KingdomsAtDusk/Assets/Scripts/Core/GameEvents.cs`

**Building-Related Events:**
```csharp
public struct BuildingPlacedEvent
{
    public GameObject Building { get; }
    public Vector3 Position { get; }
}

public struct BuildingCompletedEvent
{
    public GameObject Building { get; }
    public string BuildingName { get; }
}

public struct BuildingDestroyedEvent
{
    public GameObject Building { get; }
    public string BuildingName { get; }
}

public struct BuildingSelectedEvent
{
    public GameObject Building { get; }
}

public struct ResourcesGeneratedEvent
{
    public string BuildingName { get; }
    public ResourceType ResourceType { get; }
    public int Amount { get; }
}

public struct ConstructionProgressEvent
{
    public GameObject Building { get; }
    public string BuildingName { get; }
    public float Progress { get; } // 0-1
}
```

**Resource Events:**
```csharp
public struct ResourcesChangedEvent
{
    public int WoodDelta;
    public int FoodDelta;
    public int GoldDelta;
    public int StoneDelta;
}

public struct ResourcesSpentEvent
{
    public int Wood, Food, Gold, Stone;
    public bool Success;
}
```

**Happiness Events:**
```csharp
public struct HappinessChangedEvent
{
    public float NewHappiness;
    public float Delta;
}

public struct BuildingBonusChangedEvent
{
    public float BonusDelta;
    public string BuildingName;
}
```

---

## 5. PROJECT STRUCTURE

### Directory Organization

```
/Assets/
├── Scripts/
│   ├── RTSBuildingsSystems/          # Core building system
│   │   ├── Building.cs               # Main building component
│   │   ├── BuildingDataSO.cs         # Building configuration
│   │   ├── BuildingManager.cs        # Placement controller (in Managers/)
│   │   ├── BuildingSelectable.cs     # Selection component
│   │   ├── BuildingButton.cs         # UI button
│   │   ├── BuildingHUD.cs            # Building UI
│   │   ├── WallPlacementController.cs # Wall placement system
│   │   ├── WallConnectionSystem.cs   # Wall connections
│   │   ├── BuildingSelectionManager.cs # Selection management
│   │   └── Editor/                   # Editor tools
│   │       ├── WallPrefabSetupUtility.cs
│   │       └── WallConnectionSystemEditor.cs
│   │
│   ├── Managers/
│   │   ├── BuildingManager.cs        # Building placement control
│   │   ├── ResourceManager.cs        # Resource system
│   │   └── HappinessManager.cs
│   │
│   ├── Core/
│   │   ├── GameEvents.cs             # Event definitions
│   │   ├── EventBus.cs               # Event system
│   │   ├── IServices.cs              # Service interfaces
│   │   └── ServiceLocator.cs         # Dependency injection
│   │
│   ├── Units/
│   │   └── Components/
│   │       ├── UnitCombat.cs         # Combat mechanics
│   │       ├── UnitHealth.cs
│   │       └── UnitSelectable.cs
│   │
│   └── UI/
│       ├── BuildingDetailsUI.cs
│       ├── WallResourcePreviewUI.cs
│       └── ResourceUI.cs
│
├── Prefabs/
│   ├── BuildingPrefabs&Data/
│   │   ├── HouseBuildingData.asset
│   │   ├── FarmBuildingData.asset
│   │   ├── LumberMillBuildingData.asset
│   │   ├── GoldMineBuildingData.asset
│   │   ├── StoneQuaryBuildingData.asset
│   │   ├── TownHouseBuildingData.asset
│   │   ├── BaraksBuildingData.asset
│   │   ├── TowerBuildingData.asset        ← Tower data exists!
│   │   ├── HousePrefab.prefab
│   │   ├── FarmPrefab.prefab
│   │   ├── BarraclsPrefab.prefab
│   │   ├── WatchTower_SecondAge_Level1.prefab
│   │   └── [other building prefabs]
│   │
│   ├── WallPrefabs&Data/
│   │   ├── Wall_1_Data.asset
│   │   ├── Wall_2_Data.asset
│   │   ├── WallTowers_1_Data.asset
│   │   ├── WallTowers_2_Data.asset
│   │   ├── WallTowers_D1_Data.asset
│   │   ├── WallTowers_D2_Data.asset
│   │   ├── WallTowers_DoorC1_Data.asset
│   │   ├── WallTowers_DoorC2.asset
│   │   ├── Wall_1Prefab.prefab
│   │   ├── Wall_2Prefab.prefab
│   │   ├── WallTowers_1Prefab.prefab
│   │   ├── WallTowers_2Prefab.prefab
│   │   └── [door variants]
│   │
│   └── UnitsPrefabs&Data/
│
├── Scenes/
│   └── [various test/game scenes]
│
├── UltimateRtsFantasy/              # Asset pack for models
│   ├── FBX/                         # Wall and tower models
│   │   ├── Wall_FirstAge.fbx
│   │   ├── Wall_SecondAge.fbx
│   │   ├── WallTowers_FirstAge.fbx
│   │   └── WallTowers_SecondAge.fbx
│   └── PNG/                         # Sprite assets
│
└── [other assets]
```

---

## 6. KEY INTEGRATION POINTS FOR TOWER SYSTEM

### How Buildings Get Placed

1. **UI Button Click** → `BuildingButton.cs` calls `BuildingManager.StartPlacingBuilding(BuildingDataSO)`
2. **Placement Mode** → `BuildingManager.UpdateBuildingPreview()` shows preview, validates placement
3. **Confirmation Click** → `BuildingManager.PlaceBuilding()` deducts resources, instantiates prefab
4. **Building Initialization** → Prefab's `Building.cs` component checks if construction is needed
5. **Event Publishing** → `BuildingPlacedEvent` triggers wall connections and other listeners

### How Walls Get Placed

1. **UI Button Click** → `BuildingManager.StartPlacingBuilding()` detects wall type
2. **Wall Mode Redirect** → Calls `WallPlacementController.StartPlacingWalls(BuildingDataSO)`
3. **Pole Placement** → First click places first pole, shows preview of wall segments
4. **Segment Calculation** → `CalculateWallSegmentsWithScaling()` determines segments needed
5. **Wall Instantiation** → Second click instantiates all segments with calculated positions/scales
6. **Connection Update** → Each wall's `WallConnectionSystem` updates neighbor list

### How Combat Works

1. **Unit Selection** → `UnitSelectable.cs` marks unit as selected
2. **Target Assignment** → `UnitCombat.SetTarget(Transform target)`
3. **Attack Check** → Every frame calls `CanAttack()` to check range, cooldown, target alive
4. **Damage Application** → `TryAttack()` → `UnitHealth.TakeDamage()`

---

## 7. SERVICE ARCHITECTURE

### ServiceLocator Pattern
**Location:** `/home/user/KingdomsAtDusk/Assets/Scripts/Core/ServiceLocator.cs`

All services are registered at startup and accessed globally:
```csharp
IResourcesService resourceService = ServiceLocator.TryGet<IResourcesService>();
IHappinessService happinessService = ServiceLocator.TryGet<IHappinessService>();
IBuildingService buildingService = ServiceLocator.TryGet<IBuildingService>();
ITimeService timeService = ServiceLocator.TryGet<ITimeService>();
IPoolService poolService = ServiceLocator.TryGet<IPoolService>();
```

### Available Services
- **IResourcesService** → Manages wood, food, gold, stone
- **IHappinessService** → Tracks population happiness
- **IBuildingService** → Building management (BuildingManager)
- **ITimeService** → Day/night cycle, time scaling
- **IPoolService** → Object pooling

---

## 8. HELPER CLASSES AND UTILITIES

### ResourceCost Builder
```csharp
// Easy way to build resource cost dictionaries
var costs = ResourceCost.Build()
    .Wood(100)
    .Stone(50)
    .Gold(25)
    .Create();
```

### EventBus
```csharp
// Subscribe to events
EventBus.Subscribe<BuildingPlacedEvent>(OnBuildingPlaced);

// Publish events
EventBus.Publish(new BuildingPlacedEvent(buildingGO, position));

// Unsubscribe
EventBus.Unsubscribe<BuildingPlacedEvent>(OnBuildingPlaced);
```

---

## SUMMARY: TOWER SYSTEM IMPLEMENTATION PATH

To implement a tower system effectively:

1. **Leverage Existing Tower Data**
   - `TowerBuildingData.asset` already exists
   - Can add BuildingType.Defensive towers to building data array

2. **Add Tower Combat Component**
   - Attach combat script similar to UnitCombat
   - Define tower's attackRange, attackDamage, attackRate
   - Subscribe to BuildingPlacedEvent to activate nearby towers

3. **Tower Targeting System**
   - Tower scans for enemies within range each frame
   - Uses layer masks to detect enemy units
   - Manages target acquisition and switching

4. **Tower Placement Constraints**
   - BuildingManager already validates placement
   - Add wall-attachment system or place anywhere
   - Can use existing grid snapping or free placement

5. **Tower Visuals & Feedback**
   - Use existing wall tower prefabs (WallTowers_1, WallTowers_2, etc.)
   - Animate attacks (projectiles or area effects)
   - Publish custom TowerFiredEvent for visual/audio feedback

6. **Integration Points**
   - Towers are just Defensive buildings in BuildingDataSO
   - Works with existing resource/happiness/event systems
   - Can add tower-specific building bonuses (defense bonus to nearby walls)

