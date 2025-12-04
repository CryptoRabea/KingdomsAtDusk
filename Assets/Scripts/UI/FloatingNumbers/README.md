# Floating Numbers System

A comprehensive, modular floating numbers and HP bar system for **Kingdoms at Dusk** RTS game.

## Features

### Core Features
- ✨ **Animated Floating Numbers** - Smooth, eye-catching number animations
- ❤️ **HP Bars** - Dynamic health bars above units and buildings
- 💥 **Damage Numbers** - Red numbers when units/buildings take damage
- 💚 **Healing Numbers** - Green numbers when units are healed
- 🪵 **Resource Numbers** - Gold numbers for resource generation
- 🔧 **Repair Numbers** - Blue numbers when buildings are repaired
- 🎮 **Fully Configurable** - Every feature can be toggled on/off
- ⚡ **Performance Optimized** - Object pooling and efficient rendering

### Future-Ready Features (Placeholders)
- 🌟 **Experience Numbers** - Ready for XP system integration
- 📦 **Resource Pickups** - Ready for pickup system
- 🎉 **Level Up Notifications** - Ready for leveling system

## Installation

### Automated Setup (Recommended)

1. Open the setup tool: `Tools > RTS > Setup > Floating Numbers System`
2. Click **"Complete Setup (All Steps)"**
3. The tool will:
   - Create settings asset
   - Add manager to scene
   - Create settings panel prefab
   - Show integration instructions

### Manual Setup

If you prefer manual setup:

1. **Create Settings Asset**
   ```
   Assets > Create > KAD > UI > Floating Numbers Settings
   ```

2. **Add Manager to Scene**
   - Create GameObject named "FloatingNumbersManager"
   - Add `FloatingNumbersManager` component
   - Assign settings asset
   - Place under GameManager in hierarchy

3. **Register Service**
   - Already integrated in `GameManager.InitializeServices()`
   - Service automatically found and registered

## Configuration

### Settings Asset

Located at: `Assets/Settings/FloatingNumbersSettings.asset`

#### Feature Toggles
- **Show HP Bars** - Display health bars above entities
- **Show Damage Numbers** - Display damage numbers
- **Show Heal Numbers** - Display healing numbers
- **Show Resource Gathering** - Display resource collection
- **Show Building Resources** - Display building generation
- **Show Repair Numbers** - Display repair amounts

#### HP Bar Options
- **Only When Selected** - Show bars only for selected units
- **Only When Damaged** - Hide bars at full health
- **Width** - Bar width in world units
- **Height** - Bar height in world units
- **Offset** - Height above entity

#### Animation Settings
- **Number Duration** - How long numbers stay visible (1.5s default)
- **Float Height** - How high numbers float (2.0 units default)
- **Font Size** - Text size (24 default)
- **Scale Curve** - Animation curve for scaling
- **Fade Curve** - Animation curve for fading

#### Colors
- **Damage Color** - Red (customizable)
- **Heal Color** - Green (customizable)
- **Resource Color** - Gold (customizable)
- **Repair Color** - Blue (customizable)
- **Critical Color** - Orange (for future crits)

#### Performance
- **Max Active Numbers** - Limit concurrent numbers (50 default)
- **Pool Size** - Pre-instantiated objects (100 default)
- **HP Bar Update Interval** - Update every N frames (3 default)

## Usage

### In-Game Settings

Add the settings panel to your pause/game menu:

1. Find prefab: `Assets/Prefabs/UI/FloatingNumbers/FloatingNumbersSettingsPanel.prefab`
2. Add to your menu canvas
3. Players can toggle features in real-time

### Programmatic Access

```csharp
using RTS.Core.Services;

// Get service
var floatingNumbers = ServiceLocator.Get<IFloatingNumberService>();

// Show damage number
floatingNumbers.ShowDamageNumber(worldPosition, 50f, isCritical: false);

// Show healing number
floatingNumbers.ShowHealNumber(worldPosition, 25f);

// Show resource gain
floatingNumbers.ShowResourceNumber(worldPosition, ResourceType.Gold, 10);

// Show repair
floatingNumbers.ShowRepairNumber(worldPosition, 100f);

// Register HP bar for custom entity
floatingNumbers.RegisterHPBar(
    gameObject,
    () => currentHealth,
    () => maxHealth
);

// Unregister when destroyed
floatingNumbers.UnregisterHPBar(gameObject);
```

### Automatic Integration

The system automatically integrates with:

- **UnitHealth.cs** - HP bars and damage/heal numbers
- **BuildingHealth.cs** - HP bars and damage/repair numbers
- **ResourcesGeneratedEvent** - Building resource generation
- **DamageDealtEvent** - Combat damage
- **HealingAppliedEvent** - Healer units

No additional code needed for basic features!

## Architecture

### Service Pattern

Implements `IFloatingNumberService` interface and registers with `ServiceLocator`:

```csharp
public interface IFloatingNumberService
{
    void ShowDamageNumber(Vector3 worldPosition, float damageAmount, bool isCritical);
    void ShowHealNumber(Vector3 worldPosition, float healAmount);
    void ShowResourceNumber(Vector3 worldPosition, ResourceType resourceType, int amount);
    void ShowRepairNumber(Vector3 worldPosition, float repairAmount);
    void RegisterHPBar(GameObject target, Func<float> getCurrentHealth, Func<float> getMaxHealth);
    void UnregisterHPBar(GameObject target);
    FloatingNumbersSettings Settings { get; }
    void RefreshSettings();
}
```

### Object Pooling

All floating numbers and HP bars are pooled for performance:

