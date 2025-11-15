# Fog of War Implementation Guide

## Summary of Key Architectural Components

This guide identifies the exact files and code patterns you need to integrate with to implement a fog of war system.

---

## 1. VISIBILITY STATE TRACKING

### Primary Integration Point: MinimapEntity Component

**File:** `/Assets/Scripts/UI/Minimap/MinimapEntity.cs`

**Current Implementation:**
```csharp
public class MinimapEntity : MonoBehaviour, IMinimapEntity
{
    [SerializeField] private MinimapEntityOwnership ownership = MinimapEntityOwnership.Friendly;
    public MinimapEntityOwnership GetOwnership() => ownership;
}
```

**Extension for Fog of War:**
```csharp
// Add to MinimapEntity.cs
[SerializeField] private bool isVisibleToPlayer = true;
[SerializeField] private int[] visibleToPlayers = { 0 }; // Player IDs who can see this

public bool IsVisibleToPlayer(int playerIndex)
{
    return System.Array.IndexOf(visibleToPlayers, playerIndex) >= 0;
}

public void SetVisibility(int playerIndex, bool visible)
{
    // Update visibility state
}
```

---

## 2. UNIT POSITION MONITORING

### Units - Primary Source of Positions

**File:** `/Assets/Scripts/Units/Components/UnitMovement.cs`

**Key Properties:**
```csharp
public void SetDestination(Vector3 destination);
public bool IsMoving => agent != null && agent.velocity.sqrMagnitude > 0.01f;
public Transform currentTarget; // What unit is moving toward
```

**Access Unit Positions for FOW:**
```csharp
// Iterate all units
var allUnits = FindObjectsByType<UnitAIController>(FindObjectsSortMode.None);
foreach (var unit in allUnits)
{
    Vector3 position = unit.transform.position;
    UnitHealth health = unit.Health;
    float detectionRange = unit.Config.detectionRange;
    
    // Check if unit is enemy/ally
    bool isEnemy = unit.gameObject.layer == LayerMask.NameToLayer("Enemy");
}
```

**Unit Component Architecture:**
```
UnitAIController (main coordinator)
├── UnitHealth (position: transform.position)
├── UnitMovement (position: transform.position, movement data)
├── UnitCombat (range information)
├── UnitSelectable (selection state)
└── MinimapEntity (ownership information) [OPTIONAL]
```

---

## 3. BUILDING POSITION MONITORING

### Buildings - Secondary Source of Positions

**File:** `/Assets/Scripts/RTSBuildingsSystems/Building.cs`

**Key Properties:**
```csharp
public BuildingDataSO Data => data;
public bool IsConstructed => isConstructed;
public float ConstructionProgress => constructionProgress / constructionTime;
```

**Access Building Positions for FOW:**
```csharp
// Iterate all buildings
var allBuildings = FindObjectsByType<Building>(FindObjectsSortMode.None);
foreach (var building in allBuildings)
{
    Vector3 position = building.transform.position;
    BuildingDataSO data = building.Data;
    bool isConstructed = building.IsConstructed;
    
    // Check ownership
    if (building.TryGetComponent<MinimapEntity>(out var entity))
    {
        var ownership = entity.GetOwnership();
    }
}
```

---

## 4. OWNERSHIP/FACTION DETECTION

### Three Methods (Recommended Order)

#### Method 1: MinimapEntity (Most Flexible - RECOMMENDED)
```csharp
if (unit.TryGetComponent<MinimapEntity>(out var entity))
{
    MinimapEntityOwnership ownership = entity.GetOwnership();
    // Friendly, Enemy, Neutral, Ally, Player1-4
}
```

#### Method 2: Layer-Based
```csharp
bool isEnemy = unit.gameObject.layer == LayerMask.NameToLayer("Enemy");
// Works but less flexible
```

#### Method 3: Tag-Based
```csharp
bool isFriendly = unit.gameObject.CompareTag("Friendly");
bool isEnemy = unit.gameObject.CompareTag("Enemy");
// Works but less flexible
```

---

## 5. EVENT SYSTEM INTEGRATION

### Subscribe to Unit/Building Creation and Destruction

**File:** `/Assets/Scripts/Core/GameEvents.cs`

