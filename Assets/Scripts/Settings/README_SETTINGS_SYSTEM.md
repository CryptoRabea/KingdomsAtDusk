# RTS Settings System - Complete Documentation

## Overview

This comprehensive settings system provides a full-featured configuration interface for your RTS game, covering all major aspects including graphics, audio, gameplay, controls, UI, accessibility, network, and system settings.

## Architecture

The settings system follows the existing **Service Locator pattern** used throughout the codebase, ensuring consistency and easy integration with other game systems.

### Core Components

1. **Data Layer** (`GameSettings.cs`, `SettingsEnums.cs`)
   - Serializable data structures
   - Enum definitions for all setting types
   - JSON-based persistence

2. **Service Layer** (`ISettingsService`, `IAudioService`)
   - Interface definitions
   - Service registration with ServiceLocator
   - Event-driven architecture

3. **Manager Layer** (`RTSSettingsManager`, `AudioManager`)
   - Settings application logic
   - Audio system integration
   - Graphics settings management

4. **UI Layer** (`SettingsPanel.cs`)
   - Tabbed interface
   - Real-time preview
   - Input validation

## File Structure

```
Assets/Scripts/
├── Settings/
│   ├── SettingsEnums.cs              # All enum definitions
│   ├── GameSettings.cs               # Settings data structures
│   └── README_SETTINGS_SYSTEM.md     # This file
│
├── Managers/
│   ├── RTSSettingsManager.cs         # Main settings manager
│   ├── AudioManager.cs               # Audio system manager
│   └── GameManager.cs                # Updated with new services
│
├── UI/Settings/
│   └── SettingsPanel.cs              # Settings UI controller
│
└── Core/
    └── IServices.cs                  # Updated with new interfaces
```

## Settings Categories

### 1. General Settings
- **Language**: Multi-language support (English, Arabic, French, etc.)
- **UI Scale**: Adjustable UI scaling (0.8 - 1.4)
- **Theme**: Light / Dark / High-Contrast modes
- **Tooltips**: Enable/disable tooltips
- **Show Tutorials**: Tutorial system toggle
- **Auto-Save**: Configurable auto-save intervals (Off, 5min, 10min, 20min)
- **Developer Console**: Enable developer tools

### 2. Graphics Settings

#### Display
- **Fullscreen Mode**: Fullscreen / Borderless / Windowed
- **Resolution**: Dynamic list from available screen resolutions
- **Refresh Rate**: Monitor refresh rate selection
- **VSync**: Off / On / On (Half Rate)
- **Quality Preset**: Low / Medium / High / Ultra / Custom

#### Rendering
- **Anti-Aliasing**: Off / SMAA / FXAA / TAA (URP support)
- **Render Scale**: 0.5 - 1.4 (resolution scaling)
- **Shadow Distance**: 0 - 150 units
- **Shadow Resolution**: Off / Low / Medium / High / Very High
- **Texture Quality**: Quarter / Half / Full resolution
- **Anisotropic Filtering**: Off / Per-Texture / Forced 16x
- **Terrain Quality**: Low / Medium / High
- **Vegetation Density**: 0 - 100%
- **Grass Draw Distance**: Adjustable range

#### Post-Processing
- **Bloom**: On/Off toggle
- **Motion Blur**: On/Off + Intensity slider
- **Ambient Occlusion**: On/Off
- **Color Grading**: Neutral / Filmic
- **Depth of Field**: On/Off

#### RTS-Specific Graphics
- **Unit Highlight Outlines**: Selection outlines
- **Selection Circles**: Ground selection indicators
- **Health Bars**: Unit HP bars toggle
- **Fog of War Quality**: Low / Medium / High

### 3. Audio Settings

#### Volume Control
- **Master Volume**: Global volume control
- **Music Volume**: Background music
- **SFX Volume**: Sound effects
- **UI Volume**: UI interaction sounds
- **Voice Volume**: Unit voices and alerts

#### Sound Options
- **3D Spatial Blend**: 2D - 3D audio mixing
- **Audio Device**: System audio device selection (placeholder)
- **Dynamic Range**: Night Mode / Normal / Wide

#### RTS-Specific Audio
- **Alert Notifications**: Toggle alert sounds
- **Unit Voices**: Classic / Modern / Off
- **Battle SFX Intensity**: Low / Normal / High

### 4. Gameplay Settings

