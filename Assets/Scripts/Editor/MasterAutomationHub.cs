using UnityEngine;
using UnityEditor;

namespace RTS.Editor
{
    /// <summary>
    /// Master Automation Hub - Central access point for all RTS automation tools.
    /// Provides quick access to all setup and generation tools.
    /// Access via: Tools > RTS > Automation Hub
    /// </summary>
    public class MasterAutomationHub : EditorWindow
    {
        private enum CategoryFilter
        {
            All,
            SystemSetup,
            UIGeneration,
            SceneTools,
            BuildingTools,
            PackageTools
        }

        private CategoryFilter currentFilter = CategoryFilter.All;
        private Vector2 scrollPos;
        private string searchQuery = "";

        private GUIStyle headerStyle;
        private GUIStyle categoryStyle;
        private GUIStyle toolButtonStyle;
        private GUIStyle descriptionStyle;

        [MenuItem("Tools/RTS/Automation Hub", priority = 0)]
        public static void ShowWindow()
        {
            MasterAutomationHub window = GetWindow<MasterAutomationHub>("RTS Automation Hub");
            window.minSize = new Vector2(600, 700);
        }

        private void OnEnable()
        {
            InitializeStyles();
        }

        private void InitializeStyles()
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };

            categoryStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = new Color(0.3f, 0.7f, 1f) }
            };

            toolButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                padding = new RectOffset(10, 10, 8, 8)
            };

            descriptionStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 10,
                wordWrap = true,
                normal = { textColor = Color.gray }
            };
        }

        private void OnGUI()
        {
            if (headerStyle == null) InitializeStyles();

            DrawHeader();
            GUILayout.Space(10);

            DrawFilterBar();
            GUILayout.Space(10);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            DrawToolsList();

            EditorGUILayout.EndScrollView();

            GUILayout.Space(10);
            DrawFooter();
        }

        #region UI Drawing

        private void DrawHeader()
        {
            GUILayout.Space(10);
            GUILayout.Label("🛠️ RTS Automation Hub", headerStyle);
            GUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "Welcome to the RTS Automation Hub!\n" +
                "Access all automation tools from one central location.\n" +
                "Click any tool below to launch it.",
                MessageType.Info);
        }

        private void DrawFilterBar()
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label("Filter:", GUILayout.Width(50));
            currentFilter = (CategoryFilter)EditorGUILayout.EnumPopup(currentFilter, GUILayout.Width(150));

            GUILayout.Space(10);

            GUILayout.Label("Search:", GUILayout.Width(50));
            searchQuery = EditorGUILayout.TextField(searchQuery);

            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                searchQuery = "";
                currentFilter = CategoryFilter.All;
            }

            GUILayout.EndHorizontal();
        }

        private void DrawToolsList()
        {
            // System Setup Tools
            if (ShouldShowCategory(CategoryFilter.SystemSetup))
            {
                DrawCategory("🎮 System Setup Tools", new ToolInfo[]
                {
                    new ToolInfo
                    {
                        name = "Unit System Setup",
                        description = "Create complete unit prefabs with AI, combat, health, and movement components. Supports batch configuration.",
                        action = () => UnitSystemSetupTool.ShowWindow(),
                        keywords = "unit ai combat health movement"
                    },
                    new ToolInfo
                    {
                        name = "Manager Setup",
                        description = "Set up GameManager and all service managers (Resource, Happiness, Building, Wave). Complete hierarchy creation.",
                        action = () => ManagerSetupTool.ShowWindow(),
                        keywords = "manager gamemanager service resource happiness"
                    },
                    new ToolInfo
                    {
                        name = "Camera System Setup",
                        description = "Configure RTS camera with movement, zoom, rotation, and input bindings. Supports edge scrolling and bounds.",
                        action = () => CameraSystemSetupTool.ShowWindow(),
                        keywords = "camera rts movement zoom rotation"
                    }
                });
            }

            // UI Generation Tools
            if (ShouldShowCategory(CategoryFilter.UIGeneration))
            {
                DrawCategory("🎨 UI Generation Tools", new ToolInfo[]
                {
                    new ToolInfo
                    {
                        name = "UI System Generator",
                        description = "Generate Resource UI, Happiness UI, Notification UI, or complete game HUD. Fully styled and functional.",
                        action = () => UISystemGenerator.ShowWindow(),
                        keywords = "ui resource happiness notification hud"
                    },
                    new ToolInfo
                    {
                        name = "Building HUD Setup",
                        description = "Create complete building construction UI with buttons, tooltips, and placement info. Auto-generates prefabs.",
                        action = () => BuildingHUDSetup.ShowWindow(),
                        keywords = "building hud ui construction"
                    },
                    new ToolInfo
                    {
                        name = "Building Training UI Setup",
                        description = "Generate building training UI with unit buttons, progress bars, and queue display. Perfect for barracks/production.",
                        action = () => BuildingTrainingUISetup.ShowWindow(),
                        keywords = "training unit building ui queue"
                    }
                });
            }

            // Building Tools
            if (ShouldShowCategory(CategoryFilter.BuildingTools))
            {
                DrawCategory("🏗️ Building System Tools", new ToolInfo[]
                {
                    new ToolInfo
                    {
                        name = "Wall Prefab Setup",
                        description = "Create wall prefabs with 16 connection variants. Supports auto-generation or custom meshes.",
                        action = () => WallPrefabSetupUtility.ShowWindow(),
                        keywords = "wall prefab connection variants mesh"
                    }
                });
            }

            // Scene Tools
            if (ShouldShowCategory(CategoryFilter.SceneTools))
            {
                DrawCategory("🎬 Scene Tools", new ToolInfo[]
                {
                    new ToolInfo
                    {
                        name = "Scene Template Generator",
                        description = "Generate complete game scenes with managers, UI, camera, and lighting. Choose from templates or customize.",
                        action = () => SceneTemplateGenerator.ShowWindow(),
                        keywords = "scene template complete setup"
                    }
                });
            }

            // Package Tools
            if (ShouldShowCategory(CategoryFilter.PackageTools))
            {
                DrawCategory("📦 Package Tools", new ToolInfo[]
                {
                    new ToolInfo
                    {
                        name = "Standalone System Extractor",
                        description = "Extract systems into standalone Unity packages. Creates complete package structure with docs and samples.",
                        action = () => StandaloneSystemExtractor.ShowWindow(),
                        keywords = "package extract standalone system export"
                    }
                });
            }

            // Quick Actions
            GUILayout.Space(20);
            DrawQuickActions();
        }

        private void DrawCategory(string categoryName, ToolInfo[] tools)
        {
            GUILayout.Space(10);
            GUILayout.Label(categoryName, categoryStyle);
            GUILayout.Space(5);

            foreach (var tool in tools)
            {
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    string searchLower = searchQuery.ToLower();
                    if (!tool.name.ToLower().Contains(searchLower) &&
                        !tool.description.ToLower().Contains(searchLower) &&
                        !tool.keywords.ToLower().Contains(searchLower))
                    {
                        continue;
                    }
                }

                DrawToolButton(tool);
            }
        }

        private void DrawToolButton(ToolInfo tool)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);

            if (GUILayout.Button(tool.name, toolButtonStyle, GUILayout.Height(30)))
            {
                tool.action?.Invoke();
            }

            GUILayout.Label(tool.description, descriptionStyle);

            GUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private void DrawQuickActions()
        {
            GUILayout.Label("⚡ Quick Actions", categoryStyle);
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("📖 Documentation", GUILayout.Height(30)))
            {
                Application.OpenURL("https://github.com/YourRepo/KingdomsAtDusk/wiki");
            }

            if (GUILayout.Button("🐛 Report Issue", GUILayout.Height(30)))
            {
                Application.OpenURL("https://github.com/YourRepo/KingdomsAtDusk/issues");
            }

            if (GUILayout.Button("💡 Suggest Feature", GUILayout.Height(30)))
            {
                Application.OpenURL("https://github.com/YourRepo/KingdomsAtDusk/issues/new");
            }

            GUILayout.EndHorizontal();
        }

        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            GUILayout.Label($"🎯 {GetToolCount()} automation tools available", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label("RTS Automation Hub v1.0", EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Helper Methods

        private bool ShouldShowCategory(CategoryFilter category)
        {
            return currentFilter == CategoryFilter.All || currentFilter == category;
        }

        private int GetToolCount()
        {
            return 9; // Update this as tools are added
        }

        #endregion

        #region Data Structures

        private class ToolInfo
        {
            public string name;
            public string description;
            public System.Action action;
            public string keywords;
        }

        #endregion
    }
}
