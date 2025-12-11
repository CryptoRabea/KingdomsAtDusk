using UnityEngine;

namespace CircularLensVision
{
    /// <summary>
    /// Configuration asset for Circular Lens Vision system.
    /// Create via: Assets > Create > Circular Lens Vision > Config
    /// </summary>
    [CreateAssetMenu(fileName = "LensVisionConfig", menuName = "Circular Lens Vision/Config", order = 1)]
    public class LensVisionConfig : ScriptableObject
    {
        [Header("Lens Settings")]
        [Tooltip("Default radius of the circular lens vision area")]
        [Range(5f, 100f)]
        public float defaultLensRadius = 20f;

        [Tooltip("Update interval in seconds (lower = more responsive, higher = better performance)")]
        [Range(0.01f, 0.5f)]
        public float updateInterval = 0.1f;

        [Tooltip("Maximum number of objects to process per frame")]
        [Range(10, 200)]
        public int maxObjectsPerFrame = 50;

        [Header("Visual Settings")]
        [Tooltip("X-Ray color for player units")]
        public Color playerUnitXRayColor = new Color(0.3f, 0.7f, 1f, 0.8f);

        [Tooltip("X-Ray color for enemy units")]
        public Color enemyUnitXRayColor = new Color(1f, 0.3f, 0.3f, 0.8f);

        [Tooltip("X-Ray intensity")]
        [Range(0f, 2f)]
        public float xrayIntensity = 1f;

        [Tooltip("Rim power for x-ray effect")]
        [Range(0.1f, 8f)]
        public float rimPower = 3f;

        [Tooltip("Transparency amount for obstacles")]
        [Range(0f, 1f)]
        public float obstacleTransparency = 0.3f;

        [Tooltip("Transparent color tint for obstacles")]
        public Color obstacleTransparentColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

        [Header("Transition Settings")]
        [Tooltip("Fade speed for lens effect activation")]
        [Range(0.1f, 20f)]
        public float fadeSpeed = 5f;

        [Header("Performance Settings")]
        [Tooltip("Use spatial partitioning for better performance")]
        public bool useSpatialPartitioning = true;

        [Tooltip("Grid cell size for spatial partitioning")]
        [Range(5f, 50f)]
        public float gridCellSize = 10f;

        [Header("Layer Settings")]
        [Tooltip("Layers that count as obstacles")]
        public LayerMask obstacleLayers = ~0;

        [Tooltip("Layers that count as units")]
        public LayerMask unitLayers = ~0;

        [Header("Materials")]
        [Tooltip("Unit x-ray vision shader")]
        public Shader unitXRayShader;

        [Tooltip("Obstacle transparent shader")]
        public Shader obstacleTransparentShader;

        [Header("Debug")]
        [Tooltip("Show debug visualization")]
        public bool showDebugVisualization = true;

        [Tooltip("Debug visualization color")]
        public Color debugColor = new Color(0.3f, 0.7f, 1f, 0.3f);

        /// <summary>
        /// Apply this config to a CircularLensVision component
        /// </summary>
        public void ApplyToController(CircularLensVision controller)
        {
            if (controller == null) return;

            controller.SetLensRadius(defaultLensRadius);
            // Additional property setters can be added as needed
        }

        /// <summary>
        /// Create materials from the configured shaders
        /// </summary>
        public Material CreateUnitXRayMaterial()
        {
            if (unitXRayShader == null)
            {
                Debug.LogWarning("LensVisionConfig: Unit X-Ray shader not assigned!", this);
                return null;
            }

            Material mat = new Material(unitXRayShader);
            mat.SetColor("_XRayColor", playerUnitXRayColor);
            mat.SetFloat("_XRayIntensity", xrayIntensity);
            mat.SetFloat("_RimPower", rimPower);
            return mat;
        }

        /// <summary>
        /// Create materials from the configured shaders
        /// </summary>
        public Material CreateObstacleTransparentMaterial()
        {
            if (obstacleTransparentShader == null)
            {
                Debug.LogWarning("LensVisionConfig: Obstacle transparent shader not assigned!", this);
                return null;
            }

            Material mat = new Material(obstacleTransparentShader);
            mat.SetFloat("_TransparencyAmount", obstacleTransparency);
            mat.SetColor("_TransparentColor", obstacleTransparentColor);
            mat.SetFloat("_FadeSpeed", fadeSpeed);
            return mat;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Clamp values
            defaultLensRadius = Mathf.Max(5f, defaultLensRadius);
            updateInterval = Mathf.Max(0.01f, updateInterval);
            maxObjectsPerFrame = Mathf.Max(10, maxObjectsPerFrame);
            gridCellSize = Mathf.Max(5f, gridCellSize);
            fadeSpeed = Mathf.Max(0.1f, fadeSpeed);

            obstacleTransparency = Mathf.Clamp01(obstacleTransparency);
            xrayIntensity = Mathf.Max(0f, xrayIntensity);
            rimPower = Mathf.Max(0.1f, rimPower);
        }
#endif
    }
}
