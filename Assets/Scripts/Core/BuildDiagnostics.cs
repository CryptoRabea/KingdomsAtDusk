using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using System.Text;

namespace RTS.Core
{
    /// <summary>
    /// Runtime diagnostics for build issues.
    /// Press 'D' key in build to show detailed diagnostics.
    /// Helps identify performance bottlenecks, GPU issues, and configuration problems.
    /// </summary>
    public class BuildDiagnostics : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Key diagnosticsKey = Key.D;
        [SerializeField] private bool showOnStartup = false;
        [SerializeField] private bool showGUI = true;

        private bool showDiagnostics = false;
        private string diagnosticsText = "";
        private GUIStyle textStyle;
        private float lastUpdateTime;
        private const float updateInterval = 1f;

        // Performance tracking
        private float fps;
        private float frameTime;
        private int drawCalls;
        private int batches;

        private void Start()
        {
            if (showOnStartup)
            {
                UpdateDiagnostics();
                showDiagnostics = true;
            }

            // Initialize GUI style
            textStyle = new GUIStyle();
            textStyle.fontSize = 14;
            textStyle.normal.textColor = Color.white;
            textStyle.fontStyle = FontStyle.Normal;
            textStyle.wordWrap = false;
        }

        private void Update()
        {
            // Toggle diagnostics with key
            if (Keyboard.current != null && Keyboard.current[diagnosticsKey].wasPressedThisFrame)
            {
                showDiagnostics = !showDiagnostics;
                if (showDiagnostics)
                {
                    UpdateDiagnostics();
                }
            }

            // Update diagnostics periodically when visible
            if (showDiagnostics && Time.time - lastUpdateTime > updateInterval)
            {
                UpdateDiagnostics();
                lastUpdateTime = Time.time;
            }

            // Track performance metrics
            UpdatePerformanceMetrics();
        }

        private void UpdatePerformanceMetrics()
        {
            fps = 1f / Time.unscaledDeltaTime;
            frameTime = Time.unscaledDeltaTime * 1000f; // Convert to milliseconds
        }

        private void UpdateDiagnostics()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("=== BUILD DIAGNOSTICS ===");
            sb.AppendLine($"Press '{diagnosticsKey}' to toggle this display");
            sb.AppendLine();

            // Performance Metrics
            sb.AppendLine("--- PERFORMANCE ---");
            sb.AppendLine($"FPS: {fps:F1}");
            sb.AppendLine($"Frame Time: {frameTime:F2}ms");
            sb.AppendLine($"VSync: {(QualitySettings.vSyncCount > 0 ? "ON [!]" : "OFF OK")}");
            sb.AppendLine($"Target FPS: {(Application.targetFrameRate <= 0 ? "Unlimited OK" : Application.targetFrameRate.ToString())}");
            sb.AppendLine($"Time Scale: {Time.timeScale}");
            sb.AppendLine();

