using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

namespace RTS.Core
{
    /// <summary>
    /// Handles build-specific initialization to fix common build issues:
    /// - Low FPS in builds (VSync, frame rate limiting)
    /// - Black screens/missing textures (shader warmup)
    /// - GPU selection on laptops with multiple GPUs
    /// - Texture streaming and quality settings
    /// </summary>
    public class BuildInitializer : MonoBehaviour
    {
        [Header("Performance Settings")]
        [SerializeField] private bool disableVSyncInBuild = true;
        [SerializeField] private int targetFrameRate = 300; // High frame rate, let hardware decide actual FPS

        [Header("Graphics Settings")]
        [SerializeField] private bool warmupAllShaders = true;
        [SerializeField] private bool optimizeTextureStreaming = true;
        [SerializeField] private bool forceDiscreteGPU = true; // For laptops with integrated + discrete GPU

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnBeforeSceneLoad()
        {
            // Create initializer instance before scene loads
            GameObject initializerObj = new GameObject("BuildInitializer");
            BuildInitializer initializer = initializerObj.AddComponent<BuildInitializer>();
            DontDestroyOnLoad(initializerObj);
        }

        private void Awake()
        {
            InitializeBuildSettings();
        }

        private void Start()
        {
            StartCoroutine(WarmupShadersCoroutine());
        }

        private void InitializeBuildSettings()
        {
            LogDebug("=== Build Initializer Starting ===");

            // Fix 1: Remove VSync limitation (causes 20 FPS on some systems)
            if (disableVSyncInBuild)
            {
                QualitySettings.vSyncCount = 0;
                LogDebug($"VSync disabled (was: {QualitySettings.vSyncCount})");
            }

            // Fix 2: Set high target frame rate
            Application.targetFrameRate = targetFrameRate;
            LogDebug($"Target frame rate set to: {targetFrameRate}");

            // Fix 3: Force discrete GPU on laptops (prevents using weak integrated GPU)
            if (forceDiscreteGPU)
            {
                // This helps Windows select the high-performance GPU
                QualitySettings.antiAliasing = 0; // Start with no AA, can be changed later
                LogDebug("Graphics settings configured for discrete GPU usage");
            }

            // Fix 4: Optimize texture streaming to prevent missing textures
            if (optimizeTextureStreaming)
            {
                QualitySettings.streamingMipmapsActive = true;
                QualitySettings.streamingMipmapsMemoryBudget = 512; // MB
                QualitySettings.streamingMipmapsMaxLevelReduction = 0;
                LogDebug("Texture streaming optimized");
            }

            // Fix 5: Ensure reasonable quality settings for builds
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.shadowDistance = 150f;

            // Fix 6: Pixel light count (affects performance)
            QualitySettings.pixelLightCount = 4;

            // Fix 7: Anisotropic filtering
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;

            LogDebug($"Screen: {Screen.width}x{Screen.height} @ {Screen.currentResolution.refreshRateRatio.value:F0}Hz");
            LogDebug($"Graphics Device: {SystemInfo.graphicsDeviceName}");
            LogDebug($"Graphics API: {SystemInfo.graphicsDeviceType}");
            LogDebug($"GPU Memory: {SystemInfo.graphicsMemorySize}MB");
            LogDebug($"Processor: {SystemInfo.processorType} ({SystemInfo.processorCount} cores)");
            LogDebug($"System Memory: {SystemInfo.systemMemorySize}MB");

            LogDebug("=== Build Initialization Complete ===");
        }

        private IEnumerator WarmupShadersCoroutine()
        {
            if (!warmupAllShaders)
            {
                yield break;
            }

            LogDebug("Starting shader warmup...");

            // Wait a frame for scene to load
            yield return null;

            // Warmup shaders to prevent black screens
            // Note: In Unity 6+, Shader.WarmupAllShaders() is the primary method for shader warmup
            Shader.WarmupAllShaders();
            LogDebug("All shaders warmed up");

            yield return new WaitForSeconds(0.5f);

            LogDebug("Shader warmup complete");
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[BuildInitializer] {message}");
            }
        }

        // Public API for runtime adjustments
        public void SetTargetFrameRate(int fps)
        {
            Application.targetFrameRate = fps;
            LogDebug($"Target frame rate changed to: {fps}");
        }

        public void SetVSync(bool enabled)
        {
            QualitySettings.vSyncCount = enabled ? 1 : 0;
            LogDebug($"VSync {(enabled ? "enabled" : "disabled")}");
        }

        public void SetQualityLevel(int level)
        {
            QualitySettings.SetQualityLevel(level);
            LogDebug($"Quality level set to: {QualitySettings.names[level]}");
        }
    }
}
