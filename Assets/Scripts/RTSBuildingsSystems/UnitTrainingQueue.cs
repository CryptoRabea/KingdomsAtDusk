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

        [Header("Performance")]
        [SerializeField] private float progressUpdateInterval = 0.1f; // Only update progress every 0.1s

        private Queue<TrainingQueueEntry> trainingQueue = new Queue<TrainingQueueEntry>();
        private TrainingQueueEntry currentTraining;
        private Building building;
        private IResourcesService resourceService;
        private BuildingDataSO buildingData;
        private float progressUpdateTimer = 0f;
        private float lastPublishedProgress = 0f;

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
                }
                else
                {
                    // Fallback: Create a spawn point in front of the building for backward compatibility
                    GameObject spawnObj = new GameObject("SpawnPoint");
                    spawnObj.transform.SetParent(transform);
                    spawnObj.transform.localPosition = Vector3.forward * 3f; // 3 units in front
                    spawnPoint = spawnObj.transform;
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

                // Publish progress update at intervals, not every frame (massive performance boost)
                progressUpdateTimer += Time.deltaTime;
                if (progressUpdateTimer >= progressUpdateInterval)
                {
                    progressUpdateTimer = 0f;

                    // Only publish if progress actually changed significantly
                    float currentProgress = currentTraining.Progress;
                    if (Mathf.Abs(currentProgress - lastPublishedProgress) > 0.01f)
                    {
                        if (currentTraining.unitData?.unitConfig != null)
                        {
                            EventBus.Publish(new TrainingProgressEvent(
                                gameObject,
                                currentTraining.unitData.unitConfig.unitName,
                                currentProgress
                            ));
                            lastPublishedProgress = currentProgress;
                        }
                    }
                }

                if (currentTraining.timeRemaining <= 0)
                {
                    CompleteTraining();
                    progressUpdateTimer = 0f;
                    lastPublishedProgress = 0f;
                }
            }
            else if (trainingQueue.Count > 0)
            {
                // Start next unit in queue
                currentTraining = trainingQueue.Dequeue();
                progressUpdateTimer = 0f;
                lastPublishedProgress = 0f;
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
                return false;
            }

            // Check queue capacity
            if (QueueCount >= maxQueueSize)
            {
                return false;
            }

            // Check and spend resources
            if (resourceService != null)
            {
                var costs = unitData.GetCosts();
                if (!resourceService.CanAfford(costs))
                {
                    return false;
                }

                if (!resourceService.SpendResources(costs))
                {
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
            }

            return true;
        }

        private void CompleteTraining()
        {
            if (currentTraining?.unitData?.unitConfig?.unitPrefab == null)
            {
                currentTraining = null;
                return;
            }

            if (showDebugInfo)
            {
            }

            // Spawn the unit
            GameObject spawnedUnit = Instantiate(
                currentTraining.unitData.unitConfig.unitPrefab,
                spawnPoint.position,
                Quaternion.identity
            );

            if (showDebugInfo)
            {
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
            }

            currentTraining = null;
        }

        /// <summary>
        /// Clear the entire training queue (without refund).
        /// </summary>
        public void ClearQueue()
        {
            trainingQueue.Clear();
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
                }
            }

            // Set the world position directly (no parenting to avoid transform issues)
            rallyPoint.position = position;

            if (showDebugInfo)
            {
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
            }

            // Wait a frame for the unit to fully initialize
            yield return null;

            if (unit == null)
            {
                yield break;
            }

            if (unit.TryGetComponent<UnitMovement>(out var unitMovement))
            {
                if (showDebugInfo)
                {
                }

                unitMovement.SetDestination(destination);

                if (showDebugInfo)
                {
                }
            }
            else
            {
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
