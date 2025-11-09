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
            BuildingType.House => "Provides housing for citizens",
            BuildingType.Farm => "Generates food over time",
            BuildingType.Barracks => "Trains military units",
            BuildingType.Tower => "Defensive structure",
            BuildingType.Wall => "Protects your base",
            _ => "Building"
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

        stats.Add($"Build Time: {data.buildTime}s");

        if (data.happinessBonus > 0)
            stats.Add($"Happiness: +{data.happinessBonus}");

        if (data.housingCapacity > 0)
            stats.Add($"Housing: +{data.housingCapacity}");

        if (data.resourceGenerationRate > 0)
            stats.Add($"Production: +{data.resourceGenerationRate}/s");

        return string.Join("\n", stats);
    }
}
}


