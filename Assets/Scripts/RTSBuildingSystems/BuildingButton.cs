using RTS.Buildings;
using RTS.Core.Services;
using RTS.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace RTS.UI
{
    public class BuildingButton : MonoBehaviour
    {
        [Header("UI Components (Auto-found)")]
        private TextMeshProUGUI nameText;
        private TextMeshProUGUI costText;
        private TextMeshProUGUI hotkeyText;
        private Image iconImage;
        private Image backgroundImage;
        private Button button;

        [Header("Visual Settings")]
        [SerializeField] private Color affordableColor = Color.white;
        [SerializeField] private Color unaffordableColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.5f, 1f);

        private BuildingDataSO buildingData;
        private int buildingIndex;
        private BuildingHUD parentHUD;

        public void Initialize(BuildingDataSO data, int index, BuildingHUD hud)
        {
            buildingData = data;
            buildingIndex = index;
            parentHUD = hud;

            // Find UI components
            FindUIComponents();

            // Set initial data
            UpdateDisplay();
        }

        private void FindUIComponents()
        {
            // Try to find components by common names
            nameText = transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            costText = transform.Find("CostText")?.GetComponent<TextMeshProUGUI>();
            hotkeyText = transform.Find("HotkeyText")?.GetComponent<TextMeshProUGUI>();
            iconImage = transform.Find("Icon")?.GetComponent<Image>();
            backgroundImage = GetComponent<Image>();
            button = GetComponent<Button>();

            // If not found by name, try getting from children
            if (nameText == null) nameText = GetComponentInChildren<TextMeshProUGUI>();
        }

        public void UpdateState(IResourceService resourceService)
        {
            if (buildingData == null || resourceService == null) return;

            var costs = buildingData.GetCosts();
            bool canAfford = resourceService.CanAfford(costs);

            // Update button interactability
            if (button != null)
            {
                button.interactable = canAfford;
            }

            // Update visual feedback
            if (backgroundImage != null)
            {
                backgroundImage.color = canAfford ? affordableColor : unaffordableColor;
            }

            // Update cost text color
            if (costText != null)
            {
                costText.color = canAfford ? Color.white : Color.red;
            }
        }

        private void UpdateDisplay()
        {
            if (buildingData == null) return;

            // Update name
            if (nameText != null)
            {
                nameText.text = buildingData.buildingName;
            }

            // Update cost
            if (costText != null)
            {
                costText.text = GetCostString();
            }

            // Update hotkey hint
            if (hotkeyText != null && parentHUD != null)
            {
                // You could show the hotkey here if you track it
                hotkeyText.text = $"[{buildingIndex + 1}]";
            }

            // Update icon (if you have icons)
            if (iconImage != null)
            {
                // Set building icon sprite here if you have one
                // iconImage.sprite = buildingData.icon;
            }
        }

        private string GetCostString()
        {
            var costs = buildingData.GetCosts();
            var costStrings = new List<string>();

            foreach (var cost in costs)
            {
                costStrings.Add($"{GetResourceIcon(cost.Key)}{cost.Value}");
            }

            return string.Join(" ", costStrings);
        }

        private string GetResourceIcon(ResourceType type)
        {
            // You can use text icons or actual image icons
            return type switch
            {
                ResourceType.Wood => "🌲",
                ResourceType.Food => "🌾",
                ResourceType.Gold => "💰",
                ResourceType.Stone => "🪨",
                _ => ""
            };
        }

        #region Mouse Hover Effects (Optional)

        public void OnPointerEnter()
        {
            if (backgroundImage != null && button != null && button.interactable)
            {
                backgroundImage.color = highlightColor;
            }
        }

        public void OnPointerExit()
        {
            // Reset to affordable/unaffordable color
            if (backgroundImage != null)
            {
                var resourceService = ServiceLocator.TryGet<IResourceService>();
                if (resourceService != null)
                {
                    var costs = buildingData.GetCosts();
                    bool canAfford = resourceService.CanAfford(costs);
                    backgroundImage.color = canAfford ? affordableColor : unaffordableColor;
                }
            }
        }

        #endregion
    }
}