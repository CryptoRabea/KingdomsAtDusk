namespace RTSGame.Settings
{
    // General Settings Enums
    public enum Language
    {
        English,
        Arabic,
        French,
        German,
        Spanish,
        Chinese,
        Japanese,
        Russian
    }

    public enum ThemeType
    {
        Light,
        Dark,
        HighContrast
    }

    public enum AutoSaveInterval
    {
        Off,
        FiveMinutes,
        TenMinutes,
        TwentyMinutes
    }

    // Graphics Settings Enums
    public enum FullscreenModeType
    {
        Fullscreen,
        Borderless,
        Windowed
    }

    public enum VSyncMode
    {
        Off,
        On,
        OnHalfRate
    }

    public enum QualityPreset
    {
        Low,
        Medium,
        High,
        Ultra,
        Custom
    }

    public enum AntiAliasingMode
    {
        Off,
        SMAA,
        FXAA,
        TAA
    }

    public enum ShadowQuality
    {
        Off,
        Low,
        Medium,
        High,
        VeryHigh
    }

    public enum TextureQuality
    {
        Quarter,
        Half,
        Full
    }

    public enum AnisotropicMode
    {
        Off,
        PerTexture,
        Forced16x
    }

    public enum TerrainQuality
    {
        Low,
        Medium,
        High
    }

    public enum FogOfWarQuality
    {
        Low,
        Medium,
        High
    }

    public enum ColorGradingMode
    {
        Neutral,
        Filmic
    }

    // Audio Settings Enums
    public enum DynamicRange
    {
        NightMode,
        Normal,
        Wide
    }

    public enum BattleSFXIntensity
    {
        Low,
        Normal,
        High
    }

    public enum UnitVoiceStyle
    {
        Classic,
        Modern,
        Off
    }

    // Gameplay Settings Enums
    public enum AIDifficulty
    {
        Easy,
        Normal,
        Hard,
        Brutal
    }

    public enum GameSpeedMultiplier
    {
        Half,      // 0.5x
        Normal,    // 1x
        Fast,      // 1.5x
        VeryFast   // 2x
    }

    public enum MinimapIconSize
    {
        Small,
        Medium,
        Large
    }

    public enum CameraDragButton
    {
        RightClick,
        LeftClick,
        MiddleClick
    }

    public enum CommandButtonStyle
    {
        RightClick,
        LeftClick
    }

    public enum UnitControlStyle
    {
        ClassicRTS,    // StarCraft-like
        ModernRTS,     // Company of Heroes-like
        Custom
    }

    // UI Settings Enums
    public enum NameplateMode
    {
        Off,
        On,
        OnlySelected
    }

    public enum CursorStyle
    {
        Default,
        Modern,
        Classic
    }

    public enum ColorblindMode
    {
        Off,
        Deuteranopia,
        Protanopia,
        Tritanopia
    }

    // System Settings Enums
    public enum FPSCounterMode
    {
        Off,
        Simple,
        Detailed
    }

    public enum FPSCapMode
    {
        Off,
        Cap30,
        Cap60,
        Cap120,
        Unlimited
    }

    // Network Settings Enums (Placeholder)
    public enum NetworkRegion
    {
        Auto,
        NorthAmerica,
        Europe,
        Asia,
        SouthAmerica,
        Oceania
    }

    public enum PacketRate
    {
        Low,
        Medium,
        High
    }
}
