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
        [SerializeField] private float gridSize = 1f;
        [Tooltip("Should this wall connect to other walls?")]
        [SerializeField] private bool enableConnections = true;

        [Header("Visual Variants")]
        [Tooltip("Assign 16 mesh variants (index = connection bitmask). North=1, East=2, South=4, West=8")]
        [SerializeField] private GameObject[] meshVariants = new GameObject[16];

        // Connection state bits
        private const int NORTH = 1;  // 0001
        private const int EAST = 2;   // 0010
        private const int SOUTH = 4;  // 0100
        private const int WEST = 8;   // 1000

        // Static registry of all walls
        private static Dictionary<Vector2Int, WallConnectionSystem> wallRegistry = new Dictionary<Vector2Int, WallConnectionSystem>();

        // Instance data
        private Vector2Int gridPosition;
        private int connectionState = 0;
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
            RegisterWall();

            // Subscribe to building events
            EventBus.Subscribe<BuildingPlacedEvent>(OnBuildingPlaced);
            EventBus.Subscribe<BuildingDestroyedEvent>(OnBuildingDestroyed);

            // Initial update
            UpdateConnections();
        }

        private void OnDestroy()
        {
            // Unregister this wall
            UnregisterWall();

            // Unsubscribe from events
            EventBus.Unsubscribe<BuildingPlacedEvent>(OnBuildingPlaced);
            EventBus.Unsubscribe<BuildingDestroyedEvent>(OnBuildingDestroyed);

            // Update neighbors when this wall is destroyed
            UpdateNeighborConnections();
        }

        #region Wall Registration

        private void RegisterWall()
        {
            if (isRegistered) return;

            gridPosition = WorldToGrid(transform.position);

            if (wallRegistry.ContainsKey(gridPosition))
            {
                Debug.LogWarning($"Wall already exists at grid position {gridPosition}! Replacing...");
                wallRegistry[gridPosition] = this;
            }
            else
            {
                wallRegistry.Add(gridPosition, this);
            }

            isRegistered = true;
            Debug.Log($"Wall registered at grid position {gridPosition}");
        }

        private void UnregisterWall()
        {
            if (!isRegistered) return;

            if (wallRegistry.ContainsKey(gridPosition))
            {
                wallRegistry.Remove(gridPosition);
                Debug.Log($"Wall unregistered from grid position {gridPosition}");
            }

            isRegistered = false;
        }

        #endregion

        #region Grid Conversion

        private Vector2Int WorldToGrid(Vector3 worldPosition)
        {
            int x = Mathf.RoundToInt(worldPosition.x / gridSize);
            int z = Mathf.RoundToInt(worldPosition.z / gridSize);
            return new Vector2Int(x, z);
        }

        private Vector3 GridToWorld(Vector2Int gridPos)
        {
            return new Vector3(gridPos.x * gridSize, 0, gridPos.y * gridSize);
        }

        #endregion

        #region Connection Detection

        /// <summary>
        /// Update this wall's connections based on neighbors.
        /// </summary>
        public void UpdateConnections()
        {
            if (!enableConnections) return;

            int newConnectionState = 0;

            // Check all 4 directions
            if (HasWallAt(gridPosition + Vector2Int.up))    newConnectionState |= NORTH;
            if (HasWallAt(gridPosition + Vector2Int.right)) newConnectionState |= EAST;
            if (HasWallAt(gridPosition + Vector2Int.down))  newConnectionState |= SOUTH;
            if (HasWallAt(gridPosition + Vector2Int.left))  newConnectionState |= WEST;

            // Only update visual if state changed
            if (newConnectionState != connectionState)
            {
                connectionState = newConnectionState;
                UpdateVisual();
                Debug.Log($"Wall at {gridPosition} updated: connections = {GetConnectionDebugString()}");
            }
        }

        /// <summary>
        /// Check if there's a wall at the given grid position.
        /// </summary>
        private bool HasWallAt(Vector2Int gridPos)
        {
            return wallRegistry.ContainsKey(gridPos) && wallRegistry[gridPos] != null;
        }

        /// <summary>
        /// Update all neighboring walls' connections.
        /// </summary>
        private void UpdateNeighborConnections()
        {
            Vector2Int[] neighbors = new Vector2Int[]
            {
                gridPosition + Vector2Int.up,    // North
                gridPosition + Vector2Int.right, // East
                gridPosition + Vector2Int.down,  // South
                gridPosition + Vector2Int.left   // West
            };

            foreach (var neighborPos in neighbors)
            {
                if (wallRegistry.TryGetValue(neighborPos, out WallConnectionSystem neighbor))
                {
                    if (neighbor != null)
                    {
                        neighbor.UpdateConnections();
                    }
                }
            }
        }

        #endregion

        #region Visual Updates

        /// <summary>
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
            // Check if the placed building is a wall
            if (evt.Building == null) return;

            var wallSystem = evt.Building.GetComponent<WallConnectionSystem>();
            if (wallSystem != null)
            {
                // Update this wall if the new wall is adjacent
                Vector2Int placedGridPos = WorldToGrid(evt.Position);

                if (IsAdjacent(gridPosition, placedGridPos))
                {
                    UpdateConnections();
                }
            }
        }

        private void OnBuildingDestroyed(BuildingDestroyedEvent evt)
        {
            // When any building is destroyed, nearby walls might need updating
            // This is handled in OnDestroy() which calls UpdateNeighborConnections()
        }

        private bool IsAdjacent(Vector2Int pos1, Vector2Int pos2)
        {
            int dx = Mathf.Abs(pos1.x - pos2.x);
            int dy = Mathf.Abs(pos1.y - pos2.y);
            return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
        }

        #endregion

        #region Debug Helpers

        private string GetConnectionDebugString()
        {
            string result = "";
            if ((connectionState & NORTH) != 0) result += "N";
            if ((connectionState & EAST) != 0) result += "E";
            if ((connectionState & SOUTH) != 0) result += "S";
            if ((connectionState & WEST) != 0) result += "W";
            return result.Length > 0 ? result : "None";
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !enableConnections) return;

            // Draw grid position
            Gizmos.color = Color.cyan;
            Vector3 worldPos = GridToWorld(gridPosition);
            Gizmos.DrawWireSphere(worldPos + Vector3.up * 0.5f, 0.2f);

            // Draw connection lines to neighbors
            Gizmos.color = Color.green;
            if ((connectionState & NORTH) != 0)
                Gizmos.DrawLine(transform.position, transform.position + Vector3.forward * gridSize);
            if ((connectionState & EAST) != 0)
                Gizmos.DrawLine(transform.position, transform.position + Vector3.right * gridSize);
            if ((connectionState & SOUTH) != 0)
                Gizmos.DrawLine(transform.position, transform.position + Vector3.back * gridSize);
            if ((connectionState & WEST) != 0)
                Gizmos.DrawLine(transform.position, transform.position + Vector3.left * gridSize);
        }

        [ContextMenu("Force Update Connections")]
        private void DebugForceUpdate()
        {
            UpdateConnections();
        }

        [ContextMenu("Print Connection State")]
        private void DebugPrintState()
        {
            Debug.Log($"Wall at {gridPosition}: State={connectionState}, Connections={GetConnectionDebugString()}");
        }

        [ContextMenu("Print All Walls")]
        private void DebugPrintAllWalls()
        {
            Debug.Log($"Total walls registered: {wallRegistry.Count}");
            foreach (var kvp in wallRegistry)
            {
                Debug.Log($"  {kvp.Key} -> {(kvp.Value != null ? "Active" : "Null")}");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get the current connection state bitmask.
        /// </summary>
        public int GetConnectionState() => connectionState;

        /// <summary>
        /// Get the grid position of this wall.
        /// </summary>
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
