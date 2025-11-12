using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using RTS.UI;
using RTS.Managers;

namespace RTS.Editor
{
    /// <summary>
    /// Editor tool to automatically create and setup the BuildingHUD UI.
    /// Usage: Tools > RTS > Setup BuildingHUD
    /// </summary>
    public class BuildingHUDSetup : EditorWindow
    {
        private Canvas targetCanvas;
        private BuildingManager buildingManager;

        [MenuItem("Tools/RTS/Setup BuildingHUD")]
        public static void ShowWindow()
        {
            GetWindow<BuildingHUDSetup>("BuildingHUD Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("BuildingHUD Setup Tool", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool will create:\n" +
                "• BuildingHUD panel with building buttons\n" +
                "• BuildingButton prefab for each building type\n" +
                "• Placement info panel\n" +
                "• Toggle button to show/hide the menu\n" +
                "• All references automatically connected",
                MessageType.Info);

            GUILayout.Space(10);

            targetCanvas = (Canvas)EditorGUILayout.ObjectField(
                "Target Canvas",
                targetCanvas,
                typeof(Canvas),
                true);

            if (targetCanvas == null)
            {
                EditorGUILayout.HelpBox("Please assign a Canvas to create the UI in.", MessageType.Warning);
            }

            buildingManager = (BuildingManager)EditorGUILayout.ObjectField(
                "Building Manager",
                buildingManager,
                typeof(BuildingManager),
                true);

            if (buildingManager == null)
            {
                EditorGUILayout.HelpBox("BuildingManager is optional but recommended. It will be auto-assigned if found.", MessageType.Info);
            }

            GUILayout.Space(20);

            GUI.enabled = targetCanvas != null;

            if (GUILayout.Button("Create Complete BuildingHUD", GUILayout.Height(40)))
            {
                CreateCompleteBuildingHUD();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Create BuildingHUD Panel Only", GUILayout.Height(30)))
            {
                CreateBuildingHUDPanel();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Create Building Button Prefab Only", GUILayout.Height(30)))
            {
                CreateBuildingButtonPrefab();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Create Toggle Button Only", GUILayout.Height(30)))
            {
                CreateToggleButton();
            }

            GUI.enabled = true;
        }

        private void CreateCompleteBuildingHUD()
        {
            // Auto-find BuildingManager if not assigned
            if (buildingManager == null)
            {
                buildingManager = FindObjectOfType<BuildingManager>();
            }

            GameObject hudPanel = CreateBuildingHUDPanel();
            GameObject buttonPrefab = CreateBuildingButtonPrefab();
            CreateToggleButton();

            // Link button prefab to HUD
            if (hudPanel != null && buttonPrefab != null)
            {
                BuildingHUD hud = hudPanel.GetComponent<BuildingHUD>();
                if (hud != null)
                {
                    SerializedObject serializedHUD = new SerializedObject(hud);
                    serializedHUD.FindProperty("buildingButtonPrefab").objectReferenceValue = buttonPrefab;
                    serializedHUD.ApplyModifiedProperties();
                    Debug.Log("✅ BuildingButton prefab linked to BuildingHUD!");
                }
            }

            Debug.Log("✅✅✅ Complete BuildingHUD system created successfully!");
            EditorUtility.DisplayDialog("Success!",
                "BuildingHUD system created!\n\n" +
                "• BuildingHUD panel\n" +
                "• BuildingButton prefab\n" +
                "• Toggle button\n\n" +
                "All references are connected!", "OK");
        }

        private GameObject CreateBuildingHUDPanel()
        {
            // Create main HUD panel
            GameObject hudPanel = CreateUIElement("BuildingHUD", targetCanvas.transform);
            RectTransform hudRect = hudPanel.GetComponent<RectTransform>();

            // Position at bottom of screen
            hudRect.anchorMin = new Vector2(0.25f, 0);
            hudRect.anchorMax = new Vector2(0.75f, 0);
            hudRect.pivot = new Vector2(0.5f, 0);
            hudRect.anchoredPosition = new Vector2(0, 10);
            hudRect.sizeDelta = new Vector2(0, 150);

            // Add background
            Image hudBg = hudPanel.AddComponent<Image>();
            hudBg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            hudBg.raycastTarget = true; // Block clicks

            // Add BuildingHUD component
            BuildingHUD buildingHUD = hudPanel.AddComponent<BuildingHUD>();

            // Create building panel container
            GameObject buildingPanel = CreateUIElement("BuildingPanel", hudPanel.transform);
            RectTransform buildingPanelRect = buildingPanel.GetComponent<RectTransform>();
            buildingPanelRect.anchorMin = Vector2.zero;
            buildingPanelRect.anchorMax = Vector2.one;
            buildingPanelRect.offsetMin = new Vector2(10, 10);
            buildingPanelRect.offsetMax = new Vector2(-10, -10);

            // Add horizontal layout for buttons
            HorizontalLayoutGroup horizontalLayout = buildingPanel.AddComponent<HorizontalLayoutGroup>();
            horizontalLayout.spacing = 10;
            horizontalLayout.childAlignment = TextAnchor.MiddleCenter;
            horizontalLayout.childControlWidth = false;
            horizontalLayout.childControlHeight = false;
            horizontalLayout.childForceExpandWidth = false;
            horizontalLayout.childForceExpandHeight = false;

            // Add content size fitter
            ContentSizeFitter fitter = buildingPanel.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Create scroll view for buttons if many buildings
            GameObject scrollView = CreateScrollView("ButtonScrollView", hudPanel.transform, true);
            GameObject scrollContent = scrollView.transform.Find("Viewport/Content").gameObject;

            // Move buildingPanel into scroll content
            buildingPanel.transform.SetParent(scrollContent.transform, false);

            // Create placement info panel
            GameObject placementPanel = CreatePlacementInfoPanel(targetCanvas.transform);

            // Assign references to BuildingHUD
            SerializedObject serializedHUD = new SerializedObject(buildingHUD);

            // Auto-find BuildingManager if not assigned
            if (buildingManager == null)
            {
                buildingManager = FindObjectOfType<BuildingManager>();
            }

            serializedHUD.FindProperty("buildingManager").objectReferenceValue = buildingManager;
            serializedHUD.FindProperty("buildingButtonContainer").objectReferenceValue = buildingPanel.transform;
            serializedHUD.FindProperty("buildingPanel").objectReferenceValue = buildingPanel;
            serializedHUD.FindProperty("placementInfoPanel").objectReferenceValue = placementPanel;

            // Find placement info text
            TextMeshProUGUI placementText = placementPanel.GetComponentInChildren<TextMeshProUGUI>();
            if (placementText != null)
            {
                serializedHUD.FindProperty("placementInfoText").objectReferenceValue = placementText;
            }

            serializedHUD.ApplyModifiedProperties();

            EditorUtility.SetDirty(hudPanel);
            Selection.activeGameObject = hudPanel;

            Debug.Log("✅ BuildingHUD panel created successfully!");

            if (buildingManager == null)
            {
                Debug.LogWarning("⚠️ BuildingManager not found. Please assign it manually in the BuildingHUD inspector.");
            }
            else
            {
                Debug.Log($"✅ BuildingManager '{buildingManager.name}' automatically assigned!");
            }

            return hudPanel;
        }

        private GameObject CreateBuildingButtonPrefab()
        {
            // Create button
            GameObject buttonObj = CreateUIElement("BuildingButton", null);
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(100, 120);

            // Add button component
            Button button = buttonObj.AddComponent<Button>();

            // Add background
            Image buttonBg = buttonObj.AddComponent<Image>();
            buttonBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            button.targetGraphic = buttonBg;

            // Add vertical layout
            VerticalLayoutGroup layout = buttonObj.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(5, 5, 5, 5);
            layout.spacing = 5;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // Building icon
            GameObject iconObj = CreateUIElement("Icon", buttonObj.transform);
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(60, 60);
            Image buildingIcon = iconObj.AddComponent<Image>();
            buildingIcon.color = Color.white;
            buildingIcon.preserveAspect = true;

            // Building name
            GameObject nameObj = CreateTextElement("NameText", buttonObj.transform, "Building", 12, TextAlignmentOptions.Center);
            TextMeshProUGUI nameText = nameObj.GetComponent<TextMeshProUGUI>();
            nameText.fontStyle = FontStyles.Bold;

            // Cost container
            GameObject costContainer = CreateUIElement("CostContainer", buttonObj.transform);
            VerticalLayoutGroup costLayout = costContainer.AddComponent<VerticalLayoutGroup>();
            costLayout.spacing = 2;
            costLayout.childAlignment = TextAnchor.MiddleCenter;

            // Wood cost
            GameObject woodObj = CreateResourceCostElement("WoodCost", costContainer.transform, "🪵 0");
            // Food cost
            GameObject foodObj = CreateResourceCostElement("FoodCost", costContainer.transform, "🍖 0");
            // Gold cost
            GameObject goldObj = CreateResourceCostElement("GoldCost", costContainer.transform, "💰 0");
            // Stone cost
            GameObject stoneObj = CreateResourceCostElement("StoneCost", costContainer.transform, "🪨 0");

            // Hotkey indicator
            GameObject hotkeyObj = CreateTextElement("HotkeyText", buttonObj.transform, "[B]", 10, TextAlignmentOptions.Center);
            TextMeshProUGUI hotkeyText = hotkeyObj.GetComponent<TextMeshProUGUI>();
            hotkeyText.color = new Color(1f, 1f, 0.5f, 0.8f);

            // Add BuildingButton component
            var buildingButton = buttonObj.AddComponent<RTS.UI.BuildingButton>();

            // Assign references using reflection (since fields are private/serialized)
            SerializedObject serializedButton = new SerializedObject(buildingButton);
            serializedButton.FindProperty("button").objectReferenceValue = button;
            serializedButton.FindProperty("buildingIcon").objectReferenceValue = buildingIcon;
            serializedButton.FindProperty("buildingNameText").objectReferenceValue = nameText;
            serializedButton.FindProperty("hotkeyText").objectReferenceValue = hotkeyText;

            // Cost text references
            serializedButton.FindProperty("woodCostText").objectReferenceValue = woodObj.GetComponent<TextMeshProUGUI>();
            serializedButton.FindProperty("foodCostText").objectReferenceValue = foodObj.GetComponent<TextMeshProUGUI>();
            serializedButton.FindProperty("goldCostText").objectReferenceValue = goldObj.GetComponent<TextMeshProUGUI>();
            serializedButton.FindProperty("stoneCostText").objectReferenceValue = stoneObj.GetComponent<TextMeshProUGUI>();

            serializedButton.ApplyModifiedProperties();

            // Save as prefab
            string prefabPath = "Assets/Prefabs/UI/BuildingButton.prefab";

            // Ensure directory exists
            string directory = System.IO.Path.GetDirectoryName(prefabPath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            // Save prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(buttonObj, prefabPath);
            DestroyImmediate(buttonObj);

            EditorUtility.SetDirty(prefab);
            Selection.activeObject = prefab;

            Debug.Log($"✅ BuildingButton prefab created at: {prefabPath}");

            // Try to assign to BuildingHUD if it exists
            BuildingHUD hud = FindObjectOfType<BuildingHUD>();
            if (hud != null)
            {
                SerializedObject serializedHUD = new SerializedObject(hud);
                serializedHUD.FindProperty("buildingButtonPrefab").objectReferenceValue = prefab;
                serializedHUD.ApplyModifiedProperties();
                Debug.Log("✅ BuildingButton prefab automatically assigned to BuildingHUD!");
            }

            return prefab;
        }

        private GameObject CreatePlacementInfoPanel(Transform parent)
        {
            GameObject panel = CreateUIElement("PlacementInfoPanel", parent);
            RectTransform panelRect = panel.GetComponent<RectTransform>();

            // Position at bottom center
            panelRect.anchorMin = new Vector2(0.5f, 0);
            panelRect.anchorMax = new Vector2(0.5f, 0);
            panelRect.pivot = new Vector2(0.5f, 0);
            panelRect.anchoredPosition = new Vector2(0, 170);
            panelRect.sizeDelta = new Vector2(400, 40);

            // Add background
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.8f, 0.6f, 0.2f, 0.9f);
            bg.raycastTarget = true;

            // Add text
            GameObject textObj = CreateTextElement("Text", panel.transform, "Left Click: Place  |  Right Click/ESC: Cancel", 14, TextAlignmentOptions.Center);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // Initially hidden
            panel.SetActive(false);

            return panel;
        }

        private void CreateToggleButton()
        {
            // Create toggle button
            GameObject buttonObj = CreateUIElement("BuildingHUDToggleButton", targetCanvas.transform);
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();

            // Position in top-left corner
            buttonRect.anchorMin = new Vector2(0, 1);
            buttonRect.anchorMax = new Vector2(0, 1);
            buttonRect.pivot = new Vector2(0, 1);
            buttonRect.anchoredPosition = new Vector2(10, -10);
            buttonRect.sizeDelta = new Vector2(50, 50);

            // Add button component
            Button button = buttonObj.AddComponent<Button>();

            // Add background image
            Image buttonBg = buttonObj.AddComponent<Image>();
            buttonBg.color = new Color(0.2f, 0.5f, 0.8f, 0.9f);
            button.targetGraphic = buttonBg;

            // Add icon text
            GameObject textObj = CreateTextElement("Text", buttonObj.transform, "🏗️", 24, TextAlignmentOptions.Center);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // Add BuildingHUDToggle component
            BuildingHUDToggle toggle = buttonObj.AddComponent<BuildingHUDToggle>();

            // Try to find and assign BuildingHUD
            BuildingHUD buildingHUD = FindObjectOfType<BuildingHUD>();
            if (buildingHUD != null)
            {
                SerializedObject serializedToggle = new SerializedObject(toggle);
                serializedToggle.FindProperty("buildingHUD").objectReferenceValue = buildingHUD;
                serializedToggle.FindProperty("startOpen").boolValue = true;
                serializedToggle.ApplyModifiedProperties();
                Debug.Log("✅ BuildingHUD automatically assigned to toggle button!");
            }

            EditorUtility.SetDirty(buttonObj);

            Debug.Log("✅ BuildingHUD toggle button created!");
        }

        // Helper methods
        private GameObject CreateUIElement(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            RectTransform rect = obj.AddComponent<RectTransform>();

            if (parent != null)
            {
                obj.transform.SetParent(parent, false);
            }

            return obj;
        }

        private GameObject CreateTextElement(string name, Transform parent, string text, int fontSize, TextAlignmentOptions alignment)
        {
            GameObject obj = CreateUIElement(name, parent);
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            return obj;
        }

        private GameObject CreateResourceCostElement(string name, Transform parent, string defaultText)
        {
            GameObject obj = CreateTextElement(name, parent, defaultText, 10, TextAlignmentOptions.Left);
            TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
            obj.SetActive(false); // Hidden by default, shown when cost > 0
            return obj;
        }

        private GameObject CreateScrollView(string name, Transform parent, bool horizontal)
        {
            GameObject scrollObj = CreateUIElement(name, parent);
            RectTransform scrollRect = scrollObj.GetComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;

            Image scrollBg = scrollObj.AddComponent<Image>();
            scrollBg.color = new Color(0.05f, 0.05f, 0.05f, 0.5f);
            scrollBg.raycastTarget = true;

            ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = horizontal;
            scroll.vertical = !horizontal;

            // Viewport
            GameObject viewport = CreateUIElement("Viewport", scrollObj.transform);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.raycastTarget = true;
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            // Content
            GameObject content = CreateUIElement("Content", viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();

            if (horizontal)
            {
                contentRect.anchorMin = new Vector2(0, 0);
                contentRect.anchorMax = new Vector2(0, 1);
                contentRect.pivot = new Vector2(0, 0.5f);
            }
            else
            {
                contentRect.anchorMin = new Vector2(0, 1);
                contentRect.anchorMax = new Vector2(1, 1);
                contentRect.pivot = new Vector2(0.5f, 1);
            }

            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            if (horizontal)
            {
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            else
            {
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            scroll.viewport = viewportRect;
            scroll.content = contentRect;

            return scrollObj;
        }
    }
}
