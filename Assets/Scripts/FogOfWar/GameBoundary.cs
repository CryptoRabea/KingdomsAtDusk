using UnityEngine;

namespace KingdomsAtDusk.FogOfWar
{
    /// <summary>
    /// Single source of truth for game world boundaries.
    /// Used by fog of war, minimap, and other systems that need world bounds.
    /// </summary>
    [System.Serializable]
    public class GameBoundary
    {
        [Header("Boundary Configuration")]
        [Tooltip("Center point of the game world")]
        [SerializeField] private Vector3 center = Vector3.zero;

        [Tooltip("Size of the game world (width, height, depth)")]
        [SerializeField] private Vector3 size = new Vector3(1000f, 100f, 1000f);

        [Header("Grid Configuration")]
        [Tooltip("Size of each grid cell in world units (used for fog of war grid)")]
        [SerializeField] private float cellSize = 2f;

        // Cached bounds
        private Bounds cachedBounds;
        private bool isDirty = true;

        /// <summary>
        /// Get the Unity Bounds object representing the game world
        /// </summary>
        public Bounds Bounds
        {
            get
            {
                if (isDirty)
                {
                    cachedBounds = new Bounds(center, size);
                    isDirty = false;
                }
                return cachedBounds;
            }
        }

        /// <summary>
        /// Center of the game world
        /// </summary>
        public Vector3 Center
        {
            get => center;
            set
            {
                center = value;
                isDirty = true;
            }
        }

        /// <summary>
        /// Size of the game world
        /// </summary>
        public Vector3 Size
        {
            get => size;
            set
            {
                size = value;
                isDirty = true;
            }
        }

        /// <summary>
        /// Grid cell size for fog of war
        /// </summary>
        public float CellSize
        {
            get => cellSize;
            set => cellSize = Mathf.Max(0.1f, value);
        }

        /// <summary>
        /// Minimum point of the world bounds
        /// </summary>
        public Vector3 Min => Bounds.min;

        /// <summary>
        /// Maximum point of the world bounds
        /// </summary>
        public Vector3 Max => Bounds.max;

        /// <summary>
        /// Width of the world (X axis)
        /// </summary>
        public float Width => size.x;

        /// <summary>
        /// Height of the world (Y axis)
        /// </summary>
        public float Height => size.y;

        /// <summary>
        /// Depth of the world (Z axis)
        /// </summary>
        public float Depth => size.z;

        /// <summary>
        /// Grid dimensions based on cell size
        /// </summary>
        public Vector2Int GridDimensions => new Vector2Int(
            Mathf.CeilToInt(Width / cellSize),
            Mathf.CeilToInt(Depth / cellSize)
        );

        // Constructors
        public GameBoundary()
        {
            center = Vector3.zero;
            size = new Vector3(1000f, 100f, 1000f);
            cellSize = 2f;
            isDirty = true;
        }

        public GameBoundary(Vector3 center, Vector3 size, float cellSize = 2f)
        {
            this.center = center;
            this.size = size;
            this.cellSize = cellSize;
            isDirty = true;
        }

        public GameBoundary(Bounds bounds, float cellSize = 2f)
        {
            this.center = bounds.center;
            this.size = bounds.size;
            this.cellSize = cellSize;
            isDirty = true;
        }

        /// <summary>
        /// Check if a world position is within bounds
        /// </summary>
        public bool Contains(Vector3 worldPosition)
        {
            return Bounds.Contains(worldPosition);
        }

        /// <summary>
        /// Clamp a world position to stay within bounds
        /// </summary>
        public Vector3 ClampToBounds(Vector3 worldPosition)
        {
            return new Vector3(
                Mathf.Clamp(worldPosition.x, Min.x, Max.x),
                Mathf.Clamp(worldPosition.y, Min.y, Max.y),
                Mathf.Clamp(worldPosition.z, Min.z, Max.z)
            );
        }

        /// <summary>
        /// Get normalized position within bounds (0-1 range)
        /// </summary>
        public Vector2 GetNormalizedPosition(Vector3 worldPosition)
        {
            float normalizedX = Mathf.InverseLerp(Min.x, Max.x, worldPosition.x);
            float normalizedZ = Mathf.InverseLerp(Min.z, Max.z, worldPosition.z);
            return new Vector2(normalizedX, normalizedZ);
        }

        /// <summary>
        /// Get world position from normalized coordinates (0-1 range)
        /// </summary>
        public Vector3 GetWorldPosition(Vector2 normalizedPosition)
        {
            float worldX = Mathf.Lerp(Min.x, Max.x, normalizedPosition.x);
            float worldZ = Mathf.Lerp(Min.z, Max.z, normalizedPosition.y);
            return new Vector3(worldX, center.y, worldZ);
        }

        /// <summary>
        /// Draw debug gizmos for the boundary
        /// </summary>
        public void DrawGizmos(Color color)
        {
            Gizmos.color = color;
            Gizmos.DrawWireCube(center, size);
        }

        public override string ToString()
        {
            return $"GameBoundary(Center: {center}, Size: {size}, CellSize: {cellSize}, GridDims: {GridDimensions})";
        }
    }
}
