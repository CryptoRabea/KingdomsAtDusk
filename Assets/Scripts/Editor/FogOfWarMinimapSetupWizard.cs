using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.Rendering;
using RTS.UI;
using RTS.UI.Minimap;
using System.IO;

namespace RTS.Editor
{
    /// <summary>
    /// Automated setup wizard for Fog of War and Minimap systems.
    /// Creates all necessary GameObjects, configurations, and materials for URP.
    /// </summary>
    public class FogOfWarMinimapSetupWizard : EditorWindow
    {
        private Vector2 worldMin = new Vector2(-500f, -500f);
        private Vector2 worldMax = new Vector2(500f, 500f);
        private float cellSize = 2f;
        private int localPlayerId = 0;
        private Color fogColor = new Color(0f, 0f, 0.2f, 0.8f);
        private float fogHeight = 50f;
        private float minimapCameraHeight = 500f;
        private int renderTextureSize = 512;
        private LayerMask minimapLayers = -1;

        private Canvas targetCanvas;
        private RTSCameraController cameraController;

        [MenuItem("Tools/RTS/Fog of War & Minimap Setup Wizard")]
        public static void ShowWindow()
        {
            var window = GetWindow<FogOfWarMinimapSetupWizard>("FoW & Minimap Setup");
            window.minSize = new Vector2(450, 600);
            window.Show();
        }

        private void OnEnable()
        {
            // Try to find existing camera controller
            cameraController = FindFirstObjectByType<RTSCameraController>();

            // Try to find existing canvas
            targetCanvas = FindFirstObjectByType<Canvas>();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("FOG OF WAR & MINIMAP AUTOMATED SETUP", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This wizard will automatically set up the complete Fog of War and Minimap system with URP compatibility.", MessageType.Info);

            EditorGUILayout.Space(10);

            // World Bounds Section
            EditorGUILayout.LabelField("WORLD BOUNDS", EditorStyles.boldLabel);
            worldMin = EditorGUILayout.Vector2Field("World Min (X, Z)", worldMin);
            worldMax = EditorGUILayout.Vector2Field("World Max (X, Z)", worldMax);
            cellSize = EditorGUILayout.FloatField("Grid Cell Size", cellSize);

            EditorGUILayout.Space(10);

            // Fog of War Settings
            EditorGUILayout.LabelField("FOG OF WAR SETTINGS", EditorStyles.boldLabel);
            localPlayerId = EditorGUILayout.IntField("Local Player ID", localPlayerId);
            fogColor = EditorGUILayout.ColorField("Fog Color", fogColor);
            fogHeight = EditorGUILayout.FloatField("Fog Height", fogHeight);

            EditorGUILayout.Space(10);

            // Minimap Settings
            EditorGUILayout.LabelField("MINIMAP SETTINGS", EditorStyles.boldLabel);
            targetCanvas = (Canvas)EditorGUILayout.ObjectField("Target Canvas", targetCanvas, typeof(Canvas), true);
            cameraController = (RTSCameraController)EditorGUILayout.ObjectField("RTS Camera Controller", cameraController, typeof(RTSCameraController), true);
            minimapCameraHeight = EditorGUILayout.FloatField("Minimap Camera Height", minimapCameraHeight);
            renderTextureSize = EditorGUILayout.IntField("Render Texture Size", renderTextureSize);
            minimapLayers.value = EditorGUILayout.MaskField(
                "Minimap Layers",
                minimapLayers.value,
                UnityEditorInternal.InternalEditorUtility.layers
            );

            EditorGUILayout.Space(20);

            // Setup Button
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("SETUP EVERYTHING AUTOMATICALLY", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("Confirm Setup",
                    "This will create:\n\n" +
                    "• GameBoundary ScriptableObject\n" +
                    "• MinimapConfig ScriptableObject\n" +
                    "• FogOfWarManager GameObject\n" +
                    "• Minimap UI (Canvas if needed)\n" +
                    "• URP Fog Material\n" +
                    "• All necessary renderers and components\n\n" +
                    "Continue?", "Yes", "Cancel"))
                {
                    SetupEverything();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);

            // Cleanup Button
            GUI.backgroundColor = new Color(1f, 0.5f, 0f);
            if (GUILayout.Button("CLEANUP UNUSED SCRIPTS", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Confirm Cleanup",
                    "This will DELETE unused/legacy scripts:\n\n" +
                    "• SimpleToon camera scripts\n" +
                    "• AOSFogWar legacy system\n" +
                    "• Legacy shaders\n" +
                    "• Shadowcaster.cs\n\n" +
                    "This action cannot be undone. Continue?", "Yes, Delete", "Cancel"))
                {
                    CleanupUnusedScripts();
                }
            }
            GUI.backgroundColor = Color.white;
        }

