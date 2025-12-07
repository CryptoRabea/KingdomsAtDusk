using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;
using KAD.UI.FloatingNumbers;
using Assets.Scripts.UI.FloatingNumbers;

namespace RTS.Editor
{
    /// <summary>
    /// Automated setup tool for the Floating Numbers System.
    /// Creates all necessary components, prefabs, and integrates with the game.
    /// </summary>
    public class FloatingNumbersSetupTool : EditorWindow
    {
        private const string SETTINGS_PATH = "Assets/Settings/FloatingNumbersSettings.asset";
        private const string PREFABS_PATH = "Assets/Prefabs/UI/FloatingNumbers/";

        private FloatingNumbersSettings settings;
        private bool setupComplete;
        private string statusMessage = "";

        [MenuItem("Tools/RTS/Setup/Floating Numbers System")]
        public static void ShowWindow()
        {
            FloatingNumbersSetupTool window = GetWindow<FloatingNumbersSetupTool>("Floating Numbers Setup");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Floating Numbers System Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This tool will automatically set up the floating numbers system:\n\n" +
                "• Create FloatingNumbersSettings asset\n" +
                "• Create FloatingNumbersManager in the scene\n" +
                "• Register service with GameManager\n" +
                "• Create settings panel prefab\n" +
                "• Integrate with game menu\n\n" +
                "Make sure you have a GameManager in your scene!",
                MessageType.Info
            );

            EditorGUILayout.Space(10);

            // Settings reference
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            settings = (FloatingNumbersSettings)EditorGUILayout.ObjectField(
                "Settings Asset",
                settings,
                typeof(FloatingNumbersSettings),
                false
            );

            EditorGUILayout.Space(10);

            // Setup buttons
            EditorGUI.BeginDisabledGroup(setupComplete);

