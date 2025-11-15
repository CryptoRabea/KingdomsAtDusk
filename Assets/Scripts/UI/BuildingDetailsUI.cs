using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using RTS.Buildings;
using RTS.Core.Events;

namespace RTS.UI
{
    /// <summary>
    /// UI panel that displays building details and unit training options.
    /// Shows when a building is selected.
    /// </summary>
    public class BuildingDetailsUI : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TextMeshProUGUI buildingNameText;
        [SerializeField] private TextMeshProUGUI buildingDescriptionText;
        [SerializeField] private Image buildingIcon;

        [Header("Training Queue Display")]
        [SerializeField] private GameObject trainingQueuePanel;
        [SerializeField] private TextMeshProUGUI queueCountText;
        [SerializeField] private Image trainingProgressBar;
        [SerializeField] private TextMeshProUGUI currentTrainingText;

        [Header("Unit Training Buttons")]
        [SerializeField] private Transform unitButtonContainer;
        [SerializeField] private GameObject trainUnitButtonPrefab;

        [Header("Rally Point Button")]
        [SerializeField] private GameObject setRallyPointButton;
        [SerializeField] private TextMeshProUGUI setRallyPointButtonText;

        [Header("References")]
        [SerializeField] private BuildingSelectionManager selectionManager;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        private GameObject currentSelectedBuilding;
        private Building buildingComponent;
        private UnitTrainingQueue trainingQueue;
        private List<GameObject> spawnedButtons = new List<GameObject>();
        private bool isSettingRallyPoint = false;

        private void OnEnable()
        {
            EventBus.Subscribe<BuildingSelectedEvent>(OnBuildingSelected);
            EventBus.Subscribe<BuildingDeselectedEvent>(OnBuildingDeselected);
            EventBus.Subscribe<TrainingProgressEvent>(OnTrainingProgress);

            if (enableDebugLogs)
                Debug.Log($"✅ BuildingDetailsUI subscribed to events on {gameObject.name}");
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<BuildingSelectedEvent>(OnBuildingSelected);
            EventBus.Unsubscribe<BuildingDeselectedEvent>(OnBuildingDeselected);
            EventBus.Unsubscribe<TrainingProgressEvent>(OnTrainingProgress);

            if (enableDebugLogs)
                Debug.Log($"❌ BuildingDetailsUI unsubscribed from events on {gameObject.name}");
        }

        private void Start()
        {
            if (enableDebugLogs)
            {
                Debug.Log($"BuildingDetailsUI.Start() on {gameObject.name}");
                Debug.Log($"  - panelRoot: {(panelRoot != null ? panelRoot.name : "NULL")}");
                Debug.Log($"  - Component enabled: {enabled}");
                Debug.Log($"  - GameObject active: {gameObject.activeInHierarchy}");
            }

            // Find selection manager if not assigned
            if (selectionManager == null)
            {
                selectionManager = Object.FindAnyObjectByType<BuildingSelectionManager>();
                if (selectionManager == null && enableDebugLogs)
                {
                    Debug.LogWarning("BuildingDetailsUI: BuildingSelectionManager not found in scene!");
                }
            }

            // Set up rally point button click handler
            if (setRallyPointButton != null)
            {
                var button = setRallyPointButton.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(OnSetRallyPointButtonClicked);
                }
                setRallyPointButton.SetActive(false); // Hide initially
            }

