using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTS.Buildings;
using RTS.Core.Services;
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
        }

        private void Update()
        {
            // Update affordability color in real-time
            UpdateAffordability();
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

            // Update cost
            if (costText != null)
            {
                costText.text = GetCostString();
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

        private string GetCostString()
        {
            var costs = new List<string>();

            if (unitData.woodCost > 0) costs.Add($"🪵 {unitData.woodCost}");
            if (unitData.foodCost > 0) costs.Add($"🍖 {unitData.foodCost}");
            if (unitData.goldCost > 0) costs.Add($"💰 {unitData.goldCost}");
            if (unitData.stoneCost > 0) costs.Add($"🪨 {unitData.stoneCost}");

            return string.Join(" ", costs);
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
