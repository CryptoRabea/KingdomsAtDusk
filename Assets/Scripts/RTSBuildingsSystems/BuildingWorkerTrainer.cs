using UnityEngine;
using System.Collections.Generic;
using RTS.Buildings;
using RTS.Units;
using KingdomsAtDusk.Core;
using KingdomsAtDusk.Units.AI;
using RTS.Core.Events;

namespace KingdomsAtDusk.Buildings
{
    /// <summary>
    /// Component that handles training and managing workers for a building.
    /// Automatically trains workers when in worker gathering mode.
    /// </summary>
    [RequireComponent(typeof(Building))]
    public class BuildingWorkerTrainer : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Override building data settings")]
        public bool overrideSettings = false;

        [Tooltip("Maximum workers (if overriding)")]
        public int maxWorkers = 3;

        [Tooltip("Worker unit config (if overriding)")]
        public UnitConfigSO workerConfig;

        [Header("Spawning")]
        [Tooltip("Where workers spawn (if not set, uses building position + offset)")]
        public Transform spawnPoint;

        [Tooltip("Offset from building position if no spawn point")]
        public Vector3 spawnOffset = new Vector3(3f, 0f, 0f);

        [Header("Runtime Info")]
        [SerializeField, Tooltip("Current active workers")]
        private List<GameObject> activeWorkers = new List<GameObject>();

        // Component references
        private Building building;
        private BuildingDataSO buildingData;
        private GameConfigSO gameConfig;

        // State
        private bool isInitialized = false;
        private bool hasAutoSpawned = false;

        private void Awake()
        {
            building = GetComponent<Building>();
            gameConfig =UnityEngine. Resources.Load<GameConfigSO>("GameConfig");
        }

        private void Start()
        {
            if (building != null && building.buildingData != null)
            {
                buildingData = building.buildingData;

                // Subscribe to building completion event
                EventBus.Subscribe<BuildingCompletedEvent>(OnBuildingCompleted);

                // If building is already completed, initialize
                if (building.IsConstructed)
                {
                    Initialize();
                }
            }
        }

        private void OnDestroy()
        {
            RTS.Core.Events.EventBus.Unsubscribe<BuildingCompletedEvent>(OnBuildingCompleted);

            // Clean up all workers when building is destroyed
            DespawnAllWorkers();
        }

        private void OnBuildingCompleted(BuildingCompletedEvent evt)
        {
            // Check if this event is for our building
            if (evt.Building == gameObject)
            {
                Initialize();
            }
        }

        private void Initialize()
        {
            if (isInitialized) return;
            if (buildingData == null) return;

            // Only initialize if building can train workers
            bool canTrain = overrideSettings || buildingData.canTrainWorkers;
            if (!canTrain) return;

            isInitialized = true;

            // Auto-spawn workers if enabled and in worker gathering mode
            if (ShouldAutoSpawnWorkers())
            {
                AutoSpawnWorkers();
            }
        }

        private bool ShouldAutoSpawnWorkers()
        {
            // Check if auto-spawn is enabled
            bool autoSpawn = overrideSettings ? true : buildingData.autoTrainWorkers;
            if (!autoSpawn) return false;

            // Check if we're in worker gathering mode
            if (gameConfig == null) return false;
            if (gameConfig.gatheringMode != ResourceGatheringMode.WorkerGathering) return false;

            // Don't spawn multiple times
            if (hasAutoSpawned) return false;

            return true;
        }

        private void AutoSpawnWorkers()
        {
            hasAutoSpawned = true;

            int targetWorkers = overrideSettings ? maxWorkers : buildingData.maxWorkers;
            UnitConfigSO config = overrideSettings ? workerConfig : buildingData.workerUnitConfig;

            if (config == null)
            {
                Debug.LogWarning($"Building {buildingData.buildingName} has no worker config assigned!");
                return;
            }

            // Spawn workers up to max
            for (int i = 0; i < targetWorkers; i++)
            {
                SpawnWorker(config);
            }

            Debug.Log($"Auto-spawned {targetWorkers} workers for {buildingData.buildingName}");
        }

