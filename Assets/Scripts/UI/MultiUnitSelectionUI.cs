using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using RTS.Core.Events;
using RTS.Units;

namespace RTS.UI
{
    /// <summary>
    /// Displays multiple selected units as icons with HP bars in a grid layout.
    /// Similar to RTS games like Warcraft 3 where selected units are shown as portraits.
    /// </summary>
    public class MultiUnitSelectionUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject multiUnitPanel;
        [SerializeField] private Transform unitIconContainer;
        [SerializeField] private GameObject unitIconPrefab;

        [Header("Layout Settings")]
        [SerializeField] private int maxIconsToDisplay = 12;
        [SerializeField] private bool autoHideWhenEmpty = true;
        [SerializeField] private bool showOnlyWhenMultiple = true;

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
            // Hide panel initially
            if (multiUnitPanel != null)
            {
                multiUnitPanel.SetActive(false);
            }

            // Validate references
            if (unitIconContainer == null)
            {
                Debug.LogWarning("[MultiUnitSelectionUI] Unit icon container not assigned!");
            }

            if (unitIconPrefab == null)
            {
                Debug.LogWarning("[MultiUnitSelectionUI] Unit icon prefab not assigned!");
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
        /// </summary>
        private void RefreshDisplay()
        {
            // Get the selection manager
            var selectionManager = FindFirstObjectByType<UnitSelectionManager>();
            if (selectionManager == null)
            {
                Debug.LogWarning("[MultiUnitSelectionUI] UnitSelectionManager not found!");
                ClearAllIcons();
                HidePanel();
                return;
            }

            var selectedUnits = selectionManager.SelectedUnits;

            // Check if we should show the panel
            bool shouldShow = selectedUnits.Count > 0;
            if (showOnlyWhenMultiple)
            {
                shouldShow = selectedUnits.Count > 1;
            }

            if (!shouldShow)
            {
                ClearAllIcons();
                HidePanel();
                return;
            }

            // Clear existing icons
            ClearAllIcons();

            // Create icons for each selected unit (up to max)
            int iconsToCreate = Mathf.Min(selectedUnits.Count, maxIconsToDisplay);
            for (int i = 0; i < iconsToCreate; i++)
            {
                var unit = selectedUnits[i];
                if (unit != null && unit.gameObject != null)
                {
                    CreateIconForUnit(unit.gameObject);
                }
            }

            // Show the panel
            ShowPanel();
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
            var iconComponent = iconObj.GetComponent<UnitIconWithHP>();
            if (iconComponent != null)
            {
                iconComponent.Initialize(unit);
                activeIcons.Add(iconComponent);
            }
            else
            {
                Debug.LogWarning("[MultiUnitSelectionUI] Unit icon prefab doesn't have UnitIconWithHP component!");
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

            // Hide panel if no icons left
            if (activeIcons.Count == 0 && autoHideWhenEmpty)
            {
                HidePanel();
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
        /// Show the multi-unit panel.
        /// </summary>
        private void ShowPanel()
        {
            if (multiUnitPanel != null && !multiUnitPanel.activeSelf)
            {
                multiUnitPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Hide the multi-unit panel.
        /// </summary>
        private void HidePanel()
        {
            if (multiUnitPanel != null && multiUnitPanel.activeSelf)
            {
                multiUnitPanel.SetActive(false);
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
