using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTS.Core.Services;
using RTSGame.Settings;
using System.Collections.Generic;
using System.Linq;

namespace RTSGame.UI.Settings
{
    /// <summary>
    /// Main settings panel controller with tabbed interface.
    /// Manages all settings UI and applies changes through the settings service.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject generalTab;
        [SerializeField] private GameObject graphicsTab;
        [SerializeField] private GameObject audioTab;
        [SerializeField] private GameObject gameplayTab;
        [SerializeField] private GameObject controlsTab;
        [SerializeField] private GameObject uiTab;
        [SerializeField] private GameObject accessibilityTab;
        [SerializeField] private GameObject networkTab;
        [SerializeField] private GameObject systemTab;

        [Header("Tab Buttons")]
        [SerializeField] private Button generalTabButton;
        [SerializeField] private Button graphicsTabButton;
        [SerializeField] private Button audioTabButton;
        [SerializeField] private Button gameplayTabButton;
        [SerializeField] private Button controlsTabButton;
        [SerializeField] private Button uiTabButton;
        [SerializeField] private Button accessibilityTabButton;
        [SerializeField] private Button networkTabButton;
        [SerializeField] private Button systemTabButton;

        [Header("Action Buttons")]
        [SerializeField] private Button applyButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button closeButton;

        [Header("General Settings UI")]
        [SerializeField] private TMP_Dropdown languageDropdown;
        [SerializeField] private Slider uiScaleSlider;
        [SerializeField] private TMP_Text uiScaleText;
        [SerializeField] private TMP_Dropdown themeDropdown;
        [SerializeField] private Toggle tooltipsToggle;
        [SerializeField] private Toggle tutorialsToggle;
        [SerializeField] private TMP_Dropdown autoSaveDropdown;
        [SerializeField] private Toggle devConsoleToggle;

        [Header("Graphics Settings UI - Display")]
        [SerializeField] private TMP_Dropdown fullscreenDropdown;
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private TMP_Dropdown refreshRateDropdown;
        [SerializeField] private TMP_Dropdown vsyncDropdown;
        [SerializeField] private TMP_Dropdown qualityPresetDropdown;

        [Header("Graphics Settings UI - Rendering")]
        [SerializeField] private TMP_Dropdown antiAliasingDropdown;
        [SerializeField] private Slider renderScaleSlider;
        [SerializeField] private TMP_Text renderScaleText;
        [SerializeField] private Slider shadowDistanceSlider;
        [SerializeField] private TMP_Text shadowDistanceText;
        [SerializeField] private TMP_Dropdown shadowQualityDropdown;
        [SerializeField] private TMP_Dropdown textureQualityDropdown;
        [SerializeField] private TMP_Dropdown anisotropicDropdown;
        [SerializeField] private TMP_Dropdown terrainQualityDropdown;
        [SerializeField] private Slider vegetationDensitySlider;
        [SerializeField] private TMP_Text vegetationDensityText;
        [SerializeField] private Slider grassDrawDistanceSlider;
        [SerializeField] private TMP_Text grassDrawDistanceText;

        [Header("Graphics Settings UI - Post Processing")]
        [SerializeField] private Toggle bloomToggle;
        [SerializeField] private Toggle motionBlurToggle;
        [SerializeField] private Slider motionBlurIntensitySlider;
        [SerializeField] private Toggle ambientOcclusionToggle;
        [SerializeField] private TMP_Dropdown colorGradingDropdown;
        [SerializeField] private Toggle depthOfFieldToggle;

        [Header("Graphics Settings UI - RTS Specific")]
        [SerializeField] private Toggle unitOutlinesToggle;
        [SerializeField] private Toggle selectionCirclesToggle;
        [SerializeField] private Toggle healthBarsToggle;
        [SerializeField] private TMP_Dropdown fogOfWarQualityDropdown;

