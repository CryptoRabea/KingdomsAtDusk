using UnityEngine;

namespace KingdomsAtDusk.Core
{
    /// <summary>
    /// Global game configuration settings for runtime behavior.
    /// This ScriptableObject controls game-wide mechanics and systems.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "RTS/Game Config", order = 0)]
    public class GameConfigSO : ScriptableObject
    {
        [Header("Resource Gathering System")]
        [Tooltip("Toggle between building auto-generation and worker gathering with animations")]
        public ResourceGatheringMode gatheringMode = ResourceGatheringMode.BuildingAutoGenerate;

        [Header("Worker System")]
        [Tooltip("Enable/disable the peasant workforce system")]
        public bool enablePeasantSystem = true;

        [Tooltip("Enable worker visual animations during gathering")]
        public bool enableGatheringAnimations = true;

        [Tooltip("Enable carrying visual feedback when workers return with resources")]
        public bool enableCarryingVisuals = true;

        [Header("Worker Gathering Settings")]
        [Tooltip("Time in seconds for a worker to gather one resource unit")]
        [Range(1f, 30f)]
        public float gatheringTime = 5f;

        [Tooltip("How many resources a worker carries per trip")]
        [Range(1, 20)]
        public int resourcesPerTrip = 5;

        [Tooltip("Maximum distance workers will search for resource nodes")]
        [Range(10f, 100f)]
        public float maxGatheringDistance = 50f;

        // TODO: Future Feature - School Building
        // Add school building or similar structure to allow changing worker types
        // Example: Converting a lumber worker to a farmer, or retraining peasants
        // Should have a cost and time associated with retraining

        // TODO: Future Feature - Peasant System Toggle
        // Add ability to enable/disable the entire peasant workforce system at runtime
        // This would affect: campfire gathering, worker allocation, population bonuses
        // Consider: What happens to assigned workers when system is disabled?

        [Header("Circular Lens Vision")]
        [Tooltip("Enable/disable the circular lens vision system (x-ray vision through obstacles)")]
        public bool enableLensVision = true;

        [Tooltip("Default radius of the lens vision area")]
        [Range(5f, 100f)]
        public float lensVisionRadius = 20f;

        [Tooltip("X-Ray color for player units")]
        public Color playerUnitXRayColor = new Color(0.3f, 0.7f, 1f, 0.8f);

        [Tooltip("X-Ray color for enemy units")]
        public Color enemyUnitXRayColor = new Color(1f, 0.3f, 0.3f, 0.8f);

        [Tooltip("Transparency amount for obstacles in lens")]
        [Range(0f, 1f)]
        public float obstacleTransparency = 0.3f;

        [Tooltip("Update interval for lens vision (higher = better performance)")]
        [Range(0.01f, 0.5f)]
        public float lensVisionUpdateInterval = 0.1f;

        [Header("Debug")]
        [Tooltip("Show debug gizmos for worker paths and resource nodes")]
        public bool showDebugGizmos = false;
    }

    /// <summary>
    /// Defines how resources are gathered in the game.
    /// </summary>
    public enum ResourceGatheringMode
    {
        /// <summary>
        /// Buildings automatically generate resources on a timer (passive generation)
        /// </summary>
        BuildingAutoGenerate,

        /// <summary>
        /// Workers must physically gather resources with animations and pathfinding
        /// </summary>
        WorkerGathering
    }
}
