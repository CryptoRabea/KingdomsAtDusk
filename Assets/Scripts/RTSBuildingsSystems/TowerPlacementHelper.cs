using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace RTS.Buildings
{
    /// <summary>
    /// Helper component for BuildingManager to handle tower-specific placement.
    /// Handles wall snapping and wall replacement logic.
    /// </summary>
    public class TowerPlacementHelper : MonoBehaviour
    {
        [Header("Wall Detection")]
        [SerializeField] private float wallDetectionRadius = 2f;
        #pragma warning restore CS0414
        [SerializeField] private LayerMask wallLayer; // Optional: specific layer for walls

        [Header("Visual Feedback")]
        [SerializeField] private Color wallSnapColor = Color.cyan;
        [SerializeField] private float snapIndicatorSize = 0.5f;

        // State
        private Vector3 snappedPosition;
        private bool isSnappedToWall = false;
        private GameObject nearestWall;

        public bool IsSnappedToWall => isSnappedToWall;
        public GameObject NearestWall => nearestWall;
        public Vector3 SnappedPosition => snappedPosition;

        /// <summary>
        /// Try to snap position to nearest wall within range.
        /// Returns true if snapped, and outputs the snapped position, rotation, and wall object(s).
        /// </summary>
        public bool TrySnapToWall(Vector3 position, TowerDataSO towerData, out Vector3 outPosition, out Quaternion outRotation, out List<GameObject> outWalls)
        {
            outPosition = position;
            outRotation = Quaternion.identity;
            outWalls = new List<GameObject>();

            if (towerData == null || !towerData.canReplaceWalls)
            {
                return false;
            }

            // Find all walls in detection radius
            List<GameObject> nearbyWalls = FindNearbyWalls(position, towerData.wallSnapDistance);

            if (nearbyWalls.Count == 0)
            {
                return false;
            }

            // Find nearest wall
            GameObject nearest = null;
            float minDistance = float.MaxValue;

            foreach (var wall in nearbyWalls)
            {
                float distance = Vector3.Distance(position, wall.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = wall;
                }
            }

            if (nearest != null)
            {
                // Calculate rotation from wall direction
                outRotation = CalculateWallRotation(nearest);

                // Find all wall segments that the tower will cover
                if (towerData.replaceMultipleSegments)
                {
                    outWalls = FindWallSegmentsToCover(nearest, towerData, out Vector3 adjustedPosition);

                    // Use adjusted position if allowed
                    if (towerData.allowPositionAdjustment)
                    {
                        // Check if adjustment is within allowed range
                        float adjustmentDistance = Vector3.Distance(nearest.transform.position, adjustedPosition);
                        if (adjustmentDistance <= towerData.maxPositionAdjustment)
                        {
                            outPosition = adjustedPosition;
                        }
                        else
                        {
                            outPosition = nearest.transform.position;
                        }
                    }
                    else
                    {
                        outPosition = nearest.transform.position;
                    }
                }
                else
                {
                    // Single wall replacement
                    outPosition = nearest.transform.position;
                    outWalls.Add(nearest);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Overload for backward compatibility (returns single wall).
        /// </summary>
        public bool TrySnapToWall(Vector3 position, TowerDataSO towerData, out Vector3 outPosition, out GameObject outWall)
        {
            bool result = TrySnapToWall(position, towerData, out outPosition, out Quaternion rotation, out List<GameObject> walls);
            outWall = walls.Count > 0 ? walls[0] : null;
            return result;
        }

        /// <summary>
        /// Find all walls near a position.
        /// </summary>
        private List<GameObject> FindNearbyWalls(Vector3 position, float radius)
        {
            List<GameObject> walls = new List<GameObject>();

            // Get all WallConnectionSystem components in scene
            WallConnectionSystem[] allWalls = WallConnectionSystem.GetAllWalls().ToArray();

            foreach (var wallSystem in allWalls)
            {
                if (wallSystem == null) continue;

                float distance = Vector3.Distance(position, wallSystem.transform.position);
                if (distance <= radius)
                {
                    walls.Add(wallSystem.gameObject);
                }
            }

            // Also try physics overlap if layer mask is set
            if (wallLayer.value != 0)
            {
                Collider[] hits = Physics.OverlapSphere(position, radius, wallLayer);
                foreach (var hit in hits)
                {
                    if (!walls.Contains(hit.gameObject))
                    {
                        // Check if it has WallConnectionSystem or is a wall building
                        if (hit.TryGetComponent<WallConnectionSystem>(out var wallComp))
                        {
                        }
                        if (hit.TryGetComponent<Building>(out var buildingComp))
                        {
                        }

                        if (wallComp != null || (buildingComp != null && buildingComp.Data?.buildingType == BuildingType.Defensive))
                        {
                            walls.Add(hit.gameObject);
                        }
                    }
                }
            }

            return walls;
        }

        /// <summary>
        /// Calculate perfect rotation from wall direction.
        /// Removes X and Z rotation components, keeping only Y (yaw) for alignment.
        /// </summary>
        private Quaternion CalculateWallRotation(GameObject wall)
        {
            if (wall == null) return Quaternion.identity;

            // Get wall rotation and extract only the Y axis rotation
            Quaternion wallRot = wall.transform.rotation;
            Vector3 euler = wallRot.eulerAngles;
            euler.x = 0f; // Remove pitch
            euler.z = 0f; // Remove roll

            return Quaternion.Euler(euler);
        }

        /// <summary>
        /// Find all wall segments that the tower will cover based on its size.
        /// Returns list of walls and calculates optimal centered position.
        /// </summary>
        private List<GameObject> FindWallSegmentsToCover(GameObject centerWall, TowerDataSO towerData, out Vector3 centerPosition)
        {
            List<GameObject> wallsToCover = new List<GameObject>();
            centerPosition = centerWall.transform.position;

            if (centerWall == null || towerData == null)
            {
                return wallsToCover;
            }

            // Get the wall connection system
            WallConnectionSystem centerWallSystem = centerWall.GetComponent<WallConnectionSystem>();
            if (centerWallSystem == null)
            {
                // If no connection system, just return the center wall
                wallsToCover.Add(centerWall);
                return wallsToCover;
            }

            // Get wall direction vector
            Vector3 wallDirection = centerWall.transform.forward;
            wallDirection.y = 0;
            wallDirection.Normalize();

            // Calculate how far the tower extends from center in each direction
            float halfTowerLength = towerData.towerWallLength / 2f;

            // Find all connected walls along the wall line
            List<GameObject> connectedWalls = new List<GameObject>();
            connectedWalls.Add(centerWall);

            // Get walls connected to center wall
            List<WallConnectionSystem> centerConnections = centerWallSystem.GetConnectedWalls();

            // Traverse in both directions along the wall
            TraverseWallDirection(centerWall, centerWallSystem, wallDirection, halfTowerLength, connectedWalls);
            TraverseWallDirection(centerWall, centerWallSystem, -wallDirection, halfTowerLength, connectedWalls);

            // Calculate the actual center position based on walls found
            if (connectedWalls.Count > 1)
            {
                // Find the geometric center of all wall segments
                Vector3 minPoint = connectedWalls[0].transform.position;
                Vector3 maxPoint = connectedWalls[0].transform.position;

                foreach (var wall in connectedWalls)
                {
                    Vector3 wallPos = wall.transform.position;

                    // Project onto wall direction to find min/max along the wall line
                    float projection = Vector3.Dot(wallPos - centerWall.transform.position, wallDirection);

                    if (projection < Vector3.Dot(minPoint - centerWall.transform.position, wallDirection))
                        minPoint = wallPos;
                    if (projection > Vector3.Dot(maxPoint - centerWall.transform.position, wallDirection))
                        maxPoint = wallPos;
                }

                centerPosition = (minPoint + maxPoint) / 2f;
            }

            wallsToCover = connectedWalls;
            return wallsToCover;
        }

        /// <summary>
        /// Traverse wall connections in a specific direction to find segments within tower range.
        /// </summary>
        private void TraverseWallDirection(GameObject startWall, WallConnectionSystem startSystem, Vector3 direction, float maxDistance, List<GameObject> foundWalls)
        {
            float currentDistance = 0f;
            GameObject currentWall = startWall;
            WallConnectionSystem currentSystem = startSystem;

            while (currentDistance < maxDistance)
            {
                // Find the next wall in this direction
                GameObject nextWall = FindNextWallInDirection(currentWall, currentSystem, direction);

                if (nextWall == null || foundWalls.Contains(nextWall))
                    break;

                // Calculate distance to next wall
                float distToNext = Vector3.Distance(currentWall.transform.position, nextWall.transform.position);
                currentDistance += distToNext;

                if (currentDistance <= maxDistance)
                {
                    foundWalls.Add(nextWall);
                    currentWall = nextWall;
                    currentSystem = nextWall.GetComponent<WallConnectionSystem>();

                    if (currentSystem == null)
                        break;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Find the next connected wall in a specific direction.
        /// </summary>
        private GameObject FindNextWallInDirection(GameObject currentWall, WallConnectionSystem wallSystem, Vector3 direction)
        {
            if (wallSystem == null) return null;

            List<WallConnectionSystem> connections = wallSystem.GetConnectedWalls();
            GameObject bestMatch = null;
            float bestAlignment = -1f;

            foreach (var connectedSystem in connections)
            {
                if (connectedSystem == null) continue;

                GameObject connectedWall = connectedSystem.gameObject;
                Vector3 toConnected = (connectedWall.transform.position - currentWall.transform.position).normalized;
                toConnected.y = 0;

                float alignment = Vector3.Dot(toConnected, direction);

                // Must be in the same direction (positive dot product)
                if (alignment > 0.5f && alignment > bestAlignment)
                {
                    bestAlignment = alignment;
                    bestMatch = connectedWall;
                }
            }

            return bestMatch;
        }

        /// <summary>
        /// Update snap preview (for visual feedback).
        /// Call this during placement preview update.
        /// </summary>
        public void UpdateSnapPreview(Vector3 currentPosition, TowerDataSO towerData)
        {
            isSnappedToWall = TrySnapToWall(currentPosition, towerData, out snappedPosition, out nearestWall);
        }

        /// <summary>
        /// Replace wall(s) with a tower.
        /// Stores wall connection data and returns it for the tower to inherit.
        /// Handles both single and multi-segment replacement.
        /// </summary>
        public WallReplacementData ReplaceWallWithTower(List<GameObject> walls, TowerDataSO towerData)
        {
            if (walls == null || walls.Count == 0)
            {
                return null;
            }

            // Use center wall as reference
            GameObject centerWall = walls[0];
            Vector3 wallPosition = centerWall.transform.position;
            Quaternion wallRotation = centerWall.transform.rotation;

            // Collect all connections from all walls being replaced
            List<WallConnectionSystem> allConnectedWalls = new List<WallConnectionSystem>();

            foreach (var wall in walls)
            {
                if (wall == null) continue;

                if (wall.TryGetComponent<WallConnectionSystem>(out var wallConnection))
                {
                    List<WallConnectionSystem> connections = wallConnection.GetConnectedWalls();
                    foreach (var conn in connections)
                    {
                        // Only add connections that are NOT part of the walls being replaced
                        bool isBeingReplaced = false;
                        foreach (var replacedWall in walls)
                        {
                            if (conn.gameObject == replacedWall)
                            {
                                isBeingReplaced = true;
                                break;
                            }
                        }

                        if (!isBeingReplaced && !allConnectedWalls.Contains(conn))
                        {
                            allConnectedWalls.Add(conn);
                        }
                    }
                }
            }

            // Calculate optimal position (average of all wall positions)
            if (walls.Count > 1)
            {
                Vector3 totalPos = Vector3.zero;
                foreach (var wall in walls)
                {
                    totalPos += wall.transform.position;
                }
                wallPosition = totalPos / walls.Count;
            }

            // Create replacement data
            var replacementData = new WallReplacementData
            {
                originalWalls = new List<GameObject>(walls),
                position = wallPosition,
                rotation = wallRotation,
                connectedWalls = allConnectedWalls
            };

            return replacementData;
        }

        /// <summary>
        /// Overload for single wall replacement (backward compatibility).
        /// </summary>
        public WallReplacementData ReplaceWallWithTower(GameObject wall, TowerDataSO towerData)
        {
            if (wall == null) return null;
            return ReplaceWallWithTower(new List<GameObject> { wall }, towerData);
        }

        /// <summary>
        /// Apply wall connections to a tower that replaced a wall.
        /// This makes the tower act as a wall segment for connection purposes.
        /// </summary>
        public void ApplyWallConnectionsToTower(GameObject tower, WallReplacementData replacementData)
        {
            if (tower == null || replacementData == null) return;

            // Add WallConnectionSystem to tower so it maintains wall continuity
            if (tower.TryGetComponent<WallConnectionSystem>(out var towerWallConnection))
            {
            }
            if (towerWallConnection == null)
            {
                towerWallConnection = tower.AddComponent<WallConnectionSystem>();
            }


            // Force update connections after a short delay to ensure all systems are initialized
            if (towerWallConnection != null)
            {
                StartCoroutine(DelayedConnectionUpdate(towerWallConnection));
            }
        }

        private System.Collections.IEnumerator DelayedConnectionUpdate(WallConnectionSystem wallConnection)
        {
            yield return new WaitForSeconds(0.2f);
            wallConnection.UpdateConnections();
        }

        /// <summary>
        /// Check if a wall can be replaced (not connected to too many walls, etc.).
        /// This prevents breaking wall continuity.
        /// </summary>
        public bool CanReplaceWall(GameObject wall, bool checkConnections = true)
        {
            if (wall == null) return false;

            if (!checkConnections) return true;

            // Check wall connections
            if (wall.TryGetComponent<WallConnectionSystem>(out var wallSystem))
            {
                // Don't replace walls with many connections (corner walls)
                // This is optional - you can adjust this logic
                int connectionCount = wallSystem.GetConnectionCount();

                // Allow replacement of walls with 0-2 connections
                // Block replacement of walls with 3+ connections (corner pieces)
                if (connectionCount > 2)
                {
                    return false;
                }
            }

            return true;
        }

        #region Debug Visualization

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            // Draw snap indicator
            if (isSnappedToWall && nearestWall != null)
            {
                Gizmos.color = wallSnapColor;
                Gizmos.DrawWireSphere(snappedPosition, snapIndicatorSize);
                Gizmos.DrawLine(snappedPosition, snappedPosition + Vector3.up * 2f);

                // Draw line to wall
                Gizmos.DrawLine(snappedPosition, nearestWall.transform.position);
            }
        }

        #endregion

        #region Public Utilities

        /// <summary>
        /// Get all walls within a certain radius of a position.
        /// </summary>
        public static List<GameObject> GetWallsInRadius(Vector3 position, float radius)
        {
            List<GameObject> walls = new List<GameObject>();

            WallConnectionSystem[] allWalls = WallConnectionSystem.GetAllWalls().ToArray();

            foreach (var wallSystem in allWalls)
            {
                if (wallSystem == null) continue;

                float distance = Vector3.Distance(position, wallSystem.transform.position);
                if (distance <= radius)
                {
                    walls.Add(wallSystem.gameObject);
                }
            }

            return walls;
        }

        /// <summary>
        /// Find the nearest wall to a position.
        /// </summary>
        public static GameObject FindNearestWall(Vector3 position, float maxDistance = float.MaxValue)
        {
            WallConnectionSystem[] allWalls = WallConnectionSystem.GetAllWalls().ToArray();

            GameObject nearest = null;
            float minDistance = maxDistance;

            foreach (var wallSystem in allWalls)
            {
                if (wallSystem == null) continue;

                float distance = Vector3.Distance(position, wallSystem.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = wallSystem.gameObject;
                }
            }

            return nearest;
        }

        #endregion
    }

    /// <summary>
    /// Data structure for storing wall replacement information.
    /// Used when replacing wall(s) with a tower or gate.
    /// Supports both single and multi-segment replacement.
    /// </summary>
    public class WallReplacementData
    {
        public List<GameObject> originalWalls;  // All walls being replaced
        public Vector3 position;                // Calculated optimal position
        public Quaternion rotation;             // Wall rotation for tower alignment
        public List<WallConnectionSystem> connectedWalls;  // External wall connections to maintain

        // Legacy support - returns first wall
        public GameObject OriginalWall => (originalWalls != null && originalWalls.Count > 0) ? originalWalls[0] : null;

        public GameObject originalWall { get; internal set; }
    }
}