        [Header("Audio Settings UI")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private TMP_Text masterVolumeText;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private TMP_Text musicVolumeText;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TMP_Text sfxVolumeText;
        [SerializeField] private Slider uiVolumeSlider;
        [SerializeField] private TMP_Text uiVolumeText;
        [SerializeField] private Slider voiceVolumeSlider;
        [SerializeField] private TMP_Text voiceVolumeText;
        [SerializeField] private Slider spatialBlendSlider;
        [SerializeField] private TMP_Text spatialBlendText;
        [SerializeField] private TMP_Dropdown dynamicRangeDropdown;
        [SerializeField] private Toggle alertNotificationsToggle;
        [SerializeField] private TMP_Dropdown unitVoicesDropdown;
        [SerializeField] private TMP_Dropdown battleSFXDropdown;

        [Header("Gameplay Settings UI - Camera")]
        [SerializeField] private Slider cameraPanSpeedSlider;
        [SerializeField] private TMP_Text cameraPanSpeedText;
        [SerializeField] private Slider cameraRotationSpeedSlider;
        [SerializeField] private TMP_Text cameraRotationSpeedText;
        [SerializeField] private Toggle edgeScrollingToggle;
        [SerializeField] private Slider edgeScrollSensitivitySlider;
        [SerializeField] private Slider zoomSpeedSlider;
        [SerializeField] private TMP_Text zoomSpeedText;
        [SerializeField] private Slider minZoomSlider;
        [SerializeField] private Slider maxZoomSlider;
        [SerializeField] private Toggle invertPanningToggle;
        [SerializeField] private Toggle invertZoomToggle;

        [Header("Gameplay Settings UI - Difficulty")]
        [SerializeField] private TMP_Dropdown aiDifficultyDropdown;
        [SerializeField] private TMP_Dropdown gameSpeedDropdown;

        [Header("Gameplay Settings UI - RTS Mechanics")]
        [SerializeField] private Toggle autoUnitGroupingToggle;
        [SerializeField] private Toggle smartPathfindingToggle;
        [SerializeField] private Toggle unitCollisionToggle;
        [SerializeField] private Toggle minimapRotationToggle;
        [SerializeField] private TMP_Dropdown minimapIconSizeDropdown;
        [SerializeField] private Toggle autoRebuildWorkersToggle;
        [SerializeField] private Toggle autoRepairBuildingsToggle;

        [Header("Control Settings UI")]
        [SerializeField] private Slider mouseSensitivitySlider;
        [SerializeField] private TMP_Text mouseSensitivityText;
        [SerializeField] private Toggle mouseSmoothingToggle;
        [SerializeField] private TMP_Dropdown cameraDragButtonDropdown;
        [SerializeField] private TMP_Dropdown commandButtonDropdown;
        [SerializeField] private TMP_Dropdown unitControlStyleDropdown;
        [SerializeField] private Button rebindKeysButton;
        [SerializeField] private TMP_Text keybindInfoText;

        [Header("UI Settings UI")]
        [SerializeField] private Slider uiScaleSlider2;
        [SerializeField] private TMP_Text uiScaleText2;
        [SerializeField] private TMP_Dropdown nameplatesDropdown;
        [SerializeField] private Toggle damageNumbersToggle;
        [SerializeField] private TMP_Dropdown cursorStyleDropdown;
        [SerializeField] private Toggle flashAlertsToggle;
        [SerializeField] private TMP_Dropdown colorblindModeDropdown;

        [Header("Accessibility Settings UI")]
        [SerializeField] private Toggle highContrastToggle;
        [SerializeField] private TMP_Dropdown colorblindFilterDropdown;
        [SerializeField] private Toggle subtitlesToggle;
        [SerializeField] private Slider subtitleSizeSlider;
        [SerializeField] private Slider subtitleOpacitySlider;
        [SerializeField] private Toggle reducedCameraShakeToggle;
        [SerializeField] private Toggle simplifiedEffectsToggle;

        [Header("Network Settings UI (Placeholder)")]
        [SerializeField] private TMP_Dropdown regionDropdown;
        [SerializeField] private Slider maxPingSlider;
        [SerializeField] private TMP_Text maxPingText;
        [SerializeField] private Toggle autoReconnectToggle;
        [SerializeField] private TMP_Dropdown packetRateDropdown;
        [SerializeField] private Toggle voiceChatToggle;
        [SerializeField] private Slider voiceChatVolumeSlider;
        [SerializeField] private Toggle pushToTalkToggle;

        [Header("System Settings UI")]
        [SerializeField] private Toggle diagnosticsLogToggle;
        [SerializeField] private TMP_Dropdown fpsCounterDropdown;
        [SerializeField] private TMP_Dropdown fpsCapDropdown;
        [SerializeField] private Button clearCacheButton;
        [SerializeField] private Button openSaveFolderButton;

        // Services
        private ISettingsService settingsService;
        private IAudioService audioService;

        // Current tab
        private GameObject currentTab;

        // Resolutions
        private Resolution[] availableResolutions;

        private void Awake()
        {
            // Get services
            settingsService = ServiceLocator.TryGet<ISettingsService>();
            audioService = ServiceLocator.TryGet<IAudioService>();

            // Setup buttons
            SetupTabButtons();
            SetupActionButtons();
            SetupSystemButtons();

            // Initialize dropdowns
            InitializeDropdowns();

            // Initialize resolutions
            InitializeResolutions();

            // Load current settings into UI
            LoadSettingsIntoUI();

            // Setup listeners
            SetupListeners();

            // Show first tab
            ShowTab(generalTab);
        }

        #region Tab Management

        private void SetupTabButtons()
        {
            if (generalTabButton != null)
                generalTabButton.onClick.AddListener(() => ShowTab(generalTab));
            if (graphicsTabButton != null)
                graphicsTabButton.onClick.AddListener(() => ShowTab(graphicsTab));
            if (audioTabButton != null)
                audioTabButton.onClick.AddListener(() => ShowTab(audioTab));
            if (gameplayTabButton != null)
                gameplayTabButton.onClick.AddListener(() => ShowTab(gameplayTab));
            if (controlsTabButton != null)
                controlsTabButton.onClick.AddListener(() => ShowTab(controlsTab));
            if (uiTabButton != null)
                uiTabButton.onClick.AddListener(() => ShowTab(uiTab));
            if (accessibilityTabButton != null)
                accessibilityTabButton.onClick.AddListener(() => ShowTab(accessibilityTab));
            if (networkTabButton != null)
                networkTabButton.onClick.AddListener(() => ShowTab(networkTab));
            if (systemTabButton != null)
                systemTabButton.onClick.AddListener(() => ShowTab(systemTab));
        }

        private void SetupActionButtons()
        {
            if (applyButton != null)
                applyButton.onClick.AddListener(OnApplyClicked);
            if (resetButton != null)
                resetButton.onClick.AddListener(OnResetClicked);
            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseClicked);
        }

