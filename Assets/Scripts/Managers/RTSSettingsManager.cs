using UnityEngine;
using UnityEngine.Rendering.Universal;
using RTS.Core.Services;
using RTSGame.Settings;
using System;
using System.IO;
using AudioSettings = RTSGame.Settings.AudioSettings;

namespace RTSGame.Managers
{
    /// <summary>
    /// Manages all game settings including graphics, audio, gameplay, controls, UI, etc.
    /// Implements the ISettingsService interface.
    /// </summary>
    public class RTSSettingsManager : MonoBehaviour, ISettingsService
    {
        private const string SETTINGS_FILE_NAME = "game_settings.json";

        [Header("Dependencies")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private UniversalRenderPipelineAsset urpAsset;

        // Settings
        private GameSettings currentSettings;

        // Events
        public event Action OnSettingsChanged;
        public event Action<QualityPreset> OnQualityPresetChanged;

        // Properties
        public GameSettings CurrentSettings => currentSettings;
        public GeneralSettings General => currentSettings?.General;
        public GraphicsSettings Graphics => currentSettings?.Graphics;
        public AudioSettings Audio => currentSettings?.Audio;
        public GameplaySettings Gameplay => currentSettings?.Gameplay;
        public ControlSettings Controls => currentSettings?.Controls;
        public UISettings UI => currentSettings?.UI;
        public AccessibilitySettings Accessibility => currentSettings?.Accessibility;
        public NetworkSettings Network => currentSettings?.Network;
        public SystemSettings System => currentSettings?.System;

        private string SettingsFilePath => Path.Combine(Application.persistentDataPath, SETTINGS_FILE_NAME);

        private void Awake()
        {
            // Try to find main camera if not assigned
            if (mainCamera == null)
                mainCamera = Camera.main;

            // Initialize settings
            currentSettings = GameSettings.CreateDefault();
            LoadSettings();
        }

        private void Start()
        {
            // Apply all settings on start
            ApplySettings();
        }

        #region Settings Management

        public void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    currentSettings = JsonUtility.FromJson<GameSettings>(json);
                    Debug.Log("[RTSSettingsManager] Settings loaded successfully.");
                }
                else
                {
                    Debug.Log("[RTSSettingsManager] No settings file found, using defaults.");
                    currentSettings = GameSettings.CreateDefault();
                    SaveSettings(); // Save default settings
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RTSSettingsManager] Failed to load settings: {ex.Message}");
                currentSettings = GameSettings.CreateDefault();
            }
        }

        public void SaveSettings()
        {
            try
            {
                string json = JsonUtility.ToJson(currentSettings, true);
                File.WriteAllText(SettingsFilePath, json);
                Debug.Log("[RTSSettingsManager] Settings saved successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RTSSettingsManager] Failed to save settings: {ex.Message}");
            }
        }

        public void ResetToDefaults()
        {
            currentSettings = GameSettings.CreateDefault();
            ApplySettings();
            SaveSettings();
            OnSettingsChanged?.Invoke();
            Debug.Log("[RTSSettingsManager] Settings reset to defaults.");
        }

        public void ApplySettings()
        {
            ApplyGraphicsSettings();
            ApplyAudioSettings();
            ApplyGameplaySettings();
            ApplyControlSettings();
            ApplyUISettings();
            OnSettingsChanged?.Invoke();
        }

        #endregion

        #region Graphics Settings

        public void ApplyGraphicsSettings()
        {
            if (Graphics == null) return;

            // Display Settings
            ApplyDisplaySettings();

            // Quality Settings
            ApplyQualitySettings();

            // Post-Processing
            ApplyPostProcessing();

            // RTS-Specific
            ApplyRTSGraphicsSettings();

            Debug.Log("[RTSSettingsManager] Graphics settings applied.");
        }

        private void ApplyDisplaySettings()
        {
            // Fullscreen mode
            FullScreenMode fullscreenMode = Graphics.FullscreenMode switch
            {
                FullscreenModeType.Fullscreen => FullScreenMode.ExclusiveFullScreen,
                FullscreenModeType.Borderless => FullScreenMode.FullScreenWindow,
                FullscreenModeType.Windowed => FullScreenMode.Windowed,
                _ => FullScreenMode.ExclusiveFullScreen
            };

            Screen.SetResolution(Graphics.ResolutionWidth, Graphics.ResolutionHeight, fullscreenMode, Graphics.RefreshRate);

            // VSync
            QualitySettings.vSyncCount = Graphics.VSync switch
            {
                VSyncMode.Off => 0,
                VSyncMode.On => 1,
                VSyncMode.OnHalfRate => 2,
                _ => 1
            };
        }

        private void ApplyQualitySettings()
        {
            // Shadows
            QualitySettings.shadowDistance = Graphics.ShadowDistance;
            QualitySettings.shadowResolution = Graphics.ShadowResolution switch
            {
                ShadowResolutionQuality.Off => UnityEngine.ShadowResolution.Low,
                ShadowResolutionQuality.Low => UnityEngine.ShadowResolution.Low,
                ShadowResolutionQuality.Medium => UnityEngine.ShadowResolution.Medium,
                ShadowResolutionQuality.High => UnityEngine.ShadowResolution.High,
                ShadowResolutionQuality.VeryHigh => UnityEngine.ShadowResolution.VeryHigh,
                _ => UnityEngine.ShadowResolution.Medium
            };

            // Texture Quality
            QualitySettings.globalTextureMipmapLimit = Graphics.TextureQuality switch
            {
                TextureQuality.Quarter => 2,
                TextureQuality.Half => 1,
                TextureQuality.Full => 0,
                _ => 0
            };

            // Anisotropic Filtering
            QualitySettings.anisotropicFiltering = Graphics.AnisotropicFiltering switch
            {
                AnisotropicMode.Off => AnisotropicFiltering.Disable,
                AnisotropicMode.PerTexture => AnisotropicFiltering.Enable,
                AnisotropicMode.Forced16x => AnisotropicFiltering.ForceEnable,
                _ => AnisotropicFiltering.Enable
            };

            // Anti-Aliasing (URP specific - requires URP asset configuration)
            // This is a placeholder - actual implementation depends on URP setup
            if (urpAsset != null)
            {
                // Note: URP MSAA is set on the URP asset itself
                // You may need to create multiple URP assets for different quality levels
                // or modify the asset at runtime (which requires reflection or scriptable objects)
                Debug.Log($"[RTSSettingsManager] Anti-Aliasing mode: {Graphics.AntiAliasing}");
            }

            // Render Scale
            if (urpAsset != null)
            {
                urpAsset.renderScale = Graphics.RenderScale;
            }
        }

        private void ApplyPostProcessing()
        {
            // Post-processing settings would be applied here
            // This requires references to post-processing volume components
            // Placeholder for now
            Debug.Log($"[RTSSettingsManager] Post-Processing: Bloom={Graphics.Bloom}, AO={Graphics.AmbientOcclusion}, DOF={Graphics.DepthOfField}");
        }

        private void ApplyRTSGraphicsSettings()
        {
            // RTS-specific graphics settings
            // These would be applied to specific game systems
            Debug.Log($"[RTSSettingsManager] RTS Graphics: Outlines={Graphics.UnitHighlightOutlines}, Health Bars={Graphics.HealthBars}");

            // Example: Update floating number service if it exists
            var floatingNumberService = ServiceLocator.TryGet<IFloatingNumberService>();
            if (floatingNumberService != null)
            {
                floatingNumberService.Settings.SetShowHPBars(Graphics.HealthBars);
                floatingNumberService.Settings.SetShowDamageNumbers(UI?.DamageNumbers ?? true);
                floatingNumberService.RefreshSettings();
            }
        }

        public void ApplyQualityPreset(QualityPreset preset)
        {
            Graphics.QualityPreset = preset;

            switch (preset)
            {
                case QualityPreset.Low:
                    Graphics.ShadowDistance = 50f;
                    Graphics.ShadowResolution = ShadowResolutionQuality.Low;
                    Graphics.TextureQuality = TextureQuality.Half;
                    Graphics.AntiAliasing = AntiAliasingMode.Off;
                    Graphics.RenderScale = 0.75f;
                    Graphics.VegetationDensity = 0.5f;
                    Graphics.Bloom = false;
                    Graphics.AmbientOcclusion = false;
                    Graphics.MotionBlur = false;
                    Graphics.DepthOfField = false;
                    break;

                case QualityPreset.Medium:
                    Graphics.ShadowDistance = 75f;
                    Graphics.ShadowResolution = ShadowResolutionQuality.Medium;
                    Graphics.TextureQuality = TextureQuality.Half;
                    Graphics.AntiAliasing = AntiAliasingMode.FXAA;
                    Graphics.RenderScale = 1.0f;
                    Graphics.VegetationDensity = 0.7f;
                    Graphics.Bloom = true;
                    Graphics.AmbientOcclusion = false;
                    Graphics.MotionBlur = false;
                    Graphics.DepthOfField = false;
                    break;

                case QualityPreset.High:
                    Graphics.ShadowDistance = 100f;
                    Graphics.ShadowResolution = ShadowResolutionQuality.High;
                    Graphics.TextureQuality = TextureQuality.Full;
                    Graphics.AntiAliasing = AntiAliasingMode.TAA;
                    Graphics.RenderScale = 1.0f;
                    Graphics.VegetationDensity = 1.0f;
                    Graphics.Bloom = true;
                    Graphics.AmbientOcclusion = true;
                    Graphics.MotionBlur = false;
                    Graphics.DepthOfField = false;
                    break;

                case QualityPreset.Ultra:
                    Graphics.ShadowDistance = 150f;
                    Graphics.ShadowResolution = ShadowResolutionQuality.VeryHigh;
                    Graphics.TextureQuality = TextureQuality.Full;
                    Graphics.AntiAliasing = AntiAliasingMode.TAA;
                    Graphics.RenderScale = 1.2f;
                    Graphics.VegetationDensity = 1.0f;
                    Graphics.Bloom = true;
                    Graphics.AmbientOcclusion = true;
                    Graphics.MotionBlur = true;
                    Graphics.DepthOfField = true;
                    break;
            }

            if (preset != QualityPreset.Custom)
            {
                ApplyGraphicsSettings();
                OnQualityPresetChanged?.Invoke(preset);
            }
        }

        #endregion

        #region Audio Settings

        public void ApplyAudioSettings()
        {
            if (Audio == null) return;

            // Get audio service
            var audioService = ServiceLocator.TryGet<IAudioService>();
            if (audioService != null)
            {
                audioService.ApplySettings(Audio);
                Debug.Log("[RTSSettingsManager] Audio settings applied.");
            }
            else
            {
                Debug.LogWarning("[RTSSettingsManager] Audio service not found!");
            }
        }

        #endregion

        #region Gameplay Settings

        public void ApplyGameplaySettings()
        {
            if (Gameplay == null) return;

            // Apply camera settings
            ApplyCameraSettings();

            // Apply game speed
            Time.timeScale = Gameplay.GameSpeed switch
            {
                GameSpeedMultiplier.Half => 0.5f,
                GameSpeedMultiplier.Normal => 1.0f,
                GameSpeedMultiplier.Fast => 1.5f,
                GameSpeedMultiplier.VeryFast => 2.0f,
                _ => 1.0f
            };

            Debug.Log("[RTSSettingsManager] Gameplay settings applied.");
        }

        private void ApplyCameraSettings()
        {
            // Camera settings would be applied to the RTS camera controller
            // This requires a reference to RTSCameraController
            // Placeholder for now
            Debug.Log($"[RTSSettingsManager] Camera: Pan Speed={Gameplay.CameraPanSpeed}, Zoom Speed={Gameplay.ZoomSpeed}");

            // Example: If you have a reference to the camera controller:
            // var cameraController = FindObjectOfType<RTSCameraController>();
            // if (cameraController != null)
            // {
            //     cameraController.panSpeed = Gameplay.CameraPanSpeed;
            //     cameraController.zoomSpeed = Gameplay.ZoomSpeed;
            //     // etc.
            // }
        }

        #endregion

        #region Control Settings

        public void ApplyControlSettings()
        {
            if (Controls == null) return;

            // Control settings are mostly handled by Unity's Input System
            // Custom settings like mouse sensitivity would be applied here
            Debug.Log($"[RTSSettingsManager] Controls: Mouse Sensitivity={Controls.MouseSensitivity}, Drag Button={Controls.CameraDragButton}");

            // Placeholder for custom control logic
        }

        #endregion

        #region UI Settings

        public void ApplyUISettings()
        {
            if (UI == null) return;

            // Apply UI scale
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            foreach (var canvas in canvases)
            {
                if (canvas.TryGetComponent<UnityEngine.UI.CanvasScaler>(out var scaler))
                {
                    scaler.scaleFactor = UI.UIScale;
                }
            }

            // Apply colorblind mode
            ApplyColorblindMode(UI.ColorblindMode);

            Debug.Log("[RTSSettingsManager] UI settings applied.");
        }

        private void ApplyColorblindMode(ColorblindMode mode)
        {
            // Colorblind mode would require shader replacements or color adjustments
            // Placeholder for now
            Debug.Log($"[RTSSettingsManager] Colorblind Mode: {mode}");
        }

        #endregion

        #region System Settings

        public void ApplyFPSCap()
        {
            if (System == null) return;

            Application.targetFrameRate = System.FPSCap switch
            {
                FPSCapMode.Off => -1,
                FPSCapMode.Cap30 => 30,
                FPSCapMode.Cap60 => 60,
                FPSCapMode.Cap120 => 120,
                FPSCapMode.Unlimited => -1,
                _ => 60
            };
        }

        #endregion

        #region Utility Methods

        public void ClearCache()
        {
            Caching.ClearCache();
            Debug.Log("[RTSSettingsManager] Cache cleared.");
        }

        public void OpenSaveFolder()
        {
            Application.OpenURL(Application.persistentDataPath);
        }

        #endregion
    }
}
