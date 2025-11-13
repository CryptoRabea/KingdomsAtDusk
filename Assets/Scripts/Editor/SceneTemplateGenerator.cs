using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using RTS.Core.Services;

namespace RTS.Editor
{
    /// <summary>
    /// Master automation tool for creating complete game scenes.
    /// Sets up managers, UI, camera, and all necessary systems.
    /// Access via: Tools > RTS > Scene Template Generator
    /// </summary>
    public class SceneTemplateGenerator : EditorWindow
    {
        private enum TemplateType
        {
            CompleteGameScene,
            TestingScene,
            MinimalScene
        }

        private TemplateType templateType = TemplateType.CompleteGameScene;
        private string sceneName = "NewRTSScene";

        [Header("Scene Components")]
        private bool includeManagers = true;
        private bool includeUI = true;
        private bool includeCamera = true;
        private bool includeEventSystem = true;
        private bool includeLighting = true;
        private bool includePostProcessing = false;

        [Header("Manager Components")]
        private bool includeGameManager = true;
        private bool includeResourceManager = true;
        private bool includeHappinessManager = true;
        private bool includeBuildingManager = true;
        private bool includeWaveManager = true;

        [Header("UI Components")]
        private bool includeResourceUI = true;
        private bool includeHappinessUI = true;
        private bool includeNotificationUI = true;

        private Vector2 scrollPos;

