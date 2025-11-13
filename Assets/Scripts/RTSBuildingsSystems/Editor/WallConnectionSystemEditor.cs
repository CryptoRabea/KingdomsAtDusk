using UnityEngine;
using UnityEditor;

namespace RTS.Buildings.Editor
{
    [CustomEditor(typeof(WallConnectionSystem))]
    public class WallConnectionSystemEditor : UnityEditor.Editor
    {
        private SerializedProperty gridSizeProp;
        private SerializedProperty enableConnectionsProp;
        private SerializedProperty wallMeshProp;
        private SerializedProperty wallColliderProp;
        private SerializedProperty autoCreateColliderProp;

        private void OnEnable()
        {
            gridSizeProp = serializedObject.FindProperty("gridSize");
            enableConnectionsProp = serializedObject.FindProperty("enableConnections");
            wallMeshProp = serializedObject.FindProperty("wallMesh");
            wallColliderProp = serializedObject.FindProperty("wallCollider");
            autoCreateColliderProp = serializedObject.FindProperty("autoCreateCollider");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            WallConnectionSystem wall = (WallConnectionSystem)target;

            EditorGUILayout.LabelField("Simplified Wall System - Stronghold Style", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Info box
            EditorGUILayout.HelpBox(
                "Simplified wall system using single wall mesh with automatic rotation.\n" +
                "Walls automatically connect and rotate based on neighbors.\n" +
                "No need for 16 mesh variants!",
                MessageType.Info
            );

            EditorGUILayout.Space();

            // Basic settings
            EditorGUILayout.PropertyField(gridSizeProp);
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
                EditorGUILayout.LabelField($"Grid Position: {wall.GetGridPosition()}");
                EditorGUILayout.LabelField($"Wall Type: {wall.GetWallType()}");
                EditorGUILayout.LabelField($"Connection State: {wall.GetConnectionState()} ({GetConnectionLabel(wall.GetConnectionState())})");
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space();

                // Connection status diagram
                EditorGUILayout.LabelField("Connections", EditorStyles.boldLabel);
                DrawConnectionDiagram(wall.GetConnectionState(), 60);

                EditorGUILayout.Space();

                // Connection buttons
                EditorGUILayout.BeginHorizontal();
                DrawConnectionButton("North", wall.IsConnected(WallDirection.North));
                DrawConnectionButton("East", wall.IsConnected(WallDirection.East));
                DrawConnectionButton("South", wall.IsConnected(WallDirection.South));
                DrawConnectionButton("West", wall.IsConnected(WallDirection.West));
                EditorGUILayout.EndHorizontal();

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

        private void DrawConnectionDiagram(int connectionState, float size)
        {
            Rect rect = GUILayoutUtility.GetRect(size, size);

            // Draw center square
            EditorGUI.DrawRect(new Rect(rect.center.x - 3, rect.center.y - 3, 6, 6), Color.gray);

            // Draw connections
            Color connectedColor = new Color(0.3f, 0.8f, 0.3f);

            // North (up)
            if ((connectionState & 1) != 0)
                EditorGUI.DrawRect(new Rect(rect.center.x - 1, rect.y, 2, size/2 - 3), connectedColor);

            // East (right)
            if ((connectionState & 2) != 0)
                EditorGUI.DrawRect(new Rect(rect.center.x + 3, rect.center.y - 1, size/2 - 3, 2), connectedColor);

            // South (down)
            if ((connectionState & 4) != 0)
                EditorGUI.DrawRect(new Rect(rect.center.x - 1, rect.center.y + 3, 2, size/2 - 3), connectedColor);

            // West (left)
            if ((connectionState & 8) != 0)
                EditorGUI.DrawRect(new Rect(rect.x, rect.center.y - 1, size/2 - 3, 2), connectedColor);
        }

        private void DrawConnectionButton(string direction, bool isConnected)
        {
            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = isConnected ? Color.green : Color.red;
            GUILayout.Button(direction, GUILayout.Height(20));
            GUI.backgroundColor = oldColor;
        }

        private string GetConnectionLabel(int state)
        {
            if (state == 0) return "None";

            string result = "";
            if ((state & 1) != 0) result += "N";
            if ((state & 2) != 0) result += "E";
            if ((state & 4) != 0) result += "S";
            if ((state & 8) != 0) result += "W";
            return result;
        }
    }
}
