using UnityEngine;
using RTS.Core.Events;
using RTS.Core.Services;
using System.Collections.Generic;

namespace RTS.Buildings.WorkerModules
{
    /// <summary>
    /// Optional module: Allocates peasants from campfire to resource generation buildings.
    /// Increases resource production when workers are assigned.
    /// Add this component to a Campfire to enable resource worker allocation.
    /// </summary>
    [RequireComponent(typeof(Campfire))]
    public class ResourceWorkerModule : MonoBehaviour
    {
        [Header("Worker Settings")]
        [SerializeField] private bool enableModule = true;
        [SerializeField] private int peasantsPerResourceBuilding = 3;
        [SerializeField] private float resourceProductionBonus = 1.5f;

        [Header("Auto-Assignment")]
        [SerializeField] private bool autoAssignWorkers = true;
        [SerializeField] private float assignmentUpdateInterval = 3f;

        [Header("Target Building Types")]
        [SerializeField] private bool assignToFarms = true;
        [SerializeField] private bool assignToMines = true;
        [SerializeField] private bool assignToLumberMills = true;
        [SerializeField] private bool assignToQuarries = true;

        private Campfire campfire;
        private IPeasantWorkforceService workforceService;
        private IPopulationService populationService;

        private Dictionary<GameObject, int> assignedWorkers = new Dictionary<GameObject, int>();
        private List<GameObject> trackedResourceBuildings = new List<GameObject>();
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
                EventBus.Subscribe<BuildingCompletedEvent>(OnBuildingCompleted);
                EventBus.Subscribe<BuildingDestroyedEvent>(OnBuildingDestroyed);

                // Find existing resource buildings
                FindExistingResourceBuildings();
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

        private void OnBuildingCompleted(BuildingCompletedEvent evt)
        {
            if (evt.Building == null) return;

            Building building = evt.Building.GetComponent<Building>();
            if (building == null || building.Data == null) return;

            // Check if it's a resource-generating building
            if (building.Data.generatesResources && IsTargetBuildingType(building.Data.buildingName))
            {
                if (!trackedResourceBuildings.Contains(evt.Building))
                {
                    trackedResourceBuildings.Add(evt.Building);
                }

                if (autoAssignWorkers)
                {
                    TryAssignWorkers(evt.Building);
                }
            }
        }

        private void OnBuildingDestroyed(BuildingDestroyedEvent evt)
        {
            if (evt.Building == null) return;

            ReleaseWorkersFromBuilding(evt.Building);
            trackedResourceBuildings.Remove(evt.Building);
        }

        private void FindExistingResourceBuildings()
        {
            GameObject[] allBuildings = GameObject.FindGameObjectsWithTag("Building");
            if (allBuildings == null) return;

            foreach (var buildingObj in allBuildings)
            {
                if (buildingObj == null) continue;

                Building building = buildingObj.GetComponent<Building>();
                if (building == null || !building.IsConstructed || building.Data == null) continue;

                if (building.Data.generatesResources && IsTargetBuildingType(building.Data.buildingName))
                {
                    if (!trackedResourceBuildings.Contains(buildingObj))
                    {
                        trackedResourceBuildings.Add(buildingObj);
                    }
                }
            }
        }

        private bool IsTargetBuildingType(string buildingName)
        {
            if (string.IsNullOrEmpty(buildingName)) return false;

            string lowerName = buildingName.ToLower();

            if (assignToFarms && lowerName.Contains("farm")) return true;
            if (assignToMines && (lowerName.Contains("mine") || lowerName.Contains("gold"))) return true;
            if (assignToLumberMills && (lowerName.Contains("lumber") || lowerName.Contains("wood"))) return true;
            if (assignToQuarries && (lowerName.Contains("quarry") || lowerName.Contains("stone"))) return true;

            return false;
        }

        private void UpdateWorkerAssignments()
        {
            if (workforceService == null) return;

            // Clean up null references
            trackedResourceBuildings.RemoveAll(b => b == null);

            // Try to assign workers to buildings without them
            foreach (var building in trackedResourceBuildings)
            {
                if (building == null) continue;

                Building buildingComponent = building.GetComponent<Building>();
                if (buildingComponent == null || !buildingComponent.IsConstructed) continue;

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
            if (buildingComponent == null || !buildingComponent.IsConstructed) return false;

            // Check if we can assign workers
            if (!workforceService.CanAssignWorkers(peasantsPerResourceBuilding)) return false;

            // Request workers
            if (workforceService.RequestWorkers("Resource", peasantsPerResourceBuilding, building))
            {
                assignedWorkers[building] = peasantsPerResourceBuilding;

                // Apply production bonus
                ApplyProductionBonus(building, true);

                return true;
            }

            return false;
        }

        private void ReleaseWorkersFromBuilding(GameObject building)
        {
            if (building == null || workforceService == null) return;

            if (assignedWorkers.TryGetValue(building, out int workerCount))
            {
                workforceService.ReleaseWorkers("Resource", workerCount, building);
                assignedWorkers.Remove(building);

                // Remove production bonus
                ApplyProductionBonus(building, false);

            }
        }

        private void ApplyProductionBonus(GameObject building, bool apply)
        {
            // This would integrate with the Building component to modify resource generation
            // In a real implementation, you'd modify the resource generation rate/amount

            Building buildingComponent = building.GetComponent<Building>();
            if (buildingComponent == null) return;

            // You could add a public method to Building like:
            // buildingComponent.SetResourceProductionMultiplier(apply ? resourceProductionBonus : 1f);

            if (apply)
            {
            }
        }

        public void EnableModule(bool enable)
        {
            enableModule = enable;

            if (!enable)
            {
                ReleaseAllWorkers();
            }
        }

        private void ReleaseAllWorkers()
        {
            foreach (var kvp in assignedWorkers)
            {
                if (kvp.Key != null && workforceService != null)
                {
                    workforceService.ReleaseWorkers("Resource", kvp.Value, kvp.Key);
                    ApplyProductionBonus(kvp.Key, false);
                }
            }
            assignedWorkers.Clear();
        }

        private void OnDestroy()
        {
            ReleaseAllWorkers();

            if (enableModule)
            {
                EventBus.Unsubscribe<BuildingCompletedEvent>(OnBuildingCompleted);
                EventBus.Unsubscribe<BuildingDestroyedEvent>(OnBuildingDestroyed);
            }
        }

        #region Debug Methods

        [ContextMenu("Show Assigned Workers")]
        private void DebugShowAssignedWorkers()
        {
            foreach (var kvp in assignedWorkers)
            {
                if (kvp.Key != null)
                {
                    Building b = kvp.Key.GetComponent<Building>();
                }
            }
        }

        [ContextMenu("Refresh Resource Buildings List")]
        private void DebugRefreshBuildings()
        {
            trackedResourceBuildings.Clear();
            FindExistingResourceBuildings();
        }

        #endregion
    }
}
