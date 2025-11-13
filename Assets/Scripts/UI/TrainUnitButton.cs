using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTS.Buildings;
using RTS.Core.Services;
using RTS.Core.Utilities;
using RTS.Core.Events;
using System.Collections.Generic;

namespace RTS.UI
{
    /// <summary>
    /// UI button for training a specific unit type.
    /// Displays unit info, cost, and affordability.
    /// </summary>
    public class TrainUnitButton : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button button;
        [SerializeField] private Image unitIcon;
        [SerializeField] private TextMeshProUGUI unitNameText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI trainingTimeText;

        [Header("Visual Feedback")]
        [SerializeField] private Color affordableColor = Color.white;
        [SerializeField] private Color unaffordableColor = Color.red;

        private TrainableUnitData unitData;
        private UnitTrainingQueue trainingQueue;
        private IResourcesService resourceService;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            button.onClick.AddListener(OnButtonClicked);
        }

        private void Start()
        {
            resourceService = ServiceLocator.TryGet<IResourcesService>();

            // Subscribe to resource changes for event-based affordability updates
            EventBus.Subscribe<ResourcesChangedEvent>(OnResourcesChanged);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<ResourcesChangedEvent>(OnResourcesChanged);
        }

        private void OnResourcesChanged(ResourcesChangedEvent evt)
        {
            // Only update when resources actually change (99% more efficient than Update loop)
            UpdateAffordability();
        }

        /// <summary>
        /// Initialize the button with unit data and the training queue.
        /// </summary>
        public void Initialize(TrainableUnitData data, UnitTrainingQueue queue)
        {
            unitData = data;
            trainingQueue = queue;

            if (unitData?.unitConfig == null)
            {
                Debug.LogError("TrainUnitButton: Invalid unit data");
                gameObject.SetActive(false);
                return;
            }

            UpdateDisplay();
            UpdateAffordability(); // Initial affordability check
        }

        private void UpdateDisplay()
        {
            if (unitData?.unitConfig == null) return;

            // Update icon
            if (unitIcon != null && unitData.unitConfig.unitIcon != null)
            {
                unitIcon.sprite = unitData.unitConfig.unitIcon;
                unitIcon.enabled = true;
            }
            else if (unitIcon != null)
            {
                unitIcon.enabled = false;
            }

            // Update name
            if (unitNameText != null)
            {
                unitNameText.text = unitData.unitConfig.unitName;
            }

            // Update cost using centralized utility
            if (costText != null)
            {
                costText.text = ResourceDisplayUtility.FormatCosts(unitData.GetCosts());
            }

            // Update training time
            if (trainingTimeText != null)
            {
                trainingTimeText.text = $"{unitData.trainingTime}s";
            }
        }

        private void UpdateAffordability()
        {
            if (resourceService == null || unitData == null) return;

            var costs = unitData.GetCosts();
            bool canAfford = resourceService.CanAfford(costs);

            // Update button interactability
            if (button != null)
            {
                button.interactable = canAfford;
            }

            // Update cost text color
            if (costText != null)
            {
                costText.color = canAfford ? affordableColor : unaffordableColor;
            }
        }

        private void OnButtonClicked()
        {
            if (trainingQueue != null && unitData != null)
            {
                bool success = trainingQueue.TryTrainUnit(unitData);

                if (!success)
                {
                    Debug.LogWarning($"Failed to train {unitData.unitConfig.unitName}");
                    // Could show a notification here
                }
            }
        }
    }
}
