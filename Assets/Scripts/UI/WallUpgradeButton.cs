using RTS.Buildings;
using RTS.Core.Events;
using RTS.Core.Services;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RTS.UI
{
    /// <summary>
    /// Button component for upgrading a wall to a tower or gate.
    /// </summary>
    public class WallUpgradeButton : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private Image icon;
        [SerializeField] private GameObject affordableIndicator;
        [SerializeField] private GameObject unaffordableIndicator;

        private BuildingDataSO buildingData;
        private GameObject targetWall;
        private IResourcesService resourceService;
        private Dictionary<ResourceType, int> costs;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (button != null)
            {
                button.onClick.AddListener(OnUpgradeClicked);
            }
        }

        public void Initialize(BuildingDataSO data, GameObject wall, IResourcesService resources)
        {
            buildingData = data;
            targetWall = wall;
            resourceService = resources;

            if (buildingData != null)
            {
                costs = buildingData.GetCosts();
                UpdateDisplay();
            }
        }

        private void Update()
        {
            // Update affordability indicator
            if (resourceService != null && costs != null)
            {
                bool canAfford = resourceService.CanAfford(costs);
                UpdateAffordabilityIndicator(canAfford);
            }
        }

        private void UpdateDisplay()
        {
            if (nameText != null)
            {
                nameText.text = buildingData.buildingName;
            }

            if (costText != null)
            {
                costText.text = GetCostString();
            }

            if (icon != null && buildingData.icon != null)
            {
                icon.sprite = buildingData.icon;
                icon.enabled = true;
            }
            else if (icon != null)
            {
                icon.enabled = false;
            }

            UpdateAffordabilityIndicator(resourceService != null && resourceService.CanAfford(costs));
        }

        private string GetCostString()
        {
            if (costs == null || costs.Count == 0)
            {
                return "Free";
            }

            List<string> costParts = new List<string>();
            foreach (var cost in costs)
            {
                if (cost.Value > 0)
                {
                    costParts.Add($"{cost.Key}: {cost.Value}");
                }
            }

            return string.Join(", ", costParts);
        }

        private void UpdateAffordabilityIndicator(bool canAfford)
        {
            if (button != null)
            {
                button.interactable = canAfford;
            }

            if (affordableIndicator != null)
            {
                affordableIndicator.SetActive(canAfford);
            }

            if (unaffordableIndicator != null)
            {
                unaffordableIndicator.SetActive(!canAfford);
            }
        }

        private void OnUpgradeClicked()
        {
            if (targetWall == null || buildingData == null)
            {
                return;
            }

            if (resourceService == null)
            {
                return;
            }

            // Check if can afford
            if (!resourceService.CanAfford(costs))
            {
                EventBus.Publish(new BuildingPlacementFailedEvent("Not enough resources!"));
                return;
            }

            // Spend resources
            if (!resourceService.SpendResources(costs))
            {
                return;
            }

            // Perform upgrade
            WallUpgradeHelper.UpgradeWallToBuilding(targetWall, buildingData);

            // Deselect the wall (it's been destroyed)
            var selectionManager = Object.FindAnyObjectByType<BuildingSelectionManager>();
            if (selectionManager != null)
            {
                selectionManager.DeselectBuilding();
            }
        }
    }
}
