using UnityEngine;
using System.Collections.Generic;
using RTS.Core.Events;
using RTS.Core.Services;
using RTS.Units;

namespace RTS.Buildings
{
    /// <summary>
    /// Represents a unit currently being trained.
    /// </summary>
    [System.Serializable]
    public class TrainingQueueEntry
    {
        public TrainableUnitData unitData;
        public float timeRemaining;
        public float totalTime;

        public float Progress => 1f - (timeRemaining / totalTime);

        public TrainingQueueEntry(TrainableUnitData data)
        {
            unitData = data;
            totalTime = data.trainingTime;
            timeRemaining = data.trainingTime;
        }
    }

    /// <summary>
    /// Manages unit training queue for a building.
    /// Attach this to buildings that can train units.
    /// </summary>
    public class UnitTrainingQueue : MonoBehaviour
    {
        [Header("Training Settings")]
        [SerializeField] private int maxQueueSize = 5;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform rallyPoint;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        private Queue<TrainingQueueEntry> trainingQueue = new Queue<TrainingQueueEntry>();
        private TrainingQueueEntry currentTraining;
        private Building building;
        private IResourcesService resourceService;
        private BuildingDataSO buildingData;

        public int QueueCount => trainingQueue.Count + (currentTraining != null ? 1 : 0);
        public bool IsTraining => currentTraining != null;
        public TrainingQueueEntry CurrentTraining => currentTraining;
        public IReadOnlyCollection<TrainingQueueEntry> Queue => trainingQueue;

        private void Awake()
        {
            building = GetComponent<Building>();

            if (spawnPoint == null)
            {
                // Create a spawn point in front of the building
                GameObject spawnObj = new GameObject("SpawnPoint");
                spawnObj.transform.SetParent(transform);
                spawnObj.transform.localPosition = Vector3.forward * 3f; // 3 units in front
                spawnPoint = spawnObj.transform;
            }

            if (rallyPoint == null)
            {
                // Create a rally point (initially at spawn point position)
                GameObject rallyObj = new GameObject("RallyPoint");
                rallyObj.transform.SetParent(transform);
                rallyObj.transform.position = spawnPoint.position;
                rallyPoint = rallyObj.transform;
            }
        }

        private void Start()
        {
            resourceService = ServiceLocator.TryGet<IResourcesService>();

            if (building != null && building.Data != null)
            {
                buildingData = building.Data;
            }
        }

        private void Update()
        {
            // Only train units if building is constructed
            if (building != null && !building.IsConstructed)
                return;

            if (currentTraining != null)
            {
                currentTraining.timeRemaining -= Time.deltaTime;

                // Publish progress update
                if (currentTraining.unitData?.unitConfig != null)
                {
                    EventBus.Publish(new TrainingProgressEvent(
                        gameObject,
                        currentTraining.unitData.unitConfig.unitName,
                        currentTraining.Progress
                    ));
                }

                if (currentTraining.timeRemaining <= 0)
                {
                    CompleteTraining();
                }
            }
            else if (trainingQueue.Count > 0)
            {
                // Start next unit in queue
                currentTraining = trainingQueue.Dequeue();
            }
        }

        /// <summary>
        /// Attempt to add a unit to the training queue.
        /// Returns true if successful (resources spent and added to queue).
        /// </summary>
        public bool TryTrainUnit(TrainableUnitData unitData)
        {
            if (unitData == null || unitData.unitConfig == null)
            {
                Debug.LogWarning("Cannot train unit: invalid unit data");
                return false;
            }

            // Check queue capacity
            if (QueueCount >= maxQueueSize)
            {
                Debug.LogWarning($"Training queue is full ({maxQueueSize})");
                return false;
            }

            // Check and spend resources
            if (resourceService != null)
            {
                var costs = unitData.GetCosts();
                if (!resourceService.CanAfford(costs))
                {
                    Debug.LogWarning($"Cannot afford {unitData.unitConfig.unitName}");
                    return false;
                }

                if (!resourceService.SpendResources(costs))
                {
                    Debug.LogWarning($"Failed to spend resources for {unitData.unitConfig.unitName}");
                    return false;
                }
            }

            // Add to queue
            var queueEntry = new TrainingQueueEntry(unitData);
            trainingQueue.Enqueue(queueEntry);

            // Publish event
            EventBus.Publish(new UnitTrainingStartedEvent(gameObject, unitData.unitConfig.unitName));

            if (showDebugInfo)
            {
                Debug.Log($"Started training {unitData.unitConfig.unitName} at {buildingData?.buildingName ?? "Building"}. Queue: {QueueCount}");
            }

            return true;
        }

