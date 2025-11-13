using UnityEngine;
using UnityEditor;

namespace RTS.Buildings.Editor
{
    [CustomEditor(typeof(WallConnectionSystem))]
    public class WallConnectionSystemEditor : UnityEditor.Editor
    {
        private SerializedProperty connectionDistanceProp;
        private SerializedProperty enableConnectionsProp;
        private SerializedProperty wallMeshProp;
        private SerializedProperty wallColliderProp;
        private SerializedProperty autoCreateColliderProp;

        private void OnEnable()
        {
            connectionDistanceProp = serializedObject.FindProperty("connectionDistance");
            enableConnectionsProp = serializedObject.FindProperty("enableConnections");
            wallMeshProp = serializedObject.FindProperty("wallMesh");
            wallColliderProp = serializedObject.FindProperty("wallCollider");
            autoCreateColliderProp = serializedObject.FindProperty("autoCreateCollider");
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
                "✓ NO grid snapping\n" +
                "✓ NO variants\n" +
                "✓ Place anywhere, drag to connect\n" +
                "✓ Distance-based connections",
                MessageType.Info
            );

            EditorGUILayout.Space();

            // Basic settings
            EditorGUILayout.PropertyField(connectionDistanceProp, new GUIContent("Connection Distance", "How close walls need to be to connect"));
            EditorGUILayout.PropertyField(enableConnectionsProp);
            EditorGUILayout.Space();

            // Visual settings
            EditorGUILayout.LabelField("Visual Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(wallMeshProp, new GUIContent("Wall Mesh", "The main wall mesh object that will be rotated"));
            EditorGUILayout.Space();

            // Collider settings
            EditorGUILayout.LabelField("Collider Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(wallColliderProp, new GUIContent("Wall Collider", "Collider for selection and gameplay"));
            EditorGUILayout.PropertyField(autoCreateColliderProp, new GUIContent("Auto Create Collider", "Automatically create collider if not assigned"));

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
                            EditorGUILayout.LabelField($"  • Wall at {connectedWall.transform.position} (dist: {dist:F2})");
                        }
                    }
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space();

                // Manual controls
                EditorGUILayout.LabelField("Manual Controls", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Rotate 90°"))
                {
                    wall.RotateWall(90f);
                }
                if (GUILayout.Button("Force Update"))
                {
                    wall.UpdateConnections();
                }
                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
