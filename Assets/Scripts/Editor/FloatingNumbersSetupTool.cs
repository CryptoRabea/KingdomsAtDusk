#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using RTS.Units.Components;
using RTS.Buildings.Components;
using RTS.UI.FloatingNumbers;
using RTS.UI.HealthBar;

namespace RTS.Editor
{
    /// <summary>
    /// Editor tool for automatically setting up floating numbers and health bars
    /// Access via: Tools > RTS > Setup Floating Numbers & Health Bars
    /// </summary>
    public class FloatingNumbersSetupTool : EditorWindow
    {
        private GameObject healthBarPrefab;
        private bool setupFloatingNumbers = true;
        private bool setupHealthBars = true;
        private bool setupOnUnits = true;
        private bool setupOnBuildings = true;

        [MenuItem("Tools/RTS/Setup Floating Numbers & Health Bars")]
        public static void ShowWindow()
        {
            var window = GetWindow<FloatingNumbersSetupTool>("Auto Setup Tool");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Floating Numbers & Health Bars Auto Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool automatically adds FloatingNumbersAutoSetup and HealthBarAutoSetup components to all units and buildings in the scene.",
                MessageType.Info
            );

            GUILayout.Space(10);

            // Options
            setupFloatingNumbers = EditorGUILayout.Toggle("Setup Floating Numbers", setupFloatingNumbers);
            setupHealthBars = EditorGUILayout.Toggle("Setup Health Bars", setupHealthBars);

            GUILayout.Space(10);

            setupOnUnits = EditorGUILayout.Toggle("Setup on Units", setupOnUnits);
            setupOnBuildings = EditorGUILayout.Toggle("Setup on Buildings", setupOnBuildings);

            GUILayout.Space(10);

            // Health bar prefab reference
            if (setupHealthBars)
            {
                healthBarPrefab = (GameObject)EditorGUILayout.ObjectField(
                    "Health Bar Prefab",
                    healthBarPrefab,
                    typeof(GameObject),
                    false
                );

                if (healthBarPrefab == null)
                {
                    EditorGUILayout.HelpBox(
                        "Health Bar Prefab not assigned. The script will try to find it in Resources/Prefabs/UI/HealthBar",
                        MessageType.Warning
                    );
                }
            }

            GUILayout.Space(20);

            // Setup button
            GUI.enabled = setupFloatingNumbers || setupHealthBars;
            if (GUILayout.Button("Auto Setup All", GUILayout.Height(40)))
            {
                PerformAutoSetup();
            }
            GUI.enabled = true;

            GUILayout.Space(10);

            // Individual setup buttons
            EditorGUILayout.LabelField("Individual Setup:", EditorStyles.boldLabel);

            if (GUILayout.Button("Setup Units Only"))
            {
                SetupUnits();
            }

            if (GUILayout.Button("Setup Buildings Only"))
            {
                SetupBuildings();
            }

            GUILayout.Space(10);

            // Cleanup buttons
            EditorGUILayout.LabelField("Cleanup:", EditorStyles.boldLabel);

            if (GUILayout.Button("Remove All FloatingNumbersAutoSetup"))
            {
                RemoveAllFloatingNumbersSetup();
            }

            if (GUILayout.Button("Remove All HealthBarAutoSetup"))
            {
                RemoveAllHealthBarSetup();
            }
        }

        private void PerformAutoSetup()
        {
            int totalAdded = 0;

            if (setupOnUnits)
            {
                totalAdded += SetupUnits();
            }

            if (setupOnBuildings)
            {
                totalAdded += SetupBuildings();
            }

            EditorUtility.DisplayDialog(
                "Setup Complete",
                $"Successfully added {totalAdded} components!",
                "OK"
            );
        }

        private int SetupUnits()
        {
            UnitHealth[] allUnits = FindObjectsOfType<UnitHealth>();
            int added = 0;

            foreach (var unit in allUnits)
            {
                if (setupFloatingNumbers)
                {
                    if (unit.GetComponent<FloatingNumbersAutoSetup>() == null)
                    {
                        Undo.AddComponent<FloatingNumbersAutoSetup>(unit.gameObject);
                        added++;
                    }
                }

                if (setupHealthBars)
                {
                    if (unit.GetComponent<HealthBarAutoSetup>() == null)
                    {
                        var healthBarSetup = Undo.AddComponent<HealthBarAutoSetup>(unit.gameObject);
                        if (healthBarPrefab != null)
                        {
                            SerializedObject so = new SerializedObject(healthBarSetup);
                            so.FindProperty("healthBarPrefab").objectReferenceValue = healthBarPrefab;
                            so.ApplyModifiedProperties();
                        }
                        added++;
                    }
                }
            }

            Debug.Log($"Added {added} components to {allUnits.Length} units");
            return added;
        }

        private int SetupBuildings()
        {
            BuildingHealth[] allBuildings = FindObjectsOfType<BuildingHealth>();
            int added = 0;

            foreach (var building in allBuildings)
            {
                if (setupFloatingNumbers)
                {
                    if (building.GetComponent<FloatingNumbersAutoSetup>() == null)
                    {
                        Undo.AddComponent<FloatingNumbersAutoSetup>(building.gameObject);
                        added++;
                    }
                }

                if (setupHealthBars)
                {
                    if (building.GetComponent<HealthBarAutoSetup>() == null)
                    {
                        var healthBarSetup = Undo.AddComponent<HealthBarAutoSetup>(building.gameObject);
                        if (healthBarPrefab != null)
                        {
                            SerializedObject so = new SerializedObject(healthBarSetup);
                            so.FindProperty("healthBarPrefab").objectReferenceValue = healthBarPrefab;
                            so.ApplyModifiedProperties();
                        }
                        added++;
                    }
                }
            }

            Debug.Log($"Added {added} components to {allBuildings.Length} buildings");
            return added;
        }

        private void RemoveAllFloatingNumbersSetup()
        {
            if (!EditorUtility.DisplayDialog(
                "Remove All FloatingNumbersAutoSetup",
                "Are you sure you want to remove all FloatingNumbersAutoSetup components from the scene?",
                "Yes", "Cancel"))
            {
                return;
            }

            FloatingNumbersAutoSetup[] all = FindObjectsOfType<FloatingNumbersAutoSetup>();
            foreach (var component in all)
            {
                Undo.DestroyObjectImmediate(component);
            }

            Debug.Log($"Removed {all.Length} FloatingNumbersAutoSetup components");
            EditorUtility.DisplayDialog("Complete", $"Removed {all.Length} components", "OK");
        }

        private void RemoveAllHealthBarSetup()
        {
            if (!EditorUtility.DisplayDialog(
                "Remove All HealthBarAutoSetup",
                "Are you sure you want to remove all HealthBarAutoSetup components from the scene?",
                "Yes", "Cancel"))
            {
                return;
            }

            HealthBarAutoSetup[] all = FindObjectsOfType<HealthBarAutoSetup>();
            foreach (var component in all)
            {
                Undo.DestroyObjectImmediate(component);
            }

            Debug.Log($"Removed {all.Length} HealthBarAutoSetup components");
            EditorUtility.DisplayDialog("Complete", $"Removed {all.Length} components", "OK");
        }
    }
}
#endif