        private void CompleteTraining()
        {
            if (currentTraining?.unitData?.unitConfig?.unitPrefab == null)
            {
                Debug.LogError("Cannot complete training: missing unit prefab");
                currentTraining = null;
                return;
            }

            // Spawn the unit at the spawn point
            GameObject spawnedUnit = Instantiate(
                currentTraining.unitData.unitConfig.unitPrefab,
                spawnPoint.position,
                Quaternion.identity
            );

            // Send unit to rally point if it's different from spawn point
            if (rallyPoint != null && Vector3.Distance(rallyPoint.position, spawnPoint.position) > 0.5f)
            {
                UnitMovement unitMovement = spawnedUnit.GetComponent<UnitMovement>();
                if (unitMovement != null)
                {
                    unitMovement.SetDestination(rallyPoint.position);
                    if (showDebugInfo)
                    {
                        Debug.Log($"Unit moving to rally point at {rallyPoint.position}");
                    }
                }
            }

            // Publish events
            EventBus.Publish(new UnitTrainingCompletedEvent(
                gameObject,
                spawnedUnit,
                currentTraining.unitData.unitConfig.unitName
            ));

            EventBus.Publish(new UnitSpawnedEvent(spawnedUnit, spawnPoint.position));

            if (showDebugInfo)
            {
                Debug.Log($"✅ Completed training {currentTraining.unitData.unitConfig.unitName}");
            }

            currentTraining = null;
        }

        /// <summary>
        /// Cancel the current training and refund resources (optional).
        /// </summary>
        public void CancelCurrentTraining(bool refund = true)
        {
            if (currentTraining == null) return;

            if (refund && resourceService != null)
            {
                // Refund partial resources based on progress
                var costs = currentTraining.unitData.GetCosts();
                var refundAmount = new Dictionary<ResourceType, int>();

                foreach (var cost in costs)
                {
                    // Refund based on remaining time
                    int refundValue = Mathf.CeilToInt(cost.Value * (currentTraining.timeRemaining / currentTraining.totalTime));
                    refundAmount[cost.Key] = refundValue;
                }

                resourceService.AddResources(refundAmount);
                Debug.Log($"Refunded resources for cancelled training");
            }

            currentTraining = null;
        }

        /// <summary>
        /// Clear the entire training queue (without refund).
        /// </summary>
        public void ClearQueue()
        {
            trainingQueue.Clear();
            Debug.Log("Training queue cleared");
        }

        /// <summary>
        /// Set rally point position (where units go after spawning)
        /// </summary>
        public void SetRallyPointPosition(Vector3 position)
        {
            if (rallyPoint != null)
            {
                rallyPoint.position = position;
                if (showDebugInfo)
                {
                    Debug.Log($"Rally point set to {position}");
                }
            }
        }

        /// <summary>
        /// Get the spawn point transform (where units spawn)
        /// </summary>
        public Transform GetSpawnPoint()
        {
            return spawnPoint;
        }

        /// <summary>
        /// Get the rally point transform (where units go after spawning)
        /// </summary>
        public Transform GetRallyPoint()
        {
            return rallyPoint;
        }

        private void OnDrawGizmosSelected()
        {
            // Draw spawn point (where units spawn)
            if (spawnPoint != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
                Gizmos.DrawLine(transform.position, spawnPoint.position);
            }

            // Draw rally point (where units go after spawning)
            if (rallyPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(rallyPoint.position, 0.5f);

                // Draw line from spawn point to rally point if they're different
                if (spawnPoint != null && Vector3.Distance(rallyPoint.position, spawnPoint.position) > 0.5f)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(spawnPoint.position, rallyPoint.position);
                }
            }
        }
    }
}
