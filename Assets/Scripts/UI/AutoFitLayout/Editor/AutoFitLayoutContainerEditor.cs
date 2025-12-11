using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace RTS.UI.AutoFit.Editor
{
    /// <summary>
    /// Custom editor for AutoFitLayoutContainer with visual preview and easy setup.
    /// </summary>
    [CustomEditor(typeof(AutoFitLayoutContainer))]
    public class AutoFitLayoutContainerEditor : UnityEditor.Editor
    {
        private SerializedProperty shapeProp;
        private SerializedProperty minContainerWidthProp;
        private SerializedProperty maxContainerWidthProp;
        private SerializedProperty minContainerHeightProp;
        private SerializedProperty maxContainerHeightProp;
        private SerializedProperty paddingProp;
        private SerializedProperty minCellSizeProp;
        private SerializedProperty maxCellSizeProp;
        private SerializedProperty cellSpacingProp;
        private SerializedProperty fixedColumnsProp;
        private SerializedProperty fixedRowsProp;
        private SerializedProperty layoutPreferenceProp;
        private SerializedProperty flowDirectionProp;
        private SerializedProperty hideOverflowProp;
        private SerializedProperty warnOnOverflowProp;
        private SerializedProperty updateInEditModeProp;
        private SerializedProperty debugModeProp;

        private void OnEnable()
        {
            shapeProp = serializedObject.FindProperty("shape");
            minContainerWidthProp = serializedObject.FindProperty("minContainerWidth");
            maxContainerWidthProp = serializedObject.FindProperty("maxContainerWidth");
            minContainerHeightProp = serializedObject.FindProperty("minContainerHeight");
            maxContainerHeightProp = serializedObject.FindProperty("maxContainerHeight");
            paddingProp = serializedObject.FindProperty("padding");
            minCellSizeProp = serializedObject.FindProperty("minCellSize");
            maxCellSizeProp = serializedObject.FindProperty("maxCellSize");
            cellSpacingProp = serializedObject.FindProperty("cellSpacing");
            fixedColumnsProp = serializedObject.FindProperty("fixedColumns");
            fixedRowsProp = serializedObject.FindProperty("fixedRows");
            layoutPreferenceProp = serializedObject.FindProperty("layoutPreference");
            flowDirectionProp = serializedObject.FindProperty("flowDirection");
            hideOverflowProp = serializedObject.FindProperty("hideOverflow");
            warnOnOverflowProp = serializedObject.FindProperty("warnOnOverflow");
            updateInEditModeProp = serializedObject.FindProperty("updateInEditMode");
            debugModeProp = serializedObject.FindProperty("debugMode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            AutoFitLayoutContainer container = (AutoFitLayoutContainer)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Auto-Fit Layout Container", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "NEVER shows content outside container bounds. Hides overflow items instead.\n" +
                "Respects min/max cell sizes and container sizes.",
                MessageType.Info
            );

            EditorGUILayout.Space();

            // Container Bounds
            EditorGUILayout.LabelField("Container Bounds", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(shapeProp, new GUIContent("Shape"));

            EditorGUILayout.PropertyField(minContainerWidthProp, new GUIContent("Min Container Width"));
            EditorGUILayout.PropertyField(maxContainerWidthProp, new GUIContent("Max Container Width"));
            EditorGUILayout.PropertyField(minContainerHeightProp, new GUIContent("Min Container Height"));
            EditorGUILayout.PropertyField(maxContainerHeightProp, new GUIContent("Max Container Height"));
            EditorGUILayout.PropertyField(paddingProp, new GUIContent("Padding"));

            EditorGUILayout.Space();

            // Cell Size Constraints
            EditorGUILayout.LabelField("Cell Size Constraints", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(minCellSizeProp, new GUIContent("Min Cell Size", "NEVER goes smaller than this"));
            EditorGUILayout.PropertyField(maxCellSizeProp, new GUIContent("Max Cell Size", "NEVER goes larger than this"));
            EditorGUILayout.PropertyField(cellSpacingProp, new GUIContent("Cell Spacing"));

            EditorGUILayout.Space();

            // Grid Configuration
            EditorGUILayout.LabelField("Grid Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(fixedColumnsProp, new GUIContent("Fixed Columns", "0 = unlimited/auto-calculate"));
            EditorGUILayout.PropertyField(fixedRowsProp, new GUIContent("Fixed Rows", "0 = unlimited/auto-calculate"));
            EditorGUILayout.PropertyField(layoutPreferenceProp, new GUIContent("Layout Preference"));
            EditorGUILayout.PropertyField(flowDirectionProp, new GUIContent("Flow Direction"));

            EditorGUILayout.Space();

            // Overflow Handling
            EditorGUILayout.LabelField("Overflow Handling", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(hideOverflowProp, new GUIContent("Hide Overflow", "Hide items that don't fit"));
            EditorGUILayout.PropertyField(warnOnOverflowProp, new GUIContent("Warn On Overflow"));

            // Show overflow info
            if (Application.isPlaying)
            {
                int visible = container.GetVisibleChildCount();
                int hidden = container.GetHiddenChildCount();
                if (hidden > 0)
                {
                    EditorGUILayout.HelpBox($"⚠️ {hidden} items hidden (showing {visible})", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox($"✓ All {visible} items visible", MessageType.Info);
                }
            }

            EditorGUILayout.Space();

            // Advanced
            EditorGUILayout.LabelField("Advanced", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(updateInEditModeProp, new GUIContent("Update in Edit Mode", "Continuously update layout in editor"));
            EditorGUILayout.PropertyField(debugModeProp, new GUIContent("Debug Mode", "Show debug logs"));

            EditorGUILayout.Space();

            // Quick Actions
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Update Layout Now"))
            {
                container.ForceUpdate();
            }

            if (GUILayout.Button("Reset to Defaults"))
            {
                if (EditorUtility.DisplayDialog("Reset to Defaults", "Reset all settings to default values?", "Yes", "Cancel"))
                {
                    ResetToDefaults();
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Layout Component Info
            ShowLayoutComponentInfo(container);

            serializedObject.ApplyModifiedProperties();

            // Auto-update when properties change
            if (GUI.changed)
            {
                container.ForceUpdate();
            }
        }

        private void ShowLayoutComponentInfo(AutoFitLayoutContainer container)
        {
            EditorGUILayout.LabelField("Detected Layout", EditorStyles.boldLabel);

            if (container.TryGetComponent<GridLayoutGroup>(out var grid))
            {
            }
            HorizontalLayoutGroup horizontal = container.GetComponent<HorizontalLayoutGroup>();
            VerticalLayoutGroup vertical = container.GetComponent<VerticalLayoutGroup>();

            if (grid != null)
            {
                EditorGUILayout.HelpBox("✓ GridLayoutGroup detected", MessageType.Info);
            }
            else if (horizontal != null)
            {
                EditorGUILayout.HelpBox("✓ HorizontalLayoutGroup detected", MessageType.Info);
            }
            else if (vertical != null)
            {
                EditorGUILayout.HelpBox("✓ VerticalLayoutGroup detected", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("No layout group detected. Using manual arrangement.", MessageType.Warning);

                if (GUILayout.Button("Add GridLayoutGroup"))
                {
                    Undo.AddComponent<GridLayoutGroup>(container.gameObject);
                    container.ForceUpdate();
                }
            }
        }

        private void ResetToDefaults()
        {
            shapeProp.enumValueIndex = 0; // Square
            minContainerWidthProp.floatValue = 100f;
            maxContainerWidthProp.floatValue = 500f;
            minContainerHeightProp.floatValue = 100f;
            maxContainerHeightProp.floatValue = 500f;
            paddingProp.floatValue = 10f;
            minCellSizeProp.floatValue = 32f;
            maxCellSizeProp.floatValue = 128f;
            cellSpacingProp.floatValue = 8f;
            fixedColumnsProp.intValue = 0;
            fixedRowsProp.intValue = 0;
            layoutPreferenceProp.enumValueIndex = 0; // PreferHorizontal
            flowDirectionProp.enumValueIndex = 0; // LeftToRight
            hideOverflowProp.boolValue = true;
            warnOnOverflowProp.boolValue = true;
            updateInEditModeProp.boolValue = true;
            debugModeProp.boolValue = false;

            serializedObject.ApplyModifiedProperties();

            AutoFitLayoutContainer container = (AutoFitLayoutContainer)target;
            container.ForceUpdate();
        }
    }

    /// <summary>
    /// Menu item to quickly add AutoFitLayoutContainer to selected GameObject.
    /// </summary>
    public static class AutoFitLayoutMenu
    {
        [MenuItem("GameObject/UI/Auto-Fit Layout Container", false, 10)]
        public static void CreateAutoFitContainer(MenuCommand menuCommand)
        {
            // Create container GameObject
            GameObject container = new GameObject("AutoFitContainer");
            GameObjectUtility.SetParentAndAlign(container, menuCommand.context as GameObject);

            // Add RectTransform
            RectTransform rect = container.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 300);

            // Add AutoFitLayoutContainer
            container.AddComponent<AutoFitLayoutContainer>();

            // Add GridLayoutGroup by default
            GridLayoutGroup grid = container.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(64, 64);
            grid.spacing = new Vector2(8, 8);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;

            // Add Image for visibility
            Image image = container.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

            // Register undo
            Undo.RegisterCreatedObjectUndo(container, "Create AutoFit Container");
            Selection.activeObject = container;
        }

        [MenuItem("CONTEXT/RectTransform/Add Auto-Fit Layout Container")]
        public static void AddAutoFitToSelected(MenuCommand command)
        {
            RectTransform rectTransform = command.context as RectTransform;
            if (rectTransform != null)
            {
                Undo.AddComponent<AutoFitLayoutContainer>(rectTransform.gameObject);
            }
        }

        [MenuItem("Tools/RTS/Create Auto-Fit Layout Package")]
        public static void CreatePackage()
        {
            string message = "Auto-Fit Layout Container is already set up as a reusable package!\n\n" +
                            "To use in other projects:\n" +
                            "1. Copy the 'AutoFitLayout' folder\n" +
                            "2. Paste into your new project's Assets folder\n" +
                            "3. Use via: GameObject > UI > Auto-Fit Layout Container\n\n" +
                            "The package includes:\n" +
                            "• AutoFitLayoutContainer.cs (main component)\n" +
                            "• AutoFitLayoutContainerEditor.cs (custom inspector)\n" +
                            "• Works with any layout type\n" +
                            "• Supports Square, Rectangle, Circle, Triangle shapes";

            EditorUtility.DisplayDialog("Auto-Fit Layout Package", message, "OK");
        }
    }
}
