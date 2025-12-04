using RTS.Buildings;
using RTS.Core.Services;
using RTS.Core.Utilities;
using RTS.Core.Events;
using RTS.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace RTS.UI
{
    /// <summary>
    /// Enhanced Building Button with icon-only display and tooltip on hover.
    /// Shows only the building icon - all info displayed in tooltip.
    /// </summary>
    public class BuildingButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI nameText; // Hidden - kept for backwards compatibility
        [SerializeField] private TextMeshProUGUI costText; // Hidden - kept for backwards compatibility
        [SerializeField] private TextMeshProUGUI hotkeyText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Button button;

        [Header("Resource Cost Display")]
        [SerializeField] private Transform costContainer; // Hidden - kept for backwards compatibility
        [SerializeField] private GameObject resourceCostPrefab; // Kept for backwards compatibility

        [Header("Tooltip")]
        [SerializeField] private UniversalTooltip tooltip; // Reference to tooltip component

        [Header("Visual Settings")]
        [SerializeField] private Color affordableColor = new Color(0.2f, 0.8f, 0.2f, 1f); // Green
        [SerializeField] private Color unaffordableColor = new Color(0.8f, 0.2f, 0.2f, 1f); // Red
        [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.5f, 1f); // Yellow
        [SerializeField] private Color selectedColor = new Color(0.2f, 0.5f, 1f, 1f); // Blue
        [SerializeField] private Color pressedColor = new Color(0.2f, 0.5f, 1f, 1f); // Blue

        [Header("Display Settings")]
        [SerializeField] private bool showIconOnly = true; // NEW: Show only icon, hide text
        [SerializeField] private bool showTooltipOnHover = true; // NEW: Show tooltip on hover

        [Header("State Management")]
        [SerializeField] private bool maintainColorState = true; // Keep colors permanent

        private BuildingDataSO buildingData;
        private int buildingIndex;
        private BuildingHUD parentHUD;
        private IResourcesService resourceService;

        // State tracking
        private Color currentColor;
        private bool isAffordable;
        private bool isSelected;
        private bool isHovered;

        public void Initialize(BuildingDataSO data, int index, BuildingHUD hud, UniversalTooltip tooltipReference = null)
        {
            buildingData = data;
            buildingIndex = index;
            parentHUD = hud;

            // Set tooltip reference if provided
            if (tooltipReference != null)
            {
                tooltip = tooltipReference;
            }

            // Get resource service from ServiceLocator
            resourceService = ServiceLocator.TryGet<IResourcesService>();

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

            // Subscribe to resource changes
            EventBus.Subscribe<ResourcesChangedEvent>(OnResourcesChanged);

            // Initial affordability check
            UpdateState(resourceService);
        }

        /// <summary>
        /// Set the tooltip reference (can be called after initialization)
        /// </summary>
        public void SetTooltip(UniversalTooltip tooltipReference)
        {
            tooltip = tooltipReference;
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<ResourcesChangedEvent>(OnResourcesChanged);
        }

        private void OnResourcesChanged(ResourcesChangedEvent evt)
        {
            // Only update when resources actually change (99% more efficient than Update loop)
            UpdateState(resourceService);
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

            // UPDATE COLOR PERMANENTLY (only if changed)
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

            // Icon-only mode: Hide text elements
            if (showIconOnly)
            {
                if (nameText != null) nameText.gameObject.SetActive(false);
                if (costText != null) costText.gameObject.SetActive(false);
                if (costContainer != null) costContainer.gameObject.SetActive(false);
            }
            else
            {
                // Legacy mode: Show all elements
                if (nameText != null)
                {
                    nameText.gameObject.SetActive(true);
                    nameText.text = buildingData.buildingName;
                }

                // Update cost display
                if (costContainer != null && resourceCostPrefab != null)
                {
                    costContainer.gameObject.SetActive(true);
                    UpdateCostDisplayWithIcons();
                }
                else if (costText != null)
                {
                    costText.gameObject.SetActive(true);
                    // Fallback to simple text display using utility
                    costText.text = ResourceDisplayUtility.FormatCosts(buildingData.GetCosts());
                }
            }

            // Update building icon (always visible)
            if (iconImage != null && buildingData.icon != null)
            {
                iconImage.sprite = buildingData.icon;
                iconImage.enabled = true;
            }

            // Update hotkey hint (always visible if present)
            if (hotkeyText != null)
            {
                hotkeyText.text = $"[{buildingIndex + 1}]";
            }
        }

        /// <summary>
        /// NEW: Display costs with resource icons from ResourceUI
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

                // Use centralized resource colors from utility
                if (iconImg != null)
                {
                    // Use color-coded squares based on resource type
                    iconImg.color = ResourceDisplayUtility.GetResourceColor(cost.Key);
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

                string icon = ResourceDisplayUtility.GetResourceEmoji(cost.Key);
                string colorTag = canAffordThis ? "<color=white>" : "<color=red>";
                costStrings.Add($"{icon} {colorTag}{required}</color>");
            }

            costText.text = string.Join(" ", costStrings);
        }

        /// <summary>
        /// PERMANENT COLOR: Updates and maintains color state
        /// </summary>
        private void UpdateColor(Color newColor)
        {
            currentColor = newColor;

            if (backgroundImage != null)
            {
                backgroundImage.color = currentColor;
            }
        }

        #region Mouse Hover Effects (Unity Event System)

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;

            // Show tooltip
            if (showTooltipOnHover && tooltip != null && buildingData != null)
            {
                var tooltipData = TooltipData.FromBuilding(buildingData);
                tooltip.Show(tooltipData);
            }

            // Visual feedback
            if (backgroundImage != null && button != null && button.interactable)
            {
                if (!maintainColorState)
                {
                    // Old behavior: temporary highlight
                    backgroundImage.color = highlightColor;
                }
                else
                {
                    // NEW: Brighten current color instead of replacing
                    Color brightened = currentColor * 1.2f;
                    brightened.a = currentColor.a;
                    backgroundImage.color = brightened;
                }
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;

            // Hide tooltip
            if (showTooltipOnHover && tooltip != null)
            {
                tooltip.Hide();
            }

            // Visual feedback
            if (!maintainColorState)
            {
                // OLD BEHAVIOR: Reset to affordable/unaffordable
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
                // NEW BEHAVIOR: Return to current state color
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