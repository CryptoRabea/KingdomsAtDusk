using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using RTS.SaveLoad;
using System.IO;

namespace RTS.SaveLoad.Editor
{
    /// <summary>
    /// Automated setup tool for the complete Save/Load system.
    /// Creates UI, components, settings, and auto-wires all references.
    /// Access via: Tools > RTS > Setup Save/Load System
    /// </summary>
    public class SaveLoadSystemSetup : EditorWindow
    {
        private const string SETTINGS_PATH = "Assets/Settings/SaveLoadSettings.asset";
        private const string PREFABS_PATH = "Assets/Prefabs/UI/SaveLoad";

        [MenuItem("Tools/RTS/Setup Save/Load System")]
        public static void ShowWindow()
        {
            var window = GetWindow<SaveLoadSystemSetup>("Save/Load Setup");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Space(10);

            EditorGUILayout.LabelField("Save/Load System Auto Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This tool will automatically create and configure the entire Save/Load system:\n\n" +
                "- SaveLoadSettings ScriptableObject\n" +
                "- SaveLoadSystem GameObject with all components\n" +
                "- In-game menu with Resume, Save, Load, Back to Main Menu, Save & Quit, Quit without Saving\n" +
                "- SaveListItem prefab\n" +
                "- Auto-wire all references\n" +
                "- Integrate with GameManager",
                MessageType.Info
            );

            GUILayout.Space(20);

            if (GUILayout.Button("Setup Complete Save/Load System", GUILayout.Height(40)))
            {
                SetupCompleteSystem();
            }

            GUILayout.Space(10);

            EditorGUILayout.LabelField("Individual Setup Options:", EditorStyles.boldLabel);

            if (GUILayout.Button("1. Create SaveLoadSettings Only"))
            {
                CreateSaveLoadSettings();
            }

            if (GUILayout.Button("2. Create Save/Load UI Only"))
            {
                var itemPrefab = CreateSaveListItemPrefab();
                CreateSaveLoadUI(itemPrefab);
            }

            if (GUILayout.Button("3. Create SaveLoadSystem GameObject Only"))
            {
                CreateSaveLoadSystemObject();
            }

            if (GUILayout.Button("4. Auto-Wire All References"))
            {
                AutoWireReferences();
            }

            GUILayout.Space(20);
            EditorGUILayout.HelpBox(
                "After setup, press Play and use:\n" +
                "F5 - Quick Save\n" +
                "F9 - Quick Load\n" +
                "F10/ESC - Toggle Menu",
                MessageType.None
            );
        }

        private void SetupCompleteSystem()
        {
            if (!EditorUtility.DisplayDialog(
                "Setup Save/Load System",
                "This will create all necessary assets and UI. Continue?",
                "Yes", "Cancel"))
            {
                return;
            }

            try
            {
                EditorUtility.DisplayProgressBar("Setting Up Save/Load", "Creating directories...", 0.1f);
                CreateDirectories();

                EditorUtility.DisplayProgressBar("Setting Up Save/Load", "Creating settings...", 0.2f);
                var settings = CreateSaveLoadSettings();

                EditorUtility.DisplayProgressBar("Setting Up Save/Load", "Creating prefab...", 0.3f);
                var itemPrefab = CreateSaveListItemPrefab();

                EditorUtility.DisplayProgressBar("Setting Up Save/Load", "Creating UI...", 0.5f);
                var menu = CreateSaveLoadUI(itemPrefab);

                EditorUtility.DisplayProgressBar("Setting Up Save/Load", "Creating system GameObject...", 0.7f);
                var systemObj = CreateSaveLoadSystemObject();

                EditorUtility.DisplayProgressBar("Setting Up Save/Load", "Wiring references...", 0.8f);
                WireReferences(systemObj, settings, menu, itemPrefab);

                EditorUtility.DisplayProgressBar("Setting Up Save/Load", "Integrating with GameManager...", 0.9f);
                IntegrateWithGameManager(systemObj);

                EditorUtility.DisplayProgressBar("Setting Up Save/Load", "Marking scene dirty...", 0.95f);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
                );

                EditorUtility.ClearProgressBar();

                EditorUtility.DisplayDialog(
                    "Setup Complete!",
                    "Save/Load system has been fully configured!\n\n" +
                    "- SaveLoadSettings created\n" +
                    "- UI Menu created (hidden by default)\n" +
                    "- SaveListItem prefab created\n" +
                    "- All references wired\n\n" +
                    "Press Play and use:\n" +
                    "F5 - Quick Save\n" +
                    "F9 - Quick Load\n" +
                    "F10/ESC - Toggle Menu",
                    "OK"
                );

                Debug.Log(" Save/Load system setup complete!");
                Selection.activeGameObject = systemObj;
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Setup Failed", $"Error during setup:\n{e.Message}", "OK");
                Debug.LogError($"Setup failed: {e.Message}\n{e.StackTrace}");
            }
        }

