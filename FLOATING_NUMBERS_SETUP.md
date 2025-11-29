# Floating Numbers & Health Bars System

## Overview

This system provides automatic damage numbers, healing numbers, and health bars for units and buildings in Kingdoms at Dusk. The system is designed to be easy to use with minimal setup required.

## Features

### 🎯 Floating Numbers
- **Damage Numbers**: Red floating numbers when units/buildings take damage
- **Healing Numbers**: Green floating numbers with "+" prefix when healed
- **Critical Hits**: Gold "CRIT!" numbers with configurable chance and multiplier
- **Smooth Animations**: Floating upward with fade-out effect
- **Object Pooling**: Efficient reuse of floating number instances

### ❤️ Health Bars
- **World-Space UI**: Health bars that follow units and buildings
- **Color Gradient**: Automatically changes color based on health (red → yellow → green)
- **Auto-Hide**: Can hide when at full health
- **Smooth Animations**: Smooth fill amount transitions
- **Optional Text**: Can display actual health values (e.g., "450/500")

### 🏗️ Building Health System
- **BuildingHealth Component**: Similar to UnitHealth but for buildings
- **Damage Visuals**: Can swap to damaged material when health is low
- **Destruction Effects**: Optional destruction effect prefab
- **Event-Driven**: Publishes events for damage, healing, and destruction

### ⚔️ Building Combat
- **Attack Buildings**: Units can now attack and destroy buildings
- **Updated UnitCombat**: Supports both units and buildings as targets
- **Cursor Feedback**: Attack cursor appears when hovering over enemy buildings

## Quick Setup

### Option 1: Auto Setup Tool (Recommended)

1. Open Unity Editor
2. Go to `Tools > RTS > Setup Floating Numbers & Health Bars`
3. Configure options:
   - Toggle "Setup Floating Numbers" (damage/heal numbers)
   - Toggle "Setup Health Bars" (health bar UI)
   - Assign Health Bar Prefab (if you have one)
   - Choose "Setup on Units" and/or "Setup on Buildings"
4. Click "Auto Setup All"

Done! All units and buildings in your scene now have floating numbers and health bars.

### Option 2: Manual Setup

#### For Individual GameObjects:

1. **Add Health Component** (if not already present):
   - For buildings: Add `BuildingHealth` component
   - For units: Should already have `UnitHealth`

2. **Add Floating Numbers** (optional):
   - Add `FloatingNumbersAutoSetup` component
   - Configure settings (show damage/heal, critical hits, etc.)

3. **Add Health Bar** (optional):
   - Add `HealthBarAutoSetup` component
   - Assign a health bar prefab or leave null to use default from Resources

#### Context Menu Setup:

Right-click on `FloatingNumbersAutoSetup` or `HealthBarAutoSetup` components and use:
- "Auto Setup on All Units"
- "Auto Setup on All Buildings"

## System Components

### Core Scripts

#### Floating Numbers
- `FloatingNumber.cs` - Individual floating number behavior
- `FloatingNumbersManager.cs` - Singleton manager with object pooling
- `FloatingNumbersAutoSetup.cs` - Auto-attaches to health components

#### Health Bars
- `HealthBarUI.cs` - World-space health bar UI
- `HealthBarAutoSetup.cs` - Auto-creates health bars

#### Building Health
- `BuildingHealth.cs` - Health component for buildings
- Supports damage, healing, death, and events
- Visual feedback with damaged materials

### Events

New building events added to `GameEvents.cs`:
- `BuildingHealthChangedEvent`
- `BuildingDamageDealtEvent`
- `BuildingHealingAppliedEvent`

## Setup FloatingNumbersManager

1. Create an empty GameObject in your scene
2. Name it "FloatingNumbersManager"
3. Add the `FloatingNumbersManager` component
4. Create a floating number prefab:

### Creating Floating Number Prefab

1. Create a new Canvas with:
   - Render Mode: World Space
   - Scale: (0.01, 0.01, 0.01)
2. Add a TextMeshPro text as a child
3. Add the `FloatingNumber` component to the canvas
4. Add a `CanvasGroup` component
5. Assign the TextMeshPro and CanvasGroup references
6. Save as prefab
7. Assign to FloatingNumbersManager

## Configuration

### FloatingNumbersAutoSetup

```csharp
[SerializeField] private bool showDamageNumbers = true;
[SerializeField] private bool showHealNumbers = true;
[SerializeField] private Vector3 spawnOffset = Vector3.up * 2f;
[SerializeField] private bool enableCriticalHits = false;
[SerializeField] private float criticalChance = 0.15f;
[SerializeField] private float criticalMultiplier = 1.5f;
```

### HealthBarUI

