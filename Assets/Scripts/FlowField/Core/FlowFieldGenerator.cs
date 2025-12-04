using UnityEngine;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;

namespace FlowField.Core
{
    /// <summary>
    /// Generates flow fields using Dijkstra's algorithm + gradient descent
    /// This is the core pathfinding engine for large-scale unit movement
    /// </summary>
    public class FlowFieldGenerator
    {
        private readonly FlowFieldGrid grid;
        private readonly Queue<GridPosition> openSet;
        private readonly HashSet<GridPosition> closedSet;

        // Reusable arrays to avoid allocations
        private GridPosition[] neighborBuffer;
        private float[] costBuffer;

        public FlowFieldGenerator(FlowFieldGrid grid)
        {
            this.grid = grid;
            this.openSet = new Queue<GridPosition>(grid.width * grid.height / 4);
            this.closedSet = new HashSet<GridPosition>();
            this.neighborBuffer = new GridPosition[8];
            this.costBuffer = new float[8];
        }

        /// <summary>
        /// Generate a complete flow field from a destination point
        /// This uses Dijkstra's algorithm to create an integration field,
        /// then calculates flow directions using gradient descent
        /// </summary>
        public void GenerateFlowField(Vector3 destinationWorldPos)
        {
            GridPosition destinationCell = grid.WorldToGrid(destinationWorldPos);

            if (!grid.IsValidGridPosition(destinationCell))
            {
                UnityEngine.Debug.LogWarning($"Destination {destinationWorldPos} is outside grid bounds");
                return;
            }

            // Step 1: Reset integration field
            ResetIntegrationField();

            // Step 2: Calculate integration field (Dijkstra)
            CalculateIntegrationField(destinationCell);

            // Step 3: Calculate flow field (Gradient descent)
            CalculateFlowField();
        }

        /// <summary>
        /// Generate flow field for multiple destinations (multi-goal pathfinding)
        /// Useful for units converging on multiple rally points or formation positions
        /// </summary>
        public void GenerateFlowField(List<Vector3> destinationWorldPositions)
        {
            if (destinationWorldPositions == null || destinationWorldPositions.Count == 0)
                return;

            List<GridPosition> destinationCells = new List<GridPosition>(destinationWorldPositions.Count);

            foreach (var worldPos in destinationWorldPositions)
            {
                GridPosition gridPos = grid.WorldToGrid(worldPos);
                if (grid.IsValidGridPosition(gridPos) && grid.GetCell(gridPos).IsWalkable)
                {
                    destinationCells.Add(gridPos);
                }
            }

            if (destinationCells.Count == 0)
            {
                UnityEngine.Debug.LogWarning("No valid destinations for flow field");
                return;
            }

            // Step 1: Reset integration field
            ResetIntegrationField();

            // Step 2: Calculate integration field from multiple goals
            CalculateIntegrationField(destinationCells);

            // Step 3: Calculate flow field
            CalculateFlowField();
        }

        /// <summary>
        /// Reset all integration costs to maximum
        /// </summary>
        private void ResetIntegrationField()
        {
            for (int z = 0; z < grid.height; z++)
            {
                for (int x = 0; x < grid.width; x++)
                {
                    GridCell cell = grid.GetCell(x, z);
                    cell.bestCost = GridCell.MAX_INTEGRATION_COST;
                    cell.bestDirection = Vector2.zero;
                    grid.SetCell(x, z, cell);
                }
            }
        }