#### Camera Options
- **Camera Pan Speed**: Mouse pan sensitivity
- **Camera Rotation Speed**: Rotation speed
- **Edge Scrolling**: Enable/disable edge scrolling
- **Edge Scroll Sensitivity**: Edge detection range
- **Zoom Speed**: Mouse wheel zoom speed
- **Zoom Limits**: Min/Max camera height
- **Invert Panning**: Invert pan controls
- **Invert Zoom**: Invert zoom direction

#### Difficulty
- **AI Difficulty**: Easy / Normal / Hard / Brutal
- **Game Speed Multiplier**: 0.5x / 1x / 1.5x / 2x

#### RTS Mechanics
- **Automatic Unit Grouping**: Auto-formation
- **Smart Pathfinding**: Advanced pathfinding
- **Enable Unit Collision**: Unit collision physics
- **Minimap Rotation**: Rotate with camera
- **Minimap Icon Size**: Small / Medium / Large
- **Auto-Rebuild Workers**: Automatic worker replacement
- **Auto-Repair Buildings**: Automatic building repairs

### 5. Control Settings

#### Mouse
- **Mouse Sensitivity**: Adjustable sensitivity
- **Mouse Smoothing**: Smooth mouse movement
- **Camera Drag Button**: Right/Left/Middle click
- **Command Button Style**: Right/Left click for commands

#### Keyboard
- **Full Keybind List**: Placeholder for Input System integration
- **Reset to Default Keybinds**: Reset all keybinds
- **Unit Control Style**: Classic RTS / Modern RTS / Custom

### 6. UI Settings
- **UI Scale**: Duplicate control for consistency
- **Nameplates**: Off / On / Only Selected
- **Damage Numbers**: Show floating damage
- **Cursor Style**: Default / Modern / Classic (placeholder)
- **Flash Alerts**: Screen flash for alerts
- **Colorblind Mode**: Off / Deuteranopia / Protanopia / Tritanopia

### 7. Accessibility Settings

#### Visual
- **High-Contrast Mode**: Enhanced contrast
- **Colorblind Filters**: Multiple colorblind support modes
- **Subtitles**: Enable/disable subtitles
- **Subtitle Size**: Text size adjustment
- **Subtitle Background Opacity**: Background transparency

#### Gameplay Aid
- **Reduced Camera Shake**: Minimize camera shake effects
- **Simplified Effects**: Reduced particle effects

### 8. Network Settings (Placeholder)
- **Region Selector**: Geographic region selection
- **Max Ping Filter**: Ping limit for matchmaking
- **Auto-Reconnect**: Automatic reconnection
- **Packet Rate**: Low / Medium / High
- **Voice Chat**: Enable/disable voice chat
- **Voice Chat Volume**: Voice level control
- **Push-To-Talk**: PTT toggle + keybind

### 9. System Settings
- **Diagnostics Log**: Enable debug logging
- **FPS Counter**: Off / Simple / Detailed
- **FPS Cap**: Off / 30 / 60 / 120 / Unlimited
- **Clear Cache**: Clear game cache
- **Reset All Settings**: Reset to defaults
- **Open Save Folder**: Open persistent data folder

## Integration Guide

### Step 1: Verify Service Registration

The `GameManager.cs` has been updated to automatically register the new services:

```csharp
// Services are initialized in this order:
1. Core services (ObjectPool)
2. Game state service
3. Resources & Happiness
4. Population, Reputation, Workforce
5. Building management
6. Audio system (NEW)
7. Settings system (NEW)
8. Save/Load system
9. UI systems (Floating numbers)
```

### Step 2: Accessing Settings Service

From any script in your game:

```csharp
using RTS.Core.Services;
using RTSGame.Settings;

// Get the settings service
var settingsService = ServiceLocator.Get<ISettingsService>();

// Access current settings
float cameraPanSpeed = settingsService.Gameplay.CameraPanSpeed;
bool healthBarsEnabled = settingsService.Graphics.HealthBars;

// Listen to settings changes
settingsService.OnSettingsChanged += OnSettingsChanged;

private void OnSettingsChanged()
{
    // Refresh your system when settings change
    UpdateMySystem();
}
```

### Step 3: Accessing Audio Service

```csharp
using RTS.Core.Services;

// Get the audio service
var audioService = ServiceLocator.Get<IAudioService>();

// Control audio
audioService.MasterVolume = 0.8f;
audioService.PlayMusic("BattleTheme");
audioService.PlaySFX("UnitSelect", unitPosition);
```

### Step 4: Unity Scene Setup

