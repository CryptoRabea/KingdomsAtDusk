using UnityEngine;

namespace RTS.DayNightCycle
{
    /// <summary>
    /// Configuration ScriptableObject for the Day-Night Cycle system.
    /// Contains all customizable settings for time, lighting, celestial bodies, and ambiance.
    /// </summary>
    [CreateAssetMenu(fileName = "DayNightConfig", menuName = "RTS/Day Night Cycle/Config", order = 0)]
    public class DayNightConfigSO : ScriptableObject
    {
        [Header("=== TIME SETTINGS ===")]
        [Tooltip("Total duration of a full day-night cycle in real-world seconds")]
        [SerializeField, Range(60f, 3600f)] private float dayDurationInSeconds = 600f;

        [Tooltip("How many in-game hours pass per real-world second (calculated from day duration)")]
        [SerializeField, Range(0.01f, 10f)] private float timeScale = 1f;

        [Tooltip("Starting hour of the day (0-24, where 6 = 6 AM, 18 = 6 PM)")]
        [SerializeField, Range(0f, 24f)] private float startingHour = 6f;

        [Header("=== DAY PHASE DEFINITIONS ===")]
        [Tooltip("Hour when dawn begins (transition from night to day)")]
        [SerializeField, Range(0f, 12f)] private float dawnStartHour = 5f;

        [Tooltip("Hour when dawn ends and full day begins")]
        [SerializeField, Range(0f, 12f)] private float dawnEndHour = 7f;

        [Tooltip("Hour when dusk begins (transition from day to night)")]
        [SerializeField, Range(12f, 24f)] private float duskStartHour = 18f;

        [Tooltip("Hour when dusk ends and full night begins")]
        [SerializeField, Range(12f, 24f)] private float duskEndHour = 20f;

        [Header("=== CELESTIAL BODY SETTINGS ===")]
        [Tooltip("Enable sun rotation for dynamic shadows")]
        [SerializeField] private bool enableSunRotation = true;

        [Tooltip("Enable moon rotation during night")]
        [SerializeField] private bool enableMoonRotation = true;

        [Tooltip("Sun rotation axis (typically Vector3.right for east-west rotation)")]
        [SerializeField] private Vector3 sunRotationAxis = Vector3.right;

        [Tooltip("Moon rotation axis")]
        [SerializeField] private Vector3 moonRotationAxis = Vector3.right;

        [Tooltip("Initial rotation offset for sun (adjusts starting position)")]
        [SerializeField, Range(-180f, 180f)] private float sunRotationOffset = 0f;

        [Tooltip("Initial rotation offset for moon")]
        [SerializeField, Range(-180f, 180f)] private float moonRotationOffset = 180f;

        [Tooltip("Sun elevation angle offset (higher = sun arcs higher in sky)")]
        [SerializeField, Range(0f, 90f)] private float sunElevationOffset = 30f;

        [Tooltip("Moon elevation angle offset")]
        [SerializeField, Range(0f, 90f)] private float moonElevationOffset = 30f;

        [Header("=== SUNLIGHT SETTINGS ===")]
        [Tooltip("Sun light intensity during full day")]
        [SerializeField, Range(0f, 3f)] private float sunIntensityDay = 1.2f;

        [Tooltip("Sun light intensity during dawn/dusk")]
        [SerializeField, Range(0f, 3f)] private float sunIntensityDawnDusk = 0.6f;

        [Tooltip("Sun light intensity during night (usually 0)")]
        [SerializeField, Range(0f, 1f)] private float sunIntensityNight = 0f;

        [Tooltip("Sun color during full day")]
        [SerializeField] private Color sunColorDay = new Color(1f, 0.98f, 0.92f);

        [Tooltip("Sun color during dawn")]
        [SerializeField] private Color sunColorDawn = new Color(1f, 0.6f, 0.4f);

        [Tooltip("Sun color during dusk")]
        [SerializeField] private Color sunColorDusk = new Color(1f, 0.5f, 0.3f);

        [Header("=== MOONLIGHT SETTINGS ===")]
        [Tooltip("Moon light intensity during full night")]
        [SerializeField, Range(0f, 1f)] private float moonIntensityNight = 0.3f;

        [Tooltip("Moon light intensity during day (usually 0)")]
        [SerializeField, Range(0f, 0.5f)] private float moonIntensityDay = 0f;

        [Tooltip("Moon color during night")]
        [SerializeField] private Color moonColorNight = new Color(0.6f, 0.7f, 1f);

        [Header("=== AMBIENT LIGHTING ===")]
        [Tooltip("Ambient light color during day")]
        [SerializeField] private Color ambientColorDay = new Color(0.9f, 0.9f, 0.95f);