        #region Directory Creation

        private void CreateDirectories()
        {
            CreateDirectoryIfNeeded("Assets/Settings");
            CreateDirectoryIfNeeded("Assets/Prefabs");
            CreateDirectoryIfNeeded("Assets/Prefabs/UI");
            CreateDirectoryIfNeeded(PREFABS_PATH);
        }

        private void CreateDirectoryIfNeeded(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace("\\", "/");
                string folder = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        #endregion

        #region Settings Creation

        private SaveLoadSettings CreateSaveLoadSettings()
        {
            // Check if settings already exist
            var existing = AssetDatabase.LoadAssetAtPath<SaveLoadSettings>(SETTINGS_PATH);
            if (existing != null)
            {
                Debug.Log("SaveLoadSettings already exists, using existing asset.");
                return existing;
            }

            // Create new settings
            var settings = ScriptableObject.CreateInstance<SaveLoadSettings>();

            // Configure default values
            settings.saveDirectory = "Saves";
            settings.saveFileExtension = ".sav";
            settings.useCompression = false;
            settings.useEncryption = false;
            settings.enableAutoSave = true;
            settings.autoSaveInterval = 300f;
            settings.maxAutoSaves = 3;
            settings.autoSaveOnQuit = true;
            settings.quickSaveSlotName = "QuickSave";
            settings.maxManualSaves = 0;
            settings.enableDebugLogging = true;
            settings.createBackupBeforeLoad = false;
            settings.confirmOverwrite = true;
            settings.showSaveNotifications = true;
            settings.notificationDuration = 2f;

            AssetDatabase.CreateAsset(settings, SETTINGS_PATH);
            AssetDatabase.SaveAssets();

            Debug.Log($" Created SaveLoadSettings at {SETTINGS_PATH}");
            return settings;
        }

        #endregion

        #region UI Creation

        private SaveLoadMenu CreateSaveLoadUI(GameObject itemPrefab)
        {
            // Find or create Canvas
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                canvas = CreateCanvas();
            }

            // Create main container with SaveLoadMenu component
            GameObject menuContainer = new GameObject("SaveLoadMenuSystem");
            menuContainer.AddComponent<RectTransform>();
            menuContainer.transform.SetParent(canvas.transform, false);

            // Create Main Menu Panel
            GameObject mainMenuPanel = CreateMainMenuPanel(menuContainer, itemPrefab);

            // Create Save Panel
            GameObject savePanel = CreateSavePanel(menuContainer, itemPrefab);

            // Create Load Panel
            GameObject loadPanel = CreateLoadPanel(menuContainer, itemPrefab);

            // Add SaveLoadMenu component to container
            var saveLoadMenu = menuContainer.AddComponent<SaveLoadMenu>();

            // Wire all references using SerializedObject
            var serializedMenu = new SerializedObject(saveLoadMenu);

            // Main panels
            serializedMenu.FindProperty("menuPanel").objectReferenceValue = mainMenuPanel;
            serializedMenu.FindProperty("savePanel").objectReferenceValue = savePanel;
            serializedMenu.FindProperty("loadPanel").objectReferenceValue = loadPanel;

            // Save Panel UI
            serializedMenu.FindProperty("saveNameInput").objectReferenceValue = savePanel.transform.Find("ContentPanel/SaveNameInput").GetComponent<TMP_InputField>();
            serializedMenu.FindProperty("performSaveButton").objectReferenceValue = savePanel.transform.Find("ContentPanel/PerformSaveButton").GetComponent<Button>();
            serializedMenu.FindProperty("cancelSaveButton").objectReferenceValue = savePanel.transform.Find("ContentPanel/CancelSaveButton").GetComponent<Button>();
            serializedMenu.FindProperty("saveListContentSave").objectReferenceValue = savePanel.transform.Find("ContentPanel/SaveListScrollView/Viewport/Content");
            serializedMenu.FindProperty("savePanelTitle").objectReferenceValue = savePanel.transform.Find("ContentPanel/Title").GetComponent<TextMeshProUGUI>();

            // Load Panel UI
            serializedMenu.FindProperty("saveListContentLoad").objectReferenceValue = loadPanel.transform.Find("ContentPanel/SaveListScrollView/Viewport/Content");
            serializedMenu.FindProperty("performLoadButton").objectReferenceValue = loadPanel.transform.Find("ContentPanel/PerformLoadButton").GetComponent<Button>();
            serializedMenu.FindProperty("cancelLoadButton").objectReferenceValue = loadPanel.transform.Find("ContentPanel/CancelLoadButton").GetComponent<Button>();
            serializedMenu.FindProperty("loadPanelTitle").objectReferenceValue = loadPanel.transform.Find("ContentPanel/Title").GetComponent<TextMeshProUGUI>();

            // Shared buttons (in main menu)
            serializedMenu.FindProperty("showSavePanelButton").objectReferenceValue = mainMenuPanel.transform.Find("ContentPanel/ShowSavePanelButton").GetComponent<Button>();
            serializedMenu.FindProperty("showLoadPanelButton").objectReferenceValue = mainMenuPanel.transform.Find("ContentPanel/ShowLoadPanelButton").GetComponent<Button>();
            serializedMenu.FindProperty("deleteButton").objectReferenceValue = mainMenuPanel.transform.Find("ContentPanel/ActionButtons/DeleteButton").GetComponent<Button>();
            serializedMenu.FindProperty("renameButton").objectReferenceValue = mainMenuPanel.transform.Find("ContentPanel/ActionButtons/RenameButton").GetComponent<Button>();
            serializedMenu.FindProperty("resumeButton").objectReferenceValue = mainMenuPanel.transform.Find("ContentPanel/ResumeButton").GetComponent<Button>();
            serializedMenu.FindProperty("backToMainMenuButton").objectReferenceValue = mainMenuPanel.transform.Find("ContentPanel/BackToMainMenuButton").GetComponent<Button>();
            serializedMenu.FindProperty("saveAndQuitButton").objectReferenceValue = mainMenuPanel.transform.Find("ContentPanel/SaveAndQuitButton").GetComponent<Button>();
            serializedMenu.FindProperty("quitWithoutSavingButton").objectReferenceValue = mainMenuPanel.transform.Find("ContentPanel/QuitWithoutSavingButton").GetComponent<Button>();
            serializedMenu.FindProperty("closeButton").objectReferenceValue = mainMenuPanel.transform.Find("ContentPanel/CloseButton").GetComponent<Button>();

            // Prefab and settings
            serializedMenu.FindProperty("saveListItemPrefab").objectReferenceValue = itemPrefab;
            serializedMenu.FindProperty("pauseGameWhenOpen").boolValue = true;
            serializedMenu.FindProperty("mainMenuSceneName").stringValue = "MainMenu";

            serializedMenu.ApplyModifiedProperties();
            EditorUtility.SetDirty(saveLoadMenu);

            // Initially hide all panels
            mainMenuPanel.SetActive(false);
            savePanel.SetActive(false);
            loadPanel.SetActive(false);

            Debug.Log(" Created Save/Load UI with separate Save/Load/Main panels");
            return saveLoadMenu;
        }