            // Quality Settings
            sb.AppendLine("--- QUALITY SETTINGS ---");
            sb.AppendLine($"Quality Level: {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
            sb.AppendLine($"Shadow Quality: {QualitySettings.shadows}");
            sb.AppendLine($"Shadow Resolution: {QualitySettings.shadowResolution}");
            sb.AppendLine($"Shadow Distance: {QualitySettings.shadowDistance}m");
            sb.AppendLine($"Pixel Light Count: {QualitySettings.pixelLightCount}");
            sb.AppendLine($"Anisotropic Filtering: {QualitySettings.anisotropicFiltering}");
            sb.AppendLine();

            // Graphics Settings
            sb.AppendLine("--- GRAPHICS ---");
            sb.AppendLine($"Device: {SystemInfo.graphicsDeviceName}");
            sb.AppendLine($"API: {SystemInfo.graphicsDeviceType}");
            sb.AppendLine($"GPU Memory: {SystemInfo.graphicsMemorySize}MB");
            sb.AppendLine($"Shader Level: {SystemInfo.graphicsShaderLevel}");
            sb.AppendLine($"Max Texture Size: {SystemInfo.maxTextureSize}");
            sb.AppendLine($"NPOT Support: {SystemInfo.npotSupport}");
            sb.AppendLine();

            // Screen Settings
            sb.AppendLine("--- DISPLAY ---");
            sb.AppendLine($"Resolution: {Screen.width}x{Screen.height}");
            sb.AppendLine($"Refresh Rate: {Screen.currentResolution.refreshRateRatio.value:F0}Hz");
            sb.AppendLine($"Fullscreen: {Screen.fullScreenMode}");
            sb.AppendLine($"DPI: {Screen.dpi}");
            sb.AppendLine();

            // System Info
            sb.AppendLine("--- SYSTEM ---");
            sb.AppendLine($"OS: {SystemInfo.operatingSystem}");
            sb.AppendLine($"CPU: {SystemInfo.processorType}");
            sb.AppendLine($"CPU Cores: {SystemInfo.processorCount}");
            sb.AppendLine($"System Memory: {SystemInfo.systemMemorySize}MB");
            sb.AppendLine($"Battery: {(SystemInfo.batteryStatus == BatteryStatus.Unknown ? "Desktop/Unknown" : SystemInfo.batteryStatus.ToString())}");
            if (SystemInfo.batteryStatus != BatteryStatus.Unknown)
            {
                sb.AppendLine($"Battery Level: {(SystemInfo.batteryLevel * 100):F0}%");
            }
            sb.AppendLine();

            // Texture Streaming
            sb.AppendLine("--- TEXTURE STREAMING ---");
            sb.AppendLine($"Active: {(QualitySettings.streamingMipmapsActive ? "YES OK" : "NO [!]")}");
            if (QualitySettings.streamingMipmapsActive)
            {
                sb.AppendLine($"Memory Budget: {QualitySettings.streamingMipmapsMemoryBudget}MB");
                sb.AppendLine($"Max Level Reduction: {QualitySettings.streamingMipmapsMaxLevelReduction}");
            }
            sb.AppendLine();

            // Potential Issues
            sb.AppendLine("--- ISSUE DETECTION ---");
            bool hasIssues = false;

            if (QualitySettings.vSyncCount > 0)
            {
                sb.AppendLine("[!] VSync is ON - May limit FPS!");
                hasIssues = true;
            }

            if (Application.targetFrameRate > 0 && Application.targetFrameRate < 60)
            {
                sb.AppendLine($"[!] Target FPS is {Application.targetFrameRate} - Too low!");
                hasIssues = true;
            }

            if (fps < 30 && Time.time > 5f)
            {
                sb.AppendLine($"[!] Low FPS detected ({fps:F0}) - Check GPU selection!");
                hasIssues = true;
            }

            if (!QualitySettings.streamingMipmapsActive)
            {
                sb.AppendLine("[!] Texture streaming disabled - May cause texture issues!");
                hasIssues = true;
            }

            if (SystemInfo.graphicsDeviceName.Contains("Intel") &&
                !SystemInfo.graphicsDeviceName.Contains("Arc"))
            {
                sb.AppendLine("[!] Using Intel integrated GPU - Check Windows Graphics Settings!");
                hasIssues = true;
            }

            if (SystemInfo.batteryStatus != BatteryStatus.Unknown &&
                SystemInfo.batteryStatus != BatteryStatus.Charging &&
                SystemInfo.batteryLevel < 0.2f)
            {
                sb.AppendLine("[!] Low battery - Performance may be throttled!");
                hasIssues = true;
            }

            if (!hasIssues)
            {
                sb.AppendLine("OK No obvious issues detected!");
            }

            sb.AppendLine();
            sb.AppendLine("=== END DIAGNOSTICS ===");

            diagnosticsText = sb.ToString();

            // Also log to console
        }

        private void OnGUI()
        {
            if (!showGUI || !showDiagnostics) return;

            // Semi-transparent background
            GUI.Box(new Rect(10, 10, 500, Screen.height - 20), "");

            // Display diagnostics text
            GUI.Label(new Rect(20, 20, 480, Screen.height - 40), diagnosticsText, textStyle);
        }

        // Public API
        public void ShowDiagnostics()
        {
            UpdateDiagnostics();
            showDiagnostics = true;
        }

        public void HideDiagnostics()
        {
            showDiagnostics = false;
        }

        public string GetDiagnosticsText()
        {
            UpdateDiagnostics();
            return diagnosticsText;
        }
    }
}
