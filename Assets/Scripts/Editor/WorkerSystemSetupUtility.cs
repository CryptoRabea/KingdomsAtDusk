#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using RTS.Buildings;
using KingdomsAtDusk.Buildings;
using System.Collections.Generic;

namespace KingdomsAtDusk.Editor
{
    /// <summary>
    /// Editor utility for automatically setting up the worker gathering system.
    /// Provides menu commands and validation tools.
    /// </summary>
    public class WorkerSystemSetupUtility : EditorWindow
    {
        private Vector2 scrollPosition;
        private bool showDetailedLogs = true;

        [MenuItem("RTS/Worker System/Setup Utility")]
        public static void ShowWindow()
        {
            var window = GetWindow<WorkerSystemSetupUtility>("Worker System Setup");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("Worker Gathering System Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This utility helps you set up the worker gathering system automatically. " +
                "It will add required components to buildings and validate your configuration.",
                MessageType.Info
            );

            EditorGUILayout.Space();
            showDetailedLogs = EditorGUILayout.Toggle("Show Detailed Logs", showDetailedLogs);
            EditorGUILayout.Space();

            // Auto-setup buttons
            if (GUILayout.Button("Auto-Configure All Resource Buildings", GUILayout.Height(40)))
            {
                AutoConfigureAllBuildings();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Validate Worker System Setup", GUILayout.Height(30)))
            {
                ValidateSetup();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Create GameConfig Asset", GUILayout.Height(30)))
            {
                CreateGameConfigAsset();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Find Missing Components", GUILayout.Height(30)))
            {
                FindMissingComponents();
            }

            EditorGUILayout.Space();

            // Documentation
            EditorGUILayout.LabelField("Documentation", EditorStyles.boldLabel);
            if (GUILayout.Button("Open Setup Guide"))
            {
                Application.OpenURL("file://" + Application.dataPath + "/../WORKER_GATHERING_SYSTEM.md");
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Automatically configure all resource buildings with worker trainers.
        /// </summary>
        private void AutoConfigureAllBuildings()
        {
            int configuredCount = 0;
            int skippedCount = 0;
            List<string> configuredBuildings = new List<string>();

            // Find all building prefabs
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab == null) continue;

                if (prefab.TryGetComponent<Building>(out var building))
                {
                }
                if (building == null || building.Data == null) continue;

                // Check if it's a resource building
                if (building.Data.generatesResources)
                {
                    // Check if it already has a worker trainer
                    if (prefab.TryGetComponent<BuildingWorkerTrainer>(out var trainer))
                    {
                    }

                    if (trainer == null)
                    {
                        // Add the component
                        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                        trainer = instance.AddComponent<BuildingWorkerTrainer>();

                        // Create spawn point transform
                        GameObject spawnPoint = new GameObject("WorkerSpawnPoint");
                        spawnPoint.transform.SetParent(instance.transform);
                        spawnPoint.transform.localPosition = new Vector3(3f, 0f, 0f);
                        trainer.spawnPoint = spawnPoint.transform;

                        // Save changes
                        PrefabUtility.SaveAsPrefabAsset(instance, path);
                        DestroyImmediate(instance);

                        configuredCount++;
                        configuredBuildings.Add(building.Data.buildingName);

                        if (showDetailedLogs)
                        {
                        }
                    }
                    else
                    {
                        skippedCount++;
                        if (showDetailedLogs)
                        {
                        }
                    }
                }
            }

            // Summary
            EditorUtility.DisplayDialog(
                "Auto-Configuration Complete",
                $"Configured: {configuredCount} buildings\n" +
                $"Skipped: {skippedCount} buildings\n\n" +
                $"Configured Buildings:\n{string.Join("\n", configuredBuildings)}",
                "OK"
            );

        }

        /// <summary>
        /// Validate the entire worker system setup.
        /// </summary>
        private void ValidateSetup()
        {
            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();
            List<string> info = new List<string>();

            // Check for GameConfig
            var gameConfig = UnityEngine.Resources.Load("GameConfig");
            if (gameConfig == null)
            {
                errors.Add("❌ GameConfig asset not found in Resources folder!");
            }
            else
            {
                info.Add("✅ GameConfig found");
            }

            // Check building configurations
            string[] buildingGuids = AssetDatabase.FindAssets("t:BuildingDataSO");
            int buildingsWithWorkers = 0;
            int buildingsWithoutWorkerConfig = 0;

            foreach (string guid in buildingGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var buildingData = AssetDatabase.LoadAssetAtPath<BuildingDataSO>(path);

                if (buildingData != null && buildingData.canTrainWorkers)
                {
                    buildingsWithWorkers++;

                    if (buildingData.workerUnitConfig == null)
                    {
                        warnings.Add($"⚠️ {buildingData.buildingName} can train workers but has no worker config assigned");
                        buildingsWithoutWorkerConfig++;
                    }
                }
            }

            info.Add($"✅ Found {buildingsWithWorkers} buildings with worker training enabled");

            if (buildingsWithoutWorkerConfig > 0)
            {
                warnings.Add($"⚠️ {buildingsWithoutWorkerConfig} buildings missing worker configs");
            }

            // Check for worker unit configs
            string[] unitGuids = AssetDatabase.FindAssets("t:UnitConfigSO");
            int workerUnits = 0;

            foreach (string guid in unitGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var unitConfig = AssetDatabase.LoadAssetAtPath<RTS.Units.UnitConfigSO>(path);

                if (unitConfig != null && unitConfig.isWorker)
                {
                    workerUnits++;
                }
            }

            if (workerUnits == 0)
            {
                warnings.Add("⚠️ No worker unit configs found! Create worker units for each resource type.");
            }
            else
            {
                info.Add($"✅ Found {workerUnits} worker unit configurations");
            }

            // Display results
            string message = "";

            if (errors.Count > 0)
            {
                message += "ERRORS:\n" + string.Join("\n", errors) + "\n\n";
            }

            if (warnings.Count > 0)
            {
                message += "WARNINGS:\n" + string.Join("\n", warnings) + "\n\n";
            }

            if (info.Count > 0)
            {
                message += "INFO:\n" + string.Join("\n", info);
            }

            if (errors.Count == 0 && warnings.Count == 0)
            {
                message = "✅ Worker system setup is valid! No issues found.";
                EditorUtility.DisplayDialog("Validation Complete", message, "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Validation Results", message, "OK");
            }

        }

        /// <summary>
        /// Create a GameConfig asset in Resources folder.
        /// </summary>
        private void CreateGameConfigAsset()
        {
            // Check if Resources folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            // Check if GameConfig already exists
            var existing = UnityEngine.Resources.Load("GameConfig");
            if (existing != null)
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "GameConfig Exists",
                    "A GameConfig asset already exists. Do you want to select it?",
                    "Select",
                    "Cancel"
                );

                if (overwrite)
                {
                    Selection.activeObject = existing;
                    EditorGUIUtility.PingObject(existing);
                }
                return;
            }

            // Create new GameConfig
            var config = ScriptableObject.CreateInstance<KingdomsAtDusk.Core.GameConfigSO>();
            config.gatheringMode = KingdomsAtDusk.Core.ResourceGatheringMode.WorkerGathering;
            config.enablePeasantSystem = true;
            config.enableGatheringAnimations = true;
            config.enableCarryingVisuals = true;

            string path = "Assets/Resources/GameConfig.asset";
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);

            EditorUtility.DisplayDialog(
                "GameConfig Created",
                "GameConfig asset created successfully in Assets/Resources/\n\n" +
                "The asset has been selected in the Project window.",
                "OK"
            );

        }

