#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using RTS.Units.AI;
using RTS.Buildings;

namespace CircularLensVision.Editor
{
    /// <summary>
    /// Setup wizard to quickly configure Circular Lens Vision system in your scene.
    /// Access via: Tools > Circular Lens Vision > Setup Wizard
    /// </summary>
    public class LensVisionSetupWizard : EditorWindow
    {
        private GameObject camera;
        private float lensRadius = 20f;
        private bool autoSetupUnits = true;
        private bool autoSetupBuildings = true;
        private bool autoSetupObstacles = true;
        private bool addDebugComponent = true;

        [MenuItem("Tools/Circular Lens Vision/Setup Wizard")]
        public static void ShowWindow()
        {
            var window = GetWindow<LensVisionSetupWizard>("Lens Vision Setup");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Circular Lens Vision Setup Wizard", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This wizard will help you quickly set up the Circular Lens Vision system in your scene.",
                MessageType.Info
            );

            GUILayout.Space(10);

            // Step 1: Camera Selection
            GUILayout.Label("Step 1: Select Camera", EditorStyles.boldLabel);
            camera = (GameObject)EditorGUILayout.ObjectField("Camera GameObject", camera, typeof(GameObject), true);

            if (camera == null)
            {
                EditorGUILayout.HelpBox("Select the camera that should have lens vision (usually Main Camera or RTS Camera)", MessageType.Warning);
            }

            GUILayout.Space(10);

            // Step 2: Lens Configuration
            GUILayout.Label("Step 2: Configure Lens", EditorStyles.boldLabel);
            lensRadius = EditorGUILayout.Slider("Lens Radius", lensRadius, 5f, 100f);

            GUILayout.Space(10);

            // Step 3: Auto-Setup Options
            GUILayout.Label("Step 3: Auto-Setup Options", EditorStyles.boldLabel);
            autoSetupUnits = EditorGUILayout.Toggle("Auto Setup Units", autoSetupUnits);
            autoSetupBuildings = EditorGUILayout.Toggle("Auto Setup Buildings", autoSetupBuildings);
            autoSetupObstacles = EditorGUILayout.Toggle("Auto Setup Obstacles", autoSetupObstacles);
            addDebugComponent = EditorGUILayout.Toggle("Add Debug Component", addDebugComponent);

            GUILayout.Space(10);

            // Step 4: Execute Setup
            GUILayout.Label("Step 4: Execute Setup", EditorStyles.boldLabel);

            GUI.enabled = camera != null;

            if (GUILayout.Button("Setup Lens Vision System", GUILayout.Height(40)))
            {
                ExecuteSetup();
            }

            GUI.enabled = true;

            GUILayout.Space(20);

            // Additional Tools
            GUILayout.Label("Additional Tools", EditorStyles.boldLabel);

            if (GUILayout.Button("Find and Setup All Existing Objects"))
            {
                SetupExistingObjects();
            }

            if (GUILayout.Button("Remove All Lens Vision Components"))
            {
                RemoveAllComponents();
            }

            GUILayout.Space(10);

            // Documentation
            EditorGUILayout.HelpBox(
                "After setup, check the README.md in Assets/Scripts/CircularLensVision/ for detailed documentation.",
                MessageType.Info
            );
        }

        private void ExecuteSetup()
        {
            if (camera == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a camera GameObject first.", "OK");
                return;
            }

            Undo.SetCurrentGroupName("Setup Circular Lens Vision");
            int undoGroup = Undo.GetCurrentGroup();

            // Add CircularLensVision to camera
            var lensVision = camera.GetComponent<CircularLensVision>();
            if (lensVision == null)
            {
                lensVision = Undo.AddComponent<CircularLensVision>(camera);
            }

            lensVision.SetLensRadius(lensRadius);

            // Add debug component if requested
            if (addDebugComponent)
            {
                var debugComponent = camera.GetComponent<LensVisionDebug>();
                if (debugComponent == null)
                {
                    Undo.AddComponent<LensVisionDebug>(camera);
                }
            }

            // Create or find LensVisionManager
            GameObject manager = GameObject.Find("LensVisionManager");
            if (manager == null)
            {
                manager = new GameObject("LensVisionManager");
                Undo.RegisterCreatedObjectUndo(manager, "Create LensVisionManager");
            }

            // Add integration component
            var integration = manager.GetComponent<LensVisionIntegration>();
            if (integration == null)
            {
                integration = Undo.AddComponent<LensVisionIntegration>(manager);
            }

            // Configure integration (we need to use SerializedObject for private fields)
            SerializedObject serializedIntegration = new SerializedObject(integration);
            serializedIntegration.FindProperty("autoSetupUnits").boolValue = autoSetupUnits;
            serializedIntegration.FindProperty("autoSetupBuildings").boolValue = autoSetupBuildings;
            serializedIntegration.FindProperty("autoSetupObstacles").boolValue = autoSetupObstacles;
            serializedIntegration.FindProperty("lensController").objectReferenceValue = lensVision;
            serializedIntegration.ApplyModifiedProperties();

            Undo.CollapseUndoOperations(undoGroup);

            EditorUtility.DisplayDialog(
                "Setup Complete!",
                "Circular Lens Vision system has been set up successfully.\n\n" +
                "Components added:\n" +
                $"- CircularLensVision on {camera.name}\n" +
                "- LensVisionIntegration on LensVisionManager\n" +
                (addDebugComponent ? $"- LensVisionDebug on {camera.name}\n" : "") +
                "\nPress Play to test the system!",
                "OK"
            );

            Selection.activeGameObject = camera;
        }

