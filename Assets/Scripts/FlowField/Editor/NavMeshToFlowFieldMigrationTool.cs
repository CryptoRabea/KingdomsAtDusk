#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using FlowField.Core;
using FlowField.Movement;
using FlowField.Obstacles;
using RTS.Units;
using RTS.Buildings;
using Unity.AI.Navigation;

namespace FlowField.Editor
{
    /// <summary>
    /// Comprehensive migration tool to convert NavMesh-based movement to FlowField
    /// - Converts all units from NavMeshAgent to FlowFieldFollower
    /// - Replaces NavMeshObstacle with FlowFieldObstacle
    /// - Removes NavMeshSurface components
    /// - Cleans up all NavMesh-related components from scenes and prefabs
    /// </summary>
    public class NavMeshToFlowFieldMigrationTool : EditorWindow
    {
        private Vector2 scrollPosition;
        private bool scanComplete = false;

        // Scan results
        private List<GameObject> unitsWithNavMesh = new List<GameObject>();
        private List<GameObject> buildingsWithNavMeshObstacle = new List<GameObject>();
        private List<GameObject> wallsWithNavMeshObstacle = new List<GameObject>();
        private List<NavMeshSurface> navMeshSurfaces = new List<NavMeshSurface>();
        private List<string> prefabsToUpdate = new List<string>();

        // Migration settings
        private bool convertUnits = true;
        private bool convertBuildings = true;
        private bool convertWalls = true;
        private bool removeNavMeshSurfaces = true;
        private bool updatePrefabs = true;
        private bool createFlowFieldManager = true;
        private bool removeNavMeshComponents = true;

        // FlowField settings
        private float flowFieldCellSize = 1f;
        private bool autoDetectGridBounds = true;
        private Vector3 manualGridOrigin = Vector3.zero;
        private float manualGridWidth = 100f;
        private float manualGridHeight = 100f;

