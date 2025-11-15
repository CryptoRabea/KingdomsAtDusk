# Actual Code Patterns from Kingdoms at Dusk Codebase

This document shows REAL code from the actual project to demonstrate how systems work.

---

## 1. BUILDING LIFECYCLE - Building.cs

### Construction Phase
```csharp
private bool isConstructed = false;
private float constructionProgress = 0f;

private void Update()
{
    if (!isConstructed && requiresConstruction)
    {
        constructionProgress += Time.deltaTime;

        if (constructionProgress >= constructionTime)
        {
            CompleteConstruction();
        }
    }
}

private void CompleteConstruction()
{
    isConstructed = true;
    constructionProgress = constructionTime;

    if (constructionVisual != null)
        constructionVisual.SetActive(false);

    // Apply happiness bonus
    if (data != null && happinessService != null && data.happinessBonus != 0)
    {
        happinessService.AddBuildingBonus(data.happinessBonus, data.buildingName);
    }

    // Publish event - THIS ACTIVATES WALL CONNECTIONS!
    EventBus.Publish(new BuildingCompletedEvent(gameObject, data.buildingName));
}
```

### Resource Generation
```csharp
private void Update()
{
    // Only generate after construction is complete
    if (isConstructed && data != null && data.generatesResources)
    {
        resourceGenerationTimer += Time.deltaTime;

        if (resourceGenerationTimer >= data.generationInterval)
        {
            GenerateResources();
            resourceGenerationTimer = 0f;
        }
    }
}

private void GenerateResources()
{
    if (resourceService == null || data == null) return;

    var resources = new Dictionary<ResourceType, int>
    {
        { data.resourceType, data.resourceAmount }
    };

    resourceService.AddResources(resources);

    Debug.Log($"✅ {data.buildingName} generated {data.resourceAmount} {data.resourceType}");

    // Publish event for UI/audio feedback
    EventBus.Publish(new ResourcesGeneratedEvent(
        data.buildingName,
        data.resourceType,
        data.resourceAmount
    ));
}
```

### Building Destruction
```csharp
private void OnDestroy()
{
    // Remove happiness bonus when destroyed
    if (isConstructed && data != null && happinessService != null && data.happinessBonus != 0)
    {
        happinessService.RemoveBuildingBonus(data.happinessBonus, data.buildingName);
    }

    // Publish destruction event
    if (data != null)
    {
        EventBus.Publish(new BuildingDestroyedEvent(gameObject, data.buildingName));
    }
}
```

---

## 2. WALL PLACEMENT - WallPlacementController.cs

### Mesh-Based Segment Calculation
```csharp
private List<WallSegmentData> CalculateWallSegmentsWithScaling(Vector3 start, Vector3 end)
{
    List<WallSegmentData> segments = new List<WallSegmentData>();

    Vector3 diff = end - start;
    float totalDistance = diff.magnitude;

    if (totalDistance < wallMeshLength * minScaleFactor)
        return segments; // Too short

    Vector3 direction = diff.normalized;
    Vector3 baseScale = currentWallData.buildingPrefab.transform.localScale;

    // Number of full-size segments that fit
    int fullCount = Mathf.FloorToInt(totalDistance / wallMeshLength);
    float used = fullCount * wallMeshLength;
    float remaining = totalDistance - used;

    // Determine which axis we scale
    int axisIndex = wallLengthAxis == WallLengthAxis.X ? 0 : 
                    (wallLengthAxis == WallLengthAxis.Y ? 1 : 2);

    // 1) Place all full-size segments
    for (int i = 0; i < fullCount; i++)
    {
        float centerOffset = (i * wallMeshLength) + (wallMeshLength * 0.5f);
        Vector3 pos = start + direction * centerOffset;
        segments.Add(new WallSegmentData(pos, baseScale, wallMeshLength));
    }

    // 2) Handle final SCALING segment
    if (remaining > wallMeshLength * minScaleFactor)
    {
        float scaleFactor = remaining / wallMeshLength;
        float centerOffset = used + (remaining * 0.5f);
        Vector3 pos = start + direction * centerOffset;

        Vector3 scaled = baseScale;
        scaled[axisIndex] = baseScale[axisIndex] * scaleFactor;

        segments.Add(new WallSegmentData(pos, scaled, remaining));
    }

    return segments;
}
```

