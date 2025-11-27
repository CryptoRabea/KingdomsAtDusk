using UnityEngine;
using TopDownWallBuilding.Core.Events;

namespace TopDownWallBuilding.WallSystems
{
    /// <summary>
    /// Core building/wall component that manages lifecycle and construction.
    /// Attach this to wall prefabs.
    /// </summary>
    public class Building : MonoBehaviour
    {
        [Header("Building Data")]
        [SerializeField] private BuildingDataSO buildingData;

        [Header("Construction Settings")]
        [SerializeField] private bool startCompleted = false;
        [SerializeField] private bool enableConstruction = true;

        private bool isConstructed = false;
        private float constructionProgress = 0f;
        private float constructionTimer = 0f;

        private void Start()
        {
            if (startCompleted || !enableConstruction)
            {
                CompleteConstruction();
            }
            else if (buildingData != null)
            {
                constructionTimer = 0f;
                constructionProgress = 0f;
            }
        }

        private void Update()
        {
            if (!isConstructed && enableConstruction && buildingData != null)
            {
                constructionTimer += Time.deltaTime;
                constructionProgress = Mathf.Clamp01(constructionTimer / buildingData.constructionTime);

                if (constructionProgress >= 1f)
                {
                    CompleteConstruction();
                }
            }
        }

        private void CompleteConstruction()
        {
            if (isConstructed) return;

            isConstructed = true;
            constructionProgress = 1f;

            if (buildingData != null)
            {
                EventBus.Publish(new BuildingCompletedEvent(gameObject, buildingData.buildingName));
            }

            Debug.Log($"Building completed: {(buildingData != null ? buildingData.buildingName : gameObject.name)}");
        }

        private void OnDestroy()
        {
            if (buildingData != null && isConstructed)
            {
                EventBus.Publish(new BuildingDestroyedEvent(gameObject, buildingData.buildingName));
            }
        }

        #region Public API

        /// <summary>
        /// Set the building data (used when instantiating from prefab).
        /// </summary>
        public void SetData(BuildingDataSO data)
        {
            buildingData = data;
        }

        /// <summary>
        /// Get the building data.
        /// </summary>
        public BuildingDataSO GetData()
        {
            return buildingData;
        }

        /// <summary>
        /// Check if construction is complete.
        /// </summary>
        public bool IsConstructed => isConstructed;

        /// <summary>
        /// Get construction progress (0-1).
        /// </summary>
        public float GetConstructionProgress() => constructionProgress;

        /// <summary>
        /// Force complete construction immediately.
        /// </summary>
        public void ForceComplete()
        {
            CompleteConstruction();
        }

        #endregion
    }
}