        private void SetupEverything()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Setup", "Creating directories...", 0.1f);
                CreateDirectories();

                EditorUtility.DisplayProgressBar("Setup", "Creating MinimapConfig...", 0.3f);
                MinimapConfig minimapConfig = CreateMinimapConfig();

                EditorUtility.DisplayProgressBar("Setup", "Creating URP Fog Material...", 0.4f);
                Material fogMaterial = CreateURPFogMaterial();

                EditorUtility.DisplayProgressBar("Setup", "Setting up Fog of War Manager...", 0.5f);
                GameObject fogManager = SetupFogOfWarManager(fogMaterial);

                EditorUtility.DisplayProgressBar("Setup", "Setting up Minimap UI...", 0.7f);
                SetupMinimapUI(minimapConfig);

                EditorUtility.DisplayProgressBar("Setup", "Finalizing...", 0.9f);

                // Save all assets
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.ClearProgressBar();

                EditorUtility.DisplayDialog("Success!",
                    "Fog of War and Minimap setup completed successfully!\n\n" +
                    "Created:\n" +
                    "• MinimapConfig at Assets/Configs/MinimapConfig.asset\n" +
                    "• FogOfWarManager in scene\n" +
                    "• Minimap UI in scene\n" +
                    "• URP Fog Material\n\n" +
                    "Next steps:\n" +
                    "1. Add VisionProvider components to your units/buildings\n" +
                    "2. Add MinimapEntity components to entities you want on minimap\n" +
                    "3. Configure player IDs and team settings\n" +
                    "4. Adjust fog colors and settings in FogOfWarManager", "OK");