        /// <summary>
        /// Calculate integration field using Dijkstra's algorithm
        /// This creates a "distance to goal" map where each cell knows
        /// the cost to reach the destination
        /// </summary>
        private void CalculateIntegrationField(GridPosition destination)
        {
            openSet.Clear();
            closedSet.Clear();

            // Set destination cell cost to 0
            GridCell destCell = grid.GetCell(destination);
            destCell.bestCost = 0;
            grid.SetCell(destination, destCell);

            openSet.Enqueue(destination);

            // Dijkstra expansion
            while (openSet.Count > 0)
            {
                GridPosition current = openSet.Dequeue();

                if (closedSet.Contains(current))
                    continue;

                closedSet.Add(current);

                GridCell currentCell = grid.GetCell(current);

                // Process neighbors
                grid.GetNeighbors(current, out neighborBuffer, out costBuffer);

                for (int i = 0; i < neighborBuffer.Length; i++)
                {
                    GridPosition neighbor = neighborBuffer[i];
                    GridCell neighborCell = grid.GetCell(neighbor);

                    // Skip unwalkable cells
                    if (!neighborCell.IsWalkable)
                        continue;

                    // Skip already processed
                    if (closedSet.Contains(neighbor))
                        continue;

                    // Calculate new cost
                    // cost = current cost + edge cost + terrain cost
                    ushort newCost = (ushort)(currentCell.bestCost +
                                             (costBuffer[i] * neighborCell.cost));

                    // Clamp to prevent overflow
                    if (newCost > GridCell.MAX_INTEGRATION_COST)
                        newCost = GridCell.MAX_INTEGRATION_COST;

                    // Update if we found a better path
                    if (newCost < neighborCell.bestCost)
                    {
                        neighborCell.bestCost = newCost;
                        grid.SetCell(neighbor, neighborCell);

                        // Add to queue for expansion
                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Enqueue(neighbor);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Multi-goal Dijkstra's algorithm
        /// All goals start with cost 0 and expand simultaneously
        /// </summary>
        private void CalculateIntegrationField(List<GridPosition> destinations)
        {
            openSet.Clear();
            closedSet.Clear();

            // Initialize all destinations with cost 0
            foreach (var dest in destinations)
            {
                GridCell destCell = grid.GetCell(dest);
                destCell.bestCost = 0;
                grid.SetCell(dest, destCell);
                openSet.Enqueue(dest);
            }

            // Same Dijkstra expansion as single-goal
            while (openSet.Count > 0)
            {
                GridPosition current = openSet.Dequeue();

                if (closedSet.Contains(current))
                    continue;

                closedSet.Add(current);

                GridCell currentCell = grid.GetCell(current);

                grid.GetNeighbors(current, out neighborBuffer, out costBuffer);

                for (int i = 0; i < neighborBuffer.Length; i++)
                {
                    GridPosition neighbor = neighborBuffer[i];
                    GridCell neighborCell = grid.GetCell(neighbor);

                    if (!neighborCell.IsWalkable || closedSet.Contains(neighbor))
                        continue;

                    ushort newCost = (ushort)(currentCell.bestCost +
                                             (costBuffer[i] * neighborCell.cost));

                    if (newCost > GridCell.MAX_INTEGRATION_COST)
                        newCost = GridCell.MAX_INTEGRATION_COST;

                    if (newCost < neighborCell.bestCost)
                    {
                        neighborCell.bestCost = newCost;
                        grid.SetCell(neighbor, neighborCell);

                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Enqueue(neighbor);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Calculate flow field using gradient descent
        /// Each cell points toward its lowest-cost neighbor
        /// This creates smooth flow paths toward the goal
        /// </summary>
        private void CalculateFlowField()
        {
            for (int z = 0; z < grid.height; z++)
            {
                for (int x = 0; x < grid.width; x++)
                {
                    GridPosition current = new GridPosition(x, z);
                    GridCell currentCell = grid.GetCell(current);

                    // Skip unwalkable or unreachable cells
                    if (!currentCell.IsWalkable ||
                        currentCell.bestCost == GridCell.MAX_INTEGRATION_COST)
                    {
                        continue;
                    }

                    // Find best neighbor (lowest integration cost)
                    GridPosition bestNeighbor = current;
                    ushort bestCost = currentCell.bestCost;

                    grid.GetNeighbors(current, out neighborBuffer, out costBuffer);

                    for (int i = 0; i < neighborBuffer.Length; i++)
                    {
                        GridPosition neighbor = neighborBuffer[i];
                        GridCell neighborCell = grid.GetCell(neighbor);

                        if (!neighborCell.IsWalkable)
                            continue;

                        if (neighborCell.bestCost < bestCost)
                        {
                            bestCost = neighborCell.bestCost;
                            bestNeighbor = neighbor;
                        }
                    }

                    // Calculate direction to best neighbor
                    if (bestNeighbor != current)
                    {
                        Vector2 direction = new Vector2(
                            bestNeighbor.x - current.x,
                            bestNeighbor.z - current.z
                        );

                        currentCell.bestDirection = direction.normalized;
                    }
                    else
                    {
                        // This cell is the goal or isolated
                        currentCell.bestDirection = Vector2.zero;
                    }

                    grid.SetCell(current, currentCell);
                }
            }
        }

        /// <summary>
        /// Check if a path exists from start to destination
        /// </summary>
        public bool PathExists(Vector3 startWorldPos, Vector3 destWorldPos)
        {
            GridPosition startGrid = grid.WorldToGrid(startWorldPos);
            GridPosition destGrid = grid.WorldToGrid(destWorldPos);

            if (!grid.IsValidGridPosition(startGrid) || !grid.IsValidGridPosition(destGrid))
                return false;

            GridCell startCell = grid.GetCell(startGrid);
            GridCell destCell = grid.GetCell(destGrid);

            // Both must be walkable
            if (!startCell.IsWalkable || !destCell.IsWalkable)
                return false;

            // If integration field has been calculated, check if start is reachable
            if (startCell.bestCost < GridCell.MAX_INTEGRATION_COST)
                return true;

            return false;
        }

        /// <summary>
        /// Get estimated path length from position to goal
        /// </summary>
        public float GetPathCost(Vector3 worldPos)
        {
            GridPosition gridPos = grid.WorldToGrid(worldPos);
            if (!grid.IsValidGridPosition(gridPos))
                return float.MaxValue;

            GridCell cell = grid.GetCell(gridPos);
            return cell.bestCost;
        }
    }
}
