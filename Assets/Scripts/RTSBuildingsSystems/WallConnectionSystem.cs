using UnityEngine;
using System.Collections.Generic;
using RTS.Core.Events;

namespace RTS.Buildings
{
    /// <summary>
    /// Handles modular wall connections in RTS style.
    /// Walls automatically detect neighbors and update their visual mesh based on connections.
    /// Attach this component to wall building prefabs.
    /// </summary>
    public class WallConnectionSystem : MonoBehaviour
    {
        [Header("Wall Settings")]
        [SerializeField] private float connectionDistance = 1.5f;
        [Tooltip("Should this wall connect to other walls?")]
        [SerializeField] private bool enableConnections = true;

        [Header("Visual Variants")]
        [Tooltip("Assign 16 mesh variants (index = connection bitmask). North=1, East=2, South=4, West=8")]
        [SerializeField] private GameObject[] meshVariants = new GameObject[16];

        // Static registry of all walls - NO GRID, just a list
        private static List<WallConnectionSystem> allWalls = new List<WallConnectionSystem>();

        // Instance data
        private List<WallConnectionSystem> connectedWalls = new List<WallConnectionSystem>();
        private Building buildingComponent;
        private bool isRegistered = false;

        private void Awake()
        {
            buildingComponent = GetComponent<Building>();
        }

        private void Start()
        {
            if (!enableConnections) return;

            // Register this wall
            allWalls.Add(this);

            // Subscribe to building events
            EventBus.Subscribe<BuildingPlacedEvent>(OnBuildingPlaced);
            EventBus.Subscribe<BuildingDestroyedEvent>(OnBuildingDestroyed);

            // Initial update
            UpdateConnections();
        }

        /// <summary>
        /// Setup or create collider for wall selection and gameplay
        /// </summary>
        private void SetupCollider()
        {
            if (wallCollider == null && autoCreateCollider)
            {
                wallCollider = gameObject.GetComponent<BoxCollider>();
                if (wallCollider == null)
                {
                    wallCollider = gameObject.AddComponent<BoxCollider>();
                }
            }

            if (wallCollider != null)
            {
                // Set default collider size for a standard wall segment
                wallCollider.center = new Vector3(0, 1f, 0);
                wallCollider.size = new Vector3(1f, 2f, 0.5f);
            }
        }

        private void OnDestroy()
        {
            // Unregister this wall
            allWalls.Remove(this);

            // Unsubscribe from events
            EventBus.Unsubscribe<BuildingPlacedEvent>(OnBuildingPlaced);
            EventBus.Unsubscribe<BuildingDestroyedEvent>(OnBuildingDestroyed);

            // Update neighbors when this wall is destroyed
            UpdateNearbyWalls();
        }

        #region Connection Detection

        /// <summary>
        /// Update this wall's connections based on nearby walls - FREE PLACEMENT, NO GRID
        /// </summary>
        public void UpdateConnections()
        {
            if (!enableConnections) return;

            // Clear old connections
            connectedWalls.Clear();

            // Find nearby walls within connection distance
            Vector3 myPos = transform.position;
            foreach (var otherWall in allWalls)
            {
                if (otherWall == this || otherWall == null) continue;

                float distance = Vector3.Distance(myPos, otherWall.transform.position);
                if (distance <= connectionDistance)
                {
                    connectedWalls.Add(otherWall);
                }
            }

            // Update visual based on connections
            UpdateVisual();
        }

        /// <summary>
        /// Update all nearby walls' connections
        /// </summary>
        private void UpdateNearbyWalls()
        {
            Vector3 myPos = transform.position;
            foreach (var wall in allWalls)
            {
                if (wall == this || wall == null) continue;

                float distance = Vector3.Distance(myPos, wall.transform.position);
                if (distance <= connectionDistance * 2f) // Update walls within double connection distance
                {
                    wall.UpdateConnections();
                }
            }
        }

        #endregion

        #region Visual Updates