**Events to Monitor:**
```csharp
// Subscribe in OnEnable()
EventBus.Subscribe<UnitSpawnedEvent>(OnUnitSpawned);
EventBus.Subscribe<UnitDiedEvent>(OnUnitDied);
EventBus.Subscribe<BuildingPlacedEvent>(OnBuildingPlaced);
EventBus.Subscribe<BuildingDestroyedEvent>(OnBuildingDestroyed);

// Handler implementations
private void OnUnitSpawned(UnitSpawnedEvent evt)
{
    // Register unit for FOW tracking
    AddUnitToFOWSystem(evt.Unit, evt.Position);
}

private void OnUnitDied(UnitDiedEvent evt)
{
    // Unregister unit from FOW tracking
    RemoveUnitFromFOWSystem(evt.Unit);
}

private void OnBuildingPlaced(BuildingPlacedEvent evt)
{
    // Register building for FOW tracking
    AddBuildingToFOWSystem(evt.Building, evt.Position);
}

private void OnBuildingDestroyed(BuildingDestroyedEvent evt)
{
    // Unregister building from FOW tracking
    RemoveBuildingFromFOWSystem(evt.Building);
}
```

**In OnDisable(), unsubscribe:**
```csharp
private void OnDisable()
{
    EventBus.Unsubscribe<UnitSpawnedEvent>(OnUnitSpawned);
    EventBus.Unsubscribe<UnitDiedEvent>(OnUnitDied);
    EventBus.Unsubscribe<BuildingPlacedEvent>(OnBuildingPlaced);
    EventBus.Unsubscribe<BuildingDestroyedEvent>(OnBuildingDestroyed);
}
```

---

## 6. RENDERING INTEGRATION

### Layer-Based Visibility Control

**Current Setup:**
- Layer "Enemy" = Enemy units
- Layer "Default" = Friendly units

**For FOW, use layers to hide fogged units:**
```csharp
// Show unit (visible)
unit.gameObject.layer = LayerMask.NameToLayer("Default");

// Hide unit (fogged)
unit.gameObject.layer = LayerMask.NameToLayer("HiddenFOW");
```

**Shader-Based Approach (Alternative):**
```csharp
// Use MaterialPropertyBlock to control visibility
private MaterialPropertyBlock propertyBlock;
private static readonly int VisibilityID = Shader.PropertyToID("_Visibility");

propertyBlock.SetFloat(VisibilityID, isVisible ? 1f : 0f);
renderer.SetPropertyBlock(propertyBlock);
```

---

## 7. MINIMAP INTEGRATION

### Hide Fogged Markers on Minimap

**File:** `/Assets/Scripts/UI/Minimap/MinimapMarkerManager.cs`

**Current Implementation:**
```csharp
protected void UpdateMarkerPosition(RectTransform marker, Vector3 worldPosition)
{
    // Existing code...
    
    // Add FOW visibility check
    if (shouldHideMarker) // Determined by FOW system
    {
        marker.gameObject.SetActive(false);
    }
}
```

**Extension:**
```csharp
// Modify to check visibility before updating
public void UpdateMarkers(FogOfWarSystem fowSystem)
{
    foreach (var kvp in markers)
    {
        if (fowSystem.IsVisible(kvp.Key, playerIndex))
        {
            UpdateMarkerPosition(kvp.Value, kvp.Key.transform.position);
        }
        else
        {
            kvp.Value.gameObject.SetActive(false);
        }
    }
}
```

---

## 8. RECOMMENDED FOG OF WAR CONTROLLER STRUCTURE

### Create New File: `Assets/Scripts/FogOfWar/FogOfWarController.cs`

