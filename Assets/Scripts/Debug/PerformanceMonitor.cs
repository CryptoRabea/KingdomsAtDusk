using UnityEngine;
using UnityEngine.Rendering;
using System.Text;
using System.Collections.Generic;

namespace KingdomsAtDusk.Debug
{
    /// <summary>
    /// Comprehensive performance monitoring system that displays FPS, memory, GPU, CPU,
    /// and rendering statistics even in production builds.
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        [Header("Display Settings")]
        [SerializeField] private bool showOnStart = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F3;
        [SerializeField] private bool enableInBuilds = true;

        [Header("Update Settings")]
        [SerializeField] private float updateInterval = 0.5f; // Update display every 0.5 seconds

        [Header("UI Settings")]
        [SerializeField] private int fontSize = 14;
        [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.7f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color goodColor = Color.green;
        [SerializeField] private Color warningColor = Color.yellow;
        [SerializeField] private Color badColor = Color.red;
        [SerializeField] private int padding = 10;

        // Performance tracking
        private bool isVisible;
        private float deltaTime;
        private float fps;
        private float avgFps;
        private float minFps = float.MaxValue;
        private float maxFps = 0f;
        private float timeSinceUpdate;
        private int frameCount;

        // Memory tracking
        private long totalMemory;
        private long allocatedMemory;
        private long reservedMemory;
        private long monoUsedSize;
        private long monoHeapSize;

        // Rendering stats
        private int drawCalls;
        private int batches;
        private int triangles;
        private int vertices;
        private int setPassCalls;

        // Shadow info
        private ShadowQuality shadowQuality;
        private ShadowResolution shadowResolution;
        private int shadowCascades;

        // Display
        private GUIStyle backgroundStyle;
        private GUIStyle textStyle;
        private StringBuilder displayText;
        private Rect windowRect;

        // FPS history for averaging
        private Queue<float> fpsHistory = new Queue<float>(60);

        private void Awake()
        {
            isVisible = showOnStart;
            displayText = new StringBuilder(1024);

            // Make persistent across scenes
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitializeStyles();
        }

        private void Update()
        {
            // Toggle visibility
            if (Input.GetKeyDown(toggleKey))
            {
                isVisible = !isVisible;
            }

            // Update FPS calculation every frame for smooth readings
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            fps = 1.0f / deltaTime;

            // Track min/max FPS
            if (fps < minFps) minFps = fps;
            if (fps > maxFps && fps < 1000f) maxFps = fps; // Cap max to avoid initial spike

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

            frameCount++;
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
            totalMemory = System.GC.GetTotalMemory(false);
            allocatedMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            reservedMemory = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong();
            monoUsedSize = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
            monoHeapSize = UnityEngine.Profiling.Profiler.GetMonoHeapSizeLong();

            // Rendering stats (from FrameDebugger and RenderPipeline)
            // Note: These are approximate in builds
            drawCalls = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(null) > 0 ? 0 : 0;

            // Shadow settings
            shadowQuality = QualitySettings.shadows;
            shadowResolution = QualitySettings.shadowResolution;
            shadowCascades = QualitySettings.shadowCascades;
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

            // Calculate window size based on content
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

            // Header
            displayText.AppendLine("<b>=== PERFORMANCE MONITOR ===</b>");
            displayText.AppendLine($"<color=grey>[Press {toggleKey} to toggle]</color>");
            displayText.AppendLine();

            // FPS Stats
            displayText.AppendLine("<b>Frame Rate:</b>");
            displayText.AppendLine($"  FPS: {GetColoredValue(fps, 60f, 30f)} ({deltaTime * 1000f:F1}ms)");
            displayText.AppendLine($"  Avg: {avgFps:F1} | Min: {minFps:F1} | Max: {maxFps:F1}");
            displayText.AppendLine();

            // Memory Stats
            displayText.AppendLine("<b>Memory Usage:</b>");
            displayText.AppendLine($"  Total Allocated: {FormatBytes(allocatedMemory)}");
            displayText.AppendLine($"  Total Reserved: {FormatBytes(reservedMemory)}");
            displayText.AppendLine($"  Mono Used: {FormatBytes(monoUsedSize)}");
            displayText.AppendLine($"  Mono Heap: {FormatBytes(monoHeapSize)}");
            displayText.AppendLine($"  GC Memory: {FormatBytes(totalMemory)}");
            displayText.AppendLine();

            // System Info
            displayText.AppendLine("<b>System Info:</b>");
            displayText.AppendLine($"  GPU: {SystemInfo.graphicsDeviceName}");
            displayText.AppendLine($"  GPU Memory: {SystemInfo.graphicsMemorySize} MB");
            displayText.AppendLine($"  CPU: {SystemInfo.processorType}");
            displayText.AppendLine($"  CPU Cores: {SystemInfo.processorCount}");
            displayText.AppendLine($"  System Memory: {SystemInfo.systemMemorySize} MB");
            displayText.AppendLine();

            // Graphics Settings
            displayText.AppendLine("<b>Graphics Settings:</b>");
            displayText.AppendLine($"  Quality Level: {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
            displayText.AppendLine($"  VSync: {QualitySettings.vSyncCount}");
            displayText.AppendLine($"  Target FPS: {Application.targetFrameRate}");
            displayText.AppendLine($"  Resolution: {Screen.width}x{Screen.height} @ {Screen.currentResolution.refreshRateRatio.value:F0}Hz");
            displayText.AppendLine($"  Fullscreen: {Screen.fullScreenMode}");
            displayText.AppendLine();

            // Shadow Settings
            displayText.AppendLine("<b>Shadow Settings:</b>");
            displayText.AppendLine($"  Quality: {shadowQuality}");
            displayText.AppendLine($"  Resolution: {shadowResolution}");
            displayText.AppendLine($"  Cascades: {shadowCascades}");
            displayText.AppendLine($"  Distance: {QualitySettings.shadowDistance:F1}m");
            displayText.AppendLine();

            // Rendering Stats (URP)
            displayText.AppendLine("<b>Rendering Info:</b>");
            displayText.AppendLine($"  Render Pipeline: {GraphicsSettings.currentRenderPipeline?.GetType().Name ?? "Built-in"}");
            displayText.AppendLine($"  Active Cameras: {Camera.allCamerasCount}");
            displayText.AppendLine();

            // Additional Stats
            displayText.AppendLine("<b>Additional Stats:</b>");
            displayText.AppendLine($"  Time Scale: {Time.timeScale:F2}");
            displayText.AppendLine($"  Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            displayText.AppendLine($"  Objects: {FindObjectsOfType<GameObject>().Length}");
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

        private string FormatBytes(long bytes)
        {
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

        /// <summary>
        /// Reset min/max FPS tracking
        /// </summary>
        [ContextMenu("Reset FPS Stats")]
        public void ResetFPSStats()
        {
            minFps = float.MaxValue;
            maxFps = 0f;
            fpsHistory.Clear();
        }

        /// <summary>
        /// Toggle the performance monitor visibility
        /// </summary>
        public void Toggle()
        {
            isVisible = !isVisible;
        }

        /// <summary>
        /// Show the performance monitor
        /// </summary>
        public void Show()
        {
            isVisible = true;
        }

        /// <summary>
        /// Hide the performance monitor
        /// </summary>
        public void Hide()
        {
            isVisible = false;
        }
    }
}
