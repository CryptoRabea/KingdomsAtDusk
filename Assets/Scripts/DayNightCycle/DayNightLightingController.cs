using UnityEngine;
using UnityEngine.Rendering;
using RTS.Core;
using RTS.Core.Events;

namespace RTS.DayNightCycle
{
    /// <summary>
    /// Controls all lighting aspects of the day-night cycle including:
    /// - Directional light color and intensity (sun/moon)
    /// - Ambient lighting color and intensity
    /// - Fog color and density
    /// - Skybox parameters
    /// - Reflection probe updates
    /// </summary>
    public class DayNightLightingController : MonoBehaviour
    {
        [Header("=== LIGHT REFERENCES ===")]
        [Tooltip("Main directional light (sun)")]
        [SerializeField] private Light sunLight;

        [Tooltip("Secondary directional light (moon, optional)")]
        [SerializeField] private Light moonLight;

        [Tooltip("Additional fill lights that respond to day-night cycle")]
        [SerializeField] private Light[] additionalLights;

        [Header("=== SKYBOX SETTINGS ===")]
        [Tooltip("Skybox material to modify")]
        [SerializeField] private Material skyboxMaterial;

        [Tooltip("Property name for skybox exposure")]
        [SerializeField] private string skyboxExposureProperty = "_Exposure";

        [Tooltip("Property name for skybox tint")]
        [SerializeField] private string skyboxTintProperty = "_Tint";

        [Tooltip("Property name for skybox rotation")]
        [SerializeField] private string skyboxRotationProperty = "_Rotation";

        [Tooltip("Rotate skybox with time")]
        [SerializeField] private bool rotateSkybox = false;

        [Tooltip("Skybox rotation speed (degrees per game hour)")]
        [SerializeField] private float skyboxRotationSpeed = 15f;

        [Header("=== REFLECTION PROBES ===")]
        [Tooltip("Reflection probes to update with lighting changes")]
        [SerializeField] private ReflectionProbe[] reflectionProbes;

        [Tooltip("How often to update reflection probes (in game hours)")]
        [SerializeField, Range(0.1f, 6f)] private float reflectionUpdateInterval = 1f;

        [Header("=== TRANSITION SETTINGS ===")]
        [Tooltip("Speed of color/intensity transitions")]
        [SerializeField, Range(0.1f, 20f)] private float transitionSpeed = 5f;

        [Tooltip("Use smooth transitions instead of instant")]
        [SerializeField] private bool useSmoothTransitions = true;

        [Header("=== FOG SETTINGS ===")]
        [Tooltip("Control scene fog")]
        [SerializeField] private bool controlFog = true;

        [Tooltip("Control fog start/end distance based on time")]
        [SerializeField] private bool controlFogDistance = false;

        [Tooltip("Fog start distance during day")]
        [SerializeField] private float fogStartDistanceDay = 50f;

        [Tooltip("Fog start distance during night")]
        [SerializeField] private float fogStartDistanceNight = 30f;

        [Tooltip("Fog end distance during day")]
        [SerializeField] private float fogEndDistanceDay = 500f;

        [Tooltip("Fog end distance during night")]
        [SerializeField] private float fogEndDistanceNight = 300f;

        [Header("=== SHADOW SETTINGS ===")]
        [Tooltip("Adjust shadow strength based on time")]
        [SerializeField] private bool controlShadowStrength = true;

        [Tooltip("Shadow strength during full day")]
        [SerializeField, Range(0f, 1f)] private float shadowStrengthDay = 1f;

        [Tooltip("Shadow strength during dawn/dusk")]
        [SerializeField, Range(0f, 1f)] private float shadowStrengthDawnDusk = 0.7f;

        [Tooltip("Shadow strength during night")]
        [SerializeField, Range(0f, 1f)] private float shadowStrengthNight = 0.3f;

        [Header("=== DEBUG ===")]
        [SerializeField] private bool showDebugInfo = false;

