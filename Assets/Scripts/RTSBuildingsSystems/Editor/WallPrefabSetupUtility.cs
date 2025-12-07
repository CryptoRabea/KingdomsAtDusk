using UnityEngine;
using UnityEditor;

namespace RTS.Buildings.Editor
{
    /// <summary>
    /// Utility to quickly set up wall prefabs with mesh variants.
    /// Access via: Tools > RTS > Setup Wall Prefab
    /// </summary>
    public class WallPrefabSetupUtility : EditorWindow
    {
        private GameObject wallPrefabRoot;
        private GameObject baseMeshPrefab;
        private bool createSimpleVariants = true;
        private float wallHeight = 2f;
        private float wallWidth = 1f;
        private float wallThickness = 0.2f;

        [MenuItem("Tools/RTS/Setup Wall Prefab")]
        public static void ShowWindow()
        {
            WallPrefabSetupUtility window = GetWindow<WallPrefabSetupUtility>("Wall Prefab Setup");
            window.minSize = new Vector2(400, 500);
        }

        private void OnGUI()
        {
            GUILayout.Label("Wall Prefab Setup Utility", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This utility helps you quickly set up a wall prefab with all 16 mesh variants.\n\n" +
                "Two modes:\n" +
                "1. AUTO: Creates simple colored cube variants for testing\n" +
                "2. MANUAL: Uses your custom mesh as a base for all variants",
                MessageType.Info
            );

            EditorGUILayout.Space();

            // Mode selection
            createSimpleVariants = EditorGUILayout.Toggle("Auto-Create Test Variants", createSimpleVariants);

            EditorGUILayout.Space();

            if (createSimpleVariants)
            {
                // Simple variant settings
                GUILayout.Label("Simple Variant Settings", EditorStyles.boldLabel);
                wallHeight = EditorGUILayout.FloatField("Wall Height", wallHeight);
                wallWidth = EditorGUILayout.FloatField("Wall Width", wallWidth);
                wallThickness = EditorGUILayout.FloatField("Wall Thickness", wallThickness);
            }
            else
            {
                // Manual variant settings
                GUILayout.Label("Manual Variant Settings", EditorStyles.boldLabel);
                baseMeshPrefab = (GameObject)EditorGUILayout.ObjectField(
                    "Base Mesh Prefab",
                    baseMeshPrefab,
                    typeof(GameObject),
                    false
                );

                EditorGUILayout.HelpBox(
                    "Your base mesh will be duplicated 16 times. You'll need to manually adjust each variant later.",
                    MessageType.Warning
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
            GUI.enabled = wallPrefabRoot != null && (createSimpleVariants || baseMeshPrefab != null);

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

            // Connection state reference
            DrawConnectionStateReference();
        }

        private void SetupWallPrefab()
        {
            if (wallPrefabRoot == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Wall Prefab Root GameObject!", "OK");
                return;
            }

            if (!createSimpleVariants && baseMeshPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Base Mesh Prefab!", "OK");
                return;
            }

            // Ensure required components
            if (wallPrefabRoot.GetComponent<Building>() == null)
            {
                wallPrefabRoot.AddComponent<Building>();
            }

            WallConnectionSystem wallSystem = wallPrefabRoot.GetComponent<WallConnectionSystem>();
            if (wallSystem == null)
            {
                wallSystem = wallPrefabRoot.AddComponent<WallConnectionSystem>();
            }

            // Create variant container
            Transform variantContainer = wallPrefabRoot.transform.Find("Variants");
            if (variantContainer == null)
            {
                GameObject container = new GameObject("Variants");
                container.transform.SetParent(wallPrefabRoot.transform);
                container.transform.localPosition = Vector3.zero;
                variantContainer = container.transform;
            }

            // Create 16 variants
            GameObject[] variants = new GameObject[16];

            for (int i = 0; i < 16; i++)
            {
                string variantName = GetVariantName(i);
                Transform existingVariant = variantContainer.Find(variantName);

                if (existingVariant != null)
                {
                    variants[i] = existingVariant.gameObject;
                }
                else
                {
                    if (createSimpleVariants)
                    {
                        variants[i] = CreateSimpleVariant(i, variantName, variantContainer);
                    }
                    else
                    {
                        variants[i] = CreateManualVariant(i, variantName, variantContainer);
                    }

                }

                // Set initial state (only first variant active)
                variants[i].SetActive(i == 0);
            }

            // Assign variants to WallConnectionSystem
            SerializedObject so = new SerializedObject(wallSystem);
            SerializedProperty meshVariantsProp = so.FindProperty("meshVariants");

            meshVariantsProp.arraySize = 16;
            for (int i = 0; i < 16; i++)
            {
                meshVariantsProp.GetArrayElementAtIndex(i).objectReferenceValue = variants[i];
            }

            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(wallPrefabRoot);
            EditorUtility.DisplayDialog("Success", "Wall prefab setup complete!\n\nAll 16 variants have been created and assigned.", "OK");
        }

        private GameObject CreateSimpleVariant(int index, string name, Transform parent)
        {
            GameObject variant = new GameObject(name);
            variant.transform.SetParent(parent);
            variant.transform.localPosition = Vector3.zero;

            // Create center cube
            GameObject center = GameObject.CreatePrimitive(PrimitiveType.Cube);
            center.name = "Center";
            center.transform.SetParent(variant.transform);
            center.transform.localPosition = Vector3.zero;
            center.transform.localScale = new Vector3(wallThickness, wallHeight, wallThickness);

            // Destroy collider (we don't need it on visual variants)
            DestroyImmediate(center.GetComponent<Collider>());

            // Get connection directions
            bool north = (index & 1) != 0;
            bool east = (index & 2) != 0;
            bool south = (index & 4) != 0;
            bool west = (index & 8) != 0;

            // Create connection segments
            if (north) CreateWallSegment("North", variant.transform, new Vector3(0, 0, wallWidth / 2), new Vector3(wallThickness, wallHeight, wallWidth / 2));
            if (east) CreateWallSegment("East", variant.transform, new Vector3(wallWidth / 2, 0, 0), new Vector3(wallWidth / 2, wallHeight, wallThickness));
            if (south) CreateWallSegment("South", variant.transform, new Vector3(0, 0, -wallWidth / 2), new Vector3(wallThickness, wallHeight, wallWidth / 2));
            if (west) CreateWallSegment("West", variant.transform, new Vector3(-wallWidth / 2, 0, 0), new Vector3(wallWidth / 2, wallHeight, wallThickness));

            // Color based on connection count
            Color variantColor = GetVariantColor(index);
            foreach (Renderer renderer in variant.GetComponentsInChildren<Renderer>())
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = variantColor;
                renderer.sharedMaterial = mat;
            }

            return variant;
        }

        private void CreateWallSegment(string name, Transform parent, Vector3 position, Vector3 scale)
        {
            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = name;
            segment.transform.SetParent(parent);
            segment.transform.localPosition = position;
            segment.transform.localScale = scale;

            // Destroy collider
            DestroyImmediate(segment.GetComponent<Collider>());
        }

        private GameObject CreateManualVariant(int index, string name, Transform parent)
        {
            GameObject variant = Instantiate(baseMeshPrefab, parent);
            variant.name = name;
            variant.transform.localPosition = Vector3.zero;

            return variant;
        }

        private void CreateNewWallPrefab()
        {
            GameObject newWall = new GameObject("WallPrefab");
            wallPrefabRoot = newWall;

            Selection.activeGameObject = newWall;
            EditorGUIUtility.PingObject(newWall);

        }

        private string GetVariantName(int index)
        {
            string[] names = new string[]
            {
                "Variant_00_None",
                "Variant_01_N",
                "Variant_02_E",
                "Variant_03_NE",
                "Variant_04_S",
                "Variant_05_NS",
                "Variant_06_ES",
                "Variant_07_NES",
                "Variant_08_W",
                "Variant_09_NW",
                "Variant_10_EW",
                "Variant_11_NEW",
                "Variant_12_SW",
                "Variant_13_NSW",
                "Variant_14_ESW",
                "Variant_15_NESW"
            };

            return names[index];
        }

        private Color GetVariantColor(int index)
        {
            // Color based on number of connections
            int connectionCount = 0;
            if ((index & 1) != 0) connectionCount++;
            if ((index & 2) != 0) connectionCount++;
            if ((index & 4) != 0) connectionCount++;
            if ((index & 8) != 0) connectionCount++;

            switch (connectionCount)
            {
                case 0: return new Color(0.5f, 0.5f, 0.5f); // Gray - isolated
                case 1: return new Color(0.6f, 0.4f, 0.2f); // Brown - single
                case 2: return new Color(0.4f, 0.6f, 0.3f); // Green - straight/corner
                case 3: return new Color(0.3f, 0.5f, 0.8f); // Blue - T-junction
                case 4: return new Color(0.8f, 0.5f, 0.3f); // Orange - 4-way
                default: return Color.white;
            }
        }

        private void DrawConnectionStateReference()
        {
            GUILayout.Label("Connection State Reference", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "0: None (isolated)\n" +
                "1: N  |  2: E  |  3: NE\n" +
                "4: S  |  5: NS (straight)  |  6: ES\n" +
                "7: NES  |  8: W  |  9: NW\n" +
                "10: EW (straight)  |  11: NEW\n" +
                "12: SW  |  13: NSW  |  14: ESW\n" +
                "15: NESW (4-way intersection)",
                MessageType.None
            );
        }
    }
}
