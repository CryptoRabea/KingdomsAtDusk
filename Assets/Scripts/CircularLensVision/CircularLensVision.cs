using UnityEngine;
using System.Collections.Generic;
using KingdomsAtDusk.Core;

namespace CircularLensVision
{
    /// <summary>
    /// Main controller for circular lens vision system that allows seeing units through obstacles.
    /// Attach this to the camera or a selected unit to enable x-ray vision within a circular radius.
    /// </summary>
    public class CircularLensVision : MonoBehaviour
    {
        [Header("Game Config Integration")]
        [Tooltip("Use settings from GameConfig ScriptableObject (if null, uses local settings)")]
        [SerializeField] private GameConfigSO gameConfig;

        [Tooltip("Override game config and use local settings")]
        [SerializeField] private bool useLocalSettings = false;

        [Header("Lens Configuration")]
        [Tooltip("Radius of the circular lens vision area")]
        [SerializeField] private float lensRadius = 20f;

        [Tooltip("Center position mode")]
        [SerializeField] private LensCenterMode centerMode = LensCenterMode.Camera;

        [Tooltip("Target transform for lens center (when mode is SelectedUnit)")]
        [SerializeField] private Transform targetTransform;

        [Tooltip("Offset from center position")]
        [SerializeField] private Vector3 centerOffset = Vector3.zero;

        [Header("Detection Settings")]
        [Tooltip("Layers that count as obstacles (trees, buildings, vegetation)")]
        [SerializeField] private LayerMask obstacleLayers = ~0;

        [Tooltip("Layers that count as units to highlight")]
        [SerializeField] private LayerMask unitLayers = ~0;

        [Tooltip("Maximum number of objects to process per frame")]
        [SerializeField] private int maxObjectsPerFrame = 50;

        [Tooltip("Update interval in seconds (lower = more responsive, higher = better performance)")]
        [SerializeField] private float updateInterval = 0.1f;

        [Header("Visual Settings")]
        [Tooltip("Show debug visualization of lens radius")]
        [SerializeField] private bool showDebugVisualization = true;

        [Tooltip("Color of debug visualization")]
        [SerializeField] private Color debugColor = new Color(0.3f, 0.7f, 1f, 0.3f);

        [Header("Performance")]
        [Tooltip("Use spatial partitioning for better performance with many objects")]
        [SerializeField] private bool useSpatialPartitioning = true;

        [Tooltip("Grid cell size for spatial partitioning")]
        [SerializeField] private float gridCellSize = 10f;

        // Runtime state
        private Vector3 currentLensCenter;
        private float updateTimer;
        private HashSet<LensVisionTarget> activeTargets = new HashSet<LensVisionTarget>();
        private List<LensVisionTarget> objectsInRange = new List<LensVisionTarget>();
        private Camera mainCamera;

        // Spatial partitioning grid
        private Dictionary<Vector2Int, List<LensVisionTarget>> spatialGrid;
        private bool needsGridRebuild = true;

        public enum LensCenterMode
        {
            Camera,
            SelectedUnit,
            CustomPosition
        }

        // Public properties
        public float LensRadius => lensRadius;
        public Vector3 LensCenter => currentLensCenter;
        public bool IsActive { get; set; } = true;

        private void Awake()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }

            spatialGrid = new Dictionary<Vector2Int, List<LensVisionTarget>>();

            // Load game config if not already assigned
            if (gameConfig == null && !useLocalSettings)
            {
                gameConfig = Resources.Load<GameConfigSO>("GameConfig");
            }

