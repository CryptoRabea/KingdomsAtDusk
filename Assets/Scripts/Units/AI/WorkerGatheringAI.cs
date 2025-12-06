using UnityEngine;
using RTS.Units;
using KingdomsAtDusk.Units;
using KingdomsAtDusk.Core;
using RTS.Units.Animation;
using RTS.Core.Services;
using KingdomsAtDusk.Resources;
using RTS.Buildings;
using System.Collections.Generic;

namespace KingdomsAtDusk.Units.AI
{
    /// <summary>
    /// AI behavior for worker units that gather resources.
    /// Handles the full gather cycle: find resource -> move -> gather -> return -> deposit.
    /// </summary>
    [RequireComponent(typeof(UnitMovement))]
    public class WorkerGatheringAI : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("The worker type - determines which resources to gather")]
        public WorkerUnitType workerType = WorkerUnitType.LumberWorker;

        [Tooltip("Building this worker belongs to (where they deposit resources)")]
        public GameObject homeBuilding;

        [Header("Gathering Settings")]
        [Tooltip("Search radius for finding resource nodes")]
        [Range(10f, 100f)]
        public float searchRadius = 50f;

        [Tooltip("Time to gather resources (overridden by GameConfig if available)")]
        public float gatherTime = 5f;

        [Tooltip("Amount to gather per trip (overridden by GameConfig if available)")]
        public int gatherAmount = 5;

        // Component references
        private UnitMovement movement;
        private UnitAnimationController animController;
        private WorkerCarryingVisual carryingVisual;
        private GameConfigSO gameConfig;

        // State tracking
        private WorkerState currentState = WorkerState.Idle;
        private Resources.ResourceNode targetNode;
        private Vector3 gatheringPosition;
        private float gatherTimer = 0f;
        private int carriedResources = 0;
        private ResourceType resourceType;

        // Optimization
        private float nextSearchTime = 0f;
        private const float SEARCH_INTERVAL = 2f; // Search for new nodes every 2 seconds

        private void Awake()
        {
            movement = GetComponent<UnitMovement>();
            animController = GetComponent<UnitAnimationController>();
            carryingVisual = GetComponent<WorkerCarryingVisual>();

            // Load game config
            gameConfig = UnityEngine.Resources.Load<GameConfigSO>("GameConfig");
            if (gameConfig != null)
            {
                gatherTime = gameConfig.gatheringTime;
                gatherAmount = gameConfig.resourcesPerTrip;
                searchRadius = gameConfig.maxGatheringDistance;
            }

            resourceType = workerType.GetResourceType();
        }

        private void Start()
        {
            // Auto-detect home building if not set
            if (homeBuilding == null)
            {
                homeBuilding = FindNearestResourceBuilding();
            }

            TransitionToState(WorkerState.SearchingForResource);
        }

        private void Update()
        {
            // Check if gathering mode is enabled
            if (gameConfig != null && gameConfig.gatheringMode != ResourceGatheringMode.WorkerGathering)
            {
                // Gathering disabled, just idle
                if (currentState != WorkerState.Idle)
                {
                    TransitionToState(WorkerState.Idle);
                }
                return;
            }

            // State machine
            switch (currentState)
            {
                case WorkerState.Idle:
                    UpdateIdle();
                    break;

                case WorkerState.SearchingForResource:
                    UpdateSearching();
                    break;

                case WorkerState.MovingToResource:
                    UpdateMovingToResource();
                    break;

                case WorkerState.Gathering:
                    UpdateGathering();
                    break;

                case WorkerState.ReturningToBuilding:
                    UpdateReturning();
                    break;

                case WorkerState.Depositing:
                    UpdateDepositing();
                    break;
            }
        }

        #region State Updates

        private void UpdateIdle()
        {
            // Wait a bit before searching again
            if (Time.time >= nextSearchTime)
            {
                TransitionToState(WorkerState.SearchingForResource);
            }
        }

        private void UpdateSearching()
        {
            if (Time.time < nextSearchTime) return;

            targetNode = FindNearestResourceNode();
            if (targetNode != null && targetNode.CanGatherFrom())
            {
                if (targetNode.RegisterWorker())
                {
                    gatheringPosition = targetNode.GetGatheringPosition();
                    TransitionToState(WorkerState.MovingToResource);
                }
                else
                {
                    // Node is full, keep searching
                    nextSearchTime = Time.time + SEARCH_INTERVAL;
                }
            }
            else
            {
                // No valid nodes found, wait before searching again
                nextSearchTime = Time.time + SEARCH_INTERVAL;
                TransitionToState(WorkerState.Idle);
            }
        }

        private void UpdateMovingToResource()
        {
            if (targetNode == null)
            {
                TransitionToState(WorkerState.SearchingForResource);
                return;
            }

            // Check if we've reached the gathering position
            float distance = Vector3.Distance(transform.position, gatheringPosition);
            if (distance < 0.5f) // Close enough
            {
                movement.Stop();
                TransitionToState(WorkerState.Gathering);
            }
        }

        private void UpdateGathering()
        {
            if (targetNode == null)
            {
                TransitionToState(WorkerState.SearchingForResource);
                return;
            }

            gatherTimer += Time.deltaTime;

            if (gatherTimer >= gatherTime)
            {
                // Gather resources
                carriedResources = targetNode.GatherResources(gatherAmount);

                if (carriedResources > 0)
                {
                    // Show carrying visual
                    if (carryingVisual != null && gameConfig.enableCarryingVisuals)
                    {
                        carryingVisual.ShowCarrying(resourceType, carriedResources);
                    }

                    // Unregister from node and return to building
                    targetNode.UnregisterWorker();
                    targetNode = null;
                    TransitionToState(WorkerState.ReturningToBuilding);
                }
                else
                {
                    // Node depleted, find a new one
                    targetNode.UnregisterWorker();
                    targetNode = null;
                    TransitionToState(WorkerState.SearchingForResource);
                }

                gatherTimer = 0f;
            }
        }

        private void UpdateReturning()
        {
            if (homeBuilding == null)
            {
                // No home building, just deposit here
                TransitionToState(WorkerState.Depositing);
                return;
            }

            float distance = Vector3.Distance(transform.position, homeBuilding.transform.position);
            if (distance < 3f) // Close enough to building
            {
                movement.Stop();
                TransitionToState(WorkerState.Depositing);
            }
        }

        private void UpdateDepositing()
        {
            // Deposit resources to resource manager
            if (carriedResources > 0)
            {
                var resourceManager = ServiceLocator.Get<ResourceManager>();

                if (resourceManager != null)
                {
                    // Fix: Use a dictionary as required by AddResources signature
                    var amounts = new Dictionary<ResourceType, int>
                    {
                        { resourceType, carriedResources }
                    };
                    resourceManager.AddResources(amounts);
                }

                carriedResources = 0;

                // Hide carrying visual
                if (carryingVisual != null)
                {
                    carryingVisual.HideCarrying();
                }
            }

            // Go back to searching for more resources
            TransitionToState(WorkerState.SearchingForResource);
        }

        #endregion

        #region State Transitions

        private void TransitionToState(WorkerState newState)
        {
            // Exit current state
            ExitState(currentState);

            // Enter new state
            currentState = newState;
            EnterState(newState);
        }

        private void EnterState(WorkerState state)
        {
            switch (state)
            {
                case WorkerState.Idle:
                    if (animController != null) animController.PlayCustomAnimation("Idle");
                    nextSearchTime = Time.time + 1f;
                    break;

                case WorkerState.SearchingForResource:
                    nextSearchTime = Time.time; // Search immediately
                    break;

                case WorkerState.MovingToResource:
                    if (targetNode != null)
                    {
                        movement.SetDestination(gatheringPosition);
                    }
                    break;

                case WorkerState.Gathering:
                    gatherTimer = 0f;
                    // Play gathering animation
                    if (animController != null)
                    {
                        // Use attack animation as gathering animation for now
                        // TODO: Add dedicated gathering animation to UnitAnimationProfile
                        animController.PlayCustomAnimation("DoIdleAction");
                    }
                    break;

                case WorkerState.ReturningToBuilding:
                    if (homeBuilding != null)
                    {
                        movement.SetDestination(homeBuilding.transform.position);
                    }
                    break;

                case WorkerState.Depositing:
                    // Brief pause for deposit
                    break;
            }
        }

        private void ExitState(WorkerState state)
        {
            switch (state)
            {
                case WorkerState.Gathering:
                    // Stop gathering animation
                    break;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Find the nearest resource node of the appropriate type.
        /// </summary>
        private ResourceNode FindNearestResourceNode()
        {
            ResourceNode[] allNodes = FindObjectsByType<ResourceNode>(FindObjectsSortMode.None);
            ResourceNode nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (var node in allNodes)
            {
                // Check if node is the right type
                if (node.resourceType != resourceType) continue;

                // Check if we can gather from it
                if (!node.CanGatherFrom()) continue;

                // Check distance
                float distance = Vector3.Distance(transform.position, node.transform.position);
                if (distance < nearestDistance && distance <= searchRadius)
                {
                    nearest = node;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Find the nearest building that generates this resource type.
        /// Used to auto-detect home building.
        /// </summary>
        private GameObject FindNearestResourceBuilding()
        {
            var buildings = FindObjectsByType<Building>(FindObjectsSortMode.None);
            GameObject nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (var building in buildings)
            {
                if (building.buildingData == null) continue;

                // Check if building generates the right resource
                if (building.buildingData.generatesResources &&
                    building.buildingData.resourceType == resourceType)
                {
                    float distance = Vector3.Distance(transform.position, building.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearest = building.gameObject;
                        nearestDistance = distance;
                    }
                }
            }

            return nearest;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set the home building for this worker.
        /// </summary>
        public void SetHomeBuilding(GameObject building)
        {
            homeBuilding = building;
        }

        /// <summary>
        /// Get current worker state for debugging.
        /// </summary>
        public string GetStateInfo()
        {
            return $"Worker ({workerType}): {currentState} | Carrying: {carriedResources}";
        }

        #endregion

        private void OnDestroy()
        {
            // Unregister from resource node if gathering
            if (targetNode != null)
            {
                targetNode.UnregisterWorker();
            }
        }

        // Visualization
        private void OnDrawGizmosSelected()
        {
            if (gameConfig != null && !gameConfig.showDebugGizmos) return;

            // Draw search radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, searchRadius);

            // Draw target node connection
            if (targetNode != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, targetNode.transform.position);
            }

            // Draw home building connection
            if (homeBuilding != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, homeBuilding.transform.position);
            }
        }
    }

    /// <summary>
    /// Worker AI state machine states.
    /// </summary>
    public enum WorkerState
    {
        Idle,                   // Waiting
        SearchingForResource,   // Looking for a resource node
        MovingToResource,       // Walking to the resource node
        Gathering,              // Gathering resources (playing animation)
        ReturningToBuilding,    // Walking back with resources
        Depositing              // Depositing resources at building
    }
}