        /// <summary>
        /// Find buildings and workers with missing components.
        /// </summary>
        private void FindMissingComponents()
        {
            List<string> issues = new List<string>();

            // Check building prefabs
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab == null) continue;

                if (prefab.TryGetComponent<Building>(out var building) && building.Data != null)
                {
                    // Check resource buildings
                    if (building.Data.generatesResources)
                    {
                        if (prefab.TryGetComponent<BuildingWorkerTrainer>(out var trainer))
                        {
                        }
                        if (trainer == null)
                        {
                            issues.Add($"🏗️ Building missing WorkerTrainer: {building.Data.buildingName} ({path})");
                        }
                    }
                }

                // Check worker prefabs
                var unitConfig = prefab.GetComponent<RTS.Units.UnitConfigSO>();
                if (prefab.TryGetComponent<KingdomsAtDusk.Units.AI.WorkerGatheringAI>(out var gatheringAI))
                {
                }
                if (prefab.TryGetComponent<KingdomsAtDusk.Units.WorkerCarryingVisual>(out var carryingVisual))
                {
                }

                if (gatheringAI != null)
                {
                    // This is a worker
                    if (carryingVisual == null)
                    {
                        issues.Add($"👷 Worker missing CarryingVisual: {prefab.name} ({path})");
                    }

                    if (prefab.TryGetComponent<RTS.Units.UnitMovement>(out var movement))
                    {
                    }
                    if (movement == null)
                    {
                        issues.Add($"👷 Worker missing UnitMovement: {prefab.name} ({path})");
                    }
                }
            }

            // Display results
            if (issues.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "No Issues Found",
                    "All buildings and workers have required components!",
                    "OK"
                );
            }
            else
            {
                string message = $"Found {issues.Count} missing components:\n\n" + string.Join("\n", issues);
                EditorUtility.DisplayDialog("Missing Components", message, "OK");

                foreach (var issue in issues)
                {
                }
            }
        }
    }
}
#endif
