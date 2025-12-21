using UnityEngine;
using RTS.Core;
using RTS.Core.Events;

namespace RTS.DayNightCycle
{
    /// <summary>
    /// Controls the rotation and positioning of celestial bodies (sun and moon).
    /// Handles directional light rotation for dynamic shadows throughout the day.
    /// </summary>
    public class CelestialController : MonoBehaviour
    {
        [Header("=== CELESTIAL BODY REFERENCES ===")]
        [Tooltip("The directional light representing the sun")]
        [SerializeField] private Light sunLight;

        [Tooltip("The directional light representing the moon (optional)")]
        [SerializeField] private Light moonLight;

        [Tooltip("Parent transform for sun rotation (optional, uses sunLight.transform if null)")]
        [SerializeField] private Transform sunPivot;

        [Tooltip("Parent transform for moon rotation (optional, uses moonLight.transform if null)")]
        [SerializeField] private Transform moonPivot;

        [Header("=== SUN VISUAL ELEMENTS ===")]
        [Tooltip("Visual mesh/sprite for the sun (optional)")]
        [SerializeField] private GameObject sunVisual;

        [Tooltip("Sun lens flare or glow effect (optional)")]
        [SerializeField] private GameObject sunFlare;

        [Header("=== MOON VISUAL ELEMENTS ===")]
        [Tooltip("Visual mesh/sprite for the moon (optional)")]
        [SerializeField] private GameObject moonVisual;

        [Tooltip("Moon glow effect (optional)")]
        [SerializeField] private GameObject moonGlow;

        [Header("=== STARS ===")]
        [Tooltip("Star particle system or skybox stars (optional)")]
        [SerializeField] private ParticleSystem starsParticleSystem;

        [Tooltip("Stars material for controlling emission (optional)")]
        [SerializeField] private Material starsMaterial;

        [Tooltip("Emission property name in stars material")]
        [SerializeField] private string starsEmissionProperty = "_EmissionColor";

        [Header("=== ROTATION SETTINGS ===")]
        [Tooltip("World forward direction (typically north)")]
        [SerializeField] private Vector3 worldForward = Vector3.forward;

        [Tooltip("Rotation smoothing (higher = smoother but more latency)")]
        [SerializeField, Range(0f, 20f)] private float rotationSmoothness = 5f;

        [Tooltip("Use smooth rotation interpolation")]
        [SerializeField] private bool useSmoothRotation = true;

        [Header("=== ADVANCED SETTINGS ===")]
        [Tooltip("Distance of sun from world center (for visual positioning)")]
        [SerializeField] private float sunDistance = 500f;

        [Tooltip("Distance of moon from world center")]
        [SerializeField] private float moonDistance = 400f;

        [Tooltip("Enable sun below-horizon hiding")]
        [SerializeField] private bool hideSunBelowHorizon = true;

        [Tooltip("Enable moon below-horizon hiding")]
        [SerializeField] private bool hideMoonBelowHorizon = true;

        // ===== Private State =====
        private DayNightCycleManager cycleManager;
        private DayNightConfigSO config;

        private Quaternion targetSunRotation;
        private Quaternion targetMoonRotation;
        private float currentSunAngle;
        private float currentMoonAngle;

        // ===== Cached Components =====
        private Transform sunTransform;
        private Transform moonTransform;
        private ParticleSystem.EmissionModule starsEmission;
        private bool hasStarsParticles;
        private bool hasStarsMaterial;

        #region Unity Lifecycle

        private void Start()
        {
            Initialize();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void LateUpdate()
        {
            if (cycleManager == null || config == null) return;

            UpdateCelestialRotations();
            UpdateVisuals();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            // Get reference to cycle manager
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
                Debug.LogWarning("[CelestialController] DayNightCycleManager not found. Celestial rotation disabled.");
                enabled = false;
                return;
            }

            // Setup transforms
            sunTransform = sunPivot != null ? sunPivot : (sunLight != null ? sunLight.transform : null);
            moonTransform = moonPivot != null ? moonPivot : (moonLight != null ? moonLight.transform : null);

            // Setup stars
            if (starsParticleSystem != null)
            {
                starsEmission = starsParticleSystem.emission;
                hasStarsParticles = true;
            }

            hasStarsMaterial = starsMaterial != null;

            // Auto-find lights if not assigned
            if (sunLight == null)
            {
                Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                foreach (Light light in lights)
                {
                    if (light.type == LightType.Directional && light.gameObject.name.ToLower().Contains("sun"))
                    {
                        sunLight = light;
                        sunTransform = light.transform;
                        break;
                    }
                }
            }

            // Initialize rotations
            if (sunTransform != null)
            {
                UpdateSunRotation(cycleManager.CurrentTime, true);
            }

            if (moonTransform != null)
            {
                UpdateMoonRotation(cycleManager.CurrentTime, true);
            }
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<TimeUpdatedEvent>(OnTimeUpdated);
            EventBus.Subscribe<DayPhaseChangedEvent>(OnPhaseChanged);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<TimeUpdatedEvent>(OnTimeUpdated);
            EventBus.Unsubscribe<DayPhaseChangedEvent>(OnPhaseChanged);
        }

