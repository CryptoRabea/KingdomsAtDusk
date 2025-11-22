using UnityEngine;

namespace RTS.UI.HUD
{
    /// <summary>
    /// Configuration for the main HUD framework.
    /// Allows developers to enable/disable HUD components and customize layouts.
    /// </summary>
    [CreateAssetMenu(fileName = "HUDConfiguration", menuName = "RTS/UI/HUD Configuration", order = 1)]
    public class HUDConfiguration : ScriptableObject
    {
        [Header("Layout Settings")]
        [Tooltip("The layout preset to use (Classic Warcraft, Modern RTS, etc.)")]
        public HUDLayoutPreset layoutPreset;

        [Header("Core Components")]
        [Tooltip("Enable the minimap")]
        public bool enableMinimap = true;

        [Tooltip("Enable unit details panel")]
        public bool enableUnitDetails = true;

        [Tooltip("Enable building details panel")]
        public bool enableBuildingDetails = true;

        [Tooltip("Enable building placement HUD")]
        public bool enableBuildingHUD = true;

        [Header("Optional Components")]
        [Tooltip("Enable top resource bar (like Warcraft 3)")]
        public bool enableTopBar = false;

        [Tooltip("Include resources in top bar (if false, only menu buttons)")]
        public bool includeResourcesInTopBar = true;

        [Tooltip("Enable inventory system for units")]
        public bool enableInventory = false;

        [Tooltip("Number of inventory slots (3x2 = 6, 2x3 = 6, etc.)")]
        public Vector2Int inventoryGridSize = new Vector2Int(3, 2);

        [Header("Resource Display")]
        [Tooltip("Show resources in separate panel (when top bar is disabled)")]
        public bool showStandaloneResourcePanel = true;

        [Tooltip("Show happiness indicator")]
        public bool showHappiness = true;

        [Header("Additional Features")]
        [Tooltip("Enable notifications panel")]
        public bool enableNotifications = true;

        [Tooltip("Enable cursor state management")]
        public bool enableCustomCursor = true;

        [Tooltip("Enable wall resource preview")]
        public bool enableWallPreview = true;

        [Header("Performance")]
        [Tooltip("Update frequency for HUD elements (times per second)")]
        [Range(10, 60)]
        public int hudUpdateRate = 30;

        [Tooltip("Enable UI animations")]
        public bool enableAnimations = true;

        /// <summary>
        /// Validates the configuration and applies default values if needed.
        /// </summary>
        public void Validate()
        {
            // Ensure inventory grid is at least 1x1
            if (inventoryGridSize.x < 1) inventoryGridSize.x = 1;
            if (inventoryGridSize.y < 1) inventoryGridSize.y = 1;

            // If top bar is enabled with resources, disable standalone resource panel
            if (enableTopBar && includeResourcesInTopBar)
            {
                showStandaloneResourcePanel = false;
            }
        }
    }
}