- **Floating Numbers Pool**: 100 objects (configurable)
- **HP Bars Pool**: 50 objects (configurable)
- Automatic return to pool after animation
- Dynamic expansion if pool exhausted

### Event-Driven

Subscribes to game events via `EventBus`:

- `DamageDealtEvent` → Shows damage numbers
- `HealingAppliedEvent` → Shows heal numbers
- `ResourcesGeneratedEvent` → Shows resource numbers
- `BuildingDamagedEvent` → Shows damage/repair numbers

## File Structure

```
Assets/Scripts/UI/FloatingNumbers/
├── FloatingNumbersSettings.cs           [ScriptableObject configuration]
├── FloatingNumbersManager.cs            [Main service implementation]
├── FloatingNumber.cs                    [Animated number component]
├── HPBar.cs                             [Health bar component]
├── FloatingNumbersSettingsPanel.cs      [In-game settings UI]
└── README.md                            [This file]

Assets/Scripts/Editor/
└── FloatingNumbersSetupTool.cs         [Automated setup tool]

Assets/Scripts/Core/
└── IServices.cs                         [IFloatingNumberService interface]

Assets/Scripts/Managers/
└── GameManager.cs                       [Service registration]

Assets/Scripts/Units/Components/
└── UnitHealth.cs                        [HP bar registration]

Assets/Scripts/RTSBuildingsSystems/
└── BuildingHealth.cs                    [HP bar registration]

Assets/Settings/
└── FloatingNumbersSettings.asset        [Default settings]

Assets/Prefabs/UI/FloatingNumbers/
└── FloatingNumbersSettingsPanel.prefab  [Settings UI prefab]
```

## Extending the System

### Adding New Number Types

1. **Add method to interface** (`IServices.cs`):
```csharp
void ShowExperienceNumber(Vector3 worldPosition, int xpAmount);
```

2. **Implement in manager** (`FloatingNumbersManager.cs`):
```csharp
public void ShowExperienceNumber(Vector3 worldPosition, int xpAmount)
{
    if (!settings.ShowExperienceNumbers) return;
    Vector2 screenPos = WorldToCanvasPosition(worldPosition);
    ShowNumber($"+{xpAmount} XP", screenPos, Color.cyan);
}
```

3. **Add toggle to settings** (`FloatingNumbersSettings.cs`):
```csharp
[SerializeField] private bool showExperienceNumbers = false;
public bool ShowExperienceNumbers => showExperienceNumbers;
public void SetShowExperienceNumbers(bool value) => showExperienceNumbers = value;
```

4. **Add to UI panel** (`FloatingNumbersSettingsPanel.cs`):
```csharp
[SerializeField] private Toggle showExperienceNumbersToggle;
// Wire up in OnEnable and button handlers
```

### Adding Custom Events

```csharp
// Subscribe to custom event in FloatingNumbersManager
EventBus.Subscribe<YourCustomEvent>(OnYourCustomEvent);

private void OnYourCustomEvent(YourCustomEvent evt)
{
    // Show appropriate number
    ShowNumber("Your Text", screenPosition, yourColor);
}
```

### Custom HP Bar Styles

Modify `HPBar.cs` or create derived classes:

```csharp
public class BossHPBar : HPBar
{
    // Custom appearance for boss units
    // Larger size, different colors, special effects
}
```

## Performance Considerations

### Optimization Features

1. **Object Pooling** - Reuses objects instead of instantiating
2. **Update Throttling** - HP bars update every 3 frames
3. **Conditional Rendering** - Hide bars at full health (optional)
4. **Canvas Layering** - Separate canvases for world/screen space
5. **Event-Driven** - No polling, only updates on events

### Performance Settings

Adjust these in settings asset for your target platform:

- **Pool Size**: Higher = less dynamic allocation
- **Max Active Numbers**: Lower = better performance
- **HP Bar Update Interval**: Higher = better performance
- **Only When Damaged**: Reduces visible bars

## Troubleshooting

### Numbers Not Showing

1. Check `FloatingNumbersManager` is in scene
2. Verify service is registered in `GameManager`
3. Check feature toggles in settings asset
4. Ensure camera is tagged "MainCamera"

### HP Bars Not Appearing

1. Check `ShowHPBars` is enabled in settings
2. Verify entities have `UnitHealth` or `BuildingHealth`
3. Check `OnlyWhenDamaged` setting
4. Damage entity to make bar visible

### Performance Issues

1. Reduce `MaxActiveNumbers` in settings
2. Increase `HPBarUpdateInterval`
3. Enable `OnlyWhenDamaged` to hide bars
4. Reduce `PoolSize` if memory constrained

### Numbers in Wrong Position

1. Verify main camera is set
2. Check canvas render mode
3. Adjust `HPBarOffset` in settings
4. Ensure proper world-to-screen conversion

## Credits

**System Design**: Modular, extensible architecture following RTS game patterns
**Integration**: Event-driven design with ServiceLocator pattern
**Performance**: Object pooling and efficient rendering
**Extensibility**: Ready for future features (XP, pickups, levels)

## Version History

### v1.0.0 (2025-12-01)
- Initial release
- HP bars for units and buildings
- Damage, healing, and resource numbers
- Repair numbers for buildings
- Fully modular and configurable
- In-game settings panel
- Automated setup tool
- Future-ready placeholders

## Support

For issues or questions:
1. Check this README
2. Review code comments
3. Use the setup tool for clean installation
4. All features are optional - disable what you don't need!

---

**Built for Kingdoms at Dusk RTS Game**
