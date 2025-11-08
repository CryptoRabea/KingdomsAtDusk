using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTS.Core.Events;
using RTS.Core.Services;
using System.Collections.Generic;
using System.Linq;

namespace RTS.UI
{
    /// <summary>
    /// NEW FLEXIBLE ResourceUI - Automatically handles ANY number of resources!
    /// Add a new resource to the enum? This UI adapts automatically!
    /// </summary>
    public class ResourceUI : MonoBehaviour
    {
        [System.Serializable]
        public class ResourceDisplay
        {
            [Tooltip("Which resource to display")]
            public ResourceType resourceType;

            [Tooltip("Text component to update")]
            public TextMeshProUGUI textComponent;

            [Tooltip("Optional icon image")]
            public Image iconImage;

            [Tooltip("Format string. {0} = resource name, {1} = amount")]
            public string displayFormat = "{0}: {1}";

            [Tooltip("Show resource name?")]
            public bool showName = true;

            [Header("Animation Settings")]
            public bool animateChanges = true;
            public Color positiveChangeColor = Color.green;
            public Color negativeChangeColor = Color.red;
            public float colorFadeDuration = 0.5f;

            [HideInInspector] public int cachedAmount;
        }

        [Header("Resource Displays")]
        [SerializeField] private ResourceDisplay[] resourceDisplays;

        [Header("Auto-Setup (Optional)")]
        [Tooltip("If enabled, automatically creates displays for all resources")]
        [SerializeField] private bool autoCreateDisplays = false;
        [SerializeField] private Transform displayContainer;
        [SerializeField] private GameObject displayPrefab;

        private IResourceService resourceService;

        private void OnEnable()
        {
            EventBus.Subscribe<ResourcesChangedEvent>(OnResourcesChanged);
            resourceService = ServiceLocator.TryGet<IResourceService>();

            if (autoCreateDisplays && displayContainer != null && displayPrefab != null)
            {
                AutoCreateDisplays();
            }

            InitializeDisplays();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ResourcesChangedEvent>(OnResourcesChanged);
        }

        private void InitializeDisplays()
        {
            if (resourceService == null) return;

            foreach (var display in resourceDisplays)
            {
                if (display != null)
                {
                    display.cachedAmount = resourceService.GetResource(display.resourceType);
                    UpdateDisplay(display);
                }
            }
        }

        private void OnResourcesChanged(ResourcesChangedEvent evt)
        {
            if (resourceService == null) return;

            foreach (var display in resourceDisplays)
            {
                if (display == null) continue;

                int oldAmount = display.cachedAmount;
                int newAmount = resourceService.GetResource(display.resourceType);
                int delta = newAmount - oldAmount;

                display.cachedAmount = newAmount;
                UpdateDisplay(display);

                // Animate if changed
                if (display.animateChanges && delta != 0)
                {
                    AnimateDisplay(display, delta);
                }
            }
        }

        private void UpdateDisplay(ResourceDisplay display)
        {
            if (display.textComponent == null) return;

            string resourceName = display.showName ? display.resourceType.ToString() : "";
            display.textComponent.text = string.Format(
                display.displayFormat,
                resourceName,
                display.cachedAmount
            );
        }

        private void AnimateDisplay(ResourceDisplay display, int delta)
        {
            if (display.textComponent == null) return;

            Color flashColor = delta > 0 ? display.positiveChangeColor : display.negativeChangeColor;
            StartCoroutine(ColorFadeCoroutine(display.textComponent, flashColor, display.colorFadeDuration));
        }

        private System.Collections.IEnumerator ColorFadeCoroutine(TextMeshProUGUI textComponent, Color flashColor, float duration)
        {
            Color originalColor = textComponent.color;
            textComponent.color = flashColor;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                textComponent.color = Color.Lerp(flashColor, originalColor, elapsed / duration);
                yield return null;
            }

            textComponent.color = originalColor;
        }

        /// <summary>
        /// Automatically create displays for all resource types.
        /// Great for prototyping or dynamic UIs!
        /// </summary>
        private void AutoCreateDisplays()
        {
            // Clear existing
            foreach (Transform child in displayContainer)
            {
                Destroy(child.gameObject);
            }

            var displays = new List<ResourceDisplay>();

            // Create display for each resource type
            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            {
                GameObject displayObj = Instantiate(displayPrefab, displayContainer);
                var textComponent = displayObj.GetComponentInChildren<TextMeshProUGUI>();
                var imageComponent = displayObj.GetComponentInChildren<Image>();

                if (textComponent != null)
                {
                    var display = new ResourceDisplay
                    {
                        resourceType = type,
                        textComponent = textComponent,
                        iconImage = imageComponent,
                        displayFormat = "{0}: {1}",
                        showName = true,
                        animateChanges = true
                    };

                    displays.Add(display);
                }
            }

            resourceDisplays = displays.ToArray();
        }

        #region Public API

        /// <summary>
        /// Get display for a specific resource type.
        /// </summary>
        public ResourceDisplay GetDisplay(ResourceType type)
        {
            return resourceDisplays?.FirstOrDefault(d => d.resourceType == type);
        }

        /// <summary>
        /// Manually refresh all displays.
        /// </summary>
        public void RefreshAll()
        {
            InitializeDisplays();
        }

