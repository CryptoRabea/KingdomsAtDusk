using RTS.Buildings;
using RTS.Core.Services;
using RTS.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RTS.UI
{
    /// <summary>
    /// Enhanced Building Button with resource icons and permanent color states.
    /// Gets resource icons from ResourceUI configuration.
    /// </summary>
    public class BuildingButton : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI hotkeyText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Button button;

        [Header("Resource Cost Display")]
        [SerializeField] private Transform costContainer; // Container for individual resource cost entries
        [SerializeField] private GameObject resourceCostPrefab; // Prefab with Icon + Text

        [Header("Visual Settings")]
        [SerializeField] private Color affordableColor = new Color(0.2f, 0.8f, 0.2f, 1f); // Green
        [SerializeField] private Color unaffordableColor = new Color(0.8f, 0.2f, 0.2f, 1f); // Red
        [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.5f, 1f); // Yellow
        [SerializeField] private Color selectedColor = new Color(0.2f, 0.5f, 1f, 1f); // Blue
        [SerializeField] private Color pressedColor = new Color(0.2f, 0.5f, 1f, 1f); // Blue

        [Header("State Management")]
        [SerializeField] private bool maintainColorState = true; // ✅ NEW: Keep colors permanent

        private BuildingDataSO buildingData;
        private int buildingIndex;
        private BuildingHUD parentHUD;
        private ResourceUI resourceUI;

        // State tracking
        private Color currentColor;
        private bool isAffordable;
        private bool isSelected;
        private bool isHovered;

        public void Initialize(BuildingDataSO data, int index, BuildingHUD hud)
        {
            buildingData = data;
            buildingIndex = index;
            parentHUD = hud;

            // Find ResourceUI in scene
            resourceUI = Object.FindAnyObjectByType<ResourceUI>();
            if (resourceUI == null)
            {
                Debug.LogWarning("BuildingButton: ResourceUI not found. Resource icons may not work.");
            }

            // Auto-find components if not assigned
            if (nameText == null) nameText = transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            if (costText == null) costText = transform.Find("CostText")?.GetComponent<TextMeshProUGUI>();
            if (hotkeyText == null) hotkeyText = transform.Find("HotkeyText")?.GetComponent<TextMeshProUGUI>();
            if (iconImage == null) iconImage = transform.Find("Icon")?.GetComponent<Image>();
            if (backgroundImage == null) backgroundImage = GetComponent<Image>();
            if (button == null) button = GetComponent<Button>();
            if (costContainer == null) costContainer = transform.Find("CostContainer");

            // Set initial data
            UpdateDisplay();
        }

        public void UpdateState(IResourcesService resourceService)
        {
            if (buildingData == null || resourceService == null) return;

            var costs = buildingData.GetCosts();
            bool canAfford = resourceService.CanAfford(costs);

            // Track state change
            bool affordabilityChanged = (canAfford != isAffordable);
            isAffordable = canAfford;

            // Update button interactability
            if (button != null)
            {
                button.interactable = canAfford;
            }

            // ✅ UPDATE COLOR PERMANENTLY (only if changed)
            if (affordabilityChanged && !isSelected && !isHovered)
            {
                UpdateColor(canAfford ? affordableColor : unaffordableColor);
            }

            // Update cost display with current amounts
            UpdateCostDisplay(resourceService);
        }

        private void UpdateDisplay()
        {
            if (buildingData == null) return;

            // Update name
            if (nameText != null)
            {
                nameText.text = buildingData.buildingName;
            }

            // Update building icon
            if (iconImage != null && buildingData.icon != null)
            {
                iconImage.sprite = buildingData.icon;
                iconImage.enabled = true;
            }

            // Update hotkey hint
            if (hotkeyText != null)
            {
                hotkeyText.text = $"[{buildingIndex + 1}]";
            }

            // Update cost display
            if (costContainer != null && resourceCostPrefab != null)
            {
                UpdateCostDisplayWithIcons();
            }
            else if (costText != null)
            {
                // Fallback to simple text display
                costText.text = GetCostString();
            }
        }

        /// <summary>
        /// ✅ NEW: Display costs with resource icons from ResourceUI
        /// </summary>
        private void UpdateCostDisplayWithIcons()
        {
            // Clear existing cost displays
            foreach (Transform child in costContainer)
            {
                Destroy(child.gameObject);
            }

            var costs = buildingData.GetCosts();

            foreach (var cost in costs)
            {
                // Create cost entry
                GameObject costEntry = Instantiate(resourceCostPrefab, costContainer);

                // Find components in prefab
                Image iconImg = costEntry.transform.Find("Icon")?.GetComponent<Image>();
                TextMeshProUGUI amountTxt = costEntry.transform.Find("Amount")?.GetComponent<TextMeshProUGUI>();

                // ✅ GET ICON FROM RESOURCEUI
                if (iconImg != null && resourceUI != null)
                {
                    var resourceDisplay = resourceUI.GetDisplay(cost.Key);
                    if (resourceDisplay != null && resourceDisplay.iconImage != null)
                    {
                        iconImg.sprite = resourceDisplay.iconImage.sprite;
                    }
                    else
                    {
                        // Fallback: Use color-coded squares
                        iconImg.color = GetResourceColor(cost.Key);
                    }
                }

                // Set amount text
                if (amountTxt != null)
                {
                    amountTxt.text = cost.Value.ToString();
                }
            }
        }

        /// <summary>
        /// Update cost display with current resource availability
        /// </summary>
        private void UpdateCostDisplay(IResourcesService resourceService)
        {
            if (costContainer == null || buildingData == null) return;

            var costs = buildingData.GetCosts();
            int childIndex = 0;

            foreach (var cost in costs)
            {
                if (childIndex >= costContainer.childCount) break;

                Transform costEntry = costContainer.GetChild(childIndex);
                TextMeshProUGUI amountTxt = costEntry.Find("Amount")?.GetComponent<TextMeshProUGUI>();

                if (amountTxt != null && resourceService != null)
                {
                    int current = resourceService.GetResource(cost.Key);
                    int required = cost.Value;
                    bool canAffordThis = current >= required;

                    // Color code the amount: green if affordable, red if not
                    amountTxt.color = canAffordThis ? Color.white : Color.red;

                    // Optional: Show current/required
                    // amountTxt.text = $"{required}"; // Just required
                    // amountTxt.text = $"{current}/{required}"; // Current/Required
                }

                childIndex++;
            }

            // Fallback for simple text display
            if (costText != null)
            {
                UpdateSimpleCostText(resourceService);
            }
        }

        /// <summary>
        /// Simple text-based cost display (fallback)
        /// </summary>
        private void UpdateSimpleCostText(IResourcesService resourceService)
        {
            var costs = buildingData.GetCosts();
            var costStrings = new List<string>();

            foreach (var cost in costs)
            {
                int current = resourceService.GetResource(cost.Key);
                int required = cost.Value;
                bool canAffordThis = current >= required;

                string icon = GetResourceIconText(cost.Key);
                string colorTag = canAffordThis ? "<color=white>" : "<color=red>";
                costStrings.Add($"{icon}{colorTag}{required}</color>");
            }

            costText.text = string.Join(" ", costStrings);
        }

        private string GetCostString()
        {
            var costs = buildingData.GetCosts();
            var costStrings = new List<string>();

            foreach (var cost in costs)
            {
                costStrings.Add($"{GetResourceIconText(cost.Key)}{cost.Value}");
            }

            return string.Join(" ", costStrings);
        }

        /// <summary>
        /// Get resource icon as emoji/text (fallback if no sprite)
        /// </summary>
        private string GetResourceIconText(ResourceType type)
        {
            return type switch
            {
                ResourceType.Wood => "🌲",
                ResourceType.Food => "🌾",
                ResourceType.Gold => "💰",
                ResourceType.Stone => "🪨",
                _ => ""
            };
        }

        /// <summary>
        /// Get fallback color for resource type
        /// </summary>
        private Color GetResourceColor(ResourceType type)
        {
            return type switch
            {
                ResourceType.Wood => new Color(0.55f, 0.27f, 0.07f), // Brown
                ResourceType.Food => new Color(0.9f, 0.8f, 0.2f),    // Yellow
                ResourceType.Gold => new Color(1f, 0.84f, 0f),       // Gold
                ResourceType.Stone => new Color(0.5f, 0.5f, 0.5f),   // Gray
                _ => Color.white
            };
        }

        /// <summary>
        /// ✅ PERMANENT COLOR: Updates and maintains color state
        /// </summary>
        private void UpdateColor(Color newColor)
        {
            currentColor = newColor;

            if (backgroundImage != null)
            {
                backgroundImage.color = currentColor;
            }
        }

        #region Mouse Hover Effects

        public void OnPointerEnter()
        {
            isHovered = true;

            if (backgroundImage != null && button != null && button.interactable)
            {
                if (!maintainColorState)
                {
                    // Old behavior: temporary highlight
                    backgroundImage.color = highlightColor;
                }
                else
                {
                    // ✅ NEW: Brighten current color instead of replacing
                    Color brightened = currentColor * 1.2f;
                    brightened.a = currentColor.a;
                    backgroundImage.color = brightened;
                }
            }
        }

        public void OnPointerExit()
        {
            isHovered = false;

            if (!maintainColorState)
            {
                // ❌ OLD BEHAVIOR: Reset to affordable/unaffordable
                if (backgroundImage != null)
                {
                    var resourceService = ServiceLocator.TryGet<IResourcesService>();
                    if (resourceService != null)
                    {
                        var costs = buildingData.GetCosts();
                        bool canAfford = resourceService.CanAfford(costs);
                        backgroundImage.color = canAfford ? affordableColor : unaffordableColor;
                    }
                }
            }
            else
            {
                // ✅ NEW BEHAVIOR: Return to current state color
                if (backgroundImage != null)
                {
                    backgroundImage.color = currentColor;
                }
            }
        }

        public void OnClick()
        {
            // Mark as selected
            isSelected = true;
            UpdateColor(selectedColor);
        }

        public void Deselect()
        {
            isSelected = false;
            // Return to affordable/unaffordable color
            UpdateColor(isAffordable ? affordableColor : unaffordableColor);
        }

        #endregion

        #region Public API

        public BuildingDataSO BuildingData => buildingData;
        public int BuildingIndex => buildingIndex;
        public bool IsAffordable => isAffordable;
        public bool IsSelected => isSelected;

        #endregion
    }
}