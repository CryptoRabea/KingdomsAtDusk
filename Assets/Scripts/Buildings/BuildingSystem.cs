using UnityEngine;
using RTS.Core.Services;
using RTS.Core.Events;
using System.Collections.Generic;

namespace RTS.Buildings
{
    /// <summary>
    /// Building configuration data using the new scalable resource system.
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingData", menuName = "RTS/BuildingData")]
    public class BuildingDataSO : ScriptableObject
    {
        public string buildingName = "Building";
        public BuildingType buildingType;

        [Header("Costs")]
        public int woodCost;
        public int foodCost;
        public int goldCost;
        public int stoneCost;

        [Header("Construction")]
        public float buildTime = 3f;

        [Header("Effects")]
        public float happinessBonus;
        public int housingCapacity;
        public float resourceGenerationRate;

        /// <summary>
        /// Get costs as a dictionary for the new resource system.
        /// This allows the building system to work with any number of resources!
        /// </summary>
        public Dictionary<ResourceType, int> GetCosts()
        {
            var costs = new Dictionary<ResourceType, int>();

            if (woodCost > 0) costs[ResourceType.Wood] = woodCost;
            if (foodCost > 0) costs[ResourceType.Food] = foodCost;
            if (goldCost > 0) costs[ResourceType.Gold] = goldCost;
            if (stoneCost > 0) costs[ResourceType.Stone] = stoneCost;

            return costs;
        }

        /// <summary>
        /// Helper to set costs programmatically (useful for procedural generation).
        /// Example: buildingData.SetCosts(ResourceCost.Build().Wood(100).Stone(50).Create())
        /// </summary>
        public void SetCosts(Dictionary<ResourceType, int> costs)
        {
            woodCost = costs.GetValueOrDefault(ResourceType.Wood, 0);
            foodCost = costs.GetValueOrDefault(ResourceType.Food, 0);
            goldCost = costs.GetValueOrDefault(ResourceType.Gold, 0);
            stoneCost = costs.GetValueOrDefault(ResourceType.Stone, 0);
        }
    }

    public enum BuildingType
    {
        House,
        Farm,
        Barracks,
        Wall,
        Tower,
        Tavern,
        Church,
        Garden
    }

    /// <summary>
    /// Building component using event-driven architecture and the new resource system.
    /// No direct dependencies on manager singletons.
    /// </summary>
    public class Building : MonoBehaviour
    {
        [SerializeField] private BuildingDataSO buildingData;

        private BuildingState currentState = BuildingState.Placing;
        private float constructionProgress = 0f;
        private bool isConstructionComplete = false;

        public BuildingDataSO Data => buildingData;
        public BuildingState State => currentState;
        public float ConstructionProgress => buildingData != null ? constructionProgress / buildingData.buildTime : 0f;

        private void Start()
        {
            if (buildingData == null)
            {
                Debug.LogError($"Building {gameObject.name} has no BuildingData assigned!");
                Destroy(gameObject);
                return;
            }

            TryStartConstruction();
        }

        private void Update()
        {
            if (currentState == BuildingState.Constructing)
            {
                UpdateConstruction();
            }
        }

        private void TryStartConstruction()
        {
            // Try to spend resources through service
            var resourceService = ServiceLocator.TryGet<IResourceService>();

            if (resourceService == null)
            {
                Debug.LogError("ResourceService not available!");
                Destroy(gameObject);
                return;
            }

            // Get costs using the new system
            var costs = buildingData.GetCosts();

            // Check if can afford
            if (!resourceService.CanAfford(costs))
            {
                Debug.Log($"Cannot afford {buildingData.buildingName}");
                Destroy(gameObject);
                return;
            }

            // Spend resources
            bool success = resourceService.SpendResources(costs);

            if (!success)
            {
                Debug.LogError("Failed to spend resources even though we could afford it!");
                Destroy(gameObject);
                return;
            }

            // Successfully placed and paid for
            currentState = BuildingState.Constructing;
            EventBus.Publish(new BuildingPlacedEvent(gameObject, transform.position));

            Debug.Log($"Started constructing {buildingData.buildingName}");
        }

        private void UpdateConstruction()
        {
            constructionProgress += Time.deltaTime;

            if (!isConstructionComplete && constructionProgress >= buildingData.buildTime)
            {
                CompleteConstruction();
            }
        }

        private void CompleteConstruction()
        {
            isConstructionComplete = true;
            currentState = BuildingState.Built;

            // Apply building effects through events
            ApplyBuildingEffects();

            EventBus.Publish(new BuildingCompletedEvent(gameObject, buildingData.buildingType.ToString()));
            Debug.Log($"{buildingData.buildingName} construction completed!");
        }

        private void ApplyBuildingEffects()
        {
            // Apply happiness bonus if any
            if (buildingData.happinessBonus > 0)
            {
                var happinessService = ServiceLocator.TryGet<IHappinessService>();
                happinessService?.AddBuildingBonus(buildingData.happinessBonus, buildingData.buildingName);
            }

            // Could add other effects here (housing, resource generation, etc.)
            // Example:
            // if (buildingData.housingCapacity > 0)
            // {
            //     var populationService = ServiceLocator.TryGet<IPopulationService>();
            //     populationService?.AddHousing(buildingData.housingCapacity);
            // }
        }

        private void OnDestroy()
        {
            // Only remove effects if construction was completed
            if (isConstructionComplete)
            {
                RemoveBuildingEffects();
                EventBus.Publish(new BuildingDestroyedEvent(gameObject, buildingData.buildingType.ToString()));
            }
        }

        private void RemoveBuildingEffects()
        {
            if (buildingData.happinessBonus > 0)
            {
                var happinessService = ServiceLocator.TryGet<IHappinessService>();
                happinessService?.RemoveBuildingBonus(buildingData.happinessBonus, buildingData.buildingName);
            }

            // Remove other effects if needed
            // if (buildingData.housingCapacity > 0)
            // {
            //     var populationService = ServiceLocator.TryGet<IPopulationService>();
            //     populationService?.RemoveHousing(buildingData.housingCapacity);
            // }
        }

        #region Public API

        /// <summary>
        /// Destroy the building (player demolishes it, or it's destroyed in combat).
        /// </summary>
        public void Demolish()
        {
            Destroy(gameObject);
        }

        /// <summary>
        /// Take damage (for destructible buildings).
        /// </summary>
        public void TakeDamage(float damage)
        {
            // Implement health system for buildings if needed
            // For now, just log
            Debug.Log($"{buildingData.buildingName} took {damage} damage");

            // TODO: Add building health system
            // if (health <= 0) Demolish();
        }

        /// <summary>
        /// Get readable info about this building.
        /// </summary>
        public string GetInfo()
        {
            string info = $"{buildingData.buildingName} ({buildingData.buildingType})\n";
            info += $"State: {currentState}\n";

            if (currentState == BuildingState.Constructing)
            {
                info += $"Progress: {Mathf.RoundToInt(ConstructionProgress * 100)}%\n";
            }

            return info;
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Complete Construction Instantly")]
        private void DebugCompleteConstruction()
        {
            if (currentState == BuildingState.Constructing)
            {
                constructionProgress = buildingData.buildTime;
                CompleteConstruction();
            }
        }

        [ContextMenu("Demolish Building")]
        private void DebugDemolish()
        {
            Demolish();
        }

        #endregion
    }

    public enum BuildingState
    {
        Placing,
        Constructing,
        Built,
        Damaged
    }
}