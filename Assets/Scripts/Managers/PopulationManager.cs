using UnityEngine;
using RTS.Core.Services;
using RTS.Core.Events;

namespace RTS.Managers
{
    /// <summary>
    /// Manages population and peasant allocation system.
    /// Tracks total population, housing capacity, and available/assigned peasants.
    /// </summary>
    public class PopulationManager : MonoBehaviour, IPopulationService
    {
        [Header("Population Settings")]
        [SerializeField] private int startingPopulation = 10;
        [SerializeField] private int baseHousingCapacity = 20;
        [SerializeField] private float updateInterval = 1f;

        [Header("Population Growth (Optional)")]
        [SerializeField] private bool enableNaturalGrowth = false;
        [SerializeField] private float growthRate = 0.1f; // Peasants per second at 100% happiness
        [SerializeField] private float minimumHappinessForGrowth = 50f;

        private int totalPopulation;
        private int assignedPeasants;
        private int housingCapacity;
        private float updateTimer;
        private float growthAccumulator;

        // Properties implementing IPopulationService
        public int TotalPopulation => totalPopulation;
        public int AvailablePeasants => Mathf.Max(0, totalPopulation - assignedPeasants);
        public int AssignedPeasants => assignedPeasants;
        public int HousingCapacity => housingCapacity;

        private void Awake()
        {
            totalPopulation = startingPopulation;
            housingCapacity = baseHousingCapacity;
            assignedPeasants = 0;

            PublishPopulationEvent();
        }

        private void Update()
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0f;
                UpdatePopulationGrowth();
            }
        }

        #region IPopulationService Implementation

        public void AddPopulation(int amount)
        {
            if (amount <= 0) return;

            totalPopulation += amount;
            // Cap by housing capacity
            totalPopulation = Mathf.Min(totalPopulation, housingCapacity);

            PublishPopulationEvent();
        }

        public void RemovePopulation(int amount)
        {
            if (amount <= 0) return;

            totalPopulation = Mathf.Max(0, totalPopulation - amount);
            // Ensure assigned peasants don't exceed total
            assignedPeasants = Mathf.Min(assignedPeasants, totalPopulation);

            PublishPopulationEvent();
        }

        public void UpdateHousingCapacity(int capacity)
        {
            housingCapacity = capacity;
            // Cap population by new capacity
            totalPopulation = Mathf.Min(totalPopulation, housingCapacity);

            PublishPopulationEvent();
        }

        public bool TryAssignPeasants(int amount, string workType, GameObject assignedTo)
        {
            if (amount <= 0) return true;
            if (AvailablePeasants < amount) return false;

            assignedPeasants += amount;
            EventBus.Publish(new PeasantAssignedEvent(workType, amount, assignedTo));
            PublishPopulationEvent();

            return true;
        }

        public void ReleasePeasants(int amount, string workType, GameObject releasedFrom)
        {
            if (amount <= 0) return;

            assignedPeasants = Mathf.Max(0, assignedPeasants - amount);
            EventBus.Publish(new PeasantReleasedEvent(workType, amount, releasedFrom));
            PublishPopulationEvent();
        }

        #endregion

        private void UpdatePopulationGrowth()
        {
            if (!enableNaturalGrowth) return;
            if (totalPopulation >= housingCapacity) return;

            // Get happiness service
            var happinessService = ServiceLocator.TryGet<IHappinessService>();
            if (happinessService == null) return;

            float happiness = happinessService.CurrentHappiness;
            if (happiness < minimumHappinessForGrowth) return;

            // Calculate growth based on happiness
            float happinessMultiplier = happiness / 100f;
            growthAccumulator += growthRate * happinessMultiplier * updateInterval;

            // Add population when accumulator reaches 1.0
            if (growthAccumulator >= 1f)
            {
                int newPeasants = Mathf.FloorToInt(growthAccumulator);
                growthAccumulator -= newPeasants;
                AddPopulation(newPeasants);
            }
        }

        private void PublishPopulationEvent()
        {
            EventBus.Publish(new PopulationChangedEvent(
                totalPopulation,
                AvailablePeasants,
                assignedPeasants,
                housingCapacity
            ));
        }

        #region Debug Methods

        [ContextMenu("Add 5 Peasants")]
        private void DebugAddPeasants()
        {
            AddPopulation(5);
        }

        [ContextMenu("Remove 5 Peasants")]
        private void DebugRemovePeasants()
        {
            RemovePopulation(5);
        }

        [ContextMenu("Increase Housing +10")]
        private void DebugIncreaseHousing()
        {
            UpdateHousingCapacity(housingCapacity + 10);
        }

        #endregion
    }
}
