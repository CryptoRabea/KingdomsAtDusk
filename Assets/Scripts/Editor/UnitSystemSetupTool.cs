using UnityEngine;
using UnityEditor;
using RTS.Units;
using RTS.Units.AI;
using RTS.Units.Components;

namespace RTS.Editor
{
    /// <summary>
    /// Advanced automation tool for setting up complete unit systems.
    /// Creates unit prefabs with all necessary components, AI, and configurations.
    /// Access via: Tools > RTS > Unit System Setup
    /// </summary>
    public class UnitSystemSetupTool : EditorWindow
    {
        private enum SetupMode
        {
            CreateNewUnit,
            ConfigureExisting,
            BatchSetup,
            CreateUnitConfig
        }

        [Header("Setup Configuration")]
        private SetupMode setupMode = SetupMode.CreateNewUnit;

        [Header("Unit Configuration")]
        private string unitName = "NewUnit";
        private GameObject unitModel;
        private UnitConfigSO unitConfig;
        private AISettingsSO aiSettings;
        private GameObject existingUnit;

        [Header("Component Settings")]
        private bool addAI = true;
        private bool addHealth = true;
        private bool addMovement = true;
        private bool addCombat = true;
        private bool addAnimation = true;
        private bool addSelection = true;

        [Header("AI Configuration")]
        private AIBehaviorType behaviorType = AIBehaviorType.Aggressive;

        [Header("Stats")]
        private float maxHealth = 100f;
        private float speed = 3.5f;
        private float attackRange = 2f;
        private float attackDamage = 10f;
        private float attackRate = 1f;
        private float detectionRange = 10f;
        private bool canRetreat = true;
        private float retreatThreshold = 20f;

        [Header("Batch Setup")]
        private GameObject[] unitsToSetup;

        private Vector2 scrollPos;