        [MenuItem("Tools/RTS/Scene Template Generator")]
        public static void ShowWindow()
        {
            SceneTemplateGenerator window = GetWindow<SceneTemplateGenerator>("Scene Template Generator");
            window.minSize = new Vector2(450, 700);
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            GUILayout.Label("Scene Template Generator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Generates a complete RTS game scene with all necessary systems.\n" +
                "Choose a template type or customize components individually.",
                MessageType.Info);

            GUILayout.Space(10);

            // Template Selection
            templateType = (TemplateType)EditorGUILayout.EnumPopup("Template Type", templateType);

            switch (templateType)
            {
                case TemplateType.CompleteGameScene:
                    EditorGUILayout.HelpBox(
                        "Complete Game Scene: Includes all managers, full UI, camera, lighting, and event system.\n" +
                        "Perfect for starting a new game level!",
                        MessageType.Info);
                    break;
                case TemplateType.TestingScene:
                    EditorGUILayout.HelpBox(
                        "Testing Scene: Minimal setup with managers and basic UI.\n" +
                        "Great for prototyping and testing features.",
                        MessageType.Info);
                    break;
                case TemplateType.MinimalScene:
                    EditorGUILayout.HelpBox(
                        "Minimal Scene: Only essential components.\n" +
                        "Use this as a clean starting point.",
                        MessageType.Info);
                    break;
            }

            GUILayout.Space(10);

            sceneName = EditorGUILayout.TextField("Scene Name", sceneName);

            GUILayout.Space(10);

            // Component Selection
            GUILayout.Label("Scene Components", EditorStyles.boldLabel);
            includeManagers = EditorGUILayout.Toggle("Include Managers", includeManagers);
            includeUI = EditorGUILayout.Toggle("Include UI", includeUI);
            includeCamera = EditorGUILayout.Toggle("Include Camera", includeCamera);
            includeEventSystem = EditorGUILayout.Toggle("Include Event System", includeEventSystem);
            includeLighting = EditorGUILayout.Toggle("Include Lighting", includeLighting);
            includePostProcessing = EditorGUILayout.Toggle("Include Post Processing", includePostProcessing);

            GUILayout.Space(10);

            // Manager Details
            if (includeManagers)
            {
                GUILayout.Label("Manager Components", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                includeGameManager = EditorGUILayout.Toggle("Game Manager", includeGameManager);
                includeResourceManager = EditorGUILayout.Toggle("Resource Manager", includeResourceManager);
                includeHappinessManager = EditorGUILayout.Toggle("Happiness Manager", includeHappinessManager);
                includeBuildingManager = EditorGUILayout.Toggle("Building Manager", includeBuildingManager);
                includeWaveManager = EditorGUILayout.Toggle("Wave Manager", includeWaveManager);
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(10);

            // UI Details
            if (includeUI)
            {
                GUILayout.Label("UI Components", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                includeResourceUI = EditorGUILayout.Toggle("Resource UI", includeResourceUI);
                includeHappinessUI = EditorGUILayout.Toggle("Happiness UI", includeHappinessUI);
                includeNotificationUI = EditorGUILayout.Toggle("Notification UI", includeNotificationUI);
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(20);

            // Preview
            DrawScenePreview();

            GUILayout.Space(20);

            // Generate Button
            GUI.enabled = !string.IsNullOrEmpty(sceneName);
            if (GUILayout.Button("Generate Scene", GUILayout.Height(40)))
            {
                GenerateScene();
            }
            GUI.enabled = true;

            EditorGUILayout.EndScrollView();
        }

        private void DrawScenePreview()
        {
            GUILayout.Label("Scene Preview", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            if (includeManagers && includeGameManager)
                EditorGUILayout.LabelField("└─ GameManager");

            if (includeCamera)
                EditorGUILayout.LabelField("└─ RTSCamera");

            if (includeUI)
            {
                EditorGUILayout.LabelField("└─ Canvas");
                EditorGUI.indentLevel++;
                if (includeResourceUI) EditorGUILayout.LabelField("├─ ResourceUI");
                if (includeHappinessUI) EditorGUILayout.LabelField("├─ HappinessUI");
                if (includeNotificationUI) EditorGUILayout.LabelField("└─ NotificationUI");
                EditorGUI.indentLevel--;
            }

            if (includeEventSystem)
                EditorGUILayout.LabelField("└─ EventSystem");

            if (includeLighting)
                EditorGUILayout.LabelField("└─ Directional Light");

            EditorGUI.indentLevel--;
        }

        private void GenerateScene()
        {
            // Apply template presets
            ApplyTemplatePreset();

            // Create new scene
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Remove default objects we'll recreate
            GameObject[] rootObjects = newScene.GetRootGameObjects();
            foreach (GameObject obj in rootObjects)
            {
                if (obj.name == "Main Camera" && includeCamera)
                {
                    DestroyImmediate(obj);
                }
                else if (obj.name == "Directional Light" && !includeLighting)
                {
                    DestroyImmediate(obj);
                }
            }

            // Create managers
            if (includeManagers)
            {
                CreateManagersCall();
            }

            // Create camera
            if (includeCamera)
            {
                CreateCameraCall();
            }

            // Create UI
            if (includeUI)
            {
                CreateUICall();
            }

            // Create event system
            if (includeEventSystem)
            {
                CreateEventSystemCall();
            }

            // Save scene
            string scenePath = $"Assets/Scenes/{sceneName}.unity";
            string directory = System.IO.Path.GetDirectoryName(scenePath);

            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            EditorSceneManager.SaveScene(newScene, scenePath);

            Debug.Log($"✅✅✅ Scene generated successfully: {scenePath}");

            EditorUtility.DisplayDialog("Success!",
                $"Scene '{sceneName}' generated successfully!\n\n" +
                $"Location: {scenePath}\n\n" +
                GetSceneSummary(),
                "OK");
        }

        private void ApplyTemplatePreset()
        {
            switch (templateType)
            {
                case TemplateType.CompleteGameScene:
                    // All components enabled
                    includeManagers = true;
                    includeUI = true;
                    includeCamera = true;
                    includeEventSystem = true;
                    includeLighting = true;
                    includeGameManager = true;
                    includeResourceManager = true;
                    includeHappinessManager = true;
                    includeBuildingManager = true;
                    includeWaveManager = true;
                    includeResourceUI = true;
                    includeHappinessUI = true;
                    includeNotificationUI = true;
                    break;

                case TemplateType.TestingScene:
                    // Minimal for testing
                    includeManagers = true;
                    includeUI = true;
                    includeCamera = true;
                    includeEventSystem = true;
                    includeLighting = true;
                    includePostProcessing = false;
                    includeGameManager = true;
                    includeResourceManager = true;
                    includeHappinessManager = false;
                    includeBuildingManager = true;
                    includeWaveManager = false;
                    includeResourceUI = true;
                    includeHappinessUI = false;
                    includeNotificationUI = true;
                    break;

                case TemplateType.MinimalScene:
                    // Bare minimum
                    includeManagers = true;
                    includeUI = false;
                    includeCamera = true;
                    includeEventSystem = false;
                    includeLighting = true;
                    includePostProcessing = false;
                    includeGameManager = true;
                    includeResourceManager = false;
                    includeHappinessManager = false;
                    includeBuildingManager = false;
                    includeWaveManager = false;
                    break;
            }
        }

        #region Component Creation Helpers

        private void CreateManagersCall()
        {
            // This would call ManagerSetupTool logic
            // For now, create basic structure
            GameObject gameManager = new GameObject("GameManager");
            gameManager.AddComponent<RTS.Managers.GameManager>();

            if (includeResourceManager)
            {
                GameObject rm = new GameObject("ResourceManager");
                rm.transform.SetParent(gameManager.transform);
                rm.AddComponent<ResourceManager>();
            }

            if (includeHappinessManager)
            {
                GameObject hm = new GameObject("HappinessManager");
                hm.transform.SetParent(gameManager.transform);
                hm.AddComponent<RTS.Managers.HappinessManager>();
            }

            if (includeBuildingManager)
            {
                GameObject bm = new GameObject("BuildingManager");
                bm.transform.SetParent(gameManager.transform);
                bm.AddComponent<RTS.Managers.BuildingManager>();
            }

            if (includeWaveManager)
            {
                GameObject wm = new GameObject("WaveManager");
                wm.transform.SetParent(gameManager.transform);
                wm.AddComponent<RTS.Managers.WaveManager>();
            }

            Debug.Log("✅ Managers created");
        }

        private void CreateCameraCall()
        {
            GameObject cameraObj = new GameObject("RTSCamera");
            Camera camera = cameraObj.AddComponent<Camera>();
            camera.tag = "MainCamera";

            cameraObj.transform.position = new Vector3(0, 15, -10);
            cameraObj.transform.eulerAngles = new Vector3(45, 0, 0);

            RTSCameraController controller = cameraObj.AddComponent<RTSCameraController>();

            Debug.Log("✅ Camera created");
        }

        private void CreateUICall()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Create UI components (simplified)
            if (includeResourceUI)
            {
                GameObject resourceUI = new GameObject("ResourceUI");
                resourceUI.transform.SetParent(canvasObj.transform, false);
                // Add ResourceUI component
            }

            if (includeHappinessUI)
            {
                GameObject happinessUI = new GameObject("HappinessUI");
                happinessUI.transform.SetParent(canvasObj.transform, false);
                // Add HappinessUI component
            }

            if (includeNotificationUI)
            {
                GameObject notificationUI = new GameObject("NotificationUI");
                notificationUI.transform.SetParent(canvasObj.transform, false);
            }

            Debug.Log("✅ UI created");
        }

        private void CreateEventSystemCall()
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            Debug.Log("✅ Event System created");
        }

        #endregion

        private string GetSceneSummary()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            if (includeManagers) sb.AppendLine("✓ Manager System");
            if (includeUI) sb.AppendLine("✓ UI System");
            if (includeCamera) sb.AppendLine("✓ RTS Camera");
            if (includeEventSystem) sb.AppendLine("✓ Event System");
            if (includeLighting) sb.AppendLine("✓ Lighting");

            return sb.ToString();
        }
    }
}