            // Apply settings from game config
            ApplyGameConfigSettings();
        }

        private void OnEnable()
        {
            // Register all existing targets
            LensVisionTarget[] existingTargets = FindObjectsOfType<LensVisionTarget>();
            foreach (var target in existingTargets)
            {
                RegisterTarget(target);
            }
        }

        private void OnDisable()
        {
            // Deactivate all targets
            foreach (var target in activeTargets)
            {
                if (target != null)
                {
                    target.SetLensActive(false);
                }
            }
            activeTargets.Clear();
        }

        private void Update()
        {
            // Check if lens vision is enabled in game config
            if (!IsLensVisionEnabled() || !IsActive) return;

            // Update lens center based on mode
            UpdateLensCenter();

            // Throttle updates for performance
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                updateTimer -= updateInterval;
                UpdateLensVision();
            }
        }

        /// <summary>
        /// Check if lens vision is enabled in game config
        /// </summary>
        private bool IsLensVisionEnabled()
        {
            if (useLocalSettings || gameConfig == null)
            {
                return true; // Use local settings, always enabled
            }

            return gameConfig.enableLensVision;
        }

        /// <summary>
        /// Apply settings from game config
        /// </summary>
        private void ApplyGameConfigSettings()
        {
            if (useLocalSettings || gameConfig == null) return;

            lensRadius = gameConfig.lensVisionRadius;
            updateInterval = gameConfig.lensVisionUpdateInterval;
        }

        private void UpdateLensCenter()
        {
            switch (centerMode)
            {
                case LensCenterMode.Camera:
                    if (mainCamera != null)
                    {
                        // Project camera position to ground plane
                        Vector3 camPos = mainCamera.transform.position;
                        Ray ray = new Ray(camPos, Vector3.down);
                        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
                        {
                            currentLensCenter = hit.point + centerOffset;
                        }
                        else
                        {
                            currentLensCenter = new Vector3(camPos.x, 0, camPos.z) + centerOffset;
                        }
                    }
                    break;

                case LensCenterMode.SelectedUnit:
                    if (targetTransform != null)
                    {
                        currentLensCenter = targetTransform.position + centerOffset;
                    }
                    break;

                case LensCenterMode.CustomPosition:
                    // currentLensCenter can be set externally via SetLensCenter()
                    break;
            }
        }

        private void UpdateLensVision()
        {
            objectsInRange.Clear();

            // Find all targets within lens radius
            if (useSpatialPartitioning)
            {
                FindTargetsWithSpatialPartitioning();
            }
            else
            {
                FindTargetsWithBruteForce();
            }

            // Process objects (limit per frame for performance)
            int processedCount = 0;
            HashSet<LensVisionTarget> previouslyActive = new HashSet<LensVisionTarget>(activeTargets);

            foreach (var target in objectsInRange)
            {
                if (processedCount >= maxObjectsPerFrame) break;

                if (!activeTargets.Contains(target))
                {
                    target.SetLensActive(true);
                    activeTargets.Add(target);
                }
                previouslyActive.Remove(target);
                processedCount++;
            }

            // Deactivate targets no longer in range
            foreach (var target in previouslyActive)
            {
                if (target != null)
                {
                    target.SetLensActive(false);
                }
                activeTargets.Remove(target);
            }
        }

        private void FindTargetsWithBruteForce()
        {
            // Use OverlapSphere for simple detection
            Collider[] colliders = Physics.OverlapSphere(currentLensCenter, lensRadius);

            foreach (var collider in colliders)
            {
                LensVisionTarget target = collider.GetComponent<LensVisionTarget>();
                if (target != null && target.enabled)
                {
                    objectsInRange.Add(target);
                }
            }
        }

        private void FindTargetsWithSpatialPartitioning()
        {
            if (needsGridRebuild)
            {
                RebuildSpatialGrid();
                needsGridRebuild = false;
            }

            // Calculate grid cells that overlap with lens radius
            Vector2Int centerCell = WorldToGrid(currentLensCenter);
            int cellRadius = Mathf.CeilToInt(lensRadius / gridCellSize);

            for (int x = -cellRadius; x <= cellRadius; x++)
            {
                for (int z = -cellRadius; z <= cellRadius; z++)
                {
                    Vector2Int cell = new Vector2Int(centerCell.x + x, centerCell.y + z);

                    if (spatialGrid.TryGetValue(cell, out List<LensVisionTarget> targets))
                    {
                        foreach (var target in targets)
                        {
                            if (target == null || !target.enabled) continue;

                            float distance = Vector3.Distance(currentLensCenter, target.transform.position);
                            if (distance <= lensRadius)
                            {
                                objectsInRange.Add(target);
                            }
                        }
                    }
                }
            }
        }

        private void RebuildSpatialGrid()
        {
            spatialGrid.Clear();

            foreach (var target in activeTargets)
            {
                if (target != null)
                {
                    AddToSpatialGrid(target);
                }
            }
        }

        private void AddToSpatialGrid(LensVisionTarget target)
        {
            Vector2Int cell = WorldToGrid(target.transform.position);

            if (!spatialGrid.ContainsKey(cell))
            {
                spatialGrid[cell] = new List<LensVisionTarget>();
            }

            spatialGrid[cell].Add(target);
        }

        private Vector2Int WorldToGrid(Vector3 worldPos)
        {
            int x = Mathf.FloorToInt(worldPos.x / gridCellSize);
            int z = Mathf.FloorToInt(worldPos.z / gridCellSize);
            return new Vector2Int(x, z);
        }

        /// <summary>
        /// Register a target for lens vision tracking
        /// </summary>
        public void RegisterTarget(LensVisionTarget target)
        {
            if (target != null && !activeTargets.Contains(target))
            {
                activeTargets.Add(target);

                if (useSpatialPartitioning)
                {
                    AddToSpatialGrid(target);
                }
            }
        }

        /// <summary>
        /// Unregister a target (called when target is destroyed)
        /// </summary>
        public void UnregisterTarget(LensVisionTarget target)
        {
            if (target != null)
            {
                target.SetLensActive(false);
                activeTargets.Remove(target);
                needsGridRebuild = true;
            }
        }

        /// <summary>
        /// Set the lens center manually (when using CustomPosition mode)
        /// </summary>
        public void SetLensCenter(Vector3 position)
        {
            currentLensCenter = position + centerOffset;
        }

        /// <summary>
        /// Set the target transform for SelectedUnit mode
        /// </summary>
        public void SetTargetTransform(Transform target)
        {
            targetTransform = target;
        }

        /// <summary>
        /// Change the lens radius at runtime
        /// </summary>
        public void SetLensRadius(float radius)
        {
            lensRadius = Mathf.Max(0.1f, radius);
        }

        private void OnDrawGizmos()
        {
            if (!showDebugVisualization) return;

            // Draw lens radius
            Gizmos.color = debugColor;

            Vector3 center = Application.isPlaying ? currentLensCenter : transform.position;

            // Draw circle on ground plane
            int segments = 32;
            float angleStep = 360f / segments;
            Vector3 previousPoint = center + new Vector3(lensRadius, 0, 0);

            for (int i = 1; i <= segments; i++)
            {
                float angle = angleStep * i * Mathf.Deg2Rad;
                Vector3 nextPoint = center + new Vector3(
                    Mathf.Cos(angle) * lensRadius,
                    0,
                    Mathf.Sin(angle) * lensRadius
                );

                Gizmos.DrawLine(previousPoint, nextPoint);
                previousPoint = nextPoint;
            }

            // Draw vertical line at center
            Gizmos.DrawLine(center, center + Vector3.up * 5f);
        }

        private void OnDrawGizmosSelected()
        {
            if (!showDebugVisualization) return;

            // Draw more detailed info when selected
            Gizmos.color = Color.cyan;
            Vector3 center = Application.isPlaying ? currentLensCenter : transform.position;

            // Draw sphere representing lens volume
            Gizmos.DrawWireSphere(center, lensRadius);
        }
    }
}