        #endregion

        #region Event Handlers

        private void OnTimeUpdated(TimeUpdatedEvent evt)
        {
            // Most updates happen in LateUpdate for smooth interpolation
        }

        private void OnPhaseChanged(DayPhaseChangedEvent evt)
        {
            // Handle phase-specific visual changes
            UpdatePhaseVisuals(evt.NewPhase);
        }

        #endregion

        #region Celestial Rotation

        private void UpdateCelestialRotations()
        {
            float currentTime = cycleManager.CurrentTime;

            // Update sun
            if (sunTransform != null && config.EnableSunRotation)
            {
                UpdateSunRotation(currentTime, false);
            }

            // Update moon
            if (moonTransform != null && config.EnableMoonRotation)
            {
                UpdateMoonRotation(currentTime, false);
            }

            // Update stars
            UpdateStars();
        }

        private void UpdateSunRotation(float currentTime, bool immediate)
        {
            // Calculate sun angle based on time
            // Sun rises at 6:00 (angle 0), peaks at 12:00 (angle 90), sets at 18:00 (angle 180)
            // Night: sun is below horizon (angle 180-360)
            float normalizedTime = currentTime / 24f;
            float sunAngle = normalizedTime * 360f - 90f; // -90 offset so 6:00 = sunrise at horizon

            // Apply elevation offset
            Vector3 rotationAxis = config.SunRotationAxis.normalized;
            Quaternion elevationRotation = Quaternion.AngleAxis(config.SunElevationOffset, Vector3.forward);

            // Calculate target rotation
            targetSunRotation = Quaternion.AngleAxis(sunAngle + config.SunRotationOffset, rotationAxis) * elevationRotation;

            // Apply rotation
            if (immediate || !useSmoothRotation)
            {
                sunTransform.rotation = targetSunRotation;
            }
            else
            {
                sunTransform.rotation = Quaternion.Slerp(
                    sunTransform.rotation,
                    targetSunRotation,
                    Time.deltaTime * rotationSmoothness
                );
            }

            currentSunAngle = sunAngle;

            // Update sun position (for visual sun disc)
            if (sunVisual != null)
            {
                Vector3 sunDirection = -sunTransform.forward;
                sunVisual.transform.position = transform.position + sunDirection * sunDistance;
                sunVisual.transform.LookAt(transform.position);
            }

            // Hide sun when below horizon
            if (hideSunBelowHorizon)
            {
                bool sunAboveHorizon = sunTransform.forward.y < 0; // Forward points down = light pointing at ground = sun visible
                if (sunLight != null)
                {
                    sunLight.enabled = sunAboveHorizon;
                }
                if (sunVisual != null)
                {
                    sunVisual.SetActive(sunAboveHorizon);
                }
                if (sunFlare != null)
                {
                    sunFlare.SetActive(sunAboveHorizon);
                }
            }
        }

        private void UpdateMoonRotation(float currentTime, bool immediate)
        {
            // Moon is opposite to sun (offset by 12 hours)
            float normalizedTime = ((currentTime + 12f) % 24f) / 24f;
            float moonAngle = normalizedTime * 360f - 90f;

            // Apply elevation offset
            Vector3 rotationAxis = config.MoonRotationAxis.normalized;
            Quaternion elevationRotation = Quaternion.AngleAxis(config.MoonElevationOffset, Vector3.forward);

            // Calculate target rotation
            targetMoonRotation = Quaternion.AngleAxis(moonAngle + config.MoonRotationOffset, rotationAxis) * elevationRotation;

            // Apply rotation
            if (immediate || !useSmoothRotation)
            {
                moonTransform.rotation = targetMoonRotation;
            }
            else
            {
                moonTransform.rotation = Quaternion.Slerp(
                    moonTransform.rotation,
                    targetMoonRotation,
                    Time.deltaTime * rotationSmoothness
                );
            }

            currentMoonAngle = moonAngle;

            // Update moon position (for visual moon disc)
            if (moonVisual != null)
            {
                Vector3 moonDirection = -moonTransform.forward;
                moonVisual.transform.position = transform.position + moonDirection * moonDistance;
                moonVisual.transform.LookAt(transform.position);
            }

            // Hide moon when below horizon
            if (hideMoonBelowHorizon)
            {
                bool moonAboveHorizon = moonTransform.forward.y < 0;
                if (moonLight != null)
                {
                    moonLight.enabled = moonAboveHorizon && cycleManager.GetCurrentMoonIntensity() > 0.01f;
                }
                if (moonVisual != null)
                {
                    moonVisual.SetActive(moonAboveHorizon);
                }
                if (moonGlow != null)
                {
                    moonGlow.SetActive(moonAboveHorizon);
                }
            }
        }