        [MenuItem("Tools/FlowField/NavMesh to FlowField Migration Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<NavMeshToFlowFieldMigrationTool>("NavMesh → FlowField Migration");
            window.minSize = new Vector2(600, 500);
            window.Show();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            DrawScanSection();

            if (scanComplete)
            {
                DrawScanResults();
                DrawMigrationSettings();
                DrawFlowFieldSettings();
                DrawMigrationButtons();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("NavMesh to FlowField Migration Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This tool will migrate your project from NavMesh to FlowField pathfinding.\n" +
                "1. Scan the project to find all NavMesh components\n" +
                "2. Review what will be migrated\n" +
                "3. Click 'Migrate Project' to perform the conversion\n\n" +
                "⚠️ Create a backup before proceeding!",
                MessageType.Info
            );
            EditorGUILayout.Space(10);
        }

        private void DrawScanSection()
        {
            EditorGUILayout.LabelField("Step 1: Scan Project", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (GUILayout.Button("Scan Project for NavMesh Components", GUILayout.Height(30)))
            {
                ScanProject();
            }

            EditorGUILayout.Space(10);
        }

        private void DrawScanResults()
        {
            EditorGUILayout.LabelField("Step 2: Scan Results", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField($"Units with NavMeshAgent: {unitsWithNavMesh.Count}", EditorStyles.boldLabel);
            if (unitsWithNavMesh.Count > 0)
            {
                EditorGUI.indentLevel++;
                foreach (var unit in unitsWithNavMesh.Take(5))
                {
                    EditorGUILayout.ObjectField(unit.name, unit, typeof(GameObject), true);
                }
                if (unitsWithNavMesh.Count > 5)
                {
                    EditorGUILayout.LabelField($"... and {unitsWithNavMesh.Count - 5} more");
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField($"Buildings with NavMeshObstacle: {buildingsWithNavMeshObstacle.Count}", EditorStyles.boldLabel);
            if (buildingsWithNavMeshObstacle.Count > 0)
            {
                EditorGUI.indentLevel++;
                foreach (var building in buildingsWithNavMeshObstacle.Take(5))
                {
                    EditorGUILayout.ObjectField(building.name, building, typeof(GameObject), true);
                }
                if (buildingsWithNavMeshObstacle.Count > 5)
                {
                    EditorGUILayout.LabelField($"... and {buildingsWithNavMeshObstacle.Count - 5} more");
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField($"Walls with NavMeshObstacle: {wallsWithNavMeshObstacle.Count}", EditorStyles.boldLabel);
            if (wallsWithNavMeshObstacle.Count > 0)
            {
                EditorGUI.indentLevel++;
                foreach (var wall in wallsWithNavMeshObstacle.Take(5))
                {
                    EditorGUILayout.ObjectField(wall.name, wall, typeof(GameObject), true);
                }
                if (wallsWithNavMeshObstacle.Count > 5)
                {
                    EditorGUILayout.LabelField($"... and {wallsWithNavMeshObstacle.Count - 5} more");
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField($"NavMesh Surfaces: {navMeshSurfaces.Count}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Prefabs to update: {prefabsToUpdate.Count}", EditorStyles.boldLabel);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void DrawMigrationSettings()
        {
            EditorGUILayout.LabelField("Step 3: Migration Options", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            convertUnits = EditorGUILayout.ToggleLeft(
                $"Convert Units ({unitsWithNavMesh.Count} found)",
                convertUnits
            );

            convertBuildings = EditorGUILayout.ToggleLeft(
                $"Convert Buildings ({buildingsWithNavMeshObstacle.Count} found)",
                convertBuildings
            );

            convertWalls = EditorGUILayout.ToggleLeft(
                $"Convert Walls ({wallsWithNavMeshObstacle.Count} found)",
                convertWalls
            );

            removeNavMeshSurfaces = EditorGUILayout.ToggleLeft(
                $"Remove NavMesh Surfaces ({navMeshSurfaces.Count} found)",
                removeNavMeshSurfaces
            );

            updatePrefabs = EditorGUILayout.ToggleLeft(
                $"Update Prefabs ({prefabsToUpdate.Count} found)",
                updatePrefabs
            );

            createFlowFieldManager = EditorGUILayout.ToggleLeft(
                "Create FlowFieldManager in scene (if missing)",
                createFlowFieldManager
            );

            removeNavMeshComponents = EditorGUILayout.ToggleLeft(
                "Remove all NavMesh components after migration",
                removeNavMeshComponents
            );

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void DrawFlowFieldSettings()
        {
            EditorGUILayout.LabelField("Step 4: FlowField Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            flowFieldCellSize = EditorGUILayout.FloatField("Cell Size", flowFieldCellSize);
            autoDetectGridBounds = EditorGUILayout.Toggle("Auto-Detect Grid Bounds", autoDetectGridBounds);

            if (!autoDetectGridBounds)
            {
                EditorGUI.indentLevel++;
                manualGridOrigin = EditorGUILayout.Vector3Field("Grid Origin", manualGridOrigin);
                manualGridWidth = EditorGUILayout.FloatField("Grid Width", manualGridWidth);
                manualGridHeight = EditorGUILayout.FloatField("Grid Height", manualGridHeight);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void DrawMigrationButtons()
        {
            EditorGUILayout.LabelField("Step 5: Execute Migration", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "⚠️ WARNING: This operation cannot be easily undone!\n" +
                "Make sure you have a backup before proceeding.",
                MessageType.Warning
            );

            EditorGUILayout.Space(5);

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("✓ Migrate Project to FlowField", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog(
                    "Confirm Migration",
                    "Are you sure you want to migrate from NavMesh to FlowField?\n\n" +
                    "This will modify scenes and prefabs in your project.",
                    "Yes, Migrate",
                    "Cancel"
                ))
                {
                    MigrateProject();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Rescan Project", GUILayout.Height(25)))
            {
                ScanProject();
            }
        }

        private void ScanProject()
        {

            unitsWithNavMesh.Clear();
            buildingsWithNavMeshObstacle.Clear();
            wallsWithNavMeshObstacle.Clear();
            navMeshSurfaces.Clear();
            prefabsToUpdate.Clear();

            // Find all NavMeshAgents in scene
            NavMeshAgent[] agents = FindObjectsByType<NavMeshAgent>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var agent in agents)
            {
                unitsWithNavMesh.Add(agent.gameObject);
            }

            // Find all NavMeshObstacles in scene
            NavMeshObstacle[] obstacles = FindObjectsByType<NavMeshObstacle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var obstacle in obstacles)
            {
                // Check if it's a building or wall
                if (obstacle.GetComponent<BuildingNavMeshObstacle>() != null ||
                    obstacle.GetComponent<RTS.Buildings.Building>() != null)
                {
                    buildingsWithNavMeshObstacle.Add(obstacle.gameObject);
                }
                else if (obstacle.TryGetComponent<WallNavMeshObstacle>(out var wallNavMeshObstacle))
                {
                    wallsWithNavMeshObstacle.Add(obstacle.gameObject);
                }
                else
                {
                    // Generic obstacle, treat as building
                    buildingsWithNavMeshObstacle.Add(obstacle.gameObject);
                }
            }

            // Find all NavMeshSurfaces
            navMeshSurfaces.AddRange(FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Include, FindObjectsSortMode.None));

            // Find prefabs with NavMesh components
            ScanPrefabs();

            scanComplete = true;


            Repaint();
        }

        private void ScanPrefabs()
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab != null)
                {
                    bool hasNavMeshComponents =
                        prefab.GetComponentInChildren<NavMeshAgent>(true) != null ||
                        prefab.GetComponentInChildren<NavMeshObstacle>(true) != null ||
                        prefab.GetComponentInChildren<NavMeshSurface>(true) != null ||
                        prefab.GetComponentInChildren<BuildingNavMeshObstacle>(true) != null ||
                        prefab.GetComponentInChildren<WallNavMeshObstacle>(true) != null;

                    if (hasNavMeshComponents)
                    {
                        prefabsToUpdate.Add(path);
                    }
                }
            }
        }

        private void MigrateProject()
        {

            int totalSteps = 0;
            int currentStep = 0;

            // Calculate total steps
            if (convertUnits) totalSteps += unitsWithNavMesh.Count;
            if (convertBuildings) totalSteps += buildingsWithNavMeshObstacle.Count;
            if (convertWalls) totalSteps += wallsWithNavMeshObstacle.Count;
            if (removeNavMeshSurfaces) totalSteps += navMeshSurfaces.Count;
            if (updatePrefabs) totalSteps += prefabsToUpdate.Count;

            try
            {
                // Create FlowFieldManager if needed
                if (createFlowFieldManager)
                {
                    EnsureFlowFieldManager();
                }

                // Convert units
                if (convertUnits)
                {
                    foreach (var unit in unitsWithNavMesh)
                    {
                        EditorUtility.DisplayProgressBar(
                            "Migrating to FlowField",
                            $"Converting unit: {unit.name}",
                            (float)currentStep++ / totalSteps
                        );
                        ConvertUnitToFlowField(unit);
                    }
                }

                // Convert buildings
                if (convertBuildings)
                {
                    foreach (var building in buildingsWithNavMeshObstacle)
                    {
                        EditorUtility.DisplayProgressBar(
                            "Migrating to FlowField",
                            $"Converting building: {building.name}",
                            (float)currentStep++ / totalSteps
                        );
                        ConvertBuildingToFlowField(building);
                    }
                }

                // Convert walls
                if (convertWalls)
                {
                    foreach (var wall in wallsWithNavMeshObstacle)
                    {
                        EditorUtility.DisplayProgressBar(
                            "Migrating to FlowField",
                            $"Converting wall: {wall.name}",
                            (float)currentStep++ / totalSteps
                        );
                        ConvertWallToFlowField(wall);
                    }
                }

                // Remove NavMesh surfaces
                if (removeNavMeshSurfaces)
                {
                    foreach (var surface in navMeshSurfaces)
                    {
                        EditorUtility.DisplayProgressBar(
                            "Migrating to FlowField",
                            "Removing NavMesh surfaces",
                            (float)currentStep++ / totalSteps
                        );
                        if (surface != null)
                        {
                            DestroyImmediate(surface);
                        }
                    }
                }

                // Update prefabs
                if (updatePrefabs)
                {
                    foreach (var prefabPath in prefabsToUpdate)
                    {
                        EditorUtility.DisplayProgressBar(
                            "Migrating to FlowField",
                            $"Updating prefab: {System.IO.Path.GetFileName(prefabPath)}",
                            (float)currentStep++ / totalSteps
                        );
                        UpdatePrefab(prefabPath);
                    }
                }

                // Mark scene as dirty
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

                EditorUtility.ClearProgressBar();

                EditorUtility.DisplayDialog(
                    "Migration Complete",
                    $"Successfully migrated project to FlowField!\n\n" +
                    $"Converted:\n" +
                    $"- {unitsWithNavMesh.Count} units\n" +
                    $"- {buildingsWithNavMeshObstacle.Count} buildings\n" +
                    $"- {wallsWithNavMeshObstacle.Count} walls\n" +
                    $"- Removed {navMeshSurfaces.Count} NavMesh surfaces\n" +
                    $"- Updated {prefabsToUpdate.Count} prefabs\n\n" +
                    $"Don't forget to save your scene!",
                    "OK"
                );


                // Rescan to show results
                ScanProject();
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog(
                    "Migration Error",
                    $"An error occurred during migration:\n{e.Message}\n\nCheck the console for details.",
                    "OK"
                );
            }
        }

        private void EnsureFlowFieldManager()
        {
            FlowFieldManager existing = FindFirstObjectByType<FlowFieldManager>();
            if (existing != null)
            {
                return;
            }

            GameObject managerObj = new GameObject("FlowFieldManager");
            FlowFieldManager manager = managerObj.AddComponent<FlowFieldManager>();

            // Configure using reflection (since fields are private serialized)
            var managerType = typeof(FlowFieldManager);

            SetPrivateField(manager, "cellSize", flowFieldCellSize);
            SetPrivateField(manager, "autoDetectGridBounds", autoDetectGridBounds);

            if (!autoDetectGridBounds)
            {
                SetPrivateField(manager, "gridOrigin", manualGridOrigin);
                SetPrivateField(manager, "gridWidth", manualGridWidth);
                SetPrivateField(manager, "gridHeight", manualGridHeight);
            }

            EditorUtility.SetDirty(manager);

        }

        private void ConvertUnitToFlowField(GameObject unit)
        {
            if (unit == null) return;

            // Check if already converted
            if (unit.TryGetComponent<FlowFieldFollower>(out var flowFieldFollower))
            {
                return;
            }

            NavMeshAgent agent = unit.GetComponent<NavMeshAgent>();
            UnitMovement unitMovement = unit.GetComponent<UnitMovement>();

            // Capture settings before removal
            float speed = 5f;
            float radius = 0.5f;

            if (agent != null)
            {
                speed = agent.speed;
                radius = agent.radius;
            }

            // Add FlowFieldFollower
            FlowFieldFollower follower = unit.AddComponent<FlowFieldFollower>();

            // Configure using reflection
            SetPrivateField(follower, "maxSpeed", speed);
            SetPrivateField(follower, "unitRadius", radius);

            // Ensure Rigidbody exists and is configured
            if (unit.TryGetComponent<Rigidbody>(out var rb))
            {
            }
            if (rb == null)
            {
                rb = unit.AddComponent<Rigidbody>();
            }

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotationX |
                            RigidbodyConstraints.FreezeRotationZ;

            // Remove NavMesh components
            if (removeNavMeshComponents)
            {
                if (unitMovement != null)
                {
                    DestroyImmediate(unitMovement);
                }
                if (agent != null)
                {
                    DestroyImmediate(agent);
                }
            }
            else
            {
                // Just disable them
                if (unitMovement != null) unitMovement.enabled = false;
                if (agent != null) agent.enabled = false;
            }

            EditorUtility.SetDirty(unit);

        }

        private void ConvertBuildingToFlowField(GameObject building)
        {
            if (building == null) return;

            // Check if already converted
            if (building.TryGetComponent<BuildingFlowFieldObstacle>(out var buildingFlowFieldObstacle))
            {
                return;
            }

            // Add FlowField obstacle
            building.AddComponent<BuildingFlowFieldObstacle>();

            // Remove NavMesh obstacle components
            if (removeNavMeshComponents)
            {
                if (building.TryGetComponent<BuildingNavMeshObstacle>(out var oldObstacle))
                {
                }
                if (building.TryGetComponent<NavMeshObstacle>(out var navObstacle))
                {
                }

                if (oldObstacle != null) DestroyImmediate(oldObstacle);
                if (navObstacle != null) DestroyImmediate(navObstacle);
            }

            EditorUtility.SetDirty(building);

        }

        private void ConvertWallToFlowField(GameObject wall)
        {
            if (wall == null) return;

            // Check if already converted
            if (wall.TryGetComponent<WallFlowFieldObstacle>(out var wallFlowFieldObstacle))
            {
                return;
            }

            // Add FlowField obstacle
            wall.AddComponent<WallFlowFieldObstacle>();

            // Remove NavMesh obstacle components
            if (removeNavMeshComponents)
            {
                if (wall.TryGetComponent<WallNavMeshObstacle>(out var oldObstacle))
                {
                }
                if (wall.TryGetComponent<NavMeshObstacle>(out var navObstacle))
                {
                }

                if (oldObstacle != null) DestroyImmediate(oldObstacle);
                if (navObstacle != null) DestroyImmediate(navObstacle);
            }

            EditorUtility.SetDirty(wall);

        }

        private void UpdatePrefab(string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;

            // Load prefab in isolation mode
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject prefabContents = PrefabUtility.LoadPrefabContents(path);

            bool modified = false;

            // Convert units in prefab
            NavMeshAgent[] agents = prefabContents.GetComponentsInChildren<NavMeshAgent>(true);
            foreach (var agent in agents)
            {
                ConvertUnitToFlowField(agent.gameObject);
                modified = true;
            }

            // Convert obstacles in prefab
            BuildingNavMeshObstacle[] buildingObstacles = prefabContents.GetComponentsInChildren<BuildingNavMeshObstacle>(true);
            foreach (var obstacle in buildingObstacles)
            {
                ConvertBuildingToFlowField(obstacle.gameObject);
                modified = true;
            }

            WallNavMeshObstacle[] wallObstacles = prefabContents.GetComponentsInChildren<WallNavMeshObstacle>(true);
            foreach (var obstacle in wallObstacles)
            {
                ConvertWallToFlowField(obstacle.gameObject);
                modified = true;
            }

            // Remove NavMesh surfaces
            NavMeshSurface[] surfaces = prefabContents.GetComponentsInChildren<NavMeshSurface>(true);
            foreach (var surface in surfaces)
            {
                DestroyImmediate(surface);
                modified = true;
            }

            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabContents, path);
            }

            PrefabUtility.UnloadPrefabContents(prefabContents);
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance
            );

            if (field != null)
            {
                field.SetValue(obj, value);
            }
        }
    }
}
#endif
