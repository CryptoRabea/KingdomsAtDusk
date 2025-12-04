using UnityEngine;
using UnityEngine.AI;

namespace FlowField.Core
{
    /// <summary>
    /// Core grid structure for flow field pathfinding
    /// Manages cost field, integration field, and flow field
    /// </summary>
    public class FlowFieldGrid
    {
        public readonly int width;
        public readonly int height;
        public readonly float cellSize;
        public readonly Vector3 worldOrigin;

        private readonly GridCell[] cells;
        private readonly Vector3 worldBounds;

        // Neighbor offsets for 8-directional movement
        private static readonly GridPosition[] NeighborOffsets = new GridPosition[]
        {
            new GridPosition(0, 1),   // North
            new GridPosition(1, 1),   // NE
            new GridPosition(1, 0),   // East
            new GridPosition(1, -1),  // SE
            new GridPosition(0, -1),  // South
            new GridPosition(-1, -1), // SW
            new GridPosition(-1, 0),  // West
            new GridPosition(-1, 1)   // NW
        };

        private static readonly float DiagonalCost = 1.414f; // sqrt(2)

        public FlowFieldGrid(Vector3 worldOrigin, float width, float height, float cellSize)
        {
            this.worldOrigin = worldOrigin;
            this.cellSize = cellSize;
            this.width = Mathf.CeilToInt(width / cellSize);
            this.height = Mathf.CeilToInt(height / cellSize);
            this.worldBounds = new Vector3(width, 0, height);

            cells = new GridCell[this.width * this.height];

            InitializeCostField();
        }

        public GridCell GetCell(int x, int z)
        {
            if (!IsValidGridPosition(x, z))
                return default;

            return cells[GetIndex(x, z)];
        }

        public GridCell GetCell(GridPosition pos)
        {
            return GetCell(pos.x, pos.z);
        }

        public void SetCell(int x, int z, GridCell cell)
        {
            if (!IsValidGridPosition(x, z))
                return;

            cells[GetIndex(x, z)] = cell;
        }

        public void SetCell(GridPosition pos, GridCell cell)
        {
            SetCell(pos.x, pos.z, cell);
        }

        public bool IsValidGridPosition(int x, int z)
        {
            return x >= 0 && x < width && z >= 0 && z < height;
        }

        public bool IsValidGridPosition(GridPosition pos)
        {
            return IsValidGridPosition(pos.x, pos.z);
        }

        public GridPosition WorldToGrid(Vector3 worldPosition)
        {
            Vector3 localPos = worldPosition - worldOrigin;
            return new GridPosition(
                Mathf.FloorToInt(localPos.x / cellSize),
                Mathf.FloorToInt(localPos.z / cellSize)
            );
        }

        public Vector3 GridToWorld(GridPosition gridPosition)
        {
            return GridToWorld(gridPosition.x, gridPosition.z);
        }

        public Vector3 GridToWorld(int x, int z)
        {
            return worldOrigin + new Vector3(
                (x + 0.5f) * cellSize,
                0,
                (z + 0.5f) * cellSize
            );
        }

        public int GetIndex(int x, int z)
        {
            return z * width + x;
        }

        public int GetIndex(GridPosition pos)
        {
            return GetIndex(pos.x, pos.z);
        }

        public void GetGridPositionFromIndex(int index, out int x, out int z)
        {
            z = index / width;
            x = index - z * width;
        }

