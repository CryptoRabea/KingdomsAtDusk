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
        #pragma warning disable CS0414 // Field is assigned but never used - reserved for future wall detection feature
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
        /// Returns true if snapped, and outputs the snapped position and wall object.
        /// </summary>
        public bool TrySnapToWall(Vector3 position, TowerDataSO towerData, out Vector3 outPosition, out GameObject outWall)
        {
            outPosition = position;
            outWall = null;

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
                outPosition = nearest.transform.position;
                outWall = nearest;
                return true;
            }

            return false;
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
                        var wallComp = hit.GetComponent<WallConnectionSystem>();
                        var buildingComp = hit.GetComponent<Building>();

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
        /// Update snap preview (for visual feedback).
        /// Call this during placement preview update.
        /// </summary>
        public void UpdateSnapPreview(Vector3 currentPosition, TowerDataSO towerData)
        {
            isSnappedToWall = TrySnapToWall(currentPosition, towerData, out snappedPosition, out nearestWall);
        }

        /// <summary>
        /// Replace a wall with a tower.
        /// Destroys the wall and places the tower in its position.
        /// </summary>
        public void ReplaceWallWithTower(GameObject wall, GameObject towerPrefab, TowerDataSO towerData)
        {
            if (wall == null || towerPrefab == null)
            {
                Debug.LogWarning("Cannot replace wall: wall or tower prefab is null!");
                return;
            }

            Vector3 wallPosition = wall.transform.position;
            Quaternion wallRotation = wall.transform.rotation;

            // Store wall data before destroying it
            var wallBuilding = wall.GetComponent<Building>();
            string wallName = wallBuilding != null ? wallBuilding.Data?.buildingName : "Wall";

            Debug.Log($"Replacing {wallName} at {wallPosition} with {towerData.buildingName}");

            // Destroy the wall
            Destroy(wall);

            // Note: The tower placement will be handled by BuildingManager
            // This method just handles the wall destruction
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
                    Debug.Log($"Cannot replace wall: too many connections ({connectionCount})");
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
}
