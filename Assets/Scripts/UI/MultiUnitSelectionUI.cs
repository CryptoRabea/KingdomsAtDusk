using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using RTS.Core.Events;
using RTS.Units;

namespace RTS.UI
{
    /// <summary>
    /// Displays multiple selected units as icons with HP bars in a grid layout.
    /// Integrates with UnitDetailsUI - replaces stats section when 2+ units selected.
    /// Formation buttons remain visible.
    /// Automatically scales icons to fit within a square container.
    /// </summary>
    public class MultiUnitSelectionUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Transform unitIconContainer;
        [SerializeField] private GameObject unitIconPrefab;
        [SerializeField] private RectTransform containerRect;

        [Header("Layout Settings")]
        [SerializeField] private int maxIconsToDisplay = 12;
        [SerializeField] private float baseIconSize = 64f;
        [SerializeField] private float minIconSize = 32f;
        [SerializeField] private float iconSpacing = 8f;
        [SerializeField] private float containerPadding = 10f;

        [Header("Square Container")]
        [SerializeField] private bool maintainSquare = true;
        [Tooltip("Maximum size of the square container in pixels")]
        [SerializeField] private float maxContainerSize = 300f;

        [Header("Grid Layout (Optional)")]
        [Tooltip("If assigned, will be used to configure grid layout dynamically")]
        [SerializeField] private GridLayoutGroup gridLayoutGroup;

        private List<UnitIconWithHP> activeIcons = new List<UnitIconWithHP>();
        private List<GameObject> iconPool = new List<GameObject>();

        private void OnEnable()
        {
            EventBus.Subscribe<UnitSelectedEvent>(OnUnitSelected);
            EventBus.Subscribe<UnitDeselectedEvent>(OnUnitDeselected);
            EventBus.Subscribe<SelectionChangedEvent>(OnSelectionChanged);
            EventBus.Subscribe<UnitDiedEvent>(OnUnitDied);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<UnitSelectedEvent>(OnUnitSelected);
            EventBus.Unsubscribe<UnitDeselectedEvent>(OnUnitDeselected);
            EventBus.Unsubscribe<SelectionChangedEvent>(OnSelectionChanged);
            EventBus.Unsubscribe<UnitDiedEvent>(OnUnitDied);
        }

        private void Start()
        {
            // Validate references
            if (unitIconContainer == null)
            {
            }

            if (unitIconPrefab == null)
            {
            }

            // Auto-assign container rect if not set
            if (containerRect == null && unitIconContainer != null)
            {
                containerRect = unitIconContainer.GetComponent<RectTransform>();
            }

            // Initialize grid layout if assigned
            if (gridLayoutGroup != null)
            {
                ConfigureGridLayout();
            }
        }

        private void ConfigureGridLayout()
        {
            // You can customize the grid layout here
            // For example, set cell size, spacing, etc.
            // This is optional and can be configured in the Inspector as well
        }

        private void OnUnitSelected(UnitSelectedEvent evt)
        {
            // Refresh the display when selection changes
            RefreshDisplay();
        }

        private void OnUnitDeselected(UnitDeselectedEvent evt)
        {
            // Refresh the display when selection changes
            RefreshDisplay();
        }

        private void OnSelectionChanged(SelectionChangedEvent evt)
        {
            // Refresh the display when selection count changes
            RefreshDisplay();
        }

        private void OnUnitDied(UnitDiedEvent evt)
        {
            // Remove the icon if the unit died
            RemoveIconForUnit(evt.Unit);
        }

        /// <summary>
        /// Refresh the entire display based on current selection.
        /// Called by UnitDetailsUI when switching to multi-unit mode.
        /// </summary>
        private void RefreshDisplay()
        {
            // Get the selection manager
            var selectionManager = FindFirstObjectByType<UnitSelectionManager>();
            if (selectionManager == null)
            {
                ClearAllIcons();
                return;
            }

            var selectedUnits = selectionManager.SelectedUnits;

            // Clear existing icons
            ClearAllIcons();

            // Only show icons if we have units selected
            if (selectedUnits.Count == 0)
            {
                return;
            }

            // Calculate optimal grid and icon size
            int iconsToCreate = Mathf.Min(selectedUnits.Count, maxIconsToDisplay);
            UpdateGridLayoutForCount(iconsToCreate);

            // Create icons for each selected unit (up to max)
            for (int i = 0; i < iconsToCreate; i++)
            {
                var unit = selectedUnits[i];
                if (unit != null && unit.gameObject != null)
                {
                    CreateIconForUnit(unit.gameObject);
                }
            }
        }