        /// <summary>
        /// Initialize cost field based on NavMesh walkability
        /// </summary>
        private void InitializeCostField()
        {
            NavMeshHit hit;
            float sampleDistance = cellSize * 0.5f;

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector3 worldPos = GridToWorld(x, z);
                    GridCell cell = cells[GetIndex(x, z)];

                    // Check if position is on NavMesh
                    bool isWalkable = NavMesh.SamplePosition(worldPos, out hit, sampleDistance, NavMesh.AllAreas);

                    if (isWalkable)
                    {
                        // Default walkable cost
                        cell.cost = GridCell.DEFAULT_COST;

                        // Optional: Adjust cost based on NavMesh area cost
                        // Higher area cost = harder to traverse
                        int areaMask = 1 << hit.position.GetHashCode(); // Simplified
                        // You can enhance this with actual NavMesh area costs
                    }
                    else
                    {
                        cell.cost = GridCell.UNWALKABLE_COST;
                    }

                    cell.bestCost = GridCell.MAX_INTEGRATION_COST;
                    cell.bestDirection = Vector2.zero;

                    cells[GetIndex(x, z)] = cell;
                }
            }
        }

        /// <summary>
        /// Update cost field for dynamic obstacles
        /// Call this when buildings are placed/destroyed
        /// </summary>
        public void UpdateCostFieldRegion(Bounds bounds)
        {
            GridPosition min = WorldToGrid(bounds.min);
            GridPosition max = WorldToGrid(bounds.max);

            // Clamp to grid bounds
            min.x = Mathf.Max(0, min.x);
            min.z = Mathf.Max(0, min.z);
            max.x = Mathf.Min(width - 1, max.x);
            max.z = Mathf.Min(height - 1, max.z);

            NavMeshHit hit;
            float sampleDistance = cellSize * 0.5f;

            for (int z = min.z; z <= max.z; z++)
            {
                for (int x = min.x; x <= max.x; x++)
                {
                    Vector3 worldPos = GridToWorld(x, z);
                    GridCell cell = cells[GetIndex(x, z)];

                    bool isWalkable = NavMesh.SamplePosition(worldPos, out hit, sampleDistance, NavMesh.AllAreas);
                    cell.cost = isWalkable ? GridCell.DEFAULT_COST : GridCell.UNWALKABLE_COST;

                    cells[GetIndex(x, z)] = cell;
                }
            }
        }

        /// <summary>
        /// Get all valid neighbors for a grid position (8-directional)
        /// </summary>
        public void GetNeighbors(GridPosition pos, out GridPosition[] neighbors, out float[] costs)
        {
            neighbors = new GridPosition[8];
            costs = new float[8];
            int count = 0;

            for (int i = 0; i < NeighborOffsets.Length; i++)
            {
                GridPosition neighbor = pos + NeighborOffsets[i];

                if (IsValidGridPosition(neighbor))
                {
                    neighbors[count] = neighbor;

                    // Diagonal movement costs more
                    bool isDiagonal = (i % 2) == 1;
                    costs[count] = isDiagonal ? DiagonalCost : 1f;

                    count++;
                }
            }

            // Resize if needed
            if (count < 8)
            {
                System.Array.Resize(ref neighbors, count);
                System.Array.Resize(ref costs, count);
            }
        }

        /// <summary>
        /// Sample flow direction at world position using bilinear interpolation
        /// This creates smooth movement instead of grid-snapped movement
        /// </summary>
        public Vector2 SampleFlowDirection(Vector3 worldPosition)
        {
            Vector3 localPos = worldPosition - worldOrigin;
            float gridX = localPos.x / cellSize;
            float gridZ = localPos.z / cellSize;

            // Get 4 surrounding cells
            int x0 = Mathf.FloorToInt(gridX);
            int z0 = Mathf.FloorToInt(gridZ);
            int x1 = x0 + 1;
            int z1 = z0 + 1;

            // Clamp to grid bounds
            x0 = Mathf.Clamp(x0, 0, width - 1);
            z0 = Mathf.Clamp(z0, 0, height - 1);
            x1 = Mathf.Clamp(x1, 0, width - 1);
            z1 = Mathf.Clamp(z1, 0, height - 1);

            // Interpolation weights
            float tx = gridX - x0;
            float tz = gridZ - z0;

            // Bilinear interpolation
            Vector2 dir00 = GetCell(x0, z0).bestDirection;
            Vector2 dir10 = GetCell(x1, z0).bestDirection;
            Vector2 dir01 = GetCell(x0, z1).bestDirection;
            Vector2 dir11 = GetCell(x1, z1).bestDirection;

            Vector2 dir0 = Vector2.Lerp(dir00, dir10, tx);
            Vector2 dir1 = Vector2.Lerp(dir01, dir11, tx);
            Vector2 finalDir = Vector2.Lerp(dir0, dir1, tz);

            return finalDir.normalized;
        }

        /// <summary>
        /// Check if a world position is in a walkable cell
        /// </summary>
        public bool IsWalkable(Vector3 worldPosition)
        {
            GridPosition gridPos = WorldToGrid(worldPosition);
            if (!IsValidGridPosition(gridPos))
                return false;

            return GetCell(gridPos).IsWalkable;
        }

        /// <summary>
        /// Get cost at world position
        /// </summary>
        public byte GetCostAtPosition(Vector3 worldPosition)
        {
            GridPosition gridPos = WorldToGrid(worldPosition);
            if (!IsValidGridPosition(gridPos))
                return GridCell.UNWALKABLE_COST;

            return GetCell(gridPos).cost;
        }

        public Bounds GetWorldBounds()
        {
            return new Bounds(
                worldOrigin + worldBounds * 0.5f,
                worldBounds
            );
        }

        /// <summary>
        /// Debug visualization
        /// </summary>
        public void DrawGizmos(bool showCost = true, bool showFlow = true)
        {
            if (showCost)
            {
                for (int z = 0; z < height; z++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        GridCell cell = GetCell(x, z);
                        Vector3 worldPos = GridToWorld(x, z);

                        // Color based on cost
                        if (cell.cost == GridCell.UNWALKABLE_COST)
                        {
                            Gizmos.color = new Color(1, 0, 0, 0.5f); // Red = unwalkable
                        }
                        else
                        {
                            float costNormalized = cell.cost / 255f;
                            Gizmos.color = new Color(costNormalized, 1f - costNormalized, 0, 0.3f);
                        }

                        Gizmos.DrawCube(worldPos, Vector3.one * cellSize * 0.8f);
                    }
                }
            }

            if (showFlow)
            {
                Gizmos.color = Color.cyan;
                for (int z = 0; z < height; z++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        GridCell cell = GetCell(x, z);
                        if (cell.bestDirection != Vector2.zero)
                        {
                            Vector3 worldPos = GridToWorld(x, z);
                            Vector3 dir = new Vector3(cell.bestDirection.x, 0, cell.bestDirection.y);
                            Gizmos.DrawRay(worldPos, dir * cellSize * 0.4f);
                        }
                    }
                }
            }
        }
    }
}
