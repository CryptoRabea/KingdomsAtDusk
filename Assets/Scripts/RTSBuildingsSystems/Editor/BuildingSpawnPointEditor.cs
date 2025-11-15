using UnityEngine;
using UnityEditor;
using RTS.Buildings;

namespace RTSBuildingsSystems.Editor
{
    /// <summary>
    /// Editor utility to add spawn points to building prefabs.
    /// </summary>
    public class BuildingSpawnPointEditor : EditorWindow
    {
        [MenuItem("RTS/Building Tools/Add Spawn Point to Selected Building")]
        public static void AddSpawnPointToSelected()
        {
            GameObject selected = Selection.activeGameObject;

            if (selected == null)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select a building GameObject in the scene or prefab.", "OK");
                return;
            }

            // Check if spawn point already exists
            BuildingSpawnPoint existingSpawnPoint = selected.GetComponentInChildren<BuildingSpawnPoint>();
            if (existingSpawnPoint != null)
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Spawn Point Exists",
                    $"A spawn point already exists at position {existingSpawnPoint.transform.localPosition}. Do you want to select it?",
                    "Select Existing",
                    "Cancel"
                );

                if (overwrite)
                {
                    Selection.activeGameObject = existingSpawnPoint.gameObject;
                }
                return;
            }

            // Create spawn point
            GameObject spawnPointObj = new GameObject("SpawnPoint");
            spawnPointObj.transform.SetParent(selected.transform);
            spawnPointObj.transform.localPosition = Vector3.forward * 3f; // 3 units in front
            spawnPointObj.transform.localRotation = Quaternion.identity;

            // Add the BuildingSpawnPoint component
            BuildingSpawnPoint spawnPoint = spawnPointObj.AddComponent<BuildingSpawnPoint>();

            // Register undo
            Undo.RegisterCreatedObjectUndo(spawnPointObj, "Add Building Spawn Point");

            // Select the new spawn point
            Selection.activeGameObject = spawnPointObj;

            Debug.Log($"✅ Added spawn point to {selected.name} at position {spawnPointObj.transform.localPosition}");

            // Mark scene dirty if not a prefab
            if (!PrefabUtility.IsPartOfPrefabAsset(selected))
            {
                EditorUtility.SetDirty(selected);
            }
        }

        [MenuItem("RTS/Building Tools/Add Spawn Point to Selected Building", true)]
        public static bool ValidateAddSpawnPoint()
        {
            return Selection.activeGameObject != null;
        }

        [MenuItem("RTS/Building Tools/Batch Add Spawn Points to All Building Prefabs")]
        public static void BatchAddSpawnPointsToPrefabs()
        {
            // Find all building prefabs
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/BuildingPrefabs&Data" });

            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("No Prefabs Found", "No building prefabs found in Assets/Prefabs/BuildingPrefabs&Data", "OK");
                return;
            }

            bool confirm = EditorUtility.DisplayDialog(
                "Batch Add Spawn Points",
                $"This will add spawn points to {guids.Length} building prefabs. Continue?",
                "Yes",
                "Cancel"
            );

            if (!confirm) return;

            int addedCount = 0;
            int skippedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab == null) continue;

                // Check if prefab has UnitTrainingQueue component (buildings that can train units)
                UnitTrainingQueue trainingQueue = prefab.GetComponent<UnitTrainingQueue>();
                if (trainingQueue == null)
                {
                    skippedCount++;
                    continue;
                }

                // Check if spawn point already exists
                BuildingSpawnPoint existingSpawnPoint = prefab.GetComponentInChildren<BuildingSpawnPoint>();
                if (existingSpawnPoint != null)
                {
                    skippedCount++;
                    continue;
                }

                // Load prefab contents
                GameObject prefabContents = PrefabUtility.LoadPrefabContents(path);

                try
                {
                    // Create spawn point
                    GameObject spawnPointObj = new GameObject("SpawnPoint");
                    spawnPointObj.transform.SetParent(prefabContents.transform);
                    spawnPointObj.transform.localPosition = Vector3.forward * 3f;
                    spawnPointObj.transform.localRotation = Quaternion.identity;

                    // Add component
                    spawnPointObj.AddComponent<BuildingSpawnPoint>();

                    // Save prefab
                    PrefabUtility.SaveAsPrefabAsset(prefabContents, path);
                    addedCount++;

                    Debug.Log($"✅ Added spawn point to {prefab.name}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to add spawn point to {prefab.name}: {e.Message}");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefabContents);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Batch Complete",
                $"Added spawn points to {addedCount} prefabs.\nSkipped {skippedCount} prefabs (already have spawn point or no training queue).",
                "OK"
            );
        }
    }

    /// <summary>
    /// Custom inspector for BuildingSpawnPoint to show helper info.
    /// </summary>
    [CustomEditor(typeof(BuildingSpawnPoint))]
    public class BuildingSpawnPointInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            BuildingSpawnPoint spawnPoint = (BuildingSpawnPoint)target;

            EditorGUILayout.HelpBox(
                "This is the spawn point for units trained in this building.\n\n" +
                "Position this where you want units to appear when they finish training.\n\n" +
                "The spawn point should be:\n" +
                "- Outside the building\n" +
                "- On walkable NavMesh area\n" +
                "- 2-4 units away from the building center",
                MessageType.Info
            );

            EditorGUILayout.Space();

            DrawDefaultInspector();

            EditorGUILayout.Space();

            // Show world position
            EditorGUILayout.LabelField("World Position", spawnPoint.Position.ToString("F2"));

            // Quick position presets
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Position Presets:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Front (+Z)"))
            {
                Undo.RecordObject(spawnPoint.transform, "Set Spawn Point Front");
                spawnPoint.transform.localPosition = Vector3.forward * 3f;
            }
            if (GUILayout.Button("Back (-Z)"))
            {
                Undo.RecordObject(spawnPoint.transform, "Set Spawn Point Back");
                spawnPoint.transform.localPosition = Vector3.back * 3f;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Right (+X)"))
            {
                Undo.RecordObject(spawnPoint.transform, "Set Spawn Point Right");
                spawnPoint.transform.localPosition = Vector3.right * 3f;
            }
            if (GUILayout.Button("Left (-X)"))
            {
                Undo.RecordObject(spawnPoint.transform, "Set Spawn Point Left");
                spawnPoint.transform.localPosition = Vector3.left * 3f;
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
