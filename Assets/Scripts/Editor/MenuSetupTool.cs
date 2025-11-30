using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using RTS.UI;

namespace RTS.Editor
{
    /// <summary>
    /// Auto-setup tool for creating Main Menu and Loading Screen systems.
    /// </summary>
    public class MenuSetupTool : EditorWindow
    {
        private string mainMenuSceneName = "MainMenu";
        private string gameSceneName = "GameScene";
        private bool createMainMenuScene = true;
        private bool createLoadingScreen = true;
        private Color menuBackgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);
        private Color buttonColor = new Color(0.2f, 0.6f, 0.9f, 1f);

        [MenuItem("Tools/RTS/Setup/Main Menu & Loading Screen Setup")]
        private static void ShowWindow()
        {
            MenuSetupTool window = GetWindow<MenuSetupTool>("Menu Setup Tool");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Main Menu & Loading Screen Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This tool will create:\n" +
                "- Main Menu scene with UI\n" +
                "- Loading Screen prefab\n" +
                "- Scene Transition Manager\n" +
                "- All necessary scripts and components",
                MessageType.Info);

            EditorGUILayout.Space();

            // Configuration
            GUILayout.Label("Configuration", EditorStyles.boldLabel);
            createMainMenuScene = EditorGUILayout.Toggle("Create Main Menu Scene", createMainMenuScene);
            createLoadingScreen = EditorGUILayout.Toggle("Create Loading Screen", createLoadingScreen);

            EditorGUILayout.Space();

            mainMenuSceneName = EditorGUILayout.TextField("Main Menu Scene Name", mainMenuSceneName);
            gameSceneName = EditorGUILayout.TextField("Game Scene Name", gameSceneName);

            EditorGUILayout.Space();

            menuBackgroundColor = EditorGUILayout.ColorField("Menu Background Color", menuBackgroundColor);
            buttonColor = EditorGUILayout.ColorField("Button Color", buttonColor);

            EditorGUILayout.Space(20);

            // Setup buttons
            if (GUILayout.Button("Setup Everything", GUILayout.Height(40)))
            {
                SetupEverything();
            }

            EditorGUILayout.Space();

            GUILayout.Label("Individual Setup", EditorStyles.boldLabel);

            if (GUILayout.Button("Setup Main Menu Only"))
            {
                SetupMainMenu();
            }

            if (GUILayout.Button("Setup Loading Screen Only"))
            {
                SetupLoadingScreen();
            }

