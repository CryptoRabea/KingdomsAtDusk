using UnityEngine;
using System.Collections.Generic;

namespace FlowField.Movement
{
    /// <summary>
    /// Lightweight RVO (Reciprocal Velocity Obstacles) local avoidance
    /// Prevents units from colliding while maintaining smooth movement
    /// Simpler than full RVO2 library but very effective for RTS games
    /// </summary>
    public class LocalAvoidance
    {
        // Spatial hash grid for fast neighbor queries
        private readonly SpatialHashGrid spatialGrid;

        // Configuration
        private readonly float avoidanceRadius;
        private readonly float maxNeighbors;
        private readonly float timeHorizon;

        // Reusable buffers (no allocations)
        private readonly List<FlowFieldFollower> neighborBuffer;
        private readonly Collider[] colliderBuffer;

        public LocalAvoidance(float avoidanceRadius = 2f, int maxNeighbors = 10, float timeHorizon = 1.5f)
        {
            this.avoidanceRadius = avoidanceRadius;
            this.maxNeighbors = maxNeighbors;
            this.timeHorizon = timeHorizon;

            this.neighborBuffer = new List<FlowFieldFollower>(maxNeighbors);
            this.colliderBuffer = new Collider[maxNeighbors];

            // Initialize spatial grid (cell size = avoidance radius for optimal performance)
            this.spatialGrid = new SpatialHashGrid(avoidanceRadius);
        }

        /// <summary>
        /// Calculate avoidance vector for a unit
        /// Returns velocity adjustment to avoid nearby units
        /// </summary>
        public Vector3 CalculateAvoidanceVelocity(
            FlowFieldFollower unit,
            Vector3 preferredVelocity,
            float radius)
        {
            // Find nearby units using spatial hash
            FindNeighbors(unit, radius);

            if (neighborBuffer.Count == 0)
            {
                return Vector3.zero; // No avoidance needed
            }

            Vector3 avoidanceVelocity = Vector3.zero;
            int validNeighbors = 0;

            foreach (var neighbor in neighborBuffer)
            {
                if (neighbor == null || neighbor == unit)
                    continue;

                // Calculate relative position and velocity
                Vector3 relativePos = neighbor.transform.position - unit.transform.position;
                Vector3 relativeVel = neighbor.CurrentVelocity - unit.CurrentVelocity;

                float distance = relativePos.magnitude;
                if (distance < 0.01f)
                    continue; // Too close, skip

                // Time to collision (simplified)
                float timeToCollision = ComputeTimeToCollision(
                    relativePos,
                    relativeVel,
                    radius + neighbor.Radius
                );

                // Only avoid if collision is imminent
                if (timeToCollision > 0 && timeToCollision < timeHorizon)
                {
                    // Calculate avoidance direction (perpendicular to relative velocity)
                    Vector3 avoidanceDir = Vector3.Cross(relativeVel, Vector3.up);

                    if (avoidanceDir.sqrMagnitude < 0.01f)
                    {
                        // Fallback: push away from neighbor
                        avoidanceDir = -relativePos.normalized;
                    }
                    else
                    {
                        avoidanceDir.Normalize();
                    }

                    // Weight by urgency (closer collisions = stronger avoidance)
                    float urgency = 1f - (timeToCollision / timeHorizon);
                    float weight = urgency * urgency; // Quadratic falloff

                    avoidanceVelocity += avoidanceDir * weight;
                    validNeighbors++;
                }
            }

            if (validNeighbors > 0)
            {
                avoidanceVelocity /= validNeighbors; // Average
                return avoidanceVelocity;
            }

            return Vector3.zero;
        }

        /// <summary>
        /// Simpler separation-based avoidance (fallback method)
        /// Just pushes units apart if too close
        /// </summary>
        public Vector3 CalculateSeparationVelocity(
            FlowFieldFollower unit,
            float separationRadius)
        {
            FindNeighbors(unit, separationRadius);

            if (neighborBuffer.Count == 0)
                return Vector3.zero;

            Vector3 separation = Vector3.zero;
            int count = 0;

            foreach (var neighbor in neighborBuffer)
            {
                if (neighbor == null || neighbor == unit)
                    continue;

                Vector3 offset = unit.transform.position - neighbor.transform.position;
                float distance = offset.magnitude;

                if (distance < separationRadius && distance > 0.01f)
                {
                    // Push away, stronger when closer
                    float strength = (separationRadius - distance) / separationRadius;
                    separation += (offset / distance) * strength;
                    count++;
                }
            }

            if (count > 0)
            {
                separation /= count;
            }

            return separation;
        }

