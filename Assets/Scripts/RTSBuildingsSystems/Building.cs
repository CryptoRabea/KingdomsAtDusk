using UnityEngine;
using RTS.Core.Events;
using RTS.Core.Services;
using System.Collections.Generic;
using KingdomsAtDusk.Core;

namespace RTS.Buildings
{
    /// <summary>
    /// Core building component - handles construction, happiness bonuses, and destruction.
    /// Attach this to every building prefab!
    /// </summary>
    public class Building : MonoBehaviour
    {
        [Header("Building Data")]
        [SerializeField] private BuildingDataSO data;

        [Header("Construction Settings")]
        [SerializeField] private bool requiresConstruction = true;
        [SerializeField] private float constructionTime = 5f;
        [SerializeField] private GameObject constructionVisual; // Optional: shows during construction

        private bool isConstructed = false;
        private float constructionProgress = 0f;
        private IHappinessService happinessService;
        private IResourcesService resourceService;
        private GameConfigSO gameConfig;

        // Resource generation
        private float resourceGenerationTimer = 0f;

        public BuildingDataSO Data => data;
        public BuildingDataSO buildingData => data; // Alternative accessor for compatibility
        public bool IsConstructed => isConstructed;
        public float ConstructionProgress => constructionProgress / constructionTime;

        public void Start()
        {
            happinessService = ServiceLocator.TryGet<IHappinessService>();
            resourceService = ServiceLocator.TryGet<IResourcesService>();
            gameConfig = Resources.Load<GameConfigSO>("GameConfig");

            if (!requiresConstruction)
            {
                CompleteConstruction();
            }
            else
            {
                StartConstruction();
            }
        }

        private void Update()
        {
            // Handle construction
            if (!isConstructed && requiresConstruction)
            {
                constructionProgress += Time.deltaTime;

                if (constructionProgress >= constructionTime)
                {
                    CompleteConstruction();
                }
            }

            // Handle resource generation (only after construction is complete)
            if (isConstructed && data != null && data.generatesResources)
            {
                resourceGenerationTimer += Time.deltaTime;

                if (resourceGenerationTimer >= data.generationInterval)
                {
                    GenerateResources();
                    resourceGenerationTimer = 0f;
                }
            }
        }

        private void StartConstruction()
        {
            // Show construction visual if available
            if (constructionVisual != null)
            {
                constructionVisual.SetActive(true);
            }

            // Publish placement event
            EventBus.Publish(new BuildingPlacedEvent(gameObject, transform.position));

        }

        private void CompleteConstruction()
        {
            isConstructed = true;
            constructionProgress = constructionTime;

            // Hide construction visual
            if (constructionVisual != null)
            {
                constructionVisual.SetActive(false);
            }

            // Apply happiness bonus
            if (data != null && happinessService != null && data.happinessBonus != 0)
            {
                happinessService.AddBuildingBonus(data.happinessBonus, data.buildingName);
            }

            // Publish completion event
            if (data != null)
            {
                EventBus.Publish(new BuildingCompletedEvent(gameObject, data.buildingName));
            }

        }

        private void GenerateResources()
        {
            if (resourceService == null || data == null) return;

            // Check if we're in worker gathering mode
            // If so, workers handle resource generation, not buildings
            if (gameConfig != null && gameConfig.gatheringMode == ResourceGatheringMode.WorkerGathering)
            {
                // Don't auto-generate resources, workers will do it
                return;
            }

            // Create resource dictionary with the generated amount
            var resources = new Dictionary<ResourceType, int>
            {
                { data.resourceType, data.resourceAmount }
            };

            // Add resources to the player
            resourceService.AddResources(resources);


            // Publish event (optional - for UI updates, sound effects, etc.)
            EventBus.Publish(new ResourcesGeneratedEvent(
                data.buildingName,
                data.resourceType,
                data.resourceAmount
            ));
        }

        public void OnDestroy()
        {
            // Remove happiness bonus when destroyed
            if (isConstructed && data != null && happinessService != null && data.happinessBonus != 0)
            {
                happinessService.RemoveBuildingBonus(data.happinessBonus, data.buildingName);
            }

            // Publish destruction event
            if (data != null)
            {
                EventBus.Publish(new BuildingDestroyedEvent(gameObject, data.buildingName));
            }
        }

        #region Public API

        public void SetData(BuildingDataSO buildingData)
        {
            data = buildingData;
        }

        public void InstantComplete()
        {
            if (!isConstructed)
            {
                CompleteConstruction();
            }
        }

        public void Demolish()
        {
            Destroy(gameObject);
        }

        #endregion

        #region Debug

        [ContextMenu("Complete Construction Instantly")]
        private void DebugCompleteConstruction()
        {
            InstantComplete();
        }

        [ContextMenu("Demolish Building")]
        private void DebugDemolish()
        {
            Demolish();
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            // Draw construction progress
            if (!isConstructed && requiresConstruction)
            {
                Gizmos.color = Color.yellow;
                float progress = constructionProgress / constructionTime;
                Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * progress);
            }
        }
    }
}