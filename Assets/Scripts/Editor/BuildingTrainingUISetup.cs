using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using RTS.UI;

namespace RTS.Editor
{
    /// <summary>
    /// Editor tool to automatically create and setup the Building Training UI.
    /// Usage: Tools > RTS > Setup Building Training UI
    /// </summary>
    public class BuildingTrainingUISetup : EditorWindow
    {
        private Canvas targetCanvas;
        private Font defaultFont;

        [MenuItem("Tools/RTS/Setup Building Training UI")]
        public static void ShowWindow()
        {
            GetWindow<BuildingTrainingUISetup>("Building Training UI Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Building Training UI Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool will create:\n" +
                "• BuildingDetailsUI panel with all child elements\n" +
                "• TrainUnitButton prefab\n" +
                "• All references will be automatically connected",
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

            GUILayout.Space(20);

            GUI.enabled = targetCanvas != null;
            if (GUILayout.Button("Create Building Details UI", GUILayout.Height(40)))
            {
                CreateBuildingDetailsUI();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Create Train Unit Button Prefab", GUILayout.Height(40)))
            {
                CreateTrainUnitButtonPrefab();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Create Both (Recommended)", GUILayout.Height(40)))
            {
                CreateBuildingDetailsUI();
                CreateTrainUnitButtonPrefab();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Create BuildingHUD Toggle Button", GUILayout.Height(40)))
            {
                CreateBuildingHUDToggleButton();
            }

            GUI.enabled = true;
        }

        private void CreateBuildingDetailsUI()
        {
            // Create main panel
            GameObject panelRoot = CreateUIElement("BuildingDetailsPanel", targetCanvas.transform);
            RectTransform panelRect = panelRoot.GetComponent<RectTransform>();

            // Position panel on the right side of screen
            panelRect.anchorMin = new Vector2(0.75f, 0.2f);
            panelRect.anchorMax = new Vector2(0.98f, 0.8f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Add background image (blocks raycasts to prevent clicking through)
            Image panelBg = panelRoot.AddComponent<Image>();
            panelBg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            panelBg.raycastTarget = true; // ✅ Block clicks to buildings behind panel

            // Add BuildingDetailsUI component
            BuildingDetailsUI detailsUI = panelRoot.AddComponent<BuildingDetailsUI>();

            // Create header section
            GameObject headerSection = CreateUIElement("HeaderSection", panelRoot.transform);
            SetupLayoutElement(headerSection, 0, 100);
            AddVerticalLayout(headerSection, 10, TextAnchor.UpperLeft);

            // Building icon
            GameObject iconObj = CreateUIElement("BuildingIcon", headerSection.transform);
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(80, 80);
            Image buildingIcon = iconObj.AddComponent<Image>();
            buildingIcon.color = Color.white;
            buildingIcon.preserveAspect = true;

            // Building name
            GameObject nameObj = CreateTextElement("BuildingName", headerSection.transform, "Building Name", 24, TextAlignmentOptions.Center);
            TextMeshProUGUI buildingNameText = nameObj.GetComponent<TextMeshProUGUI>();
            buildingNameText.fontStyle = FontStyles.Bold;

            // Building description
            GameObject descObj = CreateTextElement("BuildingDescription", headerSection.transform, "Building description goes here...", 14, TextAlignmentOptions.TopLeft);
            TextMeshProUGUI buildingDescText = descObj.GetComponent<TextMeshProUGUI>();
            SetupLayoutElement(descObj, 0, 60);

            // Create training queue panel
            GameObject trainingQueuePanel = CreateUIElement("TrainingQueuePanel", panelRoot.transform);
            AddVerticalLayout(trainingQueuePanel, 5, TextAnchor.UpperLeft);
            Image queueBg = trainingQueuePanel.AddComponent<Image>();
            queueBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            queueBg.raycastTarget = true; // Block clicks
            SetupLayoutElement(trainingQueuePanel, 0, 120);

            // Queue count text
            GameObject queueCountObj = CreateTextElement("QueueCountText", trainingQueuePanel.transform, "Queue: 0/5", 14, TextAlignmentOptions.Left);
            TextMeshProUGUI queueCountText = queueCountObj.GetComponent<TextMeshProUGUI>();

            // Current training text
            GameObject currentTrainingObj = CreateTextElement("CurrentTrainingText", trainingQueuePanel.transform, "Training: None", 14, TextAlignmentOptions.Left);
            TextMeshProUGUI currentTrainingText = currentTrainingObj.GetComponent<TextMeshProUGUI>();

            // Progress bar container
            GameObject progressContainer = CreateUIElement("ProgressBarContainer", trainingQueuePanel.transform);
            RectTransform progressRect = progressContainer.GetComponent<RectTransform>();
            progressRect.sizeDelta = new Vector2(0, 30);
            SetupLayoutElement(progressContainer, 0, 30);

            // Progress bar background
            GameObject progressBg = CreateUIElement("ProgressBarBg", progressContainer.transform);
            RectTransform progressBgRect = progressBg.GetComponent<RectTransform>();
            progressBgRect.anchorMin = Vector2.zero;
            progressBgRect.anchorMax = Vector2.one;
            progressBgRect.offsetMin = Vector2.zero;
            progressBgRect.offsetMax = Vector2.zero;
            Image progressBgImage = progressBg.AddComponent<Image>();
            progressBgImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            // Progress bar fill
            GameObject progressFill = CreateUIElement("ProgressBarFill", progressBg.transform);
            RectTransform progressFillRect = progressFill.GetComponent<RectTransform>();
            progressFillRect.anchorMin = Vector2.zero;
            progressFillRect.anchorMax = new Vector2(0, 1);
            progressFillRect.pivot = new Vector2(0, 0.5f);
            progressFillRect.offsetMin = Vector2.zero;
            progressFillRect.offsetMax = Vector2.zero;
            Image trainingProgressBar = progressFill.AddComponent<Image>();
            trainingProgressBar.color = new Color(0.2f, 0.8f, 0.2f, 1f);
            trainingProgressBar.type = Image.Type.Filled;
            trainingProgressBar.fillMethod = Image.FillMethod.Horizontal;
            trainingProgressBar.fillAmount = 0f;

            // Create unit button container
            GameObject unitButtonContainer = CreateUIElement("UnitButtonContainer", panelRoot.transform);
            AddVerticalLayout(unitButtonContainer, 10, TextAnchor.UpperCenter);

            // Add scroll view for buttons
            GameObject scrollView = CreateScrollView("ButtonScrollView", panelRoot.transform);
            GameObject scrollContent = scrollView.transform.Find("Viewport/Content").gameObject;
            AddVerticalLayout(scrollContent, 10, TextAnchor.UpperCenter);

            // Assign all references to BuildingDetailsUI
            SerializedObject serializedUI = new SerializedObject(detailsUI);

            serializedUI.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            serializedUI.FindProperty("buildingNameText").objectReferenceValue = buildingNameText;
            serializedUI.FindProperty("buildingDescriptionText").objectReferenceValue = buildingDescText;
            serializedUI.FindProperty("buildingIcon").objectReferenceValue = buildingIcon;
            serializedUI.FindProperty("trainingQueuePanel").objectReferenceValue = trainingQueuePanel;
            serializedUI.FindProperty("queueCountText").objectReferenceValue = queueCountText;
            serializedUI.FindProperty("trainingProgressBar").objectReferenceValue = trainingProgressBar;
            serializedUI.FindProperty("currentTrainingText").objectReferenceValue = currentTrainingText;
            serializedUI.FindProperty("unitButtonContainer").objectReferenceValue = scrollContent.transform;

            serializedUI.ApplyModifiedProperties();

            // Initially hide the panel
            panelRoot.SetActive(false);

            EditorUtility.SetDirty(panelRoot);
            Selection.activeGameObject = panelRoot;

            Debug.Log("✅ BuildingDetailsUI created successfully! Panel is initially hidden and will show when you select a building.");
        }

        private void CreateTrainUnitButtonPrefab()
        {
            // Create button
            GameObject buttonObj = CreateUIElement("TrainUnitButton", null);
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(300, 80);

            // Add button component
            Button button = buttonObj.AddComponent<Button>();

            // Add background image
            Image buttonBg = buttonObj.AddComponent<Image>();
            buttonBg.color = new Color(0.2f, 0.4f, 0.8f, 1f);
            button.targetGraphic = buttonBg;

            // Add horizontal layout
            HorizontalLayoutGroup layout = buttonObj.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // Unit icon
            GameObject iconObj = CreateUIElement("UnitIcon", buttonObj.transform);
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(60, 60);
            Image unitIcon = iconObj.AddComponent<Image>();
            unitIcon.color = Color.white;
            unitIcon.preserveAspect = true;

            // Info container (vertical layout for text elements)
            GameObject infoContainer = CreateUIElement("InfoContainer", buttonObj.transform);
            AddVerticalLayout(infoContainer, 2, TextAnchor.UpperLeft);
            LayoutElement infoLayout = infoContainer.AddComponent<LayoutElement>();
            infoLayout.preferredWidth = 200;

            // Unit name
            GameObject nameObj = CreateTextElement("UnitNameText", infoContainer.transform, "Unit Name", 16, TextAlignmentOptions.Left);
            TextMeshProUGUI unitNameText = nameObj.GetComponent<TextMeshProUGUI>();
            unitNameText.fontStyle = FontStyles.Bold;

            // Cost text
            GameObject costObj = CreateTextElement("CostText", infoContainer.transform, "Cost: 100", 12, TextAlignmentOptions.Left);
            TextMeshProUGUI costText = costObj.GetComponent<TextMeshProUGUI>();

            // Training time text
            GameObject timeObj = CreateTextElement("TrainingTimeText", infoContainer.transform, "Time: 5s", 12, TextAlignmentOptions.Left);
            TextMeshProUGUI trainingTimeText = timeObj.GetComponent<TextMeshProUGUI>();

            // Add TrainUnitButton component
            TrainUnitButton trainButton = buttonObj.AddComponent<TrainUnitButton>();

            // Assign references
            SerializedObject serializedButton = new SerializedObject(trainButton);
            serializedButton.FindProperty("button").objectReferenceValue = button;
            serializedButton.FindProperty("unitIcon").objectReferenceValue = unitIcon;
            serializedButton.FindProperty("unitNameText").objectReferenceValue = unitNameText;
            serializedButton.FindProperty("costText").objectReferenceValue = costText;
            serializedButton.FindProperty("trainingTimeText").objectReferenceValue = trainingTimeText;
            serializedButton.ApplyModifiedProperties();

            // Save as prefab
            string prefabPath = "Assets/Prefabs/UI/TrainUnitButton.prefab";

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

            Debug.Log($"✅ TrainUnitButton prefab created at: {prefabPath}");

            // Try to assign to BuildingDetailsUI if it exists
            BuildingDetailsUI detailsUI = Object.FindAnyObjectByType<BuildingDetailsUI>();
            if (detailsUI != null)
            {
                SerializedObject serializedUI = new SerializedObject(detailsUI);
                serializedUI.FindProperty("trainUnitButtonPrefab").objectReferenceValue = prefab;
                serializedUI.ApplyModifiedProperties();
                Debug.Log("✅ TrainUnitButton prefab automatically assigned to BuildingDetailsUI!");
            }
        }

        private void CreateBuildingHUDToggleButton()
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

            // Add icon text (using unicode hammer/tools icon)
            GameObject textObj = CreateTextElement("Text", buttonObj.transform, "🏗️", 24, TextAlignmentOptions.Center);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // Add BuildingHUDToggle component
            BuildingHUDToggle toggle = buttonObj.AddComponent<BuildingHUDToggle>();

            // Try to find and assign BuildingHUD
            BuildingHUD buildingHUD = Object.FindAnyObjectByType<BuildingHUD>();
            if (buildingHUD != null)
            {
                SerializedObject serializedToggle = new SerializedObject(toggle);
                serializedToggle.FindProperty("buildingHUD").objectReferenceValue = buildingHUD;
                serializedToggle.FindProperty("startOpen").boolValue = true;
                serializedToggle.ApplyModifiedProperties();
                Debug.Log("✅ BuildingHUD automatically assigned to toggle button!");
            }
            else
            {
                Debug.LogWarning("⚠️ BuildingHUD not found in scene. Assign it manually in the inspector.");
            }

            EditorUtility.SetDirty(buttonObj);
            Selection.activeGameObject = buttonObj;

            Debug.Log("✅ BuildingHUD toggle button created in top-left corner!");
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

        private GameObject CreateScrollView(string name, Transform parent)
        {
            GameObject scrollObj = CreateUIElement(name, parent);
            RectTransform scrollRect = scrollObj.GetComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;

            Image scrollBg = scrollObj.AddComponent<Image>();
            scrollBg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
            scrollBg.raycastTarget = true; // Block clicks

            ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            // Viewport
            GameObject viewport = CreateUIElement("Viewport", scrollObj.transform);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.raycastTarget = true; // Block clicks
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            // Content
            GameObject content = CreateUIElement("Content", viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRect;
            scroll.content = contentRect;

            return scrollObj;
        }

        private void AddVerticalLayout(GameObject obj, int spacing, TextAnchor alignment)
        {
            VerticalLayoutGroup layout = obj.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(5, 5, 5, 5);
        }

        private void SetupLayoutElement(GameObject obj, int preferredWidth, int preferredHeight)
        {
            LayoutElement element = obj.AddComponent<LayoutElement>();
            if (preferredWidth > 0)
                element.preferredWidth = preferredWidth;
            if (preferredHeight > 0)
                element.preferredHeight = preferredHeight;
        }
    }
}
