using UnityEngine;
using System.Collections.Generic;

namespace RTS.Buildings
{
    /// <summary>
    /// ScriptableObject containing campfire-specific configuration data.
    /// Extends BuildingDataSO with peasant gathering mechanics.
    /// Create via: Right-click in Project > Create > RTS/CampfireData
    /// </summary>
    [CreateAssetMenu(fileName = "CampfireData", menuName = "RTS/CampfireData")]
    public class CampfireDataSO : BuildingDataSO
    {
        [Header("Campfire Settings")]
        [Tooltip("Maximum number of peasants that can gather at this campfire")]
        public int maxPeasantCapacity = 20;

        [Tooltip("Base radius within which peasants will be attracted")]
        public float gatherRadius = 10f;

        [Tooltip("Time interval for peasant gathering calculations")]
        public float gatherUpdateInterval = 2f;

        [Header("Peasant Attraction Factors")]
        [Tooltip("Minimum happiness required for peasants to gather (0-100)")]
        public float minimumHappinessForGathering = 30f;

        [Tooltip("Minimum reputation required for peasants to gather (0-100)")]
        public float minimumReputationForGathering = 20f;

        [Tooltip("How much happiness affects peasant count (0-1)")]
        [Range(0f, 1f)]
        public float happinessInfluence = 0.3f;

        [Tooltip("How much housing affects peasant count (0-1)")]
        [Range(0f, 1f)]
        public float housingInfluence = 0.3f;

        [Tooltip("How much reputation affects peasant count (0-1)")]
        [Range(0f, 1f)]
        public float reputationInfluence = 0.2f;

        [Tooltip("How much military strength affects peasant count (0-1)")]
        [Range(0f, 1f)]
        public float strengthInfluence = 0.2f;

        [Header("Bonuses Per Peasant")]
        [Tooltip("Happiness bonus per peasant gathered")]
        public float happinessBonusPerPeasant = 0.1f;

        [Tooltip("Reputation bonus per peasant gathered")]
        public float reputationBonusPerPeasant = 0.05f;

        [Header("Visual Settings")]
        [Tooltip("Prefab for peasant visual representation (optional)")]
        public GameObject peasantVisualPrefab;

        [Tooltip("Positions around campfire where peasants appear")]
        public Vector3[] peasantGatherPositions = new Vector3[]
        {
            new Vector3(2, 0, 0),
            new Vector3(-2, 0, 0),
            new Vector3(0, 0, 2),
            new Vector3(0, 0, -2),
            new Vector3(1.5f, 0, 1.5f),
            new Vector3(-1.5f, 0, 1.5f),
            new Vector3(1.5f, 0, -1.5f),
            new Vector3(-1.5f, 0, -1.5f)
        };

        [Header("Worker Allocation (Optional Features)")]
        [Tooltip("Enable peasant allocation for building construction")]
        public bool enableBuildingWorkers = true;

        [Tooltip("Enable peasant allocation for training troops")]
        public bool enableTrainingWorkers = true;

        [Tooltip("Enable peasant allocation for resource collection")]
        public bool enableResourceWorkers = true;

        [Tooltip("Peasants required per active construction project")]
        public int peasantsPerBuilding = 2;

        [Tooltip("Peasants required per unit in training queue")]
        public int peasantsPerTraining = 1;

        [Tooltip("Peasants required per resource generation building")]
        public int peasantsPerResourceBuilding = 3;

        [Tooltip("Speed multiplier for construction when workers assigned")]
        public float workerConstructionSpeedBonus = 1.5f;

        [Tooltip("Speed multiplier for training when workers assigned")]
        public float workerTrainingSpeedBonus = 1.3f;

        [Tooltip("Production multiplier for resources when workers assigned")]
        public float workerResourceProductionBonus = 1.5f;

        /// <summary>
        /// Calculate the ideal peasant count based on current game state factors.
        /// </summary>
        public int CalculateIdealPeasantCount(float happiness, float reputation, float housingUtilization, float militaryStrength)
        {
            if (happiness < minimumHappinessForGathering || reputation < minimumReputationForGathering)
            {
                return 0;
            }

            // Normalize all factors to 0-1 range
            float happinessFactor = Mathf.Clamp01(happiness / 100f);
            float reputationFactor = Mathf.Clamp01(reputation / 100f);
            float housingFactor = Mathf.Clamp01(housingUtilization);
            float strengthFactor = Mathf.Clamp01(militaryStrength);

            // Calculate weighted sum
            float totalWeight = happinessInfluence + housingInfluence + reputationInfluence + strengthInfluence;

            float weightedSum =
                (happinessFactor * happinessInfluence +
                 reputationFactor * reputationInfluence +
                 housingFactor * housingInfluence +
                 strengthFactor * strengthInfluence) / totalWeight;

            // Convert to peasant count
            int idealCount = Mathf.RoundToInt(weightedSum * maxPeasantCapacity);
            return Mathf.Clamp(idealCount, 0, maxPeasantCapacity);
        }

        /// <summary>
        /// Get full description including campfire-specific info.
        /// </summary>
        public override string GetFullDescription()
        {
            var details = new List<string>();

            details.Add(base.GetFullDescription());
            details.Add($"\nCampfire Capacity: {maxPeasantCapacity} peasants");
            details.Add($"Gather Radius: {gatherRadius}m");

            if (happinessBonusPerPeasant > 0)
            {
                details.Add($"Happiness per Peasant: +{happinessBonusPerPeasant}");
            }

            if (reputationBonusPerPeasant > 0)
            {
                details.Add($"Reputation per Peasant: +{reputationBonusPerPeasant}");
            }

            var enabledFeatures = new List<string>();
            if (enableBuildingWorkers) enabledFeatures.Add("Building");
            if (enableTrainingWorkers) enabledFeatures.Add("Training");
            if (enableResourceWorkers) enabledFeatures.Add("Resources");

            if (enabledFeatures.Count > 0)
            {
                details.Add($"\nWorker Allocation: {string.Join(", ", enabledFeatures)}");
            }

            return string.Join("\n", details);
        }
    }
}
