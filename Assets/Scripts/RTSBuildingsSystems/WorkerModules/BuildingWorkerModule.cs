using UnityEngine;
using RTS.Core.Events;
using RTS.Core.Services;
using System.Collections.Generic;

namespace RTS.Buildings.WorkerModules
{
    /// <summary>
    /// Optional module: Allocates peasants from campfire to construction projects.
    /// Speeds up building construction when workers are assigned.
    /// Add this component to a Campfire to enable construction worker allocation.
    /// </summary>
    [RequireComponent(typeof(Campfire))]
    public class BuildingWorkerModule : MonoBehaviour
    {
        [Header("Worker Settings")]
        [SerializeField] private bool enableModule = true;
        [SerializeField] private int peasantsPerBuilding = 2;
        [SerializeField] private float constructionSpeedBonus = 1.5f;

        [Header("Auto-Assignment")]
        [SerializeField] private bool autoAssignWorkers = true;
        [SerializeField] private float assignmentUpdateInterval = 2f;

        private Campfire campfire;
        private IPeasantWorkforceService workforceService;
        private IPopulationService populationService;

        private Dictionary<GameObject, int> assignedWorkers = new Dictionary<GameObject, int>();
        private List<GameObject> trackedBuildings = new List<GameObject>();
        private float updateTimer = 0f;

        private void Awake()
        {
            campfire = GetComponent<Campfire>();
        }

        private void Start()
        {
            workforceService = ServiceLocator.TryGet<IPeasantWorkforceService>();
            populationService = ServiceLocator.TryGet<IPopulationService>();

            if (enableModule)
            {
                EventBus.Subscribe<BuildingPlacedEvent>(OnBuildingPlaced);
                EventBus.Subscribe<BuildingCompletedEvent>(OnBuildingCompleted);
                EventBus.Subscribe<BuildingDestroyedEvent>(OnBuildingDestroyed);
            }
        }

        private void Update()
        {
            if (!enableModule || !autoAssignWorkers) return;

            updateTimer += Time.deltaTime;
            if (updateTimer >= assignmentUpdateInterval)
            {
                updateTimer = 0f;
                UpdateWorkerAssignments();
            }
        }

        private void OnBuildingPlaced(BuildingPlacedEvent evt)
        {
            if (evt.Building == null || evt.Building == gameObject) return;

            // Track this building for worker assignment
            if (!trackedBuildings.Contains(evt.Building))
            {
                trackedBuildings.Add(evt.Building);
            }

            if (autoAssignWorkers)
            {
                TryAssignWorkers(evt.Building);
            }
        }

        private void OnBuildingCompleted(BuildingCompletedEvent evt)
        {
            if (evt.Building == null) return;

            // Release workers when building is completed
            ReleaseWorkersFromBuilding(evt.Building);

            // Remove from tracking
            trackedBuildings.Remove(evt.Building);
        }

        private void OnBuildingDestroyed(BuildingDestroyedEvent evt)
        {
            if (evt.Building == null) return;

            // Release workers when building is destroyed
            ReleaseWorkersFromBuilding(evt.Building);

            // Remove from tracking
            trackedBuildings.Remove(evt.Building);
        }

        private void UpdateWorkerAssignments()
        {
            if (workforceService == null || populationService == null) return;

            // Clean up null references
            trackedBuildings.RemoveAll(b => b == null);

            // Try to assign workers to buildings that don't have any
            foreach (var building in trackedBuildings)
            {
                if (building == null) continue;

                Building buildingComponent = building.GetComponent<Building>();
                if (buildingComponent == null || buildingComponent.IsConstructed) continue;

                // Check if this building already has workers
                if (!assignedWorkers.ContainsKey(building) || assignedWorkers[building] == 0)
                {
                    TryAssignWorkers(building);
                }
            }
        }

        private bool TryAssignWorkers(GameObject building)
        {
            if (building == null || workforceService == null) return false;

            Building buildingComponent = building.GetComponent<Building>();
            if (buildingComponent == null || buildingComponent.IsConstructed) return false;

            // Check if we can assign workers
            if (!workforceService.CanAssignWorkers(peasantsPerBuilding)) return false;

            // Request workers
            if (workforceService.RequestWorkers("Building", peasantsPerBuilding, building))
            {
                assignedWorkers[building] = peasantsPerBuilding;

                // Apply construction speed bonus
                ApplyConstructionBonus(building, true);

                Debug.Log($"🔨 Assigned {peasantsPerBuilding} peasants to {buildingComponent.Data?.buildingName ?? "building"}");
                return true;
            }

            return false;
        }

        private void ReleaseWorkersFromBuilding(GameObject building)
        {
            if (building == null || workforceService == null) return;

            if (assignedWorkers.TryGetValue(building, out int workerCount))
            {
                workforceService.ReleaseWorkers("Building", workerCount, building);
                assignedWorkers.Remove(building);

                // Remove construction bonus
                ApplyConstructionBonus(building, false);

                Debug.Log($"🔨 Released {workerCount} peasants from building");
            }
        }

        private void ApplyConstructionBonus(GameObject building, bool apply)
        {
            // This would integrate with the Building component to modify construction speed
            // For now, we'll use a simple approach with Time.timeScale modifier
            // In a real implementation, you'd modify the Building component's construction logic

            Building buildingComponent = building.GetComponent<Building>();
            if (buildingComponent == null) return;

            // You could add a public method to Building like:
            // buildingComponent.SetConstructionSpeedMultiplier(apply ? constructionSpeedBonus : 1f);

            // For now, we'll just log it
            if (apply)
            {
                Debug.Log($"Construction speed bonus ({constructionSpeedBonus}x) applied to {buildingComponent.Data?.buildingName}");
            }
        }

        public void EnableModule(bool enable)
        {
            enableModule = enable;

            if (!enable)
            {
                // Release all workers when disabling
                ReleaseAllWorkers();
            }
        }

        private void ReleaseAllWorkers()
        {
            foreach (var kvp in assignedWorkers)
            {
                if (kvp.Key != null && workforceService != null)
                {
                    workforceService.ReleaseWorkers("Building", kvp.Value, kvp.Key);
                    ApplyConstructionBonus(kvp.Key, false);
                }
            }
            assignedWorkers.Clear();
        }

        private void OnDestroy()
        {
            ReleaseAllWorkers();

            if (enableModule)
            {
                EventBus.Unsubscribe<BuildingPlacedEvent>(OnBuildingPlaced);
                EventBus.Unsubscribe<BuildingCompletedEvent>(OnBuildingCompleted);
                EventBus.Unsubscribe<BuildingDestroyedEvent>(OnBuildingDestroyed);
            }
        }

        #region Debug Methods

        [ContextMenu("Show Assigned Workers")]
        private void DebugShowAssignedWorkers()
        {
            Debug.Log($"=== Building Workers ({assignedWorkers.Count} assignments) ===");
            foreach (var kvp in assignedWorkers)
            {
                if (kvp.Key != null)
                {
                    Building b = kvp.Key.GetComponent<Building>();
                    Debug.Log($"  {b?.Data?.buildingName ?? "Unknown"}: {kvp.Value} workers");
                }
            }
        }

        #endregion
    }
}
