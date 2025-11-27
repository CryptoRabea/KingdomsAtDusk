using System.Collections.Generic;
using UnityEngine;
using TopDownWallBuilding.Core.Services;

namespace TopDownWallBuilding.WallSystems
{
    /// <summary>
    /// ScriptableObject holding data for wall/building prefabs.
    /// This allows data-driven configuration for different wall types.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWallData", menuName = "Top-Down Wall Building/Wall Data")]
    public class BuildingDataSO : ScriptableObject
    {
        [Header("Basic Info")]
        public string buildingName = "Wall";
        public string description = "A defensive wall segment";

        [Header("Prefab")]
        public GameObject buildingPrefab;

        [Header("Resource Costs")]
        public int woodCost = 10;
        public int stoneCost = 5;
        public int foodCost = 0;
        public int goldCost = 0;

        [Header("Construction")]
        public float constructionTime = 2f;

        /// <summary>
        /// Get resource costs as a dictionary for easy spending.
        /// </summary>
        public Dictionary<ResourceType, int> GetCosts()
        {
            var costs = new Dictionary<ResourceType, int>();

            if (woodCost > 0) costs[ResourceType.Wood] = woodCost;
            if (stoneCost > 0) costs[ResourceType.Stone] = stoneCost;
            if (foodCost > 0) costs[ResourceType.Food] = foodCost;
            if (goldCost > 0) costs[ResourceType.Gold] = goldCost;

            return costs;
        }

        /// <summary>
        /// Get formatted cost string for UI display.
        /// </summary>
        public string GetCostString()
        {
            List<string> costParts = new List<string>();

            if (woodCost > 0) costParts.Add($"Wood: {woodCost}");
            if (stoneCost > 0) costParts.Add($"Stone: {stoneCost}");
            if (foodCost > 0) costParts.Add($"Food: {foodCost}");
            if (goldCost > 0) costParts.Add($"Gold: {goldCost}");

            return costParts.Count > 0 ? string.Join(", ", costParts) : "Free";
        }
    }
}
