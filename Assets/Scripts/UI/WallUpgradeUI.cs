using RTS.Buildings;
using RTS.Core.Events;
using RTS.Core.Services;
using RTS.Managers;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RTS.UI
{
    /// <summary>
    /// UI panel that displays wall upgrade options (tower/gate).
    /// Shows when a wall is selected.
    /// </summary>
    public class WallUpgradeUI : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Upgrade Buttons")]
        [SerializeField] private Transform upgradeButtonContainer;
        [SerializeField] private GameObject upgradeButtonPrefab;

        [Header("References")]
        [SerializeField] private BuildingManager buildingManager;

        private GameObject currentSelectedWall;
        private Building wallBuildingComponent;
        private List<GameObject> spawnedButtons = new List<GameObject>();
        private IResourcesService resourceService;

        private void Start()
        {
            // Find building manager if not assigned
            if (buildingManager == null)
            {
                buildingManager = Object.FindAnyObjectByType<BuildingManager>();
            }

            // Get resource service
            resourceService = ServiceLocator.TryGet<IResourcesService>();

            HidePanel();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<BuildingSelectedEvent>(OnBuildingSelected);
            EventBus.Subscribe<BuildingDeselectedEvent>(OnBuildingDeselected);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<BuildingSelectedEvent>(OnBuildingSelected);
            EventBus.Unsubscribe<BuildingDeselectedEvent>(OnBuildingDeselected);
        }

        private void OnBuildingSelected(BuildingSelectedEvent evt)
        {
            currentSelectedWall = evt.Building;
            wallBuildingComponent = evt.Building.GetComponent<Building>();

            // Only show upgrade UI if this is a wall
            if (IsWall(evt.Building))
            {
                ShowUpgradeOptions();
            }
            else
            {
                HidePanel();
            }
        }

        private void OnBuildingDeselected(BuildingDeselectedEvent evt)
        {
            if (currentSelectedWall == evt.Building)
            {
                HidePanel();
                currentSelectedWall = null;
                wallBuildingComponent = null;
            }
        }

        private bool IsWall(GameObject building)
        {
            // Check if this building has WallConnectionSystem component
            return building.GetComponent<WallConnectionSystem>() != null;
        }

        private void ShowUpgradeOptions()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (titleText != null)
            {
                titleText.text = "Upgrade Wall";
            }

            ClearUpgradeButtons();
            CreateUpgradeButtons();
        }

        private void CreateUpgradeButtons()
        {
            if (buildingManager == null || upgradeButtonContainer == null || upgradeButtonPrefab == null)
            {
                return;
            }

            // Get all towers and gates from building manager
            var allBuildings = buildingManager.GetAllBuildingData();

            foreach (var buildingData in allBuildings)
            {
                if (buildingData == null) continue;

                // Check if this is a tower or gate that can replace walls
                bool isTower = buildingData is TowerDataSO towerData && towerData.canReplaceWalls;
                bool isGate = buildingData is GateDataSO gateData && gateData.canReplaceWalls;

                if (isTower || isGate)
                {
                    CreateUpgradeButton(buildingData);
                }
            }
        }

        private void CreateUpgradeButton(BuildingDataSO buildingData)
        {
            GameObject buttonObj = Instantiate(upgradeButtonPrefab, upgradeButtonContainer);

            if (buttonObj.TryGetComponent<WallUpgradeButton>(out var upgradeButton))
            {
                upgradeButton.Initialize(buildingData, currentSelectedWall, resourceService);
            }

            spawnedButtons.Add(buttonObj);
        }

        private void ClearUpgradeButtons()
        {
            foreach (var button in spawnedButtons)
            {
                if (button != null)
                {
                    Destroy(button);
                }
            }
            spawnedButtons.Clear();
        }

        private void HidePanel()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            ClearUpgradeButtons();
        }
    }
}
