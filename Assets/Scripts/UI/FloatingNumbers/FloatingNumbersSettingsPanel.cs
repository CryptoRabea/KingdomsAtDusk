using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTS.Core.Services;

namespace KAD.UI.FloatingNumbers
{
    /// <summary>
    /// UI panel for controlling floating numbers settings during gameplay.
    /// Provides toggles for each feature and applies changes in real-time.
    /// </summary>
    public class FloatingNumbersSettingsPanel : MonoBehaviour
    {
        [Header("Settings Reference")]
        [SerializeField] private FloatingNumbersSettings settings;

        [Header("Feature Toggles")]
        [SerializeField] private Toggle showHPBarsToggle;
        [SerializeField] private Toggle showDamageNumbersToggle;
        [SerializeField] private Toggle showHealNumbersToggle;
        [SerializeField] private Toggle showResourceGatheringToggle;
        [SerializeField] private Toggle showBuildingResourceToggle;
        [SerializeField] private Toggle showRepairNumbersToggle;

        [Header("HP Bar Options")]
        [SerializeField] private Toggle hpBarsOnlyWhenSelectedToggle;
        [SerializeField] private Toggle hpBarsOnlyWhenDamagedToggle;

        [Header("Blood Effects")]
        [SerializeField] private Toggle enableBloodEffectsToggle;
        [SerializeField] private Toggle useBlueBloodToggle;
        [SerializeField] private Toggle showBloodGushToggle;
        [SerializeField] private Toggle showBloodDrippingToggle;

        [Header("Future Features")]
        [SerializeField] private Toggle showExperienceNumbersToggle;
        [SerializeField] private Toggle showResourcePickupsToggle;
        [SerializeField] private Toggle showLevelUpNotificationsToggle;

        [Header("Buttons")]
        [SerializeField] private Button applyButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button closeButton;

        [Header("Info Text")]
        [SerializeField] private TextMeshProUGUI infoText;

        private IFloatingNumberService floatingNumberService;

        private void Awake()
        {
            // Get service reference
            floatingNumberService = ServiceLocator.TryGet<IFloatingNumberService>();

            // If service exists, get settings from it
            if (floatingNumberService != null && settings == null)
            {
                settings = floatingNumberService.Settings;
            }

            SetupButtonListeners();
        }

        private void OnEnable()
        {
            LoadCurrentSettings();
        }

        private void SetupButtonListeners()
        {
            if (applyButton != null)
                applyButton.onClick.AddListener(OnApplyClicked);

            if (resetButton != null)
                resetButton.onClick.AddListener(OnResetClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseClicked);

            // Add listeners to toggles for immediate feedback
            if (showHPBarsToggle != null)
                showHPBarsToggle.onValueChanged.AddListener(OnHPBarsToggled);

            if (showDamageNumbersToggle != null)
                showDamageNumbersToggle.onValueChanged.AddListener(OnDamageNumbersToggled);

            if (showHealNumbersToggle != null)
                showHealNumbersToggle.onValueChanged.AddListener(OnHealNumbersToggled);

            if (showResourceGatheringToggle != null)
                showResourceGatheringToggle.onValueChanged.AddListener(OnResourceGatheringToggled);

            if (showBuildingResourceToggle != null)
                showBuildingResourceToggle.onValueChanged.AddListener(OnBuildingResourceToggled);

            if (showRepairNumbersToggle != null)
                showRepairNumbersToggle.onValueChanged.AddListener(OnRepairNumbersToggled);

            if (hpBarsOnlyWhenSelectedToggle != null)
                hpBarsOnlyWhenSelectedToggle.onValueChanged.AddListener(OnHPBarsOnlyWhenSelectedToggled);

            if (hpBarsOnlyWhenDamagedToggle != null)
                hpBarsOnlyWhenDamagedToggle.onValueChanged.AddListener(OnHPBarsOnlyWhenDamagedToggled);

            if (enableBloodEffectsToggle != null)
                enableBloodEffectsToggle.onValueChanged.AddListener(OnEnableBloodEffectsToggled);

            if (useBlueBloodToggle != null)
                useBlueBloodToggle.onValueChanged.AddListener(OnUseBlueBloodToggled);

            if (showBloodGushToggle != null)
                showBloodGushToggle.onValueChanged.AddListener(OnShowBloodGushToggled);

            if (showBloodDrippingToggle != null)
                showBloodDrippingToggle.onValueChanged.AddListener(OnShowBloodDrippingToggled);

            if (showExperienceNumbersToggle != null)
                showExperienceNumbersToggle.onValueChanged.AddListener(OnExperienceNumbersToggled);

            if (showResourcePickupsToggle != null)
                showResourcePickupsToggle.onValueChanged.AddListener(OnResourcePickupsToggled);

            if (showLevelUpNotificationsToggle != null)
                showLevelUpNotificationsToggle.onValueChanged.AddListener(OnLevelUpNotificationsToggled);
        }

