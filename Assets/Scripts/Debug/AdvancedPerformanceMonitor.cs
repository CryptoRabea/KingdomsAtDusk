using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;

namespace KingdomsAtDusk.Debug
{
    /// <summary>
    /// Advanced performance monitoring with detailed per-frame rendering statistics,
    /// CPU/GPU timing, and URP-specific metrics.
    /// </summary>
    public class AdvancedPerformanceMonitor : MonoBehaviour
    {
        [Header("Display Settings")]
        [SerializeField] private bool showOnStart = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F3;
        [SerializeField] private bool enableInBuilds = true;

        [Header("Update Settings")]
        [SerializeField] private float updateInterval = 0.5f;
        [SerializeField] private bool trackDetailedRenderingStats = true;

        [Header("UI Settings")]
        [SerializeField] private int fontSize = 14;
        [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.8f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color goodColor = new Color(0.2f, 1f, 0.2f);
        [SerializeField] private Color warningColor = new Color(1f, 0.92f, 0.016f);
        [SerializeField] private Color badColor = new Color(1f, 0.2f, 0.2f);
        [SerializeField] private int padding = 10;
        [SerializeField] private bool compactMode = false;

        // Performance tracking
        private bool isVisible;
        private float deltaTime;
        private float fps;
        private float avgFps;
        private float minFps = float.MaxValue;
        private float maxFps = 0f;
        private float timeSinceUpdate;

        // Memory tracking
        private long totalAllocatedMemory;
        private long totalReservedMemory;
        private long monoUsedSize;
        private long monoHeapSize;
        private long gcMemory;
        private long lastGCMemory;
        private int gcCollectionCount;

        // Rendering stats (captured per frame)
        private int renderTextureCount;
        private int materialCount;
        private int meshCount;

        // Shadow info
        private ShadowQuality shadowQuality;
        private ShadowResolution shadowResolution;
        private int shadowCascades;
        private float shadowDistance;

        // Display
        private GUIStyle backgroundStyle;
        private GUIStyle textStyle;
        private StringBuilder displayText;
        private Rect windowRect;

        // FPS history
        private Queue<float> fpsHistory = new Queue<float>(60);

        // Timing
        private Stopwatch frameStopwatch;
        private float lastFrameTime;
        private float avgFrameTime;
        private Queue<float> frameTimeHistory = new Queue<float>(60);

        // CPU/GPU Timing (using Unity Profiling)
        private float cpuFrameTime;
        private float renderTime;

        // Game-specific stats
        private int activeCameraCount;
        private int activeGameObjectCount;
        private int activeComponentCount;

        private void Awake()
        {
            isVisible = showOnStart;
            displayText = new StringBuilder(2048);
            frameStopwatch = new Stopwatch();

            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitializeStyles();

            if (trackDetailedRenderingStats)
            {
                // Subscribe to render pipeline events for detailed stats
                RenderPipelineManager.beginFrameRendering += OnBeginFrameRendering;
                RenderPipelineManager.endFrameRendering += OnEndFrameRendering;
            }
        }

        private void OnDestroy()
        {
            if (trackDetailedRenderingStats)
            {
                RenderPipelineManager.beginFrameRendering -= OnBeginFrameRendering;
                RenderPipelineManager.endFrameRendering -= OnEndFrameRendering;
            }
        }

        private void OnBeginFrameRendering(ScriptableRenderContext context, Camera[] cameras)
        {
            frameStopwatch.Restart();
        }

        private void OnEndFrameRendering(ScriptableRenderContext context, Camera[] cameras)
        {
            frameStopwatch.Stop();
            lastFrameTime = (float)frameStopwatch.Elapsed.TotalMilliseconds;

            frameTimeHistory.Enqueue(lastFrameTime);
            if (frameTimeHistory.Count > 60)
            {
                frameTimeHistory.Dequeue();
            }

            float sum = 0f;
            foreach (float time in frameTimeHistory)
            {
                sum += time;
            }
            avgFrameTime = sum / frameTimeHistory.Count;
        }

        private void Update()
        {
            // Toggle visibility
            if (Input.GetKeyDown(toggleKey))
            {
                isVisible = !isVisible;
            }

            // Update FPS calculation
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            fps = 1.0f / deltaTime;

            // Track min/max FPS
            if (fps < minFps) minFps = fps;
            if (fps > maxFps && fps < 1000f) maxFps = fps;

            // Update FPS history
            fpsHistory.Enqueue(fps);
            if (fpsHistory.Count > 60)
            {
                fpsHistory.Dequeue();
            }

            // Calculate average FPS
            float sum = 0f;
            foreach (float f in fpsHistory)
            {
                sum += f;
            }
            avgFps = sum / fpsHistory.Count;

            timeSinceUpdate += Time.unscaledDeltaTime;

            // Update detailed stats at specified interval
            if (timeSinceUpdate >= updateInterval)
            {
                UpdateDetailedStats();
                timeSinceUpdate = 0f;
            }
        }

        private void UpdateDetailedStats()
        {
            // Memory stats
            totalAllocatedMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            totalReservedMemory = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong();
            monoUsedSize = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
            monoHeapSize = UnityEngine.Profiling.Profiler.GetMonoHeapSizeLong();

            lastGCMemory = gcMemory;
            gcMemory = System.GC.GetTotalMemory(false);
            gcCollectionCount = System.GC.CollectionCount(0);

            // Count rendering resources
            renderTextureCount = Resources.FindObjectsOfTypeAll<RenderTexture>().Length;
            materialCount = Resources.FindObjectsOfTypeAll<Material>().Length;
            meshCount = Resources.FindObjectsOfTypeAll<Mesh>().Length;

            // Shadow settings
            shadowQuality = QualitySettings.shadows;
            shadowResolution = QualitySettings.shadowResolution;
            shadowCascades = QualitySettings.shadowCascades;
            shadowDistance = QualitySettings.shadowDistance;

            // Game object stats
            activeCameraCount = Camera.allCamerasCount;
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            activeGameObjectCount = allObjects.Length;

            // Count all active components
            activeComponentCount = 0;
            foreach (GameObject go in allObjects)
            {
                activeComponentCount += go.GetComponents<Component>().Length;
            }
        }

        private void OnGUI()
        {
            #if !UNITY_EDITOR
            if (!enableInBuilds && !UnityEngine.Debug.isDebugBuild)
            {
                return;
            }
            #endif

            if (!isVisible) return;

            if (backgroundStyle == null || textStyle == null)
            {
                InitializeStyles();
            }

            BuildDisplayText();

            // Calculate window size
            GUIContent content = new GUIContent(displayText.ToString());
            Vector2 size = textStyle.CalcSize(content);
            windowRect = new Rect(padding, padding, size.x + padding * 2, size.y + padding * 2);

            // Draw background
            GUI.Box(windowRect, GUIContent.none, backgroundStyle);

            // Draw text
            GUI.Label(new Rect(windowRect.x + padding, windowRect.y + padding, size.x, size.y),
                     displayText.ToString(), textStyle);
        }

        private void BuildDisplayText()
        {
            displayText.Clear();

            if (compactMode)
            {
                BuildCompactDisplay();
            }
            else
            {
                BuildDetailedDisplay();
            }
        }

        private void BuildCompactDisplay()
        {
            displayText.AppendLine($"<b>FPS:</b> {GetColoredValue(fps, 60f, 30f)} ({deltaTime * 1000f:F1}ms) | <b>Avg:</b> {avgFps:F1}");
            displayText.AppendLine($"<b>RAM:</b> {FormatBytes(totalAllocatedMemory)} / {FormatBytes(totalReservedMemory)}");
            displayText.AppendLine($"<b>GPU:</b> {SystemInfo.graphicsDeviceName}");
            displayText.AppendLine($"<b>Objects:</b> {activeGameObjectCount} | <b>Components:</b> {activeComponentCount}");
        }

        private void BuildDetailedDisplay()
        {
            // Header
            displayText.AppendLine("<b>=== ADVANCED PERFORMANCE MONITOR ===</b>");
            displayText.AppendLine($"<color=grey>[{toggleKey} to toggle | C to toggle compact mode]</color>");
            displayText.AppendLine();

            // Frame Rate & Timing
            displayText.AppendLine("<b>═══ FRAME RATE ═══</b>");
            displayText.AppendLine($"  Current FPS: {GetColoredValue(fps, 60f, 30f)}");
            displayText.AppendLine($"  Frame Time: {GetColoredFrameTime(deltaTime * 1000f, 16.67f, 33.33f)}");
            displayText.AppendLine($"  Average FPS: {avgFps:F1}");
            displayText.AppendLine($"  Min FPS: {minFps:F1} | Max FPS: {maxFps:F1}");
            if (trackDetailedRenderingStats)
            {
                displayText.AppendLine($"  Render Time: {avgFrameTime:F2}ms");
            }
            displayText.AppendLine();

            // Memory Stats
            displayText.AppendLine("<b>═══ MEMORY USAGE ═══</b>");
            displayText.AppendLine($"  Unity Allocated: {FormatBytes(totalAllocatedMemory)}");
            displayText.AppendLine($"  Unity Reserved: {FormatBytes(totalReservedMemory)}");
            displayText.AppendLine($"  Mono Used: {FormatBytes(monoUsedSize)} / {FormatBytes(monoHeapSize)}");
            displayText.AppendLine($"  GC Memory: {FormatBytes(gcMemory)}");
            displayText.AppendLine($"  GC Collections: {gcCollectionCount}");

            long memoryDelta = gcMemory - lastGCMemory;
            if (memoryDelta != 0)
            {
                string deltaColor = memoryDelta > 0 ? "red" : "green";
                string deltaSign = memoryDelta > 0 ? "+" : "";
                displayText.AppendLine($"  <color={deltaColor}>  Delta: {deltaSign}{FormatBytes(memoryDelta)}</color>");
            }
            displayText.AppendLine();

            // System Info
            displayText.AppendLine("<b>═══ SYSTEM INFO ═══</b>");
            displayText.AppendLine($"  GPU: {SystemInfo.graphicsDeviceName}");
            displayText.AppendLine($"  GPU Memory: {SystemInfo.graphicsMemorySize} MB");
            displayText.AppendLine($"  GPU API: {SystemInfo.graphicsDeviceType}");
            displayText.AppendLine($"  CPU: {SystemInfo.processorType}");
            displayText.AppendLine($"  CPU Cores: {SystemInfo.processorCount} ({SystemInfo.processorFrequency} MHz)");
            displayText.AppendLine($"  System RAM: {SystemInfo.systemMemorySize} MB");
            displayText.AppendLine();

            // Graphics Settings
            displayText.AppendLine("<b>═══ GRAPHICS SETTINGS ═══</b>");
            displayText.AppendLine($"  Quality: {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
            displayText.AppendLine($"  Resolution: {Screen.width}x{Screen.height} @ {Screen.currentResolution.refreshRateRatio.value:F0}Hz");
            displayText.AppendLine($"  Fullscreen: {Screen.fullScreenMode}");
            displayText.AppendLine($"  VSync: {(QualitySettings.vSyncCount > 0 ? "On" : "Off")}");
            displayText.AppendLine($"  Target FPS: {(Application.targetFrameRate <= 0 ? "Unlimited" : Application.targetFrameRate.ToString())}");
            displayText.AppendLine($"  Anti-Aliasing: {QualitySettings.antiAliasing}x");
            displayText.AppendLine($"  Anisotropic: {QualitySettings.anisotropicFiltering}");
            displayText.AppendLine($"  Texture Quality: {QualitySettings.globalTextureMipmapLimit}");
            displayText.AppendLine();

            // Shadow Settings
            displayText.AppendLine("<b>═══ SHADOW SETTINGS ═══</b>");
            displayText.AppendLine($"  Quality: {shadowQuality}");
            displayText.AppendLine($"  Resolution: {shadowResolution}");
            displayText.AppendLine($"  Cascades: {shadowCascades}");
            displayText.AppendLine($"  Distance: {shadowDistance:F1}m");
            displayText.AppendLine($"  Shadow Projection: {QualitySettings.shadowProjection}");
            displayText.AppendLine();

            // Rendering Info
            displayText.AppendLine("<b>═══ RENDERING INFO ═══</b>");
            var pipeline = GraphicsSettings.currentRenderPipeline;
            displayText.AppendLine($"  Pipeline: {(pipeline != null ? pipeline.GetType().Name : "Built-in")}");
            displayText.AppendLine($"  Active Cameras: {activeCameraCount}");
            displayText.AppendLine($"  RenderTextures: {renderTextureCount}");
            displayText.AppendLine($"  Materials: {materialCount}");
            displayText.AppendLine($"  Meshes: {meshCount}");
            displayText.AppendLine();

            // Scene Stats
            displayText.AppendLine("<b>═══ SCENE STATS ═══</b>");
            displayText.AppendLine($"  Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            displayText.AppendLine($"  GameObjects: {activeGameObjectCount}");
            displayText.AppendLine($"  Components: {activeComponentCount}");
            displayText.AppendLine($"  Time Scale: {Time.timeScale:F2}");
            displayText.AppendLine($"  Play Time: {Time.realtimeSinceStartup:F1}s");
        }

        private string GetColoredValue(float value, float goodThreshold, float badThreshold)
        {
            Color color = textColor;

            if (value >= goodThreshold)
            {
                color = goodColor;
            }
            else if (value >= badThreshold)
            {
                color = warningColor;
            }
            else
            {
                color = badColor;
            }

            return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{value:F1}</color>";
        }

        private string GetColoredFrameTime(float ms, float goodThreshold, float badThreshold)
        {
            Color color = textColor;

            if (ms <= goodThreshold)
            {
                color = goodColor;
            }
            else if (ms <= badThreshold)
            {
                color = warningColor;
            }
            else
            {
                color = badColor;
            }

            return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{ms:F2}ms</color>";
        }

        private string FormatBytes(long bytes)
        {
            if (bytes < 0) bytes = 0;

            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:F2} {sizes[order]}";
        }

        private void InitializeStyles()
        {
            // Background style
            backgroundStyle = new GUIStyle(GUI.skin.box);
            Texture2D bgTexture = new Texture2D(1, 1);
            bgTexture.SetPixel(0, 0, backgroundColor);
            bgTexture.Apply();
            backgroundStyle.normal.background = bgTexture;

            // Text style
            textStyle = new GUIStyle(GUI.skin.label);
            textStyle.fontSize = fontSize;
            textStyle.richText = true;
            textStyle.normal.textColor = textColor;
            textStyle.alignment = TextAnchor.UpperLeft;
            textStyle.wordWrap = false;
        }

        [ContextMenu("Reset Stats")]
        public void ResetStats()
        {
            minFps = float.MaxValue;
            maxFps = 0f;
            fpsHistory.Clear();
            frameTimeHistory.Clear();
        }

        [ContextMenu("Toggle Compact Mode")]
        public void ToggleCompactMode()
        {
            compactMode = !compactMode;
        }

        public void Toggle()
        {
            isVisible = !isVisible;
        }

        public void Show()
        {
            isVisible = true;
        }

        public void Hide()
        {
            isVisible = false;
        }
    }
}
