using UnityEngine;
using System.Collections.Generic;

namespace KingdomsAtDusk.FogOfWar
{
    /// <summary>
    /// Grid-based fog of war tracking system for efficient vision calculations
    /// </summary>
    public class FogOfWarGrid
    {
        private VisionState[,] grid;
        private float[,] visibilityTimer; // For fade effects
        private readonly int gridWidth;
        private readonly int gridHeight;
        private readonly float cellSize;
        private readonly Vector3 gridOrigin;
        private readonly HashSet<Vector2Int> dirtyCells;

        public int Width => gridWidth;
        public int Height => gridHeight;
        public float CellSize => cellSize;
        public Vector3 Origin => gridOrigin;

        public FogOfWarGrid(Bounds worldBounds, float cellSize)
        {
            this.cellSize = cellSize;

            // Calculate grid dimensions
            gridWidth = Mathf.CeilToInt(worldBounds.size.x / cellSize);
            gridHeight = Mathf.CeilToInt(worldBounds.size.z / cellSize);

            // Calculate grid origin (bottom-left corner)
            gridOrigin = worldBounds.min;

            // Initialize grids
            grid = new VisionState[gridWidth, gridHeight];
            visibilityTimer = new float[gridWidth, gridHeight];
            dirtyCells = new HashSet<Vector2Int>();

            // Initialize all cells as unexplored
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    grid[x, y] = VisionState.Unexplored;
                    visibilityTimer[x, y] = 0f;
                }
            }

            Debug.Log($"[FogOfWarGrid] Initialized grid: {gridWidth}x{gridHeight} cells (world bounds: {worldBounds.size})");
        }

        /// <summary>
        /// Convert world position to grid coordinates
        /// </summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            Vector3 localPos = worldPos - gridOrigin;
            int x = Mathf.FloorToInt(localPos.x / cellSize);
            int z = Mathf.FloorToInt(localPos.z / cellSize);
            return new Vector2Int(x, z);
        }

        /// <summary>
        /// Convert grid coordinates to world position (center of cell)
        /// </summary>
        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            return gridOrigin + new Vector3(
                (gridPos.x + 0.5f) * cellSize,
                0f,
                (gridPos.y + 0.5f) * cellSize
            );
        }

        /// <summary>
        /// Check if grid coordinates are valid
        /// </summary>
        public bool IsValidCell(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < gridWidth &&
                   cell.y >= 0 && cell.y < gridHeight;
        }

        /// <summary>
        /// Get vision state at grid coordinates
        /// </summary>
        public VisionState GetState(Vector2Int cell)
        {
            if (!IsValidCell(cell)) return VisionState.Unexplored;
            return grid[cell.x, cell.y];
        }

        /// <summary>
        /// Get vision state at world position
        /// </summary>
        public VisionState GetState(Vector3 worldPos)
        {
            return GetState(WorldToGrid(worldPos));
        }

        /// <summary>
        /// Set vision state at grid coordinates
        /// </summary>
        public void SetState(Vector2Int cell, VisionState state)
        {
            if (!IsValidCell(cell)) return;

            if (grid[cell.x, cell.y] != state)
            {
                grid[cell.x, cell.y] = state;
                dirtyCells.Add(cell);

                // Reset timer when state changes
                if (state == VisionState.Visible)
                {
                    visibilityTimer[cell.x, cell.y] = 1f;
                }
            }
        }

        /// <summary>
        /// Update visibility timer for fade effects
        /// </summary>
        public void UpdateVisibilityTimer(Vector2Int cell, float deltaTime, float fadeSpeed)
        {
            if (!IsValidCell(cell)) return;

            VisionState state = grid[cell.x, cell.y];

            if (state == VisionState.Visible)
            {
                visibilityTimer[cell.x, cell.y] = Mathf.Min(1f, visibilityTimer[cell.x, cell.y] + deltaTime * fadeSpeed);
            }
            else if (state == VisionState.Explored)
            {
                visibilityTimer[cell.x, cell.y] = Mathf.Max(0f, visibilityTimer[cell.x, cell.y] - deltaTime * fadeSpeed);
            }
        }

        /// <summary>
        /// Get visibility alpha for smooth fading
        /// </summary>
        public float GetVisibilityAlpha(Vector2Int cell)
        {
            if (!IsValidCell(cell)) return 1f;
            return visibilityTimer[cell.x, cell.y];
        }

        /// <summary>
        /// Reveal cells in a circular radius
        /// </summary>
        public void RevealCircle(Vector3 worldCenter, float radius)
        {
            Vector2Int centerCell = WorldToGrid(worldCenter);
            int cellRadius = Mathf.CeilToInt(radius / cellSize);
            float radiusSq = radius * radius;

            for (int x = centerCell.x - cellRadius; x <= centerCell.x + cellRadius; x++)
            {
                for (int y = centerCell.y - cellRadius; y <= centerCell.y + cellRadius; y++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (!IsValidCell(cell)) continue;

                    // Check if cell is within circular radius
                    Vector3 cellWorldPos = GridToWorld(cell);
                    float distSq = (cellWorldPos - worldCenter).sqrMagnitude;

                    if (distSq <= radiusSq)
                    {
                        SetState(cell, VisionState.Visible);
                    }
                }
            }
        }

        /// <summary>
        /// Clear all visible cells (called at start of vision update)
        /// </summary>
        public void ClearVisibleCells()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (grid[x, y] == VisionState.Visible)
                    {
                        grid[x, y] = VisionState.Explored;
                        dirtyCells.Add(new Vector2Int(x, y));
                    }
                }
            }
        }

        /// <summary>
        /// Get all dirty cells and clear the dirty flag
        /// </summary>
        public HashSet<Vector2Int> GetAndClearDirtyCells()
        {
            var cells = new HashSet<Vector2Int>(dirtyCells);
            dirtyCells.Clear();
            return cells;
        }

        /// <summary>
        /// Get all cells in a specific state
        /// </summary>
        public List<Vector2Int> GetCellsInState(VisionState state)
        {
            var cells = new List<Vector2Int>();

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (grid[x, y] == state)
                    {
                        cells.Add(new Vector2Int(x, y));
                    }
                }
            }

            return cells;
        }

        /// <summary>
        /// Debug: Draw grid in scene view
        /// </summary>
        public void DrawDebugGrid()
        {
            for (int x = 0; x < gridWidth; x += 10)
            {
                for (int y = 0; y < gridHeight; y += 10)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    Vector3 worldPos = GridToWorld(cell);

                    Color color = GetState(cell) switch
                    {
                        VisionState.Unexplored => Color.black,
                        VisionState.Explored => Color.gray,
                        VisionState.Visible => Color.green,
                        _ => Color.white
                    };

                    Debug.DrawLine(worldPos, worldPos + Vector3.up * 5f, color);
                }
            }
        }
    }
}
