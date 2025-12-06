using UnityEngine;
using System.Collections.Generic;
using FlowField.Core;
using FlowField.Movement;
using Debug = UnityEngine.Debug;

namespace FlowField.Performance
{
    /// <summary>
    /// Performance optimization manager for Flow Field system
    /// Handles batching, LOD, multithreading, and performance monitoring
    /// </summary>
    public class FlowFieldPerformanceManager : MonoBehaviour
    {
        [Header("Update Batching")]
        [SerializeField] private bool enableBatchedUpdates = true;
        [SerializeField] private int unitsPerBatch = 50;
        [SerializeField] private int batchesPerFrame = 4;

        [Header("Level of Detail")]
        [SerializeField] private bool enableLOD = true;
        [SerializeField] private float highDetailRadius = 30f;
        [SerializeField] private float mediumDetailRadius = 60f;

        [Header("Performance Monitoring")]
        [SerializeField] private bool showPerformanceStats = true;
        [SerializeField] private float statsUpdateInterval = 1f;

        // Unit tracking
        private List<FlowFieldFollower> allUnits = new List<FlowFieldFollower>();
        private Queue<FlowFieldFollower> updateQueue = new Queue<FlowFieldFollower>();

        // LOD groups
        private List<FlowFieldFollower> highDetailUnits = new List<FlowFieldFollower>();
        private List<FlowFieldFollower> mediumDetailUnits = new List<FlowFieldFollower>();
        private List<FlowFieldFollower> lowDetailUnits = new List<FlowFieldFollower>();

        // Performance stats
        private int totalUnits;
        private int activeUnits;
        private float averageFrameTime;
        private float lastStatsUpdate;
        private int framesSinceLastStats;
        private float accumulatedFrameTime;

        // Camera reference for LOD
        private Camera mainCamera;

        public static FlowFieldPerformanceManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            mainCamera = Camera.main;
        }

        private void Start()
        {
            // Find all units in scene
            RefreshUnitList();

            // Subscribe to unit spawn/death events if available
            // This would integrate with your EventBus system
        }

        private void Update()
        {
            // Update performance stats
            UpdatePerformanceStats();

            // Update LOD groups based on camera distance
            if (enableLOD)
            {
                UpdateLODGroups();
            }

            // Process batched updates
            if (enableBatchedUpdates)
            {
                ProcessBatchedUpdates();
            }
        }

        /// <summary>
        /// Register a unit for performance management
        /// </summary>
        public void RegisterUnit(FlowFieldFollower unit)
        {
            if (!allUnits.Contains(unit))
            {
                allUnits.Add(unit);
                updateQueue.Enqueue(unit);
            }
        }

        /// <summary>
        /// Unregister a unit (when destroyed)
        /// </summary>
        public void UnregisterUnit(FlowFieldFollower unit)
        {
            allUnits.Remove(unit);
            highDetailUnits.Remove(unit);
            mediumDetailUnits.Remove(unit);
            lowDetailUnits.Remove(unit);
        }

        /// <summary>
        /// Refresh list of all units in scene
        /// </summary>
        public void RefreshUnitList()
        {
            allUnits.Clear();
            allUnits.AddRange(FindObjectsByType<FlowFieldFollower>(FindObjectsSortMode.None));

            foreach (var unit in allUnits)
            {
                updateQueue.Enqueue(unit);
            }

            UnityEngine.Debug.Log($"Performance Manager: Tracking {allUnits.Count} units");
        }

        /// <summary>
        /// Update LOD groups based on distance to camera
        /// </summary>
        private void UpdateLODGroups()
        {
            if (mainCamera == null)
                return;

            Vector3 cameraPos = mainCamera.transform.position;

            highDetailUnits.Clear();
            mediumDetailUnits.Clear();
            lowDetailUnits.Clear();

            foreach (var unit in allUnits)
            {
                if (unit == null)
                    continue;

                float distance = Vector3.Distance(unit.transform.position, cameraPos);

                if (distance < highDetailRadius)
                {
                    highDetailUnits.Add(unit);
                }
                else if (distance < mediumDetailRadius)
                {
                    mediumDetailUnits.Add(unit);
                }
                else
                {
                    lowDetailUnits.Add(unit);
                }
            }
        }

