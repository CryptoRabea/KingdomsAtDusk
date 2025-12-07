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

        // Static registry of all walls - NO GRID, just a list
        private static List<WallConnectionSystem> allWalls = new List<WallConnectionSystem>();

        // Instance data
        private List<WallConnectionSystem> connectedWalls = new List<WallConnectionSystem>();
        private Building buildingComponent;
        private bool isRegistered = false;

        //  FIX: Prevent cascading updates
        private bool isUpdating = false;
        private static bool isBatchUpdate = false;

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

            //  FIX: Delay initial update to avoid Start() race conditions
            Invoke(nameof(DelayedInitialUpdate), 0.1f);
        }

        private void DelayedInitialUpdate()
        {
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
            UpdateNearbyWalls();
        }

        #region Registration

        private void RegisterWall()
        {
            if (!isRegistered && !allWalls.Contains(this))
            {
                allWalls.Add(this);
                isRegistered = true;
            }
        }

        private void UnregisterWall()
        {
            if (isRegistered && allWalls.Contains(this))
            {
                allWalls.Remove(this);
                isRegistered = false;
            }
        }

        #endregion

        #region Connection Detection

        /// <summary>
        /// Update this wall's connections based on nearby walls - FREE PLACEMENT, NO GRID
        /// </summary>
        public void UpdateConnections()
        {
            if (!enableConnections) return;

            //  FIX: Prevent recursive updates
            if (isUpdating) return;
            isUpdating = true;

            try
            {
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

                // Update visual based on connections (implement your visual logic here)
                // UpdateVisualMesh();
            }
            finally
            {
                isUpdating = false;
            }
        }

        /// <summary>
        /// Update all nearby walls' connections - with protection against cascading
        /// </summary>
        private void UpdateNearbyWalls()
        {
            //  FIX: Prevent cascading updates during batch operations
            if (isBatchUpdate) return;

            Vector3 myPos = transform.position;
            List<WallConnectionSystem> wallsToUpdate = new List<WallConnectionSystem>();

            // Collect walls to update
            foreach (var wall in allWalls)
            {
                if (wall == this || wall == null) continue;

                float distance = Vector3.Distance(myPos, wall.transform.position);
                if (distance <= connectionDistance * 2f)
                {
                    wallsToUpdate.Add(wall);
                }
            }

            //  FIX: Update in batch mode to prevent cascading
            isBatchUpdate = true;
            try
            {
                foreach (var wall in wallsToUpdate)
                {
                    if (wall != null)
                    {
                        wall.UpdateConnections();
                    }
                }
            }
            finally
            {
                isBatchUpdate = false;
            }
        }

        #endregion

        #region Visual Updates

        /// <summary>
        /// Update visual mesh - activates/deactivates mesh variants based on connections
        /// Override this in derived classes or implement your visual logic
        /// </summary>
        private void UpdateVisualMesh()
        {
            // TODO: Implement your visual mesh update logic here
            // Example: Activate/deactivate mesh variants based on connection count
            // Example: Rotate or scale based on connection directions
        }

        #endregion

        #region Event Handlers

        private void OnBuildingPlaced(BuildingPlacedEvent evt)
        {
            //  FIX: Only process if this is a wall and it's nearby
            if (evt.Building == null) return;

            var wallSystem = evt.Building.GetComponent<WallConnectionSystem>();
            if (wallSystem == null) return;

            // Check if nearby
            float distance = Vector3.Distance(transform.position, evt.Position);
            if (distance <= connectionDistance * 2f)
            {
                //  FIX: Use delayed update to prevent immediate cascade
                Invoke(nameof(UpdateConnections), 0.05f);
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
            foreach (var wall in connectedWalls)
            {
                if (wall != null)
                {
                }
            }
        }

        [ContextMenu("Print All Walls")]
        private void DebugPrintAllWalls()
        {
            foreach (var wall in allWalls)
            {
                if (wall != null)
                {
                }
            }
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
        public List<WallConnectionSystem> GetConnectedWalls() => new List<WallConnectionSystem>(connectedWalls);

        /// <summary>
        /// Check if connected to a specific wall
        /// </summary>
        public bool IsConnectedTo(WallConnectionSystem otherWall)
        {
            return connectedWalls.Contains(otherWall);
        }

        /// <summary>
        /// Get connection direction to another wall (normalized vector)
        /// </summary>
        public Vector3 GetConnectionDirection(WallConnectionSystem otherWall)
        {
            if (otherWall == null) return Vector3.zero;
            return (otherWall.transform.position - transform.position).normalized;
        }

        /// <summary>
        /// Static method to get all walls in the scene
        /// </summary>
        public static List<WallConnectionSystem> GetAllWalls()
        {
            return new List<WallConnectionSystem>(allWalls);
        }

        /// <summary>
        /// Static method to clear all wall registrations (useful for scene transitions)
        /// </summary>
        public static void ClearAllWalls()
        {
            allWalls.Clear();
        }

        #endregion
    }
}