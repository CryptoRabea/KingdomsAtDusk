using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace RTS.Editor
{
    /// <summary>
    /// Automated system extractor that creates standalone Unity packages
    /// for each game system that can be sold on Asset Store or reused in other projects
    /// </summary>
    public class StandaloneSystemExtractor : EditorWindow
    {
        private Vector2 scrollPosition;
        private Dictionary<string, bool> selectedSystems = new Dictionary<string, bool>();
        private string outputPath = "StandalonePackages";
        private bool extractAll = false;

        [MenuItem("Tools/RTS/Standalone System Extractor")]
        public static void ShowWindow()
        {
            var window = GetWindow<StandaloneSystemExtractor>("System Extractor");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
            InitializeSystemSelection();
        }

        private void InitializeSystemSelection()
        {
            foreach (var system in SystemDefinitions.GetAllSystems())
            {
                if (!selectedSystems.ContainsKey(system.Name))
                {
                    selectedSystems[system.Name] = false;
                }
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Standalone System Extractor", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This tool extracts game systems into standalone Unity packages.\n" +
                "Each package includes:\n" +
                "• All required scripts and dependencies\n" +
                "• Package.json for Unity Package Manager\n" +
                "• README with usage instructions\n" +
                "• Sample scenes and prefabs\n" +
                "• Documentation",
                MessageType.Info);

            EditorGUILayout.Space();

            outputPath = EditorGUILayout.TextField("Output Path:", outputPath);
            EditorGUILayout.Space();

            extractAll = EditorGUILayout.Toggle("Extract All Systems", extractAll);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Select Systems to Extract:", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var system in SystemDefinitions.GetAllSystems())
            {
                EditorGUILayout.BeginHorizontal();
                selectedSystems[system.Name] = EditorGUILayout.Toggle(
                    extractAll || selectedSystems[system.Name],
                    GUILayout.Width(20));
                EditorGUILayout.LabelField(system.Name, GUILayout.Width(250));
                EditorGUILayout.LabelField($"({system.Category})", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();

            if (GUILayout.Button("Extract Selected Systems", GUILayout.Height(40)))
            {
                ExtractSelectedSystems();
            }
        }

        private void ExtractSelectedSystems()
        {
            var systemsToExtract = SystemDefinitions.GetAllSystems()
                .Where(s => extractAll || selectedSystems.GetValueOrDefault(s.Name, false))
                .ToList();

            if (systemsToExtract.Count == 0)
            {
                EditorUtility.DisplayDialog("No Systems Selected",
                    "Please select at least one system to extract.", "OK");
                return;
            }

            int extracted = 0;
            int total = systemsToExtract.Count;

            foreach (var system in systemsToExtract)
            {
                EditorUtility.DisplayProgressBar("Extracting Systems",
                    $"Extracting {system.Name}...", (float)extracted / total);

                try
                {
                    ExtractSystem(system);
                    extracted++;
                    Debug.Log($"✓ Successfully extracted: {system.Name}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"✗ Failed to extract {system.Name}: {ex.Message}");
                }
            }

            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Extraction Complete",
                $"Successfully extracted {extracted} out of {total} systems.\n" +
                $"Output location: {Path.GetFullPath(outputPath)}", "OK");

            AssetDatabase.Refresh();
        }

        private void ExtractSystem(SystemDefinition system)
        {
            string packagePath = Path.Combine(outputPath, system.PackageName);
            Directory.CreateDirectory(packagePath);

            // Create package structure
            CreatePackageStructure(packagePath);

            // Copy scripts and dependencies
            CopySystemFiles(system, packagePath);

            // Generate package.json
            GeneratePackageJson(system, packagePath);

            // Generate README
            GenerateReadme(system, packagePath);

            // Generate documentation
            GenerateDocumentation(system, packagePath);

            // Generate samples
            GenerateSamples(system, packagePath);

            Debug.Log($"Package created at: {packagePath}");
        }

        private void CreatePackageStructure(string packagePath)
        {
            Directory.CreateDirectory(Path.Combine(packagePath, "Runtime"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Runtime", "Scripts"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Editor"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Documentation~"));
            Directory.CreateDirectory(Path.Combine(packagePath, "Samples~"));
        }

        private void CopySystemFiles(SystemDefinition system, string packagePath)
        {
            string runtimePath = Path.Combine(packagePath, "Runtime", "Scripts");

            // Copy main system files
            foreach (var file in system.Files)
            {
                CopyFile(file, runtimePath, system);
            }

            // Copy dependency files
            foreach (var dependency in system.Dependencies)
            {
                var depSystem = SystemDefinitions.GetSystemByName(dependency);
                if (depSystem != null)
                {
                    foreach (var file in depSystem.Files)
                    {
                        CopyFile(file, runtimePath, depSystem);
                    }
                }
            }

            // Create .meta files for Unity
            CreateMetaFiles(runtimePath);
        }

        private void CopyFile(string sourceFile, string destPath, SystemDefinition system)
        {
            string fullSourcePath = Path.Combine(Application.dataPath, "..", sourceFile);

            if (!File.Exists(fullSourcePath))
            {
                Debug.LogWarning($"Source file not found: {fullSourcePath}");
                return;
            }

            // Preserve directory structure
            string relativePath = sourceFile.Replace("Assets/Scripts/", "")
                                           .Replace("Assets/RTSAnimation/", "");
            string fileName = Path.GetFileName(relativePath);
            string subDir = Path.GetDirectoryName(relativePath);

            string targetDir = string.IsNullOrEmpty(subDir) ? destPath : Path.Combine(destPath, subDir);
            Directory.CreateDirectory(targetDir);

            string destFile = Path.Combine(targetDir, fileName);
            File.Copy(fullSourcePath, destFile, true);
        }

        private void CreateMetaFiles(string directory)
        {
            foreach (var file in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                string metaFile = file + ".meta";
                if (!File.Exists(metaFile))
                {
                    string guid = GUID.Generate().ToString();
                    File.WriteAllText(metaFile, GenerateMetaFileContent(guid));
                }
            }
        }

        private string GenerateMetaFileContent(string guid)
        {
            return $@"fileFormatVersion: 2
guid: {guid}
MonoImporter:
  externalObjects: {{}}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {{instanceID: 0}}
  userData:
  assetBundleName:
  assetBundleVariant:
";
        }

        private void GeneratePackageJson(SystemDefinition system, string packagePath)
        {
            var packageJson = new
            {
                name = $"com.rts.{system.PackageName.ToLower().Replace(" ", "-")}",
                version = "1.0.0",
                displayName = system.Name,
                description = system.Description,
                unity = "2021.3",
                keywords = system.Keywords,
                author = new
                {
                    name = "Your Name",
                    email = "your.email@example.com"
                },
                dependencies = system.UnityDependencies.ToDictionary(
                    d => d.Key,
                    d => d.Value
                )
            };

            string json = JsonUtility.ToJson(packageJson, true);
            // Manual JSON formatting since Unity's JsonUtility doesn't support nested objects well
            json = FormatPackageJson(system);

            File.WriteAllText(Path.Combine(packagePath, "package.json"), json);
        }

        private string FormatPackageJson(SystemDefinition system)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"name\": \"com.rts.{system.PackageName.ToLower().Replace(" ", "-")}\",");
            sb.AppendLine($"  \"version\": \"1.0.0\",");
            sb.AppendLine($"  \"displayName\": \"{system.Name}\",");
            sb.AppendLine($"  \"description\": \"{system.Description}\",");
            sb.AppendLine($"  \"unity\": \"2021.3\",");
            sb.AppendLine($"  \"keywords\": [{string.Join(", ", system.Keywords.Select(k => $"\"{k}\""))}],");
            sb.AppendLine($"  \"author\": {{");
            sb.AppendLine($"    \"name\": \"RTS Systems\"");
            sb.AppendLine($"  }}");

            if (system.UnityDependencies.Count > 0)
            {
                sb.AppendLine($"  ,\"dependencies\": {{");
                var deps = system.UnityDependencies.ToList();
                for (int i = 0; i < deps.Count; i++)
                {
                    bool isLast = i == deps.Count - 1;
                    sb.AppendLine($"    \"{deps[i].Key}\": \"{deps[i].Value}\"{(isLast ? "" : ",")}");
                }
                sb.AppendLine($"  }}");
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        private void GenerateReadme(SystemDefinition system, string packagePath)
        {
            var readme = new StringBuilder();
            readme.AppendLine($"# {system.Name}");
            readme.AppendLine();
            readme.AppendLine($"## Description");
            readme.AppendLine(system.Description);
            readme.AppendLine();
            readme.AppendLine($"## Category");
            readme.AppendLine(system.Category);
            readme.AppendLine();
            readme.AppendLine($"## Features");
            foreach (var feature in system.Features)
            {
                readme.AppendLine($"- {feature}");
            }
            readme.AppendLine();
            readme.AppendLine($"## Installation");
            readme.AppendLine();
            readme.AppendLine($"### Via Unity Package Manager");
            readme.AppendLine($"1. Open Unity Package Manager (Window > Package Manager)");
            readme.AppendLine($"2. Click the '+' button and select 'Add package from disk...'");
            readme.AppendLine($"3. Navigate to the package.json file in this directory");
            readme.AppendLine();
            readme.AppendLine($"### Via Direct Import");
            readme.AppendLine($"1. Copy the entire package folder to your project's Assets directory");
            readme.AppendLine($"2. Unity will automatically import the scripts");
            readme.AppendLine();
            readme.AppendLine($"## Requirements");
            readme.AppendLine($"- Unity {system.MinUnityVersion}+");

            if (system.Dependencies.Count > 0)
            {
                readme.AppendLine();
                readme.AppendLine($"## Dependencies");
                readme.AppendLine($"This system requires the following other systems:");
                foreach (var dep in system.Dependencies)
                {
                    readme.AppendLine($"- {dep}");
                }
            }

            if (system.UnityDependencies.Count > 0)
            {
                readme.AppendLine();
                readme.AppendLine($"## Unity Package Dependencies");
                foreach (var dep in system.UnityDependencies)
                {
                    readme.AppendLine($"- {dep.Key}: {dep.Value}");
                }
            }

            readme.AppendLine();
            readme.AppendLine($"## Quick Start");
            readme.AppendLine(system.QuickStart);
            readme.AppendLine();
            readme.AppendLine($"## Documentation");
            readme.AppendLine($"See the Documentation~ folder for detailed usage instructions.");
            readme.AppendLine();
            readme.AppendLine($"## Support");
            readme.AppendLine($"For issues and questions, please contact support.");
            readme.AppendLine();
            readme.AppendLine($"## License");
            readme.AppendLine($"See LICENSE file for details.");

            File.WriteAllText(Path.Combine(packagePath, "README.md"), readme.ToString());
        }

        private void GenerateDocumentation(SystemDefinition system, string packagePath)
        {
            string docPath = Path.Combine(packagePath, "Documentation~");

            var doc = new StringBuilder();
            doc.AppendLine($"# {system.Name} - Technical Documentation");
            doc.AppendLine();
            doc.AppendLine($"## Overview");
            doc.AppendLine(system.Description);
            doc.AppendLine();
            doc.AppendLine($"## Architecture");
            doc.AppendLine(system.TechnicalDetails);
            doc.AppendLine();
            doc.AppendLine($"## API Reference");
            doc.AppendLine();
            doc.AppendLine($"### Main Classes");
            foreach (var file in system.Files)
            {
                string className = Path.GetFileNameWithoutExtension(file);
                doc.AppendLine($"#### {className}");
                doc.AppendLine($"Location: `{file}`");
                doc.AppendLine();
            }
            doc.AppendLine();
            doc.AppendLine($"## Usage Examples");
            doc.AppendLine(system.UsageExample);
            doc.AppendLine();
            doc.AppendLine($"## Configuration");
            doc.AppendLine(system.Configuration);
            doc.AppendLine();
            doc.AppendLine($"## Best Practices");
            foreach (var practice in system.BestPractices)
            {
                doc.AppendLine($"- {practice}");
            }

            File.WriteAllText(Path.Combine(docPath, "Documentation.md"), doc.ToString());
        }

        private void GenerateSamples(SystemDefinition system, string packagePath)
        {
            string samplesPath = Path.Combine(packagePath, "Samples~");

            // Create sample scene info
            var sampleReadme = new StringBuilder();
            sampleReadme.AppendLine($"# {system.Name} - Samples");
            sampleReadme.AppendLine();
            sampleReadme.AppendLine($"## Basic Sample");
            sampleReadme.AppendLine($"A minimal example showing how to set up and use {system.Name}.");
            sampleReadme.AppendLine();
            sampleReadme.AppendLine($"## Setup Instructions");
            sampleReadme.AppendLine($"1. Import the sample into your project");
            sampleReadme.AppendLine($"2. Open the sample scene");
            sampleReadme.AppendLine($"3. Press Play to see the system in action");

            File.WriteAllText(Path.Combine(samplesPath, "README.md"), sampleReadme.ToString());
        }
    }

    /// <summary>
    /// System definition data structure
    /// </summary>
    [Serializable]
    public class SystemDefinition
    {
        public string Name;
        public string PackageName;
        public string Category;
        public string Description;
        public List<string> Features = new List<string>();
        public List<string> Files = new List<string>();
        public List<string> Dependencies = new List<string>();
        public Dictionary<string, string> UnityDependencies = new Dictionary<string, string>();
        public string[] Keywords;
        public string MinUnityVersion = "2021.3";
        public string QuickStart;
        public string TechnicalDetails;
        public string UsageExample;
        public string Configuration;
        public List<string> BestPractices = new List<string>();
    }
}
