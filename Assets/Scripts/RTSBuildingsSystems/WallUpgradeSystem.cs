using UnityEngine;
using RTS.Core.Events;
using RTS.Resources;

namespace RTS.Buildings
{
    /// <summary>
    /// Handles wall upgrades to towers (Stronghold Crusader style).
    /// Allows selected walls to be converted to defensive towers.
    /// </summary>
    public class WallUpgradeSystem : MonoBehaviour
    {
        [Header("Upgrade Settings")]
        [Tooltip("The tower prefab to replace this wall with")]
        [SerializeField] private GameObject towerPrefab;

        [Tooltip("Can this wall be upgraded?")]
        [SerializeField] private bool canUpgrade = true;

        [Header("Upgrade Costs")]
        [SerializeField] private int woodCost = 50;
        [SerializeField] private int stoneCost = 100;
        [SerializeField] private int goldCost = 25;
        [SerializeField] private int foodCost = 0;

        [Header("Upgrade UI")]
        [Tooltip("UI element to show when wall is selected")]
        [SerializeField] private GameObject upgradeButtonUI;

        private WallConnectionSystem wallSystem;
        private BuildingSelectable selectable;
        private Building building;
        private IResourcesService resourceService;
        private bool isSelected = false;

        private void Awake()
        {
            wallSystem = GetComponent<WallConnectionSystem>();
            selectable = GetComponent<BuildingSelectable>();
            building = GetComponent<Building>();

            // Add BuildingSelectable if not present
            if (selectable == null)
            {
                selectable = gameObject.AddComponent<BuildingSelectable>();
            }

            // Hide upgrade UI initially
            if (upgradeButtonUI != null)
            {
                upgradeButtonUI.SetActive(false);
            }
        }

        private void Start()
        {
            // Get resource service
            resourceService = ServiceLocator.Get<IResourcesService>();

            // Subscribe to selection events
            EventBus.Subscribe<BuildingSelectedEvent>(OnBuildingSelected);
            EventBus.Subscribe<BuildingDeselectedEvent>(OnBuildingDeselected);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<BuildingSelectedEvent>(OnBuildingSelected);
            EventBus.Unsubscribe<BuildingDeselectedEvent>(OnBuildingDeselected);
        }

        private void OnBuildingSelected(BuildingSelectedEvent evt)
        {
            if (evt.Building == building)
            {
                isSelected = true;
                ShowUpgradeUI();
            }
        }

        private void OnBuildingDeselected(BuildingDeselectedEvent evt)
        {
            if (evt.Building == building)
            {
                isSelected = false;
                HideUpgradeUI();
            }
        }

        private void ShowUpgradeUI()
        {
            if (canUpgrade && upgradeButtonUI != null)
            {
                upgradeButtonUI.SetActive(true);
            }
        }

        private void HideUpgradeUI()
        {
            if (upgradeButtonUI != null)
            {
                upgradeButtonUI.SetActive(false);
            }
        }

        /// <summary>
        /// Attempt to upgrade this wall to a tower
        /// </summary>
        public void UpgradeToTower()
        {
            if (!canUpgrade)
            {
                Debug.LogWarning("This wall cannot be upgraded!");
                return;
            }

            if (towerPrefab == null)
            {
                Debug.LogError("Tower prefab not assigned! Cannot upgrade wall.");
                return;
            }

            // Check if player has enough resources
            if (!CanAffordUpgrade())
            {
                Debug.Log("Not enough resources to upgrade wall to tower!");
                EventBus.Publish(new NotificationEvent("Not enough resources to upgrade!", NotificationType.Warning));
                return;
            }

            // Spend resources
            SpendUpgradeCost();

            // Spawn tower at wall position
            Vector3 wallPosition = transform.position;
            Quaternion wallRotation = transform.rotation;

            GameObject tower = Instantiate(towerPrefab, wallPosition, wallRotation);

            // Copy building data if available
            Building wallBuilding = GetComponent<Building>();
            Building towerBuilding = tower.GetComponent<Building>();
            if (wallBuilding != null && towerBuilding != null && wallBuilding.BuildingData != null)
            {
                towerBuilding.Initialize(wallBuilding.BuildingData);
            }

            // Publish events
            EventBus.Publish(new BuildingPlacedEvent(tower, wallPosition));
            EventBus.Publish(new NotificationEvent("Wall upgraded to tower!", NotificationType.Success));

            Debug.Log($"Wall at {wallPosition} upgraded to tower!");

            // Destroy the wall
            Destroy(gameObject);
        }

        private bool CanAffordUpgrade()
        {
            if (resourceService == null)
            {
                Debug.LogWarning("ResourceService not available!");
                return false;
            }

            return resourceService.GetResource(ResourceType.Wood) >= woodCost &&
                   resourceService.GetResource(ResourceType.Stone) >= stoneCost &&
                   resourceService.GetResource(ResourceType.Gold) >= goldCost &&
                   resourceService.GetResource(ResourceType.Food) >= foodCost;
        }

        private void SpendUpgradeCost()
        {
            if (resourceService == null) return;

            if (woodCost > 0) resourceService.AddResource(ResourceType.Wood, -woodCost);
            if (stoneCost > 0) resourceService.AddResource(ResourceType.Stone, -stoneCost);
            if (goldCost > 0) resourceService.AddResource(ResourceType.Gold, -goldCost);
            if (foodCost > 0) resourceService.AddResource(ResourceType.Food, -foodCost);

            EventBus.Publish(new ResourcesSpentEvent(woodCost, foodCost, goldCost, stoneCost));
        }

        /// <summary>
        /// Get the upgrade costs for UI display
        /// </summary>
        public (int wood, int stone, int gold, int food) GetUpgradeCost()
        {
            return (woodCost, stoneCost, goldCost, foodCost);
        }

        /// <summary>
        /// Check if upgrade is currently affordable
        /// </summary>
        public bool IsUpgradeAffordable()
        {
            return CanAffordUpgrade();
        }

        #region Public API

        public bool CanUpgrade => canUpgrade;
        public GameObject TowerPrefab => towerPrefab;

        #endregion
    }

    /// <summary>
    /// Notification event for UI messages
    /// </summary>
    public class NotificationEvent
    {
        public string Message { get; }
        public NotificationType Type { get; }

        public NotificationEvent(string message, NotificationType type)
        {
            Message = message;
            Type = type;
        }
    }

    /// <summary>
    /// Notification types for UI styling
    /// </summary>
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }
}