        /// <summary>
        /// Manually spawn a worker for this building.
        /// </summary>
        public GameObject SpawnWorker(UnitConfigSO config = null)
        {
            if (config == null)
            {
                config = overrideSettings ? workerConfig : buildingData.workerUnitConfig;
            }

            if (config == null)
            {
                Debug.LogError("No worker config specified!");
                return null;
            }

            // Check if we've reached max workers
            CleanupDeadWorkers();
            int currentMax = overrideSettings ? maxWorkers : buildingData.maxWorkers;
            if (activeWorkers.Count >= currentMax)
            {
                Debug.LogWarning($"Building already has maximum workers ({currentMax})");
                return null;
            }

            // Determine spawn position
            Vector3 spawnPos = GetSpawnPosition();

            // Spawn the worker
            GameObject workerObj = Instantiate(config.unitPrefab, spawnPos, Quaternion.identity);

            // Set up worker AI
            var gatheringAI = workerObj.GetComponent<WorkerGatheringAI>();
            if (gatheringAI == null)
            {
                gatheringAI = workerObj.AddComponent<WorkerGatheringAI>();
            }

            // Configure worker
            gatheringAI.SetHomeBuilding(gameObject);

            // Set worker type based on config
            if (config.isWorker)
            {
                gatheringAI.workerType = config.workerType;
            }

            // Track worker
            activeWorkers.Add(workerObj);

            // Publish event
            EventBus.Publish(new UnitSpawnedEvent
            {
                Unit = workerObj,
                Position = spawnPos
            });

            return workerObj;
        }

        /// <summary>
        /// Despawn a specific worker.
        /// </summary>
        public void DespawnWorker(GameObject worker)
        {
            if (worker == null) return;

            activeWorkers.Remove(worker);
            Destroy(worker);
        }

        /// <summary>
        /// Despawn all workers associated with this building.
        /// </summary>
        public void DespawnAllWorkers()
        {
            foreach (var worker in activeWorkers)
            {
                if (worker != null)
                {
                    Destroy(worker);
                }
            }

            activeWorkers.Clear();
        }

        /// <summary>
        /// Get the spawn position for new workers.
        /// </summary>
        private Vector3 GetSpawnPosition()
        {
            if (spawnPoint != null)
            {
                return spawnPoint.position;
            }

            return transform.position + spawnOffset;
        }

        /// <summary>
        /// Remove null references from active workers list.
        /// </summary>
        private void CleanupDeadWorkers()
        {
            activeWorkers.RemoveAll(w => w == null);
        }

        #region Public API

        /// <summary>
        /// Get the number of active workers.
        /// </summary>
        public int GetActiveWorkerCount()
        {
            CleanupDeadWorkers();
            return activeWorkers.Count;
        }

        /// <summary>
        /// Get the maximum number of workers allowed.
        /// </summary>
        public int GetMaxWorkers()
        {
            return overrideSettings ? maxWorkers : (buildingData?.maxWorkers ?? 0);
        }

        /// <summary>
        /// Check if we can train more workers.
        /// </summary>
        public bool CanTrainMoreWorkers()
        {
            CleanupDeadWorkers();
            return activeWorkers.Count < GetMaxWorkers();
        }

        /// <summary>
        /// Get all active worker game objects.
        /// </summary>
        public List<GameObject> GetActiveWorkers()
        {
            CleanupDeadWorkers();
            return new List<GameObject>(activeWorkers);
        }

        #endregion

        // Debug visualization
        private void OnDrawGizmosSelected()
        {
            if (gameConfig != null && !gameConfig.showDebugGizmos) return;

            // Draw spawn point
            Vector3 spawn = GetSpawnPosition();
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(spawn, 0.5f);
            Gizmos.DrawLine(transform.position, spawn);

            // Draw worker connections
            if (Application.isPlaying)
            {
                Gizmos.color = Color.green;
                foreach (var worker in activeWorkers)
                {
                    if (worker != null)
                    {
                        Gizmos.DrawLine(transform.position, worker.transform.position);
                    }
                }
            }
        }
    }
}