        private void SetupExistingObjects()
        {
            if (EditorUtility.DisplayDialog(
                "Setup Existing Objects",
                "This will add LensVisionTarget components to all units, buildings, and obstacles in the scene. Continue?",
                "Yes",
                "No"))
            {
                int setupCount = 0;

                // Setup units
                var units = FindObjectsByType<UnitAIController>(FindObjectsSortMode.None);
                foreach (var unit in units)
                {
                    if (unit.GetComponent<LensVisionTarget>() == null)
                    {
                        Undo.AddComponent<LensVisionTarget>(unit.gameObject);
                        setupCount++;
                    }
                }

                // Setup buildings
                var buildings = FindObjectsByType<Building>(FindObjectsSortMode.None);
                foreach (var building in buildings)
                {
                    if (building.GetComponent<LensVisionTarget>() == null)
                    {
                        Undo.AddComponent<LensVisionTarget>(building.gameObject);
                        setupCount++;
                    }
                }

                // Setup obstacles by tag
                string[] obstacleTags = { "Tree", "Obstacle", "Vegetation", "Rock" };
                foreach (string tag in obstacleTags)
                {
                    try
                    {
                        GameObject[] obstacles = GameObject.FindGameObjectsWithTag(tag);
                        foreach (var obstacle in obstacles)
                        {
                            if (obstacle.GetComponent<LensVisionTarget>() == null)
                            {
                                Undo.AddComponent<LensVisionTarget>(obstacle);
                                setupCount++;
                            }
                        }
                    }
                    catch
                    {
                        // Tag doesn't exist, skip
                    }
                }

                EditorUtility.DisplayDialog("Setup Complete", $"Added LensVisionTarget to {setupCount} objects.", "OK");
            }
        }

        private void RemoveAllComponents()
        {
            if (EditorUtility.DisplayDialog(
                "Remove All Components",
                "This will remove ALL Circular Lens Vision components from the scene. This cannot be undone easily. Continue?",
                "Yes",
                "No"))
            {
                int removeCount = 0;

                // Remove LensVisionTarget components
                var targets = FindObjectsByType<LensVisionTarget>(FindObjectsSortMode.None);
                foreach (var target in targets)
                {
                    Undo.DestroyObjectImmediate(target);
                    removeCount++;
                }

                // Remove CircularLensVision components
                var lensVisions = FindObjectsByType<CircularLensVision>(FindObjectsSortMode.None);
                foreach (var lensVision in lensVisions)
                {
                    Undo.DestroyObjectImmediate(lensVision);
                    removeCount++;
                }

                // Remove integration components
                var integrations = FindObjectsByType<LensVisionIntegration>(FindObjectsSortMode.None);
                foreach (var integration in integrations)
                {
                    Undo.DestroyObjectImmediate(integration);
                    removeCount++;
                }

                // Remove debug components
                var debugs = FindObjectsByType<LensVisionDebug>(FindObjectsSortMode.None);
                foreach (var debug in debugs)
                {
                    Undo.DestroyObjectImmediate(debug);
                    removeCount++;
                }

                EditorUtility.DisplayDialog("Removal Complete", $"Removed {removeCount} components.", "OK");
            }
        }

        [MenuItem("Tools/Circular Lens Vision/Create Config Asset")]
        public static void CreateConfigAsset()
        {
            var config = CreateInstance<LensVisionConfig>();

            string path = "Assets/Scripts/CircularLensVision/LensVisionConfig.asset";
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = config;

            Debug.Log($"Created LensVisionConfig at: {path}");
        }

        [MenuItem("Tools/Circular Lens Vision/Documentation")]
        public static void OpenDocumentation()
        {
            string readmePath = "Assets/Scripts/CircularLensVision/README.md";
            var readme = AssetDatabase.LoadAssetAtPath<TextAsset>(readmePath);

            if (readme != null)
            {
                Selection.activeObject = readme;
                EditorGUIUtility.PingObject(readme);
            }
            else
            {
                EditorUtility.DisplayDialog("Documentation Not Found", $"Could not find README.md at: {readmePath}", "OK");
            }
        }
    }
}
#endif
