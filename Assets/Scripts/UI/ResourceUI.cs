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

        private IResourcesService resourceService;

        private void OnEnable()
        {
            EventBus.Subscribe<ResourcesChangedEvent>(OnResourcesChanged);
            resourceService = ServiceLocator.TryGet<IResourcesService>();

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

        private IResourcesService resourceService;

        private void OnEnable()
        {
            EventBus.Subscribe<ResourcesChangedEvent>(OnResourcesChanged);
            resourceService = ServiceLocator.TryGet<IResourcesService>();
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
}