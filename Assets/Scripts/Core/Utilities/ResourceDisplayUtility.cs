using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using RTS.Core.Services;

namespace RTS.Core.Utilities
{
    /// <summary>
    /// Centralized utility for displaying resource costs and formatting resource text.
    /// This eliminates the duplicate GetCostString() logic that appeared in 5+ different files.
    /// </summary>
    public static class ResourceDisplayUtility
    {
        // Resource emoji/icon mappings
        private static readonly Dictionary<ResourceType, string> ResourceEmojis = new Dictionary<ResourceType, string>
        {
            { ResourceType.Wood, "[W]" },
            { ResourceType.Food, "[F]" },
            { ResourceType.Gold, "[G]" },
            { ResourceType.Stone, "[S]" }
        };

        // Resource color mappings for UI
        private static readonly Dictionary<ResourceType, Color> ResourceColors = new Dictionary<ResourceType, Color>
        {
            { ResourceType.Wood, new Color(0.55f, 0.27f, 0.07f) },  // Brown
            { ResourceType.Food, new Color(0.9f, 0.8f, 0.2f) },     // Yellow
            { ResourceType.Gold, new Color(1f, 0.84f, 0f) },        // Gold
            { ResourceType.Stone, new Color(0.5f, 0.5f, 0.5f) }     // Gray
        };

        #region Cost Formatting

        /// <summary>
        /// Format resource costs as a display string.
        /// Example: "[W] 100  [G] 50  [S] 25"
        /// </summary>
        /// <param name="costs">Dictionary of resource costs</param>
        /// <param name="separator">Separator between resources (default: "  ")</param>
        /// <param name="useEmoji">Whether to include emoji icons</param>
        /// <returns>Formatted cost string</returns>
        public static string FormatCosts(Dictionary<ResourceType, int> costs, string separator = "  ", bool useEmoji = true)
        {
            if (costs == null || costs.Count == 0)
                return "Free";

            var costStrings = new List<string>();

            foreach (var cost in costs.Where(c => c.Value > 0))
            {
                string emoji = useEmoji ? GetResourceEmoji(cost.Key) : "";
                costStrings.Add($"{emoji} {cost.Value}");
            }

            return costStrings.Count > 0 ? string.Join(separator, costStrings) : "Free";
        }

        /// <summary>
        /// Format costs with resource names included.
        /// Example: "Wood: 100, Gold: 50, Stone: 25"
        /// </summary>
        public static string FormatCostsWithNames(Dictionary<ResourceType, int> costs, string separator = ", ")
        {
            if (costs == null || costs.Count == 0)
                return "Free";

            var costStrings = new List<string>();

            foreach (var cost in costs.Where(c => c.Value > 0))
            {
                costStrings.Add($"{cost.Key}: {cost.Value}");
            }

            return costStrings.Count > 0 ? string.Join(separator, costStrings) : "Free";
        }

        /// <summary>
        /// Format costs for rich text with colors.
        /// Example: "<color=#8B4513>[W] 100</color>  <color=#FFD700>[G] 50</color>"
        /// </summary>
        public static string FormatCostsRichText(Dictionary<ResourceType, int> costs, string separator = "  ")
        {
            if (costs == null || costs.Count == 0)
                return "Free";

            var sb = new StringBuilder();
            var costsList = costs.Where(c => c.Value > 0).ToList();

            for (int i = 0; i < costsList.Count; i++)
            {
                var cost = costsList[i];
                Color color = GetResourceColor(cost.Key);
                string colorHex = ColorUtility.ToHtmlStringRGB(color);
                string emoji = GetResourceEmoji(cost.Key);

                sb.Append($"<color=#{colorHex}>{emoji} {cost.Value}</color>");

                if (i < costsList.Count - 1)
                    sb.Append(separator);
            }

            return sb.Length > 0 ? sb.ToString() : "Free";
        }

        #endregion

        #region Resource Icons & Colors

        /// <summary>
        /// Get the emoji icon for a resource type.
        /// </summary>
        public static string GetResourceEmoji(ResourceType type)
        {
            return ResourceEmojis.TryGetValue(type, out string emoji) ? emoji : "[?]";
        }

        /// <summary>
        /// Get the display color for a resource type.
        /// </summary>
        public static Color GetResourceColor(ResourceType type)
        {
            return ResourceColors.TryGetValue(type, out Color color) ? color : Color.white;
        }

        #endregion

        #region Affordability Helpers

        /// <summary>
        /// Check if costs can be afforded and return a formatted string with color indicators.
        /// Green for affordable costs, red for unaffordable.
        /// </summary>
        public static string FormatCostsWithAffordability(Dictionary<ResourceType, int> costs,
            IResourcesService resourceService, string separator = "  ")
        {
            if (costs == null || costs.Count == 0)
                return "Free";

            var sb = new StringBuilder();
            var costsList = costs.Where(c => c.Value > 0).ToList();

            for (int i = 0; i < costsList.Count; i++)
            {
                var cost = costsList[i];
                int currentAmount = resourceService.GetResource(cost.Key);
                bool canAfford = currentAmount >= cost.Value;

                string colorHex = canAfford ? "00FF00" : "FF0000"; // Green or Red
                string emoji = GetResourceEmoji(cost.Key);

                sb.Append($"<color=#{colorHex}>{emoji} {cost.Value}</color>");

                if (i < costsList.Count - 1)
                    sb.Append(separator);
            }

            return sb.Length > 0 ? sb.ToString() : "Free";
        }

        /// <summary>
        /// Get a list of resources that cannot be afforded from the given costs.
        /// </summary>
        public static List<ResourceType> GetUnaffordableResources(Dictionary<ResourceType, int> costs,
            IResourcesService resourceService)
        {
            var unaffordable = new List<ResourceType>();

            if (costs == null || resourceService == null)
                return unaffordable;

            foreach (var cost in costs.Where(c => c.Value > 0))
            {
                if (resourceService.GetResource(cost.Key) < cost.Value)
                {
                    unaffordable.Add(cost.Key);
                }
            }

            return unaffordable;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get total cost value (useful for comparing building costs).
        /// </summary>
        public static int GetTotalCostValue(Dictionary<ResourceType, int> costs)
        {
            return costs?.Sum(c => c.Value) ?? 0;
        }

        /// <summary>
        /// Check if a cost dictionary is empty or has no costs greater than zero.
        /// </summary>
        public static bool IsFreeCost(Dictionary<ResourceType, int> costs)
        {
            return costs == null || costs.Count == 0 || !costs.Any(c => c.Value > 0);
        }

        #endregion
    }
}
