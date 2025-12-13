using UnityEngine;
using System.Text;

namespace CircularLensVision
{
    /// <summary>
    /// Debug and testing tool for Circular Lens Vision system.
    /// Provides runtime controls and performance monitoring.
    /// </summary>
    public class LensVisionDebug : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CircularLensVision lensController;

        [Header("Debug Display")]
        [Tooltip("Show on-screen debug info")]
        [SerializeField] private bool showDebugUI = true;

        [Tooltip("Position of debug UI")]
        [SerializeField] private Vector2 debugUIPosition = new Vector2(10, 10);

        [Header("Runtime Controls")]
        [Tooltip("Enable keyboard controls (R to increase radius, F to decrease)")]
        [SerializeField] private bool enableKeyboardControls = true;

        [Tooltip("Key to increase lens radius")]
        [SerializeField] private KeyCode increaseRadiusKey = KeyCode.R;

        [Tooltip("Key to decrease lens radius")]
        [SerializeField] private KeyCode decreaseRadiusKey = KeyCode.F;

        [Tooltip("Key to toggle lens active state")]
        [SerializeField] private KeyCode toggleActiveKey = KeyCode.T;

        [Tooltip("Radius adjustment amount per key press")]
        [SerializeField] private float radiusAdjustAmount = 5f;

        [Header("Performance Monitoring")]
        [Tooltip("Track performance metrics")]
        [SerializeField] private bool trackPerformance = true;

        [Tooltip("Update performance stats every N seconds")]
        [SerializeField] private float performanceUpdateInterval = 1f;

        // Performance tracking
        private int frameCount;
        private float fpsTimer;
        private float currentFPS;
        private float performanceTimer;

        // Stats
        private int activeTargetCount;
        private int totalTargetCount;
        private float lastUpdateTime;

        private GUIStyle debugStyle;

        private void Awake()
        {
            if (lensController == null)
            {
                lensController = GetComponent<CircularLensVision>();
            }

            if (lensController == null)
            {
                lensController = FindFirstObjectByType<CircularLensVision>();
            }

            if (lensController == null)
            {
                Debug.LogWarning("LensVisionDebug: No CircularLensVision controller found!", this);
            }
        }

        private void Update()
        {
            if (lensController == null) return;

            // Handle keyboard controls
            if (enableKeyboardControls)
            {
                HandleKeyboardInput();
            }

            // Update performance metrics
            if (trackPerformance)
            {
                UpdatePerformanceMetrics();
            }

            // Update stats
            performanceTimer += Time.deltaTime;
            if (performanceTimer >= performanceUpdateInterval)
            {
                performanceTimer = 0f;
                UpdateStats();
            }
        }

        private void HandleKeyboardInput()
        {
            // Increase radius
            if (Input.GetKeyDown(increaseRadiusKey))
            {
                float newRadius = lensController.LensRadius + radiusAdjustAmount;
                lensController.SetLensRadius(newRadius);
                Debug.Log($"Lens radius increased to: {newRadius}");
            }

            // Decrease radius
            if (Input.GetKeyDown(decreaseRadiusKey))
            {
                float newRadius = Mathf.Max(5f, lensController.LensRadius - radiusAdjustAmount);
                lensController.SetLensRadius(newRadius);
                Debug.Log($"Lens radius decreased to: {newRadius}");
            }

            // Toggle active state
            if (Input.GetKeyDown(toggleActiveKey))
            {
                lensController.IsActive = !lensController.IsActive;
                Debug.Log($"Lens vision {(lensController.IsActive ? "enabled" : "disabled")}");
            }
        }

        private void UpdatePerformanceMetrics()
        {
            frameCount++;
            fpsTimer += Time.deltaTime;

            if (fpsTimer >= 1f)
            {
                currentFPS = frameCount / fpsTimer;
                frameCount = 0;
                fpsTimer = 0f;
            }
        }

        private void UpdateStats()
        {
            // Count active targets
            LensVisionTarget[] allTargets = FindObjectsByType<LensVisionTarget>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            totalTargetCount = allTargets.Length;
            activeTargetCount = 0;

            foreach (var target in allTargets)
            {
                if (target.IsLensActive)
                {
                    activeTargetCount++;
                }
            }
        }