```csharp
using UnityEngine;
using RTS.Core.Events;
using RTS.Units;
using RTS.Buildings;
using System.Collections.Generic;
using System.Linq;

namespace RTS.FogOfWar
{
    /// <summary>
    /// Central fog of war system controller.
    /// Tracks visibility for all players.
    /// </summary>
    public class FogOfWarController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int playerCount = 1;
        [SerializeField] private float visionUpdateInterval = 0.2f; // Check every 0.2 seconds
        
        private Dictionary<GameObject, VisibilityData> visibilityMap = new();
        private Dictionary<int, List<UnitAIController>> playerUnits = new();
        private Dictionary<int, List<Building>> playerBuildings = new();
        private float visionUpdateTimer = 0f;
        
        private void OnEnable()
        {
            EventBus.Subscribe<UnitSpawnedEvent>(OnUnitSpawned);
            EventBus.Subscribe<UnitDiedEvent>(OnUnitDied);
            EventBus.Subscribe<BuildingPlacedEvent>(OnBuildingPlaced);
            EventBus.Subscribe<BuildingDestroyedEvent>(OnBuildingDestroyed);
        }
        
        private void OnDisable()
        {
            EventBus.Unsubscribe<UnitSpawnedEvent>(OnUnitSpawned);
            EventBus.Unsubscribe<UnitDiedEvent>(OnUnitDied);
            EventBus.Unsubscribe<BuildingPlacedEvent>(OnBuildingPlaced);
            EventBus.Unsubscribe<BuildingDestroyedEvent>(OnBuildingDestroyed);
        }
        
        private void Update()
        {
            // Update visibility periodically
            visionUpdateTimer += Time.deltaTime;
            if (visionUpdateTimer >= visionUpdateInterval)
            {
                UpdateAllVisibility();
                visionUpdateTimer = 0f;
            }
        }
        
        /// <summary>
        /// Register a unit for FOW tracking.
        /// </summary>
        private void OnUnitSpawned(UnitSpawnedEvent evt)
        {
            if (evt.Unit == null) return;
            
            var unit = evt.Unit.GetComponent<UnitAIController>();
            if (unit != null)
            {
                // Determine owner
                int playerId = DetermineUnitOwner(evt.Unit);
                
                if (!playerUnits.ContainsKey(playerId))
                    playerUnits[playerId] = new List<UnitAIController>();
                
                playerUnits[playerId].Add(unit);
                visibilityMap[evt.Unit] = new VisibilityData { IsUnit = true };
            }
        }
        
        /// <summary>
        /// Unregister a unit from FOW tracking.
        /// </summary>
        private void OnUnitDied(UnitDiedEvent evt)
        {
            if (evt.Unit == null) return;
            
            var unit = evt.Unit.GetComponent<UnitAIController>();
            if (unit != null)
            {
                int playerId = DetermineUnitOwner(evt.Unit);
                if (playerUnits.ContainsKey(playerId))
                    playerUnits[playerId].Remove(unit);
            }
            
            visibilityMap.Remove(evt.Unit);
        }
        
        /// <summary>
        /// Register a building for FOW tracking.
        /// </summary>
        private void OnBuildingPlaced(BuildingPlacedEvent evt)
        {
            if (evt.Building == null) return;
            
            var building = evt.Building.GetComponent<Building>();
            if (building != null)
            {
                int playerId = DetermineBuildingOwner(evt.Building);
                
                if (!playerBuildings.ContainsKey(playerId))
                    playerBuildings[playerId] = new List<Building>();
                
                playerBuildings[playerId].Add(building);
                visibilityMap[evt.Building] = new VisibilityData { IsUnit = false };
            }
        }
        
        /// <summary>
        /// Unregister a building from FOW tracking.
        /// </summary>
        private void OnBuildingDestroyed(BuildingDestroyedEvent evt)
        {
            if (evt.Building == null) return;
            
            var building = evt.Building.GetComponent<Building>();
            if (building != null)
            {
                int playerId = DetermineBuildingOwner(evt.Building);
                if (playerBuildings.ContainsKey(playerId))
                    playerBuildings[playerId].Remove(building);
            }
            
            visibilityMap.Remove(evt.Building);
        }
        
        /// <summary>
        /// Update visibility for all entities.
        /// </summary>
        private void UpdateAllVisibility()
        {
            foreach (var kvp in visibilityMap)
            {
                GameObject entity = kvp.Key;
                if (entity == null) continue;
                
                // Calculate who can see this entity
                bool[] visibility = CalculateVisibility(entity);
                
                // Apply visibility to rendering
                ApplyVisibility(entity, visibility);
                
                // Update minimap markers
                UpdateMinimapVisibility(entity, visibility);
            }
        }
        
        /// <summary>
        /// Calculate which players can see an entity.
        /// </summary>
        private bool[] CalculateVisibility(GameObject entity)
        {
            bool[] visibility = new bool[playerCount];
            Vector3 entityPos = entity.transform.position;
            
            // For each player, check if their units/buildings can see this entity
            for (int i = 0; i < playerCount; i++)
            {
                visibility[i] = CanPlayerSeeEntity(i, entity, entityPos);
            }
            
            return visibility;
        }
        
        /// <summary>
        /// Check if a player can see an entity (via their units or buildings).
        /// </summary>
        private bool CanPlayerSeeEntity(int playerId, GameObject entity, Vector3 entityPos)
        {
            // Check units' vision ranges
            if (playerUnits.TryGetValue(playerId, out var units))
            {
                foreach (var unit in units)
                {
                    if (unit != null && unit.Config != null)
                    {
                        float distance = Vector3.Distance(unit.transform.position, entityPos);
                        if (distance <= unit.Config.detectionRange)
                        {
                            return true;
                        }
                    }
                }
            }
            
            // Could also check buildings' vision ranges here
            
            return false;
        }
        
        /// <summary>
        /// Apply visibility to entity rendering.
        /// </summary>
        private void ApplyVisibility(GameObject entity, bool[] visibility)
        {
            // For single player (visibility[0]), directly apply
            bool isVisible = visibility[0];
            
            if (!isVisible)
            {
                // Option 1: Change layer to hide
                // entity.layer = LayerMask.NameToLayer("HiddenFOW");
                
                // Option 2: Disable renderers
                // var renderers = entity.GetComponentsInChildren<Renderer>();
                // foreach (var r in renderers) r.enabled = false;
                
                // Option 3: Use shader-based transparency
                var renderers = entity.GetComponentsInChildren<Renderer>();
                var propertyBlock = new MaterialPropertyBlock();
                foreach (var r in renderers)
                {
                    propertyBlock.SetFloat(Shader.PropertyToID("_FOWAlpha"), 0.3f);
                    r.SetPropertyBlock(propertyBlock);
                }
            }
        }
        
        /// <summary>
        /// Update minimap marker visibility.
        /// </summary>
        private void UpdateMinimapVisibility(GameObject entity, bool[] visibility)
        {
            if (entity.TryGetComponent<MinimapEntity>(out var mapEntity))
            {
                mapEntity.SetVisibility(0, visibility[0]); // For player 0
            }
        }
        
        /// <summary>
        /// Determine which player owns a unit.
        /// </summary>
        private int DetermineUnitOwner(GameObject unit)
        {
            if (unit.TryGetComponent<MinimapEntity>(out var entity))
            {
                return entity.GetPlayerId();
            }
            
            if (unit.layer == LayerMask.NameToLayer("Enemy"))
                return 1; // Enemy player
            
            return 0; // Default to player 0 (friendly)
        }
        
        /// <summary>
        /// Determine which player owns a building.
        /// </summary>
        private int DetermineBuildingOwner(GameObject building)
        {
            if (building.TryGetComponent<MinimapEntity>(out var entity))
            {
                return entity.GetPlayerId();
            }
            
            if (building.layer == LayerMask.NameToLayer("Enemy"))
                return 1; // Enemy player
            
            return 0; // Default to player 0 (friendly)
        }
        
        /// <summary>
        /// Check if a player can see an entity.
        /// </summary>
        public bool IsVisible(GameObject entity, int playerIndex)
        {
            if (visibilityMap.TryGetValue(entity, out var data))
            {
                return data.IsVisibleToPlayer[playerIndex];
            }
            return true; // Default to visible if not tracked
        }
        
        /// <summary>
        /// Data for tracking entity visibility.
        /// </summary>
        private class VisibilityData
        {
            public bool IsUnit { get; set; }
            public bool[] IsVisibleToPlayer { get; set; } = new bool[4]; // Max 4 players
        }
    }
}
```

