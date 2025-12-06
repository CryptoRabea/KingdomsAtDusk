using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;
using TMPro;

namespace RTS.UI.Editor
{
    /// <summary>
    /// Automated setup tool for Multi-Unit Selection UI system.
    /// Integrates with existing UnitDetailsUI panel - replaces stats with unit grid when 2+ units selected.
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
            EnsurePrefabsDirectory();

            // Step 1: Create Unit Icon Prefab
            GameObject unitIconPrefab = CreateUnitIconPrefab();
            if (unitIconPrefab == null)
            {
                Debug.LogError("[MultiUnitSelectionUISetup] Failed to create Unit Icon Prefab!");
                return;
            }

            // Step 2: Find UnitDetailsUI in the scene
            UnitDetailsUI unitDetailsUI = FindFirstObjectByType<UnitDetailsUI>();
            if (unitDetailsUI == null)
            {
                EditorUtility.DisplayDialog(
                    "Setup Failed",
                    "Could not find UnitDetailsUI in the scene!\n\n" +
                    "Please ensure you have a UnitDetailsUI component in your scene before running this setup.",
                    "OK"
                );
                Debug.LogError("[MultiUnitSelectionUISetup] UnitDetailsUI not found in scene!");
                return;
            }

            // Step 3: Integrate Multi-Unit Selection into UnitDetailsUI
            bool success = IntegrateWithUnitDetailsUI(unitDetailsUI, unitIconPrefab);
            if (!success)
            {
                Debug.LogError("[MultiUnitSelectionUISetup] Failed to integrate with UnitDetailsUI!");
                return;
            }

            // Step 4: Select the UnitDetailsUI in hierarchy
            Selection.activeGameObject = unitDetailsUI.gameObject;

            Debug.Log("[MultiUnitSelectionUISetup] ✅ Setup complete! Multi-Unit Selection UI is ready to use.");
            Debug.Log($"[MultiUnitSelectionUISetup] Prefab created: {PREFABS_PATH}/{UNIT_ICON_PREFAB_NAME}");

