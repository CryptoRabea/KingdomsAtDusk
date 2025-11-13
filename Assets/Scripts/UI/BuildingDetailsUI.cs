using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTS.Buildings;
using RTS.Core.Events;

namespace RTS.UI
{
    /// <summary>
    /// UI panel that displays building details and unit training options.
    /// Shows when a building is selected.
    /// Simplified to use pre-existing UI elements instead of dynamic instantiation.
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

        [Header("Unit Training Buttons - Pre-existing in UI")]
        [SerializeField] private TrainUnitButton[] trainUnitButtons;

        [Header("Spawn Point Button")]
        [SerializeField] private GameObject setSpawnPointButton;
        [SerializeField] private TextMeshProUGUI setSpawnPointButtonText;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;

        private GameObject currentSelectedBuilding;
        private Building buildingComponent;
        private UnitTrainingQueue trainingQueue;
        private BuildingSelectionManager selectionManager;
        private bool isSettingSpawnPoint = false;

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
            }

            // Find the BuildingSelectionManager in the scene
            selectionManager = FindObjectOfType<BuildingSelectionManager>();
            if (selectionManager == null && enableDebugLogs)
            {
                Debug.LogWarning("BuildingDetailsUI: Could not find BuildingSelectionManager in scene");
            }

            // Set up spawn point button click handler
            if (setSpawnPointButton != null)
            {
                var button = setSpawnPointButton.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(OnSetSpawnPointButtonClicked);
                }
                setSpawnPointButton.SetActive(false);
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

            // Sync spawn point mode state with selection manager
            if (selectionManager != null && isSettingSpawnPoint)
            {
                if (!selectionManager.IsSpawnPointMode())
                {
                    isSettingSpawnPoint = false;
                    UpdateSpawnPointButtonText();
                }
            }
        }

        private void OnBuildingSelected(BuildingSelectedEvent evt)
        {
            if (enableDebugLogs)
                Debug.Log($"🟢 BuildingDetailsUI received BuildingSelectedEvent for: {evt.Building.name}");

            currentSelectedBuilding = evt.Building;
            buildingComponent = evt.Building.GetComponent<Building>();
            trainingQueue = evt.Building.GetComponent<UnitTrainingQueue>();

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
                SetupTrainingButtons(data);
                if (trainingQueuePanel != null)
                {
                    trainingQueuePanel.SetActive(true);
                }

                // Show spawn point button
                if (setSpawnPointButton != null)
                {
                    setSpawnPointButton.SetActive(true);
                    UpdateSpawnPointButtonText();
                }
            }
            else
            {
                HideTrainingButtons();
                if (trainingQueuePanel != null)
                {
                    trainingQueuePanel.SetActive(false);
                }

                // Hide spawn point button
                if (setSpawnPointButton != null)
                {
                    setSpawnPointButton.SetActive(false);
                }
            }
        }

        private void SetupTrainingButtons(BuildingDataSO data)
        {
            if (trainUnitButtons == null || trainUnitButtons.Length == 0)
            {
                if (enableDebugLogs)
                    Debug.LogWarning("BuildingDetailsUI: No training buttons assigned in inspector");
                return;
            }

            // Initialize buttons with trainable units
            int buttonIndex = 0;
            foreach (var trainableUnit in data.trainableUnits)
            {
                if (buttonIndex >= trainUnitButtons.Length) break;
                if (trainableUnit?.unitConfig == null) continue;

                var button = trainUnitButtons[buttonIndex];
                if (button != null)
                {
                    button.gameObject.SetActive(true);
                    button.Initialize(trainableUnit, trainingQueue);
                    buttonIndex++;
                }
            }

            // Hide unused buttons
            for (int i = buttonIndex; i < trainUnitButtons.Length; i++)
            {
                if (trainUnitButtons[i] != null)
                {
                    trainUnitButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void HideTrainingButtons()
        {
            if (trainUnitButtons == null) return;

            foreach (var button in trainUnitButtons)
            {
                if (button != null)
                {
                    button.gameObject.SetActive(false);
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

        private void HidePanel()
        {
            if (enableDebugLogs)
                Debug.Log($"🙈 Hiding building details panel");

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            HideTrainingButtons();

            // Reset spawn point mode when hiding panel
            if (isSettingSpawnPoint && selectionManager != null)
            {
                selectionManager.SetSpawnPointMode(false);
                isSettingSpawnPoint = false;
                UpdateSpawnPointButtonText();
            }
        }

        private void OnSetSpawnPointButtonClicked()
        {
            if (selectionManager == null)
            {
                if (enableDebugLogs)
                    Debug.LogWarning("Cannot set spawn point: BuildingSelectionManager not found");
                return;
            }

            // Toggle spawn point setting mode
            isSettingSpawnPoint = !isSettingSpawnPoint;
            selectionManager.SetSpawnPointMode(isSettingSpawnPoint);
            UpdateSpawnPointButtonText();

            if (enableDebugLogs)
            {
                Debug.Log($"Spawn point mode {(isSettingSpawnPoint ? "ENABLED" : "DISABLED")}");
            }
        }

        private void UpdateSpawnPointButtonText()
        {
            if (setSpawnPointButtonText != null)
            {
                setSpawnPointButtonText.text = isSettingSpawnPoint ? "Cancel" : "Set Spawn Point";
            }
        }
    }
}