        private void LoadCurrentSettings()
        {
            if (settings == null) return;

            // Load feature toggles
            if (showHPBarsToggle != null)
                showHPBarsToggle.isOn = settings.ShowHPBars;

            if (showDamageNumbersToggle != null)
                showDamageNumbersToggle.isOn = settings.ShowDamageNumbers;

            if (showHealNumbersToggle != null)
                showHealNumbersToggle.isOn = settings.ShowHealNumbers;

            if (showResourceGatheringToggle != null)
                showResourceGatheringToggle.isOn = settings.ShowResourceGatheringNumbers;

            if (showBuildingResourceToggle != null)
                showBuildingResourceToggle.isOn = settings.ShowBuildingResourceNumbers;

            if (showRepairNumbersToggle != null)
                showRepairNumbersToggle.isOn = settings.ShowRepairNumbers;

            // Load HP bar options
            if (hpBarsOnlyWhenSelectedToggle != null)
                hpBarsOnlyWhenSelectedToggle.isOn = settings.HPBarsOnlyWhenSelected;

            if (hpBarsOnlyWhenDamagedToggle != null)
                hpBarsOnlyWhenDamagedToggle.isOn = settings.HPBarsOnlyWhenDamaged;

            // Load blood effects
            if (enableBloodEffectsToggle != null)
                enableBloodEffectsToggle.isOn = settings.EnableBloodEffects;

            if (useBlueBloodToggle != null)
                useBlueBloodToggle.isOn = settings.UseBlueBlood;

            if (showBloodGushToggle != null)
                showBloodGushToggle.isOn = settings.ShowBloodGush;

            if (showBloodDrippingToggle != null)
                showBloodDrippingToggle.isOn = settings.ShowBloodDripping;

            // Load future features
            if (showExperienceNumbersToggle != null)
                showExperienceNumbersToggle.isOn = settings.ShowExperienceNumbers;

            if (showResourcePickupsToggle != null)
                showResourcePickupsToggle.isOn = settings.ShowResourcePickupNumbers;

            if (showLevelUpNotificationsToggle != null)
                showLevelUpNotificationsToggle.isOn = settings.ShowLevelUpNotifications;

            UpdateInfoText();
        }

        // Individual toggle handlers for immediate application
        private void OnHPBarsToggled(bool value)
        {
            if (settings != null)
            {
                settings.SetShowHPBars(value);
                RefreshService();
            }
        }

        private void OnDamageNumbersToggled(bool value)
        {
            if (settings != null)
                settings.SetShowDamageNumbers(value);
        }

        private void OnHealNumbersToggled(bool value)
        {
            if (settings != null)
                settings.SetShowHealNumbers(value);
        }

        private void OnResourceGatheringToggled(bool value)
        {
            if (settings != null)
                settings.SetShowResourceGatheringNumbers(value);
        }

        private void OnBuildingResourceToggled(bool value)
        {
            if (settings != null)
                settings.SetShowBuildingResourceNumbers(value);
        }

        private void OnRepairNumbersToggled(bool value)
        {
            if (settings != null)
                settings.SetShowRepairNumbers(value);
        }

        private void OnHPBarsOnlyWhenSelectedToggled(bool value)
        {
            if (settings != null)
            {
                settings.SetHPBarsOnlyWhenSelected(value);
                RefreshService();
            }
        }

        private void OnHPBarsOnlyWhenDamagedToggled(bool value)
        {
            if (settings != null)
            {
                settings.SetHPBarsOnlyWhenDamaged(value);
                RefreshService();
            }
        }

        private void OnEnableBloodEffectsToggled(bool value)
        {
            if (settings != null)
                settings.SetEnableBloodEffects(value);
        }

        private void OnUseBlueBloodToggled(bool value)
        {
            if (settings != null)
                settings.SetUseBlueBlood(value);
        }

        private void OnShowBloodGushToggled(bool value)
        {
            if (settings != null)
                settings.SetShowBloodGush(value);
        }

        private void OnShowBloodDrippingToggled(bool value)
        {
            if (settings != null)
                settings.SetShowBloodDripping(value);
        }

        private void OnExperienceNumbersToggled(bool value)
        {
            if (settings != null)
                settings.SetShowExperienceNumbers(value);
        }

        private void OnResourcePickupsToggled(bool value)
        {
            if (settings != null)
                settings.SetShowResourcePickupNumbers(value);
        }

        private void OnLevelUpNotificationsToggled(bool value)
        {
            if (settings != null)
                settings.SetShowLevelUpNotifications(value);
        }

        private void RefreshService()
        {
            floatingNumberService?.RefreshSettings();
        }

        private void OnApplyClicked()
        {
            // Settings are already applied via toggle listeners
            // This just provides user feedback
            RefreshService();
            UpdateInfoText("Settings applied successfully!");

            // Close panel after applying
            OnCloseClicked();
        }

        private void OnResetClicked()
        {
            if (settings != null)
            {
                settings.ResetToDefaults();
                LoadCurrentSettings();
                RefreshService();
                UpdateInfoText("Settings reset to defaults!");
            }
        }

        private void OnCloseClicked()
        {
            gameObject.SetActive(false);
        }

        private void UpdateInfoText(string message = "")
        {
            if (infoText == null) return;

            if (string.IsNullOrEmpty(message))
            {
                infoText.text = "Configure floating numbers and HP bar display options.\nChanges are applied immediately.";
            }
            else
            {
                infoText.text = message;
            }
        }

        private void OnDestroy()
        {
            // Clean up button listeners
            if (applyButton != null)
                applyButton.onClick.RemoveAllListeners();

            if (resetButton != null)
                resetButton.onClick.RemoveAllListeners();

            if (closeButton != null)
                closeButton.onClick.RemoveAllListeners();
        }
    }
}