### Overlap Detection
```csharp
private bool WouldOverlapExistingWall(Vector3 start, Vector3 end)
{
    foreach (var seg in placedWallSegments)
    {
        Vector3 existingStart = seg.GetStartPosition(wallLengthAxis);
        Vector3 existingEnd = seg.GetEndPosition(wallLengthAxis);

        // 1. If connecting exactly to endpoints → allowed
        if (Vector3.Distance(start, existingStart) < 0.01f ||
            Vector3.Distance(start, existingEnd) < 0.01f ||
            Vector3.Distance(end, existingStart) < 0.01f ||
            Vector3.Distance(end, existingEnd) < 0.01f)
        {
            continue; // endpoint connections allowed
        }

        // 2. Check segment intersection in 2D
        if (SegmentsIntersect2D(start, end, existingStart, existingEnd))
        {
            return true; // Overlap or crossing detected
        }

        // 3. Check if new segment lies on top of existing one
        if (AreCollinearAndOverlapping(start, end, existingStart, existingEnd))
        {
            return true;
        }
    }

    return false;
}
```

---

## 3. WALL CONNECTIONS - WallConnectionSystem.cs

### Connection Detection
```csharp
public void UpdateConnections()
{
    if (!enableConnections) return;

    // ✅ FIX: Prevent recursive updates
    if (isUpdating) return;
    isUpdating = true;

    try
    {
        // Clear old connections
        connectedWalls.Clear();

        // Find nearby walls within connection distance
        Vector3 myPos = transform.position;
        foreach (var otherWall in allWalls)
        {
            if (otherWall == this || otherWall == null) continue;

            float distance = Vector3.Distance(myPos, otherWall.transform.position);
            if (distance <= connectionDistance)
            {
                connectedWalls.Add(otherWall);
            }
        }

        // Update visual based on connections
        // UpdateVisualMesh();
    }
    finally
    {
        isUpdating = false;
    }
}
```

### Event Integration
```csharp
private void Start()
{
    if (!enableConnections) return;

    // Register this wall
    RegisterWall();

    // Subscribe to building events
    EventBus.Subscribe<BuildingPlacedEvent>(OnBuildingPlaced);
    EventBus.Subscribe<BuildingDestroyedEvent>(OnBuildingDestroyed);

    // Delay initial update to avoid Start() race conditions
    Invoke(nameof(DelayedInitialUpdate), 0.1f);
}

private void OnBuildingPlaced(BuildingPlacedEvent evt)
{
    // Only process if this is a wall and it's nearby
    if (evt.Building == null) return;

    var wallSystem = evt.Building.GetComponent<WallConnectionSystem>();
    if (wallSystem == null) return;

    float distance = Vector3.Distance(transform.position, evt.Position);
    if (distance <= connectionDistance * 2f)
    {
        // Use delayed update to prevent immediate cascade
        Invoke(nameof(UpdateConnections), 0.05f);
    }
}
```

---

## 4. BUILDING PLACEMENT - BuildingManager.cs

### Preview & Validation
```csharp
private void UpdateBuildingPreview()
{
    if (previewBuilding == null || mouse == null) return;

    Vector2 mousePos = mouse.position.ReadValue();
    Ray ray = mainCamera.ScreenPointToRay(mousePos);

    if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
    {
        Vector3 position = hit.point;

        if (useGridSnapping)
        {
            position.x = Mathf.Round(position.x / gridSize) * gridSize;
            position.z = Mathf.Round(position.z / gridSize) * gridSize;
        }

        // Adjust Y position to place building bottom on ground
        position.y = hit.point.y + GetBuildingGroundOffset(previewBuilding);

        previewBuilding.transform.position = position;

        // Check if placement is valid
        canPlace = IsValidPlacement(position);

        // Update preview material
        SetPreviewMaterial(canPlace ? validPlacementMaterial : invalidPlacementMaterial);
    }
}

private bool IsValidPlacement(Vector3 position)
{
    if (previewBuilding == null) return false;

    // Get bounds for overlap check
    Bounds buildingBounds = GetBuildingBounds(previewBuilding);

    // Check for overlapping with OTHER BUILDINGS (ignore terrain!)
    Collider[] colliders = Physics.OverlapBox(
        buildingBounds.center,
        buildingBounds.extents,
        previewBuilding.transform.rotation,
        ~groundLayer // Exclude ground layer
    );

    foreach (var col in colliders)
    {
        // Ignore colliders on preview itself
        if (col.gameObject == previewBuilding || col.transform.IsChildOf(previewBuilding.transform))
        {
            continue;
        }

        // Ignore terrain colliders
        if (col is TerrainCollider)
        {
            continue;
        }

        // Block placement for ANY other collider
        Debug.Log($"Cannot place: colliding with {col.gameObject.name}");
        return false;
    }

    // Check if ground is suitable
    if (!IsGroundSuitable(position))
    {
        return false;
    }

    return true;
}
```

