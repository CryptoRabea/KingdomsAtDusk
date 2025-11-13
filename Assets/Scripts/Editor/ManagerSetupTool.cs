using UnityEngine;
using UnityEditor;
using RTS.Managers;
using RTS.Core.Pooling;
using RTS.Core.Services;
using UnityEditor.SceneManagement;

namespace RTS.Editor
{
    /// <summary>
    /// Advanced automation tool for setting up game managers and service architecture.
    /// Creates complete manager hierarchy with proper service registration.
    /// Access via: Tools > RTS > Manager Setup
    /// </summary>
    public class ManagerSetupTool : EditorWindow
    {
        private enum SetupMode
        {
            CompleteSetup,
            IndividualManager,
            ValidateExisting
        }

        private SetupMode setupMode = SetupMode.CompleteSetup;

        [Header("Manager Selection")]
        private bool createGameManager = true;
        private bool createResourceManager = true;
        private bool createHappinessManager = true;
        private bool createBuildingManager = true;
        private bool createWaveManager = true;
        private bool createObjectPool = true;

        [Header("Existing References")]
        private GameManager existingGameManager;
        private ResourceManager existingResourceManager;
        private HappinessManager existingHappinessManager;
        private BuildingManager existingBuildingManager;
        private WaveManager existingWaveManager;

        [Header("Configuration")]
        private bool initializeOnAwake = true;
        private bool useDontDestroyOnLoad = true;

        private Vector2 scrollPos;

