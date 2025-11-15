using UnityEngine;
using RTS.Core.Events;
using RTS.Core.Services;
using System.Collections.Generic;

namespace RTS.Buildings
{
    /// <summary>
    /// Campfire building component - manages peasant gathering mechanics.
    /// Requires Building component to be attached.
    /// Peasants gather based on happiness, reputation, housing, and military strength.
    /// </summary>
    [RequireComponent(typeof(Building))]
    public class Campfire : MonoBehaviour
    {
        [Header("Campfire Configuration")]
        [SerializeField] private CampfireDataSO campfireData;

        [Header("Visual Feedback (Optional)")]
        [SerializeField] private Transform peasantContainer; // Parent for spawned peasant visuals
        [SerializeField] private ParticleSystem fireEffect; // Campfire particle effect

        private Building building;
        private IPopulationService populationService;
        private IHappinessService happinessService;
        private IReputationService reputationService;

        private int currentPeasantCount = 0;
        private float gatherUpdateTimer = 0f;
        private float previousHappinessBonus = 0f;
        private float previousReputationBonus = 0f;

        private List<GameObject> spawnedPeasantVisuals = new List<GameObject>();

        public CampfireDataSO Data => campfireData;
        public int CurrentPeasantCount => currentPeasantCount;
        public int MaxCapacity => campfireData != null ? campfireData.maxPeasantCapacity : 0;

        private void Awake()
        {
            building = GetComponent<Building>();

            if (campfireData == null)
            {
                Debug.LogWarning($"Campfire on {gameObject.name} has no CampfireDataSO assigned!");
            }

            // Create peasant container if not assigned
            if (peasantContainer == null)
            {
                GameObject container = new GameObject("PeasantVisuals");
                container.transform.SetParent(transform);
                container.transform.localPosition = Vector3.zero;
                peasantContainer = container.transform;
            }
        }

        private void Start()
        {
            // Get services
            populationService = ServiceLocator.TryGet<IPopulationService>();
            happinessService = ServiceLocator.TryGet<IHappinessService>();
            reputationService = ServiceLocator.TryGet<IReputationService>();

            // Subscribe to building completed event
            EventBus.Subscribe<BuildingCompletedEvent>(OnBuildingCompleted);
        }

        private void Update()
        {
            // Only update if building is constructed
            if (building == null || !building.IsConstructed)
            {
                return;
            }

            gatherUpdateTimer += Time.deltaTime;

            if (gatherUpdateTimer >= (campfireData?.gatherUpdateInterval ?? 2f))
            {
                gatherUpdateTimer = 0f;
                UpdatePeasantGathering();
            }
        }

        private void OnBuildingCompleted(BuildingCompletedEvent evt)
        {
            // Start campfire effects when this building is completed
            if (evt.Building == gameObject)
            {
                StartCampfire();
            }
        }

        private void StartCampfire()
        {
            // Start fire effect
            if (fireEffect != null && !fireEffect.isPlaying)
            {
                fireEffect.Play();
            }

            Debug.Log($"🔥 Campfire activated at {transform.position}");
        }

        private void UpdatePeasantGathering()
        {
            if (campfireData == null) return;

            // Get current game state factors
            float happiness = happinessService?.CurrentHappiness ?? 50f;
            float reputation = reputationService?.CurrentReputation ?? 50f;

            // Calculate housing utilization (0-1)
            float housingUtilization = 0.5f; // Default
            if (populationService != null && populationService.HousingCapacity > 0)
            {
                housingUtilization = (float)populationService.TotalPopulation / populationService.HousingCapacity;
            }

            // Calculate military strength (placeholder - could be extended with actual unit counting)
            float militaryStrength = CalculateMilitaryStrength();

            // Calculate ideal peasant count based on factors
            int idealPeasantCount = campfireData.CalculateIdealPeasantCount(
                happiness,
                reputation,
                housingUtilization,
                militaryStrength
            );

            // Cap by available peasants
            if (populationService != null)
            {
                idealPeasantCount = Mathf.Min(idealPeasantCount, populationService.AvailablePeasants + currentPeasantCount);
            }

            // Update peasant count gradually
            if (idealPeasantCount != currentPeasantCount)
            {
                SetPeasantCount(idealPeasantCount);
            }
        }

        private float CalculateMilitaryStrength()
        {
            // Count military units (this is a simplified version)
            // Could be extended to query a UnitManager or count units with specific tags
            GameObject[] allyUnits = GameObject.FindGameObjectsWithTag("AllyUnit");
            int unitCount = allyUnits != null ? allyUnits.Length : 0;

            // Normalize to 0-1 range (assuming 10+ units = max strength)
            return Mathf.Clamp01(unitCount / 10f);
        }

        private void SetPeasantCount(int newCount)
        {
            if (newCount == currentPeasantCount) return;

            int previousCount = currentPeasantCount;
            currentPeasantCount = Mathf.Clamp(newCount, 0, campfireData.maxPeasantCapacity);

            // Update bonuses
            UpdateBonuses();

            // Update visuals
            UpdatePeasantVisuals();

            // Publish event
            float happinessMultiplier = CalculateHappinessMultiplier();
            EventBus.Publish(new CampfireGatheringChangedEvent(gameObject, currentPeasantCount, happinessMultiplier));

            Debug.Log($"🔥 Campfire peasants: {previousCount} → {currentPeasantCount}");
        }

