using UnityEngine;
using FlowField.Core;
using FlowField.Performance;
// using FlowField.Debug; // Commented to avoid namespace collision with UnityEngine.Debug
using FlowField.Integration;
using Debug = UnityEngine.Debug;
using FlowField.Debug;
using Assets.Scripts.FlowField.Integration;

namespace FlowField.Setup
{
    /// <summary>
    /// Automated setup helper for Flow Field system
    /// Creates all necessary GameObjects and configures them
    /// Run this in Editor or at runtime for quick setup
    /// </summary>
    public class FlowFieldSetupHelper : MonoBehaviour
    {
        [Header("Auto-Setup Options")]
        [SerializeField] private bool setupOnAwake = false;
        [SerializeField] private bool includePerformanceManager = true;
        [SerializeField] private bool includeDebugVisualizer = true;
        [SerializeField] private bool includeCommandHandler = true;

        [Header("Grid Configuration")]
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private bool autoDetectBounds = true;

        [Header("Debug")]
        [SerializeField] private bool showSetupLog = true;

        private void Awake()
        {
            if (setupOnAwake)
            {
                SetupFlowFieldSystem();
            }
        }

        /// <summary>
        /// Complete automated setup - creates all necessary components
        /// </summary>
        [ContextMenu("Setup Flow Field System")]
        public void SetupFlowFieldSystem()
        {
            Log("🚀 Starting Flow Field System Setup...");

            // Step 1: Create Flow Field Manager
            CreateFlowFieldManager();

            // Step 2: Create Performance Manager (optional)
            if (includePerformanceManager)
            {
                CreatePerformanceManager();
            }

            // Step 3: Create Debug Visualizer (optional)
            if (includeDebugVisualizer)
            {
                CreateDebugVisualizer();
            }

            // Step 4: Create Command Handler (optional)
            if (includeCommandHandler)
            {
                CreateCommandHandler();
            }

            // Step 5: Verify setup
            VerifySetup();

            Log("✅ Flow Field System Setup Complete!");
            Log("💡 Next steps:");
            Log("   1. Hit Play to initialize grid");
            Log("   2. Add FlowFieldFollower to your units");
            Log("   3. Right-click to test movement");
        }

        private void CreateFlowFieldManager()
        {
            // Check if already exists
            FlowFieldManager existing = FindFirstObjectByType<FlowFieldManager>();
            if (existing != null)
            {
                Log("⚠️ FlowFieldManager already exists, skipping...");
                return;
            }

            // Create GameObject
            GameObject managerObj = new GameObject("FlowFieldManager");
            managerObj.tag = "GameController";

            // Add component
            FlowFieldManager manager = managerObj.AddComponent<FlowFieldManager>();

            // Configure via reflection (since fields are private)
            SetPrivateField(manager, "cellSize", cellSize);
            SetPrivateField(manager, "autoDetectGridBounds", autoDetectBounds);
            SetPrivateField(manager, "maxCachedFlowFields", 10);
            SetPrivateField(manager, "enableFlowFieldCaching", true);
            SetPrivateField(manager, "showGridGizmos", false);

            Log("✅ Created FlowFieldManager");
        }

        private void CreatePerformanceManager()
        {
            FlowFieldPerformanceManager existing = FindFirstObjectByType<FlowFieldPerformanceManager>();
            if (existing != null)
            {
                Log("⚠️ FlowFieldPerformanceManager already exists, skipping...");
                return;
            }

            GameObject perfObj = new GameObject("FlowFieldPerformanceManager");
            FlowFieldPerformanceManager perfManager = perfObj.AddComponent<FlowFieldPerformanceManager>();

            SetPrivateField(perfManager, "enableBatchedUpdates", true);
            SetPrivateField(perfManager, "unitsPerBatch", 50);
            SetPrivateField(perfManager, "batchesPerFrame", 4);
            SetPrivateField(perfManager, "enableLOD", true);
            SetPrivateField(perfManager, "showPerformanceStats", true);

            Log("✅ Created FlowFieldPerformanceManager");
        }

