using UnityEngine;
using RTS.Core.Services;
using RTS.Core.Events;

namespace RTS.Managers
{
    /// <summary>
    /// Manages citizen happiness/morale system using event-driven architecture.
    /// Separated from UI concerns - UI subscribes to events.
    /// </summary>
    public class HappinessManager : MonoBehaviour, IHappinessService
    {
        [Header("Happiness Settings")]
        [SerializeField] private float baseHappiness = 50f;
        [SerializeField] private float startingTaxLevel = 10f;

        [Header("Calculation Settings")]
        [SerializeField] private float happinessUpdateInterval = 0.5f;
        
        private float currentHappiness;
        private float taxLevel;
        private float buildingsHappinessBonus = 0f;
        private float updateTimer;

        // Properties implementing IHappinessService
        public float CurrentHappiness => currentHappiness;
        public float TaxLevel
        {
            get => taxLevel;
            set
            {
                if (Mathf.Approximately(taxLevel, value)) return;
                taxLevel = Mathf.Clamp(value, 0f, 100f);
                RecalculateHappiness();
            }
        }

        private void Awake()
        {
            taxLevel = startingTaxLevel;
            RecalculateHappiness();
        }

        private void Update()
        {
            // Update happiness periodically instead of every frame
            updateTimer += Time.deltaTime;
            if (updateTimer >= happinessUpdateInterval)
            {
                updateTimer = 0f;
                RecalculateHappiness();
            }
        }

        #region IHappinessService Implementation

        public void AddBuildingBonus(float bonus, string buildingName)
        {
            buildingsHappinessBonus += bonus;
            EventBus.Publish(new BuildingBonusChangedEvent(bonus, buildingName));
            RecalculateHappiness();
        }

        public void RemoveBuildingBonus(float bonus, string buildingName)
        {
            buildingsHappinessBonus -= bonus;
            EventBus.Publish(new BuildingBonusChangedEvent(-bonus, buildingName));
            RecalculateHappiness();
        }

        #endregion

        private void RecalculateHappiness()
        {
            float previousHappiness = currentHappiness;
            
            // Formula: Base + Buildings Bonus - Tax Penalty
            currentHappiness = baseHappiness + buildingsHappinessBonus - taxLevel;
            currentHappiness = Mathf.Clamp(currentHappiness, 0f, 100f);

            float delta = currentHappiness - previousHappiness;
            
            // Only publish if changed significantly (avoid spam)
            if (Mathf.Abs(delta) > 0.01f)
            {
                EventBus.Publish(new HappinessChangedEvent(currentHappiness, delta));
            }
        }

        #region Debug Methods

        [ContextMenu("Add 10 Happiness")]
        private void DebugAddHappiness()
        {
            AddBuildingBonus(10f, "Debug");
        }

        [ContextMenu("Remove 10 Happiness")]
        private void DebugRemoveHappiness()
        {
            RemoveBuildingBonus(10f, "Debug");
        }

        [ContextMenu("Set Tax to 50%")]
        private void DebugSetHighTax()
        {
            TaxLevel = 50f;
        }

        #endregion
    }
}