        [Tooltip("Ambient light color during dawn/dusk")]
        [SerializeField] private Color ambientColorDawnDusk = new Color(0.8f, 0.6f, 0.5f);

        [Tooltip("Ambient light color during night")]
        [SerializeField] private Color ambientColorNight = new Color(0.15f, 0.15f, 0.25f);

        [Tooltip("Ambient intensity during day")]
        [SerializeField, Range(0f, 2f)] private float ambientIntensityDay = 1f;

        [Tooltip("Ambient intensity during night")]
        [SerializeField, Range(0f, 1f)] private float ambientIntensityNight = 0.3f;

        [Header("=== FOG SETTINGS ===")]
        [Tooltip("Enable dynamic fog that changes with time of day")]
        [SerializeField] private bool enableDynamicFog = true;

        [Tooltip("Fog color during day")]
        [SerializeField] private Color fogColorDay = new Color(0.8f, 0.85f, 0.9f);

        [Tooltip("Fog color during dawn/dusk")]
        [SerializeField] private Color fogColorDawnDusk = new Color(0.9f, 0.7f, 0.5f);

        [Tooltip("Fog color during night")]
        [SerializeField] private Color fogColorNight = new Color(0.1f, 0.1f, 0.15f);

        [Tooltip("Fog density during day")]
        [SerializeField, Range(0f, 0.1f)] private float fogDensityDay = 0.01f;

        [Tooltip("Fog density during night")]
        [SerializeField, Range(0f, 0.1f)] private float fogDensityNight = 0.02f;

        [Header("=== SKYBOX SETTINGS ===")]
        [Tooltip("Enable skybox color/exposure changes")]
        [SerializeField] private bool enableSkyboxTransitions = true;

        [Tooltip("Skybox exposure during day")]
        [SerializeField, Range(0f, 8f)] private float skyboxExposureDay = 1.3f;

        [Tooltip("Skybox exposure during night")]
        [SerializeField, Range(0f, 8f)] private float skyboxExposureNight = 0.2f;

        [Tooltip("Skybox tint during day")]
        [SerializeField] private Color skyboxTintDay = Color.white;

        [Tooltip("Skybox tint during dawn")]
        [SerializeField] private Color skyboxTintDawn = new Color(1f, 0.8f, 0.6f);

        [Tooltip("Skybox tint during dusk")]
        [SerializeField] private Color skyboxTintDusk = new Color(1f, 0.6f, 0.5f);

        [Tooltip("Skybox tint during night")]
        [SerializeField] private Color skyboxTintNight = new Color(0.4f, 0.5f, 0.7f);

        [Header("=== STARS SETTINGS ===")]
        [Tooltip("Enable stars during night")]
        [SerializeField] private bool enableStars = true;

        [Tooltip("Hour when stars start appearing")]
        [SerializeField, Range(12f, 24f)] private float starsAppearHour = 19f;

        [Tooltip("Hour when stars fully disappear")]
        [SerializeField, Range(0f, 12f)] private float starsDisappearHour = 6f;

        [Tooltip("Maximum star brightness")]
        [SerializeField, Range(0f, 2f)] private float starsMaxBrightness = 1f;

        [Header("=== UI DISPLAY SETTINGS ===")]
        [Tooltip("Show the on-screen time display")]
        [SerializeField] private bool showTimeDisplay = true;

        [Tooltip("Use 24-hour format (true) or 12-hour AM/PM format (false)")]
        [SerializeField] private bool use24HourFormat = true;

        [Tooltip("Show day counter in UI")]
        [SerializeField] private bool showDayCounter = true;

        [Tooltip("Show current phase (Dawn, Day, Dusk, Night) in UI")]
        [SerializeField] private bool showPhaseIndicator = true;

        [Header("=== TRANSITION SETTINGS ===")]
        [Tooltip("Smoothing factor for light transitions (higher = smoother but slower)")]
        [SerializeField, Range(0.1f, 10f)] private float transitionSmoothness = 2f;

        [Tooltip("Use gradient-based transitions for more control")]
        [SerializeField] private bool useGradientTransitions = false;

        [Tooltip("Custom sun color gradient across 24 hours (optional)")]
        [SerializeField] private Gradient sunColorGradient;

        [Tooltip("Custom ambient color gradient across 24 hours (optional)")]
        [SerializeField] private Gradient ambientColorGradient;

        #region Public Properties - Time Settings

        public float DayDurationInSeconds => dayDurationInSeconds;
        public float TimeScale => timeScale;
        public float StartingHour => startingHour;

        /// <summary>
        /// Real-world seconds per in-game hour
        /// </summary>
        public float SecondsPerHour => dayDurationInSeconds / 24f;

        /// <summary>
        /// In-game hours per real-world second
        /// </summary>
        public float HoursPerSecond => 24f / dayDurationInSeconds;

