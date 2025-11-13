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
                "✓ NO grid snapping\n" +
                "✓ NO variants\n" +
                "✓ Place anywhere, drag to connect\n" +
                "✓ Distance-based connections",
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
            DrawMeshVariantsGrid();

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
                EditorGUILayout.LabelField($"Grid Position: {wall.GetGridPosition()}");
                EditorGUILayout.LabelField($"Connection State: {wall.GetConnectionState()} ({GetConnectionLabel(wall.GetConnectionState())})");

                EditorGUILayout.Space();

                // Connection status
                EditorGUILayout.BeginHorizontal();
                DrawConnectionButton("North", wall.IsConnected(WallDirection.North));
                DrawConnectionButton("East", wall.IsConnected(WallDirection.East));
                DrawConnectionButton("South", wall.IsConnected(WallDirection.South));
                DrawConnectionButton("West", wall.IsConnected(WallDirection.West));
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Force Update Connections"))
                {
                    wall.UpdateConnections();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMeshVariantsGrid()
        {
            // Connection state labels and visual representation
            string[] stateLabels = new string[]
            {
                "0: None",           // 0000
                "1: N",              // 0001
                "2: E",              // 0010
                "3: N+E",            // 0011
                "4: S",              // 0100
                "5: N+S",            // 0101
                "6: E+S",            // 0110
                "7: N+E+S",          // 0111
                "8: W",              // 1000
                "9: N+W",            // 1001
                "10: E+W",           // 1010
                "11: N+E+W",         // 1011
                "12: S+W",           // 1100
                "13: N+S+W",         // 1101
                "14: E+S+W",         // 1110
                "15: N+E+S+W"        // 1111
            };

            for (int i = 0; i < 16; i++)
            {
                EditorGUILayout.BeginHorizontal();

                // State label with visual diagram
                GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.fixedWidth = 120;
                EditorGUILayout.LabelField($"{stateLabels[i]}", labelStyle);

                // Draw mini diagram
                DrawConnectionDiagram(i, 20);

                // Property field
                SerializedProperty element = meshVariantsProp.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(element, GUIContent.none);

                EditorGUILayout.EndHorizontal();
            }
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
