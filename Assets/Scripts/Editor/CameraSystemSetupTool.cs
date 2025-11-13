using UnityEngine;
using UnityEditor;

namespace RTS.Editor
{
    /// <summary>
    /// Automation tool for setting up RTS camera systems.
    /// Configures camera, input bindings, and movement settings.
    /// Access via: Tools > RTS > Camera System Setup
    /// </summary>
    public class CameraSystemSetupTool : EditorWindow
    {
        private enum SetupMode
        {
            CreateNew,
            ConfigureExisting
        }

        private SetupMode setupMode = SetupMode.CreateNew;
        private Camera existingCamera;

        [Header("Camera Settings")]
        private string cameraName = "RTSCamera";
        private Vector3 cameraPosition = new Vector3(0, 15, -10);
        private Vector3 cameraRotation = new Vector3(45, 0, 0);
        private bool orthographic = false;
        private float fieldOfView = 60f;
        private float orthographicSize = 20f;

        [Header("Movement Settings")]
        private float moveSpeed = 15f;
        private float edgeScrollSpeed = 20f;
        private bool useEdgeScroll = true;
        private float panBorderThickness = 10f;

        [Header("Zoom Settings")]
        private float zoomSpeed = 50f;
        private float minZoom = 15f;
        private float maxZoom = 80f;

        [Header("Rotation Settings")]
        private float rotationSpeed = 60f;

        [Header("Bounds")]
        private Vector2 minPosition = new Vector2(-1000, -1000);
        private Vector2 maxPosition = new Vector2(1000, 1000);

        private Vector2 scrollPos;

        [MenuItem("Tools/RTS/Camera System Setup")]
        public static void ShowWindow()
        {
            CameraSystemSetupTool window = GetWindow<CameraSystemSetupTool>("Camera System Setup");
            window.minSize = new Vector2(450, 650);
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            GUILayout.Label("Camera System Setup Tool", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Automatically sets up an RTS camera with movement, zoom, rotation, and input bindings.\n" +
                "Creates or configures a camera with RTSCameraController component.",
                MessageType.Info);

            GUILayout.Space(10);

            // Mode Selection
            setupMode = (SetupMode)EditorGUILayout.EnumPopup("Setup Mode", setupMode);

            GUILayout.Space(10);

            if (setupMode == SetupMode.ConfigureExisting)
            {
                existingCamera = (Camera)EditorGUILayout.ObjectField("Existing Camera", existingCamera, typeof(Camera), true);

                if (existingCamera == null)
                {
                    EditorGUILayout.HelpBox("Please assign an existing camera to configure.", MessageType.Warning);
                }
            }
            else
            {
                // Camera Creation Settings
                GUILayout.Label("Camera Settings", EditorStyles.boldLabel);
                cameraName = EditorGUILayout.TextField("Camera Name", cameraName);
                cameraPosition = EditorGUILayout.Vector3Field("Position", cameraPosition);
                cameraRotation = EditorGUILayout.Vector3Field("Rotation", cameraRotation);

                orthographic = EditorGUILayout.Toggle("Orthographic", orthographic);
                if (orthographic)
                {
                    orthographicSize = EditorGUILayout.FloatField("Orthographic Size", orthographicSize);
                }
                else
                {
                    fieldOfView = EditorGUILayout.FloatField("Field of View", fieldOfView);
                }
            }

            GUILayout.Space(10);

            // Movement Settings
            GUILayout.Label("Movement Settings", EditorStyles.boldLabel);
            moveSpeed = EditorGUILayout.FloatField("Move Speed", moveSpeed);
            useEdgeScroll = EditorGUILayout.Toggle("Use Edge Scroll", useEdgeScroll);
            if (useEdgeScroll)
            {
                edgeScrollSpeed = EditorGUILayout.FloatField("Edge Scroll Speed", edgeScrollSpeed);
                panBorderThickness = EditorGUILayout.FloatField("Pan Border Thickness", panBorderThickness);
            }

            GUILayout.Space(10);

            // Zoom Settings
            GUILayout.Label("Zoom Settings", EditorStyles.boldLabel);
            zoomSpeed = EditorGUILayout.FloatField("Zoom Speed", zoomSpeed);
            minZoom = EditorGUILayout.FloatField("Min Zoom", minZoom);
            maxZoom = EditorGUILayout.FloatField("Max Zoom", maxZoom);

            GUILayout.Space(10);

            // Rotation Settings
            GUILayout.Label("Rotation Settings", EditorStyles.boldLabel);
            rotationSpeed = EditorGUILayout.FloatField("Rotation Speed", rotationSpeed);

            GUILayout.Space(10);

            // Bounds
            GUILayout.Label("Camera Bounds", EditorStyles.boldLabel);
            minPosition = EditorGUILayout.Vector2Field("Min Position (X, Z)", minPosition);
            maxPosition = EditorGUILayout.Vector2Field("Max Position (X, Z)", maxPosition);

            GUILayout.Space(20);

            // Setup Button
            string buttonText = setupMode == SetupMode.CreateNew ? "Create RTS Camera" : "Configure Camera";
            GUI.enabled = setupMode == SetupMode.CreateNew || existingCamera != null;

            if (GUILayout.Button(buttonText, GUILayout.Height(40)))
            {
                SetupCamera();
            }

            GUI.enabled = true;

            EditorGUILayout.EndScrollView();
        }