            if (GUILayout.Button("Create Settings Asset", GUILayout.Height(30)))
            {
                CreateSettingsAsset();
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Setup in Current Scene", GUILayout.Height(40)))
            {
                SetupInScene();
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Create Settings Panel Prefab", GUILayout.Height(30)))
            {
                CreateSettingsPanelPrefab();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Complete Setup (All Steps)", GUILayout.Height(50)))
            {
                CompleteSetup();
            }

            EditorGUILayout.Space(10);

            // Status message
            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(statusMessage, setupComplete ? MessageType.Info : MessageType.Warning);
            }

            EditorGUILayout.Space(10);

            // Manual steps
            EditorGUILayout.LabelField("Manual Integration Steps", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "After running the setup:\n\n" +
                "1. Add the FloatingNumbersManager to your scene's GameManager hierarchy\n" +
                "2. Assign the settings asset to the manager\n" +
                "3. Add the settings panel to your pause/game menu\n" +
                "4. Test by taking damage or healing units\n\n" +
                "All features are optional and can be toggled in the settings!",
                MessageType.Info
            );
        }

        private void CreateSettingsAsset()
        {
            // Create directories if they don't exist
            string settingsDir = Path.GetDirectoryName(SETTINGS_PATH);
            if (!Directory.Exists(settingsDir))
            {
                Directory.CreateDirectory(settingsDir);
            }

            // Check if settings already exist
            settings = AssetDatabase.LoadAssetAtPath<FloatingNumbersSettings>(SETTINGS_PATH);

            if (settings == null)
            {
                settings = CreateInstance<FloatingNumbersSettings>();
                AssetDatabase.CreateAsset(settings, SETTINGS_PATH);
                AssetDatabase.SaveAssets();
                statusMessage = $"Settings asset created at: {SETTINGS_PATH}";
            }
            else
            {
                statusMessage = "Settings asset already exists!";
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.Refresh();
        }

        private void SetupInScene()
        {
            // Find or create GameManager
            GameObject gameManagerObj = GameObject.Find("GameManager");
            if (gameManagerObj == null)
            {
                statusMessage = "ERROR: GameManager not found in scene! Please add GameManager first.";
                return;
            }

            // Check if FloatingNumbersManager already exists
            FloatingNumbersManager existingManager = FindFirstObjectByType<FloatingNumbersManager>();
            if (existingManager != null)
            {
                statusMessage = "FloatingNumbersManager already exists in scene!";

                // Make sure it has settings assigned
                SerializedObject so = new SerializedObject(existingManager);
                SerializedProperty settingsProp = so.FindProperty("settings");
                if (settingsProp.objectReferenceValue == null && settings != null)
                {
                    settingsProp.objectReferenceValue = settings;
                    so.ApplyModifiedProperties();
                    statusMessage += "\nSettings assigned to existing manager.";
                }

                return;
            }

            // Create FloatingNumbersManager
            GameObject managerObj = new GameObject("FloatingNumbersManager");
            managerObj.transform.SetParent(gameManagerObj.transform);

            FloatingNumbersManager manager = managerObj.AddComponent<FloatingNumbersManager>();

            // Assign settings if available
            if (settings != null)
            {
                SerializedObject so = new SerializedObject(manager);
                SerializedProperty settingsProp = so.FindProperty("settings");
                settingsProp.objectReferenceValue = settings;
                so.ApplyModifiedProperties();
            }

            // Mark scene as dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            statusMessage = "FloatingNumbersManager created in scene!\n" +
                           "IMPORTANT: You need to register it as a service in GameManager.InitializeServices()";

            Selection.activeGameObject = managerObj;
        }

        private void CreateSettingsPanelPrefab()
        {
            // Create directories if they don't exist
            if (!Directory.Exists(PREFABS_PATH))
            {
                Directory.CreateDirectory(PREFABS_PATH);
            }

            string prefabPath = PREFABS_PATH + "FloatingNumbersSettingsPanel.prefab";

            // Check if prefab already exists
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existingPrefab != null)
            {
                statusMessage = "Settings panel prefab already exists!";
                return;
            }

            // Create the panel GameObject
            GameObject panel = CreateSettingsPanelUI();

            // Save as prefab
            PrefabUtility.SaveAsPrefabAsset(panel, prefabPath);

            // Clean up the scene instance
            DestroyImmediate(panel);

            statusMessage = $"Settings panel prefab created at: {prefabPath}\n" +
                           "Add this to your pause menu or game menu!";

            AssetDatabase.Refresh();
        }

        private GameObject CreateSettingsPanelUI()
        {
            // Create main panel
            GameObject panel = new GameObject("FloatingNumbersSettingsPanel");
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(600, 700);

            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            // Add the controller script
            FloatingNumbersSettingsPanel controller = panel.AddComponent<FloatingNumbersSettingsPanel>();

            // Create title
            CreateTitle(panel.transform, "Floating Numbers Settings");

            // Create scroll view for toggles
            GameObject scrollView = CreateScrollView(panel.transform);
            GameObject content = scrollView.transform.Find("Viewport/Content").gameObject;

            // Create sections with toggles
            float yPos = -20f;

            // Feature Toggles Section
            yPos = CreateSectionHeader(content.transform, "Display Options", yPos);
            yPos = CreateToggle(content.transform, "Show HP Bars", yPos, out Toggle hpBarsToggle);
            yPos = CreateToggle(content.transform, "Show Damage Numbers", yPos, out Toggle damageToggle);
            yPos = CreateToggle(content.transform, "Show Heal Numbers", yPos, out Toggle healToggle);
            yPos = CreateToggle(content.transform, "Show Resource Gathering", yPos, out Toggle gatherToggle);
            yPos = CreateToggle(content.transform, "Show Building Resources", yPos, out Toggle buildingResToggle);
            yPos = CreateToggle(content.transform, "Show Repair Numbers", yPos, out Toggle repairToggle);

            yPos -= 20f;

            // HP Bar Options Section
            yPos = CreateSectionHeader(content.transform, "HP Bar Options", yPos);
            yPos = CreateToggle(content.transform, "Only When Selected", yPos, out Toggle onlySelectedToggle);
            yPos = CreateToggle(content.transform, "Only When Damaged", yPos, out Toggle onlyDamagedToggle);

            yPos -= 20f;

            // Future Features Section
            yPos = CreateSectionHeader(content.transform, "Future Features (Coming Soon)", yPos);
            yPos = CreateToggle(content.transform, "Show Experience Numbers", yPos, out Toggle xpToggle);
            yPos = CreateToggle(content.transform, "Show Resource Pickups", yPos, out Toggle pickupsToggle);
            yPos = CreateToggle(content.transform, "Show Level Up Notifications", yPos, out Toggle levelUpToggle);

            // Update content size
            if (content.TryGetComponent<RectTransform>(out var contentRect))
            {
            }
            contentRect.sizeDelta = new Vector2(0, Mathf.Abs(yPos) + 20f);

            // Create buttons
            CreateButtons(panel.transform, controller, out Button applyBtn, out Button resetBtn, out Button closeBtn);

            // Create info text
            GameObject infoTextObj = CreateInfoText(panel.transform);

            // Assign references to controller using SerializedObject
            SerializedObject so = new SerializedObject(controller);

            if (settings != null)
                so.FindProperty("settings").objectReferenceValue = settings;

            so.FindProperty("showHPBarsToggle").objectReferenceValue = hpBarsToggle;
            so.FindProperty("showDamageNumbersToggle").objectReferenceValue = damageToggle;
            so.FindProperty("showHealNumbersToggle").objectReferenceValue = healToggle;
            so.FindProperty("showResourceGatheringToggle").objectReferenceValue = gatherToggle;
            so.FindProperty("showBuildingResourceToggle").objectReferenceValue = buildingResToggle;
            so.FindProperty("showRepairNumbersToggle").objectReferenceValue = repairToggle;
            so.FindProperty("hpBarsOnlyWhenSelectedToggle").objectReferenceValue = onlySelectedToggle;
            so.FindProperty("hpBarsOnlyWhenDamagedToggle").objectReferenceValue = onlyDamagedToggle;
            so.FindProperty("showExperienceNumbersToggle").objectReferenceValue = xpToggle;
            so.FindProperty("showResourcePickupsToggle").objectReferenceValue = pickupsToggle;
            so.FindProperty("showLevelUpNotificationsToggle").objectReferenceValue = levelUpToggle;
            so.FindProperty("applyButton").objectReferenceValue = applyBtn;
            so.FindProperty("resetButton").objectReferenceValue = resetBtn;
            so.FindProperty("closeButton").objectReferenceValue = closeBtn;
            so.FindProperty("infoText").objectReferenceValue = infoTextObj.GetComponent<TextMeshProUGUI>();

            so.ApplyModifiedProperties();

            return panel;
        }

        private void CreateTitle(Transform parent, string text)
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(parent, false);

            RectTransform rect = titleObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0, -20);
            rect.sizeDelta = new Vector2(560, 40);

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = text;
            titleText.fontSize = 28;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;
        }

