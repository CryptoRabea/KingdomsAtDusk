using UnityEngine;
using UnityEditor;

namespace RTS.Buildings.Editor
{
    /// <summary>
    /// Utility to quickly set up wall prefabs - Simplified Stronghold Style.
    /// Access via: Tools > RTS > Setup Wall Prefab
    /// </summary>
    public class WallPrefabSetupUtility : EditorWindow
    {
        private GameObject wallPrefabRoot;
        private GameObject customMeshPrefab;
        private bool createSimpleMesh = true;
        private float wallHeight = 2f;
        private float wallWidth = 1f;
        private float wallThickness = 0.2f;

        [MenuItem("Tools/RTS/Setup Wall Prefab")]
        public static void ShowWindow()
        {
            WallPrefabSetupUtility window = GetWindow<WallPrefabSetupUtility>("Wall Prefab Setup");
            window.minSize = new Vector2(400, 450);
        }

        private void OnGUI()
        {
            GUILayout.Label("Simplified Wall Prefab Setup - Stronghold Style", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "Simplified wall system using ONE mesh with automatic rotation!\n\n" +
                "Two modes:\n" +
                "1. AUTO: Creates a simple wall mesh for testing\n" +
                "2. MANUAL: Uses your custom wall mesh\n\n" +
                "No need for 16 variants - rotation handles everything!",
                MessageType.Info
            );

            EditorGUILayout.Space();

            // Mode selection
            createSimpleMesh = EditorGUILayout.Toggle("Auto-Create Simple Mesh", createSimpleMesh);

            EditorGUILayout.Space();

            if (createSimpleMesh)
            {
                // Simple mesh settings
                GUILayout.Label("Simple Mesh Settings", EditorStyles.boldLabel);
                wallHeight = EditorGUILayout.FloatField("Wall Height", wallHeight);
                wallWidth = EditorGUILayout.FloatField("Wall Width", wallWidth);
                wallThickness = EditorGUILayout.FloatField("Wall Thickness", wallThickness);
            }
            else
            {
                // Manual mesh settings
                GUILayout.Label("Custom Mesh Settings", EditorStyles.boldLabel);
                customMeshPrefab = (GameObject)EditorGUILayout.ObjectField(
                    "Custom Wall Mesh",
                    customMeshPrefab,
                    typeof(GameObject),
                    false
                );

                EditorGUILayout.HelpBox(
                    "Your mesh should be oriented North (positive Z). The system will rotate it automatically.",
                    MessageType.Info
                );
            }

            EditorGUILayout.Space();

            // Wall prefab root
            GUILayout.Label("Output", EditorStyles.boldLabel);
            wallPrefabRoot = (GameObject)EditorGUILayout.ObjectField(
                "Wall Prefab Root",
                wallPrefabRoot,
                typeof(GameObject),
                true
            );

            EditorGUILayout.Space();

            // Setup button
            GUI.enabled = wallPrefabRoot != null && (createSimpleMesh || customMeshPrefab != null);

            if (GUILayout.Button("Setup Wall Prefab", GUILayout.Height(40)))
            {
                SetupWallPrefab();
            }

            GUI.enabled = true;

            EditorGUILayout.Space();

            // Quick create button
            if (GUILayout.Button("Create New Wall Prefab GameObject", GUILayout.Height(30)))
            {
                CreateNewWallPrefab();
            }

            EditorGUILayout.Space();

            // Wall type reference
            DrawWallTypeReference();
        }