        [MenuItem("Tools/RTS/Unit System Setup")]
        public static void ShowWindow()
        {
            UnitSystemSetupTool window = GetWindow<UnitSystemSetupTool>("Unit System Setup");
            window.minSize = new Vector2(450, 700);
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            GUILayout.Label("Unit System Setup Tool", EditorStyles.boldLabel);
            GUILayout.Space(10);

            DrawModeSelection();
            GUILayout.Space(10);

            switch (setupMode)
            {
                case SetupMode.CreateNewUnit:
                    DrawCreateNewUnitMode();
                    break;
                case SetupMode.ConfigureExisting:
                    DrawConfigureExistingMode();
                    break;
                case SetupMode.BatchSetup:
                    DrawBatchSetupMode();
                    break;
                case SetupMode.CreateUnitConfig:
                    DrawCreateConfigMode();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        #region Mode Selection

        private void DrawModeSelection()
        {
            EditorGUILayout.HelpBox(
                "Unit System Setup Tool - Four Modes:\n" +
                "• Create New Unit: Build complete unit prefab from scratch\n" +
                "• Configure Existing: Add/update components on existing unit\n" +
                "• Batch Setup: Configure multiple units at once\n" +
                "• Create Unit Config: Generate UnitConfigSO asset",
                MessageType.Info);

            setupMode = (SetupMode)EditorGUILayout.EnumPopup("Setup Mode", setupMode);
        }

        #endregion

        #region Create New Unit Mode

        private void DrawCreateNewUnitMode()
        {
            GUILayout.Label("Create New Unit", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Creates a complete unit prefab with all components configured.\n" +
                "Perfect for rapid unit creation!",
                MessageType.Info);

            GUILayout.Space(10);

            // Basic Info
            GUILayout.Label("Basic Information", EditorStyles.boldLabel);
            unitName = EditorGUILayout.TextField("Unit Name", unitName);
            unitModel = (GameObject)EditorGUILayout.ObjectField("Unit Model (Optional)", unitModel, typeof(GameObject), false);

            GUILayout.Space(10);

            // Component Toggles
            GUILayout.Label("Components to Add", EditorStyles.boldLabel);
            addAI = EditorGUILayout.Toggle("AI Controller", addAI);
            addHealth = EditorGUILayout.Toggle("Health System", addHealth);
            addMovement = EditorGUILayout.Toggle("Movement", addMovement);
            addCombat = EditorGUILayout.Toggle("Combat System", addCombat);
            addAnimation = EditorGUILayout.Toggle("Animation Controller", addAnimation);
            addSelection = EditorGUILayout.Toggle("Selection System", addSelection);

            GUILayout.Space(10);

            // Stats Configuration
            GUILayout.Label("Unit Stats", EditorStyles.boldLabel);
            maxHealth = EditorGUILayout.FloatField("Max Health", maxHealth);
            speed = EditorGUILayout.FloatField("Movement Speed", speed);
            attackRange = EditorGUILayout.FloatField("Attack Range", attackRange);
            attackDamage = EditorGUILayout.FloatField("Attack Damage", attackDamage);
            attackRate = EditorGUILayout.FloatField("Attack Rate", attackRate);
            detectionRange = EditorGUILayout.FloatField("Detection Range", detectionRange);

            GUILayout.Space(5);
            canRetreat = EditorGUILayout.Toggle("Can Retreat", canRetreat);
            if (canRetreat)
            {
                retreatThreshold = EditorGUILayout.Slider("Retreat Threshold %", retreatThreshold, 0f, 100f);
            }

            GUILayout.Space(10);

            // AI Settings
            GUILayout.Label("AI Configuration", EditorStyles.boldLabel);
            behaviorType = (AIBehaviorType)EditorGUILayout.EnumPopup("Behavior Type", behaviorType);
            aiSettings = (AISettingsSO)EditorGUILayout.ObjectField("AI Settings", aiSettings, typeof(AISettingsSO), false);

            if (aiSettings == null)
            {
                EditorGUILayout.HelpBox("AI Settings not assigned. A default one will be created if needed.", MessageType.Warning);
            }

            GUILayout.Space(20);

            // Create Button
            GUI.enabled = !string.IsNullOrEmpty(unitName);
            if (GUILayout.Button("Create Complete Unit", GUILayout.Height(40)))
            {
                CreateCompleteUnit();
            }
            GUI.enabled = true;

            GUILayout.Space(10);

            // Quick Actions
            GUILayout.Label("Quick Actions", EditorStyles.boldLabel);
            if (GUILayout.Button("Create Unit Config First", GUILayout.Height(30)))
            {
                unitConfig = CreateUnitConfig();
                if (unitConfig != null)
                {
                    EditorGUIUtility.PingObject(unitConfig);
                    Debug.Log($"✅ Created UnitConfig: {AssetDatabase.GetAssetPath(unitConfig)}");
                }
            }
        }

        #endregion

        #region Configure Existing Mode

        private void DrawConfigureExistingMode()
        {
            GUILayout.Label("Configure Existing Unit", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Add or update components on an existing unit prefab.\n" +
                "Missing components will be added automatically.",
                MessageType.Info);

            GUILayout.Space(10);

            existingUnit = (GameObject)EditorGUILayout.ObjectField("Existing Unit", existingUnit, typeof(GameObject), true);

            if (existingUnit == null)
            {
                EditorGUILayout.HelpBox("Please assign an existing unit prefab or scene object.", MessageType.Warning);
                return;
            }

            GUILayout.Space(10);

            // Show current components
            GUILayout.Label("Current Components", EditorStyles.boldLabel);
            DrawComponentStatus(existingUnit);

            GUILayout.Space(10);

            // Configuration
            unitConfig = (UnitConfigSO)EditorGUILayout.ObjectField("Unit Config", unitConfig, typeof(UnitConfigSO), false);
            aiSettings = (AISettingsSO)EditorGUILayout.ObjectField("AI Settings", aiSettings, typeof(AISettingsSO), false);

            GUILayout.Space(10);

            // Component Toggles
            GUILayout.Label("Components to Add/Update", EditorStyles.boldLabel);
            addAI = EditorGUILayout.Toggle("AI Controller", addAI);
            addHealth = EditorGUILayout.Toggle("Health System", addHealth);
            addMovement = EditorGUILayout.Toggle("Movement", addMovement);
            addCombat = EditorGUILayout.Toggle("Combat System", addCombat);
            addAnimation = EditorGUILayout.Toggle("Animation Controller", addAnimation);
            addSelection = EditorGUILayout.Toggle("Selection System", addSelection);

            GUILayout.Space(20);

            if (GUILayout.Button("Configure Unit", GUILayout.Height(40)))
            {
                ConfigureExistingUnit(existingUnit);
            }
        }

        #endregion

        #region Batch Setup Mode

        private void DrawBatchSetupMode()
        {
            GUILayout.Label("Batch Unit Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Configure multiple units at once.\n" +
                "Great for updating existing units in bulk!",
                MessageType.Info);

            GUILayout.Space(10);

            // Draw array field for units
            ScriptableObject target = this;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty unitsProperty = so.FindProperty("unitsToSetup");
            EditorGUILayout.PropertyField(unitsProperty, true);
            so.ApplyModifiedProperties();

            GUILayout.Space(10);

            unitConfig = (UnitConfigSO)EditorGUILayout.ObjectField("Unit Config (Optional)", unitConfig, typeof(UnitConfigSO), false);
            aiSettings = (AISettingsSO)EditorGUILayout.ObjectField("AI Settings (Optional)", aiSettings, typeof(AISettingsSO), false);

            GUILayout.Space(10);

            // Component Toggles
            GUILayout.Label("Components to Add/Update", EditorStyles.boldLabel);
            addAI = EditorGUILayout.Toggle("AI Controller", addAI);
            addHealth = EditorGUILayout.Toggle("Health System", addHealth);
            addMovement = EditorGUILayout.Toggle("Movement", addMovement);
            addCombat = EditorGUILayout.Toggle("Combat System", addCombat);
            addAnimation = EditorGUILayout.Toggle("Animation Controller", addAnimation);
            addSelection = EditorGUILayout.Toggle("Selection System", addSelection);

            GUILayout.Space(20);

            GUI.enabled = unitsToSetup != null && unitsToSetup.Length > 0;
            if (GUILayout.Button($"Configure {(unitsToSetup?.Length ?? 0)} Units", GUILayout.Height(40)))
            {
                BatchConfigureUnits();
            }
            GUI.enabled = true;
        }

        #endregion

        #region Create Config Mode

        private void DrawCreateConfigMode()
        {
            GUILayout.Label("Create Unit Config", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Creates a UnitConfigSO asset with the specified stats.\n" +
                "This asset can be reused across multiple units.",
                MessageType.Info);

            GUILayout.Space(10);

            unitName = EditorGUILayout.TextField("Config Name", unitName);

            GUILayout.Space(10);
            GUILayout.Label("Unit Stats", EditorStyles.boldLabel);

            maxHealth = EditorGUILayout.FloatField("Max Health", maxHealth);
            speed = EditorGUILayout.FloatField("Movement Speed", speed);
            attackRange = EditorGUILayout.FloatField("Attack Range", attackRange);
            attackDamage = EditorGUILayout.FloatField("Attack Damage", attackDamage);
            attackRate = EditorGUILayout.FloatField("Attack Rate", attackRate);
            detectionRange = EditorGUILayout.FloatField("Detection Range", detectionRange);

            GUILayout.Space(5);
            canRetreat = EditorGUILayout.Toggle("Can Retreat", canRetreat);
            if (canRetreat)
            {
                retreatThreshold = EditorGUILayout.Slider("Retreat Threshold %", retreatThreshold, 0f, 100f);
            }

            GUILayout.Space(20);

            GUI.enabled = !string.IsNullOrEmpty(unitName);
            if (GUILayout.Button("Create Unit Config", GUILayout.Height(40)))
            {
                UnitConfigSO config = CreateUnitConfig();
                if (config != null)
                {
                    EditorGUIUtility.PingObject(config);
                    Selection.activeObject = config;
                    Debug.Log($"✅ Created UnitConfig at: {AssetDatabase.GetAssetPath(config)}");
                }
            }
            GUI.enabled = true;
        }

        #endregion

        #region Implementation Methods

        private void CreateCompleteUnit()
        {
            // Create root GameObject
            GameObject unitObj = unitModel != null ? Instantiate(unitModel) : new GameObject(unitName);
            unitObj.name = unitName;

            // Create UnitConfig if not assigned
            if (unitConfig == null)
            {
                unitConfig = CreateUnitConfig();
            }

            // Add components
            ConfigureUnitComponents(unitObj);

            // Save as prefab
            string prefabPath = $"Assets/Prefabs/Units/{unitName}.prefab";
            string directory = System.IO.Path.GetDirectoryName(prefabPath);

            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(unitObj, prefabPath);
            DestroyImmediate(unitObj);

            EditorUtility.SetDirty(prefab);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);

            Debug.Log($"✅ Complete unit created at: {prefabPath}");
            Debug.Log($"   - Config: {AssetDatabase.GetAssetPath(unitConfig)}");

            EditorUtility.DisplayDialog("Success!",
                $"Unit '{unitName}' created successfully!\n\n" +
                $"Prefab: {prefabPath}\n" +
                $"Config: {AssetDatabase.GetAssetPath(unitConfig)}\n\n" +
                $"Components added: {GetComponentSummary()}",
                "OK");
        }

        private void ConfigureExistingUnit(GameObject unit)
        {
            if (unit == null) return;

            ConfigureUnitComponents(unit);

            EditorUtility.SetDirty(unit);
            Debug.Log($"✅ Unit '{unit.name}' configured successfully!");

            EditorUtility.DisplayDialog("Success!",
                $"Unit '{unit.name}' configured!\n\n" +
                $"Components: {GetComponentSummary()}",
                "OK");
        }

        private void BatchConfigureUnits()
        {
            if (unitsToSetup == null || unitsToSetup.Length == 0) return;

            int configured = 0;
            foreach (GameObject unit in unitsToSetup)
            {
                if (unit == null) continue;

                ConfigureUnitComponents(unit);
                EditorUtility.SetDirty(unit);
                configured++;
            }

            Debug.Log($"✅ Batch configured {configured} units successfully!");
            EditorUtility.DisplayDialog("Success!",
                $"Configured {configured} units!\n\n" +
                $"Components: {GetComponentSummary()}",
                "OK");
        }

        private void ConfigureUnitComponents(GameObject unit)
        {
            // Add Health
            if (addHealth)
            {
                UnitHealth health = unit.GetComponent<UnitHealth>();
                if (health == null) health = unit.AddComponent<UnitHealth>();
            }

            // Add Movement
            if (addMovement)
            {
                UnitMovement movement = unit.GetComponent<UnitMovement>();
                if (movement == null) movement = unit.AddComponent<UnitMovement>();
            }

            // Add Combat
            if (addCombat)
            {
                UnitCombat combat = unit.GetComponent<UnitCombat>();
                if (combat == null) combat = unit.AddComponent<UnitCombat>();
            }

            // Add Selection
            if (addSelection)
            {
                UnitSelectable selectable = unit.GetComponent<UnitSelectable>();
                if (selectable == null) selectable = unit.AddComponent<UnitSelectable>();
            }

            // Add AI Controller (must be last as it requires other components)
            if (addAI)
            {
                UnitAIController ai = unit.GetComponent<UnitAIController>();
                if (ai == null) ai = unit.AddComponent<UnitAIController>();

                // Assign config via SerializedObject
                SerializedObject so = new SerializedObject(ai);
                if (unitConfig != null)
                {
                    so.FindProperty("config").objectReferenceValue = unitConfig;
                }
                if (aiSettings != null)
                {
                    so.FindProperty("aiSettings").objectReferenceValue = aiSettings;
                }
                so.FindProperty("behaviorType").enumValueIndex = (int)behaviorType;
                so.ApplyModifiedProperties();
            }

            // Add Animation (optional)
            if (addAnimation)
            {
                // Check if RTSAnimation package is available
                System.Type animControllerType = System.Type.GetType("RTSAnimation.UnitAnimationController, Assembly-CSharp");
                if (animControllerType != null)
                {
                    var animController = unit.GetComponent(animControllerType);
                    if (animController == null)
                    {
                        unit.AddComponent(animControllerType);
                        Debug.Log("   - Added UnitAnimationController");
                    }
                }
            }

            Debug.Log($"✅ Configured unit: {unit.name}");
        }

        private UnitConfigSO CreateUnitConfig()
        {
            UnitConfigSO config = CreateInstance<UnitConfigSO>();

            config.unitName = unitName;
            config.maxHealth = maxHealth;
            config.speed = speed;
            config.attackRange = attackRange;
            config.attackDamage = attackDamage;
            config.attackRate = attackRate;
            config.detectionRange = detectionRange;
            config.canRetreat = canRetreat;
            config.retreatThreshold = retreatThreshold;

            string configPath = $"Assets/ScriptableObjects/Units/{unitName}Config.asset";
            string directory = System.IO.Path.GetDirectoryName(configPath);

            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            AssetDatabase.CreateAsset(config, configPath);
            AssetDatabase.SaveAssets();

            return config;
        }

        #endregion

        #region Helper Methods

        private void DrawComponentStatus(GameObject unit)
        {
            EditorGUI.indentLevel++;

            DrawComponentStatusLine("✓ AI Controller", unit.GetComponent<UnitAIController>() != null);
            DrawComponentStatusLine("✓ Health System", unit.GetComponent<UnitHealth>() != null);
            DrawComponentStatusLine("✓ Movement", unit.GetComponent<UnitMovement>() != null);
            DrawComponentStatusLine("✓ Combat", unit.GetComponent<UnitCombat>() != null);
            DrawComponentStatusLine("✓ Selection", unit.GetComponent<UnitSelectable>() != null);

            System.Type animType = System.Type.GetType("RTSAnimation.UnitAnimationController, Assembly-CSharp");
            bool hasAnim = animType != null && unit.GetComponent(animType) != null;
            DrawComponentStatusLine("✓ Animation", hasAnim);

            EditorGUI.indentLevel--;
        }

        private void DrawComponentStatusLine(string label, bool hasComponent)
        {
            GUIStyle style = new GUIStyle(EditorStyles.label);
            style.normal.textColor = hasComponent ? Color.green : Color.gray;
            EditorGUILayout.LabelField(label, hasComponent ? "Present" : "Missing", style);
        }

        private string GetComponentSummary()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (addAI) sb.Append("AI, ");
            if (addHealth) sb.Append("Health, ");
            if (addMovement) sb.Append("Movement, ");
            if (addCombat) sb.Append("Combat, ");
            if (addAnimation) sb.Append("Animation, ");
            if (addSelection) sb.Append("Selection");

            return sb.ToString().TrimEnd(',', ' ');
        }

        #endregion
    }
}
