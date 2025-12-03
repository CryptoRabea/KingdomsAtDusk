using UnityEngine;
using System.Collections.Generic;
using RTS.Core.Services;
using RTS.Units;

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
        Special         // Unique buildings
    }

    /// <summary>
    /// Configuration for a trainable unit.
    /// </summary>
    [System.Serializable]
    public class TrainableUnitData
    {
        public UnitConfigSO unitConfig;
        public int woodCost;
        public int foodCost;
        public int goldCost;
        public int stoneCost;
        public float trainingTime = 5f;

        /// <summary>
        /// Get costs as a dictionary.
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
        public float resourceGenerationRate = 5f; // Alias for generationInterval (backwards compatible)

        [Header("Construction")]
        public float constructionTime = 5f;
        public float buildTime = 5f; // Alias for constructionTime (for backwards compatibility)
        public GameObject buildingPrefab;

        [Header("Population/Housing (Optional)")]
        [Tooltip("Does this building provide housing?")]
        public bool providesHousing = false;
        public int housingCapacity = 0;

        [Header("Additional Properties (Optional)")]
        public int maxHealth = 100;
        public float repairCostMultiplier = 0.5f; // 50% of build cost to repair
        public float visionRevealRange;

        [Header("Combat Stats (Optional)")]
        [Tooltip("Defence value - reduces incoming damage")]
        public int defence = 0;

        [Tooltip("Attack damage dealt to enemies")]
        public int attackDamage = 0;

        [Tooltip("Maximum attack range")]
        public float attackRange = 0f;

        [Tooltip("Attacks per second")]
        public float attackSpeed = 1f;

        [Header("Tooltip Display Options")]
        [Tooltip("Show HP in tooltip?")]
        public bool showHP = false;

        [Tooltip("Show Defence in tooltip?")]
        public bool showDefence = false;

        [Tooltip("Show Attack Damage in tooltip?")]
        public bool showAttackDamage = false;

        [Tooltip("Show Attack Range in tooltip?")]
        public bool showAttackRange = false;

        [Tooltip("Show Attack Speed in tooltip?")]
        public bool showAttackSpeed = false;

        [Header("Unit Training (Optional)")]
        [Tooltip("Can this building train units?")]
        public bool canTrainUnits = false;
        [Tooltip("Units that can be trained from this building")]
        public List<TrainableUnitData> trainableUnits = new List<TrainableUnitData>();

        private void OnValidate()
        {
            // Keep buildTime in sync with constructionTime
            buildTime = constructionTime;

            // Keep resourceGenerationRate in sync with generationInterval
            resourceGenerationRate = generationInterval;
        }

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
        /// Get total cost for display purposes.
        /// </summary>
        public int GetTotalCost()
        {
            return woodCost + foodCost + goldCost + stoneCost;
        }

        /// <summary>
        /// Get a formatted cost string for UI.
        /// </summary>
        public string GetCostString()
        {
            var costs = new List<string>();

            if (woodCost > 0) costs.Add($"Wood: {woodCost}");
            if (foodCost > 0) costs.Add($"Food: {foodCost}");
            if (goldCost > 0) costs.Add($"Gold: {goldCost}");
            if (stoneCost > 0) costs.Add($"Stone: {stoneCost}");

            return string.Join(", ", costs);
        }

        /// <summary>
        /// Get repair costs (percentage of original build cost).
        /// </summary>
        public Dictionary<ResourceType, int> GetRepairCosts()
        {
            var costs = new Dictionary<ResourceType, int>();

            if (woodCost > 0) costs[ResourceType.Wood] = Mathf.CeilToInt(woodCost * repairCostMultiplier);
            if (foodCost > 0) costs[ResourceType.Food] = Mathf.CeilToInt(foodCost * repairCostMultiplier);
            if (goldCost > 0) costs[ResourceType.Gold] = Mathf.CeilToInt(goldCost * repairCostMultiplier);
            if (stoneCost > 0) costs[ResourceType.Stone] = Mathf.CeilToInt(stoneCost * repairCostMultiplier);

            return costs;
        }

        /// <summary>
        /// Get a formatted description including all relevant info.
        /// </summary>
        public virtual string GetFullDescription()
        {
            var details = new List<string>();

            details.Add(description);
            details.Add($"\nCost: {GetCostString()}");
            details.Add($"Build Time: {constructionTime}s");

            if (happinessBonus != 0)
            {
                string sign = happinessBonus > 0 ? "+" : "";
                details.Add($"Happiness: {sign}{happinessBonus}");
            }

            if (providesHousing && housingCapacity > 0)
            {
                details.Add($"Housing: +{housingCapacity} population");
            }

            if (generatesResources)
            {
                details.Add($"Generates: {resourceAmount} {resourceType} every {generationInterval}s");
            }

            return string.Join("\n", details);
        }
    }
}