            HidePanel();
        }

        private void Update()
        {
            // Update training queue display if a building is selected
            if (currentSelectedBuilding != null && trainingQueue != null)
            {
                UpdateTrainingQueueDisplay();
            }

            // Sync rally point mode with selection manager
            if (selectionManager != null && isSettingRallyPoint != selectionManager.IsSpawnPointMode())
            {
                isSettingRallyPoint = selectionManager.IsSpawnPointMode();
                UpdateRallyPointButtonText();
            }
        }

        private void OnBuildingSelected(BuildingSelectedEvent evt)
        {
            if (enableDebugLogs)
                Debug.Log($"🟢 BuildingDetailsUI received BuildingSelectedEvent for: {evt.Building.name}");

            currentSelectedBuilding = evt.Building;
            buildingComponent = evt.Building.GetComponent<Building>();
            trainingQueue = evt.Building.GetComponent<UnitTrainingQueue>();

            if (enableDebugLogs)
            {
                Debug.Log($"  - Building component: {(buildingComponent != null ? "Found" : "NULL")}");
                Debug.Log($"  - Building Data: {(buildingComponent?.Data != null ? buildingComponent.Data.buildingName : "NULL")}");
            }

            if (buildingComponent != null && buildingComponent.Data != null)
            {
                ShowBuildingDetails(buildingComponent);
            }
            else
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"⚠️ Cannot show details - Building component or Data is null!");
            }
        }

        private void OnBuildingDeselected(BuildingDeselectedEvent evt)
        {
            if (enableDebugLogs)
                Debug.Log($"🔴 BuildingDetailsUI received BuildingDeselectedEvent for: {evt.Building.name}");

            if (currentSelectedBuilding == evt.Building)
            {
                HidePanel();
                currentSelectedBuilding = null;
                buildingComponent = null;
                trainingQueue = null;
            }
        }

        private void OnTrainingProgress(TrainingProgressEvent evt)
        {
            // Only update if this is our selected building
            if (evt.Building == currentSelectedBuilding)
            {
                UpdateTrainingQueueDisplay();
            }
        }

        private void ShowBuildingDetails(Building building)
        {
            if (building?.Data == null)
            {
                if (enableDebugLogs)
                    Debug.LogWarning("⚠️ ShowBuildingDetails called with null building or data!");
                return;
            }

            var data = building.Data;

            if (enableDebugLogs)
                Debug.Log($"📋 Showing building details for: {data.buildingName}");

            // Show panel
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                if (enableDebugLogs)
                    Debug.Log($"✅ Panel shown: {panelRoot.name} SetActive(true)");
            }
            else
            {
                if (enableDebugLogs)
                    Debug.LogError("❌ CRITICAL: panelRoot is NULL! Cannot show panel!");
            }

            // Update building info
            if (buildingNameText != null)
            {
                buildingNameText.text = data.buildingName;
            }

            if (buildingDescriptionText != null)
            {
                buildingDescriptionText.text = data.description;
            }

            if (buildingIcon != null && data.icon != null)
            {
                buildingIcon.sprite = data.icon;
                buildingIcon.enabled = true;
            }
            else if (buildingIcon != null)
            {
                buildingIcon.enabled = false;
            }

            // Show training options if building can train units
            if (data.canTrainUnits && trainingQueue != null)
            {
                ShowTrainingOptions(data);
                if (trainingQueuePanel != null)
                {
                    trainingQueuePanel.SetActive(true);
                }

                // Show rally point button for buildings with training queue
                if (setRallyPointButton != null)
                {
                    setRallyPointButton.SetActive(true);
                    UpdateRallyPointButtonText();
                }
            }
            else
            {
                ClearTrainingButtons();
                if (trainingQueuePanel != null)
                {
                    trainingQueuePanel.SetActive(false);
                }

                // Hide rally point button for buildings without training queue
                if (setRallyPointButton != null)
                {
                    setRallyPointButton.SetActive(false);
                }
            }
        }

        private void ShowTrainingOptions(BuildingDataSO data)
        {
            ClearTrainingButtons();

            if (unitButtonContainer == null || trainUnitButtonPrefab == null)
            {
                Debug.LogWarning("BuildingDetailsUI: Missing button container or prefab");
                return;
            }

            // Create a button for each trainable unit
            foreach (var trainableUnit in data.trainableUnits)
            {
                if (trainableUnit?.unitConfig == null) continue;

                GameObject buttonObj = Instantiate(trainUnitButtonPrefab, unitButtonContainer);
                var button = buttonObj.GetComponent<TrainUnitButton>();

                if (button != null)
                {
                    button.Initialize(trainableUnit, trainingQueue);
                    spawnedButtons.Add(buttonObj);
                }
            }
        }

        private void UpdateTrainingQueueDisplay()
        {
            if (trainingQueue == null) return;

            // Update queue count
            if (queueCountText != null)
            {
                queueCountText.text = $"Queue: {trainingQueue.QueueCount}";
            }

            // Update current training progress
            if (trainingQueue.CurrentTraining != null)
            {
                if (currentTrainingText != null)
                {
                    string unitName = trainingQueue.CurrentTraining.unitData?.unitConfig?.unitName ?? "Unit";
                    currentTrainingText.text = $"Training: {unitName}";
                }

                if (trainingProgressBar != null)
                {
                    trainingProgressBar.fillAmount = trainingQueue.CurrentTraining.Progress;
                }
            }
            else
            {
                if (currentTrainingText != null)
                {
                    currentTrainingText.text = trainingQueue.QueueCount > 0 ? "Starting..." : "Idle";
                }

                if (trainingProgressBar != null)
                {
                    trainingProgressBar.fillAmount = 0f;
                }
            }
        }

        private void ClearTrainingButtons()
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
            if (enableDebugLogs)
                Debug.Log($"🙈 Hiding building details panel");

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
                if (enableDebugLogs)
                    Debug.Log($"✅ Panel hidden: {panelRoot.name} SetActive(false)");
            }

            ClearTrainingButtons();

            // Reset rally point mode when hiding panel
            if (isSettingRallyPoint && selectionManager != null)
            {
                selectionManager.SetSpawnPointMode(false);
                isSettingRallyPoint = false;
                UpdateRallyPointButtonText();
            }
        }

        private void OnSetRallyPointButtonClicked()
        {
            if (selectionManager == null)
            {
                if (enableDebugLogs)
                    Debug.LogWarning("Cannot set rally point - BuildingSelectionManager not found!");
                return;
            }

            // Toggle rally point setting mode
            isSettingRallyPoint = !isSettingRallyPoint;
            selectionManager.SetSpawnPointMode(isSettingRallyPoint);
            UpdateRallyPointButtonText();

            if (enableDebugLogs)
            {
                Debug.Log($"Rally point mode: {(isSettingRallyPoint ? "ENABLED" : "DISABLED")}");
            }
        }

        private void UpdateRallyPointButtonText()
        {
            if (setRallyPointButtonText != null)
            {
                setRallyPointButtonText.text = isSettingRallyPoint ? "Cancel" : "Set Rally Point";
            }
        }
    }
}
