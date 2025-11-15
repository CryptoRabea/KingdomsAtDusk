# Kingdoms at Dusk: Fog of War Architecture Analysis

## Project Overview
This is a Unity-based RTS game with a complete game architecture supporting buildings, units, resources, AI, and UI systems. The codebase uses a modular service-based architecture with an event bus for decoupled communication.

---

## 1. MAIN GAME VIEW RENDERING (Scene/Camera Setup)

### Camera System
**File:** `/home/user/KingdomsAtDusk/Assets/Scripts/Camera/RTSCameraController.cs`

**Key Properties:**
- **Movement:** WASD/Arrow keys, edge scrolling, middle-mouse drag
- **Zoom:** Scroll wheel input (supports both perspective and orthographic)
- **Rotation:** Q/E keys for 90-degree rotation
- **World Bounds:** Configurable min/max position clamping (default: -1000 to 1000)
- **Touch Support:** Single finger drag, two-finger pinch zoom

**Critical Code:**
```csharp
[RequireComponent(typeof(Camera))]
public class RTSCameraController : MonoBehaviour
{
    public float moveSpeed = 15f;
    public float zoomSpeed = 50f;
    public float minZoom = 15f;
    public float maxZoom = 80f;
    public Vector2 minPosition;
    public Vector2 maxPosition;
    
    private Camera cam;
    // Supports both orthographic and perspective cameras
    private float newZoom = cam.orthographic ? cam.orthographicSize - zoomInput...
                                             : cam.fieldOfView - zoomInput...
}
```

**Rendering Pipeline:**
- Uses Universal Render Pipeline (URP) with UniversalAdditionalCameraData
- Main camera renders the game world
- Separate minimap camera renders to RenderTexture at orthographic angle (90°)
- Layer-based culling system for selective rendering

**Scene Structure:**
- No specific scene files found (likely built dynamically or in folders)
- Scenes referenced: NewRTSScene, ProtoType, SampleScene, TerrainTest
- Uses NavMesh for pathfinding and obstacle avoidance

---

## 2. MINIMAP IMPLEMENTATION AND RENDERING

### Minimap Architecture

**Primary File:** `/home/user/KingdomsAtDusk/Assets/Scripts/UI/MiniMapController.cs`

**Key Features:**
1. **Dual Rendering System:**
   - Real-time world render via separate orthographic camera
   - RenderTexture (512x512 default) displayed in UI RawImage
   - Separate unit/building marker system on top

2. **Minimap Camera Setup:**
```csharp
// Create render texture
miniMapRenderTexture = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
miniMapImage.texture = miniMapRenderTexture;

// Setup camera
miniMapCamera.targetTexture = miniMapRenderTexture;
miniMapCamera.orthographic = true;
miniMapCamera.orthographicSize = worldSize.y / 2f;
miniMapCamera.transform.position = worldCenter;
miniMapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
```

3. **World Bounds:**
```csharp
[SerializeField] private Vector2 worldMin = new Vector2(-1000f, -1000f);
[SerializeField] private Vector2 worldMax = new Vector2(1000f, 1000f);
private Vector2 worldSize; // Computed: worldMax - worldMin
```

### Minimap Configuration System
**File:** `/home/user/KingdomsAtDusk/Assets/Scripts/UI/Minimap/MinimapConfig.cs`

**Centralized Settings:**
```csharp
[CreateAssetMenu(fileName = "MinimapConfig", menuName = "RTS/UI/Minimap Config")]
public class MinimapConfig : ScriptableObject
{
    // World settings
    public Vector2 worldMin = new Vector2(-1000f, -1000f);
    public Vector2 worldMax = new Vector2(1000f, 1000f);
    
    // Render settings
    public bool renderWorldMap = true;
    public int renderTextureSize = 512;
    public float minimapCameraHeight = 500f;
    
    // Marker settings
    public Color friendlyUnitColor = Color.green;
    public Color enemyUnitColor = Color.red;
    public Color friendlyBuildingColor = Color.blue;
    public Color enemyBuildingColor = Color.red;
    public float unitMarkerSize = 3f;
    public float buildingMarkerSize = 5f;
    
    // Performance
    public int markerUpdateInterval = 2;
    public int maxMarkersPerFrame = 100;
    public bool enableMarkerCulling = true;
}
```

### Marker System Architecture

**Base Manager:** `/home/user/KingdomsAtDusk/Assets/Scripts/UI/Minimap/MinimapMarkerManager.cs`

