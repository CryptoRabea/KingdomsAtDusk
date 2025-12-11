using UnityEngine;
using UnityEngine.InputSystem;
using RTS.UI.HUD;

namespace RTS.UI.HUD
{
    /// <summary>
    /// Runtime controller for the HUD system.
    /// Provides easy access to HUD functionality for game code.
    /// Use this instead of directly accessing MainHUDFramework.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        private static HUDController instance;
        public static HUDController Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Object.FindAnyObjectByType<HUDController>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("HUDController");
                        instance = go.AddComponent<HUDController>();
                    }
                }
                return instance;
            }
        }

        [Header("References")]
        [SerializeField] private MainHUDFramework hudFramework;

        [Header("Hotkeys (Optional)")]
        [SerializeField] private bool enableHotkeys = true;
        [SerializeField] private Key toggleInventoryKey = Key.I;
        [SerializeField] private Key toggleMinimapKey = Key.M;
        [SerializeField] private Key toggleTopBarKey = Key.T;
        [SerializeField] private Key toggleAllUIKey = Key.F1;

        [Header("Presets")]
        [SerializeField] private HUDConfiguration[] presetConfigurations;
        [SerializeField] private HUDLayoutPreset[] presetLayouts;

        private bool uiVisible = true;

        private void Awake()
        {
            // Singleton pattern
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            // Auto-find MainHUDFramework if not assigned
            if (hudFramework == null)
            {
                hudFramework = Object.FindAnyObjectByType<MainHUDFramework>();
            }

            if (hudFramework == null)
            {
            }
        }

        private void Update()
        {
            if (!enableHotkeys || Keyboard.current == null) return;

            // Toggle hotkeys
            if (Keyboard.current[toggleInventoryKey].wasPressedThisFrame)
            {
                ToggleInventory();
            }

            if (Keyboard.current[toggleMinimapKey].wasPressedThisFrame)
            {
                ToggleMinimap();
            }

            if (Keyboard.current[toggleTopBarKey].wasPressedThisFrame)
            {
                ToggleTopBar();
            }

            if (Keyboard.current[toggleAllUIKey].wasPressedThisFrame)
            {
                ToggleAllUI();
            }
        }

        #region Public API - Component Toggles

        /// <summary>
        /// Toggles the inventory UI on/off.
        /// </summary>
        public void ToggleInventory()
        {
            if (hudFramework == null) return;

            var config = hudFramework.GetConfiguration();
            config.enableInventory = !config.enableInventory;
            hudFramework.ToggleComponent("inventory", config.enableInventory);

        }

        /// <summary>
        /// Toggles the minimap on/off.
        /// </summary>
        public void ToggleMinimap()
        {
            if (hudFramework == null) return;

            var config = hudFramework.GetConfiguration();
            config.enableMinimap = !config.enableMinimap;
            hudFramework.ToggleComponent("minimap", config.enableMinimap);

        }

        /// <summary>
        /// Toggles the top bar on/off.
        /// </summary>
        public void ToggleTopBar()
        {
            if (hudFramework == null) return;

            var config = hudFramework.GetConfiguration();
            config.enableTopBar = !config.enableTopBar;
            hudFramework.ToggleComponent("topbar", config.enableTopBar);

        }

        /// <summary>
        /// Toggles all UI on/off.
        /// </summary>
        public void ToggleAllUI()
        {
            uiVisible = !uiVisible;

            if (hudFramework != null)
            {
                hudFramework.gameObject.SetActive(uiVisible);
            }

        }

        /// <summary>
        /// Shows or hides a specific component.
        /// </summary>
        public void SetComponentVisible(string componentName, bool visible)
        {
            if (hudFramework == null) return;
            hudFramework.ToggleComponent(componentName, visible);
        }

        #endregion

        #region Public API - Configuration

        /// <summary>
        /// Applies a configuration preset by index.
        /// </summary>
        public void ApplyConfigurationPreset(int presetIndex)
        {
            if (hudFramework == null) return;

            if (presetIndex >= 0 && presetIndex < presetConfigurations.Length)
            {
                hudFramework.SetConfiguration(presetConfigurations[presetIndex]);
            }
            else
            {
            }
        }

        /// <summary>
        /// Applies a configuration by name.
        /// </summary>
        public void ApplyConfiguration(string configName)
        {
            if (hudFramework == null) return;

            var config = Resources.Load<HUDConfiguration>($"HUD/Configurations/{configName}");
            if (config != null)
            {
                hudFramework.SetConfiguration(config);
            }
            else
            {
            }
        }

        /// <summary>
        /// Applies a layout preset by index.
        /// </summary>
        public void ApplyLayoutPreset(int presetIndex)
        {
            if (hudFramework == null) return;

            if (presetIndex >= 0 && presetIndex < presetLayouts.Length)
            {
                hudFramework.ApplyLayoutPreset(presetLayouts[presetIndex]);
            }
            else
            {
            }
        }

        /// <summary>
        /// Applies a layout by name.
        /// </summary>
        public void ApplyLayout(string layoutName)
        {
            if (hudFramework == null) return;

            var layout = Resources.Load<HUDLayoutPreset>($"HUD/Layouts/{layoutName}");
            if (layout != null)
            {
                hudFramework.ApplyLayoutPreset(layout);
            }
            else
            {
            }
        }

        #endregion

        #region Public API - Inventory

        /// <summary>
        /// Sets the inventory data for the current unit.
        /// </summary>
        public void SetInventory(InventoryData inventory)
        {
            if (hudFramework == null) return;

            if (hudFramework.TryGetComponent<InventoryUI>(out var inventoryUI))
            {
                inventoryUI.SetInventory(inventory);
            }
        }

        /// <summary>
        /// Clears the inventory display.
        /// </summary>
        public void ClearInventory()
        {
            if (hudFramework == null) return;

            if (hudFramework.TryGetComponent<InventoryUI>(out var inventoryUI))
            {
                inventoryUI.ClearInventory();
            }
        }

        #endregion

        #region Public API - Top Bar

        /// <summary>
        /// Configures the top bar display options.
        /// </summary>
        public void ConfigureTopBar(bool showResources, bool showMenu, bool showClock, bool showPopulation)
        {
            if (hudFramework == null) return;

            if (hudFramework.TryGetComponent<TopBarUI>(out var topBar))
            {
                topBar.Configure(showResources, showMenu, showClock, showPopulation);
            }
        }

        #endregion

        #region Public API - Quick Presets

        /// <summary>
        /// Applies Warcraft 3 style HUD.
        /// </summary>
        public void ApplyWarcraft3Style()
        {
            ApplyConfiguration("Warcraft3HUDConfig");
            ApplyLayout("Warcraft3Layout");
        }

        /// <summary>
        /// Applies modern RTS style HUD.
        /// </summary>
        public void ApplyModernRTSStyle()
        {
            ApplyConfiguration("DefaultHUDConfig");
            ApplyLayout("ModernRTSLayout");
        }

        /// <summary>
        /// Applies minimal/clean style HUD.
        /// </summary>
        public void ApplyMinimalStyle()
        {
            ApplyConfiguration("MinimalHUDConfig");
            ApplyLayout("CompactLayout");
        }

        /// <summary>
        /// Applies Age of Empires style HUD.
        /// </summary>
        public void ApplyAgeOfEmpiresStyle()
        {
            ApplyConfiguration("DefaultHUDConfig");
            ApplyLayout("AgeOfEmpiresLayout");
        }

        #endregion

        #region Debug/Development

        /// <summary>
        /// Cycles through all configuration presets (for testing).
        /// </summary>
        [ContextMenu("Cycle Configurations")]
        public void CycleConfigurations()
        {
            if (presetConfigurations.Length == 0) return;

            var currentConfig = hudFramework.GetConfiguration();
            int currentIndex = System.Array.IndexOf(presetConfigurations, currentConfig);
            int nextIndex = (currentIndex + 1) % presetConfigurations.Length;

            ApplyConfigurationPreset(nextIndex);
        }

        /// <summary>
        /// Cycles through all layout presets (for testing).
        /// </summary>
        [ContextMenu("Cycle Layouts")]
        public void CycleLayouts()
        {
            if (presetLayouts.Length == 0) return;

            // Find current layout (simplified - just cycle)
            ApplyLayoutPreset(Random.Range(0, presetLayouts.Length));
        }

        /// <summary>
        /// Logs current HUD status.
        /// </summary>
        [ContextMenu("Log HUD Status")]
        public void LogHUDStatus()
        {
            if (hudFramework == null)
            {
                return;
            }

            var config = hudFramework.GetConfiguration();
        }

        #endregion
    }
}
