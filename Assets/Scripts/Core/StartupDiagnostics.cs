using UnityEngine;
using System.IO;
using System.Text;
using System;

namespace RTS.Core
{
    /// <summary>
    /// Emergency startup diagnostics to help debug black screen and crash issues.
    /// Writes detailed logs to a file to identify where initialization is failing.
    /// </summary>
    public class StartupDiagnostics : MonoBehaviour
    {
        private static string logFilePath;
        private static bool isInitialized = false;
        private static StringBuilder logBuffer = new StringBuilder();
        private static float startTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void OnAfterAssembliesLoaded()
        {
            try
            {
                startTime = Time.realtimeSinceStartup;
                string logDir = Path.Combine(Application.persistentDataPath, "Logs");
                Directory.CreateDirectory(logDir);
                logFilePath = Path.Combine(logDir, $"startup_diagnostic_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

                Log("=== STARTUP DIAGNOSTICS ===");
                Log($"Time: {DateTime.Now}");
                Log($"Unity Version: {Application.unityVersion}");
                Log($"Platform: {Application.platform}");
                Log($"Data Path: {Application.persistentDataPath}");
                Log($"Log File: {logFilePath}");
                Log("=== ASSEMBLIES LOADED ===");

                isInitialized = true;

                // Create GameObject to persist through scene loads
                GameObject diagnosticsObj = new GameObject("StartupDiagnostics");
                StartupDiagnostics diagnostics = diagnosticsObj.AddComponent<StartupDiagnostics>();
                DontDestroyOnLoad(diagnosticsObj);
            }
            catch (Exception e)
            {
                Debug.LogError($"StartupDiagnostics initialization failed: {e.Message}");
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void OnBeforeSplashScreen()
        {
            Log("=== BEFORE SPLASH SCREEN ===");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnBeforeSceneLoad()
        {
            Log("=== BEFORE SCENE LOAD ===");
            Log($"Graphics Device: {SystemInfo.graphicsDeviceName}");
            Log($"Graphics API: {SystemInfo.graphicsDeviceType}");
            Log($"GPU Memory: {SystemInfo.graphicsMemorySize}MB");
            Log($"CPU: {SystemInfo.processorType} ({SystemInfo.processorCount} cores)");
            Log($"System RAM: {SystemInfo.systemMemorySize}MB");
            Log($"OS: {SystemInfo.operatingSystem}");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnAfterSceneLoad()
        {
            Log("=== AFTER SCENE LOAD ===");
            Log($"Active Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");

            // Check for main camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Log($"Main Camera found: {mainCam.name}");
                Log($"Camera enabled: {mainCam.enabled}");
                Log($"Camera depth: {mainCam.depth}");
                Log($"Camera culling mask: {mainCam.cullingMask}");
            }
            else
            {
                Log("WARNING: No main camera found!");
            }

            // Check for lights
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            Log($"Lights in scene: {lights.Length}");

            // Check for renderers
            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            Log($"Renderers in scene: {renderers.Length}");

            FlushLog();
        }

        private void Awake()
        {
            Log($"[{GetElapsedTime()}] StartupDiagnostics.Awake()");
        }

        private void Start()
        {
            Log($"[{GetElapsedTime()}] StartupDiagnostics.Start()");
            StartCoroutine(MonitorStartup());
        }

        private System.Collections.IEnumerator MonitorStartup()
        {
            Log($"[{GetElapsedTime()}] Monitoring startup...");

            // Monitor for 10 seconds
            for (int i = 0; i < 10; i++)
            {
                yield return new WaitForSeconds(1f);
                Log($"[{GetElapsedTime()}] Still running... FPS: {(1f / Time.unscaledDeltaTime):F1}");

                // Check if screen is still black
                if (i == 3)
                {
                    Log($"[{GetElapsedTime()}] 3 seconds elapsed - checking system state...");
                    Log($"Screen resolution: {Screen.width}x{Screen.height}");
                    Log($"VSync: {QualitySettings.vSyncCount}");
                    Log($"Target FPS: {Application.targetFrameRate}");
                    Log($"Quality Level: {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
                    FlushLog();
                }
            }

            Log($"[{GetElapsedTime()}] Startup monitoring complete");
            Log("=== IF YOU SEE THIS, THE GAME IS RUNNING ===");
            FlushLog();

            // Show user where log is
            Debug.Log($"[StartupDiagnostics] Startup log saved to: {logFilePath}");
            Debug.Log($"[StartupDiagnostics] Game appears to be running normally");
        }

        private static void Log(string message)
        {
            if (!isInitialized) return;

            string timestampedMessage = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
            logBuffer.AppendLine(timestampedMessage);
            Debug.Log($"[StartupDiag] {message}");

            // Auto-flush every 10 lines
            if (logBuffer.Length > 1000)
            {
                FlushLog();
            }
        }

        private static void FlushLog()
        {
            if (!isInitialized || string.IsNullOrEmpty(logFilePath)) return;

            try
            {
                File.AppendAllText(logFilePath, logBuffer.ToString());
                logBuffer.Clear();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to write startup log: {e.Message}");
            }
        }

        private static string GetElapsedTime()
        {
            return $"{(Time.realtimeSinceStartup - startTime):F2}s";
        }

        private void OnApplicationQuit()
        {
            Log($"[{GetElapsedTime()}] Application quitting");
            Log("=== END DIAGNOSTICS ===");
            FlushLog();
        }

        private void OnDestroy()
        {
            FlushLog();
        }
    }
}
