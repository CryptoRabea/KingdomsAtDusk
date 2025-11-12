using RTS.Buildings;
using RTS.Core.Events;
using RTS.Core.Services;
using RTS.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RTS.UI
{
    /// <summary>
    /// Simple Building UI - gets building list from BuildingManager.
    /// NO DUPLICATE DATA - BuildingManager is the single source of truth!
    /// </summary>
    public class BuildingUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BuildingManager buildingManager;
        [SerializeField] private Transform buttonContainer;

        [Header("Building Button Prefab")]
        [SerializeField] private GameObject buildingButtonPrefab;

        private IResourcesService resourceService;

        private void Start()
        {
            resourceService = ServiceLocator.TryGet<IResourcesService>();

            if (buildingManager == null)
            {
                buildingManager = Object.FindAnyObjectByType<BuildingManager>();
                if (buildingManager == null)
                {
                    Debug.LogError("BuildingUI: BuildingManager not found in scene!");
                    return;
                }
            }

            CreateBuildingButtons();

            // Subscribe to resource changes
            EventBus.Subscribe<ResourcesChangedEvent>(OnResourcesChanged);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<ResourcesChangedEvent>(OnResourcesChanged);
        }

        private void CreateBuildingButtons()
        {
            if (buttonContainer == null || buildingManager == null)
            {
                Debug.LogError("BuildingUI: Missing references!");
                return;
            }

            // ✅ GET BUILDINGS FROM BUILDINGMANAGER - NO DUPLICATE ARRAY!
            BuildingDataSO[] availableBuildings = buildingManager.GetAllBuildingData();

            if (availableBuildings == null || availableBuildings.Length == 0)
            {
                Debug.LogWarning("BuildingUI: No buildings available in BuildingManager!");
                return;
            }

            // Clear existing buttons
            foreach (Transform child in buttonContainer)
            {
                Destroy(child.gameObject);
            }

            // Create button for each building from BuildingManager
            for (int i = 0; i < availableBuildings.Length; i++)
            {
                BuildingDataSO buildingData = availableBuildings[i];
                if (buildingData == null) continue;

                CreateBuildingButton(buildingData, i);
            }

            Debug.Log($"BuildingUI: Created {availableBuildings.Length} building buttons from BuildingManager");
        }

        private void CreateBuildingButton(BuildingDataSO buildingData, int buildingIndex)
        {
            GameObject buttonObj;

            if (buildingButtonPrefab != null)
            {
                buttonObj = Instantiate(buildingButtonPrefab, buttonContainer);
            }
            else
            {
                buttonObj = CreateSimpleButton();
            }

            // Setup button
            var button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnBuildingButtonClicked(buildingIndex));
            }

            // Setup text
            var text = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = buildingData.buildingName;
            }

            // Setup icon
            var icon = buttonObj.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null && buildingData.icon != null)
            {
                icon.sprite = buildingData.icon;
            }

            // Setup cost display
            UpdateButtonCostDisplay(buttonObj, buildingData);

            // Store building index in button for later reference
            var buttonComponent = buttonObj.GetComponent<BuildingButtonSimple>();
            if (buttonComponent == null)
            {
                buttonComponent = buttonObj.AddComponent<BuildingButtonSimple>();
            }
            buttonComponent.buildingIndex = buildingIndex;
            buttonComponent.buildingData = buildingData;
        }

        private GameObject CreateSimpleButton()
        {
            GameObject buttonObj = new GameObject("BuildingButton");
            buttonObj.transform.SetParent(buttonContainer);

            var button = buttonObj.AddComponent<Button>();
            var image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform);
            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 14;

            RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(100, 100);

            return buttonObj;
        }

        private void UpdateButtonCostDisplay(GameObject buttonObj, BuildingDataSO buildingData)
        {
            var costText = buttonObj.transform.Find("CostText")?.GetComponent<TextMeshProUGUI>();
            if (costText != null)
            {
                costText.text = buildingData.GetCostString();
            }
        }

        private void OnBuildingButtonClicked(int buildingIndex)
        {
            if (buildingManager == null)
            {
                Debug.LogError("BuildingManager not assigned!");
                return;
            }

            // ✅ GET BUILDING DATA FROM BUILDINGMANAGER
            BuildingDataSO[] availableBuildings = buildingManager.GetAllBuildingData();
            if (buildingIndex >= availableBuildings.Length)
            {
                Debug.LogError($"Invalid building index: {buildingIndex}");
                return;
            }

            BuildingDataSO buildingData = availableBuildings[buildingIndex];

            // Check if can afford
            if (!buildingManager.CanAffordBuilding(buildingData))
            {
                Debug.Log($"Cannot afford {buildingData.buildingName}!");
                ShowInsufficientResourcesFeedback(buildingData);
                return;
            }

            // Start placing building
            buildingManager.StartPlacingBuilding(buildingIndex);
            Debug.Log($"Started placing: {buildingData.buildingName}");
        }

        private void OnResourcesChanged(ResourcesChangedEvent evt)
        {
            UpdateAllButtons();
        }

        private void UpdateAllButtons()
        {
            if (buttonContainer == null || buildingManager == null) return;

            // ✅ GET BUILDINGS FROM BUILDINGMANAGER
            BuildingDataSO[] availableBuildings = buildingManager.GetAllBuildingData();

            int index = 0;
            foreach (Transform child in buttonContainer)
            {
                if (index >= availableBuildings.Length) break;

                var buildingData = availableBuildings[index];
                var button = child.GetComponent<Button>();

                if (button != null && buildingData != null)
                {
                    // Check if can afford
                    bool canAfford = buildingManager.CanAffordBuilding(buildingData);

                    // Update button visual state
                    button.interactable = canAfford;

                    // Update text color
                    var text = child.GetComponentInChildren<TextMeshProUGUI>();
                    if (text != null)
                    {
                        text.color = canAfford ? Color.white : Color.gray;
                    }

                    // Update image color
                    var image = child.GetComponent<Image>();
                    if (image != null)
                    {
                        image.color = canAfford ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
                    }
                }

                index++;
            }
        }

        private void ShowInsufficientResourcesFeedback(BuildingDataSO buildingData)
        {
            Debug.LogWarning($"Not enough resources for {buildingData.buildingName}!");
            // TODO: Show UI notification with required resources
        }

        /// <summary>
        /// Refresh all building buttons.
        /// Call this if BuildingManager's building list changes.
        /// </summary>
        public void RefreshButtons()
        {
            CreateBuildingButtons();
        }
    }

    /// <summary>
    /// Simple component to store building data on button GameObject.
    /// </summary>
    public class BuildingButtonSimple : MonoBehaviour
    {
        public int buildingIndex;
        public BuildingDataSO buildingData;
    }
}