        private void UpdateStars()
        {
            float starsVisibility = cycleManager.GetCurrentStarsVisibility();

            // Update particle system
            if (hasStarsParticles)
            {
                starsEmission.rateOverTimeMultiplier = starsVisibility > 0.01f ? 1f : 0f;

                var main = starsParticleSystem.main;
                Color startColor = main.startColor.color;
                startColor.a = starsVisibility;
                main.startColor = startColor;
            }

            // Update stars material emission
            if (hasStarsMaterial)
            {
                Color emissionColor = Color.white * starsVisibility * config.StarsMaxBrightness;
                starsMaterial.SetColor(starsEmissionProperty, emissionColor);
            }
        }

        #endregion

        #region Visual Updates

        private void UpdateVisuals()
        {
            // Update sun flare intensity based on time
            if (sunFlare != null && cycleManager != null)
            {
                float sunIntensity = cycleManager.GetCurrentSunIntensity();
                // Flare should be strongest during midday
                float flareIntensity = Mathf.Clamp01(sunIntensity / config.SunIntensityDay);
                sunFlare.transform.localScale = Vector3.one * flareIntensity;
            }

            // Update moon glow based on phase
            if (moonGlow != null && cycleManager != null)
            {
                float moonIntensity = cycleManager.GetCurrentMoonIntensity();
                float glowIntensity = moonIntensity / config.MoonIntensityNight;
                moonGlow.transform.localScale = Vector3.one * glowIntensity;
            }
        }

        private void UpdatePhaseVisuals(DayPhase newPhase)
        {
            // Add any phase-specific visual changes here
            switch (newPhase)
            {
                case DayPhase.Dawn:
                    // Could trigger dawn-specific effects
                    break;
                case DayPhase.Day:
                    // Full daylight effects
                    break;
                case DayPhase.Dusk:
                    // Dusk effects
                    break;
                case DayPhase.Night:
                    // Night effects
                    break;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get the current sun direction (normalized).
        /// </summary>
        public Vector3 GetSunDirection()
        {
            if (sunTransform == null) return Vector3.down;
            return -sunTransform.forward;
        }

        /// <summary>
        /// Get the current moon direction (normalized).
        /// </summary>
        public Vector3 GetMoonDirection()
        {
            if (moonTransform == null) return Vector3.up;
            return -moonTransform.forward;
        }

        /// <summary>
        /// Check if the sun is currently above the horizon.
        /// </summary>
        public bool IsSunAboveHorizon()
        {
            if (sunTransform == null) return false;
            return sunTransform.forward.y < 0;
        }

        /// <summary>
        /// Check if the moon is currently above the horizon.
        /// </summary>
        public bool IsMoonAboveHorizon()
        {
            if (moonTransform == null) return false;
            return moonTransform.forward.y < 0;
        }

        /// <summary>
        /// Set custom sun light reference.
        /// </summary>
        public void SetSunLight(Light light)
        {
            sunLight = light;
            sunTransform = light != null ? light.transform : null;
        }

        /// <summary>
        /// Set custom moon light reference.
        /// </summary>
        public void SetMoonLight(Light light)
        {
            moonLight = light;
            moonTransform = light != null ? light.transform : null;
        }

        #endregion

        #region Editor Visualization

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw sun arc
            Gizmos.color = Color.yellow;
            if (sunTransform != null)
            {
                Gizmos.DrawLine(transform.position, transform.position + GetSunDirection() * 50f);
                Gizmos.DrawWireSphere(transform.position + GetSunDirection() * 50f, 5f);
            }

            // Draw moon arc
            Gizmos.color = Color.cyan;
            if (moonTransform != null)
            {
                Gizmos.DrawLine(transform.position, transform.position + GetMoonDirection() * 40f);
                Gizmos.DrawWireSphere(transform.position + GetMoonDirection() * 40f, 3f);
            }

            // Draw rotation axes
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, Vector3.right * 20f);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, Vector3.up * 20f);
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, Vector3.forward * 20f);
        }
#endif

        #endregion
    }
}
