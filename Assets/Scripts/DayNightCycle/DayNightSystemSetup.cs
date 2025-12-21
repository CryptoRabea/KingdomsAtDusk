using UnityEngine;

namespace RTS.DayNightCycle
{
    /// <summary>
    /// Helper component for quickly setting up the complete Day-Night Cycle system.
    /// Attach this to a parent GameObject to organize all day-night components.
    /// </summary>
    public class DayNightSystemSetup : MonoBehaviour
    {
        [Header("=== CONFIGURATION ===")]
        [Tooltip("The configuration asset for the day-night cycle")]
        [SerializeField] private DayNightConfigSO config;

        [Header("=== CORE COMPONENTS ===")]
        [Tooltip("Reference to the DayNightCycleManager")]
        [SerializeField] private DayNightCycleManager cycleManager;

        [Tooltip("Reference to the CelestialController")]
        [SerializeField] private CelestialController celestialController;

        [Tooltip("Reference to the DayNightLightingController")]
        [SerializeField] private DayNightLightingController lightingController;

        [Tooltip("Reference to the DayNightAmbientController")]
        [SerializeField] private DayNightAmbientController ambientController;

        [Header("=== SCENE REFERENCES ===")]
        [Tooltip("Main directional light (sun)")]
        [SerializeField] private Light sunLight;

        [Tooltip("Moon directional light (optional)")]
        [SerializeField] private Light moonLight;

        [Header("=== DEBUG ===")]
        [SerializeField] private bool showDebugPanel = true;

        #region Public Properties

        public DayNightConfigSO Config
        {
            get => config;
            set => config = value;
        }

        public DayNightCycleManager CycleManager => cycleManager;
        public CelestialController CelestialController => celestialController;
        public DayNightLightingController LightingController => lightingController;
        public DayNightAmbientController AmbientController => ambientController;

        #endregion

        #region Unity Lifecycle

        private void OnValidate()
        {
            // Auto-find components in children
            if (cycleManager == null)
                cycleManager = GetComponentInChildren<DayNightCycleManager>();

            if (celestialController == null)
                celestialController = GetComponentInChildren<CelestialController>();

            if (lightingController == null)
                lightingController = GetComponentInChildren<DayNightLightingController>();

            if (ambientController == null)
                ambientController = GetComponentInChildren<DayNightAmbientController>();
        }

        #endregion

        #region Editor Setup

#if UNITY_EDITOR
        /// <summary>
        /// Creates a complete Day-Night system hierarchy.
        /// Call this from the Unity Editor menu or context menu.
        /// </summary>
        [ContextMenu("Setup Day-Night System")]
        public void SetupSystem()
        {
            // Create Cycle Manager
            if (cycleManager == null)
            {
                GameObject cycleObj = new GameObject("DayNightCycleManager");
                cycleObj.transform.SetParent(transform);
                cycleObj.transform.localPosition = Vector3.zero;
                cycleManager = cycleObj.AddComponent<DayNightCycleManager>();
            }

            // Create Celestial Controller
            if (celestialController == null)
            {
                GameObject celestialObj = new GameObject("CelestialController");
                celestialObj.transform.SetParent(transform);
                celestialObj.transform.localPosition = Vector3.zero;
                celestialController = celestialObj.AddComponent<CelestialController>();
            }

            // Create Lighting Controller
            if (lightingController == null)
            {
                GameObject lightingObj = new GameObject("DayNightLightingController");
                lightingObj.transform.SetParent(transform);
                lightingObj.transform.localPosition = Vector3.zero;
                lightingController = lightingObj.AddComponent<DayNightLightingController>();
            }

            // Create Ambient Controller
            if (ambientController == null)
            {
                GameObject ambientObj = new GameObject("DayNightAmbientController");
                ambientObj.transform.SetParent(transform);
                ambientObj.transform.localPosition = Vector3.zero;
                ambientController = ambientObj.AddComponent<DayNightAmbientController>();
            }

            // Auto-find sun light if not assigned
            if (sunLight == null)
            {
                Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                foreach (Light light in lights)
                {
                    if (light.type == LightType.Directional)
                    {
                        sunLight = light;
                        break;
                    }
                }
            }

            Debug.Log("[DayNightSystemSetup] Day-Night system components created successfully!");
            Debug.Log("Next steps:");
            Debug.Log("1. Create a DayNightConfigSO asset (Right-click > Create > RTS > Day Night Cycle > Config)");
            Debug.Log("2. Assign the config to the DayNightCycleManager");
            Debug.Log("3. Assign your directional light to CelestialController and LightingController");
            Debug.Log("4. (Optional) Create a UI canvas with TimeDisplayUI component");
        }

        [ContextMenu("Auto-Assign Lights")]
        public void AutoAssignLights()
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    if (light.gameObject.name.ToLower().Contains("sun") || sunLight == null)
                    {
                        sunLight = light;
                    }
                    else if (light.gameObject.name.ToLower().Contains("moon"))
                    {
                        moonLight = light;
                    }
                }
            }

            if (sunLight != null)
            {
                Debug.Log($"[DayNightSystemSetup] Found sun light: {sunLight.name}");
            }
            if (moonLight != null)
            {
                Debug.Log($"[DayNightSystemSetup] Found moon light: {moonLight.name}");
            }
        }

        private void OnGUI()
        {
            if (!showDebugPanel || cycleManager == null) return;

            GUILayout.BeginArea(new Rect(Screen.width - 200, 10, 190, 150));
            GUILayout.BeginVertical("box");

            GUILayout.Label("=== Quick Time Controls ===");

            if (GUILayout.Button("Skip to Dawn"))
                cycleManager.SkipToPhase(DayPhase.Dawn);

            if (GUILayout.Button("Skip to Noon"))
                cycleManager.SetTime(12f);

            if (GUILayout.Button("Skip to Dusk"))
                cycleManager.SkipToPhase(DayPhase.Dusk);

            if (GUILayout.Button("Skip to Midnight"))
                cycleManager.SetTime(0f);

            GUILayout.Space(5);

            if (GUILayout.Button(cycleManager.IsTimePaused ? "Resume Time" : "Pause Time"))
            {
                if (cycleManager.IsTimePaused)
                    cycleManager.ResumeTime();
                else
                    cycleManager.PauseTime();
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
#endif

        #endregion
    }
}
