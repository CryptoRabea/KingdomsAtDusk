using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RTS.Editor
{
    /// <summary>
    /// Advanced tool to automatically detect and extract all game systems as individual packages.
    /// Analyzes project structure and dependencies to create modular, reusable packages.
    /// </summary>
    public class SystemExtractorTool : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<GameSystem> detectedSystems = new List<GameSystem>();
        private bool showDependencies = false;
        private string exportFolder = "";

        [System.Serializable]
        private class GameSystem
        {
            public string name;
            public string description;
            public List<string> paths;
            public List<string> dependencies;
            public bool selected;
            public int estimatedFileCount;

            public GameSystem(string name, string description)
            {
                this.name = name;
                this.description = description;
                this.paths = new List<string>();
                this.dependencies = new List<string>();
                this.selected = false;
                this.estimatedFileCount = 0;
            }
        }

        [MenuItem("Tools/RTS/Export/System Extractor (Auto-Detect)")]
        private static void ShowWindow()
        {
            SystemExtractorTool window = GetWindow<SystemExtractorTool>("System Extractor");
            window.minSize = new Vector2(600, 700);
            window.Show();
        }

        private void OnEnable()
        {
            DetectAllSystems();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("System Extractor - Auto Detection", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This tool automatically detects all game systems in your project.\n" +
                "Select which systems to extract as standalone packages.",
                MessageType.Info);

            EditorGUILayout.Space();

            // Scan button
            if (GUILayout.Button("Re-Scan Project for Systems", GUILayout.Height(30)))
            {
                DetectAllSystems();
            }

            EditorGUILayout.Space();

            // Export folder selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Export Folder:", GUILayout.Width(100));
            exportFolder = EditorGUILayout.TextField(exportFolder);
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Export Folder", "", "");
                if (!string.IsNullOrEmpty(path))
                {
                    exportFolder = path;
                }
            }
            EditorGUILayout.EndHorizontal();

            if (string.IsNullOrEmpty(exportFolder))
            {
                exportFolder = Path.Combine(Directory.GetCurrentDirectory(), "ExportedPackages");
            }

            EditorGUILayout.Space();

            // Display detected systems
            GUILayout.Label($"Detected Systems ({detectedSystems.Count})", EditorStyles.boldLabel);

            showDependencies = EditorGUILayout.Toggle("Show Dependencies", showDependencies);

            EditorGUILayout.Space();

            // Select/Deselect all
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All"))
            {
                foreach (var system in detectedSystems)
                {
                    system.selected = true;
                }
            }
            if (GUILayout.Button("Deselect All"))
            {
                foreach (var system in detectedSystems)
                {
                    system.selected = false;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Display systems
            foreach (var system in detectedSystems)
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();
                system.selected = EditorGUILayout.Toggle(system.selected, GUILayout.Width(20));
                GUILayout.Label($"{system.name} ({system.estimatedFileCount} files)", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField(system.description, EditorStyles.wordWrappedLabel);

                EditorGUILayout.Space();

                GUILayout.Label("Paths:", EditorStyles.miniBoldLabel);
                foreach (string path in system.paths)
                {
                    EditorGUILayout.LabelField("  - " + path, EditorStyles.miniLabel);
                }

                if (showDependencies && system.dependencies.Count > 0)
                {
                    EditorGUILayout.Space();
                    GUILayout.Label("Dependencies:", EditorStyles.miniBoldLabel);
                    foreach (string dep in system.dependencies)
                    {
                        EditorGUILayout.LabelField("  -> " + dep, EditorStyles.miniLabel);
                    }
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            EditorGUILayout.Space(20);

            // Export buttons
            int selectedCount = detectedSystems.Count(s => s.selected);
            GUI.enabled = selectedCount > 0;

            if (GUILayout.Button($"Export Selected Systems ({selectedCount})", GUILayout.Height(40)))
            {
                ExportSelectedSystems();
            }

            if (GUILayout.Button($"Export All Systems ({detectedSystems.Count})", GUILayout.Height(30)))
            {
                foreach (var system in detectedSystems)
                {
                    system.selected = true;
                }
                ExportSelectedSystems();
            }

            GUI.enabled = true;

            EditorGUILayout.EndScrollView();
        }

        private void DetectAllSystems()
        {
            detectedSystems.Clear();

            Debug.Log("[SystemExtractor] Scanning project for game systems...");

            // Core Systems
            AddSystemIfExists("Core Services",
                "Service locator, event bus, and core architecture",
                new[] { "Assets/Scripts/Core" },
                new string[] { });

            // Build Initialization
            AddSystemIfExists("Build Initialization",
                "Build initialization, shader preloader, and startup diagnostics",
                new[] { "Assets/Scripts/Core/BuildInitializer.cs", "Assets/Scripts/Core/ShaderPreloader.cs", "Assets/Scripts/Core/BuildDiagnostics.cs", "Assets/Scripts/Core/StartupDiagnostics.cs" },
                new[] { "Core Services" });

            // Menu & Loading
            AddSystemIfExists("Menu & Loading System",
                "Main menu, loading screens, and scene transitions",
                new[] { "Assets/Scripts/UI/LoadingScreen", "Assets/Scripts/UI/MainMenu", "Assets/Scenes/MainMenu.unity" },
                new[] { "Core Services" });

            // Fog of War
            AddSystemIfExists("Fog of War System",
                "Fog of war with revealers and visibility management",
                new[] { "Assets/Scripts/FogOfWar", "Assets/AOSFogWar", "Assets/Settings/FogPerCamera.cs" },
                new[] { "Core Services" });

            // Building System
            AddSystemIfExists("Building System",
                "Building placement, selection, and management",
                new[] { "Assets/Scripts/RTSBuildingsSystems", "Assets/Scripts/Managers/BuildingManager.cs" },
                new[] { "Core Services", "Selection System" });

            // Unit System
            AddSystemIfExists("Unit System",
                "Unit management, movement, and behavior",
                new[] { "Assets/Scripts/Units" },
                new[] { "Core Services", "Selection System" });

            // Selection System
            AddSystemIfExists("Selection System",
                "Unit and building selection with control groups",
                new[] { "Assets/Scripts/Units/Selection", "Assets/Scripts/RTSBuildingsSystems/BuildingSelectionManager.cs", "Assets/Scripts/RTSBuildingsSystems/BuildingSelectable.cs", "Assets/Scripts/RTSBuildingsSystems/BuildingGroupManager.cs" },
                new[] { "Core Services" });

            // Resource System
            AddSystemIfExists("Resource Management",
                "Resource collection, storage, and display",
                new[] { "Assets/Scripts/Managers/ResourceManager.cs" },
                new[] { "Core Services" });

            // Happiness System
            AddSystemIfExists("Happiness System",
                "Population happiness and morale management",
                new[] { "Assets/Scripts/Managers/HappinessManager.cs" },
                new[] { "Core Services", "Resource Management" });

            // Population System
            AddSystemIfExists("Population System",
                "Population and workforce management",
                new[] { "Assets/Scripts/Managers/PopulationManager.cs", "Assets/Scripts/Managers/PeasantWorkforceManager.cs" },
                new[] { "Core Services", "Resource Management" });

            // Reputation System
            AddSystemIfExists("Reputation System",
                "Reputation and faction relationship system",
                new[] { "Assets/Scripts/Managers/ReputationManager.cs" },
                new[] { "Core Services" });

            // Wave System
            AddSystemIfExists("Wave System",
                "Enemy wave spawning and management",
                new[] { "Assets/Scripts/Managers/WaveManager.cs" },
                new[] { "Core Services", "Unit System" });

            // Camera System
            AddSystemIfExists("RTS Camera System",
                "RTS camera with pan, zoom, and rotation",
                new[] { "Assets/Scripts/Camera" },
                new string[] { });

            // UI System
            AddSystemIfExists("UI System",
                "Complete UI including HUD, minimap, and dialogs",
                new[] { "Assets/Scripts/UI" },
                new[] { "Core Services" });

            // Minimap System
            AddSystemIfExists("Minimap System",
                "Minimap with markers and fog of war integration",
                new[] { "Assets/Scripts/UI/Minimap" },
                new[] { "Core Services", "Fog of War System" });

            // Cursor System
            AddSystemIfExists("Cursor System",
                "Custom cursor with state management",
                new[] { "Assets/Scripts/UI/CursorStateManager.cs" },
                new string[] { });

            // Input System
            AddSystemIfExists("Input System",
                "Input action configuration",
                new[] { "Assets/Scripts/InputSystem_Actions.inputactions", "Assets/Scripts/InputSystem_Actions.cs" },
                new string[] { });

            // Editor Tools
            AddSystemIfExists("Editor Tools",
                "All editor tools and utilities",
                new[] { "Assets/Scripts/Editor" },
                new string[] { });

            Debug.Log($"[SystemExtractor] Found {detectedSystems.Count} systems");
        }

        private void AddSystemIfExists(string name, string description, string[] paths, string[] dependencies)
        {
            var validPaths = paths.Where(p =>
                AssetDatabase.IsValidFolder(p) || File.Exists(p)).ToList();

            if (validPaths.Count > 0)
            {
                var system = new GameSystem(name, description);
                system.paths.AddRange(validPaths);
                system.dependencies.AddRange(dependencies);

                // Estimate file count
                foreach (string path in validPaths)
                {
                    if (AssetDatabase.IsValidFolder(path))
                    {
                        system.estimatedFileCount += Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                            .Count(f => !f.EndsWith(".meta"));
                    }
                    else
                    {
                        system.estimatedFileCount++;
                    }
                }

                detectedSystems.Add(system);
            }
        }

        private void ExportSelectedSystems()
        {
            var selectedSystems = detectedSystems.Where(s => s.selected).ToList();

            if (selectedSystems.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "No systems selected!", "OK");
                return;
            }

            // Create export folder
            Directory.CreateDirectory(exportFolder);

            Debug.Log($"[SystemExtractor] Exporting {selectedSystems.Count} systems to: {exportFolder}");

            int successCount = 0;
            int failCount = 0;

            foreach (var system in selectedSystems)
            {
                try
                {
                    ExportSystem(system);
                    successCount++;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[SystemExtractor] Failed to export {system.name}: {e.Message}");
                    failCount++;
                }
            }

            // Create master README
            CreateMasterReadme(selectedSystems);

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Export Complete",
                $"System extraction complete!\n\n" +
                $"Successfully exported: {successCount}\n" +
                $"Failed: {failCount}\n\n" +
                $"Location: {exportFolder}\n\n" +
                $"A master README file has been created.",
                "OK");

            // Open folder
            EditorUtility.RevealInFinder(exportFolder);
        }

        private void ExportSystem(GameSystem system)
        {
            Debug.Log($"[SystemExtractor] Exporting: {system.name}");

            // Collect all asset paths
            List<string> allAssetPaths = new List<string>();
            foreach (string path in system.paths)
            {
                if (AssetDatabase.IsValidFolder(path))
                {
                    string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                        .Where(f => !f.EndsWith(".meta"))
                        .Select(f => f.Replace("\\", "/"))
                        .ToArray();
                    allAssetPaths.AddRange(files);
                }
                else if (File.Exists(path))
                {
                    allAssetPaths.Add(path);
                }
            }

            allAssetPaths = allAssetPaths.Distinct().ToList();

            // Export package
            string fileName = system.name.Replace(" ", "") + "_v1.0.0.unitypackage";
            string packagePath = Path.Combine(exportFolder, fileName);

            AssetDatabase.ExportPackage(
                allAssetPaths.ToArray(),
                packagePath,
                ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);

            // Create README for this system
            CreateSystemReadme(system, packagePath);

            Debug.Log($"[SystemExtractor] Exported: {system.name} ({allAssetPaths.Count} files)");
        }

        private void CreateSystemReadme(GameSystem system, string packagePath)
        {
            string readmePath = Path.ChangeExtension(packagePath, ".txt");
            string content = $@"# {system.name}

## Description
{system.description}

## Installation
1. Open your Unity project
2. Import this .unitypackage file:
   Assets -> Import Package -> Custom Package
3. Select all files and click Import

## Dependencies
{(system.dependencies.Count > 0 ?
    string.Join("\n", system.dependencies.Select(d => $"- {d}")) :
    "- None (standalone system)")}

## Included Files
{string.Join("\n", system.paths.Select(p => $"- {p}"))}

## File Count
{system.estimatedFileCount} files

## Setup Instructions
1. Import the package
2. {(system.dependencies.Count > 0 ? "Import required dependencies first" : "No dependencies required")}
3. Follow system-specific documentation
4. Add required scenes to Build Settings if applicable

## Version
1.0.0

## Export Date
{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}

---
Auto-generated by System Extractor Tool
";

            File.WriteAllText(readmePath, content);
        }

        private void CreateMasterReadme(List<GameSystem> systems)
        {
            string readmePath = Path.Combine(exportFolder, "README.txt");
            string content = $@"# Exported Game Systems

## Overview
This folder contains {systems.Count} exported game systems.
Each system is packaged as a standalone .unitypackage file.

## Exported Systems
{string.Join("\n", systems.Select(s => $"- {s.name} - {s.description}"))}

## Import Order (Recommended)
1. Core Services (required by most systems)
2. Build Initialization
3. Input System
4. Camera System
5. Selection System
6. Resource Management
7. All other systems

## Installation
1. Create a new Unity project (or use existing)
2. Import packages in the recommended order
3. Follow individual system README files
4. Configure Build Settings as needed

## Dependencies
Check individual system README files for dependency information.

## Total Files
{systems.Sum(s => s.estimatedFileCount)} files across all systems

## Export Date
{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}

## Tools Used
- System Extractor Tool (Auto-Detection)
- Package Exporter Tool

---
For questions or issues, refer to individual system documentation.
";

            File.WriteAllText(readmePath, content);
            Debug.Log($"[SystemExtractor] Master README created: {readmePath}");
        }
    }
}