        // ===== Private State =====
        private DayNightCycleManager cycleManager;
        private DayNightConfigSO config;

        // Current values (for smooth transitions)
        private Color currentSunColor;
        private float currentSunIntensity;
        private Color currentMoonColor;
        private float currentMoonIntensity;
        private Color currentAmbientColor;
        private float currentAmbientIntensity;
        private Color currentFogColor;
        private float currentFogDensity;
        private float currentSkyboxExposure;
        private Color currentSkyboxTint;

        // Target values
        private Color targetSunColor;
        private float targetSunIntensity;
        private Color targetMoonColor;
        private float targetMoonIntensity;
        private Color targetAmbientColor;
        private float targetAmbientIntensity;
        private Color targetFogColor;
        private float targetFogDensity;
        private float targetSkyboxExposure;
        private Color targetSkyboxTint;

        // Reflection probe timing
        private float lastReflectionUpdate;

        // Cached material property IDs
        private int exposurePropertyId;
        private int tintPropertyId;
        private int rotationPropertyId;
        private bool hasSkyboxMaterial;

        #region Unity Lifecycle

        private void Start()
        {
            Initialize();
            SubscribeToEvents();
            ApplyImmediateSettings();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            if (cycleManager == null || config == null) return;

            UpdateTargetValues();
            UpdateLighting();
            UpdateSkybox();
            UpdateFog();
            CheckReflectionProbeUpdate();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            // Get cycle manager reference
            cycleManager = DayNightCycleManager.Instance;
            if (cycleManager == null)
            {
                cycleManager = FindAnyObjectByType<DayNightCycleManager>();
            }

            if (cycleManager != null)
            {
                config = cycleManager.Config;
            }
            else
            {
                Debug.LogWarning("[DayNightLightingController] DayNightCycleManager not found!");
                enabled = false;
                return;
            }

            // Cache material property IDs
            if (skyboxMaterial != null)
            {
                hasSkyboxMaterial = true;
                exposurePropertyId = Shader.PropertyToID(skyboxExposureProperty);
                tintPropertyId = Shader.PropertyToID(skyboxTintProperty);
                rotationPropertyId = Shader.PropertyToID(skyboxRotationProperty);
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

            // Initialize current values
            if (sunLight != null)
            {
                currentSunColor = sunLight.color;
                currentSunIntensity = sunLight.intensity;
            }

            if (moonLight != null)
            {
                currentMoonColor = moonLight.color;
                currentMoonIntensity = moonLight.intensity;
            }

            currentAmbientColor = RenderSettings.ambientLight;
            currentAmbientIntensity = RenderSettings.ambientIntensity;
            currentFogColor = RenderSettings.fogColor;
            currentFogDensity = RenderSettings.fogDensity;

            if (hasSkyboxMaterial)
            {
                currentSkyboxExposure = skyboxMaterial.GetFloat(exposurePropertyId);
                currentSkyboxTint = skyboxMaterial.GetColor(tintPropertyId);
            }
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<DayPhaseChangedEvent>(OnPhaseChanged);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<DayPhaseChangedEvent>(OnPhaseChanged);
        }

        private void ApplyImmediateSettings()
        {
            if (cycleManager == null || config == null) return;

            // Set initial values immediately
            UpdateTargetValues();

            if (!useSmoothTransitions)
            {
                ApplyValuesImmediate();
            }
            else
            {
                // Set current to target for instant application on first frame
                currentSunColor = targetSunColor;
                currentSunIntensity = targetSunIntensity;
                currentMoonColor = targetMoonColor;
                currentMoonIntensity = targetMoonIntensity;
                currentAmbientColor = targetAmbientColor;
                currentAmbientIntensity = targetAmbientIntensity;
                currentFogColor = targetFogColor;
                currentFogDensity = targetFogDensity;
                currentSkyboxExposure = targetSkyboxExposure;
                currentSkyboxTint = targetSkyboxTint;

                ApplyValuesImmediate();
            }
        }

        #endregion

        #region Event Handlers

        private void OnPhaseChanged(DayPhaseChangedEvent evt)
        {
            // Force reflection probe update on phase change
            UpdateReflectionProbes();
        }

        #endregion

        #region Lighting Updates

        private void UpdateTargetValues()
        {
            float currentTime = cycleManager.CurrentTime;
            DayPhase currentPhase = cycleManager.CurrentPhase;

            // Sun
            targetSunColor = config.GetSunColorForHour(currentTime);
            targetSunIntensity = config.GetSunIntensityForHour(currentTime);

            // Moon
            targetMoonColor = config.MoonColorNight;
            targetMoonIntensity = cycleManager.GetCurrentMoonIntensity();

            // Ambient
            targetAmbientColor = config.GetAmbientColorForHour(currentTime);
            targetAmbientIntensity = CalculateAmbientIntensity(currentPhase);

            // Fog
            if (config.EnableDynamicFog)
            {
                targetFogColor = CalculateFogColor(currentPhase);
                targetFogDensity = CalculateFogDensity(currentPhase);
            }

            // Skybox
            if (config.EnableSkyboxTransitions)
            {
                targetSkyboxExposure = CalculateSkyboxExposure(currentPhase);
                targetSkyboxTint = CalculateSkyboxTint(currentPhase, currentTime);
            }
        }

        private void UpdateLighting()
        {
            float deltaTime = Time.deltaTime;
            float lerpFactor = useSmoothTransitions ? deltaTime * transitionSpeed : 1f;

            // Interpolate sun
            currentSunColor = Color.Lerp(currentSunColor, targetSunColor, lerpFactor);
            currentSunIntensity = Mathf.Lerp(currentSunIntensity, targetSunIntensity, lerpFactor);

            // Apply sun
            if (sunLight != null)
            {
                sunLight.color = currentSunColor;
                sunLight.intensity = currentSunIntensity;

                if (controlShadowStrength)
                {
                    sunLight.shadowStrength = CalculateShadowStrength(cycleManager.CurrentPhase);
                }
            }

            // Interpolate and apply moon
            if (moonLight != null)
            {
                currentMoonColor = Color.Lerp(currentMoonColor, targetMoonColor, lerpFactor);
                currentMoonIntensity = Mathf.Lerp(currentMoonIntensity, targetMoonIntensity, lerpFactor);

                moonLight.color = currentMoonColor;
                moonLight.intensity = currentMoonIntensity;

                if (controlShadowStrength)
                {
                    moonLight.shadowStrength = shadowStrengthNight;
                }
            }

            // Interpolate and apply ambient
            currentAmbientColor = Color.Lerp(currentAmbientColor, targetAmbientColor, lerpFactor);
            currentAmbientIntensity = Mathf.Lerp(currentAmbientIntensity, targetAmbientIntensity, lerpFactor);

            RenderSettings.ambientLight = currentAmbientColor;
            RenderSettings.ambientIntensity = currentAmbientIntensity;

            // Update additional lights
            UpdateAdditionalLights(lerpFactor);
        }

        private void UpdateAdditionalLights(float lerpFactor)
        {
            if (additionalLights == null || additionalLights.Length == 0) return;

            float multiplier = CalculateLightMultiplier(cycleManager.CurrentPhase);

            foreach (Light light in additionalLights)
            {
                if (light == null) continue;

                // Smoothly adjust intensity
                float targetIntensity = light.intensity * multiplier;
                light.intensity = Mathf.Lerp(light.intensity, targetIntensity, lerpFactor * 0.1f);
            }
        }

        private void ApplyValuesImmediate()
        {
            if (sunLight != null)
            {
                sunLight.color = currentSunColor;
                sunLight.intensity = currentSunIntensity;
            }

            if (moonLight != null)
            {
                moonLight.color = currentMoonColor;
                moonLight.intensity = currentMoonIntensity;
            }

            RenderSettings.ambientLight = currentAmbientColor;
            RenderSettings.ambientIntensity = currentAmbientIntensity;

            if (controlFog)
            {
                RenderSettings.fogColor = currentFogColor;
                RenderSettings.fogDensity = currentFogDensity;
            }

            if (hasSkyboxMaterial)
            {
                skyboxMaterial.SetFloat(exposurePropertyId, currentSkyboxExposure);
                skyboxMaterial.SetColor(tintPropertyId, currentSkyboxTint);
            }
        }

        #endregion

        #region Skybox Updates

        private void UpdateSkybox()
        {
            if (!hasSkyboxMaterial || !config.EnableSkyboxTransitions) return;

            float lerpFactor = useSmoothTransitions ? Time.deltaTime * transitionSpeed : 1f;

            // Interpolate values
            currentSkyboxExposure = Mathf.Lerp(currentSkyboxExposure, targetSkyboxExposure, lerpFactor);
            currentSkyboxTint = Color.Lerp(currentSkyboxTint, targetSkyboxTint, lerpFactor);

            // Apply to material
            skyboxMaterial.SetFloat(exposurePropertyId, currentSkyboxExposure);
            skyboxMaterial.SetColor(tintPropertyId, currentSkyboxTint);

            // Rotate skybox
            if (rotateSkybox)
            {
                float currentRotation = skyboxMaterial.GetFloat(rotationPropertyId);
                float rotationDelta = skyboxRotationSpeed * cycleManager.TimeScale * Time.deltaTime;
                skyboxMaterial.SetFloat(rotationPropertyId, currentRotation + rotationDelta);
            }
        }

        private float CalculateSkyboxExposure(DayPhase phase)
        {
            float progress = config.GetPhaseProgress(cycleManager.CurrentTime);

            switch (phase)
            {
                case DayPhase.Dawn:
                    return Mathf.Lerp(config.SkyboxExposureNight, config.SkyboxExposureDay, progress);
                case DayPhase.Day:
                    return config.SkyboxExposureDay;
                case DayPhase.Dusk:
                    return Mathf.Lerp(config.SkyboxExposureDay, config.SkyboxExposureNight, progress);
                case DayPhase.Night:
                    return config.SkyboxExposureNight;
                default:
                    return config.SkyboxExposureDay;
            }
        }

        private Color CalculateSkyboxTint(DayPhase phase, float currentTime)
        {
            float progress = config.GetPhaseProgress(currentTime);

            switch (phase)
            {
                case DayPhase.Dawn:
                    return Color.Lerp(config.SkyboxTintNight, config.SkyboxTintDawn, progress * 0.5f);
                case DayPhase.Day:
                    if (progress < 0.1f)
                        return Color.Lerp(config.SkyboxTintDawn, config.SkyboxTintDay, progress * 10f);
                    return config.SkyboxTintDay;
                case DayPhase.Dusk:
                    return Color.Lerp(config.SkyboxTintDay, config.SkyboxTintDusk, progress);
                case DayPhase.Night:
                    if (progress < 0.1f)
                        return Color.Lerp(config.SkyboxTintDusk, config.SkyboxTintNight, progress * 10f);
                    return config.SkyboxTintNight;
                default:
                    return config.SkyboxTintDay;
            }
        }

        #endregion

        #region Fog Updates

        private void UpdateFog()
        {
            if (!controlFog || !config.EnableDynamicFog) return;

            float lerpFactor = useSmoothTransitions ? Time.deltaTime * transitionSpeed : 1f;

            // Interpolate fog color and density
            currentFogColor = Color.Lerp(currentFogColor, targetFogColor, lerpFactor);
            currentFogDensity = Mathf.Lerp(currentFogDensity, targetFogDensity, lerpFactor);

            // Apply fog settings
            RenderSettings.fogColor = currentFogColor;
            RenderSettings.fogDensity = currentFogDensity;

            // Update fog distance if enabled
            if (controlFogDistance)
            {
                DayPhase phase = cycleManager.CurrentPhase;
                float progress = config.GetPhaseProgress(cycleManager.CurrentTime);

                float startDistance, endDistance;

                if (phase == DayPhase.Night || phase == DayPhase.Dusk)
                {
                    float nightProgress = phase == DayPhase.Night ? 1f : progress;
                    startDistance = Mathf.Lerp(fogStartDistanceDay, fogStartDistanceNight, nightProgress);
                    endDistance = Mathf.Lerp(fogEndDistanceDay, fogEndDistanceNight, nightProgress);
                }
                else
                {
                    float dayProgress = phase == DayPhase.Day ? 1f : progress;
                    startDistance = Mathf.Lerp(fogStartDistanceNight, fogStartDistanceDay, dayProgress);
                    endDistance = Mathf.Lerp(fogEndDistanceNight, fogEndDistanceDay, dayProgress);
                }

                RenderSettings.fogStartDistance = startDistance;
                RenderSettings.fogEndDistance = endDistance;
            }
        }

        private Color CalculateFogColor(DayPhase phase)
        {
            float progress = config.GetPhaseProgress(cycleManager.CurrentTime);

            switch (phase)
            {
                case DayPhase.Dawn:
                    return Color.Lerp(config.FogColorNight, config.FogColorDawnDusk, progress);
                case DayPhase.Day:
                    if (progress < 0.1f)
                        return Color.Lerp(config.FogColorDawnDusk, config.FogColorDay, progress * 10f);
                    return config.FogColorDay;
                case DayPhase.Dusk:
                    return Color.Lerp(config.FogColorDay, config.FogColorDawnDusk, progress);
                case DayPhase.Night:
                    if (progress < 0.1f)
                        return Color.Lerp(config.FogColorDawnDusk, config.FogColorNight, progress * 10f);
                    return config.FogColorNight;
                default:
                    return config.FogColorDay;
            }
        }

        private float CalculateFogDensity(DayPhase phase)
        {
            float progress = config.GetPhaseProgress(cycleManager.CurrentTime);

            switch (phase)
            {
                case DayPhase.Dawn:
                    return Mathf.Lerp(config.FogDensityNight, config.FogDensityDay, progress);
                case DayPhase.Day:
                    return config.FogDensityDay;
                case DayPhase.Dusk:
                    return Mathf.Lerp(config.FogDensityDay, config.FogDensityNight, progress);
                case DayPhase.Night:
                    return config.FogDensityNight;
                default:
                    return config.FogDensityDay;
            }
        }

        #endregion

        #region Helper Calculations

        private float CalculateAmbientIntensity(DayPhase phase)
        {
            float progress = config.GetPhaseProgress(cycleManager.CurrentTime);

            switch (phase)
            {
                case DayPhase.Dawn:
                    return Mathf.Lerp(config.AmbientIntensityNight, config.AmbientIntensityDay, progress);
                case DayPhase.Day:
                    return config.AmbientIntensityDay;
                case DayPhase.Dusk:
                    return Mathf.Lerp(config.AmbientIntensityDay, config.AmbientIntensityNight, progress);
                case DayPhase.Night:
                    return config.AmbientIntensityNight;
                default:
                    return config.AmbientIntensityDay;
            }
        }

        private float CalculateShadowStrength(DayPhase phase)
        {
            float progress = config.GetPhaseProgress(cycleManager.CurrentTime);

            switch (phase)
            {
                case DayPhase.Dawn:
                    return Mathf.Lerp(shadowStrengthNight, shadowStrengthDawnDusk, progress);
                case DayPhase.Day:
                    if (progress < 0.1f)
                        return Mathf.Lerp(shadowStrengthDawnDusk, shadowStrengthDay, progress * 10f);
                    return shadowStrengthDay;
                case DayPhase.Dusk:
                    return Mathf.Lerp(shadowStrengthDay, shadowStrengthDawnDusk, progress);
                case DayPhase.Night:
                    if (progress < 0.1f)
                        return Mathf.Lerp(shadowStrengthDawnDusk, shadowStrengthNight, progress * 10f);
                    return shadowStrengthNight;
                default:
                    return shadowStrengthDay;
            }
        }

        private float CalculateLightMultiplier(DayPhase phase)
        {
            switch (phase)
            {
                case DayPhase.Dawn:
                case DayPhase.Dusk:
                    return 0.7f;
                case DayPhase.Day:
                    return 1f;
                case DayPhase.Night:
                    return 0.3f;
                default:
                    return 1f;
            }
        }

        #endregion

        #region Reflection Probes

        private void CheckReflectionProbeUpdate()
        {
            if (reflectionProbes == null || reflectionProbes.Length == 0) return;

            float currentTime = cycleManager.CurrentTime;

            if (currentTime - lastReflectionUpdate >= reflectionUpdateInterval)
            {
                UpdateReflectionProbes();
                lastReflectionUpdate = currentTime;
            }
        }

        private void UpdateReflectionProbes()
        {
            if (reflectionProbes == null) return;

            foreach (ReflectionProbe probe in reflectionProbes)
            {
                if (probe != null && probe.mode == ReflectionProbeMode.Realtime)
                {
                    probe.RenderProbe();
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Force an immediate lighting update (no smooth transition).
        /// </summary>
        public void ForceImmediateUpdate()
        {
            UpdateTargetValues();
            currentSunColor = targetSunColor;
            currentSunIntensity = targetSunIntensity;
            currentMoonColor = targetMoonColor;
            currentMoonIntensity = targetMoonIntensity;
            currentAmbientColor = targetAmbientColor;
            currentAmbientIntensity = targetAmbientIntensity;
            currentFogColor = targetFogColor;
            currentFogDensity = targetFogDensity;
            currentSkyboxExposure = targetSkyboxExposure;
            currentSkyboxTint = targetSkyboxTint;

            ApplyValuesImmediate();
            UpdateReflectionProbes();
        }

        /// <summary>
        /// Set custom sun light reference.
        /// </summary>
        public void SetSunLight(Light light)
        {
            sunLight = light;
        }

        /// <summary>
        /// Set custom moon light reference.
        /// </summary>
        public void SetMoonLight(Light light)
        {
            moonLight = light;
        }

        /// <summary>
        /// Set skybox material at runtime.
        /// </summary>
        public void SetSkyboxMaterial(Material material)
        {
            skyboxMaterial = material;
            if (material != null)
            {
                hasSkyboxMaterial = true;
                exposurePropertyId = Shader.PropertyToID(skyboxExposureProperty);
                tintPropertyId = Shader.PropertyToID(skyboxTintProperty);
                rotationPropertyId = Shader.PropertyToID(skyboxRotationProperty);
            }
            else
            {
                hasSkyboxMaterial = false;
            }
        }

        #endregion

        #region Editor Debug

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!showDebugInfo || cycleManager == null) return;

            GUILayout.BeginArea(new Rect(320, 10, 300, 250));
            GUILayout.BeginVertical("box");

            GUILayout.Label("=== Lighting Debug ===");
            GUILayout.Label($"Sun Color: {currentSunColor}");
            GUILayout.Label($"Sun Intensity: {currentSunIntensity:F2}");
            GUILayout.Label($"Moon Intensity: {currentMoonIntensity:F2}");
            GUILayout.Label($"Ambient Color: {currentAmbientColor}");
            GUILayout.Label($"Ambient Intensity: {currentAmbientIntensity:F2}");
            GUILayout.Label($"Fog Color: {currentFogColor}");
            GUILayout.Label($"Fog Density: {currentFogDensity:F4}");
            GUILayout.Label($"Skybox Exposure: {currentSkyboxExposure:F2}");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
#endif

        #endregion
    }
}
