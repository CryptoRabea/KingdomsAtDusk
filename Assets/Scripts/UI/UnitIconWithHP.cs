using UnityEngine;
using UnityEngine.UI;
using RTS.Units;

namespace RTS.UI
{
    /// <summary>
    /// Represents a single unit icon with HP bar in the multi-unit selection display.
    /// Shows unit portrait and current health status.
    /// </summary>
    public class UnitIconWithHP : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image unitIcon;
        [SerializeField] private Image hpBarFill;
        [SerializeField] private Image hpBarBackground;

        [Header("Health Bar Colors")]
        [SerializeField] private Color healthyColor = Color.green;
        [SerializeField] private Color damagedColor = Color.yellow;
        [SerializeField] private Color criticalColor = Color.red;

        private GameObject trackedUnit;
        private UnitHealth unitHealth;
        private UnitConfigSO unitConfig;

        /// <summary>
        /// Initialize the unit icon with a specific unit.
        /// </summary>
        public void Initialize(GameObject unit)
        {
            if (unit == null)
            {
                return;
            }

            trackedUnit = unit;

            // Get unit components
            var aiController = unit.GetComponent<RTS.Units.AI.UnitAIController>();
            if (aiController == null || aiController.Config == null)
            {
                return;
            }

            unitConfig = aiController.Config;
            unitHealth = unit.GetComponent<UnitHealth>();

            // Set unit icon
            if (unitIcon != null && unitConfig.unitIcon != null)
            {
                unitIcon.sprite = unitConfig.unitIcon;
                unitIcon.color = Color.white;
            }

            // Configure HP bar fill to use RectTransform scaling
            ConfigureHPBar();

            // Update HP bar immediately
            UpdateHealthBar();
        }

        private void ConfigureHPBar()
        {
            if (hpBarFill != null)
            {
                RectTransform fillRect = hpBarFill.GetComponent<RectTransform>();
                if (fillRect != null)
                {
                    // Set anchors to stretch from left
                    fillRect.anchorMin = new Vector2(0, 0);
                    fillRect.anchorMax = new Vector2(0, 1);
                    fillRect.pivot = new Vector2(0, 0.5f);
                    fillRect.anchoredPosition = Vector2.zero;

                    // Get the parent width to set the fill width
                    RectTransform parentRect = hpBarFill.transform.parent.GetComponent<RectTransform>();
                    if (parentRect != null)
                    {
                        fillRect.sizeDelta = new Vector2(parentRect.rect.width, 0);
                    }
                }
            }
        }

        private void Update()
        {
            // Update HP bar each frame to reflect current health
            if (trackedUnit != null)
            {
                UpdateHealthBar();
            }
        }

        /// <summary>
        /// Update the health bar display based on current unit health.
        /// </summary>
        public void UpdateHealthBar()
        {
            if (hpBarFill == null || unitConfig == null)
                return;

            float currentHealth = unitConfig.maxHealth;
            float maxHealth = unitConfig.maxHealth;

            if (unitHealth != null)
            {
                currentHealth = unitHealth.CurrentHealth;
                maxHealth = unitHealth.MaxHealth;
            }

            if (maxHealth <= 0)
                return;

            float healthPercent = currentHealth / maxHealth;

            // Scale the fill image horizontally based on health percentage
            RectTransform fillRect = hpBarFill.GetComponent<RectTransform>();
            if (fillRect != null)
            {
                fillRect.localScale = new Vector3(healthPercent, 1, 1);
            }

            // Change color based on health percentage
            if (healthPercent > 0.6f)
            {
                hpBarFill.color = healthyColor;
            }
            else if (healthPercent > 0.3f)
            {
                hpBarFill.color = damagedColor;
            }
            else
            {
                hpBarFill.color = criticalColor;
            }
        }

        /// <summary>
        /// Get the unit this icon is tracking.
        /// </summary>
        public GameObject GetTrackedUnit()
        {
            return trackedUnit;
        }

        /// <summary>
        /// Check if the tracked unit is still valid (not destroyed).
        /// </summary>
        public bool IsValid()
        {
            return trackedUnit != null;
        }

        /// <summary>
        /// Clear the icon and release references.
        /// </summary>
        public void Clear()
        {
            trackedUnit = null;
            unitHealth = null;
            unitConfig = null;

            if (unitIcon != null)
            {
                unitIcon.sprite = null;
            }
        }
    }
}
