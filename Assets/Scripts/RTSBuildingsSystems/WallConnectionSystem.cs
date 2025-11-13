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
        /// Update visual mesh - activates/deactivates mesh variants based on connections
        /// </summary>
        private void UpdateVisual()
        {
            if (meshVariants == null || meshVariants.Length != 16)
            {
                // No variants configured, skip visual update
                return;
            }

            // Calculate connection bitmask
            int connectionBitmask = CalculateConnectionBitmask();

            // Deactivate all variants
            for (int i = 0; i < meshVariants.Length; i++)
            {
                if (meshVariants[i] != null)
                {
                    meshVariants[i].SetActive(false);
                }
            }

            // Activate the correct variant based on bitmask
            if (connectionBitmask >= 0 && connectionBitmask < meshVariants.Length)
            {
                if (meshVariants[connectionBitmask] != null)
                {
                    meshVariants[connectionBitmask].SetActive(true);
                }
            }
        }

        /// <summary>
        /// Calculate connection bitmask based on nearby walls
        /// North=1, East=2, South=4, West=8
        /// </summary>
        private int CalculateConnectionBitmask()
        {
            int bitmask = 0;

            foreach (var wall in connectedWalls)
            {
                if (wall == null) continue;

                Vector3 direction = wall.transform.position - transform.position;
                direction.y = 0; // Ignore vertical difference

                // Determine which direction the wall is in
                if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
                {
                    // East or West
                    if (direction.x > 0)
                        bitmask |= 2; // East
                    else
                        bitmask |= 8; // West
                }
                else
                {
                    // North or South
                    if (direction.z > 0)
                        bitmask |= 1; // North
                    else
                        bitmask |= 4; // South
                }
            }

            return bitmask;
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
        /// Check if this wall is connected in a specific direction.
        /// </summary>
        public bool IsConnected(WallDirection direction)
        {
            int bitmask = CalculateConnectionBitmask();
            return (bitmask & (int)direction) != 0;
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
