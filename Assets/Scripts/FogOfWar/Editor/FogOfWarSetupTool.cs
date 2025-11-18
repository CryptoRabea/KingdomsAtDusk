using UnityEngine;
using UnityEditor;
using RTS.UI;
using RTS.Units;
using RTS.Buildings;
namespace KingdomsAtDusk.FogOfWar.Editor
{
    /// <summary>
    /// Editor tool for setting up the Fog of War system in the scene
    /// </summary>
    public class FogOfWarSetupTool : EditorWindow
    {
        private GameObject fogOfWarPrefab;
        private bool autoAddToUnits = true;
        private bool autoAddToBuildings = true;
        private bool autoAddVisibilityControl = true;

        [MenuItem("Kingdoms at Dusk/Fog of War/Setup Tool")]
        public static void ShowWindow()
        {
            GetWindow<FogOfWarSetupTool>("Fog of War Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Fog of War Setup Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This tool will help you set up the Fog of War system in your scene.",
                MessageType.Info
            );

            EditorGUILayout.Space();

            // Step 1: Create Fog of War Manager
            GUILayout.Label("Step 1: Create Fog of War Manager", EditorStyles.boldLabel);

            if (GUILayout.Button("Create Fog of War Manager in Scene"))
            {
                CreateFogOfWarManager();
            }

            EditorGUILayout.Space();

            // Step 2: Auto-add components
            GUILayout.Label("Step 2: Add Components to Entities", EditorStyles.boldLabel);

            autoAddToUnits = EditorGUILayout.Toggle("Add to Units", autoAddToUnits);
            autoAddToBuildings = EditorGUILayout.Toggle("Add to Buildings", autoAddToBuildings);
            autoAddVisibilityControl = EditorGUILayout.Toggle("Add Visibility Control", autoAddVisibilityControl);

            if (GUILayout.Button("Add Vision Providers to Existing Entities"))
            {
                AddVisionProvidersToScene();
            }

            if (autoAddVisibilityControl && GUILayout.Button("Add Visibility Control to Enemies"))
            {
                AddVisibilityControlToEnemies();
            }

            EditorGUILayout.Space();

            // Step 3: Setup Minimap
            GUILayout.Label("Step 3: Setup Minimap Fog", EditorStyles.boldLabel);

            if (GUILayout.Button("Setup Minimap Fog of War"))
            {
                SetupMinimapFog();
            }

            EditorGUILayout.Space();

            // Utilities
            GUILayout.Label("Utilities", EditorStyles.boldLabel);

            if (GUILayout.Button("Find Fog of War Manager"))
            {
                var manager = FindFirstObjectByType<FogOfWarManager>();
                if (manager != null)
                {
                    Selection.activeGameObject = manager.gameObject;
                    EditorGUIUtility.PingObject(manager.gameObject);
                }
                else
                {
                    EditorUtility.DisplayDialog("Not Found", "No Fog of War Manager found in scene.", "OK");
                }
            }
        }

