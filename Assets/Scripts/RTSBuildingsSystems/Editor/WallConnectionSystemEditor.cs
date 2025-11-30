using UnityEngine;
using UnityEditor;

namespace RTS.Buildings.Editor
{
    [CustomEditor(typeof(WallConnectionSystem))]
    public class WallConnectionSystemEditor : UnityEditor.Editor
    {
        private SerializedProperty connectionDistanceProp;
        private SerializedProperty enableConnectionsProp;
        private SerializedProperty meshVariantsProp;

        private void OnEnable()
        {
            connectionDistanceProp = serializedObject.FindProperty("connectionDistance");
            enableConnectionsProp = serializedObject.FindProperty("enableConnections");
            meshVariantsProp = serializedObject.FindProperty("meshVariants");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            WallConnectionSystem wall = (WallConnectionSystem)target;

            EditorGUILayout.LabelField("FREE-BUILD Wall System - No Grid!", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Info box
            EditorGUILayout.HelpBox(
                "TRUE FREE-BUILD wall system!\n" +
                "[OK] NO grid snapping\n" +
                "[OK] NO variants\n" +
                "[OK] Place anywhere, drag to connect\n" +
                "[OK] Distance-based connections",
                MessageType.Info
            );

            EditorGUILayout.LabelField("Wall Connection System", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Basic settings
            EditorGUILayout.PropertyField(connectionDistanceProp, new GUIContent("Connection Distance", "How close walls need to be to connect"));
            EditorGUILayout.PropertyField(enableConnectionsProp);
            EditorGUILayout.Space();

            // Mesh variants with visual guide
            EditorGUILayout.LabelField("Mesh Variants (16 Connection States)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Each index represents a connection state:\n" +
                "North=1, East=2, South=4, West=8\n\n" +
                "Examples:\n" +
                "0 = No connections (isolated wall)\n" +
                "3 = North + East (corner)\n" +
                "5 = North + South (straight vertical)\n" +
                "10 = East + West (straight horizontal)\n" +
                "15 = All directions (4-way intersection)",
                MessageType.Info
            );

            EditorGUILayout.Space();

            // Draw mesh variants in a grid

            EditorGUILayout.Space();

            // Runtime info
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Runtime Info", EditorStyles.boldLabel);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Position: {wall.transform.position}");
                EditorGUILayout.LabelField($"Connected Walls: {wall.GetConnectionCount()}");

                var connected = wall.GetConnectedWalls();
                if (connected.Count > 0)
                {
                    EditorGUILayout.LabelField("Connected to:");
                    foreach (var connectedWall in connected)
                    {
                        if (connectedWall != null)
                        {
                            float dist = Vector3.Distance(wall.transform.position, connectedWall.transform.position);
                            EditorGUILayout.LabelField($"  - Wall at {connectedWall.transform.position} (dist: {dist:F2})");
                        }
                    }
                }
                EditorGUILayout.EndVertical();
                

                EditorGUILayout.Space();

               
                if (GUILayout.Button("Force Update Connections"))
                {
                    wall.UpdateConnections();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

       

            

      


    }
}
