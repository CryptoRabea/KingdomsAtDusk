using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTS.Core.Events;
using RTS.Units;

namespace RTS.UI
{
    /// <summary>
    /// Displays detailed information about a selected unit.
    /// Shows unit stats from UnitConfigSO when a unit is selected.
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

        [Header("Health Bar")]
        [SerializeField] private Image healthBarFill;
        [SerializeField] private Color healthyColor = Color.green;
        [SerializeField] private Color damagedColor = Color.yellow;
        [SerializeField] private Color criticalColor = Color.red;

        private GameObject currentSelectedUnit;
        private UnitHealth currentUnitHealth;

        private void OnEnable()
        {
            EventBus.Subscribe<UnitSelectedEvent>(OnUnitSelected);
            EventBus.Subscribe<UnitDeselectedEvent>(OnUnitDeselected);
            EventBus.Subscribe<UnitHealthChangedEvent>(OnUnitHealthChanged);
            EventBus.Subscribe<SelectionChangedEvent>(OnSelectionChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<UnitSelectedEvent>(OnUnitSelected);
            EventBus.Unsubscribe<UnitDeselectedEvent>(OnUnitDeselected);
            EventBus.Unsubscribe<UnitHealthChangedEvent>(OnUnitHealthChanged);
            EventBus.Unsubscribe<SelectionChangedEvent>(OnSelectionChanged);
        }

        private void Start()
        {
            // Hide panel initially
            if (unitDetailsPanel != null)
            {
                unitDetailsPanel.SetActive(false);
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
            // Hide details when selection is cleared
            if (evt.SelectionCount == 0)
            {
                HideUnitDetails();
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

            // Update health bar fill
            if (healthBarFill != null && maxHealth > 0)
            {
                float healthPercent = currentHealth / maxHealth;
                healthBarFill.fillAmount = healthPercent;

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

            if (unitDetailsPanel != null)
            {
                unitDetailsPanel.SetActive(false);
            }
        }
    }
}
