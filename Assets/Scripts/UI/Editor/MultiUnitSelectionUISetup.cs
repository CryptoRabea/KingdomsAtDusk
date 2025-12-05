using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;

namespace RTS.UI.Editor
{
    /// <summary>
    /// Automated setup tool for Multi-Unit Selection UI system.
    /// Creates prefabs, configures UI components, and sets up scene references automatically.
    /// Access via: Tools > RTS > Setup Multi-Unit Selection UI
    /// </summary>
    public class MultiUnitSelectionUISetup : UnityEditor.Editor
    {
        private const string PREFABS_PATH = "Assets/Prefabs/UI";
        private const string UNIT_ICON_PREFAB_NAME = "UnitIconWithHP.prefab";

        [MenuItem("Tools/RTS/Setup Multi-Unit Selection UI")]
        public static void SetupMultiUnitSelectionUI()
        {
            Debug.Log("[MultiUnitSelectionUISetup] Starting automated setup...");

            // Ensure Prefabs directory exists
            if (!AssetDatabase.IsValidFolder(PREFABS_PATH))
            {
                string[] folders = PREFABS_PATH.Split('/');
                string currentPath = folders[0];

                for (int i = 1; i < folders.Length; i++)
                {
                    string newPath = currentPath + "/" + folders[i];
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, folders[i]);
                    }
                    currentPath = newPath;
                }
                AssetDatabase.Refresh();
            }

            // Step 1: Create Unit Icon Prefab
            GameObject unitIconPrefab = CreateUnitIconPrefab();
            if (unitIconPrefab == null)
            {
                Debug.LogError("[MultiUnitSelectionUISetup] Failed to create Unit Icon Prefab!");
                return;
            }

            // Step 2: Find or create Canvas
            Canvas canvas = FindOrCreateCanvas();
            if (canvas == null)
            {
                Debug.LogError("[MultiUnitSelectionUISetup] Failed to find or create Canvas!");
                return;
            }

            // Step 3: Create Multi-Unit Selection Panel
            GameObject multiUnitPanel = CreateMultiUnitSelectionPanel(canvas, unitIconPrefab);
            if (multiUnitPanel == null)
            {
                Debug.LogError("[MultiUnitSelectionUISetup] Failed to create Multi-Unit Selection Panel!");
                return;
            }

            // Step 4: Select the created panel in hierarchy
            Selection.activeGameObject = multiUnitPanel;

            Debug.Log("[MultiUnitSelectionUISetup] ✅ Setup complete! Multi-Unit Selection UI is ready to use.");
            Debug.Log($"[MultiUnitSelectionUISetup] Panel created: {multiUnitPanel.name}");
            Debug.Log($"[MultiUnitSelectionUISetup] Prefab created: {PREFABS_PATH}/{UNIT_ICON_PREFAB_NAME}");

