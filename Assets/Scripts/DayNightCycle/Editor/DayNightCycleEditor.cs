#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace RTS.DayNightCycle.Editor
{
    /// <summary>
    /// Editor utilities for the Day-Night Cycle system.
    /// Provides menu items for quick setup and configuration.
    /// </summary>
    public class DayNightCycleEditor
    {
        [MenuItem("GameObject/RTS/Day-Night Cycle/Create Complete System", false, 10)]
        public static void CreateCompleteDayNightSystem()
        {
            // Create parent object
            GameObject parent = new GameObject("DayNightSystem");
            Undo.RegisterCreatedObjectUndo(parent, "Create Day-Night System");

            // Add setup component
            DayNightSystemSetup setup = parent.AddComponent<DayNightSystemSetup>();

            // Create Cycle Manager
            GameObject cycleObj = new GameObject("DayNightCycleManager");
            cycleObj.transform.SetParent(parent.transform);
            DayNightCycleManager cycleManager = cycleObj.AddComponent<DayNightCycleManager>();

            // Create Celestial Controller
            GameObject celestialObj = new GameObject("CelestialController");
            celestialObj.transform.SetParent(parent.transform);
            CelestialController celestialController = celestialObj.AddComponent<CelestialController>();

            // Create Lighting Controller
            GameObject lightingObj = new GameObject("DayNightLightingController");
            lightingObj.transform.SetParent(parent.transform);
            DayNightLightingController lightingController = lightingObj.AddComponent<DayNightLightingController>();

            // Create Ambient Controller
            GameObject ambientObj = new GameObject("DayNightAmbientController");
            ambientObj.transform.SetParent(parent.transform);
            DayNightAmbientController ambientController = ambientObj.AddComponent<DayNightAmbientController>();

            // Select the created object
            Selection.activeGameObject = parent;

            Debug.Log("[Day-Night Cycle] Complete system created!");
            Debug.Log("Next steps:");
            Debug.Log("1. Create a DayNightConfigSO: Right-click in Project > Create > RTS > Day Night Cycle > Config");
            Debug.Log("2. Assign the config to DayNightCycleManager");
            Debug.Log("3. Assign your Directional Light (sun) to CelestialController and LightingController");

            EditorUtility.DisplayDialog("Day-Night System Created",
                "The Day-Night Cycle system has been created.\n\n" +
                "Next steps:\n" +
                "1. Create a DayNightConfigSO asset\n" +
                "2. Assign it to the DayNightCycleManager\n" +
                "3. Assign your sun light to the controllers",
                "OK");
        }

        [MenuItem("GameObject/RTS/Day-Night Cycle/Add Cycle Manager Only", false, 11)]
        public static void AddCycleManagerOnly()
        {
            GameObject obj = new GameObject("DayNightCycleManager");
            Undo.RegisterCreatedObjectUndo(obj, "Add Day-Night Cycle Manager");
            obj.AddComponent<DayNightCycleManager>();
            Selection.activeGameObject = obj;
        }

        [MenuItem("GameObject/RTS/Day-Night Cycle/Add Celestial Controller Only", false, 12)]
        public static void AddCelestialControllerOnly()
        {
            GameObject obj = new GameObject("CelestialController");
            Undo.RegisterCreatedObjectUndo(obj, "Add Celestial Controller");
            obj.AddComponent<CelestialController>();
            Selection.activeGameObject = obj;
        }

        [MenuItem("GameObject/RTS/Day-Night Cycle/Add Lighting Controller Only", false, 13)]
        public static void AddLightingControllerOnly()
        {
            GameObject obj = new GameObject("DayNightLightingController");
            Undo.RegisterCreatedObjectUndo(obj, "Add Day-Night Lighting Controller");
            obj.AddComponent<DayNightLightingController>();
            Selection.activeGameObject = obj;
        }

        [MenuItem("GameObject/RTS/Day-Night Cycle/Add Ambient Controller Only", false, 14)]
        public static void AddAmbientControllerOnly()
        {
            GameObject obj = new GameObject("DayNightAmbientController");
            Undo.RegisterCreatedObjectUndo(obj, "Add Day-Night Ambient Controller");
            obj.AddComponent<DayNightAmbientController>();
            Selection.activeGameObject = obj;
        }

        [MenuItem("GameObject/RTS/Day-Night Cycle/Add Time Display UI", false, 20)]
        public static void AddTimeDisplayUI()
        {
            // Find or create canvas
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                // Create canvas
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            // Create Time Display UI
            GameObject timeUI = new GameObject("TimeDisplayUI");
            timeUI.transform.SetParent(canvas.transform);
            TimeDisplayUI displayUI = timeUI.AddComponent<TimeDisplayUI>();

            // Set up RectTransform for top-right corner
            RectTransform rect = timeUI.GetComponent<RectTransform>();
            if (rect == null)
                rect = timeUI.AddComponent<RectTransform>();

            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-20, -20);
            rect.sizeDelta = new Vector2(200, 80);

            Selection.activeGameObject = timeUI;
            Undo.RegisterCreatedObjectUndo(timeUI, "Add Time Display UI");

            Debug.Log("[Day-Night Cycle] Time Display UI created. You'll need to add TextMeshProUGUI components for time, day, and phase display.");
        }
    }

    /// <summary>
    /// Custom inspector for DayNightCycleManager.
    /// </summary>
    [CustomEditor(typeof(DayNightCycleManager))]
    public class DayNightCycleManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            DayNightCycleManager manager = (DayNightCycleManager)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Runtime controls available in Play Mode", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Dawn"))
                manager.SkipToPhase(DayPhase.Dawn);
            if (GUILayout.Button("Noon"))
                manager.SetTime(12f);
            if (GUILayout.Button("Dusk"))
                manager.SkipToPhase(DayPhase.Dusk);
            if (GUILayout.Button("Night"))
                manager.SkipToPhase(DayPhase.Night);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(manager.IsTimePaused ? "Resume" : "Pause"))
            {
                if (manager.IsTimePaused)
                    manager.ResumeTime();
                else
                    manager.PauseTime();
            }

            if (GUILayout.Button("0.5x"))
                manager.SetTimeScale(0.5f);
            if (GUILayout.Button("1x"))
                manager.SetTimeScale(1f);
            if (GUILayout.Button("2x"))
                manager.SetTimeScale(2f);
            if (GUILayout.Button("5x"))
                manager.SetTimeScale(5f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Current State", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Time:", manager.GetFormattedTime());
            EditorGUILayout.LabelField("Day:", manager.CurrentDay.ToString());
            EditorGUILayout.LabelField("Phase:", manager.CurrentPhase.ToString());
            EditorGUILayout.LabelField("Day Progress:", $"{manager.DayProgress:P1}");
            EditorGUILayout.LabelField("Time Scale:", $"{manager.TimeScale:F2}x");

            // Force repaint for live updates
            if (Application.isPlaying)
            {
                Repaint();
            }
        }
    }

    /// <summary>
    /// Custom inspector for DayNightConfigSO.
    /// </summary>
    [CustomEditor(typeof(DayNightConfigSO))]
    public class DayNightConfigEditor : UnityEditor.Editor
    {
        private float previewHour = 12f;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            DayNightConfigSO config = (DayNightConfigSO)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            previewHour = EditorGUILayout.Slider("Preview Hour", previewHour, 0f, 24f);

            DayPhase phase = config.GetPhaseForHour(previewHour);
            float phaseProgress = config.GetPhaseProgress(previewHour);
            Color sunColor = config.GetSunColorForHour(previewHour);
            float sunIntensity = config.GetSunIntensityForHour(previewHour);
            Color ambientColor = config.GetAmbientColorForHour(previewHour);

            EditorGUILayout.LabelField($"Phase: {phase} ({phaseProgress:P0})");
            EditorGUILayout.LabelField($"Sun Intensity: {sunIntensity:F2}");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Sun Color:", GUILayout.Width(80));
            EditorGUILayout.ColorField(GUIContent.none, sunColor, false, false, false, GUILayout.Width(60));
            EditorGUILayout.LabelField("Ambient:", GUILayout.Width(60));
            EditorGUILayout.ColorField(GUIContent.none, ambientColor, false, false, false, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Time Calculations", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Seconds per hour: {config.SecondsPerHour:F1}s");
            EditorGUILayout.LabelField($"Hours per second: {config.HoursPerSecond:F4}");
            EditorGUILayout.LabelField($"Full cycle: {config.DayDurationInSeconds:F0}s ({config.DayDurationInSeconds / 60f:F1}min)");
        }
    }
}
#endif
