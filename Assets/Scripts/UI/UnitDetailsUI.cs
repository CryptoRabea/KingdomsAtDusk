using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTS.Core.Events;
using RTS.Units;
using RTS.Units.Formation;
using System.Collections.Generic;
using System;

namespace RTS.UI
{
    /// <summary>
    /// Displays detailed information about a selected unit.
    /// Shows unit stats from UnitConfigSO when a unit is selected.
    ///
    /// MULTI-UNIT SELECTION:
    /// - When 1 unit selected: Shows detailed stats
    /// - When 2+ units selected: Shows grid of unit icons with HP bars
    /// - Formation buttons remain visible in both modes
    /// </summary>
    public class UnitDetailsUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject unitDetailsPanel;
        [SerializeField] private Image unitPortrait;
        [SerializeField] private TextMeshProUGUI unitNameText;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI speedText;
        [SerializeField] private TextMeshProUGUI attackDamageText;
        [SerializeField] private TextMeshProUGUI attackSpeedText;
        [SerializeField] private TextMeshProUGUI attackRangeText;

        [Header("Multi-Unit Selection")]
        [SerializeField] private GameObject singleUnitStatsContainer;
        [SerializeField] private GameObject multiUnitSelectionContainer;
        [SerializeField] private MultiUnitSelectionUI multiUnitSelectionUI;

        [Header("Formation")]
        [SerializeField] private TMP_Dropdown formationDropdown;
        [SerializeField] private FormationGroupManager formationGroupManager;
        [SerializeField] private Button customFormationButton;
        [SerializeField] private Button createFormationButton;
        [SerializeField] private FormationSelectorUI formationSelector;
        [SerializeField] private FormationBuilderUI formationBuilder;

        [Header("Health Bar")]
        [SerializeField] private Image healthBarFill;
        [SerializeField] private Color healthyColor = Color.green;
        [SerializeField] private Color damagedColor = Color.yellow;
        [SerializeField] private Color criticalColor = Color.red;

        private GameObject currentSelectedUnit;
        private UnitHealth currentUnitHealth;
        private int currentSelectionCount = 0;

