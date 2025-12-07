using UnityEngine;
using RTS.Core.Events;
using RTS.Core.Services;
using System.Collections.Generic;

namespace RTS.Buildings.WorkerModules
{
    /// <summary>
    /// DEPRECATED: This module caused performance issues and joint queue behavior.
    /// Each barracks now manages training independently.
    /// This component is disabled and will be removed in a future update.
    /// </summary>
    [RequireComponent(typeof(Campfire))]
    public class TrainingWorkerModule : MonoBehaviour
    {
        [Header("Worker Settings")]
        [SerializeField] private bool enableModule = false; // Disabled by default
        [SerializeField] private int peasantsPerTraining = 1;
        [SerializeField] private float trainingSpeedBonus = 1.3f;

        [Header("Auto-Assignment")]
        [SerializeField] private bool autoAssignWorkers = false; // Disabled by default
        [SerializeField] private float assignmentUpdateInterval = 1.5f;

        private Campfire campfire;
        private IPeasantWorkforceService workforceService;
        private IPopulationService populationService;

        private Dictionary<GameObject, int> assignedWorkers = new Dictionary<GameObject, int>();
        private float updateTimer = 0f;

        private void Awake()
        {
            campfire = GetComponent<Campfire>();
        }

        private void Start()
        {
            workforceService = ServiceLocator.TryGet<IPeasantWorkforceService>();
            populationService = ServiceLocator.TryGet<IPopulationService>();

            if (enableModule)
            {
                EventBus.Subscribe<UnitTrainingStartedEvent>(OnTrainingStarted);
                EventBus.Subscribe<UnitTrainingCompletedEvent>(OnTrainingCompleted);
            }
        }

        private void Update()
        {
            if (!enableModule || !autoAssignWorkers) return;

            updateTimer += Time.deltaTime;
            if (updateTimer >= assignmentUpdateInterval)
            {
                updateTimer = 0f;
                UpdateWorkerAssignments();
            }
        }

        private void OnTrainingStarted(UnitTrainingStartedEvent evt)
        {
            if (evt.Building == null) return;

            if (autoAssignWorkers)
            {
                TryAssignWorkers(evt.Building);
            }
        }

        private void OnTrainingCompleted(UnitTrainingCompletedEvent evt)
        {
            if (evt.Building == null) return;

            // Check if there are more units in queue
            if (evt.Building.TryGetComponent<UnitTrainingQueue>(out var trainingQueue) && trainingQueue.QueueCount == 0)
            {
                // Release workers if queue is empty
                ReleaseWorkersFromBuilding(evt.Building);
            }
        }

        private void UpdateWorkerAssignments()
        {
            if (workforceService == null) return;

            // Find all barracks/training buildings
            GameObject[] allBuildings = GameObject.FindGameObjectsWithTag("Building");
            if (allBuildings == null) return;

            foreach (var building in allBuildings)
            {
                if (building == null) continue;

                if (building.TryGetComponent<UnitTrainingQueue>(out var trainingQueue))
                {
                }
                if (trainingQueue == null) continue;

                // Check if training and needs workers
                if (trainingQueue.QueueCount > 0)
                {
                    if (!assignedWorkers.ContainsKey(building) || assignedWorkers[building] == 0)
                    {
                        TryAssignWorkers(building);
                    }
                }
                else
                {
                    // Release workers if no training
                    ReleaseWorkersFromBuilding(building);
                }
            }
        }

        private bool TryAssignWorkers(GameObject building)
        {
            if (building == null || workforceService == null) return false;

            if (building.TryGetComponent<UnitTrainingQueue>(out var trainingQueue))
            {
            }
            if (trainingQueue == null || trainingQueue.QueueCount == 0) return false;

            // Check if we can assign workers
            if (!workforceService.CanAssignWorkers(peasantsPerTraining)) return false;

            // Request workers
            if (workforceService.RequestWorkers("Training", peasantsPerTraining, building))
            {
                assignedWorkers[building] = peasantsPerTraining;

                // Apply training speed bonus
                ApplyTrainingBonus(building, true);

                return true;
            }

            return false;
        }

        private void ReleaseWorkersFromBuilding(GameObject building)
        {
            if (building == null || workforceService == null) return;

            if (assignedWorkers.TryGetValue(building, out int workerCount))
            {
                workforceService.ReleaseWorkers("Training", workerCount, building);
                assignedWorkers.Remove(building);

                // Remove training bonus
                ApplyTrainingBonus(building, false);

            }
        }

        private void ApplyTrainingBonus(GameObject building, bool apply)
        {
            // This would integrate with UnitTrainingQueue to modify training speed
            // In a real implementation, you'd modify the training time

            if (building.TryGetComponent<UnitTrainingQueue>(out var trainingQueue))
            {
            }
            if (trainingQueue == null) return;

            // You could add a public method to UnitTrainingQueue like:
            // trainingQueue.SetTrainingSpeedMultiplier(apply ? trainingSpeedBonus : 1f);

            if (apply)
            {
            }
        }

        public void EnableModule(bool enable)
        {
            enableModule = enable;

            if (!enable)
            {
                ReleaseAllWorkers();
            }
        }

        private void ReleaseAllWorkers()
        {
            foreach (var kvp in assignedWorkers)
            {
                if (kvp.Key != null && workforceService != null)
                {
                    workforceService.ReleaseWorkers("Training", kvp.Value, kvp.Key);
                    ApplyTrainingBonus(kvp.Key, false);
                }
            }
            assignedWorkers.Clear();
        }

        private void OnDestroy()
        {
            ReleaseAllWorkers();

            if (enableModule)
            {
                EventBus.Unsubscribe<UnitTrainingStartedEvent>(OnTrainingStarted);
                EventBus.Unsubscribe<UnitTrainingCompletedEvent>(OnTrainingCompleted);
            }
        }

        #region Debug Methods

        [ContextMenu("Show Assigned Workers")]
        private void DebugShowAssignedWorkers()
        {
            foreach (var kvp in assignedWorkers)
            {
                if (kvp.Key != null)
                {
                }
            }
        }

        #endregion
    }
}
