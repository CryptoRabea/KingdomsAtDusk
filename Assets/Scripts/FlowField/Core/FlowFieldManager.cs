using UnityEngine;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;

namespace FlowField.Core
{
    /// <summary>
    /// Central manager for flow field pathfinding
    /// Handles grid creation, caching, and flow field generation
    /// Singleton pattern for global access
    /// </summary>
    public class FlowFieldManager : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector3 gridOrigin = Vector3.zero;
        [SerializeField] private float gridWidth = 100f;
        [SerializeField] private float gridHeight = 100f;
        [SerializeField] private bool autoDetectGridBounds = true;

        [Header("Performance")]
        [SerializeField] private int maxCachedFlowFields = 10;
        [SerializeField] private bool enableFlowFieldCaching = true;

        [Header("Debug")]
        [SerializeField] private bool showGridGizmos = false;
        [SerializeField] private bool showCostField = true;
        [SerializeField] private bool showFlowField = true;

        private FlowFieldGrid grid;
        private FlowFieldGenerator generator;
        private Dictionary<Vector3, CachedFlowField> flowFieldCache;

        public static FlowFieldManager Instance { get; private set; }

        public FlowFieldGrid Grid => grid;
        public FlowFieldGenerator Generator => generator;

        private struct CachedFlowField
        {
            public Vector3 destination;
            public float timestamp;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            InitializeGrid();
        }

        private void InitializeGrid()
        {
            if (autoDetectGridBounds)
            {
                DetectGridBounds();
            }

            grid = new FlowFieldGrid(gridOrigin, gridWidth, gridHeight, cellSize);
            generator = new FlowFieldGenerator(grid);
            flowFieldCache = new Dictionary<Vector3, CachedFlowField>();

            UnityEngine.Debug.Log($"Flow Field Grid initialized: {grid.width}x{grid.height} cells " +
                      $"({grid.width * grid.height} total), Cell Size: {cellSize}");
        }

        /// <summary>
        /// Auto-detect grid bounds from NavMesh
        /// </summary>
        private void DetectGridBounds()
        {
            // Find all NavMesh surfaces
            var triangulation = UnityEngine.AI.NavMesh.CalculateTriangulation();

            if (triangulation.vertices.Length == 0)
            {
                UnityEngine.Debug.LogWarning("No NavMesh found. Using default grid bounds.");
                return;
            }

            // Calculate bounds from NavMesh vertices
            Bounds bounds = new Bounds(triangulation.vertices[0], Vector3.zero);
            foreach (var vertex in triangulation.vertices)
            {
                bounds.Encapsulate(vertex);
            }

            // Add padding
            bounds.Expand(cellSize * 2f);

            gridOrigin = bounds.min;
            gridWidth = bounds.size.x;
            gridHeight = bounds.size.z;

            UnityEngine.Debug.Log($"Auto-detected grid bounds: Origin={gridOrigin}, Size={bounds.size}");
        }

        /// <summary>
        /// Generate flow field to a destination
        /// Uses caching to avoid redundant calculations
        /// </summary>
        public void GenerateFlowField(Vector3 destination)
        {
            // Round destination to grid cell to improve cache hits
            Vector3 roundedDest = RoundToGridCell(destination);

            // Check cache
            if (enableFlowFieldCaching && flowFieldCache.ContainsKey(roundedDest))
            {
                // Cache hit - flow field already exists
                return;
            }

            // Generate new flow field
            generator.GenerateFlowField(destination);

            // Cache it
            if (enableFlowFieldCaching)
            {
                CacheFlowField(roundedDest);
            }
        }

        /// <summary>
        /// Generate flow field for multiple destinations (formation positions)
        /// </summary>
        public void GenerateFlowField(List<Vector3> destinations)
        {
            if (destinations == null || destinations.Count == 0)
                return;

            if (destinations.Count == 1)
            {
                GenerateFlowField(destinations[0]);
                return;
            }

            // Multi-goal pathfinding doesn't cache well, so always regenerate
            generator.GenerateFlowField(destinations);
        }

        /// <summary>
        /// Sample flow direction at a world position
        /// This is what units call to get their movement direction
        /// </summary>
        public Vector2 SampleFlowDirection(Vector3 worldPosition)
        {
            return grid.SampleFlowDirection(worldPosition);
        }

        /// <summary>
        /// Check if position is walkable
        /// </summary>
        public bool IsWalkable(Vector3 worldPosition)
        {
            return grid.IsWalkable(worldPosition);
        }

        /// <summary>
        /// Update cost field when obstacles change (buildings placed/destroyed)
        /// </summary>
        public void UpdateCostField(Bounds affectedRegion)
        {
            grid.UpdateCostFieldRegion(affectedRegion);

            // Invalidate cached flow fields that intersect this region
            if (enableFlowFieldCaching)
            {
                InvalidateCacheInRegion(affectedRegion);
            }
        }

        /// <summary>
        /// Round world position to nearest grid cell center
        /// Improves cache hit rate
        /// </summary>
        private Vector3 RoundToGridCell(Vector3 worldPosition)
        {
            GridPosition gridPos = grid.WorldToGrid(worldPosition);
            return grid.GridToWorld(gridPos);
        }

        /// <summary>
        /// Cache a flow field
        /// </summary>
        private void CacheFlowField(Vector3 destination)
        {
            // Check cache size limit
            if (flowFieldCache.Count >= maxCachedFlowFields)
            {
                // Remove oldest entry
                Vector3 oldestKey = Vector3.zero;
                float oldestTime = float.MaxValue;

                foreach (var kvp in flowFieldCache)
                {
                    if (kvp.Value.timestamp < oldestTime)
                    {
                        oldestTime = kvp.Value.timestamp;
                        oldestKey = kvp.Key;
                    }
                }

                flowFieldCache.Remove(oldestKey);
            }

            // Add to cache
            flowFieldCache[destination] = new CachedFlowField
            {
                destination = destination,
                timestamp = Time.time
            };
        }

        /// <summary>
        /// Invalidate cached flow fields in a region
        /// Call this when obstacles are added/removed
        /// </summary>
        private void InvalidateCacheInRegion(Bounds region)
        {
            List<Vector3> toRemove = new List<Vector3>();

            foreach (var kvp in flowFieldCache)
            {
                if (region.Contains(kvp.Key))
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var key in toRemove)
            {
                flowFieldCache.Remove(key);
            }

            if (toRemove.Count > 0)
            {
                UnityEngine.Debug.Log($"Invalidated {toRemove.Count} cached flow fields due to obstacle change");
            }
        }

        /// <summary>
        /// Clear all cached flow fields
        /// </summary>
        public void ClearCache()
        {
            flowFieldCache.Clear();
        }

        /// <summary>
        /// Get path cost from position to current goal
        /// </summary>
        public float GetPathCost(Vector3 worldPosition)
        {
            return generator.GetPathCost(worldPosition);
        }

        private void OnDrawGizmos()
        {
            if (!showGridGizmos || grid == null)
                return;

            grid.DrawGizmos(showCostField, showFlowField);
        }

        /// <summary>
        /// Public API for getting grid info
        /// </summary>
        public Vector3 GetGridOrigin() => gridOrigin;
        public float GetCellSize() => cellSize;
        public Vector2Int GetGridSize() => new Vector2Int(grid.width, grid.height);
    }
}