            if (GUILayout.Button("Setup Scene Transition Manager"))
            {
                SetupSceneTransitionManager();
            }
        }

        private void SetupEverything()
        {
            if (EditorUtility.DisplayDialog(
                "Setup Main Menu & Loading Screen",
                "This will create all menu and loading systems. Continue?",
                "Yes", "Cancel"))
            {
                Debug.Log("=== Starting Menu & Loading Screen Setup ===");

                if (createLoadingScreen)
                {
                    SetupLoadingScreen();
                }

                if (createMainMenuScene)
                {
                    SetupMainMenu();
                }

                SetupSceneTransitionManager();

                Debug.Log("=== Setup Complete ===");

                EditorUtility.DisplayDialog(
                    "Setup Complete",
                    "Main Menu and Loading Screen systems have been created!\n\n" +
                    "Next steps:\n" +
                    "1. Check the MainMenu scene\n" +
                    "2. Customize the UI as needed\n" +
                    "3. Add scenes to Build Settings\n" +
                    "4. Test the transitions",
                    "OK");
            }
        }

        private void SetupMainMenu()
        {
            Debug.Log("[MenuSetup] Creating Main Menu scene...");

            // Create or load Main Menu scene
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            newScene.name = mainMenuSceneName;

            // Create Canvas
            GameObject canvasObj = new GameObject("MainMenuCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Create EventSystem if not exists
            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Create Background
            GameObject backgroundObj = new GameObject("Background");
            backgroundObj.transform.SetParent(canvasObj.transform, false);
            Image backgroundImg = backgroundObj.AddComponent<Image>();
            backgroundImg.color = menuBackgroundColor;
            RectTransform bgRect = backgroundObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Create Main Menu Panel
            GameObject mainMenuPanel = CreatePanel(canvasObj.transform, "MainMenuPanel");

            // Create Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(mainMenuPanel.transform, false);
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "Kingdoms At Dusk";
            titleText.fontSize = 72;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.7f);
            titleRect.anchorMax = new Vector2(0.5f, 0.9f);
            titleRect.sizeDelta = new Vector2(800, 200);

            // Create Button Container
            GameObject buttonContainer = new GameObject("ButtonContainer");
            buttonContainer.transform.SetParent(mainMenuPanel.transform, false);
            VerticalLayoutGroup layout = buttonContainer.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = false;
            RectTransform containerRect = buttonContainer.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.3f);
            containerRect.anchorMax = new Vector2(0.5f, 0.6f);
            containerRect.sizeDelta = new Vector2(400, 400);

            // Create Buttons
            Button newGameBtn = CreateMenuButton(buttonContainer.transform, "NewGameButton", "New Game");
            Button continueBtn = CreateMenuButton(buttonContainer.transform, "ContinueButton", "Continue");
            Button settingsBtn = CreateMenuButton(buttonContainer.transform, "SettingsButton", "Settings");
            Button creditsBtn = CreateMenuButton(buttonContainer.transform, "CreditsButton", "Credits");
            Button quitBtn = CreateMenuButton(buttonContainer.transform, "QuitButton", "Quit");

            // Create Settings Panel (initially hidden)
            GameObject settingsPanel = CreatePanel(canvasObj.transform, "SettingsPanel");
            settingsPanel.SetActive(false);
            CreateSettingsPanelContent(settingsPanel.transform);

            // Create Credits Panel (initially hidden)
            GameObject creditsPanel = CreatePanel(canvasObj.transform, "CreditsPanel");
            creditsPanel.SetActive(false);
            CreateCreditsPanelContent(creditsPanel.transform);

            // Create Version Text
            GameObject versionObj = new GameObject("VersionText");
            versionObj.transform.SetParent(canvasObj.transform, false);
            TextMeshProUGUI versionText = versionObj.AddComponent<TextMeshProUGUI>();
            versionText.text = "v1.0.0";
            versionText.fontSize = 24;
            versionText.alignment = TextAlignmentOptions.BottomRight;
            versionText.color = new Color(1, 1, 1, 0.5f);
            RectTransform versionRect = versionObj.GetComponent<RectTransform>();
            versionRect.anchorMin = new Vector2(1, 0);
            versionRect.anchorMax = new Vector2(1, 0);
            versionRect.pivot = new Vector2(1, 0);
            versionRect.anchoredPosition = new Vector2(-20, 20);
            versionRect.sizeDelta = new Vector2(200, 50);

            // Add MainMenuManager component
            GameObject managerObj = new GameObject("MainMenuManager");
            MainMenuManager menuManager = managerObj.AddComponent<MainMenuManager>();
            SerializedObject so = new SerializedObject(menuManager);
            so.FindProperty("mainMenuPanel").objectReferenceValue = mainMenuPanel;
            so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            so.FindProperty("creditsPanel").objectReferenceValue = creditsPanel;
            so.FindProperty("newGameButton").objectReferenceValue = newGameBtn;
            so.FindProperty("continueButton").objectReferenceValue = continueBtn;
            so.FindProperty("settingsButton").objectReferenceValue = settingsBtn;
            so.FindProperty("creditsButton").objectReferenceValue = creditsBtn;
            so.FindProperty("quitButton").objectReferenceValue = quitBtn;
            so.FindProperty("versionText").objectReferenceValue = versionText;

            Button backFromSettings = settingsPanel.GetComponentInChildren<Button>();
            Button backFromCredits = creditsPanel.GetComponentInChildren<Button>();
            if (backFromSettings != null)
                so.FindProperty("backFromSettingsButton").objectReferenceValue = backFromSettings;
            if (backFromCredits != null)
                so.FindProperty("backFromCreditsButton").objectReferenceValue = backFromCredits;

            so.ApplyModifiedProperties();

            // Save scene
            string scenePath = $"Assets/Scenes/{mainMenuSceneName}.unity";
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(newScene, scenePath);

            Debug.Log($"[MenuSetup] Main Menu scene created at: {scenePath}");
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Main Menu Created",
                $"Main Menu scene created at:\n{scenePath}\n\n" +
                "Don't forget to add it to Build Settings!",
                "OK");
        }

        private GameObject CreatePanel(Transform parent, string name)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            Image panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0, 0, 0, 0); // Transparent
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return panel;
        }

        private Button CreateMenuButton(Transform parent, string name, string text)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            // Add Button component
            Button button = btnObj.AddComponent<Button>();
            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = buttonColor;

            // Setup button colors
            ColorBlock colors = button.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = buttonColor * 1.2f;
            colors.pressedColor = buttonColor * 0.8f;
            button.colors = colors;

            // Add text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = text;
            buttonText.fontSize = 32;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // Set button size
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(300, 60);

            return button;
        }

        private void CreateSettingsPanelContent(Transform parent)
        {
            GameObject titleObj = new GameObject("SettingsTitle");
            titleObj.transform.SetParent(parent, false);
            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = "Settings";
            title.fontSize = 48;
            title.alignment = TextAlignmentOptions.Center;
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.8f);
            titleRect.anchorMax = new Vector2(0.5f, 0.9f);
            titleRect.sizeDelta = new Vector2(400, 100);

            // Back button
            CreateMenuButton(parent, "BackButton", "Back");
        }

        private void CreateCreditsPanelContent(Transform parent)
        {
            GameObject titleObj = new GameObject("CreditsTitle");
            titleObj.transform.SetParent(parent, false);
            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = "Credits";
            title.fontSize = 48;
            title.alignment = TextAlignmentOptions.Center;
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.8f);
            titleRect.anchorMax = new Vector2(0.5f, 0.9f);
            titleRect.sizeDelta = new Vector2(400, 100);

            GameObject creditsText = new GameObject("CreditsText");
            creditsText.transform.SetParent(parent, false);
            TextMeshProUGUI text = creditsText.AddComponent<TextMeshProUGUI>();
            text.text = "Developed by: Your Studio\nPowered by Unity\n\nThanks for playing!";
            text.fontSize = 24;
            text.alignment = TextAlignmentOptions.Center;
            RectTransform textRect = creditsText.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.4f);
            textRect.anchorMax = new Vector2(0.5f, 0.7f);
            textRect.sizeDelta = new Vector2(600, 300);

            // Back button
            CreateMenuButton(parent, "BackButton", "Back");
        }

        private void SetupLoadingScreen()
        {
            Debug.Log("[MenuSetup] Creating Loading Screen prefab...");

            // Create Loading Screen GameObject
            GameObject loadingScreenObj = new GameObject("LoadingScreen");

            // Add Canvas
            Canvas canvas = loadingScreenObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; // Always on top

            loadingScreenObj.AddComponent<CanvasScaler>();
            CanvasScaler scaler = loadingScreenObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            loadingScreenObj.AddComponent<GraphicRaycaster>();

            // Add LoadingScreenManager
            LoadingScreenManager manager = loadingScreenObj.AddComponent<LoadingScreenManager>();

            // Create root panel
            GameObject rootPanel = new GameObject("LoadingScreenRoot");
            rootPanel.transform.SetParent(loadingScreenObj.transform, false);
            Image panelImg = rootPanel.AddComponent<Image>();
            panelImg.color = new Color(0, 0, 0, 0.95f);
            RectTransform rootRect = rootPanel.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // Create progress bar background
            GameObject progressBg = new GameObject("ProgressBarBackground");
            progressBg.transform.SetParent(rootPanel.transform, false);
            Image progressBgImg = progressBg.AddComponent<Image>();
            progressBgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            RectTransform progressBgRect = progressBg.GetComponent<RectTransform>();
            progressBgRect.anchorMin = new Vector2(0.5f, 0.4f);
            progressBgRect.anchorMax = new Vector2(0.5f, 0.4f);
            progressBgRect.pivot = new Vector2(0.5f, 0.5f);
            progressBgRect.sizeDelta = new Vector2(600, 30);

            // Create progress bar
            GameObject progressBarObj = new GameObject("ProgressBar");
            progressBarObj.transform.SetParent(rootPanel.transform, false);
            Slider progressBar = progressBarObj.AddComponent<Slider>();
            RectTransform progressBarRect = progressBarObj.GetComponent<RectTransform>();
            progressBarRect.anchorMin = new Vector2(0.5f, 0.4f);
            progressBarRect.anchorMax = new Vector2(0.5f, 0.4f);
            progressBarRect.pivot = new Vector2(0.5f, 0.5f);
            progressBarRect.sizeDelta = new Vector2(600, 30);

            // Create fill for progress bar
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(progressBarObj.transform, false);
            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.8f, 1f, 1f);
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            progressBar.fillRect = fillRect;
            progressBar.value = 0;

            // Create progress text
            GameObject progressTextObj = new GameObject("ProgressText");
            progressTextObj.transform.SetParent(rootPanel.transform, false);
            TextMeshProUGUI progressText = progressTextObj.AddComponent<TextMeshProUGUI>();
            progressText.text = "Loading...";
            progressText.fontSize = 32;
            progressText.alignment = TextAlignmentOptions.Center;
            progressText.color = Color.white;
            RectTransform progressTextRect = progressTextObj.GetComponent<RectTransform>();
            progressTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            progressTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            progressTextRect.pivot = new Vector2(0.5f, 0.5f);
            progressTextRect.anchoredPosition = new Vector2(0, -100);
            progressTextRect.sizeDelta = new Vector2(800, 100);

            // Create loading tip text
            GameObject tipTextObj = new GameObject("LoadingTip");
            tipTextObj.transform.SetParent(rootPanel.transform, false);
            TextMeshProUGUI tipText = tipTextObj.AddComponent<TextMeshProUGUI>();
            tipText.text = "Tip: This is a loading tip";
            tipText.fontSize = 24;
            tipText.alignment = TextAlignmentOptions.Center;
            tipText.color = new Color(1, 1, 1, 0.7f);
            RectTransform tipTextRect = tipTextObj.GetComponent<RectTransform>();
            tipTextRect.anchorMin = new Vector2(0.5f, 0.2f);
            tipTextRect.anchorMax = new Vector2(0.5f, 0.2f);
            tipTextRect.pivot = new Vector2(0.5f, 0.5f);
            tipTextRect.sizeDelta = new Vector2(1000, 100);

            // Assign references to manager
            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("loadingScreenRoot").objectReferenceValue = rootPanel;
            so.FindProperty("progressBar").objectReferenceValue = progressBar;
            so.FindProperty("progressText").objectReferenceValue = progressText;
            so.FindProperty("loadingTipText").objectReferenceValue = tipText;
            so.ApplyModifiedProperties();

            // Save as prefab
            string prefabPath = "Assets/Prefabs/UI/LoadingScreen.prefab";
            System.IO.Directory.CreateDirectory("Assets/Prefabs/UI");
            PrefabUtility.SaveAsPrefabAsset(loadingScreenObj, prefabPath);

            // Clean up temporary object
            DestroyImmediate(loadingScreenObj);

            Debug.Log($"[MenuSetup] Loading Screen prefab created at: {prefabPath}");
            AssetDatabase.Refresh();

            // Select the prefab in the project window
            Object prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                EditorGUIUtility.PingObject(prefab);
                Selection.activeObject = prefab;
            }

            EditorUtility.DisplayDialog("Loading Screen Created",
                $"Loading Screen prefab created at:\n{prefabPath}\n\n" +
                "Drag it into your scene when needed, or it will be instantiated automatically at runtime.",
                "OK");
        }

        private void SetupSceneTransitionManager()
        {
            Debug.Log("[MenuSetup] Setting up Scene Transition Manager...");

            GameObject managerObj = new GameObject("SceneTransitionManager");
            SceneTransitionManager manager = managerObj.AddComponent<SceneTransitionManager>();

            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("mainMenuSceneName").stringValue = mainMenuSceneName;
            so.FindProperty("gameSceneName").stringValue = gameSceneName;
            so.ApplyModifiedProperties();

            Debug.Log("[MenuSetup] Scene Transition Manager created");

            EditorUtility.DisplayDialog("Scene Transition Manager Created",
                "SceneTransitionManager has been added to the scene!\n\n" +
                "It will persist across scenes using DontDestroyOnLoad.",
                "OK");
        }
    }
}
