using UnityEngine;
using RTS.Core.Events;
using RTS.Units;
using System.Linq;
using RTS.Core.Services;

namespace RTS.Core.Conditions
{
    /// <summary>
    /// Defeat condition: All player units are dead and cannot train more
    /// </summary>
    public class AllUnitsDeadDefeat : DefeatCondition
    {
        [Header("Settings")]
        [SerializeField] private LayerMask playerUnitLayer;
        [SerializeField] private float checkInterval = 2f; // Check every 2 seconds

        private float checkTimer = 0f;
        private bool allUnitsDead = false;
        private IResourcesService resourcesService;

        public override bool IsFailed => allUnitsDead;

        public override void Initialize()
        {
            allUnitsDead = false;
            checkTimer = 0f;
            resourcesService = ServiceLocator.Get<IResourcesService>();
        }

        public override void Cleanup()
        {
            // Nothing to clean up
        }

        public override string GetStatusText()
        {
            if (allUnitsDead)
                return "ALL UNITS DEFEATED!";

            int unitCount = CountPlayerUnits();
            return $"Player Units: {unitCount}";
        }

        private void Update()
        {
            if (allUnitsDead) return;

            checkTimer += Time.deltaTime;
            if (checkTimer >= checkInterval)
            {
                checkTimer = 0f;
                CheckUnitsStatus();
            }
        }

        private void CheckUnitsStatus()
        {
            int aliveUnits = CountPlayerUnits();

            if (aliveUnits == 0)
            {
                // Check if player can train more units
                if (!CanTrainMoreUnits())
                {
                    allUnitsDead = true;
                    Debug.Log("DEFEAT: All units dead and cannot train more!");
                }
            }
        }

        private int CountPlayerUnits()
        {
            // Find all units with UnitHealth component on player layer
            var allUnits = GameObject.FindObjectsOfType<UnitHealth>();
            return allUnits.Count(unit =>
                unit != null &&
                !unit.IsDead &&
                ((1 << unit.gameObject.layer) & playerUnitLayer) != 0
            );
        }

        private bool CanTrainMoreUnits()
        {
            // Check if player has resources to train at least one basic unit
            // Assuming basic units cost around 50 food and 25 gold
            if (resourcesService != null)
            {
                return resourcesService.Food >= 50 && resourcesService.Gold >= 25;
            }

            return false;
        }
    }
}