        /// <summary>
        /// Find neighbors using Physics.OverlapSphereNonAlloc (fast, no GC)
        /// </summary>
        private void FindNeighbors(FlowFieldFollower unit, float radius)
        {
            neighborBuffer.Clear();

            // Use physics overlap (supports layers)
            int count = Physics.OverlapSphereNonAlloc(
                unit.transform.position,
                radius,
                colliderBuffer,
                LayerMask.GetMask("Unit") // Adjust layer name as needed
            );

            for (int i = 0; i < count; i++)
            {
                var follower = colliderBuffer[i].GetComponent<FlowFieldFollower>();
                if (follower != null && follower != unit)
                {
                    neighborBuffer.Add(follower);
                }
            }
        }

        /// <summary>
        /// Compute time to collision between two moving circles
        /// Returns negative if no collision
        /// </summary>
        private float ComputeTimeToCollision(Vector3 relativePos, Vector3 relativeVel, float combinedRadius)
        {
            // Project relative position onto relative velocity
            float a = relativeVel.sqrMagnitude;

            if (a < 0.0001f)
                return -1f; // No relative motion

            float b = 2f * Vector3.Dot(relativePos, relativeVel);
            float c = relativePos.sqrMagnitude - (combinedRadius * combinedRadius);

            // Quadratic formula discriminant
            float discriminant = b * b - 4f * a * c;

            if (discriminant < 0)
                return -1f; // No collision

            // Time to collision
            float t = (-b - Mathf.Sqrt(discriminant)) / (2f * a);

            return t > 0 ? t : -1f;
        }

        /// <summary>
        /// Register a unit for spatial hashing (call when unit spawns)
        /// </summary>
        public void RegisterUnit(FlowFieldFollower unit)
        {
            spatialGrid?.AddUnit(unit);
        }

        /// <summary>
        /// Unregister a unit (call when unit dies)
        /// </summary>
        public void UnregisterUnit(FlowFieldFollower unit)
        {
            spatialGrid?.RemoveUnit(unit);
        }

        /// <summary>
        /// Update spatial hash (call once per frame before all avoidance calculations)
        /// </summary>
        public void UpdateSpatialHash()
        {
            spatialGrid?.Update();
        }
    }

    /// <summary>
    /// Spatial hash grid for fast neighbor queries
    /// Divides world into cells, units query only their cell + neighbors
    /// </summary>
    public class SpatialHashGrid
    {
        private readonly float cellSize;
        private readonly Dictionary<Vector2Int, List<FlowFieldFollower>> grid;

        public SpatialHashGrid(float cellSize)
        {
            this.cellSize = cellSize;
            this.grid = new Dictionary<Vector2Int, List<FlowFieldFollower>>();
        }

        public void AddUnit(FlowFieldFollower unit)
        {
            Vector2Int cell = GetCell(unit.transform.position);

            if (!grid.ContainsKey(cell))
            {
                grid[cell] = new List<FlowFieldFollower>();
            }

            grid[cell].Add(unit);
        }

        public void RemoveUnit(FlowFieldFollower unit)
        {
            Vector2Int cell = GetCell(unit.transform.position);

            if (grid.ContainsKey(cell))
            {
                grid[cell].Remove(unit);
            }
        }

        public void Update()
        {
            // Clear and rebuild (simple approach)
            // For optimization, you can track unit movement and only update moved units
            grid.Clear();
        }

        public List<FlowFieldFollower> GetNearbyUnits(Vector3 position, float radius)
        {
            List<FlowFieldFollower> nearby = new List<FlowFieldFollower>();
            Vector2Int centerCell = GetCell(position);

            // Check cell + 8 neighbors
            int cellRadius = Mathf.CeilToInt(radius / cellSize);

            for (int x = -cellRadius; x <= cellRadius; x++)
            {
                for (int z = -cellRadius; z <= cellRadius; z++)
                {
                    Vector2Int cell = centerCell + new Vector2Int(x, z);

                    if (grid.ContainsKey(cell))
                    {
                        nearby.AddRange(grid[cell]);
                    }
                }
            }

            return nearby;
        }

        private Vector2Int GetCell(Vector3 worldPosition)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPosition.x / cellSize),
                Mathf.FloorToInt(worldPosition.z / cellSize)
            );
        }
    }
}