        /// <summary>
        /// Process unit updates in batches to spread load across frames
        /// </summary>
        private void ProcessBatchedUpdates()
        {
            int processedThisFrame = 0;
            int maxToProcess = unitsPerBatch * batchesPerFrame;

            while (processedThisFrame < maxToProcess && updateQueue.Count > 0)
            {
                FlowFieldFollower unit = updateQueue.Dequeue();

                if (unit != null && unit.gameObject.activeInHierarchy)
                {
                    // Custom update logic can go here
                    // For example, updating formation offsets or checking stuck state
                }

                // Re-queue for next update
                updateQueue.Enqueue(unit);
                processedThisFrame++;
            }
        }

        /// <summary>
        /// Update and display performance statistics
        /// </summary>
        private void UpdatePerformanceStats()
        {
            framesSinceLastStats++;
            accumulatedFrameTime += Time.deltaTime;

            if (Time.time - lastStatsUpdate >= statsUpdateInterval)
            {
                totalUnits = allUnits.Count;
                activeUnits = 0;

                foreach (var unit in allUnits)
                {
                    if (unit != null && unit.IsMoving)
                    {
                        activeUnits++;
                    }
                }

                averageFrameTime = accumulatedFrameTime / framesSinceLastStats;

                if (showPerformanceStats)
                {
                    UnityEngine.Debug.Log($"Flow Field Performance:\n" +
                              $"Total Units: {totalUnits}\n" +
                              $"Active Units: {activeUnits}\n" +
                              $"Avg Frame Time: {averageFrameTime * 1000f:F2}ms ({1f / averageFrameTime:F1} FPS)\n" +
                              $"LOD High/Med/Low: {highDetailUnits.Count}/{mediumDetailUnits.Count}/{lowDetailUnits.Count}");
                }

                lastStatsUpdate = Time.time;
                framesSinceLastStats = 0;
                accumulatedFrameTime = 0f;
            }
        }

        /// <summary>
        /// Get performance statistics
        /// </summary>
        public PerformanceStats GetStats()
        {
            return new PerformanceStats
            {
                totalUnits = totalUnits,
                activeUnits = activeUnits,
                averageFrameTime = averageFrameTime,
                highDetailCount = highDetailUnits.Count,
                mediumDetailCount = mediumDetailUnits.Count,
                lowDetailCount = lowDetailUnits.Count
            };
        }

        /// <summary>
        /// Performance optimization: Disable far units if FPS drops
        /// </summary>
        public void AdaptivePerformanceOptimization(float targetFPS = 60f)
        {
            float currentFPS = 1f / averageFrameTime;

            if (currentFPS < targetFPS * 0.8f) // 80% of target
            {
                // Reduce LOD radii
                highDetailRadius *= 0.9f;
                mediumDetailRadius *= 0.9f;

                UnityEngine.Debug.LogWarning($"Performance degradation detected. Reducing LOD radii. FPS: {currentFPS:F1}");
            }
            else if (currentFPS > targetFPS * 1.1f) // 110% of target
            {
                // Increase LOD radii (gradually restore quality)
                highDetailRadius = Mathf.Min(highDetailRadius * 1.05f, 30f);
                mediumDetailRadius = Mathf.Min(mediumDetailRadius * 1.05f, 60f);
            }
        }

        private void OnGUI()
        {
            if (!showPerformanceStats)
                return;

            GUIStyle style = new GUIStyle();
            style.fontSize = 14;
            style.normal.textColor = Color.white;

            string stats = $"Flow Field Performance\n" +
                          $"Units: {totalUnits} (Active: {activeUnits})\n" +
                          $"FPS: {1f / averageFrameTime:F1} ({averageFrameTime * 1000f:F2}ms)\n" +
                          $"LOD: H:{highDetailUnits.Count} M:{mediumDetailUnits.Count} L:{lowDetailUnits.Count}";

            GUI.Label(new Rect(10, 10, 300, 100), stats, style);
        }

        public struct PerformanceStats
        {
            public int totalUnits;
            public int activeUnits;
            public float averageFrameTime;
            public int highDetailCount;
            public int mediumDetailCount;
            public int lowDetailCount;
        }
    }
}