        private void UpdateBonuses()
        {
            if (campfireData == null) return;

            // Calculate new bonuses
            float newHappinessBonus = currentPeasantCount * campfireData.happinessBonusPerPeasant;
            float newReputationBonus = currentPeasantCount * campfireData.reputationBonusPerPeasant;

            // Update happiness bonus
            if (happinessService != null && !Mathf.Approximately(newHappinessBonus, previousHappinessBonus))
            {
                float delta = newHappinessBonus - previousHappinessBonus;
                if (delta > 0)
                {
                    happinessService.AddBuildingBonus(delta, $"Campfire ({currentPeasantCount} peasants)");
                }
                else
                {
                    happinessService.RemoveBuildingBonus(-delta, $"Campfire ({currentPeasantCount} peasants)");
                }
                previousHappinessBonus = newHappinessBonus;
            }

            // Update reputation bonus
            if (reputationService != null && !Mathf.Approximately(newReputationBonus, previousReputationBonus))
            {
                float delta = newReputationBonus - previousReputationBonus;
                reputationService.ModifyReputation(delta, $"Campfire gathering ({currentPeasantCount} peasants)");
                previousReputationBonus = newReputationBonus;
            }
        }

        private void UpdatePeasantVisuals()
        {
            if (campfireData == null || campfireData.peasantVisualPrefab == null) return;

            // Add visuals if needed
            while (spawnedPeasantVisuals.Count < currentPeasantCount)
            {
                int index = spawnedPeasantVisuals.Count;
                Vector3 position = GetPeasantPosition(index);

                GameObject peasantVisual = Instantiate(
                    campfireData.peasantVisualPrefab,
                    peasantContainer
                );
                peasantVisual.transform.localPosition = position;

                spawnedPeasantVisuals.Add(peasantVisual);
            }

            // Remove visuals if needed
            while (spawnedPeasantVisuals.Count > currentPeasantCount)
            {
                int lastIndex = spawnedPeasantVisuals.Count - 1;
                GameObject toRemove = spawnedPeasantVisuals[lastIndex];
                spawnedPeasantVisuals.RemoveAt(lastIndex);

                if (toRemove != null)
                {
                    Destroy(toRemove);
                }
            }
        }

        private Vector3 GetPeasantPosition(int index)
        {
            if (campfireData == null || campfireData.peasantGatherPositions.Length == 0)
            {
                // Generate circular positions
                float angle = (index * 360f / Mathf.Max(1, currentPeasantCount)) * Mathf.Deg2Rad;
                float radius = 3f;
                return new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            }

            // Use predefined positions with wraparound
            return campfireData.peasantGatherPositions[index % campfireData.peasantGatherPositions.Length];
        }

        private float CalculateHappinessMultiplier()
        {
            if (campfireData == null || campfireData.maxPeasantCapacity == 0) return 1f;
            return 1f + ((float)currentPeasantCount / campfireData.maxPeasantCapacity);
        }

        private void OnDestroy()
        {
            // Remove bonuses
            if (happinessService != null && previousHappinessBonus > 0)
            {
                happinessService.RemoveBuildingBonus(previousHappinessBonus, "Campfire");
            }

            // Clean up visuals
            foreach (var visual in spawnedPeasantVisuals)
            {
                if (visual != null)
                {
                    Destroy(visual);
                }
            }
            spawnedPeasantVisuals.Clear();

            // Unsubscribe from events
            EventBus.Unsubscribe<BuildingCompletedEvent>(OnBuildingCompleted);
        }

        #region Public API

        /// <summary>
        /// Get the current gathering strength as a 0-1 value.
        /// </summary>
        public float GetGatheringStrength()
        {
            if (campfireData == null || campfireData.maxPeasantCapacity == 0) return 0f;
            return (float)currentPeasantCount / campfireData.maxPeasantCapacity;
        }

        /// <summary>
        /// Manually set peasant count (for debugging or special events).
        /// </summary>
        public void SetPeasantCountManually(int count)
        {
            SetPeasantCount(count);
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Add 5 Peasants")]
        private void DebugAddPeasants()
        {
            SetPeasantCount(currentPeasantCount + 5);
        }

        [ContextMenu("Remove 5 Peasants")]
        private void DebugRemovePeasants()
        {
            SetPeasantCount(currentPeasantCount - 5);
        }

        [ContextMenu("Fill to Max Capacity")]
        private void DebugFillCapacity()
        {
            if (campfireData != null)
            {
                SetPeasantCount(campfireData.maxPeasantCapacity);
            }
        }

        [ContextMenu("Clear All Peasants")]
        private void DebugClearPeasants()
        {
            SetPeasantCount(0);
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            if (campfireData == null) return;

            // Draw gather radius
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, campfireData.gatherRadius);

            // Draw peasant positions
            if (campfireData.peasantGatherPositions != null)
            {
                Gizmos.color = Color.yellow;
                foreach (var pos in campfireData.peasantGatherPositions)
                {
                    Gizmos.DrawWireSphere(transform.position + pos, 0.3f);
                }
            }
        }
    }
}