---

## 9. INTEGRATION CHECKLIST

- [ ] Create FogOfWarController.cs
- [ ] Add FOW event subscriptions in OnEnable
- [ ] Implement vision range calculations from units
- [ ] Integrate with MinimapEntity for ownership detection
- [ ] Add visibility state tracking
- [ ] Implement rendering layer changes or shader modifications
- [ ] Update minimap marker visibility
- [ ] Test with multiple units moving
- [ ] Optimize vision range checks (cache or spatial partitioning)
- [ ] Add configuration options (vision ranges, update frequency)

---

## 10. CRITICAL FILE PATHS FOR REFERENCE

```
Assets/Scripts/
├── Core/EventBus.cs              # Event system
├── Core/GameEvents.cs            # Event definitions
├── Camera/RTSCameraController.cs  # Main game view
├── Managers/GameManager.cs        # Initialization point
├── Units/
│   ├── Components/UnitMovement.cs # Unit positions
│   ├── Components/UnitHealth.cs   # Unit state
│   └── AI/UnitAIController.cs     # Unit vision data
├── RTSBuildingsSystems/
│   ├── Building.cs                # Building positions
│   └── BuildingDataSO.cs          # Building config
├── UI/Minimap/
│   ├── MinimapEntity.cs           # [MODIFY] Add visibility tracking
│   ├── MinimapMarkerManager.cs    # [MODIFY] Hide fogged markers
│   └── MinimapConfig.cs           # FOW configuration
└── FogOfWar/
    └── FogOfWarController.cs      # [NEW] Main FOW system
```

---

## 11. QUICK START: Minimal FOW Implementation

For a quick test, start with:

1. Create `FogOfWarController.cs` (copy from section 8)
2. Attach to a GameObject in your scene
3. Subscribe to unit events
4. In Update(), calculate visibility based on unit position and detection range
5. Hide/show gameobjects based on visibility
6. Test with multiple units

This minimal setup will let you verify the integration points without building the full system.