        private Canvas CreateCanvas()
        {
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            Debug.Log(" Created Canvas");
            return canvas;
        }

        private GameObject CreateMainMenuPanel(GameObject parent, GameObject itemPrefab)
        {
            GameObject menuPanel = CreatePanelBase(parent, "MainMenuPanel");
            GameObject contentPanel = menuPanel.transform.Find("ContentPanel").gameObject;

            // Create title
            CreateTitle(contentPanel, "Game Menu");

            // Create main buttons
            CreateMenuButton(contentPanel, "ResumeButton", "Resume Game", new Color(0.2f, 0.6f, 0.2f, 1f));
            CreateMenuButton(contentPanel, "ShowSavePanelButton", "Save Game", new Color(0.2f, 0.5f, 0.7f, 1f));
            CreateMenuButton(contentPanel, "ShowLoadPanelButton", "Load Game", new Color(0.5f, 0.4f, 0.7f, 1f));

            // Create action buttons (Delete, Rename)
            GameObject actionButtons = new GameObject("ActionButtons");
            actionButtons.transform.SetParent(contentPanel.transform, false);
            var actionRect = actionButtons.AddComponent<RectTransform>();
            actionRect.sizeDelta = new Vector2(0, 50);
            var horizontalLayout = actionButtons.AddComponent<HorizontalLayoutGroup>();
            horizontalLayout.spacing = 10;
            horizontalLayout.childControlWidth = true;
            horizontalLayout.childForceExpandWidth = true;
            var layoutElement = actionButtons.AddComponent<LayoutElement>();
            layoutElement.minHeight = 50;

            CreateButton(actionButtons, "DeleteButton", "Delete Save", new Color(0.8f, 0.2f, 0.2f, 1f));
            CreateButton(actionButtons, "RenameButton", "Rename Save", new Color(0.5f, 0.5f, 0.2f, 1f));

            // Create utility buttons
            CreateMenuButton(contentPanel, "BackToMainMenuButton", "Back to Main Menu", new Color(0.6f, 0.4f, 0.2f, 1f));
            CreateMenuButton(contentPanel, "SaveAndQuitButton", "Save and Quit", new Color(0.4f, 0.5f, 0.6f, 1f));
            CreateMenuButton(contentPanel, "QuitWithoutSavingButton", "Quit Without Saving", new Color(0.8f, 0.2f, 0.2f, 1f));
            CreateMenuButton(contentPanel, "CloseButton", "Close (ESC)", new Color(0.5f, 0.5f, 0.5f, 1f));

            return menuPanel;
        }

