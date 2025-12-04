using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RTS.Core.Events;
using RTS.Core.Services;
using RTS.Units;
using RTSBuildingsSystems;

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

        [Header("Rally Point")]
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
                // First, try to find a BuildingSpawnPoint component in children
                BuildingSpawnPoint spawnPointComponent = GetComponentInChildren<BuildingSpawnPoint>();

                if (spawnPointComponent != null)
                {
                    spawnPoint = spawnPointComponent.Transform;
                    Debug.Log($"Using BuildingSpawnPoint from prefab at {spawnPoint.localPosition}");
                }
                else
                {
                    // Fallback: Create a spawn point in front of the building for backward compatibility
                    GameObject spawnObj = new GameObject("SpawnPoint");
                    spawnObj.transform.SetParent(transform);
                    spawnObj.transform.localPosition = Vector3.forward * 3f; // 3 units in front
                    spawnPoint = spawnObj.transform;
                    Debug.LogWarning($"No BuildingSpawnPoint found in prefab. Auto-created spawn point. Consider adding a BuildingSpawnPoint component to the building prefab.");
                }
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

            if (showDebugInfo)
            {
                Debug.Log($"🎖️ UnitTrainingQueue: Spawning {currentTraining.unitData.unitConfig.unitName} at {spawnPoint.position}");
            }

            // Spawn the unit
            GameObject spawnedUnit = Instantiate(
                currentTraining.unitData.unitConfig.unitPrefab,
                spawnPoint.position,
                Quaternion.identity
            );

            if (showDebugInfo)
            {
                Debug.Log($" UnitTrainingQueue: Unit spawned - {spawnedUnit.name}. Rally point null? {rallyPoint == null}");
            }

            // Move to rally point if set - use coroutine to wait for NavMeshAgent to initialize
            if (rallyPoint != null)
            {
                StartCoroutine(MoveUnitToRallyPoint(spawnedUnit, rallyPoint.position));
            }
            else
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"⚠️ UnitTrainingQueue: No rally point set for {gameObject.name}, unit will stay at spawn position");
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
                Debug.Log($" Completed training {currentTraining.unitData.unitConfig.unitName}");
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
        /// Set spawn point position
        /// </summary>
        public void SetSpawnPointPosition(Vector3 position)
        {
            if (spawnPoint != null)
            {
                spawnPoint.position = position;
                if (showDebugInfo)
                {
                    Debug.Log($"Spawn point set to {position}");
                }
            }
        }

        /// <summary>
        /// Get the spawn point transform
        /// </summary>
        public Transform GetSpawnPoint()
        {
            return spawnPoint;
        }

        /// <summary>
        /// Set rally point position
        /// </summary>
        public void SetRallyPointPosition(Vector3 position)
        {
            // Create rally point on first use if not assigned
            if (rallyPoint == null)
            {
                GameObject rallyObj = new GameObject($"RallyPoint_{gameObject.name}");
                rallyPoint = rallyObj.transform;

                if (showDebugInfo)
                {
                    Debug.Log($"🚩 UnitTrainingQueue: Created rally point for {gameObject.name}");
                }
            }

            // Set the world position directly (no parenting to avoid transform issues)
            rallyPoint.position = position;

            if (showDebugInfo)
            {
                Debug.Log($" UnitTrainingQueue: Rally point set to world position {position} for {gameObject.name}");
            }
        }

        /// <summary>
        /// Get the rally point transform
        /// </summary>
        public Transform GetRallyPoint()
        {
            return rallyPoint;
        }

        /// <summary>
        /// Clear the rally point
        /// </summary>
        public void ClearRallyPoint()
        {
            if (rallyPoint != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(rallyPoint.gameObject);
                }
                else
                {
                    DestroyImmediate(rallyPoint.gameObject);
                }
                rallyPoint = null;
            }
        }

        /// <summary>
        /// Coroutine to move unit to rally point after NavMeshAgent initializes
        /// </summary>
        private IEnumerator MoveUnitToRallyPoint(GameObject unit, Vector3 destination)
        {
            if (showDebugInfo)
            {
                Debug.Log($"🚩 UnitTrainingQueue: Rally point exists at {destination}, waiting for NavMeshAgent to initialize...");
            }

            // Wait a frame for the unit to fully initialize
            yield return null;

            if (unit == null)
            {
                Debug.LogError($" UnitTrainingQueue: Unit destroyed before it could move to rally point!");
                yield break;
            }

            if (unit.TryGetComponent<UnitMovement>(out var unitMovement))
            {
                if (showDebugInfo)
                {
                    Debug.Log($"🎯 UnitTrainingQueue: Issuing move command to {unit.name} to go to {destination}");
                }

                unitMovement.SetDestination(destination);

                if (showDebugInfo)
                {
                    Debug.Log($" UnitTrainingQueue: Unit {unit.name} commanded to move to rally point at {destination}");
                }
            }
            else
            {
                Debug.LogError($" UnitTrainingQueue: Spawned unit {unit.name} has no UnitMovement component - cannot move to rally point!");
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw spawn point in blue
            if (spawnPoint != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
                Gizmos.DrawLine(transform.position, spawnPoint.position);
            }

            // Draw rally point in green
            if (rallyPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(rallyPoint.position, 0.5f);
                Gizmos.DrawLine(transform.position, rallyPoint.position);

                // Draw line from spawn to rally if both exist
                if (spawnPoint != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(spawnPoint.position, rallyPoint.position);
                }
            }
        }
    }
}
