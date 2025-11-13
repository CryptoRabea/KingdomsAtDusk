using UnityEngine;
using RTS.Core.Events;
using RTS.Core.Services;
using System.Collections.Generic;

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

        // Resource generation
        private float resourceGenerationTimer = 0f;

        public BuildingDataSO Data => data;
        public bool IsConstructed => isConstructed;
        public float ConstructionProgress => constructionProgress / constructionTime;

        private void Start()
        {
            happinessService = ServiceLocator.TryGet<IHappinessService>();
            resourceService = ServiceLocator.TryGet<IResourcesService>();

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

            Debug.Log($"Started constructing {data?.buildingName ?? "building"}");
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
                Debug.Log($"Applied happiness bonus: +{data.happinessBonus} from {data.buildingName}");
            }

            // Publish completion event
            if (data != null)
            {
                EventBus.Publish(new BuildingCompletedEvent(gameObject, data.buildingName));
            }

            Debug.Log($"✅ {data?.buildingName ?? "Building"} construction complete!");
        }

        private void GenerateResources()
        {
            if (resourceService == null || data == null) return;

            // Create resource dictionary with the generated amount
            var resources = new Dictionary<ResourceType, int>
            {
                { data.resourceType, data.resourceAmount }
            };

            // Add resources to the player
            resourceService.AddResources(resources);

            Debug.Log($"✅ {data.buildingName} generated {data.resourceAmount} {data.resourceType}");

            // Publish event (optional - for UI updates, sound effects, etc.)
            EventBus.Publish(new ResourcesGeneratedEvent(
                data.buildingName,
                data.resourceType,
                data.resourceAmount
            ));
        }

        private void OnDestroy()
        {
            // Remove happiness bonus when destroyed
            if (isConstructed && data != null && happinessService != null && data.happinessBonus != 0)
            {
                happinessService.RemoveBuildingBonus(data.happinessBonus, data.buildingName);
                Debug.Log($"Removed happiness bonus: -{data.happinessBonus} from {data.buildingName}");
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