                // Select the fog manager for easy access
                Selection.activeGameObject = fogManager;
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Setup Failed", $"An error occurred during setup:\n\n{e.Message}\n\n{e.StackTrace}", "OK");
                Debug.LogError($"Setup failed: {e}");
            }
        }

        private void CreateDirectories()
        {
            string configsPath = "Assets/Configs";
            string materialsPath = "Assets/Materials/FogOfWar";

            if (!AssetDatabase.IsValidFolder(configsPath))
            {
                AssetDatabase.CreateFolder("Assets", "Configs");
            }

            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }

            if (!AssetDatabase.IsValidFolder(materialsPath))
            {
                AssetDatabase.CreateFolder("Assets/Materials", "FogOfWar");
            }
        }

        private MinimapConfig CreateMinimapConfig()
        {
            string path = "Assets/Configs/MinimapConfig.asset";

            // Check if already exists
            MinimapConfig existing = AssetDatabase.LoadAssetAtPath<MinimapConfig>(path);
            if (existing != null)
            {
                Debug.Log("MinimapConfig already exists, updating values...");
                UpdateMinimapConfig(existing);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            // Create new
            MinimapConfig config = ScriptableObject.CreateInstance<MinimapConfig>();
            UpdateMinimapConfig(config);

            AssetDatabase.CreateAsset(config, path);
            Debug.Log($"Created MinimapConfig at {path}");

            return config;
        }

        private void UpdateMinimapConfig(MinimapConfig config)
        {
            config.worldMin = worldMin;
            config.worldMax = worldMax;
            config.minimapCameraHeight = minimapCameraHeight;
            config.renderTextureSize = renderTextureSize;
            config.minimapLayers = minimapLayers;
        }

        private Material CreateURPFogMaterial()
        {
            string materialPath = "Assets/Materials/FogOfWar/FogOfWar_URP.mat";

            // Check if already exists
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (existing != null)
            {
                Debug.Log("URP Fog Material already exists, updating...");
                existing.SetColor("_Color", fogColor);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            // Find the URP shader
            Shader fogShader = Shader.Find("FogWar/FogPlane_URP");
            if (fogShader == null)
            {
                Debug.LogError("Could not find URP Fog Shader 'FogWar/FogPlane_URP'! Make sure the shader is in Assets/Shaders/FogOfWar_URP.shader");

                // Try fallback to legacy shader
                fogShader = Shader.Find("FogWar/FogPlane");
                if (fogShader == null)
                {
                    Debug.LogError("Fallback shader also not found! Creating material anyway, you'll need to assign shader manually.");
                    fogShader = Shader.Find("Universal Render Pipeline/Lit");
                }
            }

            // Create material
            Material fogMaterial = new Material(fogShader);
            fogMaterial.name = "FogOfWar_URP";
            fogMaterial.SetColor("_Color", fogColor);
            fogMaterial.SetFloat("_BlurOffset", 2f);
            fogMaterial.SetFloat("_RevealThreshold", 0.5f);
            fogMaterial.SetFloat("_RevealSoftness", 0.1f);

            AssetDatabase.CreateAsset(fogMaterial, materialPath);
            Debug.Log($"Created URP Fog Material at {materialPath} with shader: {fogShader.name}");

            return fogMaterial;
        }

        private GameObject SetupFogOfWarManager(Material fogMaterial)
        {
            // Check if already exists
            FogOfWarManager existing = FindFirstObjectByType<FogOfWarManager>();
            if (existing != null)
            {
                Debug.Log("FogOfWarManager already exists, updating configuration...");
                UpdateFogOfWarManager(existing, fogMaterial);
                return existing.gameObject;
            }

            // Create new GameObject
            GameObject fogManagerObj = new GameObject("FogOfWarManager");
            FogOfWarManager manager = fogManagerObj.AddComponent<FogOfWarManager>();

            // Set configuration via SerializedObject (since fields are private)
            SerializedObject so = new SerializedObject(manager);

            // Create config and set GameBoundary
            SerializedProperty configProp = so.FindProperty("config");
            SerializedProperty boundaryProp = configProp.FindPropertyRelative("gameBoundary");

            // Set boundary values
            Vector3 center = new Vector3(
                (worldMin.x + worldMax.x) / 2f,
                0f,
                (worldMin.y + worldMax.y) / 2f
            );
            Vector3 size = new Vector3(
                worldMax.x - worldMin.x,
                100f,
                worldMax.y - worldMin.y
            );

            boundaryProp.FindPropertyRelative("center").vector3Value = center;
            boundaryProp.FindPropertyRelative("size").vector3Value = size;
            boundaryProp.FindPropertyRelative("cellSize").floatValue = cellSize;

            configProp.FindPropertyRelative("updateInterval").floatValue = 0.2f;
            configProp.FindPropertyRelative("defaultVisionRadius").floatValue = 15f;
            configProp.FindPropertyRelative("buildingVisionMultiplier").floatValue = 1.5f;
            configProp.FindPropertyRelative("fadeSpeed").floatValue = 2f;
            configProp.FindPropertyRelative("maxCellUpdatesPerFrame").intValue = 100;
            configProp.FindPropertyRelative("enableDebugVisualization").boolValue = false;

            // Set colors
            configProp.FindPropertyRelative("unexploredColor").colorValue = new Color(0, 0, 0, 1);
            configProp.FindPropertyRelative("exploredColor").colorValue = new Color(0, 0, 0, 0.5f);
            configProp.FindPropertyRelative("visibleColor").colorValue = new Color(0, 0, 0, 0);

            so.FindProperty("localPlayerId").intValue = localPlayerId;

            // Create Fog Renderer BEFORE applying SerializedObject
            GameObject rendererObj = new GameObject("FogRenderer");
            rendererObj.transform.SetParent(fogManagerObj.transform);
            FogOfWarRenderer renderer = rendererObj.AddComponent<FogOfWarRenderer>();

            // Link renderer to manager
            so.FindProperty("fogRenderer").objectReferenceValue = renderer;

            // Apply all changes to manager at once
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(manager);

            // Now configure the renderer
            SerializedObject rendererSO = new SerializedObject(renderer);
            rendererSO.FindProperty("fogMaterial").objectReferenceValue = fogMaterial;
            rendererSO.FindProperty("fogHeight").floatValue = fogHeight;
            rendererSO.FindProperty("chunksPerUpdate").intValue = 10;
            rendererSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(renderer);

            Debug.Log("Created FogOfWarManager with renderer in scene");

            return fogManagerObj;
        }

        private void UpdateFogOfWarManager(FogOfWarManager manager, Material fogMaterial)
        {
            SerializedObject so = new SerializedObject(manager);
            SerializedProperty configProp = so.FindProperty("config");

            // Update boundary values
            SerializedProperty boundaryProp = configProp.FindPropertyRelative("gameBoundary");
            Vector3 center = new Vector3(
                (worldMin.x + worldMax.x) / 2f,
                0f,
                (worldMin.y + worldMax.y) / 2f
            );
            Vector3 size = new Vector3(
                worldMax.x - worldMin.x,
                100f,
                worldMax.y - worldMin.y
            );

            boundaryProp.FindPropertyRelative("center").vector3Value = center;
            boundaryProp.FindPropertyRelative("size").vector3Value = size;
            boundaryProp.FindPropertyRelative("cellSize").floatValue = cellSize;

            so.FindProperty("localPlayerId").intValue = localPlayerId;

            // Update renderer material if exists, or create if missing
            FogOfWarRenderer renderer = manager.GetComponentInChildren<FogOfWarRenderer>();
            if (renderer == null)
            {
                // Create renderer if it doesn't exist
                GameObject rendererObj = new GameObject("FogRenderer");
                rendererObj.transform.SetParent(manager.transform);
                renderer = rendererObj.AddComponent<FogOfWarRenderer>();

                // Link to manager
                so.FindProperty("fogRenderer").objectReferenceValue = renderer;
            }

            // Update renderer settings
            SerializedObject rendererSO = new SerializedObject(renderer);
            rendererSO.FindProperty("fogMaterial").objectReferenceValue = fogMaterial;
            rendererSO.FindProperty("fogHeight").floatValue = fogHeight;
            rendererSO.FindProperty("chunksPerUpdate").intValue = 10;
            rendererSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(renderer);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(manager);

            Debug.Log("Updated FogOfWarManager configuration");
        }

        private void SetupMinimapUI(MinimapConfig config)
        {
            // Check if minimap already exists
            MiniMapControllerPro existing = FindFirstObjectByType<MiniMapControllerPro>();
            if (existing != null)
            {
                Debug.Log("Minimap already exists, updating configuration...");
                UpdateMinimapController(existing, config);
                return;
            }

            // Find or create canvas
            Canvas canvas = targetCanvas;
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("MinimapCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                Debug.Log("Created new Canvas for minimap");
            }

            // Create minimap panel
            GameObject minimapPanel = new GameObject("MinimapPanel");
            minimapPanel.transform.SetParent(canvas.transform);
            RectTransform minimapRect = minimapPanel.AddComponent<RectTransform>();

            // Position in bottom-right corner
            minimapRect.anchorMin = new Vector2(1, 0);
            minimapRect.anchorMax = new Vector2(1, 0);
            minimapRect.pivot = new Vector2(1, 0);
            minimapRect.anchoredPosition = new Vector2(-20, 20);
            minimapRect.sizeDelta = new Vector2(250, 250);

            // Add background
            Image bgImage = minimapPanel.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            // Create minimap image
            GameObject minimapImageObj = new GameObject("MinimapImage");
            minimapImageObj.transform.SetParent(minimapPanel.transform);
            RectTransform imageRect = minimapImageObj.AddComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;

            RawImage minimapImage = minimapImageObj.AddComponent<RawImage>();

            // Create viewport indicator
            GameObject viewportObj = new GameObject("ViewportIndicator");
            viewportObj.transform.SetParent(minimapPanel.transform);
            RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0.5f, 0.5f);
            viewportRect.anchorMax = new Vector2(0.5f, 0.5f);
            viewportRect.sizeDelta = new Vector2(50, 50);

            Image viewportImage = viewportObj.AddComponent<Image>();
            viewportImage.color = new Color(1, 1, 1, 0.3f);
            viewportImage.raycastTarget = false;

            // Create marker containers
            GameObject buildingMarkers = new GameObject("BuildingMarkers");
            buildingMarkers.transform.SetParent(minimapPanel.transform);
            RectTransform buildingRect = buildingMarkers.AddComponent<RectTransform>();
            buildingRect.anchorMin = Vector2.zero;
            buildingRect.anchorMax = Vector2.one;
            buildingRect.offsetMin = Vector2.zero;
            buildingRect.offsetMax = Vector2.zero;

            GameObject unitMarkers = new GameObject("UnitMarkers");
            unitMarkers.transform.SetParent(minimapPanel.transform);
            RectTransform unitRect = unitMarkers.AddComponent<RectTransform>();
            unitRect.anchorMin = Vector2.zero;
            unitRect.anchorMax = Vector2.one;
            unitRect.offsetMin = Vector2.zero;
            unitRect.offsetMax = Vector2.zero;

            // Create minimap camera
            GameObject miniMapCameraObj = new GameObject("MinimapCamera");
            Camera miniMapCamera = miniMapCameraObj.AddComponent<Camera>();

            // Configure minimap camera
            miniMapCamera.orthographic = true;
            miniMapCamera.orthographicSize = (config.worldMax.y - config.worldMin.y) / 2f;

            // Position camera above world center
            Vector3 worldCenter = config.WorldCenter;
            worldCenter.y = config.minimapCameraHeight;
            miniMapCamera.transform.position = worldCenter;
            miniMapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            // Set render settings
            miniMapCamera.cullingMask = config.minimapLayers;
            miniMapCamera.clearFlags = CameraClearFlags.SolidColor;
            miniMapCamera.backgroundColor = config.backgroundColor;
            miniMapCamera.depth = -10;

            // Create render texture
            RenderTexture renderTexture = new RenderTexture(
                config.renderTextureSize,
                config.renderTextureSize,
                24,
                RenderTextureFormat.ARGB32
            );
            renderTexture.name = "MinimapRenderTexture";
            renderTexture.antiAliasing = 1;
            renderTexture.Create();

            miniMapCamera.targetTexture = renderTexture;
            minimapImage.texture = renderTexture;

            // Remove audio listener from minimap camera
            if (miniMapCamera.TryGetComponent<AudioListener>(out var listener))
            {
                UnityEngine.Object.DestroyImmediate(listener);
            }

            // Add MiniMapControllerPro
            MiniMapControllerPro controller = minimapPanel.AddComponent<MiniMapControllerPro>();

            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("miniMapRect").objectReferenceValue = minimapRect;
            so.FindProperty("miniMapImage").objectReferenceValue = minimapImage;
            so.FindProperty("miniMapCamera").objectReferenceValue = miniMapCamera;
            so.FindProperty("viewportIndicator").objectReferenceValue = viewportRect;
            so.FindProperty("buildingMarkersContainer").objectReferenceValue = buildingRect;
            so.FindProperty("unitMarkersContainer").objectReferenceValue = unitRect;

            if (cameraController != null)
            {
                so.FindProperty("cameraController").objectReferenceValue = cameraController;
            }

            so.ApplyModifiedProperties();

            // Add minimap fog renderer
            FogOfWarManager fogManager = FindFirstObjectByType<FogOfWarManager>();
            if (fogManager != null)
            {
                GameObject fogRendererObj = new GameObject("MinimapFogRenderer");
                fogRendererObj.transform.SetParent(fogManager.transform);

                FogOfWarMinimapRenderer fogRenderer = fogRendererObj.AddComponent<FogOfWarMinimapRenderer>();
                SerializedObject fogRendererSO = new SerializedObject(fogRenderer);
                fogRendererSO.FindProperty("fogOverlayImage").objectReferenceValue = minimapImage;
                fogRendererSO.ApplyModifiedProperties();
                EditorUtility.SetDirty(fogRenderer);

                // Link to manager
                SerializedObject managerSO = new SerializedObject(fogManager);
                managerSO.FindProperty("minimapRenderer").objectReferenceValue = fogRenderer;
                managerSO.ApplyModifiedProperties();
                EditorUtility.SetDirty(fogManager);
            }

            Debug.Log("Created Minimap UI in scene");
        }

        private void UpdateMinimapController(MiniMapControllerPro controller, MinimapConfig config)
        {
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("config").objectReferenceValue = config;

            if (cameraController != null)
            {
                so.FindProperty("cameraController").objectReferenceValue = cameraController;
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);

            // Ensure minimap fog renderer exists
            FogOfWarManager fogManager = FindFirstObjectByType<FogOfWarManager>();
            if (fogManager != null)
            {
                FogOfWarMinimapRenderer existingFogRenderer = fogManager.GetComponentInChildren<FogOfWarMinimapRenderer>();
                if (existingFogRenderer == null)
                {
                    // Create fog renderer if missing
                    RawImage minimapImage = controller.GetComponentInChildren<RawImage>();
                    if (minimapImage != null)
                    {
                        GameObject fogRendererObj = new GameObject("MinimapFogRenderer");
                        fogRendererObj.transform.SetParent(fogManager.transform);

                        FogOfWarMinimapRenderer fogRenderer = fogRendererObj.AddComponent<FogOfWarMinimapRenderer>();
                        SerializedObject fogRendererSO = new SerializedObject(fogRenderer);
                        fogRendererSO.FindProperty("fogOverlayImage").objectReferenceValue = minimapImage;
                        fogRendererSO.ApplyModifiedProperties();
                        EditorUtility.SetDirty(fogRenderer);

                        // Link to manager
                        SerializedObject managerSO = new SerializedObject(fogManager);
                        managerSO.FindProperty("minimapRenderer").objectReferenceValue = fogRenderer;
                        managerSO.ApplyModifiedProperties();
                        EditorUtility.SetDirty(fogManager);

                        Debug.Log("Created missing minimap fog renderer");
                    }
                }
            }
        }

        private void CleanupUnusedScripts()
        {
            int deletedCount = 0;

            // List of paths to delete
            string[] pathsToDelete = new string[]
            {
                // SimpleToon camera scripts
                "Assets/SimpleToon/Model/Kawaii Slimes/Cameras",

                // Legacy AOSFogWar system
                "Assets/AOSFogWar",

                // Legacy shaders (keep only URP version)
                "Assets/FogPlane.shader",
                "Assets/FogPlaneCS.shader",

                // Unused scripts
                "Assets/Scripts/UI/Minimap/Shadowcaster.cs",
                "Assets/FogPerCamera.cs"
            };

            foreach (string path in pathsToDelete)
            {
                if (AssetDatabase.IsValidFolder(path))
                {
                    if (AssetDatabase.DeleteAsset(path))
                    {
                        Debug.Log($"Deleted folder: {path}");
                        deletedCount++;
                    }
                }
                else if (File.Exists(Path.Combine(Application.dataPath, "..", path)))
                {
                    if (AssetDatabase.DeleteAsset(path))
                    {
                        Debug.Log($"Deleted file: {path}");
                        deletedCount++;
                    }
                }
            }

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Cleanup Complete",
                $"Deleted {deletedCount} unused files/folders.\n\n" +
                "Your project is now clean and organized with only the modern Fog of War and Minimap systems.", "OK");
        }
    }
}