**Design:**
- Abstract base class with object pooling support
- World-to-minimap coordinate conversion
- Batched update system for performance
- Marker culling for off-screen entities

**Key Methods:**
```csharp
public abstract class MinimapMarkerManager
{
    protected Vector2 WorldToMinimapPosition(Vector3 worldPosition)
    {
        Vector2 normalizedPos = new Vector2(
            Mathf.InverseLerp(config.worldMin.x, config.worldMax.x, worldPosition.x),
            Mathf.InverseLerp(config.worldMin.y, config.worldMax.y, worldPosition.z)
        );
        Vector2 localPos = new Vector2(
            (normalizedPos.x - 0.5f) * minimapRect.rect.width,
            (normalizedPos.y - 0.5f) * minimapRect.rect.height
        );
        return localPos;
    }
}
```

**Unit Markers:** `/home/user/KingdomsAtDusk/Assets/Scripts/UI/Minimap/MinimapUnitMarkerManager.cs`
- Separate object pools for friendly and enemy units
- Circular sprite markers with configurable size and color
- Fast updates using pooled RectTransforms

**Building Markers:** `/home/user/KingdomsAtDusk/Assets/Scripts/UI/Minimap/MinimapBuildingMarkerManager.cs`
- Separate pools for friendly/enemy buildings
- Square markers with color differentiation
- Same coordinate conversion system

### Click-to-Move
**Implementation:**
```csharp
public void OnPointerClick(PointerEventData eventData)
{
    // Convert click screen position to local minimap rect coordinates
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        miniMapRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
    
    // Convert to normalized position (0-1)
    Vector2 normalizedPos = new Vector2(
        (localPoint.x + miniMapRect.rect.width * 0.5f) / miniMapRect.rect.width,
        (localPoint.y + miniMapRect.rect.height * 0.5f) / miniMapRect.rect.height
    );
    
    // Convert to world position
    Vector3 worldPos = new Vector3(
        Mathf.Lerp(worldMin.x, worldMax.x, normalizedPos.x),
        cameraController.transform.position.y,
        Mathf.Lerp(worldMin.y, worldMax.y, normalizedPos.y)
    );
    
    MoveCameraToPosition(worldPos);
}
```

### Minimap Entity Interface
**File:** `/home/user/KingdomsAtDusk/Assets/Scripts/UI/Minimap/IMinimapEntity.cs`

**Flexible Ownership System:**
```csharp
public interface IMinimapEntity
{
    MinimapEntityOwnership GetOwnership();
    Vector3 GetPosition();
    GameObject GetGameObject();
}

public enum MinimapEntityOwnership
{
    Friendly,
    Enemy,
    Neutral,
    Ally,
    Player1,
    Player2,
    Player3,
    Player4
}
```

**MinimapEntity Component:** `/home/user/KingdomsAtDusk/Assets/Scripts/UI/Minimap/MinimapEntity.cs`
- Attach to any unit/building to make it appear on minimap
- Auto-detect ownership from layer/tag or set manually
- Runtime ownership changes supported

---

## 3. UNITS AND BUILDINGS STRUCTURE

### Building System

**Core Component:** `/home/user/KingdomsAtDusk/Assets/Scripts/RTSBuildingsSystems/Building.cs`

**Properties:**
```csharp
public class Building : MonoBehaviour
{
    [SerializeField] private BuildingDataSO data;
    [SerializeField] private bool requiresConstruction = true;
    [SerializeField] private float constructionTime = 5f;
    
    private bool isConstructed = false;
    private float constructionProgress = 0f;
    
    public BuildingDataSO Data => data;
    public bool IsConstructed => isConstructed;
    public float ConstructionProgress => constructionProgress / constructionTime;
}
```

**Lifecycle:**
1. Placement → Construction → Completion → Resource Generation/Happiness
2. Publishes events: `BuildingPlacedEvent`, `BuildingCompletedEvent`, `BuildingDestroyedEvent`
3. Resource generation automatic after construction complete

**BuildingDataSO:** `/home/user/KingdomsAtDusk/Assets/Scripts/RTSBuildingsSystems/BuildingDataSO.cs`

