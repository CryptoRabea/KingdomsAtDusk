using System;
using UnityEngine;
using RTS.Core.Events;
using RTS.Buildings;

namespace RTS.Core.Conditions
{
    /// <summary>
    /// Defeat condition: Player's main stronghold is destroyed
    /// </summary>
    public class StrongholdDestroyedDefeat : DefeatCondition
    {
        [Header("Stronghold Settings")]
        [SerializeField] private GameObject stronghold;
        [SerializeField] private string strongholdBuildingName = "Stronghold";

        private bool strongholdDestroyed = false;
        private Action<BuildingDestroyedEvent> buildingDestroyedHandler;

        public override bool IsFailed => strongholdDestroyed;

        public override void Initialize()
        {
            strongholdDestroyed = false;

            // If no stronghold is assigned, try to find it
            if (stronghold == null)
            {
                FindStronghold();
            }

            buildingDestroyedHandler = OnBuildingDestroyed;
            EventBus.Subscribe(buildingDestroyedHandler);
        }

        public override void Cleanup()
        {
            if (buildingDestroyedHandler != null)
            {
                EventBus.Unsubscribe(buildingDestroyedHandler);
            }
        }

        public override string GetStatusText()
        {
            if (strongholdDestroyed)
                return "STRONGHOLD DESTROYED!";
            else if (stronghold != null)
                return "Stronghold Intact";
            else
                return "No Stronghold Found";
        }

        private void FindStronghold()
        {
            // Try to find stronghold by name
            var buildings = FindObjectsByType<Building>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var building in buildings)
            {
                if (building.BuildingData != null &&
                    building.BuildingData.buildingName.Contains(strongholdBuildingName))
                {
                    stronghold = building.gameObject;
                    Debug.Log($"Found stronghold: {stronghold.name}");
                    return;
                }
            }

            Debug.LogWarning("No stronghold building found! Defeat condition may not work properly.");
        }

        private void OnBuildingDestroyed(BuildingDestroyedEvent evt)
        {
            if (evt.Building == stronghold || evt.BuildingName.Contains(strongholdBuildingName))
            {
                strongholdDestroyed = true;
                Debug.Log("DEFEAT: Stronghold has been destroyed!");
            }
        }
    }
}
