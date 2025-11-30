using RTS.Buildings;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace RTS.UI
{
    public class BuildingTooltip : MonoBehaviour
    {
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private TextMeshProUGUI tooltipTitle;
        [SerializeField] private TextMeshProUGUI tooltipDescription;
        [SerializeField] private TextMeshProUGUI tooltipCosts;
        [SerializeField] private TextMeshProUGUI tooltipStats;

        public void ShowTooltip(BuildingDataSO buildingData, Vector3 position)
        {
            if (tooltipPanel == null || buildingData == null) return;

            tooltipPanel.SetActive(true);
            tooltipPanel.transform.position = position;

            if (tooltipTitle != null)
                tooltipTitle.text = buildingData.buildingName;

            if (tooltipDescription != null)
                tooltipDescription.text = GetBuildingDescription(buildingData);

            if (tooltipCosts != null)
                tooltipCosts.text = GetDetailedCosts(buildingData);

            if (tooltipStats != null)
                tooltipStats.text = GetBuildingStats(buildingData);
        }

        public void HideTooltip()
        {
            if (tooltipPanel != null)
                tooltipPanel.SetActive(false);
        }

        private string GetBuildingDescription(BuildingDataSO data)
        {
            return data.buildingType switch
            {
                BuildingType.Residential =>
                    $"Provides housing for {data.housingCapacity} citizens. " +
                    $"Increases happiness by {data.happinessBonus}.",

                BuildingType.Production when data.generatesResources =>
                    $"Generates {data.resourceAmount} {data.resourceType} " +
                    $"every {data.resourceGenerationRate} seconds.",

                BuildingType.Production =>
                    "Production building for gathering resources.",

                BuildingType.Military =>
                    "Military building for training units and defense.",

                BuildingType.Economic =>
                    "Economic building that boosts trade and gold production.",

                BuildingType.Religious =>
                    $"Religious building. Increases happiness by {data.happinessBonus}.",

                BuildingType.Cultural =>
                    "Cultural building that provides knowledge and culture points.",

                BuildingType.Defensive =>
                    $"Defensive structure with {data.maxHealth} HP. " +
                    "Protects your base from enemy attacks.",

                BuildingType.Special =>
                    "Unique building with special game-changing effects.",

                _ => data.description ?? "Building"
            };
        }
        private string GetDetailedCosts(BuildingDataSO data)
        {
            var costs = data.GetCosts();
            var lines = new List<string>();

            foreach (var cost in costs)
            {
                lines.Add($"{cost.Key}: {cost.Value}");
            }

            return string.Join("\n", lines);
        }

        private string GetBuildingStats(BuildingDataSO data)
        {
            var stats = new List<string>();

            // [OK] FIXED: Use constructionTime instead of buildTime
            stats.Add($"Build Time: {data.constructionTime}s");

            if (data.happinessBonus > 0)
                stats.Add($"Happiness: +{data.happinessBonus}");

            if (data.housingCapacity > 0)
                stats.Add($"Housing: +{data.housingCapacity}");

            // [OK] FIXED: Use generationInterval instead of resourceGenerationRate
            if (data.generationInterval > 0)
                stats.Add($"Production: +{data.resourceAmount} every {data.generationInterval}s");

            return string.Join("\n", stats);
        }
    }
}