**Data-Driven Definition:**
```csharp
[CreateAssetMenu(fileName = "BuildingData", menuName = "RTS/BuildingData")]
public class BuildingDataSO : ScriptableObject
{
    // Identity
    public string buildingName = "Building";
    public BuildingType buildingType = BuildingType.Residential;
    public string description = "A building";
    public Sprite icon;
    
    // Costs (Dictionary converted at runtime)
    public int woodCost = 0;
    public int foodCost = 0;
    public int goldCost = 0;
    public int stoneCost = 0;
    
    // Effects
    public float happinessBonus = 0f;
    public bool generatesResources = false;
    public ResourceType resourceType = ResourceType.Wood;
    public int resourceAmount = 10;
    public float generationInterval = 5f;
    
    // Prefab reference
    public GameObject buildingPrefab;
    
    // Unit training (for military buildings)
    public bool canTrainUnits = false;
    public List<TrainableUnitData> trainableUnits;
    
    public Dictionary<ResourceType, int> GetCosts() { ... }
}

public enum BuildingType
{
    Residential, Production, Military, Economic, Religious, Cultural, Defensive, Special
}
```

### Unit System

**Core Components:**

1. **UnitHealth:** `/home/user/KingdomsAtDusk/Assets/Scripts/Units/Components/UnitHealth.cs`
```csharp
public class UnitHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool isInvulnerable = false;
    
    private float currentHealth;
    private bool isDead = false;
    
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float HealthPercent => currentHealth / maxHealth;
    public bool IsDead => isDead;
    
    public void TakeDamage(float amount, GameObject attacker = null);
    public void Heal(float amount, GameObject healer = null);
    public void Kill();
}
```

2. **UnitMovement:** `/home/user/KingdomsAtDusk/Assets/Scripts/Units/Components/UnitMovement.cs`
```csharp
[RequireComponent(typeof(NavMeshAgent))]
public class UnitMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private float stoppingDistance = 0.1f;
    
    private NavMeshAgent agent;
    private Transform currentTarget;
    private Vector3 currentDestination;
    
    public bool IsMoving => agent != null && agent.velocity.sqrMagnitude > 0.01f;
    public bool HasReachedDestination => ...;
    
    public void SetDestination(Vector3 destination);
    public void FollowTarget(Transform target);
    public void Stop();
    public void SetSpeed(float speed);
}
```

3. **UnitSelectable:** `/home/user/KingdomsAtDusk/Assets/Scripts/Units/Components/UnitSelectable.cs`
```csharp
public class UnitSelectable : MonoBehaviour
{
    [SerializeField] private GameObject selectionIndicator;
    [SerializeField] private bool useColorHighlight = true;
    [SerializeField] private Color selectedColor = Color.green;
    
    private bool isSelected;
    private bool isHovered;
    
    public bool IsSelected { get; }
    public bool IsHovered { get; }
    
    public void Select();
    public void Deselect();
    public void SetHoverHighlight(bool hover, Color hoverColor);
}
```

4. **UnitCombat:** (Referenced but file not fully shown - exists in codebase)
   - Handles attack damage, range, rate
   - Target selection and combat state

### UnitConfigSO (Data Definition)
**File:** `/home/user/KingdomsAtDusk/Assets/Scripts/Units/Data/UnitConfigSO.cs`

```csharp
[CreateAssetMenu(fileName = "UnitConfig", menuName = "RTS/UnitConfig")]
public class UnitConfigSO : ScriptableObject
{
    // Identity
    public string unitName = "Unit";
    
    // Health & Retreat
    public float maxHealth = 100f;
    public bool canRetreat = true;
    [Range(0f, 100f)]
    public float retreatThreshold = 20f;
    
    // Movement
    public float speed = 3.5f;
    
    // Combat
    public float attackRange = 2f;
    public float attackDamage = 10f;
    public float attackRate = 1f;
    
    // AI
    public float detectionRange = 10f;
    
    // Visual
    public GameObject unitPrefab;
    public Sprite unitIcon;
}
```

### AI System

**UnitAIController:** `/home/user/KingdomsAtDusk/Assets/Scripts/Units/AI/UnitAIController.cs`

**Architecture: State Machine Pattern**
```csharp
[RequireComponent(typeof(UnitHealth))]
[RequireComponent(typeof(UnitMovement))]
[RequireComponent(typeof(UnitCombat))]
public class UnitAIController : MonoBehaviour
{
    [SerializeField] private UnitConfigSO config;
    [SerializeField] private AISettingsSO aiSettings;
    [SerializeField] private AIBehaviorType behaviorType = AIBehaviorType.Aggressive;
    
    private UnitState currentState;
    private Transform currentTarget;
    
    public UnitHealth Health => healthComponent;
    public UnitMovement Movement => movementComponent;
    public UnitCombat Combat => combatComponent;
    public Transform CurrentTarget => currentTarget;
    public UnitStateType CurrentStateType => currentState?.GetStateType() ?? UnitStateType.Dead;
    
    public void ChangeState(UnitState newState);
    public Transform FindTarget();
    public bool ShouldRetreat();
}

public enum AIBehaviorType
{
    Aggressive,   // Targets nearest enemy
    Defensive,    // Targets weakest enemy
    Support       // Heals allies
}
```