        private void OnGUI()
        {
            if (!showDebugUI || lensController == null) return;

            // Initialize GUI style
            if (debugStyle == null)
            {
                debugStyle = new GUIStyle(GUI.skin.box);
                debugStyle.alignment = TextAnchor.UpperLeft;
                debugStyle.fontSize = 12;
                debugStyle.normal.textColor = Color.white;
                debugStyle.normal.background = MakeBackgroundTexture(2, 2, new Color(0, 0, 0, 0.7f));
            }

            // Build debug text
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== CIRCULAR LENS VISION DEBUG ===");
            sb.AppendLine();
            sb.AppendLine($"Active: {(lensController.IsActive ? "YES" : "NO")}");
            sb.AppendLine($"Lens Radius: {lensController.LensRadius:F1}m");
            sb.AppendLine($"Lens Center: {lensController.LensCenter}");
            sb.AppendLine();
            sb.AppendLine("--- Targets ---");
            sb.AppendLine($"Total Targets: {totalTargetCount}");
            sb.AppendLine($"Active in Lens: {activeTargetCount}");
            sb.AppendLine();

            if (trackPerformance)
            {
                sb.AppendLine("--- Performance ---");
                sb.AppendLine($"FPS: {currentFPS:F1}");
                sb.AppendLine($"Frame Time: {(Time.deltaTime * 1000f):F2}ms");
                sb.AppendLine();
            }

            if (enableKeyboardControls)
            {
                sb.AppendLine("--- Controls ---");
                sb.AppendLine($"[{increaseRadiusKey}] Increase Radius");
                sb.AppendLine($"[{decreaseRadiusKey}] Decrease Radius");
                sb.AppendLine($"[{toggleActiveKey}] Toggle Active");
            }

            // Draw debug box
            Vector2 size = debugStyle.CalcSize(new GUIContent(sb.ToString()));
            GUI.Box(new Rect(debugUIPosition.x, debugUIPosition.y, size.x + 20, size.y + 20), sb.ToString(), debugStyle);
        }

        private Texture2D MakeBackgroundTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Log detailed information about all lens vision targets
        /// </summary>
        [ContextMenu("Log All Targets Info")]
        public void LogAllTargetsInfo()
        {
            LensVisionTarget[] targets = FindObjectsByType<LensVisionTarget>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"=== LENS VISION TARGETS ({targets.Length} total) ===");

            int unitCount = 0;
            int obstacleCount = 0;
            int activeCount = 0;

            foreach (var target in targets)
            {
                if (target.Type == LensVisionTarget.TargetType.Unit)
                    unitCount++;
                else
                    obstacleCount++;

                if (target.IsLensActive)
                    activeCount++;

                sb.AppendLine($"- {target.gameObject.name}: {target.Type}, Active: {target.IsLensActive}");
            }

            sb.AppendLine();
            sb.AppendLine($"Summary: {unitCount} units, {obstacleCount} obstacles, {activeCount} active");

            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Force refresh all targets
        /// </summary>
        [ContextMenu("Refresh All Targets")]
        public void RefreshAllTargets()
        {
            LensVisionTarget[] targets = FindObjectsByType<LensVisionTarget>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (var target in targets)
            {
                target.RefreshLensState();
            }

            Debug.Log($"Refreshed {targets.Length} lens vision targets.");
        }

        /// <summary>
        /// Test performance with different radius values
        /// </summary>
        [ContextMenu("Run Performance Test")]
        public void RunPerformanceTest()
        {
            if (lensController == null)
            {
                Debug.LogWarning("Cannot run performance test: No lens controller!");
                return;
            }

            Debug.Log("Starting performance test...");
            StartCoroutine(PerformanceTestCoroutine());
        }

        private System.Collections.IEnumerator PerformanceTestCoroutine()
        {
            float[] testRadii = { 10f, 20f, 30f, 50f, 75f, 100f };
            StringBuilder results = new StringBuilder();
            results.AppendLine("=== PERFORMANCE TEST RESULTS ===");

            foreach (float radius in testRadii)
            {
                lensController.SetLensRadius(radius);
                yield return new WaitForSeconds(2f); // Let it stabilize

                // Measure FPS over 2 seconds
                int frames = 0;
                float elapsed = 0f;
                float testDuration = 2f;

                while (elapsed < testDuration)
                {
                    frames++;
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                float avgFPS = frames / elapsed;
                results.AppendLine($"Radius {radius}m: {avgFPS:F1} FPS (Active targets: {activeTargetCount})");
            }

            Debug.Log(results.ToString());
        }
    }
}