        private GameObject CreateSavePanel(GameObject parent, GameObject itemPrefab)
        {
            GameObject savePanel = CreatePanelBase(parent, "SavePanel");
            GameObject contentPanel = savePanel.transform.Find("ContentPanel").gameObject;

            // Create title
            CreateTitle(contentPanel, "Save Game");

            // Create save name input
            CreateSaveNameInput(contentPanel, "SaveNameInput");

            // Create save list
            CreateSaveList(contentPanel);

            // Create buttons
            CreateMenuButton(contentPanel, "PerformSaveButton", "Save", new Color(0.2f, 0.6f, 0.2f, 1f));
            CreateMenuButton(contentPanel, "CancelSaveButton", "Cancel", new Color(0.5f, 0.5f, 0.5f, 1f));

            return savePanel;
        }

        private GameObject CreateLoadPanel(GameObject parent, GameObject itemPrefab)
        {
            GameObject loadPanel = CreatePanelBase(parent, "LoadPanel");
            GameObject contentPanel = loadPanel.transform.Find("ContentPanel").gameObject;

            // Create title
            CreateTitle(contentPanel, "Load Game");

            // Create save list
            CreateSaveList(contentPanel);

            // Create buttons
            CreateMenuButton(contentPanel, "PerformLoadButton", "Load", new Color(0.2f, 0.4f, 0.8f, 1f));
            CreateMenuButton(contentPanel, "CancelLoadButton", "Cancel", new Color(0.5f, 0.5f, 0.5f, 1f));

            return loadPanel;
        }

        private GameObject CreatePanelBase(GameObject parent, string name)
        {
            GameObject panel = new GameObject(name);
            panel.AddComponent<RectTransform>();
            panel.transform.SetParent(parent.transform, false);

            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            panelRect.anchoredPosition = Vector2.zero;

            // Add background
            var bgImage = panel.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.8f);

            // Add CanvasGroup
            panel.AddComponent<CanvasGroup>();

            // Create content panel
            GameObject contentPanel = new GameObject("ContentPanel");
            contentPanel.transform.SetParent(panel.transform, false);

            var contentRect = contentPanel.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(600, 800);
            contentRect.anchoredPosition = Vector2.zero;

