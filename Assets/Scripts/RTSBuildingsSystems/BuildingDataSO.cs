using RTS.Core.Services;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Security.AccessControl;
using UnityEngine;

namespace RTS.Buildings
{
    /// <summary>
    /// Building type enumeration for categorizing buildings.
    /// </summary>
    public enum BuildingType
    {
        Residential,    // Housing/population
        Production,     // Resource generation
        Military,       // Barracks, towers, walls
        Economic,       // Markets, banks
        Religious,      // Temples, churches
        Cultural,       // Libraries, monuments
        Defensive,      // Walls, towers
        Special,      // Unique buildings
        Wall,
        Tower,
        Barracks,
        Farm,
        House
    }

    /// <summary>
    /// ScriptableObject containing building configuration data.
    /// Create via: Right-click in Project > Create > RTS > BuildingData
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingData", menuName = "RTS/BuildingData")]
    public class BuildingDataSO : ScriptableObject
    {
        [Header("Identity")]
        public string buildingName = "Building";
        public BuildingType buildingType = BuildingType.Residential;
        [TextArea(2, 4)]
        public string description = "A building";
        public Sprite icon;

        [Header("Costs")]
        public int woodCost = 0;
        public int foodCost = 0;
        public int goldCost = 0;
        public int stoneCost = 0;

        [Header("Effects")]
        [Tooltip("Happiness bonus/penalty this building provides")]
        public float happinessBonus = 0f;

        [Tooltip("Does this building generate resources over time?")]
        public bool generatesResources = false;

        [Header("Resource Generation (if applicable)")]
        public ResourceType resourceType = ResourceType.Wood;
        public int resourceAmount = 10;
        public float generationInterval = 5f; // seconds

        [Header("Construction")]
        public float constructionTime = 5f;
        public GameObject buildingPrefab;

        [Header("Population/Housing (Optional)")]
        [Tooltip("Does this building provide housing?")]
        public bool providesHousing = false;
        public int housingCapacity = 0;

        [Header("Additional Properties (Optional)")]
        public int maxHealth = 100;
        public float repairCostMultiplier = 0.5f; // 50% of build cost to repair

        // ❌ REMOVED OnValidate() - IT WAS CAUSING 8GB MEMORY LEAK!
        // OnValidate() gets called thousands of times during asset import,
        // causing massive memory leaks when combined with multiple BuildingData assets.

        // Instead, we removed the duplicate fields (buildTime, resourceGenerationRate)
        // and now only use constructionTime and generationInterval.

        /// <summary>
        /// Get costs as a dictionary for the new resource system.
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
        /// Check if we have enough resources to build this.
        /// </summary>
        public bool CanAfford(IResourcesService resourceService)
        {
            if (resourceService == null) return false;
            return resourceService.CanAfford(GetCosts());
        }

        /// <summary>
        /// Get a formatted cost string for UI display.
        /// </summary>
        public string GetCostString()
        {
            var costs = GetCosts();
            if (costs.Count == 0) return "Free";

            var parts = new List<string>();
            foreach (var cost in costs)
            {
                parts.Add($"{cost.Key}: {cost.Value}");
            }

            return string.Join(", ", parts);
        }
    }
}