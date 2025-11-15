using UnityEngine;
using RTS.Core.Services;
using RTS.Core.Events;

namespace RTS.Managers
{
    /// <summary>
    /// Manages kingdom reputation/fame system.
    /// Reputation affects peasant attraction to the kingdom.
    /// </summary>
    public class ReputationManager : MonoBehaviour, IReputationService
    {
        [Header("Reputation Settings")]
        [SerializeField] private float startingReputation = 50f;
        [SerializeField] private float minReputation = 0f;
        [SerializeField] private float maxReputation = 100f;

        [Header("Reputation Effects")]
        [Tooltip("Should reputation affect peasant population?")]
        [SerializeField] private bool affectsPeasantAttraction = true;
        [SerializeField] private float reputationUpdateInterval = 2f;

        private float currentReputation;
        private float updateTimer;

        // Properties implementing IReputationService
        public float CurrentReputation => currentReputation;

        private void Awake()
        {
            currentReputation = startingReputation;
            PublishReputationEvent(0f, "Initial");
        }

        private void Update()
        {
            if (!affectsPeasantAttraction) return;

            updateTimer += Time.deltaTime;
            if (updateTimer >= reputationUpdateInterval)
            {
                updateTimer = 0f;
                UpdateReputationEffects();
            }
        }

        #region IReputationService Implementation

        public void ModifyReputation(float amount, string reason)
        {
            if (Mathf.Approximately(amount, 0f)) return;

            float previousReputation = currentReputation;
            currentReputation = Mathf.Clamp(currentReputation + amount, minReputation, maxReputation);

            float actualDelta = currentReputation - previousReputation;
            if (Mathf.Abs(actualDelta) > 0.01f)
            {
                PublishReputationEvent(actualDelta, reason);
            }
        }

        #endregion

        private void UpdateReputationEffects()
        {
            if (!affectsPeasantAttraction) return;

            var populationService = ServiceLocator.TryGet<IPopulationService>();
            if (populationService == null) return;

            // High reputation attracts peasants
            if (currentReputation >= 75f)
            {
                // Small chance to attract a peasant
                if (Random.value < 0.1f) // 10% chance per interval
                {
                    populationService.AddPopulation(1);
                }
            }
            // Low reputation causes peasants to leave
            else if (currentReputation <= 25f)
            {
                // Small chance to lose a peasant
                if (Random.value < 0.05f) // 5% chance per interval
                {
                    populationService.RemovePopulation(1);
                }
            }
        }

        private void PublishReputationEvent(float delta, string reason)
        {
            EventBus.Publish(new ReputationChangedEvent(currentReputation, delta, reason));
        }

        #region Debug Methods

        [ContextMenu("Add 10 Reputation")]
        private void DebugAddReputation()
        {
            ModifyReputation(10f, "Debug Increase");
        }

        [ContextMenu("Remove 10 Reputation")]
        private void DebugRemoveReputation()
        {
            ModifyReputation(-10f, "Debug Decrease");
        }

        [ContextMenu("Set Max Reputation")]
        private void DebugMaxReputation()
        {
            ModifyReputation(maxReputation - currentReputation, "Debug Max");
        }

        #endregion
    }
}