        /// <summary>
        /// Update visual - SUPER SIMPLE, just rotate to face connected walls
        /// </summary>
        private void UpdateVisual()
        {
            if (wallMesh == null) return;

            int connectionCount = connectedWalls.Count;

            if (connectionCount == 0)
            {
                // Standalone - no rotation
                wallMesh.transform.localRotation = Quaternion.identity;
            }
            else if (connectionCount == 1)
            {
                // End piece - face the connected wall
                Vector3 directionToWall = (connectedWalls[0].transform.position - transform.position).normalized;
                float angle = Mathf.Atan2(directionToWall.x, directionToWall.z) * Mathf.Rad2Deg;
                wallMesh.transform.localRotation = Quaternion.Euler(0, angle, 0);
            }
            else if (connectionCount == 2)
            {
                // Straight wall - face between two connections
                Vector3 dir1 = (connectedWalls[0].transform.position - transform.position).normalized;
                Vector3 dir2 = (connectedWalls[1].transform.position - transform.position).normalized;
                Vector3 avgDir = (dir1 + dir2).normalized;
                float angle = Mathf.Atan2(avgDir.x, avgDir.z) * Mathf.Rad2Deg;
                wallMesh.transform.localRotation = Quaternion.Euler(0, angle, 0);
            }
            else
            {
                // Junction (3+ connections) - keep default
                wallMesh.transform.localRotation = Quaternion.identity;
        /// Update the visual mesh based on connection state.
        /// </summary>
        private void UpdateVisual()
        {
            if (meshVariants == null || meshVariants.Length != 16)
            {
                Debug.LogWarning($"Wall at {gridPosition}: meshVariants array must have exactly 16 elements!");
                return;
            }

            // Deactivate all variants
            for (int i = 0; i < meshVariants.Length; i++)
            {
                if (meshVariants[i] != null)
                {
                    meshVariants[i].SetActive(false);
                }
            }

            // Activate the correct variant
            if (connectionState >= 0 && connectionState < meshVariants.Length)
            {
                if (meshVariants[connectionState] != null)
                {
                    meshVariants[connectionState].SetActive(true);
                }
                else
                {
                    Debug.LogWarning($"Wall at {gridPosition}: mesh variant {connectionState} is not assigned!");
                }
            }
        }

        #endregion

        #region Event Handlers

        private void OnBuildingPlaced(BuildingPlacedEvent evt)
        {
            // Check if the placed building is a wall nearby
            if (evt.Building == null) return;

            var wallSystem = evt.Building.GetComponent<WallConnectionSystem>();
            if (wallSystem != null)
            {
                // Update if nearby
                float distance = Vector3.Distance(transform.position, evt.Position);
                if (distance <= connectionDistance * 2f)
                {
                    UpdateConnections();
                }
            }
        }

        private void OnBuildingDestroyed(BuildingDestroyedEvent evt)
        {
            // Handled in OnDestroy() which calls UpdateNearbyWalls()
        }

        #endregion

        #region Debug Helpers

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !enableConnections) return;

            // Draw connection distance sphere
            Gizmos.color = new Color(0, 1, 0, 0.1f);
            Gizmos.DrawWireSphere(transform.position, connectionDistance);

            // Draw lines to connected walls
            Gizmos.color = Color.green;
            foreach (var wall in connectedWalls)
            {
                if (wall != null)
                {
                    Gizmos.DrawLine(transform.position, wall.transform.position);
                }
            }
        }

        [ContextMenu("Force Update Connections")]
        private void DebugForceUpdate()
        {
            UpdateConnections();
        }

        [ContextMenu("Print Connections")]
        private void DebugPrintConnections()
        {
            Debug.Log($"Wall at {transform.position}: {connectedWalls.Count} connections");
        }

        [ContextMenu("Print All Walls")]
        private void DebugPrintAllWalls()
        {
            Debug.Log($"Total walls: {allWalls.Count}");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get number of connected walls
        /// </summary>
        public int GetConnectionCount() => connectedWalls.Count;

        /// <summary>
        /// Get list of connected walls
        /// </summary>
        public List<WallConnectionSystem> GetConnectedWalls() => connectedWalls;

        /// <summary>
        /// Manual rotation of wall mesh (for player adjustment)
        /// </summary>
        public void RotateWall(float yRotation)
        {
            if (wallMesh != null)
            {
                wallMesh.transform.Rotate(0, yRotation, 0);
            }
        public Vector2Int GetGridPosition() => gridPosition;

        /// <summary>
        /// Check if this wall is connected in a specific direction.
        /// </summary>
        public bool IsConnected(WallDirection direction)
        {
            return (connectionState & (int)direction) != 0;
        }

        #endregion
    }

    /// <summary>
    /// Wall connection directions.
    /// </summary>
    public enum WallDirection
    {
        North = 1,
        East = 2,
        South = 4,
        West = 8
    }
}
