#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace RTS.UI.HUD.Editor
{
    /// <summary>
    /// Provides template creation methods for HUD configurations and layouts.
    /// Accessible via Unity menu: Tools > RTS > Create HUD Templates
    /// </summary>
    public static class HUDConfigurationTemplates
    {
        private const string ConfigPath = "Assets/Resources/HUD/Configurations/";
        private const string LayoutPath = "Assets/Resources/HUD/Layouts/";

        [MenuItem("Tools/RTS/Create HUD Templates/All Templates")]
        public static void CreateAllTemplates()
        {
            CreateDefaultConfiguration();
            CreateMinimalConfiguration();
            CreateFullConfiguration();
            CreateWarcraft3Layout();
            CreateModernRTSLayout();
            CreateAgeOfEmpiresLayout();
            AssetDatabase.Refresh();
            Debug.Log("HUD Templates created successfully!");
        }

        #region Configuration Templates

        [MenuItem("Tools/RTS/Create HUD Templates/Configurations/Default")]
        public static void CreateDefaultConfiguration()
        {
            var config = ScriptableObject.CreateInstance<HUDConfiguration>();
            config.name = "DefaultHUDConfig";

            // Core components
            config.enableMinimap = true;
            config.enableUnitDetails = true;
            config.enableBuildingDetails = true;
            config.enableBuildingHUD = true;

            // Optional components
            config.enableTopBar = false;
            config.enableInventory = false;
            config.showStandaloneResourcePanel = true;
            config.showHappiness = true;

            // Additional features
            config.enableNotifications = true;
            config.enableCustomCursor = true;
            config.enableWallPreview = true;

            // Performance
            config.hudUpdateRate = 30;
            config.enableAnimations = true;

            SaveAsset(config, ConfigPath, "DefaultHUDConfig.asset");
        }

        [MenuItem("Tools/RTS/Create HUD Templates/Configurations/Minimal (Clean UI)")]
        public static void CreateMinimalConfiguration()
        {
            var config = ScriptableObject.CreateInstance<HUDConfiguration>();
            config.name = "MinimalHUDConfig";

            // Core components - minimal set
            config.enableMinimap = true;
            config.enableUnitDetails = true;
            config.enableBuildingDetails = true;
            config.enableBuildingHUD = true;

            // Optional components - all disabled for clean look
            config.enableTopBar = false;
            config.enableInventory = false;
            config.showStandaloneResourcePanel = true;
            config.showHappiness = false;

            // Additional features
            config.enableNotifications = false;
            config.enableCustomCursor = true;
            config.enableWallPreview = false;

            // Performance
            config.hudUpdateRate = 30;
            config.enableAnimations = false;

            SaveAsset(config, ConfigPath, "MinimalHUDConfig.asset");
        }

        [MenuItem("Tools/RTS/Create HUD Templates/Configurations/Full (Maximum Info)")]
        public static void CreateFullConfiguration()
        {
            var config = ScriptableObject.CreateInstance<HUDConfiguration>();
            config.name = "FullHUDConfig";

            // Core components
            config.enableMinimap = true;
            config.enableUnitDetails = true;
            config.enableBuildingDetails = true;
            config.enableBuildingHUD = true;

            // Optional components - everything enabled
            config.enableTopBar = true;
            config.includeResourcesInTopBar = true;
            config.enableInventory = true;
            config.inventoryGridSize = new Vector2Int(3, 2);

            // Resource display
            config.showStandaloneResourcePanel = false; // Using top bar
            config.showHappiness = true;

            // Additional features
            config.enableNotifications = true;
            config.enableCustomCursor = true;
            config.enableWallPreview = true;

            // Performance
            config.hudUpdateRate = 30;
            config.enableAnimations = true;

            SaveAsset(config, ConfigPath, "FullHUDConfig.asset");
        }

        [MenuItem("Tools/RTS/Create HUD Templates/Configurations/Warcraft 3 Style")]
        public static void CreateWarcraft3Configuration()
        {
            var config = ScriptableObject.CreateInstance<HUDConfiguration>();
            config.name = "Warcraft3HUDConfig";

            // Core components
            config.enableMinimap = true;
            config.enableUnitDetails = true;
            config.enableBuildingDetails = true;
            config.enableBuildingHUD = true;

            // Warcraft 3 style features
            config.enableTopBar = true;
            config.includeResourcesInTopBar = true;
            config.enableInventory = true;
            config.inventoryGridSize = new Vector2Int(3, 2); // 6 slots like WC3

            // Resource display
            config.showStandaloneResourcePanel = false;
            config.showHappiness = true;

            // Additional features
            config.enableNotifications = true;
            config.enableCustomCursor = true;
            config.enableWallPreview = true;

            // Performance
            config.hudUpdateRate = 30;
            config.enableAnimations = true;

            SaveAsset(config, ConfigPath, "Warcraft3HUDConfig.asset");
        }

        #endregion

        #region Layout Templates

        [MenuItem("Tools/RTS/Create HUD Templates/Layouts/Warcraft 3 Style")]
        public static void CreateWarcraft3Layout()
        {
            var layout = ScriptableObject.CreateInstance<HUDLayoutPreset>();
            layout.presetName = "Warcraft 3 Style";
            layout.description = "Classic Warcraft 3 layout: Minimap bottom-left, unit info bottom-center, inventory bottom-right, top bar for resources";

            // Minimap - Bottom Left
            layout.minimapAnchor = HUDLayoutPreset.AnchorPosition.BottomLeft;
            layout.minimapSize = new Vector2(220, 220);
            layout.minimapOffset = new Vector2(10, 10);

            // Unit Details - Bottom Center
            layout.unitDetailsAnchor = HUDLayoutPreset.AnchorPosition.BottomCenter;
            layout.unitDetailsSize = new Vector2(380, 160);
            layout.unitDetailsOffset = new Vector2(0, 10);

            // Building Details - Bottom Center (same position as unit details)
            layout.buildingDetailsAnchor = HUDLayoutPreset.AnchorPosition.BottomCenter;
            layout.buildingDetailsSize = new Vector2(380, 200);
            layout.buildingDetailsOffset = new Vector2(0, 10);

            // Building HUD - Bottom Right
            layout.buildingHUDAnchor = HUDLayoutPreset.AnchorPosition.BottomRight;
            layout.buildingHUDSize = new Vector2(220, 220);
            layout.buildingHUDOffset = new Vector2(-10, 180);

            // Inventory - Bottom Right (above building HUD)
            layout.inventoryAnchor = HUDLayoutPreset.AnchorPosition.BottomRight;
            layout.inventorySize = new Vector2(220, 150);
            layout.inventoryOffset = new Vector2(-10, 10);

            // Top Bar
            layout.topBarHeight = 50;
            layout.topBarOffset = new Vector2(0, 0);

            // Resource Panel (Standalone) - Top Center
            layout.resourcePanelAnchor = HUDLayoutPreset.AnchorPosition.TopCenter;
            layout.resourcePanelSize = new Vector2(400, 60);
            layout.resourcePanelOffset = new Vector2(0, -10);

            // Notifications
            layout.notificationsAnchor = HUDLayoutPreset.AnchorPosition.TopCenter;
            layout.notificationsOffset = new Vector2(0, -70);

            SaveAsset(layout, LayoutPath, "Warcraft3Layout.asset");
        }

        [MenuItem("Tools/RTS/Create HUD Templates/Layouts/Modern RTS")]
        public static void CreateModernRTSLayout()
        {
            var layout = ScriptableObject.CreateInstance<HUDLayoutPreset>();
            layout.presetName = "Modern RTS";
            layout.description = "Modern RTS layout: Minimap right, info bottom, resources top-center";

            // Minimap - Middle Right
            layout.minimapAnchor = HUDLayoutPreset.AnchorPosition.MiddleRight;
            layout.minimapSize = new Vector2(250, 250);
            layout.minimapOffset = new Vector2(-10, 0);

            // Unit Details - Bottom Center
            layout.unitDetailsAnchor = HUDLayoutPreset.AnchorPosition.BottomCenter;
            layout.unitDetailsSize = new Vector2(500, 160);
            layout.unitDetailsOffset = new Vector2(0, 10);

            // Building Details - Bottom Center
            layout.buildingDetailsAnchor = HUDLayoutPreset.AnchorPosition.BottomCenter;
            layout.buildingDetailsSize = new Vector2(500, 200);
            layout.buildingDetailsOffset = new Vector2(0, 10);

            // Building HUD - Bottom Right
            layout.buildingHUDAnchor = HUDLayoutPreset.AnchorPosition.BottomRight;
            layout.buildingHUDSize = new Vector2(250, 300);
            layout.buildingHUDOffset = new Vector2(-10, 180);

            // Inventory - Bottom Right
            layout.inventoryAnchor = HUDLayoutPreset.AnchorPosition.BottomRight;
            layout.inventorySize = new Vector2(250, 160);
            layout.inventoryOffset = new Vector2(-10, 10);

            // Top Bar
            layout.topBarHeight = 60;
            layout.topBarOffset = new Vector2(0, 0);

            // Resource Panel - Top Center
            layout.resourcePanelAnchor = HUDLayoutPreset.AnchorPosition.TopCenter;
            layout.resourcePanelSize = new Vector2(450, 70);
            layout.resourcePanelOffset = new Vector2(0, -10);

            // Notifications
            layout.notificationsAnchor = HUDLayoutPreset.AnchorPosition.TopCenter;
            layout.notificationsOffset = new Vector2(0, -90);

            SaveAsset(layout, LayoutPath, "ModernRTSLayout.asset");
        }

        [MenuItem("Tools/RTS/Create HUD Templates/Layouts/Age of Empires Style")]
        public static void CreateAgeOfEmpiresLayout()
        {
            var layout = ScriptableObject.CreateInstance<HUDLayoutPreset>();
            layout.presetName = "Age of Empires Style";
            layout.description = "AoE-inspired layout: Minimap top-left, controls bottom-center, resources top";

            // Minimap - Top Left
            layout.minimapAnchor = HUDLayoutPreset.AnchorPosition.TopLeft;
            layout.minimapSize = new Vector2(200, 200);
            layout.minimapOffset = new Vector2(10, -60); // Below top bar

            // Unit Details - Bottom Center (wider)
            layout.unitDetailsAnchor = HUDLayoutPreset.AnchorPosition.BottomCenter;
            layout.unitDetailsSize = new Vector2(600, 140);
            layout.unitDetailsOffset = new Vector2(0, 10);

            // Building Details - Bottom Center
            layout.buildingDetailsAnchor = HUDLayoutPreset.AnchorPosition.BottomCenter;
            layout.buildingDetailsSize = new Vector2(600, 180);
            layout.buildingDetailsOffset = new Vector2(0, 10);

            // Building HUD - Bottom Center (part of main control area)
            layout.buildingHUDAnchor = HUDLayoutPreset.AnchorPosition.BottomCenter;
            layout.buildingHUDSize = new Vector2(600, 140);
            layout.buildingHUDOffset = new Vector2(0, 10);

            // Inventory - Bottom Right
            layout.inventoryAnchor = HUDLayoutPreset.AnchorPosition.BottomRight;
            layout.inventorySize = new Vector2(200, 140);
            layout.inventoryOffset = new Vector2(-10, 10);

            // Top Bar
            layout.topBarHeight = 50;
            layout.topBarOffset = new Vector2(0, 0);

            // Resource Panel - Top Left (next to minimap)
            layout.resourcePanelAnchor = HUDLayoutPreset.AnchorPosition.TopLeft;
            layout.resourcePanelSize = new Vector2(400, 50);
            layout.resourcePanelOffset = new Vector2(220, -10);

            // Notifications
            layout.notificationsAnchor = HUDLayoutPreset.AnchorPosition.TopCenter;
            layout.notificationsOffset = new Vector2(0, -60);

            SaveAsset(layout, LayoutPath, "AgeOfEmpiresLayout.asset");
        }

        [MenuItem("Tools/RTS/Create HUD Templates/Layouts/Compact (Small Screens)")]
        public static void CreateCompactLayout()
        {
            var layout = ScriptableObject.CreateInstance<HUDLayoutPreset>();
            layout.presetName = "Compact Layout";
            layout.description = "Compact layout for smaller screens or cleaner UI";

            // Minimap - Bottom Left (smaller)
            layout.minimapAnchor = HUDLayoutPreset.AnchorPosition.BottomLeft;
            layout.minimapSize = new Vector2(160, 160);
            layout.minimapOffset = new Vector2(5, 5);

            // Unit Details - Bottom Center (smaller)
            layout.unitDetailsAnchor = HUDLayoutPreset.AnchorPosition.BottomCenter;
            layout.unitDetailsSize = new Vector2(320, 130);
            layout.unitDetailsOffset = new Vector2(0, 5);

            // Building Details
            layout.buildingDetailsAnchor = HUDLayoutPreset.AnchorPosition.BottomCenter;
            layout.buildingDetailsSize = new Vector2(320, 160);
            layout.buildingDetailsOffset = new Vector2(0, 5);

            // Building HUD - Bottom Right (smaller)
            layout.buildingHUDAnchor = HUDLayoutPreset.AnchorPosition.BottomRight;
            layout.buildingHUDSize = new Vector2(180, 200);
            layout.buildingHUDOffset = new Vector2(-5, 5);

            // Inventory
            layout.inventoryAnchor = HUDLayoutPreset.AnchorPosition.BottomRight;
            layout.inventorySize = new Vector2(180, 130);
            layout.inventoryOffset = new Vector2(-5, 5);

            // Top Bar (thinner)
            layout.topBarHeight = 40;
            layout.topBarOffset = new Vector2(0, 0);

            // Resource Panel
            layout.resourcePanelAnchor = HUDLayoutPreset.AnchorPosition.TopCenter;
            layout.resourcePanelSize = new Vector2(350, 50);
            layout.resourcePanelOffset = new Vector2(0, -5);

            // Notifications
            layout.notificationsAnchor = HUDLayoutPreset.AnchorPosition.TopCenter;
            layout.notificationsOffset = new Vector2(0, -60);

            SaveAsset(layout, LayoutPath, "CompactLayout.asset");
        }

        #endregion

        #region Utility Methods

        private static void SaveAsset(Object asset, string path, string filename)
        {
            // Ensure directory exists
            if (!AssetDatabase.IsValidFolder(path.TrimEnd('/')))
            {
                string[] folders = path.Split('/');
                string currentPath = folders[0];

                for (int i = 1; i < folders.Length; i++)
                {
                    if (folders[i] == "") continue;

                    string newPath = currentPath + "/" + folders[i];
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, folders[i]);
                    }
                    currentPath = newPath;
                }
            }

            string fullPath = path + filename;

            // Check if asset already exists
            Object existingAsset = AssetDatabase.LoadAssetAtPath(fullPath, asset.GetType());
            if (existingAsset != null)
            {
                EditorUtility.CopySerialized(asset, existingAsset);
                AssetDatabase.SaveAssets();
                Debug.Log($"Updated existing asset: {fullPath}");
            }
            else
            {
                AssetDatabase.CreateAsset(asset, fullPath);
                Debug.Log($"Created new asset: {fullPath}");
            }
        }

        #endregion
    }
}
#endif