**States:** IdleState, MovingState, AttackingState, RetreatState, HealingState, DeadState

**Unit Positions:**
- **X-axis:** World X coordinate (clamped to worldMin.x - worldMax.x)
- **Y-axis:** Terrain height (determined by terrain collider)
- **Z-axis:** World Z coordinate (clamped to worldMin.y - worldMax.y in 2D terms)

---

## 4. PLAYER/OWNERSHIP SYSTEM

### Ownership Detection Methods

**Method 1: Layer-Based**
```csharp
bool isEnemy = unit.layer == LayerMask.NameToLayer("Enemy");
```

**Method 2: Tag-Based**
```csharp
if (gameObject.CompareTag("Friendly")) ownership = MinimapEntityOwnership.Friendly;
if (gameObject.CompareTag("Enemy")) ownership = MinimapEntityOwnership.Enemy;
```

**Method 3: MinimapEntity Component** (Recommended - Most Flexible)
```csharp
public class MinimapEntity : MonoBehaviour, IMinimapEntity
{
    [SerializeField] private MinimapEntityOwnership ownership = MinimapEntityOwnership.Friendly;
    [SerializeField] private bool autoDetectOwnership = false;
    [SerializeField] private int playerId = 0;
    
    public MinimapEntityOwnership GetOwnership() => ownership;
    public int GetPlayerId() => playerId;
    
    public void SetOwnership(MinimapEntityOwnership newOwnership) => ownership = newOwnership;
    public void SetPlayerId(int newPlayerId) { ... }
    
    private void DetectOwnership()
    {
        // Auto-detect from tags/layers
        if (CompareTag("Friendly")) ownership = MinimapEntityOwnership.Friendly;
        if (gameObject.layer == LayerMask.NameToLayer("Enemy")) ownership = MinimapEntityOwnership.Enemy;
    }
}
```

**Ownership Enum:**
```csharp
public enum MinimapEntityOwnership
{
    Friendly,    // Local player (player 0)
    Enemy,       // Default enemy faction
    Neutral,     // Non-aligned
    Ally,        // Allied player
    Player1,     // Multiplayer player 1
    Player2,     // Multiplayer player 2
    Player3,     // Multiplayer player 3
    Player4      // Multiplayer player 4
}
```

### Current Implementation
- **Local Player (Friendly):** Green units on minimap
- **Enemies:** Red units on minimap
- **Buildings:** Blue (friendly) or Red (enemy) on minimap

### Minimap Ownership Detection
```csharp
// From MiniMapController.cs
private void OnUnitSpawned(UnitSpawnedEvent evt)
{
    bool isEnemy = evt.Unit.layer == LayerMask.NameToLayer("Enemy");
    CreateUnitMarker(evt.Unit, evt.Position, isEnemy);
}
```

---

## 5. EXISTING RENDERING LAYERS & POST-PROCESSING

### Layer System

**Configured Layers:**
- **"Enemy"** - Enemy units and buildings (detected by LayerMask check)
- **"Default"** - Friendly units and buildings (assumed)
- **Terrain Layer** - Excluded from building placement collision checks

**Rendering:**
```csharp
// Main camera - renders everything
Camera.main.cullingMask = -1; // All layers

// Minimap camera - selective rendering
miniMapCamera.cullingMask = miniMapLayers; // Configured in inspector
```

### Material System

**Building Placement Preview:**
```csharp
[SerializeField] private Material validPlacementMaterial;   // Green
[SerializeField] private Material invalidPlacementMaterial; // Red
```

**Unit Selection:**
```csharp
// Uses MaterialPropertyBlock to avoid material instance creation
private MaterialPropertyBlock propertyBlock;
private static readonly int ColorPropertyID = Shader.PropertyToID("_Color");

// Apply highlight without creating new materials
propertyBlock.SetColor(ColorPropertyID, selectedColor);
rend.SetPropertyBlock(propertyBlock);
```

### Rendering Pipeline

**Pipeline Type:** Universal Render Pipeline (URP)

