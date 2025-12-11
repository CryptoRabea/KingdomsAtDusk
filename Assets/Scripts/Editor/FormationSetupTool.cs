using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using RTS.Units.Formation;
using RTS.Core;

namespace RTS.Editor
{
    /// <summary>
    /// Comprehensive tool for automating formation system setup and configuration
    /// </summary>
    public class FormationSetupTool : EditorWindow
    {
        private enum TabType
        {
            SceneSetup,
            PresetTemplates,
            CustomFormations,
            BatchOperations,
            Testing
        }

        private TabType currentTab = TabType.SceneSetup;
        private Vector2 scrollPosition;

        // Scene Setup
        private GameObject formationManagersRoot;
        private FormationSettingsSO formationSettings;
        private bool autoCreateSettings = true;
        private bool autoWireReferences = true;

        // Preset Templates
        private string newFormationName = "New Formation";
        private FormationTemplate selectedTemplate = FormationTemplate.Infantry;
        private int templateUnitCount = 20;

        // Custom Formations
        private int gridWidth = 20;
        private int gridHeight = 20;
        private GenerationPattern pattern = GenerationPattern.Grid;
        private int generatedUnitCount = 25;

        // Batch Operations
        private List<CustomFormationData> formationsToImport = new List<CustomFormationData>();
        private string exportPath = "";

        // Testing
        private CustomFormationData previewFormation;
        private int previewUnitCount = 10;
        private float previewSpacing = 2.5f;
        private Vector3 previewCenter = Vector3.zero;

        private enum FormationTemplate
        {
            Infantry,
            Cavalry,
            Archers,
            Phalanx,
            ShieldWall,
            SkirmishLine,
            Turtle,
            CrescentMoon,
            DoubleEnvelopment,
            Flying_V
        }

        private enum GenerationPattern
        {
            Grid,
            Circle,
            Diamond,
            Star,
            Arrow,
            Cross,
            Checkboard,
            Spiral,
            Random
        }

        [MenuItem("RTS Tools/Formation Setup Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<FormationSetupTool>("Formation Setup Tool");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        private void OnEnable()
        {
            LoadPreferences();
            RefreshManagers();
        }

        private void OnDisable()
        {
            SavePreferences();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawTabs();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (currentTab)
            {
                case TabType.SceneSetup:
                    DrawSceneSetupTab();
                    break;
                case TabType.PresetTemplates:
                    DrawPresetTemplatesTab();
                    break;
                case TabType.CustomFormations:
                    DrawCustomFormationsTab();
                    break;
                case TabType.BatchOperations:
                    DrawBatchOperationsTab();
                    break;
                case TabType.Testing:
                    DrawTestingTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        #region Header & Tabs

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            GUILayout.Label("Formation System Setup Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Automated tool for setting up, configuring, and testing the formation system.",
                MessageType.Info
            );
            EditorGUILayout.Space(5);
        }

        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(currentTab == TabType.SceneSetup, "Scene Setup", EditorStyles.toolbarButton))
                currentTab = TabType.SceneSetup;
            if (GUILayout.Toggle(currentTab == TabType.PresetTemplates, "Templates", EditorStyles.toolbarButton))
                currentTab = TabType.PresetTemplates;
            if (GUILayout.Toggle(currentTab == TabType.CustomFormations, "Generator", EditorStyles.toolbarButton))
                currentTab = TabType.CustomFormations;
            if (GUILayout.Toggle(currentTab == TabType.BatchOperations, "Batch Ops", EditorStyles.toolbarButton))
                currentTab = TabType.BatchOperations;
            if (GUILayout.Toggle(currentTab == TabType.Testing, "Testing", EditorStyles.toolbarButton))
                currentTab = TabType.Testing;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);
        }

        #endregion

        #region Scene Setup Tab

