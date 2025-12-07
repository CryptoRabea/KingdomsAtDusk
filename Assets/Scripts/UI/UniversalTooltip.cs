using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using RTS.Core.Services;

namespace RTS.UI
{
    /// <summary>
    /// Universal tooltip system for buildings, units, towers, and walls.
    /// Shows name, costs, description, and optional stats at a fixed position above HUD.
    ///
    /// SETUP INSTRUCTIONS:
    /// 1. Create a Panel GameObject for the tooltip
    /// 2. Add TextMeshProUGUI components for: Title, Description
    /// 3. Create a container for costs (add CostItem prefabs dynamically)
    /// 4. Create a container for stats with TextMeshProUGUI for each stat type
    /// 5. Set resource icons (Wood, Food, Gold, Stone)
    /// 6. Attach this script and assign all references
    /// 7. Set fixed position (default: 0, 200 - above HUD)
    /// 8. Assign this tooltip to BuildingHUD's buildingTooltip field
    /// </summary>
    public class UniversalTooltip : MonoBehaviour
    {
        [Header("Tooltip Panel")]
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private RectTransform tooltipRect;

        [Header("Content")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI descriptionText;

        [Header("Costs Section")]
        [SerializeField] private GameObject costsContainer;
        [SerializeField] private GameObject costItemPrefab; // Prefab with Icon + Text (Icon should be named "Icon", Text should be named "Text")

        [Header("Stats Section")]
        [SerializeField] private GameObject statsContainer;
        [SerializeField] private TextMeshProUGUI constructionTimeText;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private TextMeshProUGUI defenceText;
        [SerializeField] private TextMeshProUGUI attackDamageText;
        [SerializeField] private TextMeshProUGUI attackRangeText;
        [SerializeField] private TextMeshProUGUI attackSpeedText;

        [Header("Resource Icons")]
        [SerializeField] private Sprite woodIcon;
        [SerializeField] private Sprite foodIcon;
        [SerializeField] private Sprite goldIcon;
        [SerializeField] private Sprite stoneIcon;

        [Header("Positioning")]
        [SerializeField] private Vector2 fixedPosition = new Vector2(0, -300); // Position in bottom mid box
        [SerializeField] private bool useFixedPosition = true;

        private List<GameObject> activeCostItems = new List<GameObject>();
        private Canvas canvas;

        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Show tooltip with the given data.
        /// </summary>
        public void Show(TooltipData data)
        {
            if (data == null || tooltipPanel == null) return;

            // Set title and icon
            if (titleText != null)
            {
                titleText.text = data.title;
            }

            if (iconImage != null && data.icon != null)
            {
                iconImage.sprite = data.icon;
                iconImage.gameObject.SetActive(true);
            }
            else if (iconImage != null)
            {
                iconImage.gameObject.SetActive(false);
            }

            // Set description
            if (descriptionText != null)
            {
                descriptionText.text = data.description;
            }

            // Update costs
            UpdateCosts(data.costs);

            // Update stats
            UpdateStats(data);

            // Position tooltip
            if (useFixedPosition && tooltipRect != null)
            {
                tooltipRect.anchoredPosition = fixedPosition;
            }

            // Show panel
            tooltipPanel.SetActive(true);
        }

        /// <summary>
        /// Show tooltip at a specific screen position (for mouse hover).
        /// </summary>
        public void ShowAtPosition(TooltipData data, Vector2 screenPosition)
        {
            Show(data);

            if (!useFixedPosition && tooltipRect != null && canvas != null)
            {
                // Convert screen position to canvas position
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvas.transform as RectTransform,
                    screenPosition,
                    canvas.worldCamera,
                    out Vector2 localPoint
                );

                tooltipRect.anchoredPosition = localPoint;
            }
        }