        private void SetupSystemButtons()
        {
            if (clearCacheButton != null)
                clearCacheButton.onClick.AddListener(OnClearCacheClicked);
            if (openSaveFolderButton != null)
                openSaveFolderButton.onClick.AddListener(OnOpenSaveFolderClicked);
            if (rebindKeysButton != null)
                rebindKeysButton.onClick.AddListener(OnRebindKeysClicked);
        }

        private void ShowTab(GameObject tab)
        {
            if (currentTab != null)
                currentTab.SetActive(false);

            if (tab != null)
            {
                tab.SetActive(true);
                currentTab = tab;
            }
        }

        #endregion

        #region Initialize Dropdowns

        private void InitializeDropdowns()
        {
            // General
            InitializeDropdown(languageDropdown, System.Enum.GetNames(typeof(Language)));
            InitializeDropdown(themeDropdown, System.Enum.GetNames(typeof(ThemeType)));
            InitializeDropdown(autoSaveDropdown, new[] { "Off", "5 Minutes", "10 Minutes", "20 Minutes" });

            // Graphics - Display
            InitializeDropdown(fullscreenDropdown, new[] { "Fullscreen", "Borderless", "Windowed" });
            InitializeDropdown(vsyncDropdown, new[] { "Off", "On", "On (Half Rate)" });
            InitializeDropdown(qualityPresetDropdown, System.Enum.GetNames(typeof(QualityPreset)));

            // Graphics - Rendering
            InitializeDropdown(antiAliasingDropdown, System.Enum.GetNames(typeof(AntiAliasingMode)));
            InitializeDropdown(shadowQualityDropdown, new[] { "Off", "Low", "Medium", "High", "Very High" });
            InitializeDropdown(textureQualityDropdown, new[] { "Quarter", "Half", "Full" });
            InitializeDropdown(anisotropicDropdown, new[] { "Off", "Per-Texture", "Forced 16x" });
            InitializeDropdown(terrainQualityDropdown, new[] { "Low", "Medium", "High" });

            // Graphics - Post Processing
            InitializeDropdown(colorGradingDropdown, new[] { "Neutral", "Filmic" });

            // Graphics - RTS Specific
            InitializeDropdown(fogOfWarQualityDropdown, new[] { "Low", "Medium", "High" });

            // Audio
            InitializeDropdown(dynamicRangeDropdown, new[] { "Night Mode", "Normal", "Wide" });
            InitializeDropdown(unitVoicesDropdown, new[] { "Classic", "Modern", "Off" });
            InitializeDropdown(battleSFXDropdown, new[] { "Low", "Normal", "High" });

            // Gameplay
            InitializeDropdown(aiDifficultyDropdown, new[] { "Easy", "Normal", "Hard", "Brutal" });
            InitializeDropdown(gameSpeedDropdown, new[] { "0.5x", "1x", "1.5x", "2x" });
            InitializeDropdown(minimapIconSizeDropdown, new[] { "Small", "Medium", "Large" });

            // Controls
            InitializeDropdown(cameraDragButtonDropdown, new[] { "Right Click", "Left Click", "Middle Click" });
            InitializeDropdown(commandButtonDropdown, new[] { "Right Click", "Left Click" });
            InitializeDropdown(unitControlStyleDropdown, new[] { "Classic RTS", "Modern RTS", "Custom" });

            // UI
            InitializeDropdown(nameplatesDropdown, new[] { "Off", "On", "Only Selected" });
            InitializeDropdown(cursorStyleDropdown, new[] { "Default", "Modern", "Classic" });
            InitializeDropdown(colorblindModeDropdown, new[] { "Off", "Deuteranopia", "Protanopia", "Tritanopia" });

            // Accessibility
            InitializeDropdown(colorblindFilterDropdown, new[] { "Off", "Deuteranopia", "Protanopia", "Tritanopia" });

            // Network
            InitializeDropdown(regionDropdown, new[] { "Auto", "North America", "Europe", "Asia", "South America", "Oceania" });
            InitializeDropdown(packetRateDropdown, new[] { "Low", "Medium", "High" });

            // System
            InitializeDropdown(fpsCounterDropdown, new[] { "Off", "Simple", "Detailed" });
            InitializeDropdown(fpsCapDropdown, new[] { "Off", "30", "60", "120", "Unlimited" });
        }