        private void DrawSceneSetupTab()
        {
            GUILayout.Label("Scene Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Automatically create and configure all formation managers in the current scene.",
                MessageType.Info
            );

            EditorGUILayout.Space(10);

            // Current Status
            GUILayout.Label("Current Scene Status:", EditorStyles.boldLabel);
            DrawManagerStatus("FormationGroupManager", FindObjectOfType<FormationGroupManager>() != null);
            DrawManagerStatus("CustomFormationManager", FindObjectOfType<CustomFormationManager>() != null);
            DrawManagerStatus("FormationSettingsSO", formationSettings != null);

            EditorGUILayout.Space(10);

            // Configuration
            GUILayout.Label("Setup Configuration:", EditorStyles.boldLabel);
            autoCreateSettings = EditorGUILayout.Toggle("Auto-Create Settings Asset", autoCreateSettings);
            autoWireReferences = EditorGUILayout.Toggle("Auto-Wire References", autoWireReferences);
            formationSettings = EditorGUILayout.ObjectField(
                "Formation Settings",
                formationSettings,
                typeof(FormationSettingsSO),
                false
            ) as FormationSettingsSO;

            EditorGUILayout.Space(10);

            // Setup Buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Setup Everything", GUILayout.Height(40)))
            {
                SetupCompleteFormationSystem();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create FormationGroupManager"))
            {
                CreateFormationGroupManager();
            }
            if (GUILayout.Button("Create CustomFormationManager"))
            {
                CreateCustomFormationManager();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Create Formation Settings Asset"))
            {
                CreateFormationSettingsAsset();
            }

            EditorGUILayout.Space(10);

            // Validation
            if (GUILayout.Button("Validate Setup"))
            {
                ValidateFormationSetup();
            }
        }

        private void DrawManagerStatus(string name, bool exists)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(name, GUILayout.Width(200));
            if (exists)
            {
                GUI.color = Color.green;
                GUILayout.Label("✓ Found", EditorStyles.boldLabel);
            }
            else
            {
                GUI.color = Color.yellow;
                GUILayout.Label("✗ Missing", EditorStyles.boldLabel);
            }
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Preset Templates Tab

        private void DrawPresetTemplatesTab()
        {
            GUILayout.Label("Formation Templates", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Create custom formations based on historical military templates.",
                MessageType.Info
            );

            EditorGUILayout.Space(10);

            newFormationName = EditorGUILayout.TextField("Formation Name", newFormationName);
            selectedTemplate = (FormationTemplate)EditorGUILayout.EnumPopup("Template", selectedTemplate);
            templateUnitCount = EditorGUILayout.IntSlider("Unit Count", templateUnitCount, 5, 50);

            EditorGUILayout.Space(10);

            // Template Description
            DrawTemplateDescription(selectedTemplate);

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Create Formation from Template", GUILayout.Height(40)))
            {
                CreateFormationFromTemplate();
            }

            EditorGUILayout.Space(10);

            // Available Templates List
            GUILayout.Label("Available Templates:", EditorStyles.boldLabel);
            foreach (FormationTemplate template in Enum.GetValues(typeof(FormationTemplate)))
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"• {FormatEnumName(template)}", GUILayout.Width(200));
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    selectedTemplate = template;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawTemplateDescription(FormationTemplate template)
        {
            string description = template switch
            {
                FormationTemplate.Infantry => "Standard infantry formation - tight grid for maximum combat power",
                FormationTemplate.Cavalry => "Wide spread formation optimized for mounted units",
                FormationTemplate.Archers => "Staggered ranks allowing clear lines of fire",
                FormationTemplate.Phalanx => "Dense rectangular formation with overlapping coverage",
                FormationTemplate.ShieldWall => "Tight horizontal line with minimal gaps",
                FormationTemplate.SkirmishLine => "Loose spread for harassment tactics",
                FormationTemplate.Turtle => "Defensive box with units on all sides",
                FormationTemplate.CrescentMoon => "Curved envelopment formation",
                FormationTemplate.DoubleEnvelopment => "Pincer movement with strong flanks",
                FormationTemplate.Flying_V => "Wedge with extended wings for breakthrough",
                _ => "Unknown template"
            };

            EditorGUILayout.HelpBox(description, MessageType.Info);
        }

        #endregion

        #region Custom Formations Tab

        private void DrawCustomFormationsTab()
        {
            GUILayout.Label("Custom Formation Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Programmatically generate custom formations using mathematical patterns.",
                MessageType.Info
            );

            EditorGUILayout.Space(10);

            newFormationName = EditorGUILayout.TextField("Formation Name", newFormationName);
            pattern = (GenerationPattern)EditorGUILayout.EnumPopup("Pattern", pattern);
            generatedUnitCount = EditorGUILayout.IntSlider("Unit Count", generatedUnitCount, 5, 50);

            EditorGUILayout.Space(5);

            gridWidth = EditorGUILayout.IntSlider("Grid Width", gridWidth, 10, 30);
            gridHeight = EditorGUILayout.IntSlider("Grid Height", gridHeight, 10, 30);

            EditorGUILayout.Space(10);

            DrawPatternDescription(pattern);

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Generate Formation", GUILayout.Height(40)))
            {
                GenerateCustomFormation();
            }

            EditorGUILayout.Space(10);

            // Available Patterns
            GUILayout.Label("Available Patterns:", EditorStyles.boldLabel);
            foreach (GenerationPattern p in Enum.GetValues(typeof(GenerationPattern)))
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"• {FormatEnumName(p)}", GUILayout.Width(150));
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    pattern = p;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawPatternDescription(GenerationPattern pattern)
        {
            string description = pattern switch
            {
                GenerationPattern.Grid => "Regular grid pattern with even spacing",
                GenerationPattern.Circle => "Units arranged in circular formation",
                GenerationPattern.Diamond => "Diamond/rhombus shape",
                GenerationPattern.Star => "Star pattern with radiating points",
                GenerationPattern.Arrow => "Arrow/chevron pointing forward",
                GenerationPattern.Cross => "Cross/plus shape",
                GenerationPattern.Checkboard => "Checkerboard pattern with gaps",
                GenerationPattern.Spiral => "Spiral pattern from center outward",
                GenerationPattern.Random => "Random scatter within bounds",
                _ => "Unknown pattern"
            };

            EditorGUILayout.HelpBox(description, MessageType.Info);
        }

        #endregion

        #region Batch Operations Tab

        private void DrawBatchOperationsTab()
        {
            GUILayout.Label("Batch Operations", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Import, export, and manage multiple formations at once.",
                MessageType.Info
            );

            EditorGUILayout.Space(10);

            // Export Section
            GUILayout.Label("Export Formations", EditorStyles.boldLabel);
            if (GUILayout.Button("Export All Custom Formations"))
            {
                ExportAllFormations();
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Export Selected Formations"))
            {
                ExportSelectedFormations();
            }

            EditorGUILayout.Space(10);

            // Import Section
            GUILayout.Label("Import Formations", EditorStyles.boldLabel);
            if (GUILayout.Button("Import Formations from File"))
            {
                ImportFormationsFromFile();
            }

            EditorGUILayout.Space(10);

            // Bulk Operations
            GUILayout.Label("Bulk Operations", EditorStyles.boldLabel);
            if (GUILayout.Button("Clear All Custom Formations"))
            {
                if (EditorUtility.DisplayDialog(
                    "Clear All Formations",
                    "Are you sure you want to delete all custom formations? This cannot be undone.",
                    "Yes, Delete All",
                    "Cancel"))
                {
                    ClearAllFormations();
                }
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Generate Standard Formation Pack"))
            {
                GenerateStandardFormationPack();
            }

            EditorGUILayout.Space(10);

            // Current Formations List
            DrawCurrentFormationsList();
        }

        private void DrawCurrentFormationsList()
        {
            GUILayout.Label("Current Custom Formations:", EditorStyles.boldLabel);

            var manager = CustomFormationManager.Instance;
            if (manager == null)
            {
                EditorGUILayout.HelpBox("CustomFormationManager not found in scene.", MessageType.Warning);
                return;
            }

            var formations = manager.GetAllFormations();
            if (formations.Count == 0)
            {
                EditorGUILayout.HelpBox("No custom formations found.", MessageType.Info);
                return;
            }

            foreach (var formation in formations)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(formation.name, GUILayout.Width(200));
                GUILayout.Label($"{formation.positions.Count} units", GUILayout.Width(80));

                if (GUILayout.Button("Duplicate", GUILayout.Width(70)))
                {
                    manager.DuplicateFormation(formation.id);
                }

                if (GUILayout.Button("Delete", GUILayout.Width(60)))
                {
                    if (EditorUtility.DisplayDialog(
                        "Delete Formation",
                        $"Delete formation '{formation.name}'?",
                        "Delete",
                        "Cancel"))
                    {
                        manager.DeleteFormation(formation.id);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        #endregion

        #region Testing Tab

        private void DrawTestingTab()
        {
            GUILayout.Label("Formation Testing", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Test and validate formations in the editor.",
                MessageType.Info
            );

            EditorGUILayout.Space(10);

            // Formation Selection
            GUILayout.Label("Select Formation to Test:", EditorStyles.boldLabel);

            var manager = CustomFormationManager.Instance;
            if (manager != null)
            {
                var formations = manager.GetAllFormations();
                if (formations.Count > 0)
                {
                    foreach (var formation in formations)
                    {
                        if (GUILayout.Button(formation.name))
                        {
                            previewFormation = formation;
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No formations to test. Create some first!", MessageType.Info);
                }
            }

            EditorGUILayout.Space(10);

            if (previewFormation != null)
            {
                GUILayout.Label($"Testing: {previewFormation.name}", EditorStyles.boldLabel);

                previewUnitCount = EditorGUILayout.IntSlider("Unit Count", previewUnitCount, 1, previewFormation.positions.Count);
                previewSpacing = EditorGUILayout.Slider("Spacing", previewSpacing, 0.5f, 10f);
                previewCenter = EditorGUILayout.Vector3Field("Center Position", previewCenter);

                EditorGUILayout.Space(10);

                if (GUILayout.Button("Visualize Formation (Scene View)"))
                {
                    VisualizeFormation();
                }

                EditorGUILayout.Space(5);

                if (GUILayout.Button("Generate Test Units"))
                {
                    GenerateTestUnits();
                }
            }

            EditorGUILayout.Space(10);

            // Validation
            GUILayout.Label("Validation Tools:", EditorStyles.boldLabel);
            if (GUILayout.Button("Validate All Formations"))
            {
                ValidateAllFormations();
            }
        }

        #endregion

        #region Implementation Methods

        private void SetupCompleteFormationSystem()
        {
            EditorUtility.DisplayProgressBar("Formation Setup", "Setting up formation system...", 0f);

            try
            {
                // Step 1: Create settings if needed
                if (formationSettings == null && autoCreateSettings)
                {
                    EditorUtility.DisplayProgressBar("Formation Setup", "Creating settings asset...", 0.2f);
                    CreateFormationSettingsAsset();
                }

                // Step 2: Create managers root
                EditorUtility.DisplayProgressBar("Formation Setup", "Creating managers...", 0.4f);
                if (formationManagersRoot == null)
                {
                    formationManagersRoot = new GameObject("Formation Managers");
                    Undo.RegisterCreatedObjectUndo(formationManagersRoot, "Create Formation Managers Root");
                }

                // Step 3: Create FormationGroupManager
                EditorUtility.DisplayProgressBar("Formation Setup", "Setting up FormationGroupManager...", 0.6f);
                var groupManager = CreateFormationGroupManager();

                // Step 4: Create CustomFormationManager
                EditorUtility.DisplayProgressBar("Formation Setup", "Setting up CustomFormationManager...", 0.8f);
                var customManager = CreateCustomFormationManager();

                // Step 5: Wire references
                if (autoWireReferences && formationSettings != null && groupManager != null)
                {
                    EditorUtility.DisplayProgressBar("Formation Setup", "Wiring references...", 0.9f);
                    SerializedObject so = new SerializedObject(groupManager);
                    so.FindProperty("settings").objectReferenceValue = formationSettings;
                    so.ApplyModifiedProperties();
                }

                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog(
                    "Setup Complete",
                    "Formation system has been successfully set up!\n\n" +
                    "✓ FormationGroupManager created\n" +
                    "✓ CustomFormationManager created\n" +
                    "✓ Settings asset configured\n" +
                    "✓ All references wired",
                    "OK"
                );

                RefreshManagers();
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Setup Failed", $"Error during setup: {e.Message}", "OK");
                Debug.LogError($"Formation setup failed: {e}");
            }
        }

        private FormationGroupManager CreateFormationGroupManager()
        {
            var existing = FindObjectOfType<FormationGroupManager>();
            if (existing != null)
            {
                Debug.LogWarning("FormationGroupManager already exists in scene.");
                return existing;
            }

            if (formationManagersRoot == null)
            {
                formationManagersRoot = new GameObject("Formation Managers");
                Undo.RegisterCreatedObjectUndo(formationManagersRoot, "Create Formation Managers Root");
            }

            var go = new GameObject("FormationGroupManager");
            Undo.RegisterCreatedObjectUndo(go, "Create FormationGroupManager");
            go.transform.SetParent(formationManagersRoot.transform);

            var manager = go.AddComponent<FormationGroupManager>();

            if (formationSettings != null)
            {
                SerializedObject so = new SerializedObject(manager);
                so.FindProperty("settings").objectReferenceValue = formationSettings;
                so.ApplyModifiedProperties();
            }

            Debug.Log("FormationGroupManager created successfully.");
            return manager;
        }

        private CustomFormationManager CreateCustomFormationManager()
        {
            var existing = FindObjectOfType<CustomFormationManager>();
            if (existing != null)
            {
                Debug.LogWarning("CustomFormationManager already exists in scene.");
                return existing;
            }

            if (formationManagersRoot == null)
            {
                formationManagersRoot = new GameObject("Formation Managers");
                Undo.RegisterCreatedObjectUndo(formationManagersRoot, "Create Formation Managers Root");
            }

            var go = new GameObject("CustomFormationManager");
            Undo.RegisterCreatedObjectUndo(go, "Create CustomFormationManager");
            go.transform.SetParent(formationManagersRoot.transform);

            var manager = go.AddComponent<CustomFormationManager>();

            Debug.Log("CustomFormationManager created successfully.");
            return manager;
        }

        private void CreateFormationSettingsAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Formation Settings",
                "FormationSettings",
                "asset",
                "Choose location for Formation Settings asset"
            );

            if (string.IsNullOrEmpty(path))
                return;

            var settings = ScriptableObject.CreateInstance<FormationSettingsSO>();

            // Set default values
            SerializedObject so = new SerializedObject(settings);
            so.FindProperty("defaultFormationType").enumValueIndex = (int)FormationType.Box;
            so.FindProperty("defaultSpacing").floatValue = 2.5f;
            so.FindProperty("largeGroupSpacingMultiplier").floatValue = 1.2f;
            so.FindProperty("largeGroupThreshold").intValue = 15;
            so.FindProperty("validatePositions").boolValue = true;
            so.FindProperty("maxValidationDistance").floatValue = 5f;
            so.ApplyModifiedProperties();

            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.SaveAssets();

            formationSettings = settings;
            Debug.Log($"Formation Settings created at: {path}");
        }

        private void CreateFormationFromTemplate()
        {
            var manager = CustomFormationManager.Instance;
            if (manager == null)
            {
                EditorUtility.DisplayDialog("Error", "CustomFormationManager not found. Set up scene first.", "OK");
                return;
            }

            var positions = GenerateTemplatePositions(selectedTemplate, templateUnitCount);

            // Create formation and add positions
            var formation = manager.CreateFormation(newFormationName);
            formation.positions = positions;
            manager.UpdateFormation(formation);

            Debug.Log($"Created formation '{newFormationName}' from template '{selectedTemplate}' with {positions.Count} positions.");
            EditorUtility.DisplayDialog("Success", $"Formation '{newFormationName}' created successfully!", "OK");
        }

        private List<FormationPosition> GenerateTemplatePositions(FormationTemplate template, int count)
        {
            var positions = new List<FormationPosition>();

            switch (template)
            {
                case FormationTemplate.Infantry:
                    positions = GenerateGridPattern(count, true);
                    break;
                case FormationTemplate.Cavalry:
                    positions = GenerateGridPattern(count, false, 1.5f);
                    break;
                case FormationTemplate.Archers:
                    positions = GenerateStaggeredRanks(count);
                    break;
                case FormationTemplate.Phalanx:
                    positions = GeneratePhalanx(count);
                    break;
                case FormationTemplate.ShieldWall:
                    positions = GenerateShieldWall(count);
                    break;
                case FormationTemplate.SkirmishLine:
                    positions = GenerateSkirmishLine(count);
                    break;
                case FormationTemplate.Turtle:
                    positions = GenerateTurtle(count);
                    break;
                case FormationTemplate.CrescentMoon:
                    positions = GenerateCrescent(count);
                    break;
                case FormationTemplate.DoubleEnvelopment:
                    positions = GenerateDoubleEnvelopment(count);
                    break;
                case FormationTemplate.Flying_V:
                    positions = GenerateFlyingV(count);
                    break;
            }

            return positions;
        }

        private List<FormationPosition> GenerateGridPattern(int count, bool tight = true, float spacingMultiplier = 1f)
        {
            var positions = new List<FormationPosition>();
            int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
            int rows = Mathf.CeilToInt((float)count / cols);

            float spacing = (tight ? 0.1f : 0.15f) * spacingMultiplier;

            for (int i = 0; i < count; i++)
            {
                int row = i / cols;
                int col = i % cols;

                float x = (col - (cols - 1) / 2f) * spacing;
                float y = (row - (rows - 1) / 2f) * spacing;

                positions.Add(new FormationPosition(new Vector2(x, y)));
            }

            return NormalizePositions(positions);
        }

        private List<FormationPosition> GenerateStaggeredRanks(int count)
        {
            var positions = new List<FormationPosition>();
            int ranksCount = Mathf.CeilToInt(count / 5f);
            int unitsPerRank = Mathf.CeilToInt((float)count / ranksCount);

            for (int rank = 0; rank < ranksCount && positions.Count < count; rank++)
            {
                float rankY = rank * 0.15f;
                float offset = (rank % 2) * 0.075f; // Stagger every other rank

                int unitsInThisRank = Mathf.Min(unitsPerRank, count - positions.Count);

                for (int i = 0; i < unitsInThisRank; i++)
                {
                    float x = (i - (unitsInThisRank - 1) / 2f) * 0.15f + offset;
                    positions.Add(new FormationPosition(new Vector2(x, rankY)));
                }
            }

            return NormalizePositions(positions);
        }

        private List<FormationPosition> GeneratePhalanx(int count)
        {
            var positions = new List<FormationPosition>();
            int width = Mathf.Max(5, Mathf.CeilToInt(count / 4f));
            int depth = Mathf.CeilToInt((float)count / width);

            float spacing = 0.08f; // Very tight

            for (int i = 0; i < count; i++)
            {
                int row = i / width;
                int col = i % width;

                float x = (col - (width - 1) / 2f) * spacing;
                float y = row * spacing;

                positions.Add(new FormationPosition(new Vector2(x, y)));
            }

            return NormalizePositions(positions);
        }

        private List<FormationPosition> GenerateShieldWall(int count)
        {
            var positions = new List<FormationPosition>();
            float spacing = 1.8f / count; // Spread across width

            for (int i = 0; i < count; i++)
            {
                float x = (i - (count - 1) / 2f) * spacing;
                positions.Add(new FormationPosition(new Vector2(x, 0)));
            }

            return NormalizePositions(positions);
        }

        private List<FormationPosition> GenerateSkirmishLine(int count)
        {
            var positions = new List<FormationPosition>();
            float spacing = 1.9f / count;

            for (int i = 0; i < count; i++)
            {
                float x = (i - (count - 1) / 2f) * spacing;
                float y = UnityEngine.Random.Range(-0.1f, 0.1f); // Slight randomness
                positions.Add(new FormationPosition(new Vector2(x, y)));
            }

            return NormalizePositions(positions);
        }

        private List<FormationPosition> GenerateTurtle(int count)
        {
            var positions = new List<FormationPosition>();

            // Create box perimeter
            int perimeterCount = Mathf.CeilToInt(count * 0.7f);
            int interiorCount = count - perimeterCount;

            int side = Mathf.CeilToInt(Mathf.Sqrt(perimeterCount / 4f));

            // Top and bottom
            for (int i = 0; i < side; i++)
            {
                float x = (i / (float)(side - 1)) * 1.6f - 0.8f;
                positions.Add(new FormationPosition(new Vector2(x, 0.8f)));
                positions.Add(new FormationPosition(new Vector2(x, -0.8f)));
            }

            // Left and right (excluding corners)
            for (int i = 1; i < side - 1; i++)
            {
                float y = (i / (float)(side - 1)) * 1.6f - 0.8f;
                positions.Add(new FormationPosition(new Vector2(-0.8f, y)));
                positions.Add(new FormationPosition(new Vector2(0.8f, y)));
            }

            // Fill interior if needed
            while (positions.Count < count)
            {
                float x = UnityEngine.Random.Range(-0.6f, 0.6f);
                float y = UnityEngine.Random.Range(-0.6f, 0.6f);
                positions.Add(new FormationPosition(new Vector2(x, y)));
            }

            return NormalizePositions(positions);
        }

        private List<FormationPosition> GenerateCrescent(int count)
        {
            var positions = new List<FormationPosition>();

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)(count - 1);
                float angle = Mathf.Lerp(-120f, 120f, t) * Mathf.Deg2Rad;

                float x = Mathf.Sin(angle) * 0.8f;
                float y = -Mathf.Cos(angle) * 0.5f + 0.3f;

                positions.Add(new FormationPosition(new Vector2(x, y)));
            }

            return NormalizePositions(positions);
        }

        private List<FormationPosition> GenerateDoubleEnvelopment(int count)
        {
            var positions = new List<FormationPosition>();

            int centerCount = Mathf.CeilToInt(count * 0.4f);
            int wingCount = (count - centerCount) / 2;

            // Center
            for (int i = 0; i < centerCount; i++)
            {
                float x = (i - (centerCount - 1) / 2f) * 0.1f;
                positions.Add(new FormationPosition(new Vector2(x, 0)));
            }

            // Left wing
            for (int i = 0; i < wingCount; i++)
            {
                float t = i / (float)wingCount;
                float x = -0.8f + t * 0.3f;
                float y = 0.3f + t * 0.5f;
                positions.Add(new FormationPosition(new Vector2(x, y)));
            }

            // Right wing
            for (int i = 0; i < wingCount; i++)
            {
                float t = i / (float)wingCount;
                float x = 0.8f - t * 0.3f;
                float y = 0.3f + t * 0.5f;
                positions.Add(new FormationPosition(new Vector2(x, y)));
            }

            return NormalizePositions(positions);
        }

        private List<FormationPosition> GenerateFlyingV(int count)
        {
            var positions = new List<FormationPosition>();

            // Leader at front
            positions.Add(new FormationPosition(new Vector2(0, -0.9f)));

            int remaining = count - 1;
            int perSide = remaining / 2;

            // Left wing
            for (int i = 0; i < perSide; i++)
            {
                float x = -(i + 1) * 0.15f;
                float y = -0.9f + (i + 1) * 0.2f;
                positions.Add(new FormationPosition(new Vector2(x, y)));
            }

            // Right wing
            for (int i = 0; i < perSide; i++)
            {
                float x = (i + 1) * 0.15f;
                float y = -0.9f + (i + 1) * 0.2f;
                positions.Add(new FormationPosition(new Vector2(x, y)));
            }

            // Add any remaining unit
            if (remaining % 2 == 1)
            {
                positions.Add(new FormationPosition(new Vector2(0, 0)));
            }

            return NormalizePositions(positions);
        }

        private void GenerateCustomFormation()
        {
            var manager = CustomFormationManager.Instance;
            if (manager == null)
            {
                EditorUtility.DisplayDialog("Error", "CustomFormationManager not found. Set up scene first.", "OK");
                return;
            }

            var positions = GeneratePatternPositions(pattern, generatedUnitCount);

            // Create formation and add positions
            var formation = manager.CreateFormation(newFormationName);
            formation.positions = positions;
            manager.UpdateFormation(formation);

            Debug.Log($"Generated formation '{newFormationName}' with pattern '{pattern}' and {positions.Count} positions.");
            EditorUtility.DisplayDialog("Success", $"Formation '{newFormationName}' generated successfully!", "OK");
        }

        private List<FormationPosition> GeneratePatternPositions(GenerationPattern pattern, int count)
        {
            return pattern switch
            {
                GenerationPattern.Grid => GenerateGridPattern(count),
                GenerationPattern.Circle => GenerateCirclePattern(count),
                GenerationPattern.Diamond => GenerateDiamondPattern(count),
                GenerationPattern.Star => GenerateStarPattern(count),
                GenerationPattern.Arrow => GenerateArrowPattern(count),
                GenerationPattern.Cross => GenerateCrossPattern(count),
                GenerationPattern.Checkboard => GenerateCheckerboardPattern(count),
                GenerationPattern.Spiral => GenerateSpiralPattern(count),
                GenerationPattern.Random => GenerateRandomPattern(count),
                _ => GenerateGridPattern(count)
            };
        }

        private List<FormationPosition> GenerateCirclePattern(int count)
        {
            var positions = new List<FormationPosition>();
            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle) * 0.8f;
                float y = Mathf.Sin(angle) * 0.8f;
                positions.Add(new FormationPosition(new Vector2(x, y)));
            }

            return positions;
        }

        private List<FormationPosition> GenerateDiamondPattern(int count)
        {
            var positions = new List<FormationPosition>();
            int side = Mathf.CeilToInt(count / 4f);

            // Top right
            for (int i = 0; i < side && positions.Count < count; i++)
            {
                float t = i / (float)side;
                positions.Add(new FormationPosition(new Vector2(t * 0.9f, (1 - t) * 0.9f)));
            }

            // Bottom right
            for (int i = 0; i < side && positions.Count < count; i++)
            {
                float t = i / (float)side;
                positions.Add(new FormationPosition(new Vector2((1 - t) * 0.9f, -t * 0.9f)));
            }

            // Bottom left
            for (int i = 0; i < side && positions.Count < count; i++)
            {
                float t = i / (float)side;
                positions.Add(new FormationPosition(new Vector2(-t * 0.9f, -(1 - t) * 0.9f)));
            }

            // Top left
            for (int i = 0; i < side && positions.Count < count; i++)
            {
                float t = i / (float)side;
                positions.Add(new FormationPosition(new Vector2(-(1 - t) * 0.9f, t * 0.9f)));
            }

            return positions;
        }

        private List<FormationPosition> GenerateStarPattern(int count)
        {
            var positions = new List<FormationPosition>();
            int points = 5;
            float angleStep = 360f / (count / (float)points);

            for (int i = 0; i < count; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                float radius = (i % 2 == 0) ? 0.9f : 0.45f;
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius;
                positions.Add(new FormationPosition(new Vector2(x, y)));
            }

            return positions;
        }

        private List<FormationPosition> GenerateArrowPattern(int count)
        {
            var positions = new List<FormationPosition>();

            // Tip
            positions.Add(new FormationPosition(new Vector2(0, -0.9f)));

            int remaining = count - 1;
            int rowCount = Mathf.CeilToInt(Mathf.Sqrt(remaining));

            for (int row = 1; row <= rowCount && positions.Count < count; row++)
            {
                int unitsInRow = Mathf.Min(row * 2, count - positions.Count);

                for (int i = 0; i < unitsInRow; i++)
                {
                    float x = (i - (unitsInRow - 1) / 2f) * 0.15f;
                    float y = -0.9f + row * 0.2f;
                    positions.Add(new FormationPosition(new Vector2(x, y)));
                }
            }

            return NormalizePositions(positions);
        }

        private List<FormationPosition> GenerateCrossPattern(int count)
        {
            var positions = new List<FormationPosition>();
            int perArm = count / 4;

            // Horizontal
            for (int i = -perArm; i <= perArm && positions.Count < count; i++)
            {
                if (i != 0)
                    positions.Add(new FormationPosition(new Vector2(i * 0.15f, 0)));
            }

            // Vertical
            for (int i = -perArm; i <= perArm && positions.Count < count; i++)
            {
                positions.Add(new FormationPosition(new Vector2(0, i * 0.15f)));
            }

            return NormalizePositions(positions);
        }

        private List<FormationPosition> GenerateCheckerboardPattern(int count)
        {
            var positions = new List<FormationPosition>();
            int side = Mathf.CeilToInt(Mathf.Sqrt(count * 2));

            for (int row = 0; row < side && positions.Count < count; row++)
            {
                for (int col = 0; col < side && positions.Count < count; col++)
                {
                    if ((row + col) % 2 == 0)
                    {
                        float x = (col - side / 2f) * 0.15f;
                        float y = (row - side / 2f) * 0.15f;
                        positions.Add(new FormationPosition(new Vector2(x, y)));
                    }
                }
            }

            return NormalizePositions(positions);
        }

        private List<FormationPosition> GenerateSpiralPattern(int count)
        {
            var positions = new List<FormationPosition>();
            float angleStep = 137.5f; // Golden angle

            for (int i = 0; i < count; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                float radius = Mathf.Sqrt(i / (float)count) * 0.9f;
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius;
                positions.Add(new FormationPosition(new Vector2(x, y)));
            }

            return positions;
        }

        private List<FormationPosition> GenerateRandomPattern(int count)
        {
            var positions = new List<FormationPosition>();

            for (int i = 0; i < count; i++)
            {
                float x = UnityEngine.Random.Range(-0.9f, 0.9f);
                float y = UnityEngine.Random.Range(-0.9f, 0.9f);
                positions.Add(new FormationPosition(new Vector2(x, y)));
            }

            return positions;
        }

        private List<FormationPosition> NormalizePositions(List<FormationPosition> positions)
        {
            if (positions.Count == 0) return positions;

            float minX = positions.Min(p => p.position.x);
            float maxX = positions.Max(p => p.position.x);
            float minY = positions.Min(p => p.position.y);
            float maxY = positions.Max(p => p.position.y);

            float rangeX = maxX - minX;
            float rangeY = maxY - minY;
            float maxRange = Mathf.Max(rangeX, rangeY);

            if (maxRange < 0.001f) return positions;

            var normalized = new List<FormationPosition>();
            foreach (var pos in positions)
            {
                float x = ((pos.position.x - minX) / maxRange) * 1.8f - 0.9f;
                float y = ((pos.position.y - minY) / maxRange) * 1.8f - 0.9f;
                normalized.Add(new FormationPosition(new Vector2(x, y)));
            }

            return normalized;
        }

        private void ExportAllFormations()
        {
            var manager = CustomFormationManager.Instance;
            if (manager == null)
            {
                EditorUtility.DisplayDialog("Error", "CustomFormationManager not found.", "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanel("Export Formations", "", "formations.json", "json");
            if (string.IsNullOrEmpty(path)) return;

            var formations = manager.GetAllFormations();
            string json = JsonUtility.ToJson(new FormationListWrapper { formations = formations }, true);
            System.IO.File.WriteAllText(path, json);

            Debug.Log($"Exported {formations.Count} formations to {path}");
            EditorUtility.DisplayDialog("Success", $"Exported {formations.Count} formations successfully!", "OK");
        }

        private void ExportSelectedFormations()
        {
            // TODO: Implement selection UI
            ExportAllFormations();
        }

        private void ImportFormationsFromFile()
        {
            string path = EditorUtility.OpenFilePanel("Import Formations", "", "json");
            if (string.IsNullOrEmpty(path)) return;

            var manager = CustomFormationManager.Instance;
            if (manager == null)
            {
                EditorUtility.DisplayDialog("Error", "CustomFormationManager not found.", "OK");
                return;
            }

            try
            {
                string json = System.IO.File.ReadAllText(path);
                var wrapper = JsonUtility.FromJson<FormationListWrapper>(json);

                int imported = 0;
                foreach (var formationData in wrapper.formations)
                {
                    // Create new formation with unique name
                    var formation = manager.CreateFormation(formationData.name);
                    formation.positions = formationData.positions;
                    formation.isInQuickList = formationData.isInQuickList;
                    manager.UpdateFormation(formation);
                    imported++;
                }

                Debug.Log($"Imported {imported} formations from {path}");
                EditorUtility.DisplayDialog("Success", $"Imported {imported} formations successfully!", "OK");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Import Failed", $"Error: {e.Message}", "OK");
                Debug.LogError($"Formation import failed: {e}");
            }
        }

        private void ClearAllFormations()
        {
            var manager = CustomFormationManager.Instance;
            if (manager == null) return;

            var formations = manager.GetAllFormations();
            foreach (var formation in formations.ToList())
            {
                manager.DeleteFormation(formation.id);
            }

            Debug.Log("All custom formations cleared.");
        }

        private void GenerateStandardFormationPack()
        {
            var manager = CustomFormationManager.Instance;
            if (manager == null)
            {
                EditorUtility.DisplayDialog("Error", "CustomFormationManager not found.", "OK");
                return;
            }

            int created = 0;

            foreach (FormationTemplate template in Enum.GetValues(typeof(FormationTemplate)))
            {
                string name = $"Standard {FormatEnumName(template)}";
                var positions = GenerateTemplatePositions(template, 20);

                // Create formation and add positions
                var formation = manager.CreateFormation(name);
                formation.positions = positions;
                manager.UpdateFormation(formation);
                created++;
            }

            Debug.Log($"Generated standard formation pack with {created} formations.");
            EditorUtility.DisplayDialog("Success", $"Created {created} standard formations!", "OK");
        }

        private void VisualizeFormation()
        {
            if (previewFormation == null) return;

            var worldPositions = previewFormation.CalculateWorldPositions(
                previewCenter,
                previewSpacing,
                Vector3.forward
            );

            // Limit to preview count
            if (worldPositions.Count > previewUnitCount)
            {
                worldPositions = worldPositions.GetRange(0, previewUnitCount);
            }

            Debug.Log($"Visualizing {worldPositions.Count} positions in Scene View (check Scene View Gizmos)");

            // Draw in scene view
            SceneView.RepaintAll();
        }

        private void GenerateTestUnits()
        {
            if (previewFormation == null) return;

            var worldPositions = previewFormation.CalculateWorldPositions(
                previewCenter,
                previewSpacing,
                Vector3.forward
            );

            // Limit to preview count
            if (worldPositions.Count > previewUnitCount)
            {
                worldPositions = worldPositions.GetRange(0, previewUnitCount);
            }

            GameObject testParent = new GameObject($"Test Formation - {previewFormation.name}");
            Undo.RegisterCreatedObjectUndo(testParent, "Generate Test Units");

            foreach (var pos in worldPositions)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.position = pos;
                cube.transform.localScale = Vector3.one * 0.5f;
                cube.transform.SetParent(testParent.transform);
                Undo.RegisterCreatedObjectUndo(cube, "Create Test Unit");
            }

            Selection.activeGameObject = testParent;
            SceneView.FrameLastActiveSceneView();

            Debug.Log($"Created {worldPositions.Count} test units for formation '{previewFormation.name}'");
        }

        private void ValidateAllFormations()
        {
            var manager = CustomFormationManager.Instance;
            if (manager == null)
            {
                EditorUtility.DisplayDialog("Error", "CustomFormationManager not found.", "OK");
                return;
            }

            var formations = manager.GetAllFormations();
            int valid = 0;
            int invalid = 0;
            List<string> issues = new List<string>();

            foreach (var formation in formations)
            {
                if (formation.positions.Count == 0)
                {
                    issues.Add($"'{formation.name}' has no positions");
                    invalid++;
                }
                else if (string.IsNullOrEmpty(formation.name))
                {
                    issues.Add($"Formation with ID {formation.id} has no name");
                    invalid++;
                }
                else
                {
                    valid++;
                }
            }

            string message = $"Validation Results:\n\n" +
                           $"✓ Valid formations: {valid}\n" +
                           $"✗ Invalid formations: {invalid}\n\n";

            if (issues.Count > 0)
            {
                message += "Issues found:\n" + string.Join("\n", issues);
            }

            EditorUtility.DisplayDialog("Validation Complete", message, "OK");
            Debug.Log(message);
        }

        private void ValidateFormationSetup()
        {
            List<string> issues = new List<string>();
            List<string> success = new List<string>();

            // Check managers
            if (FindObjectOfType<FormationGroupManager>() != null)
                success.Add("✓ FormationGroupManager found");
            else
                issues.Add("✗ FormationGroupManager missing");

            if (FindObjectOfType<CustomFormationManager>() != null)
                success.Add("✓ CustomFormationManager found");
            else
                issues.Add("✗ CustomFormationManager missing");

            // Check settings
            if (formationSettings != null)
                success.Add("✓ FormationSettingsSO configured");
            else
                issues.Add("✗ FormationSettingsSO not assigned");

            // Check formations
            var manager = CustomFormationManager.Instance;
            if (manager != null)
            {
                int count = manager.GetAllFormations().Count;
                success.Add($"✓ {count} custom formations loaded");
            }

            string message = "Formation System Validation:\n\n";
            message += string.Join("\n", success);
            if (issues.Count > 0)
            {
                message += "\n\n" + string.Join("\n", issues);
            }

            EditorUtility.DisplayDialog("Validation Results", message, "OK");
            Debug.Log(message);
        }

        private void RefreshManagers()
        {
            formationManagersRoot = GameObject.Find("Formation Managers");

            // Try to find existing settings asset
            if (formationSettings == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:FormationSettingsSO");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    formationSettings = AssetDatabase.LoadAssetAtPath<FormationSettingsSO>(path);
                }
            }
        }

        private string FormatEnumName(Enum value)
        {
            return value.ToString().Replace("_", " ");
        }

        private void LoadPreferences()
        {
            gridWidth = EditorPrefs.GetInt("FormationTool_GridWidth", 20);
            gridHeight = EditorPrefs.GetInt("FormationTool_GridHeight", 20);
            previewSpacing = EditorPrefs.GetFloat("FormationTool_PreviewSpacing", 2.5f);
        }

        private void SavePreferences()
        {
            EditorPrefs.SetInt("FormationTool_GridWidth", gridWidth);
            EditorPrefs.SetInt("FormationTool_GridHeight", gridHeight);
            EditorPrefs.SetFloat("FormationTool_PreviewSpacing", previewSpacing);
        }

        #endregion
    }

    [Serializable]
    public class FormationListWrapper
    {
        public List<CustomFormationData> formations;
    }
}
