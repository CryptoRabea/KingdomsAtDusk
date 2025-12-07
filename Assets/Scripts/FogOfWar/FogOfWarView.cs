/*
 * FogOfWarView.cs
 * Adapter for integrating Fog of War system with RTS architecture
 * Automatically registers all units and buildings as fog revealers
 * 
 * Integrates with:
 * - EventBus for unit/building spawn/destroy events
 * - Component-based architecture for unit/building detection
 * - Service Locator pattern for decoupled access
 */

using FischlWorks_FogWar;
using RTS.Buildings;
using RTS.Core.Events;
using RTS.Units;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RTS.FogOfWar
{
    /// <summary>
    /// Manages fog of war by automatically registering units and buildings as revealers.
    /// Uses the EventBus to listen for spawn/destroy events.
    /// </summary>
    public class FogOfWarView : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private csFogWar fogWarSystem;
        [Tooltip("If null, will search for csFogWar in scene")]

        [Header("Default Sight Ranges")]
        [SerializeField] private int defaultUnitSightRange = 10;
        [SerializeField] private int defaultBuildingSightRange = 15;
        [SerializeField] private bool updateOnlyOnMove = true;

        [Header("Building Construction Settings")]
        [SerializeField]
        [Tooltip("If true, buildings under construction will reveal fog. If false, only completed buildings reveal fog.")]
        private bool revealDuringConstruction = false;

        [Header("Custom Sight Ranges (Optional)")]
        [SerializeField] private List<UnitSightRangeConfig> unitSightRanges = new List<UnitSightRangeConfig>();
        [SerializeField] private List<BuildingSightRangeConfig> buildingSightRanges = new List<BuildingSightRangeConfig>();

        [Header("Player Team Settings")]
        [SerializeField] private LayerMask friendlyLayers = ~0; // All layers by default
        [Tooltip("Only units/buildings on these layers will reveal fog")]

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        [SerializeField] private bool showRevealerCount = false;

        // Track active revealers
        private Dictionary<GameObject, int> activeRevealers = new Dictionary<GameObject, int>();
        private Dictionary<GameObject, Vector3> lastKnownPositions = new Dictionary<GameObject, Vector3>();

        #region Initialization

        private void Awake()
        {
            // Find fog war system if not assigned
            if (fogWarSystem == null)
            {
                fogWarSystem = FindFirstObjectByType<csFogWar>();

                if (fogWarSystem == null)
                {
                    enabled = false;
                    return;
                }

                if (showDebugLogs)
            }
        }

        private void OnEnable()
        {
            // Subscribe to unit events
            EventBus.Subscribe<UnitSpawnedEvent>(OnUnitSpawned);
            EventBus.Subscribe<UnitDiedEvent>(OnUnitDied);

            // Subscribe to building events
            EventBus.Subscribe<BuildingPlacedEvent>(OnBuildingPlaced);
            EventBus.Subscribe<BuildingCompletedEvent>(OnBuildingCompleted);
            EventBus.Subscribe<BuildingDestroyedEvent>(OnBuildingDestroyed);

            if (showDebugLogs)
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            EventBus.Unsubscribe<UnitSpawnedEvent>(OnUnitSpawned);
            EventBus.Unsubscribe<UnitDiedEvent>(OnUnitDied);
            EventBus.Unsubscribe<BuildingPlacedEvent>(OnBuildingPlaced);
            EventBus.Unsubscribe<BuildingCompletedEvent>(OnBuildingCompleted);
            EventBus.Unsubscribe<BuildingDestroyedEvent>(OnBuildingDestroyed);

            if (showDebugLogs)
        }

        private void Start()
        {
            // Register any existing units/buildings in the scene
            RegisterExistingEntities();

            if (showDebugLogs)
        }

        #endregion

        #region Event Handlers

        private void OnUnitSpawned(UnitSpawnedEvent evt)
        {
            if (evt.Unit == null) return;

            // Check if unit is on friendly layer
            if (!IsOnFriendlyLayer(evt.Unit))
            {
                if (showDebugLogs)
                return;
            }

            int sightRange = GetUnitSightRange(evt.Unit);
            RegisterRevealer(evt.Unit, sightRange);

            if (showDebugLogs)
        }

        private void OnUnitDied(UnitDiedEvent evt)
        {
            if (evt.Unit == null) return;

            UnregisterRevealer(evt.Unit);

            if (showDebugLogs)
        }

        private void OnBuildingPlaced(BuildingPlacedEvent evt)
        {
            if (evt.Building == null) return;

            // Check if building is on friendly layer
            if (!IsOnFriendlyLayer(evt.Building))
            {
                if (showDebugLogs)
                return;
            }

            // Get Building component and verify it's enabled
            var buildingComponent = evt.Building.GetComponent<Building>();
            if (buildingComponent == null)
            {
                if (showDebugLogs)
                return;
            }

            // Skip if Building component is disabled (preview buildings)
            if (!buildingComponent.enabled)
            {
                if (showDebugLogs)
                return;
            }

            // Determine if we should register immediately
            bool shouldRegisterNow = buildingComponent.IsConstructed || revealDuringConstruction;

            if (shouldRegisterNow)
            {
                int sightRange = GetBuildingSightRange(evt.Building);
                RegisterRevealer(evt.Building, sightRange);

                if (showDebugLogs)
                {
                    string reason = buildingComponent.IsConstructed ? "no construction required" : "reveal during construction enabled";
                }
            }
            else
            {
                if (showDebugLogs)
            }
        }

        private void OnBuildingCompleted(BuildingCompletedEvent evt)
        {
            if (evt.Building == null) return;

            // Check if building is on friendly layer
            if (!IsOnFriendlyLayer(evt.Building))
            {
                if (showDebugLogs)
                return;
            }

            // Check if already registered (in case building didn't require construction)
            if (activeRevealers.ContainsKey(evt.Building))
            {
                if (showDebugLogs)
                return;
            }

            // Register the building now that construction is complete
            int sightRange = GetBuildingSightRange(evt.Building);
            RegisterRevealer(evt.Building, sightRange);

            if (showDebugLogs)
        }

        private void OnBuildingDestroyed(BuildingDestroyedEvent evt)
        {
            if (evt.Building == null) return;

            UnregisterRevealer(evt.Building);

            if (showDebugLogs)
        }

        #endregion

        #region Revealer Management

        private void RegisterRevealer(GameObject entity, int sightRange)
        {
            if (entity == null || fogWarSystem == null) return;

            // Safety check: Don't register buildings with disabled Building component (previews)
            var buildingComponent = entity.GetComponent<Building>();
            if (buildingComponent != null && !buildingComponent.enabled)
            {
                if (showDebugLogs)
                return;
            }

            // Don't register if already registered
            if (activeRevealers.ContainsKey(entity))
            {
                if (showDebugLogs)
                return;
            }

            // Check if entity has custom update behavior
            bool entityUpdateOnMove = updateOnlyOnMove;
            var revealerConfig = entity.GetComponent<FogRevealerConfig>();
            if (revealerConfig != null && revealerConfig.OverrideUpdateBehavior)
            {
                entityUpdateOnMove = revealerConfig.UpdateOnlyOnMove;
            }

            // Create FogRevealer and add to fog war system
            var fogRevealer = new csFogWar.FogRevealer(
                entity.transform,
                sightRange,
                entityUpdateOnMove
            );

            int revealerIndex = fogWarSystem.AddFogRevealer(fogRevealer);

            // Track the revealer
            activeRevealers[entity] = revealerIndex;
            lastKnownPositions[entity] = entity.transform.position;
        }

        private void UnregisterRevealer(GameObject entity)
        {
            if (entity == null || fogWarSystem == null) return;

            if (activeRevealers.TryGetValue(entity, out int revealerIndex))
            {
                fogWarSystem.RemoveFogRevealer(revealerIndex);
                activeRevealers.Remove(entity);
                lastKnownPositions.Remove(entity);
            }
        }

        #endregion

        #region Sight Range Configuration

        private int GetUnitSightRange(GameObject unit)
        {
            // First, check if unit has FogRevealerConfig component for per-entity override
            var revealerConfig = unit.GetComponent<FogRevealerConfig>();
            if (revealerConfig != null && revealerConfig.OverrideSightRange)
            {
                return revealerConfig.CustomSightRange;
            }

            // Try to get UnitConfigSO from the UnitAIController
            var unitAI = unit.GetComponent<RTS.Units.AI.UnitAIController>();
            if (unitAI != null && unitAI.Config != null)
            {
                // Check if we have a custom sight range for this unit type
                var config = unitSightRanges.FirstOrDefault(c => c.unitName == unitAI.Config.unitName);
                if (config != null)
                    return config.sightRange;
            }

            // Check by unit name if no config found
            var nameConfig = unitSightRanges.FirstOrDefault(c => unit.name.Contains(c.unitName));
            if (nameConfig != null)
                return nameConfig.sightRange;

            return defaultUnitSightRange;
        }

        private int GetBuildingSightRange(GameObject building)
        {
            // First, check if building has FogRevealerConfig component for per-entity override
            var revealerConfig = building.GetComponent<FogRevealerConfig>();
            if (revealerConfig != null && revealerConfig.OverrideSightRange)
            {
                return revealerConfig.CustomSightRange;
            }

            // Try to get Building component
            var buildingComponent = building.GetComponent<Building>();
            if (buildingComponent != null && buildingComponent.Data != null)
            {
                // Check if we have a custom sight range for this building type
                var config = buildingSightRanges.FirstOrDefault(c => c.buildingName == buildingComponent.Data.buildingName);
                if (config != null)
                    return config.sightRange;
            }

            // Check by building name if no config found
            var nameConfig = buildingSightRanges.FirstOrDefault(c => building.name.Contains(c.buildingName));
            if (nameConfig != null)
                return nameConfig.sightRange;

            return defaultBuildingSightRange;
        }

        private bool IsOnFriendlyLayer(GameObject entity)
        {
            return ((1 << entity.layer) & friendlyLayers) != 0;
        }

        #endregion

        #region Existing Entity Registration

        private void RegisterExistingEntities()
        {
            // Find all existing units in the scene
            var existingUnits = FindObjectsByType<UnitHealth>(FindObjectsSortMode.None);
            int registeredUnits = 0;

            foreach (var unit in existingUnits)
            {
                if (unit != null && IsOnFriendlyLayer(unit.gameObject))
                {
                    int sightRange = GetUnitSightRange(unit.gameObject);
                    RegisterRevealer(unit.gameObject, sightRange);
                    registeredUnits++;
                }
            }

            // Find all existing buildings in the scene
            var existingBuildings = FindObjectsByType<Building>(FindObjectsSortMode.None);
            int registeredBuildings = 0;

            foreach (var building in existingBuildings)
            {
                // Skip null buildings
                if (building == null) continue;

                // Skip buildings with disabled components (previews)
                if (!building.enabled)
                {
                    if (showDebugLogs)
                    continue;
                }

                // Skip non-friendly buildings
                if (!IsOnFriendlyLayer(building.gameObject))
                    continue;

                int sightRange = GetBuildingSightRange(building.gameObject);
                RegisterRevealer(building.gameObject, sightRange);
                registeredBuildings++;
            }

            if (showDebugLogs)
        }

        #endregion

        #region Update and Cleanup

        private void Update()
        {
            if (showRevealerCount && Time.frameCount % 60 == 0)
            {
            }

            // Clean up any null references
            CleanupNullRevealers();
        }

        private void CleanupNullRevealers()
        {
            // Remove any revealers whose GameObjects have been destroyed
            var nullKeys = activeRevealers.Keys.Where(k => k == null).ToList();

            foreach (var nullKey in nullKeys)
            {
                activeRevealers.Remove(nullKey);
                lastKnownPositions.Remove(nullKey);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Manually register a unit or building as a fog revealer.
        /// </summary>
        public void ManuallyRegisterRevealer(GameObject entity, int sightRange)
        {
            if (entity == null)
            {
                return;
            }

            RegisterRevealer(entity, sightRange);
        }

        /// <summary>
        /// Manually unregister a unit or building.
        /// </summary>
        public void ManuallyUnregisterRevealer(GameObject entity)
        {
            if (entity == null) return;
            UnregisterRevealer(entity);
        }

        /// <summary>
        /// Get the current number of active revealers.
        /// </summary>
        public int GetRevealerCount()
        {
            return activeRevealers.Count;
        }

        /// <summary>
        /// Check if an entity is registered as a revealer.
        /// </summary>
        public bool IsRegistered(GameObject entity)
        {
            return entity != null && activeRevealers.ContainsKey(entity);
        }

        /// <summary>
        /// Force refresh all revealers (useful after scene changes).
        /// </summary>
        public void RefreshAllRevealers()
        {
            // Clear existing revealers
            activeRevealers.Clear();
            lastKnownPositions.Clear();

            // Re-register all entities
            RegisterExistingEntities();

            if (showDebugLogs)
        }

        #endregion

        #region Configuration Classes

        [System.Serializable]
        public class UnitSightRangeConfig
        {
            public string unitName;
            [Tooltip("Sight range in world units (will be converted to fog grid units)")]
            public int sightRange = 10;
        }

        [System.Serializable]
        public class BuildingSightRangeConfig
        {
            public string buildingName;
            [Tooltip("Sight range in world units (will be converted to fog grid units)")]
            public int sightRange = 15;
        }

        #endregion

        #region Editor Utilities

#if UNITY_EDITOR
        [ContextMenu("Debug: List All Revealers")]
        private void DebugListRevealers()
        {
            foreach (var kvp in activeRevealers)
            {
                if (kvp.Key != null)
                {
                    GameObject entity = kvp.Key;
                    int sightRange = entity.GetComponent<UnitHealth>() != null
                        ? GetUnitSightRange(entity)
                        : GetBuildingSightRange(entity);

                }
            }
        }

        [ContextMenu("Debug: Refresh All Revealers")]
        private void DebugRefreshRevealers()
        {
            RefreshAllRevealers();
        }

        [ContextMenu("Debug: Clear All Revealers")]
        private void DebugClearRevealers()
        {
            foreach (var kvp in activeRevealers.ToList())
            {
                if (kvp.Key != null)
                    UnregisterRevealer(kvp.Key);
            }
        }
#endif

        #endregion
    }
}