        private void InitializeDropdown(TMP_Dropdown dropdown, string[] options)
        {
            if (dropdown == null) return;

            dropdown.ClearOptions();
            dropdown.AddOptions(options.ToList());
        }

        private void InitializeResolutions()
        {
            if (resolutionDropdown == null) return;

            availableResolutions = Screen.resolutions;
            resolutionDropdown.ClearOptions();

            List<string> options = new List<string>();
            int currentResolutionIndex = 0;

            for (int i = 0; i < availableResolutions.Length; i++)
            {
                string option = $"{availableResolutions[i].width} x {availableResolutions[i].height}";
                options.Add(option);

                if (availableResolutions[i].width == Screen.currentResolution.width &&
                    availableResolutions[i].height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = i;
                }
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentResolutionIndex;
            resolutionDropdown.RefreshShownValue();
        }

        #endregion

        #region Load Settings Into UI

        private void LoadSettingsIntoUI()
        {
            if (settingsService == null) return;

            LoadGeneralSettings();
            LoadGraphicsSettings();
            LoadAudioSettings();
            LoadGameplaySettings();
            LoadControlSettings();
            LoadUISettings();
            LoadAccessibilitySettings();
            LoadNetworkSettings();
            LoadSystemSettings();
        }

        private void LoadGeneralSettings()
        {
            var settings = settingsService.General;
            if (settings == null) return;

            SetDropdownValue(languageDropdown, (int)settings.Language);
            SetSliderValue(uiScaleSlider, uiScaleText, settings.UIScale, "0.0");
            SetDropdownValue(themeDropdown, (int)settings.Theme);
            SetToggle(tooltipsToggle, settings.ShowTooltips);
            SetToggle(tutorialsToggle, settings.ShowTutorials);
            SetDropdownValue(autoSaveDropdown, (int)settings.AutoSave);
            SetToggle(devConsoleToggle, settings.EnableDeveloperConsole);
        }

        private void LoadGraphicsSettings()
        {
            var settings = settingsService.Graphics;
            if (settings == null) return;

            // Display
            SetDropdownValue(fullscreenDropdown, (int)settings.FullscreenMode);
            SetDropdownValue(vsyncDropdown, (int)settings.VSync);
            SetDropdownValue(qualityPresetDropdown, (int)settings.QualityPreset);

            // Rendering
            SetDropdownValue(antiAliasingDropdown, (int)settings.AntiAliasing);
            SetSliderValue(renderScaleSlider, renderScaleText, settings.RenderScale, "0.0");
            SetSliderValue(shadowDistanceSlider, shadowDistanceText, settings.ShadowDistance, "0");
            SetDropdownValue(shadowQualityDropdown, (int)settings.ShadowResolution);
            SetDropdownValue(textureQualityDropdown, (int)settings.TextureQuality);
            SetDropdownValue(anisotropicDropdown, (int)settings.AnisotropicFiltering);
            SetDropdownValue(terrainQualityDropdown, (int)settings.TerrainQuality);
            SetSliderValue(vegetationDensitySlider, vegetationDensityText, settings.VegetationDensity, "0%");
            SetSliderValue(grassDrawDistanceSlider, grassDrawDistanceText, settings.GrassDrawDistance, "0");

            // Post Processing
            SetToggle(bloomToggle, settings.Bloom);
            SetToggle(motionBlurToggle, settings.MotionBlur);
            SetSliderValue(motionBlurIntensitySlider, null, settings.MotionBlurIntensity, "0.0");
            SetToggle(ambientOcclusionToggle, settings.AmbientOcclusion);
            SetDropdownValue(colorGradingDropdown, (int)settings.ColorGrading);
            SetToggle(depthOfFieldToggle, settings.DepthOfField);

            // RTS Specific
            SetToggle(unitOutlinesToggle, settings.UnitHighlightOutlines);
            SetToggle(selectionCirclesToggle, settings.SelectionCircles);
            SetToggle(healthBarsToggle, settings.HealthBars);
            SetDropdownValue(fogOfWarQualityDropdown, (int)settings.FogOfWarQuality);
        }

        private void LoadAudioSettings()
        {
            var settings = settingsService.Audio;
            if (settings == null) return;

            SetSliderValue(masterVolumeSlider, masterVolumeText, settings.MasterVolume, "0%");
            SetSliderValue(musicVolumeSlider, musicVolumeText, settings.MusicVolume, "0%");
            SetSliderValue(sfxVolumeSlider, sfxVolumeText, settings.SFXVolume, "0%");
            SetSliderValue(uiVolumeSlider, uiVolumeText, settings.UIVolume, "0%");
            SetSliderValue(voiceVolumeSlider, voiceVolumeText, settings.VoiceVolume, "0%");
            SetSliderValue(spatialBlendSlider, spatialBlendText, settings.SpatialBlend, "0.0");
            SetDropdownValue(dynamicRangeDropdown, (int)settings.DynamicRange);
            SetToggle(alertNotificationsToggle, settings.AlertNotifications);
            SetDropdownValue(unitVoicesDropdown, (int)settings.UnitVoices);
            SetDropdownValue(battleSFXDropdown, (int)settings.BattleSFXIntensity);
        }

        private void LoadGameplaySettings()
        {
            var settings = settingsService.Gameplay;
            if (settings == null) return;

            // Camera
            SetSliderValue(cameraPanSpeedSlider, cameraPanSpeedText, settings.CameraPanSpeed, "0");
            SetSliderValue(cameraRotationSpeedSlider, cameraRotationSpeedText, settings.CameraRotationSpeed, "0");
            SetToggle(edgeScrollingToggle, settings.EdgeScrolling);
            SetSliderValue(edgeScrollSensitivitySlider, null, settings.EdgeScrollSensitivity, "0.0");
            SetSliderValue(zoomSpeedSlider, zoomSpeedText, settings.ZoomSpeed, "0");
            SetSliderValue(minZoomSlider, null, settings.MinZoom, "0");
            SetSliderValue(maxZoomSlider, null, settings.MaxZoom, "0");
            SetToggle(invertPanningToggle, settings.InvertPanning);
            SetToggle(invertZoomToggle, settings.InvertZoom);

            // Difficulty
            SetDropdownValue(aiDifficultyDropdown, (int)settings.AIDifficulty);
            SetDropdownValue(gameSpeedDropdown, (int)settings.GameSpeed);

            // RTS Mechanics
            SetToggle(autoUnitGroupingToggle, settings.AutomaticUnitGrouping);
            SetToggle(smartPathfindingToggle, settings.SmartPathfinding);
            SetToggle(unitCollisionToggle, settings.EnableUnitCollision);
            SetToggle(minimapRotationToggle, settings.MinimapRotation);
            SetDropdownValue(minimapIconSizeDropdown, (int)settings.MinimapIconSize);
            SetToggle(autoRebuildWorkersToggle, settings.AutoRebuildWorkers);
            SetToggle(autoRepairBuildingsToggle, settings.AutoRepairBuildings);
        }

        private void LoadControlSettings()
        {
            var settings = settingsService.Controls;
            if (settings == null) return;

            SetSliderValue(mouseSensitivitySlider, mouseSensitivityText, settings.MouseSensitivity, "0.0");
            SetToggle(mouseSmoothingToggle, settings.MouseSmoothing);
            SetDropdownValue(cameraDragButtonDropdown, (int)settings.CameraDragButton);
            SetDropdownValue(commandButtonDropdown, (int)settings.CommandButton);
            SetDropdownValue(unitControlStyleDropdown, (int)settings.UnitControlStyle);
        }

        private void LoadUISettings()
        {
            var settings = settingsService.UI;
            if (settings == null) return;

            SetSliderValue(uiScaleSlider2, uiScaleText2, settings.UIScale, "0.0");
            SetDropdownValue(nameplatesDropdown, (int)settings.Nameplates);
            SetToggle(damageNumbersToggle, settings.DamageNumbers);
            SetDropdownValue(cursorStyleDropdown, (int)settings.CursorStyle);
            SetToggle(flashAlertsToggle, settings.FlashAlerts);
            SetDropdownValue(colorblindModeDropdown, (int)settings.ColorblindMode);
        }

        private void LoadAccessibilitySettings()
        {
            var settings = settingsService.Accessibility;
            if (settings == null) return;

            SetToggle(highContrastToggle, settings.HighContrastMode);
            SetDropdownValue(colorblindFilterDropdown, (int)settings.ColorblindFilter);
            SetToggle(subtitlesToggle, settings.Subtitles);
            SetSliderValue(subtitleSizeSlider, null, settings.SubtitleSize, "0.0");
            SetSliderValue(subtitleOpacitySlider, null, settings.SubtitleBackgroundOpacity, "0.0");
            SetToggle(reducedCameraShakeToggle, settings.ReducedCameraShake);
            SetToggle(simplifiedEffectsToggle, settings.SimplifiedEffects);
        }

        private void LoadNetworkSettings()
        {
            var settings = settingsService.Network;
            if (settings == null) return;

            SetDropdownValue(regionDropdown, (int)settings.Region);
            SetSliderValue(maxPingSlider, maxPingText, settings.MaxPing, "0");
            SetToggle(autoReconnectToggle, settings.AutoReconnect);
            SetDropdownValue(packetRateDropdown, (int)settings.PacketRate);
            SetToggle(voiceChatToggle, settings.VoiceChat);
            SetSliderValue(voiceChatVolumeSlider, null, settings.VoiceChatVolume, "0.0");
            SetToggle(pushToTalkToggle, settings.PushToTalk);
        }

        private void LoadSystemSettings()
        {
            var settings = settingsService.System;
            if (settings == null) return;

            SetToggle(diagnosticsLogToggle, settings.DiagnosticsLog);
            SetDropdownValue(fpsCounterDropdown, (int)settings.FPSCounter);
            SetDropdownValue(fpsCapDropdown, (int)settings.FPSCap);
        }

        #endregion

        #region Helper Methods

        private void SetDropdownValue(TMP_Dropdown dropdown, int value)
        {
            if (dropdown != null)
            {
                dropdown.value = value;
                dropdown.RefreshShownValue();
            }
        }

        private void SetSliderValue(Slider slider, TMP_Text text, float value, string format)
        {
            if (slider != null)
            {
                slider.value = value;
                if (text != null)
                {
                    if (format.Contains("%"))
                        text.text = $"{(value * 100):F0}%";
                    else if (format.Contains("0.0"))
                        text.text = value.ToString("F1");
                    else
                        text.text = value.ToString("F0");
                }
            }
        }

        private void SetToggle(Toggle toggle, bool value)
        {
            if (toggle != null)
                toggle.isOn = value;
        }

        #endregion

        #region Setup Listeners

        private void SetupListeners()
        {
            // Setup sliders with text updates
            SetupSliderListener(uiScaleSlider, uiScaleText, "%");
            SetupSliderListener(renderScaleSlider, renderScaleText, "F1");
            SetupSliderListener(shadowDistanceSlider, shadowDistanceText, "F0");
            SetupSliderListener(vegetationDensitySlider, vegetationDensityText, "%");
            SetupSliderListener(grassDrawDistanceSlider, grassDrawDistanceText, "F0");
            SetupSliderListener(masterVolumeSlider, masterVolumeText, "%");
            SetupSliderListener(musicVolumeSlider, musicVolumeText, "%");
            SetupSliderListener(sfxVolumeSlider, sfxVolumeText, "%");
            SetupSliderListener(uiVolumeSlider, uiVolumeText, "%");
            SetupSliderListener(voiceVolumeSlider, voiceVolumeText, "%");
            SetupSliderListener(spatialBlendSlider, spatialBlendText, "F1");
            SetupSliderListener(cameraPanSpeedSlider, cameraPanSpeedText, "F0");
            SetupSliderListener(cameraRotationSpeedSlider, cameraRotationSpeedText, "F0");
            SetupSliderListener(zoomSpeedSlider, zoomSpeedText, "F0");
            SetupSliderListener(mouseSensitivitySlider, mouseSensitivityText, "F1");
            SetupSliderListener(uiScaleSlider2, uiScaleText2, "F1");
            SetupSliderListener(maxPingSlider, maxPingText, "F0");
        }

        private void SetupSliderListener(Slider slider, TMP_Text text, string format)
        {
            if (slider == null || text == null) return;

            slider.onValueChanged.AddListener((value) =>
            {
                if (format == "%")
                    text.text = $"{(value * 100):F0}%";
                else if (format == "F1")
                    text.text = value.ToString("F1");
                else
                    text.text = value.ToString("F0");
            });
        }

        #endregion

        #region Save Settings From UI

        private void SaveSettingsFromUI()
        {
            if (settingsService == null) return;

            SaveGeneralSettings();
            SaveGraphicsSettings();
            SaveAudioSettings();
            SaveGameplaySettings();
            SaveControlSettings();
            SaveUISettings();
            SaveAccessibilitySettings();
            SaveNetworkSettings();
            SaveSystemSettings();
        }

        private void SaveGeneralSettings()
        {
            var settings = settingsService.General;
            if (settings == null) return;

            if (languageDropdown != null) settings.Language = (Language)languageDropdown.value;
            if (uiScaleSlider != null) settings.UIScale = uiScaleSlider.value;
            if (themeDropdown != null) settings.Theme = (ThemeType)themeDropdown.value;
            if (tooltipsToggle != null) settings.ShowTooltips = tooltipsToggle.isOn;
            if (tutorialsToggle != null) settings.ShowTutorials = tutorialsToggle.isOn;
            if (autoSaveDropdown != null) settings.AutoSave = (AutoSaveInterval)autoSaveDropdown.value;
            if (devConsoleToggle != null) settings.EnableDeveloperConsole = devConsoleToggle.isOn;
        }

        private void SaveGraphicsSettings()
        {
            var settings = settingsService.Graphics;
            if (settings == null) return;

            // Display
            if (fullscreenDropdown != null) settings.FullscreenMode = (FullscreenModeType)fullscreenDropdown.value;
            if (resolutionDropdown != null && availableResolutions != null && resolutionDropdown.value < availableResolutions.Length)
            {
                settings.ResolutionWidth = availableResolutions[resolutionDropdown.value].width;
                settings.ResolutionHeight = availableResolutions[resolutionDropdown.value].height;
                settings.RefreshRate = (int)(availableResolutions[resolutionDropdown.value].refreshRateRatio.value);
            }
            if (vsyncDropdown != null) settings.VSync = (VSyncMode)vsyncDropdown.value;
            if (qualityPresetDropdown != null) settings.QualityPreset = (QualityPreset)qualityPresetDropdown.value;

            // Rendering
            if (antiAliasingDropdown != null) settings.AntiAliasing = (AntiAliasingMode)antiAliasingDropdown.value;
            if (renderScaleSlider != null) settings.RenderScale = renderScaleSlider.value;
            if (shadowDistanceSlider != null) settings.ShadowDistance = shadowDistanceSlider.value;
            if (shadowQualityDropdown != null) settings.ShadowResolution = (ShadowResolutionQuality)shadowQualityDropdown.value;
            if (textureQualityDropdown != null) settings.TextureQuality = (TextureQuality)textureQualityDropdown.value;
            if (anisotropicDropdown != null) settings.AnisotropicFiltering = (AnisotropicMode)anisotropicDropdown.value;
            if (terrainQualityDropdown != null) settings.TerrainQuality = (TerrainQuality)terrainQualityDropdown.value;
            if (vegetationDensitySlider != null) settings.VegetationDensity = vegetationDensitySlider.value;
            if (grassDrawDistanceSlider != null) settings.GrassDrawDistance = grassDrawDistanceSlider.value;

            // Post Processing
            if (bloomToggle != null) settings.Bloom = bloomToggle.isOn;
            if (motionBlurToggle != null) settings.MotionBlur = motionBlurToggle.isOn;
            if (motionBlurIntensitySlider != null) settings.MotionBlurIntensity = motionBlurIntensitySlider.value;
            if (ambientOcclusionToggle != null) settings.AmbientOcclusion = ambientOcclusionToggle.isOn;
            if (colorGradingDropdown != null) settings.ColorGrading = (ColorGradingMode)colorGradingDropdown.value;
            if (depthOfFieldToggle != null) settings.DepthOfField = depthOfFieldToggle.isOn;

            // RTS Specific
            if (unitOutlinesToggle != null) settings.UnitHighlightOutlines = unitOutlinesToggle.isOn;
            if (selectionCirclesToggle != null) settings.SelectionCircles = selectionCirclesToggle.isOn;
            if (healthBarsToggle != null) settings.HealthBars = healthBarsToggle.isOn;
            if (fogOfWarQualityDropdown != null) settings.FogOfWarQuality = (FogOfWarQuality)fogOfWarQualityDropdown.value;
        }

        private void SaveAudioSettings()
        {
            var settings = settingsService.Audio;
            if (settings == null) return;

            if (masterVolumeSlider != null) settings.MasterVolume = masterVolumeSlider.value;
            if (musicVolumeSlider != null) settings.MusicVolume = musicVolumeSlider.value;
            if (sfxVolumeSlider != null) settings.SFXVolume = sfxVolumeSlider.value;
            if (uiVolumeSlider != null) settings.UIVolume = uiVolumeSlider.value;
            if (voiceVolumeSlider != null) settings.VoiceVolume = voiceVolumeSlider.value;
            if (spatialBlendSlider != null) settings.SpatialBlend = spatialBlendSlider.value;
            if (dynamicRangeDropdown != null) settings.DynamicRange = (DynamicRange)dynamicRangeDropdown.value;
            if (alertNotificationsToggle != null) settings.AlertNotifications = alertNotificationsToggle.isOn;
            if (unitVoicesDropdown != null) settings.UnitVoices = (UnitVoiceStyle)unitVoicesDropdown.value;
            if (battleSFXDropdown != null) settings.BattleSFXIntensity = (BattleSFXIntensity)battleSFXDropdown.value;
        }

        private void SaveGameplaySettings()
        {
            var settings = settingsService.Gameplay;
            if (settings == null) return;

            // Camera
            if (cameraPanSpeedSlider != null) settings.CameraPanSpeed = cameraPanSpeedSlider.value;
            if (cameraRotationSpeedSlider != null) settings.CameraRotationSpeed = cameraRotationSpeedSlider.value;
            if (edgeScrollingToggle != null) settings.EdgeScrolling = edgeScrollingToggle.isOn;
            if (edgeScrollSensitivitySlider != null) settings.EdgeScrollSensitivity = edgeScrollSensitivitySlider.value;
            if (zoomSpeedSlider != null) settings.ZoomSpeed = zoomSpeedSlider.value;
            if (minZoomSlider != null) settings.MinZoom = minZoomSlider.value;
            if (maxZoomSlider != null) settings.MaxZoom = maxZoomSlider.value;
            if (invertPanningToggle != null) settings.InvertPanning = invertPanningToggle.isOn;
            if (invertZoomToggle != null) settings.InvertZoom = invertZoomToggle.isOn;

            // Difficulty
            if (aiDifficultyDropdown != null) settings.AIDifficulty = (AIDifficulty)aiDifficultyDropdown.value;
            if (gameSpeedDropdown != null) settings.GameSpeed = (GameSpeedMultiplier)gameSpeedDropdown.value;

            // RTS Mechanics
            if (autoUnitGroupingToggle != null) settings.AutomaticUnitGrouping = autoUnitGroupingToggle.isOn;
            if (smartPathfindingToggle != null) settings.SmartPathfinding = smartPathfindingToggle.isOn;
            if (unitCollisionToggle != null) settings.EnableUnitCollision = unitCollisionToggle.isOn;
            if (minimapRotationToggle != null) settings.MinimapRotation = minimapRotationToggle.isOn;
            if (minimapIconSizeDropdown != null) settings.MinimapIconSize = (MinimapIconSize)minimapIconSizeDropdown.value;
            if (autoRebuildWorkersToggle != null) settings.AutoRebuildWorkers = autoRebuildWorkersToggle.isOn;
            if (autoRepairBuildingsToggle != null) settings.AutoRepairBuildings = autoRepairBuildingsToggle.isOn;
        }

        private void SaveControlSettings()
        {
            var settings = settingsService.Controls;
            if (settings == null) return;

            if (mouseSensitivitySlider != null) settings.MouseSensitivity = mouseSensitivitySlider.value;
            if (mouseSmoothingToggle != null) settings.MouseSmoothing = mouseSmoothingToggle.isOn;
            if (cameraDragButtonDropdown != null) settings.CameraDragButton = (CameraDragButton)cameraDragButtonDropdown.value;
            if (commandButtonDropdown != null) settings.CommandButton = (CommandButtonStyle)commandButtonDropdown.value;
            if (unitControlStyleDropdown != null) settings.UnitControlStyle = (UnitControlStyle)unitControlStyleDropdown.value;
        }

        private void SaveUISettings()
        {
            var settings = settingsService.UI;
            if (settings == null) return;

            if (uiScaleSlider2 != null) settings.UIScale = uiScaleSlider2.value;
            if (nameplatesDropdown != null) settings.Nameplates = (NameplateMode)nameplatesDropdown.value;
            if (damageNumbersToggle != null) settings.DamageNumbers = damageNumbersToggle.isOn;
            if (cursorStyleDropdown != null) settings.CursorStyle = (CursorStyle)cursorStyleDropdown.value;
            if (flashAlertsToggle != null) settings.FlashAlerts = flashAlertsToggle.isOn;
            if (colorblindModeDropdown != null) settings.ColorblindMode = (ColorblindMode)colorblindModeDropdown.value;
        }

        private void SaveAccessibilitySettings()
        {
            var settings = settingsService.Accessibility;
            if (settings == null) return;

            if (highContrastToggle != null) settings.HighContrastMode = highContrastToggle.isOn;
            if (colorblindFilterDropdown != null) settings.ColorblindFilter = (ColorblindMode)colorblindFilterDropdown.value;
            if (subtitlesToggle != null) settings.Subtitles = subtitlesToggle.isOn;
            if (subtitleSizeSlider != null) settings.SubtitleSize = subtitleSizeSlider.value;
            if (subtitleOpacitySlider != null) settings.SubtitleBackgroundOpacity = subtitleOpacitySlider.value;
            if (reducedCameraShakeToggle != null) settings.ReducedCameraShake = reducedCameraShakeToggle.isOn;
            if (simplifiedEffectsToggle != null) settings.SimplifiedEffects = simplifiedEffectsToggle.isOn;
        }

        private void SaveNetworkSettings()
        {
            var settings = settingsService.Network;
            if (settings == null) return;

            if (regionDropdown != null) settings.Region = (NetworkRegion)regionDropdown.value;
            if (maxPingSlider != null) settings.MaxPing = (int)maxPingSlider.value;
            if (autoReconnectToggle != null) settings.AutoReconnect = autoReconnectToggle.isOn;
            if (packetRateDropdown != null) settings.PacketRate = (PacketRate)packetRateDropdown.value;
            if (voiceChatToggle != null) settings.VoiceChat = voiceChatToggle.isOn;
            if (voiceChatVolumeSlider != null) settings.VoiceChatVolume = voiceChatVolumeSlider.value;
            if (pushToTalkToggle != null) settings.PushToTalk = pushToTalkToggle.isOn;
        }

        private void SaveSystemSettings()
        {
            var settings = settingsService.System;
            if (settings == null) return;

            if (diagnosticsLogToggle != null) settings.DiagnosticsLog = diagnosticsLogToggle.isOn;
            if (fpsCounterDropdown != null) settings.FPSCounter = (FPSCounterMode)fpsCounterDropdown.value;
            if (fpsCapDropdown != null) settings.FPSCap = (FPSCapMode)fpsCapDropdown.value;
        }

        #endregion

        #region Button Handlers

        private void OnApplyClicked()
        {
            SaveSettingsFromUI();
            settingsService?.ApplySettings();
            settingsService?.SaveSettings();
        }

        private void OnResetClicked()
        {
            settingsService?.ResetToDefaults();
            LoadSettingsIntoUI();
        }

        private void OnCloseClicked()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }

        private void OnClearCacheClicked()
        {
            Caching.ClearCache();
        }

        private void OnOpenSaveFolderClicked()
        {
            Application.OpenURL(Application.persistentDataPath);
        }

        private void OnRebindKeysClicked()
        {
            // Placeholder for keybind UI
            if (keybindInfoText != null)
                keybindInfoText.text = "Keybind rebinding system coming soon!";
        }

        #endregion

        #region Public Methods

        public void Open()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
                LoadSettingsIntoUI();
                ShowTab(generalTab);
            }
        }

        public void Close()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }

        #endregion
    }
}