        private GameObject CreateScrollView(Transform parent)
        {
            GameObject scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(parent, false);

            RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRect.pivot = new Vector2(0.5f, 0.5f);
            scrollRect.anchoredPosition = new Vector2(0, 0);
            scrollRect.sizeDelta = new Vector2(560, 500);

            ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
            Image scrollImage = scrollView.AddComponent<Image>();
            scrollImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewport.AddComponent<RectMask2D>();

            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 800);

            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.vertical = true;
            scroll.horizontal = false;

            return scrollView;
        }

        private float CreateSectionHeader(Transform parent, string text, float yPos)
        {
            GameObject header = new GameObject("Header_" + text.Replace(" ", ""));
            header.transform.SetParent(parent, false);

            RectTransform rect = header.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, yPos);
            rect.sizeDelta = new Vector2(-40, 30);

            TextMeshProUGUI headerText = header.AddComponent<TextMeshProUGUI>();
            headerText.text = text;
            headerText.fontSize = 20;
            headerText.fontStyle = FontStyles.Bold;
            headerText.color = new Color(1f, 0.8f, 0.2f);

            return yPos - 40f;
        }

        private float CreateToggle(Transform parent, string label, float yPos, out Toggle toggle)
        {
            GameObject toggleObj = new GameObject("Toggle_" + label.Replace(" ", ""));
            toggleObj.transform.SetParent(parent, false);

            RectTransform rect = toggleObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(20, yPos);
            rect.sizeDelta = new Vector2(-40, 30);

            toggle = toggleObj.AddComponent<Toggle>();

            // Background
            GameObject background = new GameObject("Background");
            background.transform.SetParent(toggleObj.transform, false);
            RectTransform bgRect = background.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.5f);
            bgRect.anchorMax = new Vector2(0, 0.5f);
            bgRect.pivot = new Vector2(0, 0.5f);
            bgRect.sizeDelta = new Vector2(20, 20);
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.3f, 0.3f, 0.3f);

            // Checkmark
            GameObject checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(background.transform, false);
            RectTransform checkRect = checkmark.AddComponent<RectTransform>();
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.sizeDelta = Vector2.zero;
            Image checkImage = checkmark.AddComponent<Image>();
            checkImage.color = new Color(0.2f, 1f, 0.2f);

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(toggleObj.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.offsetMin = new Vector2(30, 0);
            labelRect.offsetMax = new Vector2(0, 0);
            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 16;
            labelText.color = Color.white;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;

            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;

            return yPos - 35f;
        }

        private void CreateButtons(Transform parent, FloatingNumbersSettingsPanel controller,
            out Button apply, out Button reset, out Button close)
        {
            // Apply Button
            GameObject applyObj = new GameObject("ApplyButton");
            applyObj.transform.SetParent(parent, false);
            RectTransform applyRect = applyObj.AddComponent<RectTransform>();
            applyRect.anchorMin = new Vector2(0.5f, 0f);
            applyRect.anchorMax = new Vector2(0.5f, 0f);
            applyRect.pivot = new Vector2(0.5f, 0f);
            applyRect.anchoredPosition = new Vector2(-110, 20);
            applyRect.sizeDelta = new Vector2(100, 40);
            apply = CreateButton(applyObj, "Apply", new Color(0.2f, 0.8f, 0.2f));

            // Reset Button
            GameObject resetObj = new GameObject("ResetButton");
            resetObj.transform.SetParent(parent, false);
            RectTransform resetRect = resetObj.AddComponent<RectTransform>();
            resetRect.anchorMin = new Vector2(0.5f, 0f);
            resetRect.anchorMax = new Vector2(0.5f, 0f);
            resetRect.pivot = new Vector2(0.5f, 0f);
            resetRect.anchoredPosition = new Vector2(0, 20);
            resetRect.sizeDelta = new Vector2(100, 40);
            reset = CreateButton(resetObj, "Reset", new Color(0.8f, 0.6f, 0.2f));

            // Close Button
            GameObject closeObj = new GameObject("CloseButton");
            closeObj.transform.SetParent(parent, false);
            RectTransform closeRect = closeObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.5f, 0f);
            closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.anchoredPosition = new Vector2(110, 20);
            closeRect.sizeDelta = new Vector2(100, 40);
            close = CreateButton(closeObj, "Close", new Color(0.8f, 0.2f, 0.2f));
        }

        private Button CreateButton(GameObject obj, string text, Color color)
        {
            Image image = obj.AddComponent<Image>();
            image.color = color;

            Button button = obj.AddComponent<Button>();
            button.targetGraphic = image;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI textMesh = textObj.AddComponent<TextMeshProUGUI>();
            textMesh.text = text;
            textMesh.fontSize = 18;
            textMesh.fontStyle = FontStyles.Bold;
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.color = Color.white;

            return button;
        }

        private GameObject CreateInfoText(Transform parent)
        {
            GameObject infoObj = new GameObject("InfoText");
            infoObj.transform.SetParent(parent, false);

            RectTransform rect = infoObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0, 70);
            rect.sizeDelta = new Vector2(560, 40);

            TextMeshProUGUI text = infoObj.AddComponent<TextMeshProUGUI>();
            text.text = "Configure floating numbers and HP bar display options.\nChanges are applied immediately.";
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.7f, 0.7f, 0.7f);

            return infoObj;
        }

        private void CompleteSetup()
        {
            CreateSettingsAsset();
            SetupInScene();
            CreateSettingsPanelPrefab();

            setupComplete = true;
            statusMessage = "Setup complete! Check the console for next steps.";

        }
    }
}