            var contentBg = contentPanel.AddComponent<Image>();
            contentBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Add vertical layout
            var verticalLayout = contentPanel.AddComponent<VerticalLayoutGroup>();
            verticalLayout.padding = new RectOffset(20, 20, 20, 20);
            verticalLayout.spacing = 10;
            verticalLayout.childControlHeight = false;
            verticalLayout.childControlWidth = true;
            verticalLayout.childForceExpandHeight = false;
            verticalLayout.childForceExpandWidth = true;

            return panel;
        }

        private void CreateTitle(GameObject parent, string text)
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(parent.transform, false);

            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(0, 60);

            var titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = text;
            titleText.fontSize = 36;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;

            var layoutElement = titleObj.AddComponent<LayoutElement>();
            layoutElement.minHeight = 60;
        }

        private Button CreateMenuButton(GameObject parent, string name, string text, Color color)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent.transform, false);

            var buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(0, 50);

            var buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = color;

            var button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonImage;

            var layoutElement = buttonObj.AddComponent<LayoutElement>();
            layoutElement.minHeight = 50;

            // Create button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = text;
            buttonText.fontSize = 22;
            buttonText.fontStyle = FontStyles.Bold;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;

            return button;
        }

        private TMP_InputField CreateSaveNameInput(GameObject parent, string name)
        {
            GameObject inputObj = new GameObject(name);
            inputObj.transform.SetParent(parent.transform, false);

            var inputRect = inputObj.AddComponent<RectTransform>();
            inputRect.sizeDelta = new Vector2(0, 50);

            var inputImage = inputObj.AddComponent<Image>();
            inputImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            var inputField = inputObj.AddComponent<TMP_InputField>();
            inputField.textViewport = inputRect;

            // Create text component
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(inputObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.fontSize = 18;
            text.color = Color.white;

            // Create placeholder
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(inputObj.transform, false);
            var placeholderRect = placeholderObj.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(10, 0);
            placeholderRect.offsetMax = new Vector2(-10, 0);

            var placeholder = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholder.text = "Enter save name...";
            placeholder.fontSize = 18;
            placeholder.fontStyle = FontStyles.Italic;
            placeholder.color = new Color(1, 1, 1, 0.5f);

            inputField.textComponent = text;
            inputField.placeholder = placeholder;

            var layoutElement = inputObj.AddComponent<LayoutElement>();
            layoutElement.minHeight = 50;

            return inputField;
        }


        private Button CreateButton(GameObject parent, string name, string text, Color color)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent.transform, false);

            var buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = color;

            var button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonImage;

            // Create button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = text;
            buttonText.fontSize = 20;
            buttonText.fontStyle = FontStyles.Bold;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;

            return button;
        }

        private GameObject CreateSaveList(GameObject parent)
        {
            GameObject scrollViewObj = new GameObject("SaveListScrollView");
            scrollViewObj.transform.SetParent(parent.transform, false);

            var scrollRect = scrollViewObj.AddComponent<RectTransform>();
            scrollRect.sizeDelta = new Vector2(0, 300);

            var scrollViewImage = scrollViewObj.AddComponent<Image>();
            scrollViewImage.color = new Color(0.1f, 0.1f, 0.1f, 1f);

            var scrollView = scrollViewObj.AddComponent<ScrollRect>();

            var layoutElement = scrollViewObj.AddComponent<LayoutElement>();
            layoutElement.flexibleHeight = 1;
            layoutElement.minHeight = 200;

            // Create viewport
            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollViewObj.transform, false);

            var viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;

            viewportObj.AddComponent<Mask>().showMaskGraphic = false;
            viewportObj.AddComponent<Image>();

            // Create content
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);

            var contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);

            var contentLayout = contentObj.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 5;
            contentLayout.padding = new RectOffset(5, 5, 5, 5);
            contentLayout.childControlHeight = false;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;

            var contentSizeFitter = contentObj.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Wire ScrollRect
            scrollView.content = contentRect;
            scrollView.viewport = viewportRect;
            scrollView.horizontal = false;
            scrollView.vertical = true;

            return scrollViewObj;
        }

        private GameObject CreateSaveListItemPrefab()
        {
            // Check if prefab already exists
            string prefabPath = $"{PREFABS_PATH}/SaveListItem.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null)
            {
                Debug.Log("SaveListItem prefab already exists.");
                return existing;
            }

            // Create item GameObject
            GameObject itemObj = new GameObject("SaveListItem");

            var itemRect = itemObj.AddComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(0, 80);

            // Add background
            var bgImage = itemObj.AddComponent<Image>();
            bgImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            // Add button for selection
            var selectButton = itemObj.AddComponent<Button>();
            selectButton.targetGraphic = bgImage;

            // Add layout element
            var layoutElement = itemObj.AddComponent<LayoutElement>();
            layoutElement.minHeight = 80;
            layoutElement.preferredHeight = 80;

            // Create content layout
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(itemObj.transform, false);

            var contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(10, 10);
            contentRect.offsetMax = new Vector2(-10, -10);

            var verticalLayout = contentObj.AddComponent<VerticalLayoutGroup>();
            verticalLayout.spacing = 5;

            // Create save name text
            CreateItemText(contentObj, "SaveNameText", 20, FontStyles.Bold, TextAlignmentOptions.Left);

            // Create horizontal layout for date and time
            GameObject infoPanel = new GameObject("InfoPanel");
            infoPanel.transform.SetParent(contentObj.transform, false);

            var infoLayout = infoPanel.AddComponent<HorizontalLayoutGroup>();
            infoLayout.spacing = 20;
            infoLayout.childControlHeight = false;
            infoLayout.childControlWidth = true;
            infoLayout.childForceExpandWidth = true;

            CreateItemText(infoPanel, "SaveDateText", 14, FontStyles.Normal, TextAlignmentOptions.Left);
            CreateItemText(infoPanel, "PlayTimeText", 14, FontStyles.Normal, TextAlignmentOptions.Right);

            // Add SaveListItem component
            var saveListItem = itemObj.AddComponent<SaveListItem>();

            // Wire references using SerializedObject
            var serializedItem = new SerializedObject(saveListItem);

            serializedItem.FindProperty("saveNameText").objectReferenceValue = itemObj.transform.Find("Content/SaveNameText").GetComponent<TextMeshProUGUI>();
            serializedItem.FindProperty("saveDateText").objectReferenceValue = itemObj.transform.Find("Content/InfoPanel/SaveDateText").GetComponent<TextMeshProUGUI>();
            serializedItem.FindProperty("playTimeText").objectReferenceValue = itemObj.transform.Find("Content/InfoPanel/PlayTimeText").GetComponent<TextMeshProUGUI>();
            serializedItem.FindProperty("backgroundImage").objectReferenceValue = bgImage;
            serializedItem.FindProperty("selectButton").objectReferenceValue = selectButton;

            // Set colors
            serializedItem.FindProperty("normalColor").colorValue = new Color(0.3f, 0.3f, 0.3f, 1f);
            serializedItem.FindProperty("selectedColor").colorValue = new Color(0.2f, 0.6f, 0.2f, 1f);
            serializedItem.FindProperty("autoSaveColor").colorValue = new Color(0.5f, 0.4f, 0.2f, 1f);
            serializedItem.FindProperty("quickSaveColor").colorValue = new Color(0.2f, 0.4f, 0.6f, 1f);

            serializedItem.ApplyModifiedProperties();

            // Save as prefab
            PrefabUtility.SaveAsPrefabAsset(itemObj, prefabPath);
            DestroyImmediate(itemObj);

            Debug.Log($" Created SaveListItem prefab at {prefabPath}");
            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        private void CreateItemText(GameObject parent, string name, float fontSize, FontStyles style, TextAlignmentOptions alignment)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent.transform, false);

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.text = name;
        }

        #endregion

        #region System GameObject Creation

        private GameObject CreateSaveLoadSystemObject()
        {
            // Check if already exists
            var existing = GameObject.Find("SaveLoadSystem");
            if (existing != null)
            {
                Debug.Log("SaveLoadSystem GameObject already exists.");
                return existing;
            }

            GameObject systemObj = new GameObject("SaveLoadSystem");

            // Add components
            systemObj.AddComponent<SaveLoadManager>();
            systemObj.AddComponent<AutoSaveSystem>();
            systemObj.AddComponent<SaveLoadInputHandler>();

            Debug.Log(" Created SaveLoadSystem GameObject with all components");
            return systemObj;
        }

        #endregion

        #region Reference Wiring

        private void WireReferences(GameObject systemObj, SaveLoadSettings settings, SaveLoadMenu menu, GameObject itemPrefab)
        {
            // Wire SaveLoadManager using SerializedObject
            var manager = systemObj.GetComponent<SaveLoadManager>();
            if (manager != null)
            {
                var serializedManager = new SerializedObject(manager);
                serializedManager.FindProperty("settings").objectReferenceValue = settings;
                serializedManager.FindProperty("mainCamera").objectReferenceValue = Camera.main;
                serializedManager.ApplyModifiedProperties();
                EditorUtility.SetDirty(manager);
                Debug.Log("- Wired SaveLoadManager");
            }

            // Wire AutoSaveSystem using SerializedObject
            var autoSave = systemObj.GetComponent<AutoSaveSystem>();
            if (autoSave != null)
            {
                var serializedAutoSave = new SerializedObject(autoSave);
                serializedAutoSave.FindProperty("settings").objectReferenceValue = settings;
                serializedAutoSave.ApplyModifiedProperties();
                EditorUtility.SetDirty(autoSave);
                Debug.Log("- Wired AutoSaveSystem");
            }

            // Wire SaveLoadInputHandler using SerializedObject
            var inputHandler = systemObj.GetComponent<SaveLoadInputHandler>();
            if (inputHandler != null)
            {
                var serializedInputHandler = new SerializedObject(inputHandler);
                serializedInputHandler.FindProperty("inGameMenu").objectReferenceValue = menu;
                serializedInputHandler.ApplyModifiedProperties();
                EditorUtility.SetDirty(inputHandler);
                Debug.Log("- Wired SaveLoadInputHandler");
            }

            // Double-check SaveLoadMenu has prefab (should already be set in CreateSaveLoadUI)
            if (menu != null && itemPrefab != null)
            {
                var serializedMenu = new SerializedObject(menu);
                var prefabProperty = serializedMenu.FindProperty("saveListItemPrefab");

                if (prefabProperty.objectReferenceValue == null)
                {
                    Debug.LogWarning("SaveListItemPrefab was null, assigning now...");
                    prefabProperty.objectReferenceValue = itemPrefab;
                    serializedMenu.ApplyModifiedProperties();
                }

                EditorUtility.SetDirty(menu);
                Debug.Log("- Verified SaveLoadMenu prefab reference");
            }

            EditorUtility.SetDirty(systemObj);
            Debug.Log(" All references wired successfully");
        }

        private void AutoWireReferences()
        {
            var systemObj = GameObject.Find("SaveLoadSystem");
            if (systemObj == null)
            {
                EditorUtility.DisplayDialog("Not Found", "SaveLoadSystem GameObject not found!", "OK");
                return;
            }

            var settings = AssetDatabase.LoadAssetAtPath<SaveLoadSettings>(SETTINGS_PATH);
            var menu = FindAnyObjectByType<SaveLoadMenu>();
            var itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/SaveListItem.prefab");

            if (settings == null || menu == null || itemPrefab == null)
            {
                EditorUtility.DisplayDialog(
                    "Missing Components",
                    $"Could not find all components:\n" +
                    $"Settings: {(settings != null ? "-" : "X")}\n" +
                    $"Menu: {(menu != null ? "-" : "X")}\n" +
                    $"Item Prefab: {(itemPrefab != null ? "-" : "X")}",
                    "OK"
                );
                return;
            }

            WireReferences(systemObj, settings, menu, itemPrefab);
            EditorUtility.DisplayDialog("Success", "All references wired successfully!", "OK");
        }

        #endregion

        #region GameManager Integration

        private void IntegrateWithGameManager(GameObject systemObj)
        {
            var gameManager = FindAnyObjectByType<RTS.Managers.GameManager>();
            if (gameManager == null)
            {
                Debug.LogWarning("GameManager not found in scene. SaveLoadManager will be auto-found at runtime.");
                return;
            }

            var manager = systemObj.GetComponent<SaveLoadManager>();
            if (manager != null)
            {
                var serializedGameManager = new SerializedObject(gameManager);
                serializedGameManager.FindProperty("saveLoadManager").objectReferenceValue = manager;
                serializedGameManager.ApplyModifiedProperties();
                EditorUtility.SetDirty(gameManager.gameObject);
                Debug.Log(" Integrated with GameManager");
            }
        }

        #endregion
    }
}