        private void CreateFogOfWarManager()
        {
            // Check if manager already exists
            var existingManager = FindFirstObjectByType<FogOfWarManager>();
            if (existingManager != null)
            {
                bool replace = EditorUtility.DisplayDialog(
                    "Manager Already Exists",
                    "A Fog of War Manager already exists in the scene. Replace it?",
                    "Replace", "Cancel"
                );

                if (!replace) return;

                DestroyImmediate(existingManager.gameObject);
            }

            // Create new manager object
            GameObject managerObj = new GameObject("FogOfWarManager");
            var manager = managerObj.AddComponent<FogOfWarManager>();

            // Create renderer child
            GameObject rendererObj = new GameObject("FogRenderer");
            rendererObj.transform.SetParent(managerObj.transform);
            var renderer = rendererObj.AddComponent<FogOfWarRenderer>();
            rendererObj.AddComponent<MeshFilter>();
            rendererObj.AddComponent<MeshRenderer>();

            // Create minimap renderer child
            GameObject minimapRendererObj = new GameObject("MinimapFogRenderer");
            minimapRendererObj.transform.SetParent(managerObj.transform);
            var minimapRenderer = minimapRendererObj.AddComponent<FogOfWarMinimapRenderer>();

            // Try to find the shader and create material
            Shader fogShader = Shader.Find("KingdomsAtDusk/FogOfWar");
            if (fogShader != null)
            {
                Material fogMaterial = new Material(fogShader);
                fogMaterial.name = "FogOfWarMaterial";

                // Save material as asset
                string path = "Assets/Materials";
                if (!AssetDatabase.IsValidFolder(path))
                {
                    AssetDatabase.CreateFolder("Assets", "Materials");
                }

                AssetDatabase.CreateAsset(fogMaterial, $"{path}/FogOfWarMaterial.mat");
                AssetDatabase.SaveAssets();

                // Assign material to renderer
                var meshRenderer = rendererObj.GetComponent<MeshRenderer>();
                meshRenderer.material = fogMaterial;

                // Use reflection to set the fog material field
                var rendererType = renderer.GetType();
                var fogMaterialField = rendererType.GetField("fogMaterial",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fogMaterialField != null)
                {
                    fogMaterialField.SetValue(renderer, fogMaterial);
                }
            }
            else
            {
                Debug.LogWarning("Fog of War shader not found. Please ensure 'KingdomsAtDusk/FogOfWar' shader exists.");
            }

            // Link components
            var managerType = manager.GetType();
            var fogRendererField = managerType.GetField("fogRenderer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var minimapRendererField = managerType.GetField("minimapRenderer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (fogRendererField != null)
            {
                fogRendererField.SetValue(manager, renderer);
            }

            if (minimapRendererField != null)
            {
                minimapRendererField.SetValue(manager, minimapRenderer);
            }

            EditorUtility.SetDirty(manager);

            Selection.activeGameObject = managerObj;
            EditorGUIUtility.PingObject(managerObj);

            Debug.Log("[FogOfWarSetupTool] Created Fog of War Manager successfully!");
        }

        private void AddVisionProvidersToScene()
        {
            int unitsCount = 0;
            int buildingsCount = 0;

            // Add to units
            if (autoAddToUnits)
            {
                var units = FindObjectsByType<RTS.Units.AI.UnitAIController>(FindObjectsSortMode.None);
                foreach (var unit in units)
                {
                    if (unit.GetComponent<VisionProvider>() == null)
                    {
                        var visionProvider = unit.gameObject.AddComponent<VisionProvider>();

                        // Try to detect ownership from MinimapEntity
                        var minimapEntity = unit.GetComponent<RTS.UI.Minimap.MinimapEntity>();
                        if (minimapEntity != null)
                        {
                            // Set owner based on ownership (0 for friendly, 1 for enemy, etc.)
                            int ownerId = minimapEntity.GetOwnership() == RTS.UI.Minimap.MinimapEntityOwnership.Friendly ? 0 : 1;
                            visionProvider.SetOwnerId(ownerId);
                        }

                        EditorUtility.SetDirty(unit.gameObject);
                        unitsCount++;
                    }
                }
            }

            // Add to buildings
            if (autoAddToBuildings)
            {
                var buildings = FindObjectsByType<RTS.Buildings.Building>(FindObjectsSortMode.None);
                foreach (var building in buildings)
                {
                    if (building.GetComponent<VisionProvider>() == null)
                    {
                        var visionProvider = building.gameObject.AddComponent<VisionProvider>();
                        visionProvider.SetOwnerId(0); // Buildings are typically player-owned
                        visionProvider.SetVisionRadius(20f); // Buildings have larger vision

                        EditorUtility.SetDirty(building.gameObject);
                        buildingsCount++;
                    }
                }
            }

            Debug.Log($"[FogOfWarSetupTool] Added VisionProvider to {unitsCount} units and {buildingsCount} buildings");
            EditorUtility.DisplayDialog(
                "Success",
                $"Added VisionProvider to:\n- {unitsCount} units\n- {buildingsCount} buildings",
                "OK"
            );
        }

        private void AddVisibilityControlToEnemies()
        {
            int count = 0;

            // Add to units
            var units = FindObjectsByType<RTS.Units.AI.UnitAIController>(FindObjectsSortMode.None);
            foreach (var unit in units)
            {
                // Check if enemy
                var minimapEntity = unit.GetComponent<RTS.UI.Minimap.MinimapEntity>();
                bool isEnemy = minimapEntity != null &&
                              minimapEntity.GetOwnership() != RTS.UI.Minimap.MinimapEntityOwnership.Friendly;

                if (isEnemy && unit.GetComponent<FogOfWarEntityVisibility>() == null)
                {
                    var visibility = unit.gameObject.AddComponent<FogOfWarEntityVisibility>();
                    visibility.SetPlayerOwned(false);
                    EditorUtility.SetDirty(unit.gameObject);
                    count++;
                }
                else if (!isEnemy && unit.GetComponent<FogOfWarEntityVisibility>() == null)
                {
                    var visibility = unit.gameObject.AddComponent<FogOfWarEntityVisibility>();
                    visibility.SetPlayerOwned(true);
                    EditorUtility.SetDirty(unit.gameObject);
                }
            }

            Debug.Log($"[FogOfWarSetupTool] Added FogOfWarEntityVisibility to {count} enemy units");
            EditorUtility.DisplayDialog("Success", $"Added visibility control to {count} enemy units", "OK");
        }

        private void SetupMinimapFog()
        {
            // Find minimap in scene
            var minimapController = FindFirstObjectByType<RTS.UI.MiniMapControllerPro>();
            if (minimapController == null)
            {
                EditorUtility.DisplayDialog(
                    "Minimap Not Found",
                    "Could not find MiniMapController in scene. Please ensure your minimap is set up first.",
                    "OK"
                );
                return;
            }

            // Create fog overlay UI element
            var minimapTransform = minimapController.transform;
            var fogOverlay = new GameObject("FogOverlay");
            fogOverlay.transform.SetParent(minimapTransform, false);

            var rectTransform = fogOverlay.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;

            var rawImage = fogOverlay.AddComponent<UnityEngine.UI.RawImage>();
            rawImage.color = Color.white;

            // Find fog of war manager
            var manager = FindFirstObjectByType<FogOfWarManager>();
            if (manager != null)
            {
                var minimapRenderer = manager.GetComponentInChildren<FogOfWarMinimapRenderer>();
                if (minimapRenderer != null)
                {
                    // Use reflection to set the fog overlay
                    var rendererType = minimapRenderer.GetType();
                    var fogOverlayField = rendererType.GetField("fogOverlay",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (fogOverlayField != null)
                    {
                        fogOverlayField.SetValue(minimapRenderer, rawImage);
                    }

                    EditorUtility.SetDirty(minimapRenderer);
                }
            }

            Debug.Log("[FogOfWarSetupTool] Minimap fog of war setup complete!");
            EditorUtility.DisplayDialog("Success", "Minimap fog of war has been set up!", "OK");
        }
    }
}