```csharp
[SerializeField] private bool showHealthText = false;
[SerializeField] private Vector3 offset = new Vector3(0, 2.5f, 0);
[SerializeField] private bool hideWhenFull = true;
[SerializeField] private bool alwaysFaceCamera = true;
[SerializeField] private Gradient healthGradient; // Color based on health %
[SerializeField] private float smoothSpeed = 10f;
[SerializeField] private bool animateChanges = true;
```

### BuildingHealth

```csharp
[SerializeField] private float maxHealth = 500f;
[SerializeField] private bool isInvulnerable = false;
[SerializeField] private GameObject destructionEffectPrefab;
[SerializeField] private float destructionDelay = 0.5f;
[SerializeField] private Material damagedMaterial;
[SerializeField] private float damageThreshold = 0.5f; // Switch at 50% health
```

## Usage Examples

### Spawning Custom Floating Numbers

```csharp
// Get the manager instance
var manager = FloatingNumbersManager.Instance;

// Spawn damage number
manager.SpawnDamage(50f, enemyPosition);

// Spawn heal number
manager.SpawnHeal(25f, allyPosition);

// Spawn critical hit
manager.SpawnCritical(75f, enemyPosition);

// Spawn custom number
manager.SpawnNumber("BLOCKED!", Color.blue, position);
```

### Building Health API

```csharp
BuildingHealth health = building.GetComponent<BuildingHealth>();

// Take damage
health.TakeDamage(100f, attackerGameObject);

// Heal
health.Heal(50f, healerGameObject);

// Set health directly
health.SetHealth(250f);

// Kill instantly
health.Kill();

// Make invulnerable
health.SetInvulnerable(true);

// Check status
float currentHP = health.CurrentHealth;
float maxHP = health.MaxHealth;
float percent = health.HealthPercentage;
bool dead = health.IsDead;
```

### Subscribe to Events

```csharp
// Component events
health.OnHealthChanged += (current, max) => {
    Debug.Log($"Health: {current}/{max}");
};

health.OnDamageDealt += (attacker, target, amount) => {
    Debug.Log($"{attacker.name} dealt {amount} damage!");
};

health.OnBuildingDestroyed += () => {
    Debug.Log("Building destroyed!");
};

// EventBus events
EventBus.Subscribe<BuildingHealthChangedEvent>(OnBuildingHealthChanged);
EventBus.Subscribe<BuildingDamageDealtEvent>(OnBuildingDamaged);
```

## Cursor System Updates

The `CursorStateManager` now shows an attack cursor when hovering over enemy buildings with units selected:

1. Ensure your buildings are on the correct layer (set in CursorStateManager)
2. Buildings must have the `BuildingHealth` component
3. Buildings must be tagged as enemies (Enemy layer)
4. Selected units must have the `UnitCombat` component

## Layer Setup

Make sure you have these layers configured:
- **Ground**: For terrain/ground objects
- **Unit**: For all units
- **Building**: For all buildings
- **Enemy**: For enemy units and buildings

Set the layers in `CursorStateManager`:
- groundLayer: Ground
- unitLayer: Unit + Enemy
- buildingLayer: Building + Enemy

## Troubleshooting

### Floating numbers don't appear
1. Check that `FloatingNumbersManager` exists in the scene
2. Verify the floating number prefab is assigned
3. Ensure `FloatingNumbersAutoSetup` is attached to your units/buildings
4. Check that the `UnitHealth` or `BuildingHealth` component exists

### Health bars don't show
1. Verify `HealthBarAutoSetup` is attached
2. Check that a health bar prefab is assigned (or exists in Resources)
3. Ensure the prefab has the `HealthBarUI` component
4. Check the offset settings - bar might be spawning off-screen

### Units can't attack buildings
1. Verify buildings have `BuildingHealth` component
2. Check that buildings are on the correct layer (set in `UnitCombat.targetLayers`)
3. Ensure buildings are marked as enemies (Enemy layer)
4. Check unit's attack range is sufficient

### Cursor doesn't change over buildings
1. Verify `buildingLayer` is set in `CursorStateManager`
2. Check that buildings have `BuildingHealth` component
3. Ensure buildings are on the Enemy layer
4. Verify units are selected and have `UnitCombat` component

## Performance Notes

- The floating numbers system uses object pooling for efficiency
- Initial pool size is 20 (configurable in FloatingNumbersManager)
- Pool automatically grows if more numbers are needed
- Health bars use MaterialPropertyBlock to avoid material instances

## Future Enhancements

Possible additions:
- Different number styles for different damage types
- Sound effects on damage/heal
- Screen shake on critical hits
- Damage number size based on damage amount
- Combo counters
- Shield/armor indicators on health bars

## Contact

For issues or questions, please refer to the main project documentation or create an issue in the repository.