        private void CreateDebugVisualizer()
        {
            FlowFieldDebugVisualizer existing = FindFirstObjectByType<FlowFieldDebugVisualizer>();
            if (existing != null)
            {
                Log("⚠️ FlowFieldDebugVisualizer already exists, skipping...");
                return;
            }

            GameObject debugObj = new GameObject("FlowFieldDebugVisualizer");
            FlowFieldDebugVisualizer visualizer = debugObj.AddComponent<FlowFieldDebugVisualizer>();

            SetPrivateField(visualizer, "showFlowField", true);
            SetPrivateField(visualizer, "showUnitVelocities", true);
            SetPrivateField(visualizer, "displayEveryNthCell", 2);

            Log("✅ Created FlowFieldDebugVisualizer");
        }

        private void CreateCommandHandler()
        {
            FlowFieldRTSCommandHandler existing = FindFirstObjectByType<FlowFieldRTSCommandHandler>();
            if (existing != null)
            {
                Log("⚠️ FlowFieldRTSCommandHandler already exists, skipping...");
                return;
            }

            GameObject commandObj = new GameObject("FlowFieldCommandHandler");
            FlowFieldRTSCommandHandler handler = commandObj.AddComponent<FlowFieldRTSCommandHandler>();

            Log("✅ Created FlowFieldRTSCommandHandler");
            Log("💡 Formation Hotkeys:");
            Log("   F1 = Line, F2 = Column, F3 = Box");
            Log("   F4 = Wedge, F5 = Circle, F6 = Scatter");
        }

        private void VerifySetup()
        {
            Log("\n📋 Verification:");

            bool hasManager = FindFirstObjectByType<FlowFieldManager>() != null;
            Log($"   FlowFieldManager: {(hasManager ? "✅" : "❌")}");

            if (includePerformanceManager)
            {
                bool hasPerf = FindFirstObjectByType<FlowFieldPerformanceManager>() != null;
                Log($"   PerformanceManager: {(hasPerf ? "✅" : "❌")}");
            }

            if (includeDebugVisualizer)
            {
                bool hasDebug = FindFirstObjectByType<FlowFieldDebugVisualizer>() != null;
                Log($"   DebugVisualizer: {(hasDebug ? "✅" : "❌")}");
            }

            if (includeCommandHandler)
            {
                bool hasCommand = FindFirstObjectByType<FlowFieldRTSCommandHandler>() != null;
                Log($"   CommandHandler: {(hasCommand ? "✅" : "❌")}");
            }
        }

        /// <summary>
        /// Quick convert all units in scene
        /// </summary>
        [ContextMenu("Convert All Units to Flow Field")]
        public void ConvertAllUnits()
        {
            // Find or create converter
            UnitConverter converter = FindFirstObjectByType<UnitConverter>();
            if (converter == null)
            {
                GameObject converterObj = new GameObject("UnitConverter");
                converter = converterObj.AddComponent<UnitConverter>();
            }

            converter.ConvertAllUnits();
            Log("✅ Converted all units to Flow Field movement");
        }

        /// <summary>
        /// Create a test scene with flow field visualization
        /// </summary>
        [ContextMenu("Create Test Visualization Scene")]
        public void CreateTestScene()
        {
            Log("Creating test visualization scene...");

            // Setup system
            SetupFlowFieldSystem();

            // Enable all visualization
            var visualizer = FindFirstObjectByType<FlowFieldDebugVisualizer>();
            if (visualizer != null)
            {
                SetPrivateField(visualizer, "showCostField", true);
                SetPrivateField(visualizer, "showFlowField", true);
                SetPrivateField(visualizer, "showUnitVelocities", true);
                SetPrivateField(visualizer, "showGridBounds", true);
            }

            var manager = FindFirstObjectByType<FlowFieldManager>();
            if (manager != null)
            {
                SetPrivateField(manager, "showGridGizmos", true);
                SetPrivateField(manager, "showCostField", true);
                SetPrivateField(manager, "showFlowField", true);
            }

            Log("✅ Test visualization scene ready!");
            Log("💡 Hit Play and right-click terrain to see flow field");
        }

        /// <summary>
        /// Helper to set private serialized fields via reflection
        /// </summary>
        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(obj, value);
            }
            else
            {
                UnityEngine.Debug.LogWarning($"Field '{fieldName}' not found on {obj.GetType().Name}");
            }
        }

        private void Log(string message)
        {
            if (showSetupLog)
            {
                UnityEngine.Debug.Log($"[FlowField Setup] {message}");
            }
        }
    }
}