        private void SetupCamera()
        {
            Camera camera;
            GameObject cameraObj;

            if (setupMode == SetupMode.ConfigureExisting && existingCamera != null)
            {
                camera = existingCamera;
                cameraObj = camera.gameObject;
            }
            else
            {
                // Create new camera
                cameraObj = new GameObject(cameraName);
                camera = cameraObj.AddComponent<Camera>();

                // Set position and rotation
                cameraObj.transform.position = cameraPosition;
                cameraObj.transform.eulerAngles = cameraRotation;

                // Camera settings
                camera.orthographic = orthographic;
                if (orthographic)
                {
                    camera.orthographicSize = orthographicSize;
                }
                else
                {
                    camera.fieldOfView = fieldOfView;
                }

                // Tag as MainCamera
                cameraObj.tag = "MainCamera";

                Debug.Log($"✅ Created new camera: {cameraName}");
            }

            // Add or get RTSCameraController
            RTSCameraController controller = cameraObj.GetComponent<RTSCameraController>();
            if (controller == null)
            {
                controller = cameraObj.AddComponent<RTSCameraController>();
                Debug.Log("✅ Added RTSCameraController component");
            }

            // Configure controller via SerializedObject
            SerializedObject so = new SerializedObject(controller);

            so.FindProperty("moveSpeed").floatValue = moveSpeed;
            so.FindProperty("edgeScrollSpeed").floatValue = edgeScrollSpeed;
            so.FindProperty("useEdgeScroll").boolValue = useEdgeScroll;
            so.FindProperty("panBorderThickness").floatValue = panBorderThickness;

            so.FindProperty("zoomSpeed").floatValue = zoomSpeed;
            so.FindProperty("minZoom").floatValue = minZoom;
            so.FindProperty("maxZoom").floatValue = maxZoom;

            so.FindProperty("rotationSpeed").floatValue = rotationSpeed;

            so.FindProperty("minPosition").vector2Value = minPosition;
            so.FindProperty("maxPosition").vector2Value = maxPosition;

            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(controller);
            Selection.activeGameObject = cameraObj;
            EditorGUIUtility.PingObject(cameraObj);

            Debug.Log("✅ Camera system configured successfully!");

            EditorUtility.DisplayDialog("Success!",
                "RTS Camera configured successfully!\n\n" +
                "Controls:\n" +
                "• WASD / Arrow Keys: Move\n" +
                "• Mouse Wheel: Zoom\n" +
                "• Q/E: Rotate\n" +
                "• Middle Mouse: Drag\n" +
                "• Edge Scrolling: " + (useEdgeScroll ? "Enabled" : "Disabled"),
                "OK");
        }
    }
}
