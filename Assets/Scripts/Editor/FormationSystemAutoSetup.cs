using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using RTS.UI;
using RTS.Units;
using RTS.Units.Formation;
using TMPro;
using System.Collections.Generic;
using System.Linq;

namespace RTS.Editor
{
    /// <summary>
    /// Auto-setup tool for the Formation System.
    /// Automatically finds, references, and configures all necessary components.
    /// Removes duplicates and unnecessary references.
    /// </summary>
    public class FormationSystemAutoSetup : EditorWindow
    {
        private Vector2 scrollPosition;
        private bool showLog = true;
        private List<string> setupLog = new List<string>();

        [MenuItem("Tools/RTS/Formation System Auto Setup")]
        public static void ShowWindow()
        {
            var window = GetWindow<FormationSystemAutoSetup>("Formation Auto Setup");
            window.minSize = new Vector2(400, 600);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Formation System Auto Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool will:\n" +
                "• Find or create all necessary managers\n" +
                "• Set up proper references automatically\n" +
                "• Remove duplicate instances\n" +
                "• Clean up invalid cross-scene references\n" +
                "• Validate the complete setup",
                MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("Auto Setup Formation System", GUILayout.Height(40)))
            {
                SetupFormationSystem();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Clean Up Duplicates Only", GUILayout.Height(30)))
            {
                CleanUpDuplicates();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Validate Current Setup", GUILayout.Height(30)))
            {
                ValidateSetup();
            }

            GUILayout.Space(20);

            showLog = EditorGUILayout.Foldout(showLog, "Setup Log", true);
            if (showLog && setupLog.Count > 0)
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
                foreach (var log in setupLog)
                {
                    if (log.StartsWith("ERROR:"))
                        EditorGUILayout.HelpBox(log, MessageType.Error);
                    else if (log.StartsWith("WARNING:"))
                        EditorGUILayout.HelpBox(log, MessageType.Warning);
                    else if (log.StartsWith("SUCCESS:"))
                        EditorGUILayout.HelpBox(log, MessageType.Info);
                    else
                        EditorGUILayout.LabelField(log);
                }
                EditorGUILayout.EndScrollView();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Clear Log"))
            {
                setupLog.Clear();
            }
        }

        private void SetupFormationSystem()
        {
            setupLog.Clear();
            Log("=== Starting Formation System Auto Setup ===");

            // Step 1: Clean up duplicates first
            CleanUpDuplicates();

            // Step 2: Find or create CustomFormationManager
            var customFormationManager = SetupCustomFormationManager();

            // Step 3: Find or create FormationGroupManager
            var formationGroupManager = SetupFormationGroupManager();

            // Step 4: Find or create UnitSelectionManager
            var selectionManager = SetupUnitSelectionManager();

            // Step 5: Setup UnitDetailsUI
            SetupUnitDetailsUI(formationGroupManager);

            // Step 6: Setup FormationBuilderUI
            SetupFormationBuilderUI();

            // Step 7: Setup FormationSelectorUI
            SetupFormationSelectorUI();

            // Step 8: Final validation
            ValidateSetup();

            // Mark scene as dirty
            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            Log("SUCCESS: Formation System Auto Setup Complete!");
            Log("=== Setup Finished ===");
        }

        private CustomFormationManager SetupCustomFormationManager()
        {
            Log("\n--- Setting up CustomFormationManager ---");

            var existing = FindObjectsByType<CustomFormationManager>(FindObjectsSortMode.None);

            if (existing.Length > 1)
            {
                Log($"WARNING: Found {existing.Length} CustomFormationManager instances. Keeping the first one.");
                for (int i = 1; i < existing.Length; i++)
                {
                    DestroyImmediate(existing[i].gameObject);
                    Log($"Removed duplicate CustomFormationManager from {existing[i].gameObject.name}");
                }
            }

            var manager = FindFirstObjectByType<CustomFormationManager>();

            if (manager == null)
            {
                // Create new manager
                GameObject managerObj = new GameObject("CustomFormationManager");
                manager = managerObj.AddComponent<CustomFormationManager>();
                Log("Created new CustomFormationManager");
            }
            else
            {
                Log($"Found existing CustomFormationManager on {manager.gameObject.name}");
            }

            // Ensure it's marked as DontDestroyOnLoad if needed
            if (manager.transform.parent == null && manager.gameObject.scene.name == "DontDestroyOnLoad")
            {
                Log("CustomFormationManager is in DontDestroyOnLoad scene");
            }

            return manager;
        }

        private FormationGroupManager SetupFormationGroupManager()
        {
            Log("\n--- Setting up FormationGroupManager ---");

            var existing = FindObjectsByType<FormationGroupManager>(FindObjectsSortMode.None);

            if (existing.Length > 1)
            {
                Log($"WARNING: Found {existing.Length} FormationGroupManager instances. Keeping the first one.");
                for (int i = 1; i < existing.Length; i++)
                {
                    DestroyImmediate(existing[i].gameObject);
                    Log($"Removed duplicate FormationGroupManager from {existing[i].gameObject.name}");
                }
            }

            var manager = FindFirstObjectByType<FormationGroupManager>();

            if (manager == null)
            {
                // Try to find GameManager or similar
                GameObject managerObj = GameObject.Find("GameManager");
                if (managerObj == null)
                {
                    managerObj = new GameObject("FormationGroupManager");
                }

                manager = managerObj.GetComponent<FormationGroupManager>();
                if (manager == null)
                {
                    manager = managerObj.AddComponent<FormationGroupManager>();
                    Log($"Added FormationGroupManager to {managerObj.name}");
                }
            }
            else
            {
                Log($"Found existing FormationGroupManager on {manager.gameObject.name}");
            }

            // Setup references
            var selectionManager = FindFirstObjectByType<UnitSelectionManager>();
            if (selectionManager != null)
            {
                var so = new SerializedObject(manager);
                so.FindProperty("selectionManager").objectReferenceValue = selectionManager;
                so.FindProperty("mainCamera").objectReferenceValue = Camera.main;
                so.ApplyModifiedProperties();
                Log("Set up FormationGroupManager references");
            }

            return manager;
        }

        private UnitSelectionManager SetupUnitSelectionManager()
        {
            Log("\n--- Setting up UnitSelectionManager ---");

            var existing = FindObjectsByType<UnitSelectionManager>(FindObjectsSortMode.None);

            if (existing.Length > 1)
            {
                Log($"WARNING: Found {existing.Length} UnitSelectionManager instances. Keeping the first one.");
                for (int i = 1; i < existing.Length; i++)
                {
                    DestroyImmediate(existing[i].gameObject);
                    Log($"Removed duplicate UnitSelectionManager from {existing[i].gameObject.name}");
                }
            }

            var manager = FindFirstObjectByType<UnitSelectionManager>();

            if (manager == null)
            {
                Log("WARNING: UnitSelectionManager not found. It should exist in your scene.");
            }
            else
            {
                Log($"Found UnitSelectionManager on {manager.gameObject.name}");
            }

            return manager;
        }

        private void SetupUnitDetailsUI(FormationGroupManager formationGroupManager)
        {
            Log("\n--- Setting up UnitDetailsUI ---");

            var unitDetailsUI = FindFirstObjectByType<UnitDetailsUI>();

            if (unitDetailsUI == null)
            {
                Log("WARNING: UnitDetailsUI not found in scene");
                return;
            }

            Log($"Found UnitDetailsUI on {unitDetailsUI.gameObject.name}");

            var so = new SerializedObject(unitDetailsUI);

            // Remove any serialized FormationGroupManager reference (we use singleton now)
            var formationGroupManagerProp = so.FindProperty("formationGroupManager");
            if (formationGroupManagerProp != null && formationGroupManagerProp.objectReferenceValue != null)
            {
                formationGroupManagerProp.objectReferenceValue = null;
                Log("Removed cross-scene FormationGroupManager reference (using singleton now)");
            }

            // Setup FormationSettingsSO reference if missing
            var formationSettingsProp = so.FindProperty("formationSettings");
            if (formationSettingsProp != null)
            {
                if (formationSettingsProp.objectReferenceValue == null)
                {
                    // Try to find FormationSettings SO in project
                    var guids = AssetDatabase.FindAssets("t:FormationSettingsSO");
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        var formationSettings = AssetDatabase.LoadAssetAtPath<FormationSettingsSO>(path);
                        formationSettingsProp.objectReferenceValue = formationSettings;
                        Log($"Set FormationSettings SO reference: {formationSettings.name}");
                    }
                    else
                    {
                        Log("WARNING: No FormationSettings SO found in project. Create one via: Assets > Create > RTS > Formation Settings");
                    }
                }
                else
                {
                    Log("FormationSettings SO already assigned");
                }
            }

            // Setup FormationBuilderUI reference if missing
            var formationBuilderProp = so.FindProperty("formationBuilder");
            if (formationBuilderProp != null && formationBuilderProp.objectReferenceValue == null)
            {
                var formationBuilder = FindFirstObjectByType<FormationBuilderUI>();
                if (formationBuilder != null)
                {
                    formationBuilderProp.objectReferenceValue = formationBuilder;
                    Log("Set FormationBuilderUI reference");
                }
            }

            // Setup FormationSelectorUI reference if missing
            var formationSelectorProp = so.FindProperty("formationSelector");
            if (formationSelectorProp != null && formationSelectorProp.objectReferenceValue == null)
            {
                var formationSelector = FindFirstObjectByType<FormationSelectorUI>();
                if (formationSelector != null)
                {
                    formationSelectorProp.objectReferenceValue = formationSelector;
                    Log("Set FormationSelectorUI reference");
                }
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(unitDetailsUI);
        }

        private void SetupFormationBuilderUI()
        {
            Log("\n--- Setting up FormationBuilderUI ---");

            var formationBuilder = FindFirstObjectByType<FormationBuilderUI>();

            if (formationBuilder == null)
            {
                Log("INFO: FormationBuilderUI not found (may be created at runtime)");
                return;
            }

            Log($"Found FormationBuilderUI on {formationBuilder.gameObject.name}");
            EditorUtility.SetDirty(formationBuilder);
        }

        private void SetupFormationSelectorUI()
        {
            Log("\n--- Setting up FormationSelectorUI ---");

            var formationSelector = FindFirstObjectByType<FormationSelectorUI>();

            if (formationSelector == null)
            {
                Log("INFO: FormationSelectorUI not found (optional component)");
                return;
            }

            Log($"Found FormationSelectorUI on {formationSelector.gameObject.name}");
            EditorUtility.SetDirty(formationSelector);
        }

        private void CleanUpDuplicates()
        {
            setupLog.Clear();
            Log("=== Cleaning Up Duplicates ===");

            // Clean up CustomFormationManager duplicates
            CleanupDuplicatesOfType<CustomFormationManager>("CustomFormationManager");

            // Clean up FormationGroupManager duplicates
            CleanupDuplicatesOfType<FormationGroupManager>("FormationGroupManager");

            // Clean up UnitSelectionManager duplicates
            CleanupDuplicatesOfType<UnitSelectionManager>("UnitSelectionManager");

            // Clean up UnitDetailsUI duplicates
            CleanupDuplicatesOfType<UnitDetailsUI>("UnitDetailsUI");

            // Clean up FormationBuilderUI duplicates
            CleanupDuplicatesOfType<FormationBuilderUI>("FormationBuilderUI");

            // Clean up FormationSelectorUI duplicates
            CleanupDuplicatesOfType<FormationSelectorUI>("FormationSelectorUI");

            // Mark scene as dirty
            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            Log("=== Cleanup Complete ===");
        }

        private void CleanupDuplicatesOfType<T>(string componentName) where T : Component
        {
            var components = FindObjectsByType<T>(FindObjectsSortMode.None);

            if (components.Length > 1)
            {
                Log($"WARNING: Found {components.Length} {componentName} instances");

                // Keep the first one, destroy the rest
                for (int i = 1; i < components.Length; i++)
                {
                    Log($"Removing duplicate {componentName} from {components[i].gameObject.name}");
                    DestroyImmediate(components[i]);
                }

                Log($"SUCCESS: Kept only one {componentName} instance");
            }
            else if (components.Length == 1)
            {
                Log($"{componentName}: OK (1 instance found)");
            }
            else
            {
                Log($"INFO: No {componentName} found in scene");
            }
        }

        private void ValidateSetup()
        {
            setupLog.Clear();
            Log("=== Validating Formation System Setup ===");

            bool isValid = true;

            // Check CustomFormationManager
            var customFormationManager = FindFirstObjectByType<CustomFormationManager>();
            if (customFormationManager == null)
            {
                Log("ERROR: CustomFormationManager not found!");
                isValid = false;
            }
            else if (CustomFormationManager.Instance == null)
            {
                Log("ERROR: CustomFormationManager.Instance is null!");
                isValid = false;
            }
            else
            {
                Log("✓ CustomFormationManager: OK");
            }

            // Check FormationGroupManager
            var formationGroupManager = FindFirstObjectByType<FormationGroupManager>();
            if (formationGroupManager == null)
            {
                Log("ERROR: FormationGroupManager not found!");
                isValid = false;
            }
            else if (FormationGroupManager.Instance == null)
            {
                Log("ERROR: FormationGroupManager.Instance is null!");
                isValid = false;
            }
            else
            {
                Log("✓ FormationGroupManager: OK");
            }

            // Check UnitSelectionManager
            var selectionManager = FindFirstObjectByType<UnitSelectionManager>();
            if (selectionManager == null)
            {
                Log("WARNING: UnitSelectionManager not found (should exist in scene)");
            }
            else
            {
                Log("✓ UnitSelectionManager: OK");
            }

            // Check UnitDetailsUI
            var unitDetailsUI = FindFirstObjectByType<UnitDetailsUI>();
            if (unitDetailsUI == null)
            {
                Log("WARNING: UnitDetailsUI not found");
            }
            else
            {
                var so = new SerializedObject(unitDetailsUI);
                var formationGroupManagerProp = so.FindProperty("formationGroupManager");

                // Check for cross-scene references
                if (formationGroupManagerProp != null && formationGroupManagerProp.objectReferenceValue != null)
                {
                    Log("WARNING: UnitDetailsUI has serialized FormationGroupManager reference (should use singleton)");
                    isValid = false;
                }
                else
                {
                    Log("✓ UnitDetailsUI: No cross-scene references");
                }

                // Check FormationSettingsSO reference
                var formationSettingsProp = so.FindProperty("formationSettings");
                if (formationSettingsProp != null)
                {
                    if (formationSettingsProp.objectReferenceValue == null)
                    {
                        Log("WARNING: UnitDetailsUI is missing FormationSettings SO reference");
                        Log("  Create one via: Assets > Create > RTS > Formation Settings");
                    }
                    else
                    {
                        Log("✓ UnitDetailsUI: FormationSettings SO assigned");
                    }
                }
            }

            // Check for duplicates
            var customFormationManagers = FindObjectsByType<CustomFormationManager>(FindObjectsSortMode.None);
            if (customFormationManagers.Length > 1)
            {
                Log($"ERROR: Found {customFormationManagers.Length} CustomFormationManager instances!");
                isValid = false;
            }

            var formationGroupManagers = FindObjectsByType<FormationGroupManager>(FindObjectsSortMode.None);
            if (formationGroupManagers.Length > 1)
            {
                Log($"ERROR: Found {formationGroupManagers.Length} FormationGroupManager instances!");
                isValid = false;
            }

            // Final result
            if (isValid)
            {
                Log("\nSUCCESS: All validation checks passed! ✓");
            }
            else
            {
                Log("\nERROR: Validation failed. Please run Auto Setup.");
            }

            Log("=== Validation Complete ===");
        }

        private void Log(string message)
        {
            setupLog.Add(message);
            Debug.Log($"[Formation Auto Setup] {message}");
            Repaint();
        }
    }
}