        [MenuItem("Tools/RTS/Manager Setup")]
        public static void ShowWindow()
        {
            ManagerSetupTool window = GetWindow<ManagerSetupTool>("Manager Setup");
            window.minSize = new Vector2(450, 650);
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            GUILayout.Label("Manager Setup Tool", EditorStyles.boldLabel);
            GUILayout.Space(10);

            DrawModeSelection();
            GUILayout.Space(10);

            switch (setupMode)
            {
                case SetupMode.CompleteSetup:
                    DrawCompleteSetupMode();
                    break;
                case SetupMode.IndividualManager:
                    DrawIndividualManagerMode();
                    break;
                case SetupMode.ValidateExisting:
                    DrawValidateExistingMode();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        #region Mode Selection

        private void DrawModeSelection()
        {
            EditorGUILayout.HelpBox(
                "Manager Setup Tool - Three Modes:\n" +
                "• Complete Setup: Creates full manager hierarchy from scratch\n" +
                "• Individual Manager: Create/update specific managers\n" +
                "• Validate Existing: Check and fix existing manager setup",
                MessageType.Info);

            setupMode = (SetupMode)EditorGUILayout.EnumPopup("Setup Mode", setupMode);
        }

        #endregion

        #region Complete Setup Mode

        private void DrawCompleteSetupMode()
        {
            GUILayout.Label("Complete Manager Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Creates a complete game manager hierarchy with all services.\n" +
                "Perfect for new scenes or projects!",
                MessageType.Info);

            GUILayout.Space(10);

            // Configuration
            GUILayout.Label("Configuration", EditorStyles.boldLabel);
            initializeOnAwake = EditorGUILayout.Toggle("Initialize On Awake", initializeOnAwake);
            useDontDestroyOnLoad = EditorGUILayout.Toggle("Use DontDestroyOnLoad", useDontDestroyOnLoad);

            GUILayout.Space(10);

            // Manager Selection
            GUILayout.Label("Managers to Create", EditorStyles.boldLabel);
            createGameManager = EditorGUILayout.Toggle("Game Manager (Root)", createGameManager);

            EditorGUI.indentLevel++;
            GUI.enabled = createGameManager;
            createResourceManager = EditorGUILayout.Toggle("Resource Manager", createResourceManager);
            createHappinessManager = EditorGUILayout.Toggle("Happiness Manager", createHappinessManager);
            createBuildingManager = EditorGUILayout.Toggle("Building Manager", createBuildingManager);
            createWaveManager = EditorGUILayout.Toggle("Wave Manager", createWaveManager);
            createObjectPool = EditorGUILayout.Toggle("Object Pool", createObjectPool);
            GUI.enabled = true;
            EditorGUI.indentLevel--;

            GUILayout.Space(20);

            // Create Button
            if (GUILayout.Button("Create Complete Manager Hierarchy", GUILayout.Height(40)))
            {
                CreateCompleteHierarchy();
            }

            GUILayout.Space(10);

            // Preview
            DrawHierarchyPreview();
        }

        #endregion

        #region Individual Manager Mode

        private void DrawIndividualManagerMode()
        {
            GUILayout.Label("Individual Manager Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Create or update individual managers.\n" +
                "Great for adding managers to existing setups!",
                MessageType.Info);

            GUILayout.Space(10);

            GUILayout.Label("Game Manager", EditorStyles.boldLabel);
            existingGameManager = (GameManager)EditorGUILayout.ObjectField(
                "Game Manager",
                existingGameManager,
                typeof(GameManager),
                true);

            if (GUILayout.Button("Create/Update Game Manager", GUILayout.Height(30)))
            {
                CreateOrUpdateGameManager();
            }

            GUILayout.Space(10);

            GUILayout.Label("Resource Manager", EditorStyles.boldLabel);
            existingResourceManager = (ResourceManager)EditorGUILayout.ObjectField(
                "Resource Manager",
                existingResourceManager,
                typeof(ResourceManager),
                true);

            if (GUILayout.Button("Create/Update Resource Manager", GUILayout.Height(30)))
            {
                CreateOrUpdateResourceManager();
            }

            GUILayout.Space(10);

            GUILayout.Label("Happiness Manager", EditorStyles.boldLabel);
            existingHappinessManager = (HappinessManager)EditorGUILayout.ObjectField(
                "Happiness Manager",
                existingHappinessManager,
                typeof(HappinessManager),
                true);

            if (GUILayout.Button("Create/Update Happiness Manager", GUILayout.Height(30)))
            {
                CreateOrUpdateHappinessManager();
            }

            GUILayout.Space(10);

            GUILayout.Label("Building Manager", EditorStyles.boldLabel);
            existingBuildingManager = (BuildingManager)EditorGUILayout.ObjectField(
                "Building Manager",
                existingBuildingManager,
                typeof(BuildingManager),
                true);

            if (GUILayout.Button("Create/Update Building Manager", GUILayout.Height(30)))
            {
                CreateOrUpdateBuildingManager();
            }

            GUILayout.Space(10);

            GUILayout.Label("Wave Manager", EditorStyles.boldLabel);
            existingWaveManager = (WaveManager)EditorGUILayout.ObjectField(
                "Wave Manager",
                existingWaveManager,
                typeof(WaveManager),
                true);

            if (GUILayout.Button("Create/Update Wave Manager", GUILayout.Height(30)))
            {
                CreateOrUpdateWaveManager();
            }
        }

        #endregion

        #region Validate Existing Mode

        private void DrawValidateExistingMode()
        {
            GUILayout.Label("Validate Existing Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Validates your current manager setup and identifies issues.",
                MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("Scan Current Scene", GUILayout.Height(40)))
            {
                ScanAndValidate();
            }

            GUILayout.Space(10);

            // Display results
            DrawValidationResults();
        }

        #endregion

        #region Implementation Methods

        private void CreateCompleteHierarchy()
        {
            // Check if GameManager already exists
            GameManager existing = FindAnyObjectByType<GameManager>();
            if (existing != null)
            {
                bool replace = EditorUtility.DisplayDialog(
                    "GameManager Already Exists",
                    "A GameManager already exists in the scene. Replace it?",
                    "Replace",
                    "Cancel");

                if (!replace) return;
                DestroyImmediate(existing.gameObject);
            }

            // Create root GameManager object
            GameObject root = new GameObject("GameManager");
            GameManager gameManager = root.AddComponent<GameManager>();

            // Configure GameManager
            SerializedObject gmSO = new SerializedObject(gameManager);
            gmSO.FindProperty("initializeOnAwake").boolValue = initializeOnAwake;
            gmSO.ApplyModifiedProperties();

            if (useDontDestroyOnLoad)
            {
                // Mark for DontDestroyOnLoad (will happen in Awake)
                Debug.Log("GameManager will use DontDestroyOnLoad");
            }

            // Create child managers
            if (createResourceManager)
            {
                GameObject rmObj = new GameObject("ResourceManager");
                rmObj.transform.SetParent(root.transform);
                ResourceManager rm = rmObj.AddComponent<ResourceManager>();

                // Assign to GameManager
                gmSO.FindProperty("resourceManager").objectReferenceValue = rm;
                Debug.Log("✅ Created ResourceManager");
            }

            if (createHappinessManager)
            {
                GameObject hmObj = new GameObject("HappinessManager");
                hmObj.transform.SetParent(root.transform);
                HappinessManager hm = hmObj.AddComponent<HappinessManager>();

                // Assign to GameManager
                gmSO.FindProperty("happinessManager").objectReferenceValue = hm;
                Debug.Log("✅ Created HappinessManager");
            }

            if (createBuildingManager)
            {
                GameObject bmObj = new GameObject("BuildingManager");
                bmObj.transform.SetParent(root.transform);
                BuildingManager bm = bmObj.AddComponent<BuildingManager>();

                // Assign to GameManager
                gmSO.FindProperty("buildingManager").objectReferenceValue = bm;
                Debug.Log("✅ Created BuildingManager");
            }

            if (createWaveManager)
            {
                GameObject wmObj = new GameObject("WaveManager");
                wmObj.transform.SetParent(root.transform);
                WaveManager wm = wmObj.AddComponent<WaveManager>();
                Debug.Log("✅ Created WaveManager");
            }

            if (createObjectPool)
            {
                GameObject poolObj = new GameObject("ObjectPool");
                poolObj.transform.SetParent(root.transform);
                ObjectPool pool = poolObj.AddComponent<ObjectPool>();

                // Assign to GameManager
                gmSO.FindProperty("objectPool").objectReferenceValue = pool;
                Debug.Log("✅ Created ObjectPool");
            }

            gmSO.ApplyModifiedProperties();

            // Mark scene as dirty
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);

            Debug.Log("✅✅✅ Complete manager hierarchy created successfully!");

            EditorUtility.DisplayDialog("Success!",
                "Manager hierarchy created successfully!\n\n" +
                GetCreationSummary(),
                "OK");
        }

        private void CreateOrUpdateGameManager()
        {
            if (existingGameManager == null)
            {
                GameObject root = new GameObject("GameManager");
                existingGameManager = root.AddComponent<GameManager>();
                Selection.activeGameObject = root;
                Debug.Log("✅ Created new GameManager");
            }
            else
            {
                Debug.Log("✅ GameManager already exists");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorUtility.SetDirty(existingGameManager);
        }

        private void CreateOrUpdateResourceManager()
        {
            if (existingResourceManager == null)
            {
                GameObject obj = new GameObject("ResourceManager");
                existingResourceManager = obj.AddComponent<ResourceManager>();

                // Try to parent to GameManager
                GameManager gm = FindAnyObjectByType<GameManager>();
                if (gm != null)
                {
                    obj.transform.SetParent(gm.transform);
                    SerializedObject so = new SerializedObject(gm);
                    so.FindProperty("resourceManager").objectReferenceValue = existingResourceManager;
                    so.ApplyModifiedProperties();
                }

                Selection.activeGameObject = obj;
                Debug.Log("✅ Created new ResourceManager");
            }
            else
            {
                Debug.Log("✅ ResourceManager already exists");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorUtility.SetDirty(existingResourceManager);
        }

        private void CreateOrUpdateHappinessManager()
        {
            if (existingHappinessManager == null)
            {
                GameObject obj = new GameObject("HappinessManager");
                existingHappinessManager = obj.AddComponent<HappinessManager>();

                // Try to parent to GameManager
                GameManager gm = FindAnyObjectByType<GameManager>();
                if (gm != null)
                {
                    obj.transform.SetParent(gm.transform);
                    SerializedObject so = new SerializedObject(gm);
                    so.FindProperty("happinessManager").objectReferenceValue = existingHappinessManager;
                    so.ApplyModifiedProperties();
                }

                Selection.activeGameObject = obj;
                Debug.Log("✅ Created new HappinessManager");
            }
            else
            {
                Debug.Log("✅ HappinessManager already exists");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorUtility.SetDirty(existingHappinessManager);
        }

        private void CreateOrUpdateBuildingManager()
        {
            if (existingBuildingManager == null)
            {
                GameObject obj = new GameObject("BuildingManager");
                existingBuildingManager = obj.AddComponent<BuildingManager>();

                // Try to parent to GameManager
                GameManager gm = FindAnyObjectByType<GameManager>();
                if (gm != null)
                {
                    obj.transform.SetParent(gm.transform);
                    SerializedObject so = new SerializedObject(gm);
                    so.FindProperty("buildingManager").objectReferenceValue = existingBuildingManager;
                    so.ApplyModifiedProperties();
                }

                Selection.activeGameObject = obj;
                Debug.Log("✅ Created new BuildingManager");
            }
            else
            {
                Debug.Log("✅ BuildingManager already exists");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorUtility.SetDirty(existingBuildingManager);
        }

        private void CreateOrUpdateWaveManager()
        {
            if (existingWaveManager == null)
            {
                GameObject obj = new GameObject("WaveManager");
                existingWaveManager = obj.AddComponent<WaveManager>();

                // Try to parent to GameManager
                GameManager gm = FindAnyObjectByType<GameManager>();
                if (gm != null)
                {
                    obj.transform.SetParent(gm.transform);
                }

                Selection.activeGameObject = obj;
                Debug.Log("✅ Created new WaveManager");
            }
            else
            {
                Debug.Log("✅ WaveManager already exists");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorUtility.SetDirty(existingWaveManager);
        }

        private void ScanAndValidate()
        {
            Debug.Log("=== Scanning Scene for Managers ===");

            existingGameManager = FindAnyObjectByType<GameManager>();
            existingResourceManager = FindAnyObjectByType<ResourceManager>();
            existingHappinessManager = FindAnyObjectByType<HappinessManager>();
            existingBuildingManager = FindAnyObjectByType<BuildingManager>();
            existingWaveManager = FindAnyObjectByType<WaveManager>();

            Repaint();
        }

        #endregion

        #region Helper Methods

        private void DrawHierarchyPreview()
        {
            if (!createGameManager) return;

            GUILayout.Label("Hierarchy Preview", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("└─ GameManager (Root)");
            EditorGUI.indentLevel++;

            if (createResourceManager)
                EditorGUILayout.LabelField("├─ ResourceManager");
            if (createHappinessManager)
                EditorGUILayout.LabelField("├─ HappinessManager");
            if (createBuildingManager)
                EditorGUILayout.LabelField("├─ BuildingManager");
            if (createWaveManager)
                EditorGUILayout.LabelField("├─ WaveManager");
            if (createObjectPool)
                EditorGUILayout.LabelField("└─ ObjectPool");

            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;
        }

        private void DrawValidationResults()
        {
            GUILayout.Label("Validation Results", EditorStyles.boldLabel);

            DrawValidationLine("Game Manager", existingGameManager != null);
            DrawValidationLine("Resource Manager", existingResourceManager != null);
            DrawValidationLine("Happiness Manager", existingHappinessManager != null);
            DrawValidationLine("Building Manager", existingBuildingManager != null);
            DrawValidationLine("Wave Manager", existingWaveManager != null);

            ObjectPool pool = FindAnyObjectByType<ObjectPool>();
            DrawValidationLine("Object Pool", pool != null);
        }

        private void DrawValidationLine(string label, bool isValid)
        {
            GUIStyle style = new GUIStyle(EditorStyles.label);
            style.normal.textColor = isValid ? Color.green : Color.yellow;

            string status = isValid ? "✓ Found" : "⚠ Missing";
            EditorGUILayout.LabelField(label, status, style);
        }

        private string GetCreationSummary()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            if (createResourceManager) sb.AppendLine("✓ ResourceManager");
            if (createHappinessManager) sb.AppendLine("✓ HappinessManager");
            if (createBuildingManager) sb.AppendLine("✓ BuildingManager");
            if (createWaveManager) sb.AppendLine("✓ WaveManager");
            if (createObjectPool) sb.AppendLine("✓ ObjectPool");

            return sb.ToString();
        }

        #endregion
    }
}
