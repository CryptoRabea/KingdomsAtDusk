using UnityEngine;
using System.Collections.Generic;
using RTS.Core.Events;

namespace RTS.Buildings
{
    /// <summary>
    /// Static helper class for upgrading walls to towers or gates.
    /// This is used when player selects a wall and clicks an upgrade button.
    /// </summary>
    public static class WallUpgradeHelper
    {
        /// <summary>
        /// Upgrade a wall to a tower or gate.
        /// Stores wall connection data, destroys the wall, and creates the new building with connections.
        /// </summary>
        public static GameObject UpgradeWallToBuilding(GameObject wall, BuildingDataSO newBuildingData)
        {
            if (wall == null || newBuildingData == null || newBuildingData.buildingPrefab == null)
            {
                return null;
            }

            // Check if wall can be upgraded
            var wallConnection = wall.GetComponent<WallConnectionSystem>();
            if (wallConnection != null)
            {
                int connectionCount = wallConnection.GetConnectionCount();
                // Don't upgrade corner walls (3+ connections) to prevent breaking wall networks
                if (connectionCount > 2)
                {
                    EventBus.Publish(new BuildingPlacementFailedEvent("Cannot upgrade corner walls!"));
                    return null;
                }
            }

            // Store wall data before destroying
            Vector3 position = wall.transform.position;
            Quaternion rotation = wall.transform.rotation;
            List<WallConnectionSystem> connectedWalls = null;

            if (wallConnection != null)
            {
                connectedWalls = wallConnection.GetConnectedWalls();
            }

            // Destroy the wall
            Object.Destroy(wall);

            // Create the new building
            GameObject newBuilding = Object.Instantiate(newBuildingData.buildingPrefab, position, rotation);

            // Set building data
            if (newBuilding.TryGetComponent<Building>(out var buildingComponent))
            {
                buildingComponent.SetData(newBuildingData);
            }

            // Handle tower-specific setup
            if (newBuildingData is TowerDataSO towerData)
            {
                var towerComponent = newBuilding.GetComponent<Tower>();
                if (towerComponent != null)
                {
                    towerComponent.SetTowerData(towerData);
                }
            }

            // Handle gate-specific setup
            if (newBuildingData is GateDataSO gateData)
            {
                var gateComponent = newBuilding.GetComponent<Gate>();
                if (gateComponent != null)
                {
                    gateComponent.SetGateData(gateData);
                }
            }

            // Add NavMesh obstacle if not present
            if (newBuilding.GetComponent<BuildingNavMeshObstacle>() == null)
            {
                newBuilding.AddComponent<BuildingNavMeshObstacle>();
            }

            // Transfer wall connections to the new building
            if (connectedWalls != null && connectedWalls.Count > 0)
            {
                // Add WallConnectionSystem to the new building so it maintains wall continuity
                var newWallConnection = newBuilding.GetComponent<WallConnectionSystem>();
                if (newWallConnection == null)
                {
                    newWallConnection = newBuilding.AddComponent<WallConnectionSystem>();
                }


                // Force update connections after a short delay
                var helper = newBuilding.AddComponent<DelayedConnectionUpdater>();
                helper.Initialize(newWallConnection);
            }

            // Publish event
            EventBus.Publish(new BuildingPlacedEvent(newBuilding, position));


            return newBuilding;
        }

        /// <summary>
        /// Helper component to delay connection updates.
        /// Automatically destroys itself after update.
        /// </summary>
        private class DelayedConnectionUpdater : MonoBehaviour
        {
            private WallConnectionSystem wallConnection;
            private float delay = 0.2f;
            private float timer = 0f;

            public void Initialize(WallConnectionSystem connection)
            {
                wallConnection = connection;
            }

            private void Update()
            {
                timer += Time.deltaTime;
                if (timer >= delay)
                {
                    if (wallConnection != null)
                    {
                        wallConnection.UpdateConnections();
                    }
                    Destroy(this);
                }
            }
        }
    }
}
