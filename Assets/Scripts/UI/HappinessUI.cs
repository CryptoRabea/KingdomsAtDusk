using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTS.Core.Events;
using RTS.Core.Services;
using System.Collections.Generic;
using System.Linq;

namespace RTS.UI
{
    public class HappinessUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI happinessText;
        [SerializeField] private Slider happinessSlider;

        [Header("Format Settings")]
        [SerializeField] private string textFormat = "Happiness: {0}%";

        [Header("Colors")]
        [SerializeField] private Color highHappinessColor = Color.green;
        [SerializeField] private Color mediumHappinessColor = Color.yellow;
        [SerializeField] private Color lowHappinessColor = Color.red;
        [SerializeField] private float highThreshold = 70f;
        [SerializeField] private float lowThreshold = 30f;

        private void OnEnable()
        {
            EventBus.Subscribe<HappinessChangedEvent>(OnHappinessChanged);

            var happinessService = ServiceLocator.TryGet<IHappinessService>();
            if (happinessService != null)
            {
                UpdateDisplay(happinessService.CurrentHappiness);
            }
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<HappinessChangedEvent>(OnHappinessChanged);
        }

        private void OnHappinessChanged(HappinessChangedEvent evt)
        {
            UpdateDisplay(evt.NewHappiness);
        }

        private void UpdateDisplay(float happiness)
        {
            if (happinessText != null)
            {
                happinessText.text = string.Format(textFormat, Mathf.RoundToInt(happiness));
                happinessText.color = GetHappinessColor(happiness);
            }

            if (happinessSlider != null)
            {
                happinessSlider.value = happiness / 100f;

                if (happinessSlider.fillRect.TryGetComponent<Image>(out var fillImage))
                {
                    fillImage.color = GetHappinessColor(happiness);
                }
            }
        }

        private Color GetHappinessColor(float happiness)
        {
            if (happiness >= highThreshold)
                return highHappinessColor;
            else if (happiness <= lowThreshold)
                return lowHappinessColor;
            else
            {
                float t = (happiness - lowThreshold) / (highThreshold - lowThreshold);
                return Color.Lerp(mediumHappinessColor, highHappinessColor, t);
            }
        }
    }
}