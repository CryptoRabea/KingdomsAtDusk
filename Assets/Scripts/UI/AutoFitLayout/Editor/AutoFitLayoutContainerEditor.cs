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
        private SerializedProperty maxWidthProp;
        private SerializedProperty maxHeightProp;
        private SerializedProperty paddingProp;
        private SerializedProperty minContentSizeProp;
        private SerializedProperty maxContentSizeProp;
        private SerializedProperty autoDetectLayoutProp;
        private SerializedProperty circleInscribeFactorProp;
        private SerializedProperty triangleAspectRatioProp;
        private SerializedProperty updateInEditModeProp;
        private SerializedProperty debugModeProp;

        private void OnEnable()
        {
            shapeProp = serializedObject.FindProperty("shape");
            maxWidthProp = serializedObject.FindProperty("maxWidth");
            maxHeightProp = serializedObject.FindProperty("maxHeight");
            paddingProp = serializedObject.FindProperty("padding");
            minContentSizeProp = serializedObject.FindProperty("minContentSize");
            maxContentSizeProp = serializedObject.FindProperty("maxContentSize");
            autoDetectLayoutProp = serializedObject.FindProperty("autoDetectLayout");
            circleInscribeFactorProp = serializedObject.FindProperty("circleInscribeFactor");
            triangleAspectRatioProp = serializedObject.FindProperty("triangleAspectRatio");
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
                "This component automatically scales container contents to fit within the specified shape. " +
                "Works with Grid, Horizontal, Vertical layouts, or manual arrangement.",
                MessageType.Info
            );

            EditorGUILayout.Space();

            // Container Settings
            EditorGUILayout.LabelField("Container Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(shapeProp, new GUIContent("Shape", "Shape of the container"));

            // Show appropriate size fields based on shape
            AutoFitLayoutContainer.ContainerShape shape = (AutoFitLayoutContainer.ContainerShape)shapeProp.enumValueIndex;

            if (shape == AutoFitLayoutContainer.ContainerShape.Square ||
                shape == AutoFitLayoutContainer.ContainerShape.Circle)
            {
                EditorGUILayout.PropertyField(maxWidthProp, new GUIContent("Max Size", "Maximum size (width = height)"));
                maxHeightProp.floatValue = maxWidthProp.floatValue;
            }
            else
            {
                EditorGUILayout.PropertyField(maxWidthProp, new GUIContent("Max Width"));
                EditorGUILayout.PropertyField(maxHeightProp, new GUIContent("Max Height"));
            }

            EditorGUILayout.PropertyField(paddingProp, new GUIContent("Padding", "Inner padding"));

            EditorGUILayout.Space();

            // Content Scaling
            EditorGUILayout.LabelField("Content Scaling", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(minContentSizeProp, new GUIContent("Min Content Size", "Minimum size for content elements"));
            EditorGUILayout.PropertyField(maxContentSizeProp, new GUIContent("Max Content Size", "Maximum size for content elements"));
            EditorGUILayout.PropertyField(autoDetectLayoutProp, new GUIContent("Auto-Detect Layout", "Automatically detect layout component"));

            EditorGUILayout.Space();

            // Shape-specific settings
            if (shape == AutoFitLayoutContainer.ContainerShape.Circle)
            {
                EditorGUILayout.LabelField("Circle Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(circleInscribeFactorProp, new GUIContent("Inscribe Factor", "How much to reduce usable area (0.707 = inscribed square)"));
            }
            else if (shape == AutoFitLayoutContainer.ContainerShape.Triangle)
            {
                EditorGUILayout.LabelField("Triangle Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(triangleAspectRatioProp, new GUIContent("Aspect Ratio", "Triangle height relative to width (0.866 = equilateral)"));
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

            GridLayoutGroup grid = container.GetComponent<GridLayoutGroup>();
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
            maxWidthProp.floatValue = 300f;
            maxHeightProp.floatValue = 300f;
            paddingProp.floatValue = 10f;
            minContentSizeProp.floatValue = 16f;
            maxContentSizeProp.floatValue = 128f;
            autoDetectLayoutProp.boolValue = true;
            circleInscribeFactorProp.floatValue = 0.707f;
            triangleAspectRatioProp.floatValue = 0.866f;
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