        #endregion

        #region Public Properties - Day Phases

        public float DawnStartHour => dawnStartHour;
        public float DawnEndHour => dawnEndHour;
        public float DuskStartHour => duskStartHour;
        public float DuskEndHour => duskEndHour;

        #endregion

        #region Public Properties - Celestial Bodies

        public bool EnableSunRotation => enableSunRotation;
        public bool EnableMoonRotation => enableMoonRotation;
        public Vector3 SunRotationAxis => sunRotationAxis;
        public Vector3 MoonRotationAxis => moonRotationAxis;
        public float SunRotationOffset => sunRotationOffset;
        public float MoonRotationOffset => moonRotationOffset;
        public float SunElevationOffset => sunElevationOffset;
        public float MoonElevationOffset => moonElevationOffset;

        #endregion

        #region Public Properties - Sun Light

        public float SunIntensityDay => sunIntensityDay;
        public float SunIntensityDawnDusk => sunIntensityDawnDusk;
        public float SunIntensityNight => sunIntensityNight;
        public Color SunColorDay => sunColorDay;
        public Color SunColorDawn => sunColorDawn;
        public Color SunColorDusk => sunColorDusk;

        #endregion

        #region Public Properties - Moon Light

        public float MoonIntensityNight => moonIntensityNight;
        public float MoonIntensityDay => moonIntensityDay;
        public Color MoonColorNight => moonColorNight;

        #endregion

        #region Public Properties - Ambient

        public Color AmbientColorDay => ambientColorDay;
        public Color AmbientColorDawnDusk => ambientColorDawnDusk;
        public Color AmbientColorNight => ambientColorNight;
        public float AmbientIntensityDay => ambientIntensityDay;
        public float AmbientIntensityNight => ambientIntensityNight;

        #endregion

        #region Public Properties - Fog

        public bool EnableDynamicFog => enableDynamicFog;
        public Color FogColorDay => fogColorDay;
        public Color FogColorDawnDusk => fogColorDawnDusk;
        public Color FogColorNight => fogColorNight;
        public float FogDensityDay => fogDensityDay;
        public float FogDensityNight => fogDensityNight;

        #endregion

        #region Public Properties - Skybox

        public bool EnableSkyboxTransitions => enableSkyboxTransitions;
        public float SkyboxExposureDay => skyboxExposureDay;
        public float SkyboxExposureNight => skyboxExposureNight;
        public Color SkyboxTintDay => skyboxTintDay;
        public Color SkyboxTintDawn => skyboxTintDawn;
        public Color SkyboxTintDusk => skyboxTintDusk;
        public Color SkyboxTintNight => skyboxTintNight;

        #endregion

        #region Public Properties - Stars

        public bool EnableStars => enableStars;
        public float StarsAppearHour => starsAppearHour;
        public float StarsDisappearHour => starsDisappearHour;
        public float StarsMaxBrightness => starsMaxBrightness;

        #endregion

        #region Public Properties - UI

        public bool ShowTimeDisplay => showTimeDisplay;
        public bool Use24HourFormat => use24HourFormat;
        public bool ShowDayCounter => showDayCounter;
        public bool ShowPhaseIndicator => showPhaseIndicator;

        #endregion

        #region Public Properties - Transitions

        public float TransitionSmoothness => transitionSmoothness;
        public bool UseGradientTransitions => useGradientTransitions;
        public Gradient SunColorGradient => sunColorGradient;
        public Gradient AmbientColorGradient => ambientColorGradient;

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get the current phase of day based on the hour.
        /// </summary>
        public DayPhase GetPhaseForHour(float hour)
        {
            hour = hour % 24f;

            if (hour >= dawnStartHour && hour < dawnEndHour)
                return DayPhase.Dawn;
            if (hour >= dawnEndHour && hour < duskStartHour)
                return DayPhase.Day;
            if (hour >= duskStartHour && hour < duskEndHour)
                return DayPhase.Dusk;

            return DayPhase.Night;
        }

