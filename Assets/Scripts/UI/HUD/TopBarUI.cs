using RTS.Core.Events;
using RTS.Core.Services;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RTS.UI.HUD
{
    /// <summary>
    /// Top bar UI component showing resources and menu buttons (Warcraft 3 style).
    /// Can be configured to show only resources, only menu, or both.
    /// </summary>
    public class TopBarUI : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool showResources = true;
        [SerializeField] private bool showMenuButtons = true;
        [SerializeField] private bool showClock = false;
        [SerializeField] private bool showPopulation = true;

        [Header("Resource Display")]
        [SerializeField] private Transform resourceContainer;
        [SerializeField] private GameObject resourceItemPrefab;

        [Header("Menu Buttons")]
        [SerializeField] private Transform menuButtonContainer;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button alliesButton;
        [SerializeField] private Button questsButton;
        [SerializeField] private Button chatButton;

        [Header("Clock")]
        [SerializeField] private TextMeshProUGUI clockText;

        [Header("Population")]
        [SerializeField] private TextMeshProUGUI populationText;
        [SerializeField] private Image populationIcon;

        [Header("Visual Settings")]
        [SerializeField] private Color resourceChangeColor = Color.yellow;
        [SerializeField] private float colorChangeDuration = 0.5f;

        private Dictionary<ResourceType, ResourceDisplayItem> resourceDisplays = new Dictionary<ResourceType, ResourceDisplayItem>();
        private IResourcesService resourcesService;
        private float gameTime;

        private void Awake()
        {
            // Get services - ServiceLocator is static, no Instance needed
            resourcesService = ServiceLocator.TryGet<IResourcesService>();

            // Subscribe to events
            EventBus.Subscribe<ResourcesChangedEvent>(OnResourcesChanged);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<ResourcesChangedEvent>(OnResourcesChanged);
        }

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            if (showClock && clockText != null)
            {
                UpdateClock();
            }

            if (showPopulation && populationText != null)
            {
                UpdatePopulation();
            }
        }

        /// <summary>
        /// Initializes the top bar UI.
        /// </summary>
        private void Initialize()
        {
            // Show/hide sections based on configuration
            if (resourceContainer != null)
            {
                resourceContainer.gameObject.SetActive(showResources);
            }

            if (menuButtonContainer != null)
            {
                menuButtonContainer.gameObject.SetActive(showMenuButtons);
            }

            if (clockText != null)
            {
                clockText.gameObject.SetActive(showClock);
            }

            if (populationText != null)
            {
                populationText.gameObject.SetActive(showPopulation);
            }

            // Initialize resources
            if (showResources && resourcesService != null)
            {
                InitializeResources();
            }

            // Setup menu buttons
            if (showMenuButtons)
            {
                SetupMenuButtons();
            }
        }

        /// <summary>
        /// Initializes resource displays.
        /// </summary>
        private void InitializeResources()
        {
            if (resourceContainer == null || resourceItemPrefab == null)
            {
                Debug.LogWarning("TopBarUI: Resource container or prefab not assigned!");
                return;
            }

            // Clear existing displays
            foreach (Transform child in resourceContainer)
            {
                Destroy(child.gameObject);
            }
            resourceDisplays.Clear();

            // Create displays for all resource types
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                int amount = resourcesService.GetResource(type);
                CreateResourceDisplay(type, amount);
            }
        }

        /// <summary>
        /// Creates a resource display item.
        /// </summary>
        private void CreateResourceDisplay(ResourceType resourceType, int amount)
        {
            GameObject item = Instantiate(resourceItemPrefab, resourceContainer);

            if (!item.TryGetComponent<ResourceDisplayItem>(out var displayItem))
            {
                displayItem = item.AddComponent<ResourceDisplayItem>();
            }

            displayItem.Initialize(resourceType.ToString(), amount);
            resourceDisplays[resourceType] = displayItem;
        }

        /// <summary>
        /// Sets up menu button callbacks.
        /// </summary>
        private void SetupMenuButtons()
        {
            if (menuButton != null)
            {
                menuButton.onClick.AddListener(OnMenuClicked);
            }

            if (alliesButton != null)
            {
                alliesButton.onClick.AddListener(OnAlliesClicked);
            }

            if (questsButton != null)
            {
                questsButton.onClick.AddListener(OnQuestsClicked);
            }

            if (chatButton != null)
            {
                chatButton.onClick.AddListener(OnChatClicked);
            }
        }

        /// <summary>
        /// Updates the game clock display.
        /// </summary>
        private void UpdateClock()
        {
            gameTime += Time.deltaTime;
            int minutes = Mathf.FloorToInt(gameTime / 60f);
            int seconds = Mathf.FloorToInt(gameTime % 60f);
            clockText.text = $"{minutes:00}:{seconds:00}";
        }

        /// <summary>
        /// Updates the population display.
        /// </summary>
        private void UpdatePopulation()
        {
            if (resourcesService != null)
            {
                // Using Food as population for now
                int currentPop = resourcesService.Food;

                // Simple display for now
                populationText.text = $"Population: {currentPop}";
            }
        }

        /// <summary>
        /// Handles resource changes.
        /// </summary>
        private void OnResourcesChanged(ResourcesChangedEvent e)
        {
            if (resourcesService == null) return;

            // Update all resource displays with current values
            if (e.WoodDelta != 0 && resourceDisplays.ContainsKey(ResourceType.Wood))
            {
                int newAmount = resourcesService.Wood;
                resourceDisplays[ResourceType.Wood].UpdateAmount(newAmount, resourceChangeColor, colorChangeDuration);
            }

            if (e.FoodDelta != 0 && resourceDisplays.ContainsKey(ResourceType.Food))
            {
                int newAmount = resourcesService.Food;
                resourceDisplays[ResourceType.Food].UpdateAmount(newAmount, resourceChangeColor, colorChangeDuration);
            }

            if (e.GoldDelta != 0 && resourceDisplays.ContainsKey(ResourceType.Gold))
            {
                int newAmount = resourcesService.Gold;
                resourceDisplays[ResourceType.Gold].UpdateAmount(newAmount, resourceChangeColor, colorChangeDuration);
            }

            if (e.StoneDelta != 0 && resourceDisplays.ContainsKey(ResourceType.Stone))
            {
                int newAmount = resourcesService.Stone;
                resourceDisplays[ResourceType.Stone].UpdateAmount(newAmount, resourceChangeColor, colorChangeDuration);
            }
        }

        // Menu button callbacks
        private void OnMenuClicked()
        {
            Debug.Log("Menu button clicked (F10)");
            // Open game menu
        }

        private void OnAlliesClicked()
        {
            Debug.Log("Allies button clicked (F11)");
            // Open allies panel
        }

        private void OnQuestsClicked()
        {
            Debug.Log("Quests button clicked (F9)");
            // Open quests panel
        }

        private void OnChatClicked()
        {
            Debug.Log("Chat button clicked (F12)");
            // Open chat panel
        }

        /// <summary>
        /// Public method to configure the top bar at runtime.
        /// </summary>
        public void Configure(bool resources, bool menu, bool clock, bool population)
        {
            showResources = resources;
            showMenuButtons = menu;
            showClock = clock;
            showPopulation = population;
            Initialize();
        }
    }

    /// <summary>
    /// Individual resource display item in the top bar.
    /// </summary>
    public class ResourceDisplayItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI amountText;
        [SerializeField] private Image icon;

        private string resourceName;
        private int currentAmount;
        private Color originalColor;
        private float colorTimer;
        private bool isAnimating;
        private Color targetColor;
        private float animationDuration;

        private void Awake()
        {
            // Auto-find components if not assigned
            if (nameText == null)
            {
                var texts = GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length > 0) nameText = texts[0];
                if (texts.Length > 1) amountText = texts[1];
            }

            if (amountText != null)
            {
                originalColor = amountText.color;
            }
        }

        private void Update()
        {
            if (isAnimating && amountText != null)
            {
                colorTimer += Time.deltaTime;
                float t = colorTimer / animationDuration;

                if (t >= 1f)
                {
                    amountText.color = originalColor;
                    isAnimating = false;
                }
                else
                {
                    amountText.color = Color.Lerp(targetColor, originalColor, t);
                }
            }
        }

        public void Initialize(string name, int amount)
        {
            resourceName = name;
            currentAmount = amount;

            if (nameText != null)
            {
                nameText.text = name;
            }

            if (amountText != null)
            {
                amountText.text = amount.ToString();
                originalColor = amountText.color;
            }
        }

        public void UpdateAmount(int newAmount, Color highlightColor, float duration)
        {
            currentAmount = newAmount;

            if (amountText != null)
            {
                amountText.text = newAmount.ToString();

                // Trigger color animation
                targetColor = highlightColor;
                animationDuration = duration;
                colorTimer = 0f;
                isAnimating = true;
            }
        }
    }
}