            EditorUtility.DisplayDialog(
                "Setup Complete!",
                "Multi-Unit Selection UI has been set up successfully!\n\n" +
                "✅ Unit Icon Prefab created\n" +
                "✅ Selection Panel configured\n" +
                "✅ Grid Layout set up\n" +
                "✅ All references assigned\n\n" +
                "Test it by selecting multiple units in Play Mode!",
                "OK"
            );
        }

        /// <summary>
        /// Creates the UnitIconWithHP prefab with all child objects and components.
        /// </summary>
        private static GameObject CreateUnitIconPrefab()
        {
            Debug.Log("[MultiUnitSelectionUISetup] Creating Unit Icon Prefab...");

            // Check if prefab already exists
            string prefabPath = Path.Combine(PREFABS_PATH, UNIT_ICON_PREFAB_NAME);
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (existingPrefab != null)
            {
                Debug.Log("[MultiUnitSelectionUISetup] Unit Icon Prefab already exists. Using existing prefab.");
                return existingPrefab;
            }

            // Create root GameObject
            GameObject root = new GameObject("UnitIconWithHP");
            RectTransform rootRect = root.AddComponent<RectTransform>();
            Image rootImage = root.AddComponent<Image>();

            // Configure root
            rootRect.sizeDelta = new Vector2(64, 64);
            rootImage.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Dark background

            // Create UnitIcon (portrait)
            GameObject unitIconObj = new GameObject("UnitIcon");
            unitIconObj.transform.SetParent(root.transform, false);
            RectTransform iconRect = unitIconObj.AddComponent<RectTransform>();
            Image iconImage = unitIconObj.AddComponent<Image>();

            // Configure icon - fills parent with small padding
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(2, 10); // Padding: left, bottom (leave space for HP bar)
            iconRect.offsetMax = new Vector2(-2, -2); // Padding: right, top
            iconImage.color = Color.white;
            iconImage.preserveAspect = true;

            // Create HPBar background
            GameObject hpBarBg = new GameObject("HPBar");
            hpBarBg.transform.SetParent(root.transform, false);
            RectTransform hpBarBgRect = hpBarBg.AddComponent<RectTransform>();
            Image hpBarBgImage = hpBarBg.AddComponent<Image>();

            // Configure HP bar background - positioned at bottom
            hpBarBgRect.anchorMin = new Vector2(0, 0);
            hpBarBgRect.anchorMax = new Vector2(1, 0);
            hpBarBgRect.pivot = new Vector2(0.5f, 0);
            hpBarBgRect.anchoredPosition = new Vector2(0, 2);
            hpBarBgRect.sizeDelta = new Vector2(-4, 6); // Width: full - padding, Height: 6px
            hpBarBgImage.color = new Color(0.2f, 0f, 0f, 1f); // Dark red

            // Create HPBarFill
            GameObject hpBarFill = new GameObject("HPBarFill");
            hpBarFill.transform.SetParent(hpBarBg.transform, false);
            RectTransform hpBarFillRect = hpBarFill.AddComponent<RectTransform>();
            Image hpBarFillImage = hpBarFill.AddComponent<Image>();

            // Configure HP bar fill
            hpBarFillRect.anchorMin = new Vector2(0, 0);
            hpBarFillRect.anchorMax = new Vector2(1, 1);
            hpBarFillRect.offsetMin = Vector2.zero;
            hpBarFillRect.offsetMax = Vector2.zero;
            hpBarFillImage.color = Color.green;
            hpBarFillImage.type = Image.Type.Filled;
            hpBarFillImage.fillMethod = Image.FillMethod.Horizontal;
            hpBarFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;

            // Add UnitIconWithHP component
            UnitIconWithHP iconComponent = root.AddComponent<UnitIconWithHP>();

            // Use reflection to set private fields (since they're SerializeField)
            var type = typeof(UnitIconWithHP);
            var unitIconField = type.GetField("unitIcon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var hpBarFillField = type.GetField("hpBarFill", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var hpBarBackgroundField = type.GetField("hpBarBackground", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (unitIconField != null) unitIconField.SetValue(iconComponent, iconImage);
            if (hpBarFillField != null) hpBarFillField.SetValue(iconComponent, hpBarFillImage);
            if (hpBarBackgroundField != null) hpBarBackgroundField.SetValue(iconComponent, hpBarBgImage);

            // Save as prefab
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

            // Clean up scene object
            DestroyImmediate(root);

            // Load and return the prefab
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Debug.Log($"[MultiUnitSelectionUISetup] ✅ Unit Icon Prefab created at: {prefabPath}");

            return prefab;
        }

        /// <summary>
        /// Finds existing Canvas or creates a new one.
        /// </summary>
        private static Canvas FindOrCreateCanvas()
        {
            // Try to find existing Canvas
            Canvas canvas = FindFirstObjectByType<Canvas>();

            if (canvas != null)
            {
                Debug.Log($"[MultiUnitSelectionUISetup] Found existing Canvas: {canvas.name}");
                return canvas;
            }

            // Create new Canvas
            Debug.Log("[MultiUnitSelectionUISetup] Creating new Canvas...");
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            Debug.Log("[MultiUnitSelectionUISetup] ✅ Canvas created");
            return canvas;
        }

        /// <summary>
        /// Creates the Multi-Unit Selection Panel with all components configured.
        /// </summary>
        private static GameObject CreateMultiUnitSelectionPanel(Canvas canvas, GameObject unitIconPrefab)
        {
            Debug.Log("[MultiUnitSelectionUISetup] Creating Multi-Unit Selection Panel...");

            // Check if panel already exists
            Transform existing = canvas.transform.Find("MultiUnitSelectionPanel");
            if (existing != null)
            {
                Debug.LogWarning("[MultiUnitSelectionUISetup] MultiUnitSelectionPanel already exists. Updating configuration...");
                return UpdateExistingPanel(existing.gameObject, unitIconPrefab);
            }

            // Create main panel
            GameObject panel = new GameObject("MultiUnitSelectionPanel");
            panel.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            Image panelImage = panel.AddComponent<Image>();

            // Configure panel - positioned at bottom-left corner
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(0, 0);
            panelRect.pivot = new Vector2(0, 0);
            panelRect.anchoredPosition = new Vector2(20, 20);
            panelRect.sizeDelta = new Vector2(300, 200); // Will auto-adjust based on grid

            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f); // Semi-transparent dark background

            // Create icon container
            GameObject container = new GameObject("UnitIconContainer");
            container.transform.SetParent(panel.transform, false);
            RectTransform containerRect = container.AddComponent<RectTransform>();

            // Configure container to fill parent with padding
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = new Vector2(10, 10);
            containerRect.offsetMax = new Vector2(-10, -10);

            // Add GridLayoutGroup
            GridLayoutGroup grid = container.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(64, 64);
            grid.spacing = new Vector2(8, 8);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4; // 4 columns

            // Add MultiUnitSelectionUI component
            MultiUnitSelectionUI selectionUI = panel.AddComponent<MultiUnitSelectionUI>();

            // Use reflection to set private fields
            var type = typeof(MultiUnitSelectionUI);
            var multiUnitPanelField = type.GetField("multiUnitPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var unitIconContainerField = type.GetField("unitIconContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var unitIconPrefabField = type.GetField("unitIconPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxIconsField = type.GetField("maxIconsToDisplay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var autoHideField = type.GetField("autoHideWhenEmpty", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var showOnlyMultipleField = type.GetField("showOnlyWhenMultiple", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var gridLayoutField = type.GetField("gridLayoutGroup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (multiUnitPanelField != null) multiUnitPanelField.SetValue(selectionUI, panel);
            if (unitIconContainerField != null) unitIconContainerField.SetValue(selectionUI, container.transform);
            if (unitIconPrefabField != null) unitIconPrefabField.SetValue(selectionUI, unitIconPrefab);
            if (maxIconsField != null) maxIconsField.SetValue(selectionUI, 12);
            if (autoHideField != null) autoHideField.SetValue(selectionUI, true);
            if (showOnlyMultipleField != null) showOnlyMultipleField.SetValue(selectionUI, true);
            if (gridLayoutField != null) gridLayoutField.SetValue(selectionUI, grid);

            // Mark the object as dirty to ensure changes are saved
            EditorUtility.SetDirty(panel);

            Debug.Log("[MultiUnitSelectionUISetup] ✅ Multi-Unit Selection Panel created and configured");
            return panel;
        }

        /// <summary>
        /// Updates an existing panel with the correct configuration.
        /// </summary>
        private static GameObject UpdateExistingPanel(GameObject panel, GameObject unitIconPrefab)
        {
            // Get or add MultiUnitSelectionUI component
            MultiUnitSelectionUI selectionUI = panel.GetComponent<MultiUnitSelectionUI>();
            if (selectionUI == null)
            {
                selectionUI = panel.AddComponent<MultiUnitSelectionUI>();
            }

            // Find or create container
            Transform containerTransform = panel.transform.Find("UnitIconContainer");
            GameObject container;

            if (containerTransform == null)
            {
                container = new GameObject("UnitIconContainer");
                container.transform.SetParent(panel.transform, false);
                RectTransform containerRect = container.AddComponent<RectTransform>();
                containerRect.anchorMin = Vector2.zero;
                containerRect.anchorMax = Vector2.one;
                containerRect.offsetMin = new Vector2(10, 10);
                containerRect.offsetMax = new Vector2(-10, -10);

                GridLayoutGroup grid = container.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(64, 64);
                grid.spacing = new Vector2(8, 8);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 4;
            }
            else
            {
                container = containerTransform.gameObject;
            }

            // Update references
            var type = typeof(MultiUnitSelectionUI);
            var multiUnitPanelField = type.GetField("multiUnitPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var unitIconContainerField = type.GetField("unitIconContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var unitIconPrefabField = type.GetField("unitIconPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (multiUnitPanelField != null) multiUnitPanelField.SetValue(selectionUI, panel);
            if (unitIconContainerField != null) unitIconContainerField.SetValue(selectionUI, container.transform);
            if (unitIconPrefabField != null) unitIconPrefabField.SetValue(selectionUI, unitIconPrefab);

            EditorUtility.SetDirty(panel);
            Debug.Log("[MultiUnitSelectionUISetup] ✅ Existing panel updated");

            return panel;
        }

        /// <summary>
        /// Menu item to clean up/remove the Multi-Unit Selection UI.
        /// </summary>
        [MenuItem("Tools/RTS/Remove Multi-Unit Selection UI")]
        public static void RemoveMultiUnitSelectionUI()
        {
            // Find and remove the panel
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform panel = canvas.transform.Find("MultiUnitSelectionPanel");
                if (panel != null)
                {
                    DestroyImmediate(panel.gameObject);
                    Debug.Log("[MultiUnitSelectionUISetup] Multi-Unit Selection Panel removed from scene");
                }
            }

            EditorUtility.DisplayDialog(
                "Cleanup Complete",
                "Multi-Unit Selection Panel has been removed from the scene.\n\n" +
                "The prefab is still available if you want to set it up again.",
                "OK"
            );
        }
    }
}