        /// <summary>
        /// Get normalized progress through the current phase (0-1).
        /// </summary>
        public float GetPhaseProgress(float hour)
        {
            hour = hour % 24f;
            DayPhase phase = GetPhaseForHour(hour);

            switch (phase)
            {
                case DayPhase.Dawn:
                    return (hour - dawnStartHour) / (dawnEndHour - dawnStartHour);
                case DayPhase.Day:
                    return (hour - dawnEndHour) / (duskStartHour - dawnEndHour);
                case DayPhase.Dusk:
                    return (hour - duskStartHour) / (duskEndHour - duskStartHour);
                case DayPhase.Night:
                    if (hour >= duskEndHour)
                        return (hour - duskEndHour) / (24f - duskEndHour + dawnStartHour);
                    else
                        return (hour + 24f - duskEndHour) / (24f - duskEndHour + dawnStartHour);
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Get interpolated sun intensity for the current hour.
        /// </summary>
        public float GetSunIntensityForHour(float hour)
        {
            DayPhase phase = GetPhaseForHour(hour);
            float progress = GetPhaseProgress(hour);

            switch (phase)
            {
                case DayPhase.Dawn:
                    return Mathf.Lerp(sunIntensityNight, sunIntensityDay, progress);
                case DayPhase.Day:
                    return sunIntensityDay;
                case DayPhase.Dusk:
                    return Mathf.Lerp(sunIntensityDay, sunIntensityNight, progress);
                case DayPhase.Night:
                    return sunIntensityNight;
                default:
                    return sunIntensityDay;
            }
        }

        /// <summary>
        /// Get interpolated sun color for the current hour.
        /// </summary>
        public Color GetSunColorForHour(float hour)
        {
            if (useGradientTransitions && sunColorGradient != null)
            {
                return sunColorGradient.Evaluate(hour / 24f);
            }

            DayPhase phase = GetPhaseForHour(hour);
            float progress = GetPhaseProgress(hour);

            switch (phase)
            {
                case DayPhase.Dawn:
                    return Color.Lerp(sunColorDawn, sunColorDay, progress);
                case DayPhase.Day:
                    return sunColorDay;
                case DayPhase.Dusk:
                    return Color.Lerp(sunColorDay, sunColorDusk, progress);
                case DayPhase.Night:
                    return sunColorDusk;
                default:
                    return sunColorDay;
            }
        }

        /// <summary>
        /// Get interpolated ambient color for the current hour.
        /// </summary>
        public Color GetAmbientColorForHour(float hour)
        {
            if (useGradientTransitions && ambientColorGradient != null)
            {
                return ambientColorGradient.Evaluate(hour / 24f);
            }

            DayPhase phase = GetPhaseForHour(hour);
            float progress = GetPhaseProgress(hour);

            switch (phase)
            {
                case DayPhase.Dawn:
                    return Color.Lerp(ambientColorNight, ambientColorDawnDusk, progress * 0.5f);
                case DayPhase.Day:
                    float dayProgress = progress;
                    if (dayProgress < 0.1f)
                        return Color.Lerp(ambientColorDawnDusk, ambientColorDay, dayProgress * 10f);
                    return ambientColorDay;
                case DayPhase.Dusk:
                    return Color.Lerp(ambientColorDay, ambientColorDawnDusk, progress);
                case DayPhase.Night:
                    if (progress < 0.1f)
                        return Color.Lerp(ambientColorDawnDusk, ambientColorNight, progress * 10f);
                    return ambientColorNight;
                default:
                    return ambientColorDay;
            }
        }

        /// <summary>
        /// Check if stars should be visible at the given hour.
        /// </summary>
        public float GetStarsVisibility(float hour)
        {
            if (!enableStars) return 0f;

            hour = hour % 24f;

            // Stars are visible from starsAppearHour to starsDisappearHour (wrapping around midnight)
            if (hour >= starsAppearHour)
            {
                float fadeInProgress = (hour - starsAppearHour) / (duskEndHour - starsAppearHour + 0.5f);
                return Mathf.Clamp01(fadeInProgress) * starsMaxBrightness;
            }
            else if (hour < starsDisappearHour)
            {
                float fadeOutProgress = hour / starsDisappearHour;
                return (1f - fadeOutProgress) * starsMaxBrightness;
            }

            return 0f;
        }

        #endregion

        #region Validation

        private void OnValidate()
        {
            // Ensure phase hours are in correct order
            dawnEndHour = Mathf.Max(dawnEndHour, dawnStartHour + 0.5f);
            duskStartHour = Mathf.Max(duskStartHour, dawnEndHour + 0.5f);
            duskEndHour = Mathf.Max(duskEndHour, duskStartHour + 0.5f);

            // Ensure day duration is reasonable
            dayDurationInSeconds = Mathf.Max(60f, dayDurationInSeconds);

            // Initialize gradients if null
            if (sunColorGradient == null)
            {
                sunColorGradient = new Gradient();
                sunColorGradient.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(sunColorDawn, 0.25f),
                        new GradientColorKey(sunColorDay, 0.5f),
                        new GradientColorKey(sunColorDusk, 0.75f),
                        new GradientColorKey(sunColorDawn, 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(1f, 1f)
                    }
                );
            }
        }

        #endregion
    }

    /// <summary>
    /// Represents the current phase of the day-night cycle.
    /// </summary>
    public enum DayPhase
    {
        Dawn,   // Transitioning from night to day
        Day,    // Full daylight
        Dusk,   // Transitioning from day to night
        Night   // Full night
    }
}
