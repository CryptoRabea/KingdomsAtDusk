using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTS.Managers;
using RTS.Buildings;
using RTS.Core.Services;

namespace RTS.UI
{
    /// <summary>
    /// Building panel UI - shows available buildings and handles button clicks.
    /// Displays building costs and availability.
    /// </summary>
    public class BuildingUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BuildingManager buildingManager;
        [SerializeField] private Transform buttonContainer; // Panel to hold buttons

        [Header("Building Button Prefab")]
        [SerializeField] private GameObject buildingButtonPrefab; // Create this

        [Header("Buildings")]
        [SerializeField] private BuildingDataSO[] availableBuildings; // What can be built

        private void Start()
        {
            if (buildingManager == null)
            {
                buildingManager = Object.FindAnyObjectByType<BuildingManager>();
            }

            CreateBuildingButtons();
        }

        private void CreateBuildingButtons()
        {
            if (buttonContainer == null || availableBuildings == null) return;

            // Clear existing buttons
            foreach (Transform child in buttonContainer)
            {
                Destroy(child.gameObject);
            }

            // Create button for each building
            for (int i = 0; i < availableBuildings.Length; i++)
            {
                BuildingDataSO buildingData = availableBuildings[i];
                if (buildingData == null) continue;

                CreateBuildingButton(buildingData, i);
            }
        }

        private void CreateBuildingButton(BuildingDataSO buildingData, int index)
        {
            GameObject buttonObj;

            if (buildingButtonPrefab != null)
            {
                // Use custom prefab
                buttonObj = Instantiate(buildingButtonPrefab, buttonContainer);
            }
            else
            {
                // Create simple button
                buttonObj = new GameObject($"Button_{buildingData.buildingName}");
                buttonObj.transform.SetParent(buttonContainer);

                var button = buttonObj.AddComponent<Button>();
                var image = buttonObj.AddComponent<Image>();

                var textObj = new GameObject("Text");
                textObj.transform.SetParent(buttonObj.transform);
                var text = textObj.AddComponent<TextMeshProUGUI>();
                text.text = buildingData.buildingName;
                text.alignment = TextAlignmentOptions.Center;

                RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(100, 100);
            }

            // Setup button
            var btn = buttonObj.GetComponent<Button>();
            if (btn != null)
            {
                // Add click listener
                int buildingIndex = index; // Capture for closure
                btn.onClick.AddListener(() => OnBuildingButtonClicked(buildingIndex));
            }

            // Update button text/info
            UpdateButtonInfo(buttonObj, buildingData);
        }

        private void UpdateButtonInfo(GameObject buttonObj, BuildingDataSO buildingData)
        {
            // Find text component (might be child)
            var text = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = $"{buildingData.buildingName}\n" +
                           $"W:{buildingData.woodCost} " +
                           $"F:{buildingData.foodCost} " +
                           $"G:{buildingData.goldCost}" +
                           $"G:{buildingData.stoneCost}";
            }

            // Check if affordable
            var resourceService = ServiceLocator.TryGet<IResourceService>();
            if (resourceService != null)
            {
                var costs = buildingData.GetCosts();
                bool canAfford = resourceService.CanAfford(costs);

                // Disable button if can't afford
                var button = buttonObj.GetComponent<Button>();
                if (button != null)
                {
                    button.interactable = canAfford;
                }

                // Change color if can't afford
                var image = buttonObj.GetComponent<Image>();
                if (image != null)
                {
                    image.color = canAfford ? Color.white : Color.gray;
                }
            }
        }

        private void OnBuildingButtonClicked(int buildingIndex)
        {
            if (buildingManager == null)
            {
                Debug.LogError("BuildingManager not assigned!");
                return;
            }

            Debug.Log($"Building button clicked: {buildingIndex}");
            buildingManager.StartPlacingBuilding(buildingIndex);
        }

        private void Update()
        {
            // Update button states based on resources
            UpdateAllButtons();
        }

        private void UpdateAllButtons()
        {
            if (buttonContainer == null) return;

            int index = 0;
            foreach (Transform child in buttonContainer)
            {
                if (index < availableBuildings.Length)
                {
                    UpdateButtonInfo(child.gameObject, availableBuildings[index]);
                }
                index++;
            }
        }
    }

    /// <summary>
    /// Displays info about building being placed.
    /// Shows controls (ESC to cancel, etc.)
    /// </summary>
    public class BuildingPlacementUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject placementPanel;
        [SerializeField] private TextMeshProUGUI instructionText;

        [Header("Messages")]
        [SerializeField] private string placementInstructions = "Left Click: Place  |  Right Click: Cancel  |  ESC: Cancel";
        [SerializeField] private string invalidPlacementMessage = "Cannot place here!";

        private void Start()
        {
            if (placementPanel != null)
            {
                placementPanel.SetActive(false);
            }
        }

        public void ShowPlacementUI(bool show)
        {
            if (placementPanel != null)
            {
                placementPanel.SetActive(show);
            }

            if (show && instructionText != null)
            {
                instructionText.text = placementInstructions;
            }
        }

        public void ShowInvalidPlacement()
        {
            if (instructionText != null)
            {
                instructionText.text = invalidPlacementMessage;
                StartCoroutine(ResetTextAfterDelay(1f));
            }
        }

        private System.Collections.IEnumerator ResetTextAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (instructionText != null)
            {
                instructionText.text = placementInstructions;
            }
        }
    }
}