            EditorUtility.DisplayDialog(
                "Setup Complete!",
                "Multi-Unit Selection UI has been integrated with UnitDetailsUI!\n\n" +
                "✅ Unit Icon Prefab created\n" +
                "✅ Stats container created\n" +
                "✅ Multi-unit container created\n" +
                "✅ Grid Layout configured\n" +
                "✅ All references assigned\n\n" +
                "How it works:\n" +
                "• 1 unit selected: Shows normal stats\n" +
                "• 2+ units selected: Shows unit icon grid\n" +
                "• Formation buttons stay visible\n\n" +
                "Test it by selecting multiple units in Play Mode!",
                "OK"
            );
        }

        private static void EnsurePrefabsDirectory()
        {
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
        }

        /// <summary>
        /// Integrates Multi-Unit Selection UI into existing UnitDetailsUI panel.
        /// </summary>
        private static bool IntegrateWithUnitDetailsUI(UnitDetailsUI unitDetailsUI, GameObject unitIconPrefab)
        {
            Debug.Log("[MultiUnitSelectionUISetup] Integrating with UnitDetailsUI...");

            // Get the UnitDetailsUI game object
            GameObject unitDetailsPanel = unitDetailsUI.gameObject;

            // Use reflection to get the stats UI elements
            var type = typeof(UnitDetailsUI);
            var portraitField = type.GetField("unitPortrait", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var nameTextField = type.GetField("unitNameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var healthTextField = type.GetField("healthText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Image portrait = portraitField?.GetValue(unitDetailsUI) as Image;

            if (portrait == null)
            {
                Debug.LogError("[MultiUnitSelectionUISetup] Could not find unit portrait in UnitDetailsUI!");
                return false;
            }

            // Find the stats container - we'll look for the parent that contains all stat elements
            Transform statsParent = FindStatsContainer(unitDetailsPanel.transform);

            if (statsParent == null)
            {
                Debug.LogWarning("[MultiUnitSelectionUISetup] Could not auto-detect stats container. Creating manual setup...");
                return CreateManualSetup(unitDetailsUI, unitIconPrefab);
            }

            // Create containers
            GameObject singleUnitContainer = CreateSingleUnitStatsContainer(statsParent);
            GameObject multiUnitContainer = CreateMultiUnitSelectionContainer(unitDetailsPanel.transform, unitIconPrefab);

            // Add MultiUnitSelectionUI component to the multi-unit container
            MultiUnitSelectionUI multiUnitUI = multiUnitContainer.GetComponent<MultiUnitSelectionUI>();
            if (multiUnitUI == null)
            {
                multiUnitUI = multiUnitContainer.AddComponent<MultiUnitSelectionUI>();
            }

            // Assign references to UnitDetailsUI
            var singleUnitContainerField = type.GetField("singleUnitStatsContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var multiUnitContainerField = type.GetField("multiUnitSelectionContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var multiUnitUIField = type.GetField("multiUnitSelectionUI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (singleUnitContainerField != null) singleUnitContainerField.SetValue(unitDetailsUI, singleUnitContainer);
            if (multiUnitContainerField != null) multiUnitContainerField.SetValue(unitDetailsUI, multiUnitContainer);
            if (multiUnitUIField != null) multiUnitUIField.SetValue(unitDetailsUI, multiUnitUI);

            // Mark as dirty
            EditorUtility.SetDirty(unitDetailsUI);

            Debug.Log("[MultiUnitSelectionUISetup] ✅ Integration complete!");
            return true;
        }

        /// <summary>
        /// Finds the container that holds all the stat UI elements.
        /// </summary>
        private static Transform FindStatsContainer(Transform root)
        {
            // Look for TextMeshProUGUI elements (health, speed, etc.)
            TextMeshProUGUI[] textElements = root.GetComponentsInChildren<TextMeshProUGUI>(true);

            foreach (var text in textElements)
            {
                if (text.name.ToLower().Contains("health") ||
                    text.name.ToLower().Contains("speed") ||
                    text.name.ToLower().Contains("attack"))
                {
                    // Found a stat element - return its parent
                    Transform parent = text.transform.parent;

                    // Check if this parent contains multiple stat elements
                    TextMeshProUGUI[] siblings = parent.GetComponentsInChildren<TextMeshProUGUI>(false);
                    if (siblings.Length >= 3) // At least 3 stat elements
                    {
                        return parent;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a container for single unit stats by wrapping existing elements.
        /// </summary>
        private static GameObject CreateSingleUnitStatsContainer(Transform statsParent)
        {
            // Check if container already exists
            Transform existing = statsParent.Find("SingleUnitStatsContainer");
            if (existing != null)
            {
                Debug.Log("[MultiUnitSelectionUISetup] SingleUnitStatsContainer already exists.");
                return existing.gameObject;
            }

            // Create container
            GameObject container = new GameObject("SingleUnitStatsContainer");
            container.transform.SetParent(statsParent.parent, false);

            RectTransform containerRect = container.AddComponent<RectTransform>();

            // Match parent's rect
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            // Move stats parent into this container
            int siblingIndex = statsParent.GetSiblingIndex();
            statsParent.SetParent(container.transform, true);
            container.transform.SetSiblingIndex(siblingIndex);

            Debug.Log("[MultiUnitSelectionUISetup] Created SingleUnitStatsContainer");
            return container;
        }

        /// <summary>
        /// Creates the multi-unit selection container with grid layout.
        /// </summary>
        private static GameObject CreateMultiUnitSelectionContainer(Transform parent, GameObject unitIconPrefab)
        {
            // Check if container already exists
            Transform existing = parent.Find("MultiUnitSelectionContainer");
            if (existing != null)
            {
                Debug.Log("[MultiUnitSelectionUISetup] MultiUnitSelectionContainer already exists. Updating...");
                UpdateMultiUnitContainer(existing.gameObject, unitIconPrefab);
                return existing.gameObject;
            }

            // Create main container
            GameObject container = new GameObject("MultiUnitSelectionContainer");
            container.transform.SetParent(parent, false);
            RectTransform containerRect = container.AddComponent<RectTransform>();

            // Position in the stats area
            containerRect.anchorMin = new Vector2(0, 0);
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.offsetMin = new Vector2(10, 10);
            containerRect.offsetMax = new Vector2(-10, -10);

            // Start hidden
            container.SetActive(false);

            // Create icon grid container
            GameObject gridContainer = new GameObject("UnitIconGrid");
            gridContainer.transform.SetParent(container.transform, false);
            RectTransform gridRect = gridContainer.AddComponent<RectTransform>();

            gridRect.anchorMin = Vector2.zero;
            gridRect.anchorMax = Vector2.one;
            gridRect.offsetMin = Vector2.zero;
            gridRect.offsetMax = Vector2.zero;

            // Add GridLayoutGroup
            GridLayoutGroup grid = gridContainer.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(64, 64);
            grid.spacing = new Vector2(8, 8);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4; // 4 columns
            grid.padding = new RectOffset(5, 5, 5, 5);

            // Add ContentSizeFitter for dynamic sizing
            ContentSizeFitter fitter = gridContainer.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Add MultiUnitSelectionUI component to main container
            MultiUnitSelectionUI multiUnitUI = container.AddComponent<MultiUnitSelectionUI>();

            // Assign references using reflection
            var type = typeof(MultiUnitSelectionUI);
            var iconContainerField = type.GetField("unitIconContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var iconPrefabField = type.GetField("unitIconPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxIconsField = type.GetField("maxIconsToDisplay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var gridLayoutField = type.GetField("gridLayoutGroup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (iconContainerField != null) iconContainerField.SetValue(multiUnitUI, gridContainer.transform);
            if (iconPrefabField != null) iconPrefabField.SetValue(multiUnitUI, unitIconPrefab);
            if (maxIconsField != null) maxIconsField.SetValue(multiUnitUI, 12);
            if (gridLayoutField != null) gridLayoutField.SetValue(multiUnitUI, grid);

            EditorUtility.SetDirty(container);

            Debug.Log("[MultiUnitSelectionUISetup] Created MultiUnitSelectionContainer");
            return container;
        }

        /// <summary>
        /// Updates existing multi-unit container with proper references.
        /// </summary>
        private static void UpdateMultiUnitContainer(GameObject container, GameObject unitIconPrefab)
        {
            MultiUnitSelectionUI multiUnitUI = container.GetComponent<MultiUnitSelectionUI>();
            if (multiUnitUI == null)
            {
                multiUnitUI = container.AddComponent<MultiUnitSelectionUI>();
            }

            Transform gridContainer = container.transform.Find("UnitIconGrid");
            if (gridContainer != null)
            {
                GridLayoutGroup grid = gridContainer.GetComponent<GridLayoutGroup>();

                var type = typeof(MultiUnitSelectionUI);
                var iconContainerField = type.GetField("unitIconContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var iconPrefabField = type.GetField("unitIconPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var gridLayoutField = type.GetField("gridLayoutGroup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (iconContainerField != null) iconContainerField.SetValue(multiUnitUI, gridContainer);
                if (iconPrefabField != null) iconPrefabField.SetValue(multiUnitUI, unitIconPrefab);
                if (gridLayoutField != null && grid != null) gridLayoutField.SetValue(multiUnitUI, grid);

                EditorUtility.SetDirty(container);
            }
        }

        /// <summary>
        /// Fallback: Creates a basic setup when auto-detection fails.
        /// </summary>
        private static bool CreateManualSetup(UnitDetailsUI unitDetailsUI, GameObject unitIconPrefab)
        {
            Debug.Log("[MultiUnitSelectionUISetup] Creating manual setup...");

            GameObject panel = unitDetailsUI.gameObject;

            // Create simple containers
            GameObject singleContainer = new GameObject("SingleUnitStatsContainer");
            singleContainer.transform.SetParent(panel.transform, false);
            RectTransform singleRect = singleContainer.AddComponent<RectTransform>();
            singleRect.anchorMin = Vector2.zero;
            singleRect.anchorMax = Vector2.one;
            singleRect.offsetMin = Vector2.zero;
            singleRect.offsetMax = Vector2.zero;

            GameObject multiContainer = CreateMultiUnitSelectionContainer(panel.transform, unitIconPrefab);

            // Assign to UnitDetailsUI
            var type = typeof(UnitDetailsUI);
            var singleField = type.GetField("singleUnitStatsContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var multiField = type.GetField("multiUnitSelectionContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var multiUIField = type.GetField("multiUnitSelectionUI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (singleField != null) singleField.SetValue(unitDetailsUI, singleContainer);
            if (multiField != null) multiField.SetValue(unitDetailsUI, multiContainer);

            MultiUnitSelectionUI multiUI = multiContainer.GetComponent<MultiUnitSelectionUI>();
            if (multiUIField != null && multiUI != null) multiUIField.SetValue(unitDetailsUI, multiUI);

            EditorUtility.SetDirty(unitDetailsUI);

            Debug.LogWarning("[MultiUnitSelectionUISetup] Manual setup complete. You may need to manually organize UI elements in the Inspector.");
            return true;
        }

        /// <summary>
        /// Creates the UnitIconWithHP prefab.
        /// </summary>
        private static GameObject CreateUnitIconPrefab()
        {
            Debug.Log("[MultiUnitSelectionUISetup] Creating Unit Icon Prefab...");

            string prefabPath = Path.Combine(PREFABS_PATH, UNIT_ICON_PREFAB_NAME);
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (existingPrefab != null)
            {
                Debug.Log("[MultiUnitSelectionUISetup] Unit Icon Prefab already exists.");
                return existingPrefab;
            }

            // Create root
            GameObject root = new GameObject("UnitIconWithHP");
            RectTransform rootRect = root.AddComponent<RectTransform>();
            Image rootImage = root.AddComponent<Image>();

            rootRect.sizeDelta = new Vector2(64, 64);
            rootImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Create icon
            GameObject iconObj = new GameObject("UnitIcon");
            iconObj.transform.SetParent(root.transform, false);
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            Image iconImage = iconObj.AddComponent<Image>();

            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(2, 10);
            iconRect.offsetMax = new Vector2(-2, -2);
            iconImage.color = Color.white;
            iconImage.preserveAspect = true;

            // Create HP bar background
            GameObject hpBg = new GameObject("HPBar");
            hpBg.transform.SetParent(root.transform, false);
            RectTransform hpBgRect = hpBg.AddComponent<RectTransform>();
            Image hpBgImage = hpBg.AddComponent<Image>();

            hpBgRect.anchorMin = new Vector2(0, 0);
            hpBgRect.anchorMax = new Vector2(1, 0);
            hpBgRect.pivot = new Vector2(0.5f, 0);
            hpBgRect.anchoredPosition = new Vector2(0, 2);
            hpBgRect.sizeDelta = new Vector2(-4, 6);
            hpBgImage.color = new Color(0.2f, 0f, 0f, 1f);

            // Create HP bar fill
            GameObject hpFill = new GameObject("HPBarFill");
            hpFill.transform.SetParent(hpBg.transform, false);
            RectTransform hpFillRect = hpFill.AddComponent<RectTransform>();
            Image hpFillImage = hpFill.AddComponent<Image>();

            hpFillRect.anchorMin = Vector2.zero;
            hpFillRect.anchorMax = Vector2.one;
            hpFillRect.offsetMin = Vector2.zero;
            hpFillRect.offsetMax = Vector2.zero;
            hpFillImage.color = Color.green;

            // Add component
            UnitIconWithHP component = root.AddComponent<UnitIconWithHP>();

            var type = typeof(UnitIconWithHP);
            var iconField = type.GetField("unitIcon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var fillField = type.GetField("hpBarFill", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var bgField = type.GetField("hpBarBackground", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (iconField != null) iconField.SetValue(component, iconImage);
            if (fillField != null) fillField.SetValue(component, hpFillImage);
            if (bgField != null) bgField.SetValue(component, hpBgImage);

            // Save prefab
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            DestroyImmediate(root);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Debug.Log($"[MultiUnitSelectionUISetup] ✅ Prefab created: {prefabPath}");

            return prefab;
        }

        [MenuItem("Tools/RTS/Remove Multi-Unit Selection UI")]
        public static void RemoveMultiUnitSelectionUI()
        {
            UnitDetailsUI unitDetailsUI = FindFirstObjectByType<UnitDetailsUI>();
            if (unitDetailsUI == null)
            {
                EditorUtility.DisplayDialog("Nothing to Remove", "UnitDetailsUI not found in scene.", "OK");
                return;
            }

            // Remove containers
            Transform multiContainer = unitDetailsUI.transform.Find("MultiUnitSelectionContainer");
            if (multiContainer != null)
            {
                DestroyImmediate(multiContainer.gameObject);
            }

            Transform singleContainer = unitDetailsUI.transform.Find("SingleUnitStatsContainer");
            if (singleContainer != null)
            {
                // Move children out before destroying
                while (singleContainer.childCount > 0)
                {
                    singleContainer.GetChild(0).SetParent(unitDetailsUI.transform, true);
                }
                DestroyImmediate(singleContainer.gameObject);
            }

            EditorUtility.DisplayDialog("Cleanup Complete", "Multi-Unit Selection UI has been removed.", "OK");
        }
    }
}
