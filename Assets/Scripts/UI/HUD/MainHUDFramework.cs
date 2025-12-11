using KingdomsAtDusk.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RTS.UI.HUD
{
    /// <summary>
    /// Main HUD Framework that manages all UI components.
    /// Provides a centralized system for configuring and controlling the game's HUD.
    /// Supports multiple layout presets and developer configuration options.
    /// </summary>
    public class MainHUDFramework : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private HUDConfiguration configuration;

        [Header("Canvas")]
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private CanvasScaler canvasScaler;

        [Header("Core Components")]
        [SerializeField] private GameObject minimapPanel;
        [SerializeField] private UnitDetailsUI unitDetailsUI;
        [SerializeField] private BuildingDetailsUI buildingDetailsUI;
        [SerializeField] private BuildingHUD buildingHUD;

        [Header("Optional Components")]
        [SerializeField] private TopBarUI topBarUI;
        [SerializeField] private InventoryUI inventoryUI;
        [SerializeField] private ResourceUI resourceUI;
        [SerializeField] private HappinessUI happinessUI;
        [SerializeField] private NotificationUI notificationUI;
        [SerializeField] private WallResourcePreviewUI wallPreviewUI;

        [Header("Cursor")]
        [SerializeField] private CursorStateManager cursorStateManager;

        [Header("Layout")]
        [SerializeField] private RectTransform hudContainer;

        private Dictionary<string, RectTransform> hudElements = new Dictionary<string, RectTransform>();
        private float updateTimer;
        private float updateInterval;

        private void Awake()
        {
            // Validate configuration
            if (configuration == null)
            {
                return;
            }

            configuration.Validate();

            // Calculate update interval
            updateInterval = 1f / configuration.hudUpdateRate;

            // Initialize HUD
            InitializeHUD();
        }

        private void Start()
        {
            // Apply layout after all components are initialized
            if (configuration.layoutPreset != null)
            {
                ApplyLayoutPreset(configuration.layoutPreset);
            }
        }

        private void Update()
        {
            // Throttled updates for performance
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0f;
                UpdateHUD();
            }
        }

        /// <summary>
        /// Initializes all HUD components based on configuration.
        /// </summary>
        private void InitializeHUD()
        {

            // Core components
            SetComponentActive(minimapPanel, configuration.enableMinimap, "Minimap");
            SetComponentActive(unitDetailsUI?.gameObject, configuration.enableUnitDetails, "UnitDetails");
            SetComponentActive(buildingDetailsUI?.gameObject, configuration.enableBuildingDetails, "BuildingDetails");
            SetComponentActive(buildingHUD?.gameObject, configuration.enableBuildingHUD, "BuildingHUD");

            // Optional components
            SetComponentActive(topBarUI?.gameObject, configuration.enableTopBar, "TopBar");
            SetComponentActive(inventoryUI?.gameObject, configuration.enableInventory, "Inventory");
            SetComponentActive(notificationUI?.gameObject, configuration.enableNotifications, "Notifications");
            SetComponentActive(wallPreviewUI?.gameObject, configuration.enableWallPreview, "WallPreview");

            // Resource display logic
            if (configuration.enableTopBar && configuration.includeResourcesInTopBar)
            {
                // Resources in top bar, disable standalone
                SetComponentActive(resourceUI?.gameObject, false, "ResourceUI");
                SetComponentActive(happinessUI?.gameObject, false, "HappinessUI");

                // Configure top bar to show resources
                if (topBarUI != null)
                {
                    topBarUI.Configure(true, true, false, true);
                }
            }
            else if (configuration.showStandaloneResourcePanel)
            {
                // Standalone resource panel
                SetComponentActive(resourceUI?.gameObject, true, "ResourceUI");
                SetComponentActive(happinessUI?.gameObject, configuration.showHappiness, "HappinessUI");

                // Configure top bar to show only menu if enabled
                if (topBarUI != null && configuration.enableTopBar)
                {
                    topBarUI.Configure(false, true, false, false);
                }
            }

            // Configure inventory grid size
            if (inventoryUI != null && configuration.enableInventory)
            {
                inventoryUI.ConfigureGrid(configuration.inventoryGridSize);
            }

            // Cursor management
            if (cursorStateManager != null)
            {
                cursorStateManager.enabled = configuration.enableCustomCursor;
            }

            // Register all HUD elements for layout management
            RegisterHUDElements();

        }

        /// <summary>
        /// Registers all HUD elements for layout management.
        /// </summary>
        private void RegisterHUDElements()
        {
            hudElements.Clear();

            int registeredCount = 0;

            if (minimapPanel != null)
            {
                hudElements["Minimap"] = minimapPanel.GetComponent<RectTransform>();
                registeredCount++;
            }

            if (unitDetailsUI != null)
            {
                hudElements["UnitDetails"] = unitDetailsUI.GetComponent<RectTransform>();
                registeredCount++;
            }

            if (buildingDetailsUI != null)
            {
                hudElements["BuildingDetails"] = buildingDetailsUI.GetComponent<RectTransform>();
                registeredCount++;
            }

            if (buildingHUD != null)
            {
                hudElements["BuildingHUD"] = buildingHUD.GetComponent<RectTransform>();
                registeredCount++;
            }

            if (inventoryUI != null)
            {
                hudElements["Inventory"] = inventoryUI.GetComponent<RectTransform>();
                registeredCount++;
            }

            if (topBarUI != null)
            {
                hudElements["TopBar"] = topBarUI.GetComponent<RectTransform>();
                registeredCount++;
            }

            if (resourceUI != null)
            {
                hudElements["ResourcePanel"] = resourceUI.GetComponent<RectTransform>();
                registeredCount++;
            }

            if (notificationUI != null)
            {
                hudElements["Notifications"] = notificationUI.GetComponent<RectTransform>();
                registeredCount++;
            }

            if (registeredCount == 0)
            {
            }
            else
            {
            }
        }

        /// <summary>
        /// Applies a layout preset to all HUD elements.
        /// </summary>
        public void ApplyLayoutPreset(HUDLayoutPreset preset)
        {
            if (preset == null)
            {
                return;
            }


            // Apply minimap layout
            if (hudElements.ContainsKey("Minimap"))
            {
                ApplyElementLayout(hudElements["Minimap"], preset.minimapAnchor,
                    preset.minimapSize, preset.minimapOffset);
            }

            // Apply unit details layout
            if (hudElements.ContainsKey("UnitDetails"))
            {
                ApplyElementLayout(hudElements["UnitDetails"], preset.unitDetailsAnchor,
                    preset.unitDetailsSize, preset.unitDetailsOffset);
            }

            // Apply building details layout
            if (hudElements.ContainsKey("BuildingDetails"))
            {
                ApplyElementLayout(hudElements["BuildingDetails"], preset.buildingDetailsAnchor,
                    preset.buildingDetailsSize, preset.buildingDetailsOffset);
            }

            // Apply building HUD layout
            if (hudElements.ContainsKey("BuildingHUD"))
            {
                ApplyElementLayout(hudElements["BuildingHUD"], preset.buildingHUDAnchor,
                    preset.buildingHUDSize, preset.buildingHUDOffset);
            }

            // Apply inventory layout
            if (hudElements.ContainsKey("Inventory"))
            {
                ApplyElementLayout(hudElements["Inventory"], preset.inventoryAnchor,
                    preset.inventorySize, preset.inventoryOffset);
            }

            // Apply top bar layout
            if (hudElements.ContainsKey("TopBar"))
            {
                ApplyTopBarLayout(hudElements["TopBar"], preset.topBarHeight, preset.topBarOffset);
            }

            // Apply resource panel layout
            if (hudElements.ContainsKey("ResourcePanel"))
            {
                ApplyElementLayout(hudElements["ResourcePanel"], preset.resourcePanelAnchor,
                    preset.resourcePanelSize, preset.resourcePanelOffset);
            }

            // Apply notifications layout
            if (hudElements.ContainsKey("Notifications"))
            {
                ApplyElementLayout(hudElements["Notifications"], preset.notificationsAnchor,
                    Vector2.zero, preset.notificationsOffset);
            }
        }

        /// <summary>
        /// Applies layout to a single HUD element.
        /// </summary>
        private void ApplyElementLayout(RectTransform element, HUDLayoutPreset.AnchorPosition anchor,
            Vector2 size, Vector2 offset)
        {
            if (element == null) return;

            // Apply anchor
            HUDLayoutPreset.ApplyAnchor(element, anchor);

            // Apply size if specified
            if (size != Vector2.zero)
            {
                element.sizeDelta = size;
            }

            // Apply offset
            element.anchoredPosition = offset;

        }

        /// <summary>
        /// Applies layout to the top bar (full width).
        /// </summary>
        private void ApplyTopBarLayout(RectTransform element, float height, Vector2 offset)
        {
            if (element == null) return;

            // Top bar spans full width
            element.anchorMin = new Vector2(0, 1);
            element.anchorMax = new Vector2(1, 1);
            element.pivot = new Vector2(0.5f, 1);

            // Set height
            element.sizeDelta = new Vector2(0, height);

            // Apply offset
            element.anchoredPosition = new Vector2(0, -offset.y);
        }

        /// <summary>
        /// Updates HUD components (called at configured update rate).
        /// </summary>
        private void UpdateHUD()
        {
            // Additional periodic updates can go here if needed
        }

        /// <summary>
        /// Sets a component active/inactive and logs the action.
        /// </summary>
        private void SetComponentActive(GameObject component, bool active, string componentName)
        {
            if (component != null)
            {
                component.SetActive(active);
            }
        }

        /// <summary>
        /// Public API: Change configuration at runtime.
        /// </summary>
        public void SetConfiguration(HUDConfiguration newConfig)
        {
            if (newConfig != null)
            {
                configuration = newConfig;
                configuration.Validate();
                InitializeHUD();

                if (configuration.layoutPreset != null)
                {
                    ApplyLayoutPreset(configuration.layoutPreset);
                }
            }
        }

        /// <summary>
        /// Public API: Toggle specific HUD component.
        /// </summary>
        public void ToggleComponent(string componentName, bool enabled)
        {
            switch (componentName.ToLower())
            {
                case "minimap":
                    SetComponentActive(minimapPanel, enabled, "Minimap");
                    break;
                case "unitdetails":
                    SetComponentActive(unitDetailsUI?.gameObject, enabled, "UnitDetails");
                    break;
                case "buildingdetails":
                    SetComponentActive(buildingDetailsUI?.gameObject, enabled, "BuildingDetails");
                    break;
                case "buildinghud":
                    SetComponentActive(buildingHUD?.gameObject, enabled, "BuildingHUD");
                    break;
                case "topbar":
                    SetComponentActive(topBarUI?.gameObject, enabled, "TopBar");
                    break;
                case "inventory":
                    SetComponentActive(inventoryUI?.gameObject, enabled, "Inventory");
                    break;
                case "notifications":
                    SetComponentActive(notificationUI?.gameObject, enabled, "Notifications");
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Public API: Get current configuration.
        /// </summary>
        public HUDConfiguration GetConfiguration()
        {
            return configuration;
        }

        /// <summary>
        /// Public API: Get specific HUD component.
        /// </summary>
        public new T GetComponent<T>() where T : MonoBehaviour
        {
            if (typeof(T) == typeof(TopBarUI)) return topBarUI as T;
            if (typeof(T) == typeof(InventoryUI)) return inventoryUI as T;
            if (typeof(T) == typeof(UnitDetailsUI)) return unitDetailsUI as T;
            if (typeof(T) == typeof(BuildingDetailsUI)) return buildingDetailsUI as T;
            if (typeof(T) == typeof(BuildingHUD)) return buildingHUD as T;
            if (typeof(T) == typeof(ResourceUI)) return resourceUI as T;
            if (typeof(T) == typeof(HappinessUI)) return happinessUI as T;
            if (typeof(T) == typeof(NotificationUI)) return notificationUI as T;

            return null;
        }

#if UNITY_EDITOR
        [ContextMenu("Reload Configuration")]
        private void ReloadConfiguration()
        {
            if (configuration != null)
            {
                InitializeHUD();
                if (configuration.layoutPreset != null)
                {
                    ApplyLayoutPreset(configuration.layoutPreset);
                }
            }
        }
#endif
    }
}
