using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RTS.Editor
{
    /// <summary>
    /// Tool to export game systems as standalone Unity packages.
    /// Can export individual systems or complete features as .unitypackage files.
    /// </summary>
    public class PackageExporterTool : EditorWindow
    {
        private Vector2 scrollPosition;
        private string packageName = "MyGameSystem";
        private string packageVersion = "1.0.0";
        private string packageDescription = "Exported game system";
        private List<string> selectedPaths = new List<string>();
        private string[] predefinedSystems;
        private int selectedSystemIndex = 0;

        [MenuItem("Tools/RTS/Export/Package Exporter")]
        private static void ShowWindow()
        {
            PackageExporterTool window = GetWindow<PackageExporterTool>("Package Exporter");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        private void OnEnable()
        {
            predefinedSystems = new string[]
            {
                "Custom Selection...",
                "Menu & Loading System",
                "Fog of War System",
                "Building System",
                "Unit System",
                "Selection System",
                "Resource System",
                "Camera System",
                "UI System (Complete)",
                "Core Services",
                "All Systems (Complete Game)"
            };
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("Package Exporter", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "Export game systems as standalone Unity packages.\n" +
                "Select a predefined system or choose custom files/folders to export.",
                MessageType.Info);

            EditorGUILayout.Space();

            // Package Info
            GUILayout.Label("Package Information", EditorStyles.boldLabel);
            packageName = EditorGUILayout.TextField("Package Name", packageName);
            packageVersion = EditorGUILayout.TextField("Version", packageVersion);
            packageDescription = EditorGUILayout.TextField("Description", packageDescription);

            EditorGUILayout.Space();

            // System Selection
            GUILayout.Label("Select System to Export", EditorStyles.boldLabel);
            int newIndex = EditorGUILayout.Popup("Predefined System", selectedSystemIndex, predefinedSystems);
            if (newIndex != selectedSystemIndex)
            {
                selectedSystemIndex = newIndex;
                UpdateSelectedPaths();
            }

            EditorGUILayout.Space();

            // Manual path selection
            if (selectedSystemIndex == 0) // Custom Selection
            {
                GUILayout.Label("Custom File/Folder Selection", EditorStyles.boldLabel);

                if (GUILayout.Button("Add File or Folder"))
                {
                    string path = EditorUtility.OpenFolderPanel("Select Folder to Export", "Assets", "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        string relativePath = GetRelativePath(path);
                        if (!selectedPaths.Contains(relativePath))
                        {
                            selectedPaths.Add(relativePath);
                        }
                    }
                }

                EditorGUILayout.Space();

                GUILayout.Label("Selected Paths:", EditorStyles.boldLabel);
                for (int i = selectedPaths.Count - 1; i >= 0; i--)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(selectedPaths[i]);
                    if (GUILayout.Button("Remove", GUILayout.Width(70)))
                    {
                        selectedPaths.RemoveAt(i);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                GUILayout.Label("Paths to Export:", EditorStyles.boldLabel);
                foreach (string path in selectedPaths)
                {
                    EditorGUILayout.LabelField("• " + path);
                }
            }

            EditorGUILayout.Space(20);

            // Export button
            GUI.enabled = selectedPaths.Count > 0 && !string.IsNullOrEmpty(packageName);
            if (GUILayout.Button("Export Package", GUILayout.Height(40)))
            {
                ExportPackage();
            }
            GUI.enabled = true;

            EditorGUILayout.Space();

            // Quick export buttons
            GUILayout.Label("Quick Export", EditorStyles.boldLabel);
            if (GUILayout.Button("Export Menu & Loading System"))
            {
                QuickExportMenuSystem();
            }

            if (GUILayout.Button("Export All Game Systems"))
            {
                QuickExportAllSystems();
            }

            EditorGUILayout.EndScrollView();
        }

        private void UpdateSelectedPaths()
        {
            selectedPaths.Clear();

            switch (selectedSystemIndex)
            {
                case 1: // Menu & Loading System
                    packageName = "MenuLoadingSystem";
                    packageDescription = "Complete menu and loading screen system with scene transitions";
                    selectedPaths.AddRange(new string[]
                    {
                        "Assets/Scripts/UI/LoadingScreen",
                        "Assets/Scripts/UI/MainMenu",
                        "Assets/Prefabs/UI/LoadingScreen.prefab",
                        "Assets/Scenes/MainMenu.unity",
                        "Assets/Scripts/Editor/MenuSetupTool.cs"
                    });
                    break;

                case 2: // Fog of War System
                    packageName = "FogOfWarSystem";
                    packageDescription = "Fog of war system with revealer and visibility";
                    selectedPaths.AddRange(new string[]
                    {
                        "Assets/Scripts/FogOfWar",
                        "Assets/AOSFogWar",
                        "Assets/Settings/FogPerCamera.cs"
                    });
                    break;

                case 3: // Building System
                    packageName = "BuildingSystem";
                    packageDescription = "Complete RTS building system with placement and selection";
                    selectedPaths.AddRange(new string[]
                    {
                        "Assets/Scripts/RTSBuildingsSystems",
                        "Assets/Scripts/Managers/BuildingManager.cs"
                    });
                    break;

                case 4: // Unit System
                    packageName = "UnitSystem";
                    packageDescription = "RTS unit system with selection and commands";
                    selectedPaths.AddRange(new string[]
                    {
                        "Assets/Scripts/Units"
                    });
                    break;

                case 5: // Selection System
                    packageName = "SelectionSystem";
                    packageDescription = "Unit and building selection system";
                    selectedPaths.AddRange(new string[]
                    {
                        "Assets/Scripts/Units/Selection",
                        "Assets/Scripts/RTSBuildingsSystems/BuildingSelectionManager.cs",
                        "Assets/Scripts/RTSBuildingsSystems/BuildingSelectable.cs",
                        "Assets/Scripts/RTSBuildingsSystems/BuildingGroupManager.cs"
                    });
                    break;

                case 6: // Resource System
                    packageName = "ResourceSystem";
                    packageDescription = "Resource management system";
                    selectedPaths.AddRange(new string[]
                    {
                        "Assets/Scripts/Managers/ResourceManager.cs",
                        "Assets/Scripts/UI/ResourceDisplay.prefab"
                    });
                    break;

                case 7: // Camera System
                    packageName = "RTSCameraSystem";
                    packageDescription = "RTS camera controller with pan, zoom, and rotation";
                    selectedPaths.AddRange(new string[]
                    {
                        "Assets/Scripts/Camera"
                    });
                    break;

                case 8: // UI System
                    packageName = "UISystem";
                    packageDescription = "Complete UI system including menus, HUD, and dialogs";
                    selectedPaths.AddRange(new string[]
                    {
                        "Assets/Scripts/UI"
                    });
                    break;

                case 9: // Core Services
                    packageName = "CoreServices";
                    packageDescription = "Core service architecture with service locator and event bus";
                    selectedPaths.AddRange(new string[]
                    {
                        "Assets/Scripts/Core"
                    });
                    break;

                case 10: // All Systems
                    packageName = "CompleteRTSGame";
                    packageDescription = "Complete RTS game with all systems";
                    selectedPaths.AddRange(new string[]
                    {
                        "Assets/Scripts",
                        "Assets/Prefabs",
                        "Assets/Scenes"
                    });
                    break;
            }

            // Filter out paths that don't exist
            selectedPaths = selectedPaths.Where(path =>
                AssetDatabase.IsValidFolder(path) || File.Exists(path)).ToList();
        }

        private void ExportPackage()
        {
            if (selectedPaths.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "No paths selected for export!", "OK");
                return;
            }

            string savePath = EditorUtility.SaveFilePanel(
                "Export Unity Package",
                "",
                $"{packageName}_{packageVersion}.unitypackage",
                "unitypackage");

            if (string.IsNullOrEmpty(savePath))
            {
                return;
            }

            // Expand paths to include all files
            List<string> allAssetPaths = new List<string>();
            foreach (string path in selectedPaths)
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

            // Remove duplicates
            allAssetPaths = allAssetPaths.Distinct().ToList();

            Debug.Log($"[PackageExporter] Exporting {allAssetPaths.Count} assets...");

            try
            {
                AssetDatabase.ExportPackage(
                    allAssetPaths.ToArray(),
                    savePath,
                    ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);

                Debug.Log($"[PackageExporter] Package exported successfully: {savePath}");

                // Create a README file
                CreateReadmeFile(savePath);

                EditorUtility.DisplayDialog(
                    "Export Successful",
                    $"Package exported successfully!\n\n" +
                    $"Location: {savePath}\n" +
                    $"Assets exported: {allAssetPaths.Count}\n\n" +
                    $"A README file has been created next to the package.",
                    "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PackageExporter] Export failed: {e.Message}");
                EditorUtility.DisplayDialog("Export Failed", $"Failed to export package:\n{e.Message}", "OK");
            }
        }

        private void CreateReadmeFile(string packagePath)
        {
            string readmePath = Path.ChangeExtension(packagePath, ".txt");
            string readmeContent = $@"# {packageName} v{packageVersion}

## Description
{packageDescription}

## Installation
1. Open your Unity project
2. Go to Assets → Import Package → Custom Package
3. Select this .unitypackage file
4. Click Import

## Exported Paths
{string.Join("\n", selectedPaths.Select(p => $"• {p}"))}

## Version
{packageVersion}

## Export Date
{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}

## Notes
This package was exported using the RTS Package Exporter Tool.
Ensure all dependencies are present in your target project.

## Setup Instructions
1. Import the package into your Unity project
2. Check the documentation for system-specific setup instructions
3. Add required scenes to Build Settings if applicable

---
Generated by Package Exporter Tool
";

            File.WriteAllText(readmePath, readmeContent);
            Debug.Log($"[PackageExporter] README created: {readmePath}");
        }

        private void QuickExportMenuSystem()
        {
            selectedSystemIndex = 1;
            UpdateSelectedPaths();
            ExportPackage();
        }

        private void QuickExportAllSystems()
        {
            selectedSystemIndex = 10;
            UpdateSelectedPaths();
            ExportPackage();
        }

        private string GetRelativePath(string absolutePath)
        {
            string projectPath = Application.dataPath;
            if (absolutePath.StartsWith(projectPath))
            {
                return "Assets" + absolutePath.Substring(projectPath.Length);
            }
            return absolutePath;
        }
    }
}