        /// <summary>
        /// Dynamically adjusts grid layout to fit icons within a square container.
        /// </summary>
        private void UpdateGridLayoutForCount(int iconCount)
        {
            if (gridLayoutGroup == null || !maintainSquare)
                return;

            // Calculate optimal grid dimensions (as square as possible)
            int columns = Mathf.CeilToInt(Mathf.Sqrt(iconCount));
            int rows = Mathf.CeilToInt((float)iconCount / columns);

            // Calculate available space
            float availableSize = maxContainerSize - (containerPadding * 2);

            // Calculate icon size to fit within the square
            float iconSizeWithSpacing = availableSize / Mathf.Max(columns, rows);
            float calculatedIconSize = iconSizeWithSpacing - iconSpacing;

            // Clamp icon size between min and base size
            float finalIconSize = Mathf.Clamp(calculatedIconSize, minIconSize, baseIconSize);

            // Update grid layout
            gridLayoutGroup.constraintCount = columns;
            gridLayoutGroup.cellSize = new Vector2(finalIconSize, finalIconSize);
            gridLayoutGroup.spacing = new Vector2(iconSpacing, iconSpacing);

            // Update container size to maintain square
            if (containerRect != null)
            {
                float totalSize = (finalIconSize * Mathf.Max(columns, rows)) +
                                 (iconSpacing * (Mathf.Max(columns, rows) - 1)) +
                                 (containerPadding * 2);

                totalSize = Mathf.Min(totalSize, maxContainerSize);
                containerRect.sizeDelta = new Vector2(totalSize, totalSize);
            }

        }

        /// <summary>
        /// Create an icon for a specific unit.
        /// </summary>
        private void CreateIconForUnit(GameObject unit)
        {
            if (unitIconPrefab == null || unitIconContainer == null)
                return;

            // Get or create icon from pool
            GameObject iconObj = GetIconFromPool();

            // Add to container
            iconObj.transform.SetParent(unitIconContainer, false);
            iconObj.SetActive(true);

            // Initialize the icon
            if (iconObj.TryGetComponent<UnitIconWithHP>(out var iconComponent))
            {
                iconComponent.Initialize(unit);
                activeIcons.Add(iconComponent);
            }
            else
            {
            }
        }

        /// <summary>
        /// Remove the icon for a specific unit.
        /// </summary>
        private void RemoveIconForUnit(GameObject unit)
        {
            for (int i = activeIcons.Count - 1; i >= 0; i--)
            {
                if (activeIcons[i].GetTrackedUnit() == unit)
                {
                    ReturnIconToPool(activeIcons[i].gameObject);
                    activeIcons.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Clear all active icons.
        /// </summary>
        private void ClearAllIcons()
        {
            foreach (var icon in activeIcons)
            {
                if (icon != null)
                {
                    icon.Clear();
                    ReturnIconToPool(icon.gameObject);
                }
            }
            activeIcons.Clear();
        }

        /// <summary>
        /// Get an icon from the pool or create a new one.
        /// </summary>
        private GameObject GetIconFromPool()
        {
            // Check if there's an inactive icon in the pool
            foreach (var pooledIcon in iconPool)
            {
                if (pooledIcon != null && !pooledIcon.activeSelf)
                {
                    return pooledIcon;
                }
            }

            // Create a new icon
            GameObject newIcon = Instantiate(unitIconPrefab);
            iconPool.Add(newIcon);
            return newIcon;
        }

        /// <summary>
        /// Return an icon to the pool.
        /// </summary>
        private void ReturnIconToPool(GameObject icon)
        {
            if (icon != null)
            {
                icon.SetActive(false);
                icon.transform.SetParent(transform, false);
            }
        }

        /// <summary>
        /// Public method to manually refresh the display.
        /// </summary>
        public void ForceRefresh()
        {
            RefreshDisplay();
        }
    }
}