### Building Placement
```csharp
private void PlaceBuilding()
{
    if (previewBuilding == null || currentBuildingData == null) return;

    Vector3 position = previewBuilding.transform.position;
    Quaternion rotation = previewBuilding.transform.rotation;

    // CHECK COSTS AND SPEND RESOURCES
    if (resourceService != null)
    {
        var costs = currentBuildingData.GetCosts();

        if (!resourceService.CanAfford(costs))
        {
            Debug.Log($"Not enough resources for {currentBuildingData.buildingName}!");

            EventBus.Publish(new ResourcesSpentEvent(
                costs.GetValueOrDefault(ResourceType.Wood, 0),
                costs.GetValueOrDefault(ResourceType.Food, 0),
                costs.GetValueOrDefault(ResourceType.Gold, 0),
                costs.GetValueOrDefault(ResourceType.Stone, 0),
                false
            ));

            return;
        }

        // SPEND THE RESOURCES!
        bool success = resourceService.SpendResources(costs);
        if (!success)
        {
            Debug.LogError("Failed to spend resources!");
            return;
        }
    }

    // PLACE THE ACTUAL BUILDING
    GameObject newBuilding = Instantiate(currentBuildingData.buildingPrefab, position, rotation);

    // ENSURE BUILDING HAS DATA REFERENCE
    var buildingComponent = newBuilding.GetComponent<Building>();
    if (buildingComponent != null)
    {
        buildingComponent.SetData(currentBuildingData);
        Debug.Log($"✅ Assigned {currentBuildingData.buildingName} data to building component");
    }

    // Publish event (triggers wall updates, etc.)
    EventBus.Publish(new BuildingPlacedEvent(newBuilding, position));

    Debug.Log($"✅ Placed building: {currentBuildingData.buildingName} at {position}");

    // Cancel placement
    CancelPlacement();
}
```

---

## 5. RESOURCE SYSTEM - ResourceManager.cs

### Resource Management
```csharp
public class ResourceManager : MonoBehaviour, IResourcesService
{
    [SerializeField] private int startingWood = 100;
    [SerializeField] private int startingFood = 100;
    [SerializeField] private int startingGold = 50;
    [SerializeField] private int startingStone = 50;

    private Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>();

    public int Wood => GetResource(ResourceType.Wood);
    public int Food => GetResource(ResourceType.Food);
    public int Gold => GetResource(ResourceType.Gold);
    public int Stone => GetResource(ResourceType.Stone);

    public int GetResource(ResourceType type)
    {
        return resources.TryGetValue(type, out int amount) ? amount : 0;
    }

    public bool CanAfford(Dictionary<ResourceType, int> costs)
    {
        if (costs == null) return true;

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
        {
            PublishSpendEvent(costs, false);
            return false;
        }

        // Spend the resources
        Dictionary<ResourceType, int> deltas = new Dictionary<ResourceType, int>();
        foreach (var cost in costs)
        {
            resources[cost.Key] -= cost.Value;
            deltas[cost.Key] = -cost.Value;
        }

        PublishResourcesChanged(deltas);
        PublishSpendEvent(costs, true);
        return true;
    }

    public void AddResources(Dictionary<ResourceType, int> amounts)
    {
        if (amounts == null) return;

        Dictionary<ResourceType, int> deltas = new Dictionary<ResourceType, int>();

        foreach (var amount in amounts)
        {
            resources[amount.Key] = Mathf.Max(0, resources[amount.Key] + amount.Value);
            deltas[amount.Key] = amount.Value;
        }

        PublishResourcesChanged(deltas);
    }
}
```