        private void SetupWallPrefab()
        {
            if (wallPrefabRoot == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Wall Prefab Root GameObject!", "OK");
                return;
            }

            if (!createSimpleMesh && customMeshPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Custom Wall Mesh!", "OK");
                return;
            }

            // Ensure required components
            if (wallPrefabRoot.GetComponent<Building>() == null)
            {
                wallPrefabRoot.AddComponent<Building>();
                Debug.Log("Added Building component");
            }

            WallConnectionSystem wallSystem = wallPrefabRoot.GetComponent<WallConnectionSystem>();
            if (wallSystem == null)
            {
                wallSystem = wallPrefabRoot.AddComponent<WallConnectionSystem>();
                Debug.Log("Added WallConnectionSystem component");
            }

            // Add BuildingSelectable for selection
            if (wallPrefabRoot.GetComponent<BuildingSelectable>() == null)
            {
                wallPrefabRoot.AddComponent<BuildingSelectable>();
                Debug.Log("Added BuildingSelectable component");
            }

            // Add WallUpgradeSystem for tower upgrades
            if (wallPrefabRoot.GetComponent<WallUpgradeSystem>() == null)
            {
                wallPrefabRoot.AddComponent<WallUpgradeSystem>();
                Debug.Log("Added WallUpgradeSystem component");
            }

            // Create or find wall mesh
            Transform wallMeshTransform = wallPrefabRoot.transform.Find("WallMesh");
            GameObject wallMesh;

            if (wallMeshTransform != null)
            {
                wallMesh = wallMeshTransform.gameObject;
                Debug.Log("Using existing WallMesh");
            }
            else
            {
                if (createSimpleMesh)
                {
                    wallMesh = CreateSimpleWallMesh(wallPrefabRoot.transform);
                }
                else
                {
                    wallMesh = Instantiate(customMeshPrefab, wallPrefabRoot.transform);
                    wallMesh.name = "WallMesh";
                    wallMesh.transform.localPosition = Vector3.zero;
                    wallMesh.transform.localRotation = Quaternion.identity;
                }

                Debug.Log($"Created WallMesh");
            }

            // Assign wall mesh to WallConnectionSystem
            SerializedObject so = new SerializedObject(wallSystem);
            SerializedProperty wallMeshProp = so.FindProperty("wallMesh");
            wallMeshProp.objectReferenceValue = wallMesh;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(wallPrefabRoot);
            EditorUtility.DisplayDialog("Success",
                "Simplified wall prefab setup complete!\n\n" +
                "✓ Wall mesh created and assigned\n" +
                "✓ Auto-rotation enabled\n" +
                "✓ Collider will be auto-created\n" +
                "✓ Upgrade system added\n" +
                "✓ Selection enabled",
                "OK");
        }

        private GameObject CreateSimpleWallMesh(Transform parent)
        {
            GameObject wallMesh = new GameObject("WallMesh");
            wallMesh.transform.SetParent(parent);
            wallMesh.transform.localPosition = Vector3.zero;
            wallMesh.transform.localRotation = Quaternion.identity;

            // Create main wall segment (oriented North - positive Z)
            GameObject wallSegment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallSegment.name = "WallSegment";
            wallSegment.transform.SetParent(wallMesh.transform);
            wallSegment.transform.localPosition = Vector3.zero;
            wallSegment.transform.localScale = new Vector3(wallThickness, wallHeight, wallWidth);

            // Destroy collider (will be added at root level)
            DestroyImmediate(wallSegment.GetComponent<Collider>());

            // Apply material
            Renderer renderer = wallSegment.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.6f, 0.55f, 0.45f); // Stone color
            renderer.sharedMaterial = mat;

            return wallMesh;
        }

        private void CreateNewWallPrefab()
        {
            GameObject newWall = new GameObject("WallPrefab");
            wallPrefabRoot = newWall;

            Selection.activeGameObject = newWall;
            EditorGUIUtility.PingObject(newWall);

            Debug.Log("Created new Wall Prefab GameObject. Now click 'Setup Wall Prefab' to complete setup.");
        }

        private void DrawWallTypeReference()
        {
            GUILayout.Label("Wall Type Reference", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "WALL TYPES (Auto-detected and rotated):\n\n" +
                "• Standalone: No connections\n" +
                "• End: 1 connection (end piece)\n" +
                "• Straight: 2 opposite connections (N-S or E-W)\n" +
                "• Corner: 2 adjacent connections (NE, ES, SW, WN)\n" +
                "• T-Junction: 3 connections\n" +
                "• Cross: 4 connections (intersection)\n\n" +
                "The system automatically rotates your wall mesh!\n" +
                "Mesh should be oriented North (positive Z).",
                MessageType.None
            );
        }
    }
}