1. **Add SettingsPanel to UI Canvas**:
   - Create a new Canvas GameObject (if not exists)
   - Add a Panel as a child
   - Add the `SettingsPanel` component
   - Create tab panels as children:
     - GeneralTab
     - GraphicsTab
     - AudioTab
     - GameplayTab
     - ControlsTab
     - UITab
     - AccessibilityTab
     - NetworkTab
     - SystemTab

2. **Assign UI References**:
   - Tab Panels: Drag each tab GameObject
   - Tab Buttons: Create buttons for each tab
   - Settings Controls: Add UI elements (Sliders, Toggles, Dropdowns, etc.)
   - Action Buttons: Apply, Reset, Close buttons

3. **Link to Main Menu**:
   - Open `MainMenuManager` in Inspector
   - Assign `SettingsPanel` reference to the new field
   - The system will automatically integrate

4. **Link to In-Game Menu**:
   - Open `SaveLoadMenu` in Inspector
   - Assign `SettingsPanel` reference
   - Add a Settings button to your in-game menu UI
   - Link button to the Settings field

### Step 5: Customization

#### Adding Custom Settings

1. **Add to Enums** (`SettingsEnums.cs`):
```csharp
public enum MyCustomSetting
{
    Option1,
    Option2,
    Option3
}
```

2. **Add to Settings Class** (`GameSettings.cs`):
```csharp
[Serializable]
public class GameplaySettings
{
    // ... existing settings ...
    public MyCustomSetting CustomSetting = MyCustomSetting.Option1;
}
```

3. **Add UI Controls** (`SettingsPanel.cs`):
```csharp
[Header("Custom Settings")]
[SerializeField] private TMP_Dropdown customSettingDropdown;
```

4. **Implement Application Logic** (`RTSSettingsManager.cs`):
```csharp
private void ApplyCustomSetting()
{
    // Your custom logic here
    switch (Gameplay.CustomSetting)
    {
        case MyCustomSetting.Option1:
            // Apply option 1
            break;
        // ...
    }
}
```

## Quality Presets

The system includes predefined quality presets that configure multiple settings at once:

### Low Preset
- Shadow Distance: 50
- Shadow Quality: Low
- Texture Quality: Half
- Anti-Aliasing: Off
- Render Scale: 0.75
- Vegetation Density: 50%
- Post-Processing: Minimal

### Medium Preset
- Shadow Distance: 75
- Shadow Quality: Medium
- Texture Quality: Half
- Anti-Aliasing: FXAA
- Render Scale: 1.0
- Vegetation Density: 70%
- Post-Processing: Selective

### High Preset (Default)
- Shadow Distance: 100
- Shadow Quality: High
- Texture Quality: Full
- Anti-Aliasing: TAA
- Render Scale: 1.0
- Vegetation Density: 100%
- Post-Processing: Enhanced

### Ultra Preset
- Shadow Distance: 150
- Shadow Quality: Very High
- Texture Quality: Full
- Anti-Aliasing: TAA
- Render Scale: 1.2
- Vegetation Density: 100%
- Post-Processing: Full

## Persistence

Settings are automatically saved to:
```
Application.persistentDataPath/game_settings.json
```

### Manual Save/Load

```csharp
var settingsService = ServiceLocator.Get<ISettingsService>();

// Save current settings
settingsService.SaveSettings();

// Load saved settings
settingsService.LoadSettings();

// Reset to defaults
settingsService.ResetToDefaults();

// Apply all settings
settingsService.ApplySettings();
```

## Events

The settings system provides events for responding to changes:

```csharp
// Subscribe to all settings changes
settingsService.OnSettingsChanged += () => {
    Debug.Log("Settings changed!");
};

// Subscribe to quality preset changes
settingsService.OnQualityPresetChanged += (preset) => {
    Debug.Log($"Quality preset changed to: {preset}");
};
```

## Placeholders & Future Integration

Some features are marked as placeholders and require additional implementation:

1. **Keybind Rebinding**: Requires Unity Input System rebinding UI
2. **Audio Device Selection**: Requires platform-specific audio APIs
3. **Colorblind Filters**: Requires shader-based color adjustments
4. **Cursor Styles**: Requires custom cursor graphics
5. **Network Settings**: Requires multiplayer networking integration
6. **Post-Processing Effects**: Requires URP Post-Processing Volume setup

To implement these:
- Search for "Placeholder" comments in the code
- Refer to Unity documentation for specific features
- Extend the existing architecture