---

## 6. EVENT BUS - GameEvents.cs & EventBus.cs

### Event Definitions
```csharp
// Building placement event
public struct BuildingPlacedEvent
{
    public GameObject Building { get; }
    public Vector3 Position { get; }

    public BuildingPlacedEvent(GameObject building, Vector3 position)
    {
        Building = building;
        Position = position;
    }
}

// Building completion event
public struct BuildingCompletedEvent
{
    public GameObject Building { get; }
    public string BuildingName { get; }

    public BuildingCompletedEvent(GameObject building, string buildingName)
    {
        Building = building;
        BuildingName = buildingName;
    }
}

// Resource generation event
public struct ResourcesGeneratedEvent
{
    public string BuildingName { get; }
    public ResourceType ResourceType { get; }
    public int Amount { get; }

    public ResourcesGeneratedEvent(string buildingName, ResourceType resourceType, int amount)
    {
        BuildingName = buildingName;
        ResourceType = resourceType;
        Amount = amount;
    }
}
```

### Event Publishing & Subscription
```csharp
// Publish event
EventBus.Publish(new BuildingPlacedEvent(newBuilding, position));

// Subscribe to event
EventBus.Subscribe<BuildingPlacedEvent>(OnBuildingPlaced);

// Event handler
private void OnBuildingPlaced(BuildingPlacedEvent evt)
{
    Debug.Log($"Building placed at {evt.Position}");
    var wallSystem = evt.Building.GetComponent<WallConnectionSystem>();
    if (wallSystem != null)
    {
        wallSystem.UpdateConnections();
    }
}

// Unsubscribe on destroy
private void OnDestroy()
{
    EventBus.Unsubscribe<BuildingPlacedEvent>(OnBuildingPlaced);
}
```

---

## 7. UNIT COMBAT - UnitCombat.cs Pattern

### Combat Framework
```csharp
public class UnitCombat : MonoBehaviour
{
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRate = 1f; // attacks per second

    private float lastAttackTime = -999f;
    private Transform currentTarget;

    public bool CanAttack()
    {
        if (!canAttack) return false;
        if (currentTarget == null) return false;
        if (!IsTargetInRange(currentTarget)) return false;
        if (Time.time < lastAttackTime + (1f / attackRate)) return false;

        var targetHealth = currentTarget.GetComponent<UnitHealth>();
        if (targetHealth != null && targetHealth.IsDead) return false;

        return true;
    }

    public bool TryAttack()
    {
        if (!CanAttack()) return false;

        PerformAttack();
        return true;
    }

    private void PerformAttack()
    {
        lastAttackTime = Time.time;

        var targetHealth = currentTarget.GetComponent<UnitHealth>();
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(attackDamage, gameObject);
        }
    }

    public bool IsTargetInRange(Transform target)
    {
        if (target == null) return false;
        return Vector3.Distance(transform.position, target.position) <= attackRange;
    }
}
```

---

## 8. BUILDING DATA CONFIGURATION - BuildingDataSO.cs

### Data Structure
```csharp
[CreateAssetMenu(fileName = "BuildingData", menuName = "RTS/BuildingData")]
public class BuildingDataSO : ScriptableObject
{
    [Header("Identity")]
    public string buildingName = "Building";
    public BuildingType buildingType = BuildingType.Residential;
    public string description = "A building";
    public Sprite icon;

    [Header("Costs")]
    public int woodCost = 0;
    public int foodCost = 0;
    public int goldCost = 0;
    public int stoneCost = 0;

    [Header("Effects")]
    public float happinessBonus = 0f;
    public bool generatesResources = false;

    [Header("Resource Generation")]
    public ResourceType resourceType = ResourceType.Wood;
    public int resourceAmount = 10;
    public float generationInterval = 5f;

    [Header("Construction")]
    public float constructionTime = 5f;
    public GameObject buildingPrefab;

    [Header("Health & Repair")]
    public int maxHealth = 100;
    public float repairCostMultiplier = 0.5f;

    public Dictionary<ResourceType, int> GetCosts()
    {
        var costs = new Dictionary<ResourceType, int>();

        if (woodCost > 0) costs[ResourceType.Wood] = woodCost;
        if (foodCost > 0) costs[ResourceType.Food] = foodCost;
        if (goldCost > 0) costs[ResourceType.Gold] = goldCost;
        if (stoneCost > 0) costs[ResourceType.Stone] = stoneCost;

        return costs;
    }

    public string GetCostString()
    {
        var costs = new List<string>();

        if (woodCost > 0) costs.Add($"Wood: {woodCost}");
        if (foodCost > 0) costs.Add($"Food: {foodCost}");
        if (goldCost > 0) costs.Add($"Gold: {goldCost}");
        if (stoneCost > 0) costs.Add($"Stone: {stoneCost}");

        return string.Join(", ", costs);
    }
}
```

