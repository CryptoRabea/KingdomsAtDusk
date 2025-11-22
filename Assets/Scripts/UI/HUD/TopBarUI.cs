using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using RTS.Core.Events;
using RTS.Core.Services;

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

        private Dictionary<string, ResourceDisplayItem> resourceDisplays = new Dictionary<string, ResourceDisplayItem>();
        private IResourcesService resourcesService;
        private float gameTime;

        private void Awake()
        {
            // Get services
            resourcesService = ServiceLocator.Instance.Get<IResourcesService>();

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

            // Get current resources
            var resources = resourcesService.GetAllResources();

            foreach (var kvp in resources)
            {
                CreateResourceDisplay(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Creates a resource display item.
        /// </summary>
        private void CreateResourceDisplay(string resourceName, int amount)
        {
            GameObject item = Instantiate(resourceItemPrefab, resourceContainer);
            var displayItem = item.GetComponent<ResourceDisplayItem>();

            if (displayItem == null)
            {
                displayItem = item.AddComponent<ResourceDisplayItem>();
            }

            displayItem.Initialize(resourceName, amount);
            resourceDisplays[resourceName] = displayItem;
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
                int currentPop = resourcesService.GetResourceAmount("Food"); // Using food as population
                int maxPop = resourcesService.GetResourceAmount("MaxFood"); // Assuming this exists

                if (maxPop > 0)
                {
                    populationText.text = $"{currentPop}/{maxPop}";

                    // Color code based on population
                    if (currentPop >= maxPop)
                    {
                        populationText.color = Color.red;
                    }
                    else if (currentPop >= maxPop * 0.8f)
                    {
                        populationText.color = Color.yellow;
                    }
                    else
                    {
                        populationText.color = Color.white;
                    }
                }
            }
        }

        /// <summary>
        /// Handles resource changes.
        /// </summary>
        private void OnResourcesChanged(ResourcesChangedEvent e)
        {
            foreach (var kvp in e.Resources)
            {
                if (resourceDisplays.ContainsKey(kvp.Key))
                {
                    resourceDisplays[kvp.Key].UpdateAmount(kvp.Value, resourceChangeColor, colorChangeDuration);
                }
                else
                {
                    // Create new display for this resource
                    CreateResourceDisplay(kvp.Key, kvp.Value);
                }
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
