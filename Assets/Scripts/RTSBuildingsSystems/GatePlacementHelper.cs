using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace RTS.Buildings
{
    /// <summary>
    /// Helper component for BuildingManager to handle gate-specific placement.
    /// Handles wall snapping and wall replacement logic for gates.
    /// </summary>
    public class GatePlacementHelper : MonoBehaviour
    {
        [Header("Wall Detection")]
        [SerializeField] private float wallDetectionRadius = 2f;
        [SerializeField] private LayerMask wallLayer; // Optional: specific layer for walls

        [Header("Visual Feedback")]
        [SerializeField] private Color wallSnapColor = Color.magenta;
        [SerializeField] private float snapIndicatorSize = 0.5f;

        // State
        private Vector3 snappedPosition;
        private Quaternion snappedRotation;
        private bool isSnappedToWall = false;
        private GameObject nearestWall;

        public bool IsSnappedToWall => isSnappedToWall;
        public GameObject NearestWall => nearestWall;
        public Vector3 SnappedPosition => snappedPosition;
        public Quaternion SnappedRotation => snappedRotation;

        /// <summary>
        /// Try to snap position to nearest wall within range.
        /// Returns true if snapped, and outputs the snapped position, rotation, and wall object.
        /// </summary>
        public bool TrySnapToWall(Vector3 position, GateDataSO gateData, out Vector3 outPosition, out Quaternion outRotation, out GameObject outWall)
        {
            outPosition = position;
            outRotation = Quaternion.identity;
            outWall = null;

            if (gateData == null || !gateData.canReplaceWalls)
            {
                return false;
            }

            // Find all walls in detection radius
            List<GameObject> nearbyWalls = FindNearbyWalls(position, gateData.wallSnapDistance);

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
                if (nearest != null)
                {
                    outPosition = nearest.transform.position;

                    // Remove X-rotation (and Z for stability)
                    Quaternion wallRot = nearest.transform.rotation;
                    Vector3 euler = wallRot.eulerAngles;
                    euler.x = 0f;
                    euler.z = 0f;
                    outRotation = Quaternion.Euler(euler);

                    outWall = nearest;
                    return true;
                }
                
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
        /// Update snap preview (for visual feedback).
        /// Call this during placement preview update.
        /// </summary>
        public void UpdateSnapPreview(Vector3 currentPosition, GateDataSO gateData)
        {
            isSnappedToWall = TrySnapToWall(currentPosition, gateData, out snappedPosition, out snappedRotation, out nearestWall);
        }

        /// <summary>
        /// Replace a wall with a gate.
        /// Stores wall connection data and returns it for the gate to inherit.
        /// </summary>
        public WallReplacementData ReplaceWallWithGate(GameObject wall, GateDataSO gateData)
        {
            if (wall == null)
            {
                return null;
            }

            Vector3 wallPosition = wall.transform.position;
            Quaternion wallRotation = wall.transform.rotation;

            // Store wall connection data before destroying it
            if (wall.TryGetComponent<WallConnectionSystem>(out var wallConnection))
            {
            }
            List<WallConnectionSystem> connectedWalls = null;

            if (wallConnection != null)
            {
                connectedWalls = wallConnection.GetConnectedWalls();
            }

            if (wall.TryGetComponent<Building>(out var wallBuilding))
            {
            }
            string wallName = wallBuilding != null ? wallBuilding.Data?.buildingName : "Wall";


            // Create replacement data
            var replacementData = new WallReplacementData
            {
                originalWall = wall,
                position = wallPosition,
                rotation = wallRotation,
                connectedWalls = connectedWalls
            };

            return replacementData;
        }

        /// <summary>
        /// Apply wall connections to a gate that replaced a wall.
        /// This makes the gate act as a wall segment for connection purposes.
        /// </summary>
        public void ApplyWallConnectionsToGate(GameObject gate, WallReplacementData replacementData)
        {
            if (gate == null || replacementData == null) return;

            // Add WallConnectionSystem to gate so it maintains wall continuity
            if (gate.TryGetComponent<WallConnectionSystem>(out var gateWallConnection))
            {
            }
            if (gateWallConnection == null)
            {
                gateWallConnection = gate.AddComponent<WallConnectionSystem>();
            }


            // Force update connections after a short delay to ensure all systems are initialized
            if (gateWallConnection != null)
            {
                StartCoroutine(DelayedConnectionUpdate(gateWallConnection));
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

                // Draw rotation indicator
                Gizmos.color = Color.blue;
                Vector3 forward = snappedRotation * Vector3.forward;
                Gizmos.DrawRay(snappedPosition, forward * 2f);
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