---

## 9. SERVICE LOCATOR PATTERN - How to Access Services

### Getting Services
```csharp
// In any script that needs services:

private void Start()
{
    // Resource service
    IResourcesService resourceService = ServiceLocator.TryGet<IResourcesService>();
    
    // Happiness service
    IHappinessService happinessService = ServiceLocator.TryGet<IHappinessService>();
    
    // Building service
    IBuildingService buildingService = ServiceLocator.TryGet<IBuildingService>();

    // Always check if null (services are optional)
    if (resourceService == null)
    {
        Debug.LogError("ResourceService not available!");
        return;
    }

    // Use the service
    int wood = resourceService.GetResource(ResourceType.Wood);
    Debug.Log($"Current wood: {wood}");
}
```

### Resource Service Usage
```csharp
// Check if player can afford building
var costs = buildingData.GetCosts();
if (resourceService.CanAfford(costs))
{
    // Yes, spend resources
    resourceService.SpendResources(costs);
}
else
{
    // No, insufficient resources
    Debug.Log("Not enough resources!");
}

// Add resources (e.g., from a farm)
var gains = new Dictionary<ResourceType, int> 
{ 
    { ResourceType.Food, 25 } 
};
resourceService.AddResources(gains);
```

---

## 10. BUILDING SELECTION - BuildingSelectable.cs Pattern

### Selection Pattern
```csharp
public class BuildingSelectable : MonoBehaviour
{
    [SerializeField] private GameObject selectionIndicator;
    [SerializeField] private bool useColorHighlight = true;
    [SerializeField] private Color selectedColor = Color.cyan;

    private bool isSelected;
    private Renderer[] renderers;
    private Dictionary<Renderer, Color> originalColors = new Dictionary<Renderer, Color>();

    public void Select()
    {
        if (isSelected) return;

        isSelected = true;

        if (selectionIndicator != null)
            selectionIndicator.SetActive(true);

        if (useColorHighlight)
        {
            foreach (var rend in renderers)
            {
                if (rend != null && rend.material != null)
                    rend.material.color = selectedColor;
            }
        }

        // Publish event
        EventBus.Publish(new BuildingSelectedEvent(gameObject));
    }

    public void Deselect()
    {
        if (!isSelected) return;

        isSelected = false;

        if (selectionIndicator != null)
            selectionIndicator.SetActive(false);

        if (useColorHighlight)
        {
            foreach (var rend in renderers)
            {
                if (rend != null && rend.material != null && originalColors.ContainsKey(rend))
                    rend.material.color = originalColors[rend];
            }
        }

        // Publish event if needed
    }
}
```

---

## SUMMARY: PATTERNS TO FOLLOW

1. **Services:** Always use `ServiceLocator.TryGet<IService>()` and check for null
2. **Events:** Subscribe in Start(), unsubscribe in OnDestroy()
3. **Resource Costs:** Use `BuildingDataSO.GetCosts()` method
4. **Placement:** Use `BuildingManager.StartPlacingBuilding(BuildingDataSO)`
5. **Validation:** Check collision, terrain, and affordability before placing
6. **Building Data:** Always assign via `Building.SetData()` method
7. **Wall Segments:** Use calculated segment data with position + scale
8. **Combat:** Use OverlapSphere for target finding, cooldown check before attacking
9. **Destruction:** Always publish events and clean up references

---

**All code examples are from actual Kingdoms at Dusk source files.**  
**Follow these patterns when implementing tower system.**

