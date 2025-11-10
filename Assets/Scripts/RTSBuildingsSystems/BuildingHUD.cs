using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using RTS.Core.Services;
using RTS.Core.Events;
using RTS.Buildings;
using RTS.Managers;
using System.Collections.Generic;

namespace RTS.UI
{
    /// <summary>
    /// PROFESSIONAL RTS-STYLE BUILDING HUD
    /// Modular, data-driven, automatically updates based on resources and building availability.
    /// Uses Unity's NEW Input System for modern input handling.
    /// Inspired by Starcraft, Age of Empires, Command & Conquer.
    /// </summary>
    public class BuildingHUD : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BuildingManager buildingManager;
        [SerializeField] private Transform buildingButtonContainer;
        [SerializeField] private GameObject buildingButtonPrefab;

        [Header("Available Buildings")]
        [SerializeField] private BuildingDataSO[] availableBuildings;

        [Header("UI Panels")]
        [SerializeField] private GameObject buildingPanel;
        [SerializeField] private GameObject placementInfoPanel;
        [SerializeField] private TextMeshProUGUI placementInfoText;

        [Header("Input Actions")]
        [SerializeField] private InputActionReference hotkeyAction;

        [Header("Hotkeys (Optional)")]
        [SerializeField] private bool enableHotkeys = true;
        [SerializeField]
        private string[] buildingHotkeyStrings = new string[]
        {
            "b", "h", "f", "t", "w", "g", "c", "m"
        };

        private List<BuildingButton> buildingButtons = new List<BuildingButton>();
        private IResourcesService resourceService;

        private void Start()
        {
            resourceService = ServiceLocator.TryGet<IResourcesService>();

            if (buildingManager == null)
            {
                buildingManager = Object.FindAnyObjectByType<BuildingManager>();
            }

            InitializeBuildingButtons();

            // Subscribe to resource changes to update button states
            EventBus.Subscribe<ResourcesChangedEvent>(OnResourcesChanged);
        }

        private void OnEnable()
        {
            // Enable hotkey input action if configured
            if (hotkeyAction != null && hotkeyAction.action != null)
            {
                hotkeyAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            // Disable hotkey input action
            if (hotkeyAction != null && hotkeyAction.action != null)
            {
                hotkeyAction.action.Disable();
            }
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<ResourcesChangedEvent>(OnResourcesChanged);
        }

        private void Update()
        {
            // Handle hotkeys using new Input System
            if (enableHotkeys)
            {
                HandleHotkeys();
            }

            // Update placement info panel
            UpdatePlacementInfoPanel();
        }

        #region Initialization

        private void InitializeBuildingButtons()
        {
            if (buildingButtonContainer == null || availableBuildings == null)
            {
                Debug.LogError("BuildingHUD: Missing references!");
                return;
            }

            // Clear existing buttons
            ClearButtons();

            // Create button for each building
            for (int i = 0; i < availableBuildings.Length; i++)
            {
                BuildingDataSO buildingData = availableBuildings[i];
                if (buildingData == null) continue;

                CreateBuildingButton(buildingData, i);
            }

            // Initial update
            UpdateAllButtons();
        }

        private void ClearButtons()
        {
            foreach (Transform child in buildingButtonContainer)
            {
                Destroy(child.gameObject);
            }
            buildingButtons.Clear();
        }

        private void CreateBuildingButton(BuildingDataSO buildingData, int index)
        {
            GameObject buttonObj;

            if (buildingButtonPrefab != null)
            {
                // Use custom prefab
                buttonObj = Instantiate(buildingButtonPrefab, buildingButtonContainer);
            }
            else
            {
                // Create simple button as fallback
                buttonObj = CreateSimpleButton(buildingData.buildingName);
            }

            // Setup BuildingButton component
            var buildingButton = buttonObj.GetComponent<BuildingButton>();
            if (buildingButton == null)
            {
                buildingButton = buttonObj.AddComponent<BuildingButton>();
            }

            buildingButton.Initialize(buildingData, index, this);
            buildingButtons.Add(buildingButton);

            // Add click listener
            var button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                int buildingIndex = index; // Capture for closure
                button.onClick.AddListener(() => OnBuildingButtonClicked(buildingIndex));
            }
        }