**Camera Configuration:**
```csharp
// URP-specific setup
var cameraData = miniMapCamera.GetUniversalAdditionalCameraData();
if (cameraData != null)
{
    cameraData.renderType = CameraRenderType.Base;
    cameraData.requiresColorOption = CameraOverrideOption.On;
    cameraData.requiresDepthOption = CameraOverrideOption.On;
}
```

**No Custom Post-Processing Found**
- No bloom, depth of field, or other post-effects detected
- Uses standard URP rendering pipeline

---

## 6. EVENT-DRIVEN ARCHITECTURE

### EventBus System
**File:** `/home/user/KingdomsAtDusk/Assets/Scripts/Core/EventBus.cs`

**Pattern:** Publish-Subscribe with struct events

```csharp
public static class EventBus
{
    private static readonly Dictionary<Type, List<Delegate>> eventHandlers = ...;
    
    public static void Subscribe<T>(Action<T> handler) where T : struct;
    public static void Unsubscribe<T>(Action<T> handler) where T : struct;
    public static void Publish<T>(T eventData) where T : struct;
    public static void Clear<T>() where T : struct;
}
```

### Key Events (from GameEvents.cs)

**Building Events:**
- `BuildingPlacedEvent` - Building placed (position, gameobject)
- `BuildingCompletedEvent` - Construction finished
- `BuildingDestroyedEvent` - Building demolished
- `ConstructionProgressEvent` - Progress update (0-1)
- `ResourcesGeneratedEvent` - Resources produced

**Unit Events:**
- `UnitSpawnedEvent` - Unit created (position, gameobject)
- `UnitDiedEvent` - Unit killed (isEnemy flag)
- `UnitHealthChangedEvent` - Health delta
- `UnitStateChangedEvent` - AI state change
- `UnitSelectedEvent` / `UnitDeselectedEvent`

**Combat Events:**
- `DamageDealtEvent` - Attacker, target, damage amount
- `HealingAppliedEvent` - Healer, target, amount

**Selection Events:**
- `UnitSelectedEvent` / `UnitDeselectedEvent`
- `BuildingSelectedEvent` / `BuildingDeselectedEvent`
- `SelectionChangedEvent` - Count updated

**Wave Events:**
- `WaveStartedEvent` - Wave number, enemy count
- `WaveCompletedEvent` - Wave number

---

## 7. SERVICE LOCATOR ARCHITECTURE

**File:** `/home/user/KingdomsAtDusk/Assets/Scripts/Core/IServices.cs`

**Core Services:**

1. **IResourcesService** - Resource management (Wood, Food, Gold, Stone)
2. **IHappinessService** - Happiness/morale system
3. **IGameStateService** - Game state management (Playing, Paused, GameOver, Victory)
4. **IBuildingService** - Building placement and management
5. **IPopulationService** - Population and housing
6. **IReputationService** - Reputation system
7. **IPeasantWorkforceService** - Worker allocation
8. **IPoolService** - Object pooling
9. **ITimeService** - Day-night cycle

**Building Manager** (implements IBuildingService):
```csharp
public class BuildingManager : MonoBehaviour, IBuildingService
{
    [SerializeField] private BuildingDataSO[] buildingDataArray; // SOURCE OF TRUTH
    
    public void StartPlacingBuilding(int buildingIndex);
    public void StartPlacingBuilding(BuildingDataSO buildingData);
    public void CancelPlacement();
    public bool IsPlacing { get; }
    public BuildingDataSO[] GetAllBuildingData();
    public bool CanAffordBuilding(BuildingDataSO buildingData);
}
```

**Game Manager** (Initialization):
```csharp
public class GameManager : MonoBehaviour
{
    public void InitializeServices()
    {
        // 1. Core services
        InitializeObjectPool();
        // 2. Game state
        gameStateService = new GameStateService();
        ServiceLocator.Register<IGameStateService>(gameStateService);
        // 3. Resource and happiness
        InitializeResourceManager();
        InitializeHappinessManager();
        // 4. Building management
        InitializeBuildingManager();
    }
}
```

---

## KEY INTEGRATION POINTS FOR FOG OF WAR

### 1. **Position Tracking**
- All units: `UnitMovement.SetDestination()` updates `Transform.position`
- All buildings: `Transform.position` set at placement
- Real-time access via `gameObject.transform.position`

