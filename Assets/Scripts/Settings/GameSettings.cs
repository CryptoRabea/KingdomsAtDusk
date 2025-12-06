using System;
using UnityEngine;

namespace RTSGame.Settings
{
    [Serializable]
    public class GameSettings
    {
        public GeneralSettings General = new GeneralSettings();
        public GraphicsSettings Graphics = new GraphicsSettings();
        public AudioSettings Audio = new AudioSettings();
        public GameplaySettings Gameplay = new GameplaySettings();
        public ControlSettings Controls = new ControlSettings();
        public UISettings UI = new UISettings();
        public AccessibilitySettings Accessibility = new AccessibilitySettings();
        public NetworkSettings Network = new NetworkSettings();
        public SystemSettings System = new SystemSettings();

        public static GameSettings CreateDefault()
        {
            return new GameSettings();
        }
    }

    // ========== GENERAL SETTINGS ==========
    [Serializable]
    public class GeneralSettings
    {
        public Language Language = Language.English;
        public float UIScale = 1.0f;
        public ThemeType Theme = ThemeType.Dark;
        public bool ShowTooltips = true;
        public bool ShowTutorials = true;
        public AutoSaveInterval AutoSave = AutoSaveInterval.TenMinutes;
        public bool EnableDeveloperConsole = false;
    }

    // ========== GRAPHICS SETTINGS ==========
    [Serializable]
    public class GraphicsSettings
    {
        // Display
        public FullscreenModeType FullscreenMode = FullscreenModeType.Fullscreen;
        public int ResolutionWidth = 1920;
        public int ResolutionHeight = 1080;
        public int RefreshRate = 60;
        public VSyncMode VSync = VSyncMode.On;
        public QualityPreset QualityPreset = QualityPreset.High;

        // Rendering
        public AntiAliasingMode AntiAliasing = AntiAliasingMode.TAA;
        public float RenderScale = 1.0f;
        public float ShadowDistance = 100f;
        public ShadowResolutionQuality ShadowResolution = ShadowResolutionQuality.High;
        public TextureQuality TextureQuality = TextureQuality.Full;
        public AnisotropicMode AnisotropicFiltering = AnisotropicMode.PerTexture;
        public TerrainQuality TerrainQuality = TerrainQuality.High;
        public float VegetationDensity = 1.0f;
        public float GrassDrawDistance = 100f;

        // Post-Processing
        public bool Bloom = true;
        public bool MotionBlur = false;
        public float MotionBlurIntensity = 0.5f;
        public bool AmbientOcclusion = true;
        public ColorGradingMode ColorGrading = ColorGradingMode.Filmic;
        public bool DepthOfField = false;

        // RTS-Specific
        public bool UnitHighlightOutlines = true;
        public bool SelectionCircles = true;
        public bool HealthBars = true;
        public FogOfWarQuality FogOfWarQuality = FogOfWarQuality.High;
    }

    // ========== AUDIO SETTINGS ==========
    [Serializable]
    public class AudioSettings
    {
        // Volume Levels
        public float MasterVolume = 1.0f;
        public float MusicVolume = 0.7f;
        public float SFXVolume = 0.8f;
        public float UIVolume = 0.6f;
        public float VoiceVolume = 0.9f;

        // Sound Options
        public float SpatialBlend = 1.0f; // 0 = 2D, 1 = 3D
        public string AudioDevice = "Default";
        public DynamicRange DynamicRange = DynamicRange.Normal;

        // RTS-Specific
        public bool AlertNotifications = true;
        public UnitVoiceStyle UnitVoices = UnitVoiceStyle.Classic;
        public BattleSFXIntensity BattleSFXIntensity = BattleSFXIntensity.Normal;
    }

    // ========== GAMEPLAY SETTINGS ==========
    [Serializable]
    public class GameplaySettings
    {
        // Camera Options
        public float CameraPanSpeed = 50f;
        public float CameraRotationSpeed = 100f;
        public bool EdgeScrolling = true;
        public float EdgeScrollSensitivity = 0.5f;
        public float ZoomSpeed = 10f;
        public float MinZoom = 10f;
        public float MaxZoom = 100f;
        public bool InvertPanning = false;
        public bool InvertZoom = false;

        // Difficulty
        public AIDifficulty AIDifficulty = AIDifficulty.Normal;
        public GameSpeedMultiplier GameSpeed = GameSpeedMultiplier.Normal;

        // RTS Mechanics
        public bool AutomaticUnitGrouping = true;
        public bool SmartPathfinding = true;
        public bool EnableUnitCollision = true;
        public bool MinimapRotation = false;
        public MinimapIconSize MinimapIconSize = MinimapIconSize.Medium;
        public bool AutoRebuildWorkers = false;
        public bool AutoRepairBuildings = false;
    }

    // ========== CONTROL SETTINGS ==========
    [Serializable]
    public class ControlSettings
    {
        // Mouse
        public float MouseSensitivity = 1.0f;
        public bool MouseSmoothing = false;
        public CameraDragButton CameraDragButton = CameraDragButton.MiddleClick;
        public CommandButtonStyle CommandButton = CommandButtonStyle.RightClick;

        // Unit Control
        public UnitControlStyle UnitControlStyle = UnitControlStyle.ClassicRTS;

        // Note: Keybinds are handled by Unity's Input System
        // This is just for storing custom preferences
        public bool UseCustomKeybinds = false;
    }

    // ========== UI SETTINGS ==========
    [Serializable]
    public class UISettings
    {
        public float UIScale = 1.0f;
        public NameplateMode Nameplates = NameplateMode.OnlySelected;
        public bool DamageNumbers = true;
        public CursorStyle CursorStyle = CursorStyle.Default;
        public bool FlashAlerts = true;
        public ColorblindMode ColorblindMode = ColorblindMode.Off;
    }

    // ========== ACCESSIBILITY SETTINGS ==========
    [Serializable]
    public class AccessibilitySettings
    {
        // Visual
        public bool HighContrastMode = false;
        public ColorblindMode ColorblindFilter = ColorblindMode.Off;
        public bool Subtitles = true;
        public float SubtitleSize = 1.0f;
        public float SubtitleBackgroundOpacity = 0.7f;

        // Gameplay Aid
        public bool ReducedCameraShake = false;
        public bool SimplifiedEffects = false;
    }

    // ========== NETWORK SETTINGS (Placeholder) ==========
    [Serializable]
    public class NetworkSettings
    {
        public NetworkRegion Region = NetworkRegion.Auto;
        public int MaxPing = 150;
        public bool AutoReconnect = true;
        public PacketRate PacketRate = PacketRate.Medium;

        // Voice Chat
        public bool VoiceChat = false;
        public float VoiceChatVolume = 1.0f;
        public bool PushToTalk = true;
        // Note: Push-to-talk keybind handled by Input System
    }

    // ========== SYSTEM SETTINGS ==========
    [Serializable]
    public class SystemSettings
    {
        public bool DiagnosticsLog = false;
        public FPSCounterMode FPSCounter = FPSCounterMode.Off;
        public FPSCapMode FPSCap = FPSCapMode.Cap60;
    }
}