        #endregion
    }

    /// <summary>
    /// Advanced ResourceUI with progress bars and tooltips.
    /// </summary>
    public class ResourceUI_Advanced : MonoBehaviour
    {
        [System.Serializable]
        public class AdvancedResourceDisplay
        {
            public ResourceType resourceType;
            public TextMeshProUGUI textComponent;
            public Image iconImage;
            public Slider progressBar;
            public GameObject tooltipPanel;

            [Header("Limits")]
            public bool hasLimit = false;
            public int maxAmount = 1000;

            [Header("Warning Thresholds")]
            public bool showLowWarning = false;
            public int lowThreshold = 50;
            public Color lowColor = Color.red;
            public Color normalColor = Color.white;

            [HideInInspector] public int cachedAmount;
        }

        [SerializeField] private AdvancedResourceDisplay[] displays;

        private IResourceService resourceService;

        private void OnEnable()
        {
            EventBus.Subscribe<ResourcesChangedEvent>(OnResourcesChanged);
            resourceService = ServiceLocator.TryGet<IResourceService>();
            RefreshAll();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ResourcesChangedEvent>(OnResourcesChanged);
        }

        private void OnResourcesChanged(ResourcesChangedEvent evt)
        {
            RefreshAll();
        }

        private void RefreshAll()
        {
            if (resourceService == null) return;

            foreach (var display in displays)
            {
                if (display == null) continue;

                int amount = resourceService.GetResource(display.resourceType);
                display.cachedAmount = amount;

                UpdateDisplay(display, amount);
            }
        }

        private void UpdateDisplay(AdvancedResourceDisplay display, int amount)
        {
            // Update text
            if (display.textComponent != null)
            {
                if (display.hasLimit)
                {
                    display.textComponent.text = $"{amount} / {display.maxAmount}";
                }
                else
                {
                    display.textComponent.text = amount.ToString();
                }

                // Apply low warning color
                if (display.showLowWarning && amount < display.lowThreshold)
                {
                    display.textComponent.color = display.lowColor;
                }
                else
                {
                    display.textComponent.color = display.normalColor;
                }
            }

            // Update progress bar
            if (display.progressBar != null && display.hasLimit)
            {
                display.progressBar.maxValue = display.maxAmount;
                display.progressBar.value = amount;
            }
        }
    }

    /// <summary>
    /// Displays happiness using the new event system.
    /// Unchanged from original, included for completeness.
    /// </summary>
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

                var fillImage = happinessSlider.fillRect.GetComponent<Image>();
                if (fillImage != null)
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

    /// <summary>
    /// Displays game notifications for important events.
    /// Unchanged from original, included for completeness.
    /// </summary>
    public class NotificationUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI notificationText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Settings")]
        [SerializeField] private float displayDuration = 3f;
        [SerializeField] private float fadeDuration = 0.5f;

        private Queue<string> notificationQueue = new Queue<string>();
        private bool isDisplaying = false;

        private void OnEnable()
        {
            EventBus.Subscribe<BuildingCompletedEvent>(OnBuildingCompleted);
            EventBus.Subscribe<WaveStartedEvent>(OnWaveStarted);
            EventBus.Subscribe<UnitDiedEvent>(OnUnitDied);
            EventBus.Subscribe<ResourcesSpentEvent>(OnResourcesSpent);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<BuildingCompletedEvent>(OnBuildingCompleted);
            EventBus.Unsubscribe<WaveStartedEvent>(OnWaveStarted);
            EventBus.Unsubscribe<UnitDiedEvent>(OnUnitDied);
            EventBus.Unsubscribe<ResourcesSpentEvent>(OnResourcesSpent);
        }

        private void OnBuildingCompleted(BuildingCompletedEvent evt)
        {
            ShowNotification($"{evt.BuildingType} completed!");
        }

        private void OnWaveStarted(WaveStartedEvent evt)
        {
            ShowNotification($"Wave {evt.WaveNumber} incoming! {evt.EnemyCount} enemies!");
        }

        private void OnUnitDied(UnitDiedEvent evt)
        {
            if (!evt.WasEnemy)
            {
                ShowNotification("Unit lost!");
            }
        }

        private void OnResourcesSpent(ResourcesSpentEvent evt)
        {
            if (!evt.Success)
            {
                ShowNotification("Not enough resources!");
            }
        }

        public void ShowNotification(string message)
        {
            notificationQueue.Enqueue(message);

            if (!isDisplaying)
            {
                StartCoroutine(DisplayQueuedNotifications());
            }
        }

        private System.Collections.IEnumerator DisplayQueuedNotifications()
        {
            isDisplaying = true;

            while (notificationQueue.Count > 0)
            {
                string message = notificationQueue.Dequeue();

                if (notificationText != null)
                {
                    notificationText.text = message;
                }

                yield return StartCoroutine(DisplayCoroutine());
            }

            isDisplaying = false;
        }

        private System.Collections.IEnumerator DisplayCoroutine()
        {
            // Fade in
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                    yield return null;
                }
                canvasGroup.alpha = 1f;
            }

            // Display
            yield return new WaitForSeconds(displayDuration);

            // Fade out
            if (canvasGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                    yield return null;
                }
                canvasGroup.alpha = 0f;
            }
        }
    }
}