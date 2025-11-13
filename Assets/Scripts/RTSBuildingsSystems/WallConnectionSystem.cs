using UnityEngine;
using System.Collections.Generic;
using RTS.Core.Events;

namespace RTS.Buildings
{
    /// <summary>
    /// Simplified centralized wall system - Stronghold Crusader style.
    /// Uses single wall prefab with automatic rotation and connection detection.
    /// Walls can be dragged to create long segments and upgraded to towers.
    /// </summary>
    public class WallConnectionSystem : MonoBehaviour
    {
        [Header("Wall Settings")]
        [SerializeField] private float gridSize = 1f;
        [Tooltip("Should this wall connect to other walls?")]
        [SerializeField] private bool enableConnections = true;

        [Header("Wall Visual")]
        [Tooltip("The main wall mesh object that will be rotated")]
        [SerializeField] private GameObject wallMesh;

        [Header("Collider Settings")]
        [Tooltip("Wall collider for selection and gameplay")]
        [SerializeField] private BoxCollider wallCollider;
        [Tooltip("Auto-create collider if not assigned")]
        [SerializeField] private bool autoCreateCollider = true;

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
        private WallType currentWallType = WallType.Straight;

        private void Awake()
        {
            buildingComponent = GetComponent<Building>();

            // Auto-find wall mesh if not assigned
            if (wallMesh == null)
            {
                Transform meshTransform = transform.Find("WallMesh");
                if (meshTransform != null)
                {
                    wallMesh = meshTransform.gameObject;
                }
            }

            // Setup collider
            SetupCollider();
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
                wallCollider.size = new Vector3(gridSize, 2f, 0.5f);
            }
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
        /// Update the visual mesh based on connection state using rotation.
        /// Simplified system - one mesh, automatic rotation based on connections.
        /// </summary>
        private void UpdateVisual()
        {
            if (wallMesh == null)
            {
                Debug.LogWarning($"Wall at {gridPosition}: wallMesh is not assigned!");
                return;
            }

            // Determine wall type and rotation based on connections
            DetermineWallTypeAndRotation();

            Debug.Log($"Wall at {gridPosition} updated: Type={currentWallType}, Connections={GetConnectionDebugString()}");
        }

        /// <summary>
        /// Determine wall type (straight, corner, T-junction, cross) and apply rotation
        /// </summary>
        private void DetermineWallTypeAndRotation()
        {
            int connectionCount = CountConnections();

            switch (connectionCount)
            {
                case 0:
                    // Standalone wall - no rotation needed
                    currentWallType = WallType.Standalone;
                    wallMesh.transform.localRotation = Quaternion.identity;
                    break;

                case 1:
                    // End piece - rotate to face connection
                    currentWallType = WallType.End;
                    RotateToSingleConnection();
                    break;

                case 2:
                    // Either straight or corner
                    if (IsOppositeConnections())
                    {
                        currentWallType = WallType.Straight;
                        RotateForStraightWall();
                    }
                    else
                    {
                        currentWallType = WallType.Corner;
                        RotateForCorner();
                    }
                    break;

                case 3:
                    // T-junction
                    currentWallType = WallType.TJunction;
                    RotateForTJunction();
                    break;

                case 4:
                    // Cross/intersection
                    currentWallType = WallType.Cross;
                    wallMesh.transform.localRotation = Quaternion.identity;
                    break;
            }
        }

        private int CountConnections()
        {
            int count = 0;
            if ((connectionState & NORTH) != 0) count++;
            if ((connectionState & EAST) != 0) count++;
            if ((connectionState & SOUTH) != 0) count++;
            if ((connectionState & WEST) != 0) count++;
            return count;
        }

        private bool IsOppositeConnections()
        {
            bool northSouth = ((connectionState & NORTH) != 0) && ((connectionState & SOUTH) != 0);
            bool eastWest = ((connectionState & EAST) != 0) && ((connectionState & WEST) != 0);
            return northSouth || eastWest;
        }

        private void RotateToSingleConnection()
        {
            if ((connectionState & NORTH) != 0)
                wallMesh.transform.localRotation = Quaternion.Euler(0, 0, 0);
            else if ((connectionState & EAST) != 0)
                wallMesh.transform.localRotation = Quaternion.Euler(0, 90, 0);
            else if ((connectionState & SOUTH) != 0)
                wallMesh.transform.localRotation = Quaternion.Euler(0, 180, 0);
            else if ((connectionState & WEST) != 0)
                wallMesh.transform.localRotation = Quaternion.Euler(0, 270, 0);
        }

        private void RotateForStraightWall()
        {
            // North-South orientation
            if (((connectionState & NORTH) != 0) && ((connectionState & SOUTH) != 0))
            {
                wallMesh.transform.localRotation = Quaternion.Euler(0, 0, 0);
            }
            // East-West orientation
            else if (((connectionState & EAST) != 0) && ((connectionState & WEST) != 0))
            {
                wallMesh.transform.localRotation = Quaternion.Euler(0, 90, 0);
            }
        }

        private void RotateForCorner()
        {
            // North-East corner
            if (((connectionState & NORTH) != 0) && ((connectionState & EAST) != 0))
                wallMesh.transform.localRotation = Quaternion.Euler(0, 0, 0);
            // East-South corner
            else if (((connectionState & EAST) != 0) && ((connectionState & SOUTH) != 0))
                wallMesh.transform.localRotation = Quaternion.Euler(0, 90, 0);
            // South-West corner
            else if (((connectionState & SOUTH) != 0) && ((connectionState & WEST) != 0))
                wallMesh.transform.localRotation = Quaternion.Euler(0, 180, 0);
            // West-North corner
            else if (((connectionState & WEST) != 0) && ((connectionState & NORTH) != 0))
                wallMesh.transform.localRotation = Quaternion.Euler(0, 270, 0);
        }

        private void RotateForTJunction()
        {
            // Missing North (connected E, S, W)
            if ((connectionState & NORTH) == 0)
                wallMesh.transform.localRotation = Quaternion.Euler(0, 180, 0);
            // Missing East (connected N, S, W)
            else if ((connectionState & EAST) == 0)
                wallMesh.transform.localRotation = Quaternion.Euler(0, 270, 0);
            // Missing South (connected N, E, W)
            else if ((connectionState & SOUTH) == 0)
                wallMesh.transform.localRotation = Quaternion.Euler(0, 0, 0);
            // Missing West (connected N, E, S)
            else if ((connectionState & WEST) == 0)
                wallMesh.transform.localRotation = Quaternion.Euler(0, 90, 0);
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

        /// <summary>
        /// Get the current wall type (straight, corner, etc.)
        /// </summary>
        public WallType GetWallType() => currentWallType;

        /// <summary>
        /// Manual rotation of wall mesh (for player adjustment)
        /// </summary>
        public void RotateWall(float yRotation)
        {
            if (wallMesh != null)
            {
                wallMesh.transform.Rotate(0, yRotation, 0);
            }
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

    /// <summary>
    /// Wall types based on connection patterns.
    /// </summary>
    public enum WallType
    {
        Standalone,  // No connections
        End,         // 1 connection
        Straight,    // 2 opposite connections
        Corner,      // 2 adjacent connections
        TJunction,   // 3 connections
        Cross        // 4 connections
    }
}