        /// <summary>
        /// Hide the tooltip.
        /// </summary>
        public void Hide()
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Update the costs display.
        /// </summary>
        private void UpdateCosts(Dictionary<ResourceType, int> costs)
        {
            if (costsContainer == null || costItemPrefab == null) return;

            // Clear existing cost items
            foreach (var item in activeCostItems)
            {
                if (item != null)
                {
                    Destroy(item);
                }
            }
            activeCostItems.Clear();

            // Hide costs container if no costs
            if (costs == null || costs.Count == 0)
            {
                costsContainer.SetActive(false);
                return;
            }

            costsContainer.SetActive(true);

            // Create cost items
            foreach (var cost in costs)
            {
                if (cost.Value <= 0) continue;

                GameObject costItem = Instantiate(costItemPrefab, costsContainer.transform);
                activeCostItems.Add(costItem);

                // Find icon component - try "Icon" child first, then get from root
                Image iconImage = costItem.transform.Find("Icon")?.GetComponent<Image>();
                if (iconImage == null)
                {
                    iconImage = costItem.GetComponentInChildren<Image>();
                }

                // Find text component - try multiple common names
                TextMeshProUGUI costText = costItem.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
                if (costText == null)
                {
                    costText = costItem.transform.Find("CostText")?.GetComponent<TextMeshProUGUI>();
                }
                if (costText == null)
                {
                    costText = costItem.transform.Find("Amount")?.GetComponent<TextMeshProUGUI>();
                }
                if (costText == null)
                {
                    costText = costItem.transform.Find("Value")?.GetComponent<TextMeshProUGUI>();
                }
                if (costText == null)
                {
                    // Last resort - get any TextMeshProUGUI component in children
                    costText = costItem.GetComponentInChildren<TextMeshProUGUI>();
                }

                // Set icon
                if (iconImage != null)
                {
                    iconImage.sprite = GetResourceIcon(cost.Key);
                }

                // Set text
                if (costText != null)
                {
                    costText.text = cost.Value.ToString();
                }
                else
                {
                    Debug.LogWarning($"UniversalTooltip: Could not find TextMeshProUGUI component in cost item prefab for {cost.Key}. Make sure the prefab has a TextMeshProUGUI component.");
                }

                costItem.SetActive(true);
            }
        }

        /// <summary>
        /// Update the stats display.
        /// </summary>
        private void UpdateStats(TooltipData data)
        {
            if (statsContainer == null) return;

            bool hasAnyStats = false;

            // Construction Time
            if (constructionTimeText != null)
            {
                if (data.showConstructionTime)
                {
                    constructionTimeText.text = $"Build Time: {data.constructionTime:F1}s";
                    constructionTimeText.gameObject.SetActive(true);
                    hasAnyStats = true;
                }
                else
                {
                    constructionTimeText.gameObject.SetActive(false);
                }
            }

            // HP
            if (hpText != null)
            {
                if (data.showHP)
                {
                    hpText.text = $"HP: {data.maxHP}";
                    hpText.gameObject.SetActive(true);
                    hasAnyStats = true;
                }
                else
                {
                    hpText.gameObject.SetActive(false);
                }
            }

            // Defence
            if (defenceText != null)
            {
                if (data.showDefence)
                {
                    defenceText.text = $"Defence: {data.defence}";
                    defenceText.gameObject.SetActive(true);
                    hasAnyStats = true;
                }
                else
                {
                    defenceText.gameObject.SetActive(false);
                }
            }

            // Attack Damage
            if (attackDamageText != null)
            {
                if (data.showAttackDamage)
                {
                    attackDamageText.text = $"Attack: {data.attackDamage}";
                    attackDamageText.gameObject.SetActive(true);
                    hasAnyStats = true;
                }
                else
                {
                    attackDamageText.gameObject.SetActive(false);
                }
            }

            // Attack Range
            if (attackRangeText != null)
            {
                if (data.showAttackRange)
                {
                    attackRangeText.text = $"Range: {data.attackRange:F1}";
                    attackRangeText.gameObject.SetActive(true);
                    hasAnyStats = true;
                }
                else
                {
                    attackRangeText.gameObject.SetActive(false);
                }
            }

            // Attack Speed
            if (attackSpeedText != null)
            {
                if (data.showAttackSpeed)
                {
                    attackSpeedText.text = $"Attack Speed: {data.attackSpeed:F2}/s";
                    attackSpeedText.gameObject.SetActive(true);
                    hasAnyStats = true;
                }
                else
                {
                    attackSpeedText.gameObject.SetActive(false);
                }
            }

            // Show/hide stats container based on whether we have any stats
            statsContainer.SetActive(hasAnyStats);
        }

        /// <summary>
        /// Get the appropriate resource icon.
        /// </summary>
        private Sprite GetResourceIcon(ResourceType resourceType)
        {
            return resourceType switch
            {
                ResourceType.Wood => woodIcon,
                ResourceType.Food => foodIcon,
                ResourceType.Gold => goldIcon,
                ResourceType.Stone => stoneIcon,
                _ => null
            };
        }

        /// <summary>
        /// Check if tooltip is currently visible.
        /// </summary>
        public bool IsVisible()
        {
            return tooltipPanel != null && tooltipPanel.activeSelf;
        }
    }
}