        private void OnEnable()
        {
            EventBus.Subscribe<UnitSelectedEvent>(OnUnitSelected);
            EventBus.Subscribe<UnitDeselectedEvent>(OnUnitDeselected);
            EventBus.Subscribe<UnitHealthChangedEvent>(OnUnitHealthChanged);
            EventBus.Subscribe<SelectionChangedEvent>(OnSelectionChanged);
            EventBus.Subscribe<FormationChangedEvent>(OnFormationChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<UnitSelectedEvent>(OnUnitSelected);
            EventBus.Unsubscribe<UnitDeselectedEvent>(OnUnitDeselected);
            EventBus.Unsubscribe<UnitHealthChangedEvent>(OnUnitHealthChanged);
            EventBus.Unsubscribe<SelectionChangedEvent>(OnSelectionChanged);
            EventBus.Unsubscribe<FormationChangedEvent>(OnFormationChanged);
        }

        private void Start()
        {
            // Hide panel initially
            if (unitDetailsPanel != null)
            {
                unitDetailsPanel.SetActive(false);
            }

            // Configure health bar fill image to work as a horizontal slider
            // We'll use RectTransform scaling for the slider effect
            if (healthBarFill != null)
            {
                RectTransform fillRect = healthBarFill.GetComponent<RectTransform>();
                if (fillRect != null)
                {
                    // Set anchors to stretch from left
                    fillRect.anchorMin = new Vector2(0, 0);
                    fillRect.anchorMax = new Vector2(0, 1);
                    fillRect.pivot = new Vector2(0, 0.5f);
                    fillRect.anchoredPosition = Vector2.zero;

                    // Get the parent width to set the fill width
                    RectTransform parentRect = healthBarFill.transform.parent.GetComponent<RectTransform>();
                    if (parentRect != null)
                    {
                        fillRect.sizeDelta = new Vector2(parentRect.rect.width, 0);
                    }
                }
            }

            // Initialize formation dropdown
            InitializeFormationDropdown();

            // Setup custom formation buttons
            if (customFormationButton != null)
            {
                customFormationButton.onClick.AddListener(OnCustomFormationButtonClicked);
            }

            if (createFormationButton != null)
            {
                createFormationButton.onClick.AddListener(OnCreateFormationButtonClicked);
            }
        }

        private void InitializeFormationDropdown()
        {
            if (formationDropdown == null) return;

            // Clear existing options
            formationDropdown.ClearOptions();

            // Add all formation types
            List<string> options = new List<string>();
            foreach (FormationType formationType in Enum.GetValues(typeof(FormationType)))
            {
                options.Add(FormatFormationName(formationType));
            }

            formationDropdown.AddOptions(options);

            // Set current formation
            if (formationGroupManager != null)
            {
                formationDropdown.value = (int)formationGroupManager.CurrentFormation;
                formationDropdown.RefreshShownValue();
            }

            // Add listener
            formationDropdown.onValueChanged.AddListener(OnFormationDropdownChanged);
        }

        private string FormatFormationName(FormationType type)
        {
            switch (type)
            {
                case FormationType.None: return "No Formation";
                case FormationType.Line: return "Line";
                case FormationType.Column: return "Column";
                case FormationType.Box: return "Box";
                case FormationType.Wedge: return "Wedge";
                case FormationType.Circle: return "Circle";
                case FormationType.Scatter: return "Scatter";
                default: return type.ToString();
            }
        }

        private void OnFormationDropdownChanged(int index)
        {
            if (formationGroupManager != null)
            {
                FormationType newFormation = (FormationType)index;
                formationGroupManager.CurrentFormation = newFormation;
            }
        }

        private void OnFormationChanged(FormationChangedEvent evt)
        {
            // Update dropdown to match current formation (in case it was changed elsewhere)
            if (formationDropdown != null)
            {
                formationDropdown.value = (int)evt.FormationType;
                formationDropdown.RefreshShownValue();
            }
        }

        private void OnUnitSelected(UnitSelectedEvent evt)
        {
            // For multi-select, only show details for the first selected unit
            if (currentSelectedUnit == null)
            {
                currentSelectedUnit = evt.Unit;
                ShowUnitDetails(evt.Unit);
            }
        }

        private void OnUnitDeselected(UnitDeselectedEvent evt)
        {
            // Only hide if this was the unit we're showing
            if (currentSelectedUnit == evt.Unit)
            {
                HideUnitDetails();
            }
        }

        private void OnSelectionChanged(SelectionChangedEvent evt)
        {
            currentSelectionCount = evt.SelectionCount;

            // Hide details when selection is cleared
            if (evt.SelectionCount == 0)
            {
                HideUnitDetails();
            }
            else if (evt.SelectionCount == 1)
            {
                // Show single unit stats
                ShowSingleUnitMode();
            }
            else
            {
                // Show multi-unit selection grid
                ShowMultiUnitMode();
            }
        }

        private void OnUnitHealthChanged(UnitHealthChangedEvent evt)
        {
            // Update health display if this is the current unit
            if (currentSelectedUnit != null && evt.Unit == currentSelectedUnit)
            {
                UpdateHealthDisplay(evt.CurrentHealth, evt.MaxHealth);
            }
        }

        private void ShowUnitDetails(GameObject unit)
        {
            if (unit == null)
            {
                HideUnitDetails();
                return;
            }

            // Get the UnitAIController component which has the config
            var unitAI = unit.GetComponent<RTS.Units.AI.UnitAIController>();
            if (unitAI == null || unitAI.Config == null)
            {
                Debug.LogWarning("Selected unit doesn't have a UnitAIController component or UnitConfigSO!");
                HideUnitDetails();
                return;
            }

            UnitConfigSO config = unitAI.Config;

            // Show panel
            if (unitDetailsPanel != null)
            {
                unitDetailsPanel.SetActive(true);
            }

            // Set unit portrait
            if (unitPortrait != null && config.unitIcon != null)
            {
                unitPortrait.sprite = config.unitIcon;
                unitPortrait.color = Color.white;
            }

            // Set unit name
            if (unitNameText != null)
            {
                unitNameText.text = config.unitName;
            }

            // Get current health
            currentUnitHealth = unit.GetComponent<UnitHealth>();
            float currentHealth = config.maxHealth;
            float maxHealth = config.maxHealth;

            if (currentUnitHealth != null)
            {
                currentHealth = currentUnitHealth.CurrentHealth;
                maxHealth = currentUnitHealth.MaxHealth;
            }

            // Set stats
            if (healthText != null)
            {
                healthText.text = $"Health: {currentHealth:F0}/{maxHealth:F0}";
            }

            if (speedText != null)
            {
                speedText.text = $"Speed: {config.speed:F1}";
            }

            if (attackDamageText != null)
            {
                attackDamageText.text = $"Attack Damage: {config.attackDamage:F0}";
            }

            if (attackSpeedText != null)
            {
                // attackRate is attacks per second, so attack speed in seconds is 1/attackRate
                float attackSpeed = config.attackRate > 0 ? 1f / config.attackRate : 0f;
                attackSpeedText.text = $"Attack Speed: {attackSpeed:F2}s";
            }

            if (attackRangeText != null)
            {
                attackRangeText.text = $"Attack Range: {config.attackRange:F1}";
            }

            // Update health bar
            UpdateHealthDisplay(currentHealth, maxHealth);
        }

        private void UpdateHealthDisplay(float currentHealth, float maxHealth)
        {
            // Update health text
            if (healthText != null)
            {
                healthText.text = $"Health: {currentHealth:F0}/{maxHealth:F0}";
            }

            // Update health bar fill using RectTransform scaling
            if (healthBarFill != null && maxHealth > 0)
            {
                float healthPercent = currentHealth / maxHealth;

                // Scale the fill image horizontally based on health percentage
                RectTransform fillRect = healthBarFill.GetComponent<RectTransform>();
                if (fillRect != null)
                {
                    fillRect.localScale = new Vector3(healthPercent, 1, 1);
                }

                // Change color based on health percentage
                if (healthPercent > 0.6f)
                {
                    healthBarFill.color = healthyColor;
                }
                else if (healthPercent > 0.3f)
                {
                    healthBarFill.color = damagedColor;
                }
                else
                {
                    healthBarFill.color = criticalColor;
                }
            }
        }

        private void HideUnitDetails()
        {
            currentSelectedUnit = null;
            currentUnitHealth = null;
            currentSelectionCount = 0;

            if (unitDetailsPanel != null)
            {
                unitDetailsPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Show single unit stats mode (hides multi-unit grid)
        /// </summary>
        private void ShowSingleUnitMode()
        {
            if (singleUnitStatsContainer != null)
            {
                singleUnitStatsContainer.SetActive(true);
            }

            if (multiUnitSelectionContainer != null)
            {
                multiUnitSelectionContainer.SetActive(false);
            }
        }

        /// <summary>
        /// Show multi-unit selection grid mode (hides single unit stats)
        /// </summary>
        private void ShowMultiUnitMode()
        {
            if (singleUnitStatsContainer != null)
            {
                singleUnitStatsContainer.SetActive(false);
            }

            if (multiUnitSelectionContainer != null)
            {
                multiUnitSelectionContainer.SetActive(true);
            }

            // Force refresh the multi-unit UI
            if (multiUnitSelectionUI != null)
            {
                multiUnitSelectionUI.ForceRefresh();
            }
        }

        /// <summary>
        /// Called when the custom formation button is clicked
        /// Opens the formation selector to browse and select saved formations
        /// </summary>
        private void OnCustomFormationButtonClicked()
        {
            if (formationSelector != null)
            {
                formationSelector.OpenSelector();
            }
            else
            {
                Debug.LogWarning("FormationSelectorUI is not assigned in UnitDetailsUI!");
            }
        }

        /// <summary>
        /// Called when the create formation button is clicked
        /// Opens the formation builder to create a new custom formation
        /// </summary>
        private void OnCreateFormationButtonClicked()
        {
            if (formationBuilder != null)
            {
                formationBuilder.OpenBuilder();
            }
            else
            {
                Debug.LogWarning("FormationBuilderUI is not assigned in UnitDetailsUI!");
            }
        }
    }
}
