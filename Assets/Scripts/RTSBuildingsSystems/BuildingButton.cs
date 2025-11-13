using RTS.Buildings;
using RTS.Core.Services;
using RTS.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RTS.UI
{
    /// <summary>
    /// Simplified Building Button - displays building info and updates affordability state.
    /// UI elements should be set up in the prefab, not generated dynamically.
    /// </summary>
    public class BuildingButton : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI hotkeyText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Button button;

        [Header("Visual Settings")]
        [SerializeField] private Color affordableColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color unaffordableColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color hoverBrightness = 1.2f;

        private BuildingDataSO buildingData;
        private int buildingIndex;
        private BuildingHUD parentHUD;
        private bool isAffordable;

        public void Initialize(BuildingDataSO data, int index, BuildingHUD hud)
        {
            buildingData = data;
            buildingIndex = index;
            parentHUD = hud;

            // Auto-find components if not assigned
            if (nameText == null) nameText = transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            if (costText == null) costText = transform.Find("CostText")?.GetComponent<TextMeshProUGUI>();
            if (hotkeyText == null) hotkeyText = transform.Find("HotkeyText")?.GetComponent<TextMeshProUGUI>();
            if (iconImage == null) iconImage = transform.Find("Icon")?.GetComponent<Image>();
            if (backgroundImage == null) backgroundImage = GetComponent<Image>();
            if (button == null) button = GetComponent<Button>();

            UpdateDisplay();
        }

        public void UpdateState(IResourcesService resourceService)
        {
            if (buildingData == null || resourceService == null) return;

            var costs = buildingData.GetCosts();
            isAffordable = resourceService.CanAfford(costs);

            // Update button interactability
            if (button != null)
            {
                button.interactable = isAffordable;
            }

            // Update background color
            if (backgroundImage != null)
            {
                backgroundImage.color = isAffordable ? affordableColor : unaffordableColor;
            }

            // Update text color
            if (nameText != null)
            {
                nameText.color = isAffordable ? Color.white : Color.gray;
            }
        }

        private void UpdateDisplay()
        {
            if (buildingData == null) return;

            // Update name
            if (nameText != null)
            {
                nameText.text = buildingData.buildingName;
            }

            // Update building icon
            if (iconImage != null && buildingData.icon != null)
            {
                iconImage.sprite = buildingData.icon;
                iconImage.enabled = true;
            }

            // Update hotkey hint
            if (hotkeyText != null)
            {
                hotkeyText.text = $"[{buildingIndex + 1}]";
            }

            // Update cost display
            if (costText != null)
            {
                costText.text = buildingData.GetCostString();
            }
        }

        public void OnPointerEnter()
        {
            if (backgroundImage != null && button != null && button.interactable)
            {
                Color brightened = backgroundImage.color * hoverBrightness;
                brightened.a = backgroundImage.color.a;
                backgroundImage.color = brightened;
            }
        }

        public void OnPointerExit()
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = isAffordable ? affordableColor : unaffordableColor;
            }
        }

        public BuildingDataSO BuildingData => buildingData;
        public int BuildingIndex => buildingIndex;
        public bool IsAffordable => isAffordable;
    }
}