### 2. **Unit/Building Detection**
```csharp
// Iterate through all units
foreach (var unit in FindObjectsByType<UnitAIController>())
{
    var health = unit.Health;
    var position = unit.Movement.transform.position;
    var isAlly = unit.gameObject.CompareTag("Friendly");
}

// Iterate through all buildings
foreach (var building in FindObjectsByType<Building>())
{
    var position = building.transform.position;
    var isConstructed = building.IsConstructed;
    var data = building.Data;
}
```

### 3. **Layer System Integration**
```csharp
// Use for visibility checks
if (unit.gameObject.layer == LayerMask.NameToLayer("Enemy"))
{
    // Apply fog of war obscuring
}
```

### 4. **Event Notifications**
```csharp
// Subscribe to unit/building creation
EventBus.Subscribe<UnitSpawnedEvent>(OnUnitSpawned);
EventBus.Subscribe<BuildingPlacedEvent>(OnBuildingPlaced);

// Subscribe to destruction
EventBus.Subscribe<UnitDiedEvent>(OnUnitDied);
EventBus.Subscribe<BuildingDestroyedEvent>(OnBuildingDestroyed);
```

### 5. **Minimap Integration Point**
- MinimapEntity component provides `GetOwnership()` interface
- Can extend to track visibility state:
```csharp
// Example extension for fog of war
public interface IMinimapEntity
{
    MinimapEntityOwnership GetOwnership();
    Vector3 GetPosition();
    GameObject GetGameObject();
    bool IsVisibleToPlayer(int playerIndex); // NEW: Fog of war visibility
}
```

### 6. **Rendering Layer for Visibility**
```csharp
// Could use layer system to hide fogged entities
// Or use Material shader with visibility parameter
// Or control Canvas alpha for UI elements
```

### 7. **Height-Based Visibility**
- World bounds: Z-axis is height-based (worldMin.y to worldMax.y represent Z coordinates)
- Can use terrain height for line-of-sight calculations

---

## FILE STRUCTURE REFERENCE

```
Assets/Scripts/
├── Camera/
│   └── RTSCameraController.cs
├── Core/
│   ├── EventBus.cs
│   ├── GameEvents.cs
│   ├── IServices.cs
│   ├── ServiceLocator.cs
│   ├── ObjectPool.cs
│   └── Utilities/
├── Managers/
│   ├── GameManager.cs
│   ├── BuildingManager.cs
│   ├── ResourceManager.cs
│   ├── HappinessManager.cs
│   └── ...
├── RTSBuildingsSystems/
│   ├── Building.cs
│   ├── BuildingDataSO.cs
│   ├── BuildingSelectionManager.cs
│   ├── Tower.cs
│   ├── TowerDataSO.cs
│   └── ...
├── Units/
│   ├── Components/
│   │   ├── UnitHealth.cs
│   │   ├── UnitMovement.cs
│   │   ├── UnitCombat.cs
│   │   └── UnitSelectable.cs
│   ├── AI/
│   │   ├── UnitAIController.cs
│   │   ├── States/
│   │   └── Specialized/
│   ├── Data/
│   │   └── UnitConfigSO.cs
│   └── Selection/
├── UI/
│   ├── MiniMapController.cs
│   ├── Minimap/
│   │   ├── IMinimapEntity.cs
│   │   ├── MinimapEntity.cs
│   │   ├── MinimapConfig.cs
│   │   ├── MinimapMarkerManager.cs
│   │   ├── MinimapUnitMarkerManager.cs
│   │   ├── MinimapBuildingMarkerManager.cs
│   │   ├── MinimapMarkerPool.cs
│   │   ├── MinimapDragHandler.cs
│   │   └── MinimapEntityDetector.cs
│   ├── BuildingDetailsUI.cs
│   ├── ResourceUI.cs
│   └── ...
└── Debug/
```

---

## RECOMMENDED FOG OF WAR ARCHITECTURE

Based on the existing architecture, a fog of war system should:

1. **Create FogOfWarController** - Manage visibility states per player
2. **Extend MinimapEntity** - Add visibility tracking
3. **Integrate with EventBus** - Publish visibility change events
4. **Use Layer System** - Toggle layer visibility for fogged entities
5. **Hook into Building/UnitPosition** - Monitor transforms for vision range calculations
6. **Leverage existing Marker System** - Hide/show markers based on visibility
7. **Create FogOfWarRenderer** - Handle shader-based rendering of fogged areas
8. **Extend UnitSelectable** - Control selection based on visibility

This modular approach maintains decoupling while integrating cleanly with existing systems.