## Performance Considerations

1. **Settings Application**: Some settings (like resolution) may cause frame drops when applied
2. **Auto-Save**: Consider implementing auto-save as a coroutine to avoid hitches
3. **UI Updates**: Sliders update text in real-time; consider throttling if needed
4. **Quality Presets**: Changing presets applies multiple settings at once

## Troubleshooting

### Settings Not Saving
- Check `Application.persistentDataPath` is writable
- Verify no exceptions in the console during save
- Check JSON file format is valid

### Graphics Settings Not Applying
- Ensure `UniversalRenderPipelineAsset` is assigned in `RTSSettingsManager`
- Check Unity Quality Settings aren't overriding your values
- Verify Platform-specific settings in Project Settings

### Audio Not Working
- Confirm AudioManager is registered in GameManager
- Check AudioSource components are created properly
- Verify audio clips are assigned (optional feature)

### UI References Missing
- All UI fields in SettingsPanel are optional
- Check console for warnings about missing references
- Assign references in Inspector as needed

## Example Usage Scenarios

### Scenario 1: Apply Camera Settings from RTS Camera Controller

```csharp
using RTS.Core.Services;

public class RTSCameraController : MonoBehaviour
{
    private ISettingsService settingsService;

    private void Start()
    {
        settingsService = ServiceLocator.Get<ISettingsService>();
        ApplyCameraSettings();

        // Listen for settings changes
        settingsService.OnSettingsChanged += ApplyCameraSettings;
    }

    private void ApplyCameraSettings()
    {
        var gameplay = settingsService.Gameplay;

        panSpeed = gameplay.CameraPanSpeed;
        rotationSpeed = gameplay.CameraRotationSpeed;
        zoomSpeed = gameplay.ZoomSpeed;
        minZoom = gameplay.MinZoom;
        maxZoom = gameplay.MaxZoom;
        edgeScrolling = gameplay.EdgeScrolling;
        invertPan = gameplay.InvertPanning;
        invertZoom = gameplay.InvertZoom;
    }

    private void OnDestroy()
    {
        if (settingsService != null)
            settingsService.OnSettingsChanged -= ApplyCameraSettings;
    }
}
```

### Scenario 2: Integrate with Floating Numbers System

The system already integrates with the FloatingNumbersManager:

```csharp
// In RTSSettingsManager.ApplyRTSGraphicsSettings()
var floatingNumberService = ServiceLocator.Instance?.Get<IFloatingNumberService>();
if (floatingNumberService != null)
{
    floatingNumberService.Settings.enableHPBars = Graphics.HealthBars;
    floatingNumberService.Settings.enableDamageNumbers = UI?.DamageNumbers ?? true;
    floatingNumberService.RefreshSettings();
}
```

### Scenario 3: Custom Quality Detection

```csharp
using RTS.Core.Services;
using RTSGame.Settings;

public class GraphicsQualityDetector : MonoBehaviour
{
    private void Start()
    {
        var settingsService = ServiceLocator.Get<ISettingsService>();

        // Auto-detect appropriate quality based on hardware
        QualityPreset recommendedPreset = DetectOptimalQuality();
        settingsService.ApplyQualityPreset(recommendedPreset);
    }

    private QualityPreset DetectOptimalQuality()
    {
        // Check system specs
        int vram = SystemInfo.graphicsMemorySize;
        int processorCount = SystemInfo.processorCount;

        if (vram >= 8000 && processorCount >= 8)
            return QualityPreset.Ultra;
        else if (vram >= 4000 && processorCount >= 4)
            return QualityPreset.High;
        else if (vram >= 2000)
            return QualityPreset.Medium;
        else
            return QualityPreset.Low;
    }
}
```

## Additional Notes

- All settings are preserved across game sessions
- Settings panel can be opened from both Main Menu and In-Game menu
- The system is designed to be extensible - add your own setting categories easily
- Uses TextMeshPro for all text rendering
- Compatible with Unity's New Input System
- Follows RTS game conventions and best practices

## Support

For issues or questions:
1. Check console for error messages
2. Verify all service references in GameManager
3. Ensure UI references are assigned in Inspector
4. Review the existing code patterns in similar systems

## Version History

- **v1.0** (2025-12-05): Initial comprehensive settings system
  - Full settings implementation
  - Service integration
  - UI framework
  - Documentation

---

**Last Updated**: December 5, 2025
**System Version**: 1.0
**Unity Version**: 2022.3+ (URP)