        private GameObject CreateSimpleButton(string buildingName)
        {
            GameObject buttonObj = new GameObject($"Button_{buildingName}");
            buttonObj.transform.SetParent(buildingButtonContainer);

            var button = buttonObj.AddComponent<Button>();
            var image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform);
            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = buildingName;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 14;

            RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(100, 100);

            return buttonObj;
        }

        #endregion

        #region Input Handling

        private void OnBuildingButtonClicked(int buildingIndex)
        {
            if (buildingManager == null || buildingIndex >= availableBuildings.Length)
            {
                Debug.LogError("Invalid building selection!");
                return;
            }

            var buildingData = availableBuildings[buildingIndex];
            var costs = buildingData.GetCosts();

            // Check if can afford
            if (resourceService != null && !resourceService.CanAfford(costs))
            {
                Debug.Log($"Cannot afford {buildingData.buildingName}!");
                ShowInsufficientResourcesFeedback(buildingData);
                return;
            }

            // Start placing building
            buildingManager.StartPlacingBuilding(buildingIndex);
            Debug.Log($"Started placing: {buildingData.buildingName}");
        }

        private void HandleHotkeys()
        {
            // Check keyboard input for hotkeys
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            for (int i = 0; i < buildingHotkeyStrings.Length && i < availableBuildings.Length; i++)
            {
                string hotkey = buildingHotkeyStrings[i].ToLower();

                // Get the key from the string
                var key = GetKeyFromString(hotkey);
                if (key != Key.None && keyboard[key].wasPressedThisFrame)
                {
                    OnBuildingButtonClicked(i);
                    break;
                }
            }
        }

        private Key GetKeyFromString(string keyString)
        {
            // Convert string to Key enum
            return keyString.ToLower() switch
            {
                "a" => Key.A,
                "b" => Key.B,
                "c" => Key.C,
                "d" => Key.D,
                "e" => Key.E,
                "f" => Key.F,
                "g" => Key.G,
                "h" => Key.H,
                "i" => Key.I,
                "j" => Key.J,
                "k" => Key.K,
                "l" => Key.L,
                "m" => Key.M,
                "n" => Key.N,
                "o" => Key.O,
                "p" => Key.P,
                "q" => Key.Q,
                "r" => Key.R,
                "s" => Key.S,
                "t" => Key.T,
                "u" => Key.U,
                "v" => Key.V,
                "w" => Key.W,
                "x" => Key.X,
                "y" => Key.Y,
                "z" => Key.Z,
                "1" => Key.Digit1,
                "2" => Key.Digit2,
                "3" => Key.Digit3,
                "4" => Key.Digit4,
                "5" => Key.Digit5,
                "6" => Key.Digit6,
                "7" => Key.Digit7,
                "8" => Key.Digit8,
                "9" => Key.Digit9,
                "0" => Key.Digit0,
                _ => Key.None // Use a default enum value like Key.None
            };
        }

        #endregion

        #region UI Updates

        private void OnResourcesChanged(ResourcesChangedEvent evt)
        {
            UpdateAllButtons();
        }

        private void UpdateAllButtons()
        {
            if (resourceService == null) return;

            foreach (var button in buildingButtons)
            {
                if (button != null)
                {
                    button.UpdateState(resourceService);
                }
            }
        }

        private void UpdatePlacementInfoPanel()
        {
            if (placementInfoPanel == null) return;

            bool isPlacing = buildingManager != null && buildingManager.IsPlacing;
            placementInfoPanel.SetActive(isPlacing);

            if (isPlacing && placementInfoText != null)
            {
                placementInfoText.text = "Left Click: Place  |  Right Click/ESC: Cancel";
            }
        }

        private void ShowInsufficientResourcesFeedback(BuildingDataSO buildingData)
        {
            // Flash the button or show error message
            // You could also trigger a sound effect here
            Debug.LogWarning($"Not enough resources for {buildingData.buildingName}!");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Show or hide the building panel.
        /// </summary>
        public void SetPanelVisible(bool visible)
        {
            if (buildingPanel != null)
            {
                buildingPanel.SetActive(visible);
            }
        }

        /// <summary>
        /// Refresh all building buttons (call this if you change available buildings at runtime).
        /// </summary>
        public void RefreshButtons()
        {
            InitializeBuildingButtons();
        }

        #endregion
    }
}