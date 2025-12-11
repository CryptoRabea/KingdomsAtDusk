using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTS.Buildings;
using RTS.Core.Services;
using System.Collections.Generic;

namespace RTS.UI
{
    /// <summary>
    /// Displays resource cost preview for wall placement.
    /// Shows required resources and segment count during pole-to-pole placement.
    /// </summary>
    public class WallResourcePreviewUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WallPlacementController wallPlacementController;
        [SerializeField] private Canvas canvas;

        [Header("UI Elements")]
        [SerializeField] private GameObject previewPanel;
        [SerializeField] private TextMeshProUGUI segmentCountText;
        [SerializeField] private TextMeshProUGUI woodCostText;
        [SerializeField] private TextMeshProUGUI foodCostText;
        [SerializeField] private TextMeshProUGUI goldCostText;
        [SerializeField] private TextMeshProUGUI stoneCostText;

        [Header("Resource Icons")]
        [SerializeField] private GameObject woodIcon;
        [SerializeField] private GameObject foodIcon;
        [SerializeField] private GameObject goldIcon;
        [SerializeField] private GameObject stoneIcon;

        [Header("Colors")]
        [SerializeField] private Color affordableColor = Color.green;
        [SerializeField] private Color unaffordableColor = Color.red;

        [Header("Positioning")]
        [SerializeField] private Vector2 screenOffset = new Vector2(20, 20); // Offset from cursor
        [SerializeField] private bool followCursor = true;

        private IResourcesService resourceService;

        private void Start()
        {
            resourceService = ServiceLocator.TryGet<IResourcesService>();

            if (wallPlacementController == null)
            {
            }

            // Hide panel initially
            if (previewPanel != null)
            {
                previewPanel.SetActive(false);
            }
        }

        private void Update()
        {
            if (wallPlacementController == null) return;

            // Show/hide panel based on wall placement state
            bool isPlacing = wallPlacementController.IsPlacingWalls;

            if (previewPanel != null && previewPanel.activeSelf != isPlacing)
            {
                previewPanel.SetActive(isPlacing);
            }

            // Update UI if placing walls
            if (isPlacing)
            {
                UpdatePreviewUI();

                if (followCursor)
                {
                    UpdatePanelPosition();
                }
            }
        }

        private void UpdatePreviewUI()
        {
            // Get segment count
            int segmentCount = wallPlacementController.GetRequiredSegments();

            // Get total cost
            Dictionary<ResourceType, int> totalCost = wallPlacementController.GetTotalCost();

            // Update segment count
            if (segmentCountText != null)
            {
                segmentCountText.text = $"Segments: {segmentCount}";
            }

            // Update resource costs
            UpdateResourceCost(ResourceType.Wood, totalCost, woodCostText, woodIcon);
            UpdateResourceCost(ResourceType.Food, totalCost, foodCostText, foodIcon);
            UpdateResourceCost(ResourceType.Gold, totalCost, goldCostText, goldIcon);
            UpdateResourceCost(ResourceType.Stone, totalCost, stoneCostText, stoneIcon);
        }

        private void UpdateResourceCost(ResourceType resourceType, Dictionary<ResourceType, int> totalCost,
                                       TextMeshProUGUI costText, GameObject icon)
        {
            if (costText == null) return;

            int cost = totalCost.GetValueOrDefault(resourceType, 0);

            if (cost > 0)
            {
                // Show cost
                costText.text = cost.ToString();

                if (icon != null)
                {
                    icon.SetActive(true);
                }

                // Check if player can afford this resource
                if (resourceService != null)
                {
                    int currentAmount = resourceService.GetResource(resourceType);
                    bool canAfford = currentAmount >= cost;

                    costText.color = canAfford ? affordableColor : unaffordableColor;
                }
            }
            else
            {
                // Hide this resource if not needed
                if (icon != null)
                {
                    icon.SetActive(false);
                }

                costText.text = "";
            }
        }

        private void UpdatePanelPosition()
        {
            if (canvas == null || previewPanel == null) return;

            // Get mouse position
            Vector2 mousePos = Input.mousePosition;

            // Calculate panel position with offset
            Vector2 panelPos = mousePos + screenOffset;

            // Clamp to screen bounds
            if (previewPanel.TryGetComponent<RectTransform>(out var panelRect))
            {
                // Get screen dimensions
                float screenWidth = Screen.width;
                float screenHeight = Screen.height;

                // Get panel size
                Vector2 panelSize = panelRect.sizeDelta;

                // Clamp position
                panelPos.x = Mathf.Clamp(panelPos.x, 0, screenWidth - panelSize.x);
                panelPos.y = Mathf.Clamp(panelPos.y, 0, screenHeight - panelSize.y);

                // Set position
                panelRect.position = panelPos;
            }
        }

        #region Public API

        /// <summary>
        /// Set the wall placement controller reference.
        /// </summary>
        public void SetWallPlacementController(WallPlacementController controller)
        {
            wallPlacementController = controller;
        }

        /// <summary>
        /// Show or hide the preview panel.
        /// </summary>
        public void SetPanelVisible(bool visible)
        {
            if (previewPanel != null)
            {
                previewPanel.SetActive(visible);
            }
        }

        /// <summary>
        /// Set whether the panel should follow the cursor.
        /// </summary>
        public void SetFollowCursor(bool follow)
        {
            followCursor = follow;
        }

        #endregion
    }
}
