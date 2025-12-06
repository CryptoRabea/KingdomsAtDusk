# Modular Animation System - Quick Start Guide

## Overview

This modular animation system extends your existing `UnitAnimationController` with:

✅ **ScriptableObject-driven animation profiles** - Easily swap animations without changing code
✅ **Personality system** - Idle variations, victory celebrations, retreat animations
✅ **Group behaviors** - Synchronized group celebrations and scanning
✅ **Look-at/aim support** - Animation Rigging integration for aiming
✅ **Event-driven** - Integrates seamlessly with your EventBus system
✅ **Performance-optimized** - No heavy Update loops, minimal overhead

---

## Component Architecture

```
Unit GameObject
├── Animator (required)
├── UnitAnimationController (existing - handles core animations)
├── UnitAnimatorProfileLoader (NEW - loads animation profiles)
├── UnitPersonalityController (NEW - handles personality behaviors)
├── UnitMovement (existing)
├── UnitCombat (existing)
├── UnitHealth (existing)
└── UnitAIController (existing)

Scene
└── GroupAnimationManager (NEW - singleton, manages group behaviors)
```

---

## Quick Setup (5 Steps)

### Step 1: Create Animation Profiles

**Option A: Use the Editor Tool**
```
1. Menu: RTS > Animation > Create Example Profiles
2. Click "Create Archer Profile" (or Knight/Mage)
3. Profile asset is created and selected
```

**Option B: Manual Creation**
```
1. Right-click in Project
2. Create > RTS > Animation Profile > Archer Profile
3. Profile asset is created
```

**Option C: Assets Menu**
```
1. In Project window, right-click
2. Assets > Create > RTS > Animation Profile > [Type]
```

### Step 2: Configure Profile

Select the profile and assign animation clips in Inspector:

```
Locomotion Animations
├── Idle Animation: [Assign Idle clip]
├── Walk Animation: [Assign Walk clip]
└── Run Animation: [Assign Run clip]

Combat Animations
├── Attack Animation: [Assign Attack clip]
├── Hit Animation: [Assign Hit clip]
└── Death Animation: [Assign Death clip]

Personality Animations
├── Idle Variants [0-3]: [Assign idle action clips]
├── Victory Animation: [Assign victory clip]
└── Retreat Animation: [Assign retreat clip]

Settings
├── Min Idle Time: 5s
├── Max Idle Time: 15s
├── Look Weight: 0.5
└── Animation Speed Multiplier: 1.0
```

### Step 3: Add Components to Unit Prefab

```csharp
// Existing components (you already have these)
✓ Animator
✓ UnitAnimationController
✓ UnitMovement
✓ UnitCombat
✓ UnitHealth

// ADD these new components:
+ UnitAnimatorProfileLoader
+ UnitPersonalityController
```

In Unity:
1. Select unit prefab
2. Add Component > RTS > Animation > Unit Animator Profile Loader
3. Assign your animation profile to the "Animation Profile" field
4. Add Component > RTS > Animation > Unit Personality Controller
5. (Optional) Configure personality settings

### Step 4: Add GroupAnimationManager to Scene

```
1. Create empty GameObject in scene
2. Name it "GroupAnimationManager"
3. Add Component > RTS > Animation > Group Animation Manager
4. Configure settings (defaults are good)
5. Enable "Auto Register Units" (recommended)
```

### Step 5: Update Animator Controller

See `ANIMATOR_CONTROLLER_SETUP.md` for detailed instructions.

**Required Parameters:**
- Add `DoIdleAction` (Trigger)
- Add `IdleVariant` (Int, 0-3)
- Add `Victory` (Trigger)
- Add `Retreat` (Bool)
- Add `LookWeight` (Float)

**Required Layers:**
- Base Layer (you already have this)
- Personality Layer (add states for IdleVariant0-3, Victory, Retreat)
- Look Layer (optional)

---

## Usage Examples

### Example 1: Basic Setup

```csharp
// Your existing unit works as before
// The new system works alongside it automatically

// UnitAnimationController handles:
// - Movement animations (idle, walk, run)
// - Combat animations (attack, hit, death)
// - Event-driven updates

// UnitPersonalityController adds:
// - Random idle actions every 5-15 seconds
// - Victory animations on wave complete
// - Retreat animations when fleeing

// No code changes needed! It's automatic.
```

### Example 2: Trigger Victory Manually

```csharp
using RTS.Units.Animation;

public class MyGameManager : MonoBehaviour
{
    void OnBattleWon()
    {
        // Trigger victory for all units
        GroupAnimationManager.Instance.TriggerGlobalVictory();
    }

    void OnObjectiveComplete(Vector3 position)
    {
        // Trigger victory for nearby units only
        GroupAnimationManager.Instance.TriggerGroupVictory(position);
    }
}
```

### Example 3: Control Individual Unit Personality

```csharp
using RTS.Units.Animation;

public class MyUnitController : MonoBehaviour
{
    private UnitPersonalityController personality;

    void Start()
    {
        personality = GetComponent<UnitPersonalityController>();
    }

    public void CelebrateVictory()
    {
        personality?.TriggerVictory();
    }

    public void StartRetreating()
    {
        personality?.TriggerRetreat(true);
    }

    public void StopRetreating()
    {
        personality?.TriggerRetreat(false);
    }

    public void DoRandomIdleAction()
    {
        personality?.ForceIdleAction();
    }

    public void LookAtEnemy(Transform enemy)
    {
        personality?.SetLookAtTarget(enemy, weight: 0.8f);
    }
}
```

### Example 4: Group Scanning Behavior

```csharp
using RTS.Units.Animation;

public class AlertSystem : MonoBehaviour
{
    void OnEnemySpotted(Vector3 enemyPosition)
    {
        // Make nearby units look around alertly
        GroupAnimationManager.Instance.TriggerGroupScan(enemyPosition, radius: 20f);
    }
}
```

### Example 5: Runtime Profile Swapping

```csharp
using RTS.Units.Animation;

public class UnitUpgradeSystem : MonoBehaviour
{
    [SerializeField] private UnitAnimationProfile veteranProfile;

    public void UpgradeToVeteran(GameObject unit)
    {
        var loader = unit.GetComponent<UnitAnimatorProfileLoader>();
        if (loader != null)
        {
            // Swap to veteran animations at runtime
            loader.SwapProfile(veteranProfile);
        }
    }
}
```

### Example 6: Custom Animation Event

```csharp
using RTS.Core.Events;

// Create a custom event
public struct UnitPromotedEvent
{
    public GameObject Unit;
}

// Trigger it when unit is promoted
void OnUnitPromoted(GameObject unit)
{
    EventBus.Publish(new UnitPromotedEvent { Unit = unit });
}

// Units can subscribe to it in UnitPersonalityController
// to automatically celebrate when promoted
```

---

## EventBus Integration

The system automatically integrates with your existing EventBus:

### Events Subscribed

**UnitPersonalityController listens to:**
- `UnitStateChangedEvent` → Triggers retreat animation when fleeing
- `UnitHealthChangedEvent` → Could trigger fear at low health
- `WaveCompletedEvent` → Triggers victory celebration (30% chance)

**GroupAnimationManager listens to:**
- `WaveCompletedEvent` → Triggers global victory
- `UnitSpawnedEvent` → Auto-registers new units
- `UnitDiedEvent` → Unregisters dead units

### Custom Events

You can easily add new events:

```csharp
// 1. Define event in GameEvents.cs
public struct UnitLevelUpEvent
{
    public GameObject Unit;
    public int NewLevel;
}

// 2. Subscribe in UnitPersonalityController
private void SubscribeToEvents()
{
    // ... existing subscriptions
    EventBus.Subscribe<UnitLevelUpEvent>(OnLevelUp);
}

private void OnLevelUp(UnitLevelUpEvent evt)
{
    if (evt.Unit == gameObject)
    {
        TriggerVictory(); // Celebrate level up!
    }
}

// 3. Publish when unit levels up
EventBus.Publish(new UnitLevelUpEvent
{
    Unit = unitObject,
    NewLevel = 5
});
```

---

## Profile Configuration Tips

### Archer Profile
```
- Fast, alert personality
- Frequent idle actions (8-20s)
- High look weight (0.7) for aiming
- Fast animation speed (1.1x)
- Enable look-at rig
```

### Knight Profile
```
- Stoic, patient personality
- Infrequent idle actions (10-25s)
- Low look weight (0.4) - focused forward
- Slower animation speed (0.9x)
- Disable look-at rig
```

### Mage Profile
```
- Mystical, fidgety personality
- Very frequent idle actions (6-15s)
- High idle action probability (0.8)
- Medium look weight (0.6)
- Fast attack animations (1.5x)
- Enable look-at rig
```

---

## Performance Characteristics

### Memory Footprint
- **Per Unit:** ~200 bytes additional (2 component references)
- **Profile Asset:** ~2-4 KB (ScriptableObject)
- **Override Controller:** Created once per profile, shared if possible

### CPU Usage
- **UnitPersonalityController:** Only updates idle timer when idle
- **GroupAnimationManager:** Scans every 10s (configurable)
- **No continuous Update loops** on movement/combat
- **Event-driven:** Only executes on state changes

### Optimization Tips
```
1. Share profiles between units of same type
2. Use avatar masks to reduce blend complexity
3. Disable personality for background/distant units
4. Reduce idle action frequency for large armies
5. Use animator culling for off-screen units
```

---

## Compatibility

✅ **Unity 2022.3+** - Fully supported
✅ **Unity 6** - Fully supported
✅ **New Input System** - Required (already in your project)
✅ **Animation Rigging** - Optional (for look-at feature)
✅ **Existing UnitAnimationController** - Fully compatible
✅ **EventBus system** - Integrated

---

## Troubleshooting

### Personality animations don't play

**Check:**
1. Is `UnitPersonalityController` enabled?
2. Is the unit actually idle? (not moving, not attacking)
3. Has enough time passed? (wait minIdleTime seconds)
4. Are idle variant animations assigned in profile?
5. Is personality layer weight set to 1.0 in Animator?

**Debug:**
```csharp
// Enable debug logging
var personality = GetComponent<UnitPersonalityController>();
// Check if component exists and is enabled
Debug.Log($"Personality enabled: {personality != null && personality.enabled}");
```

### Animations not swapping with profile

**Check:**
1. Is `UnitAnimatorProfileLoader` enabled?
2. Is profile assigned in Inspector?
3. Are animation clips assigned in profile?
4. Do clip names in Animator Controller match expected names?

**Debug:**
```csharp
// Use context menu in Unity
// Right-click on UnitAnimatorProfileLoader component
// Select "Debug: List Override Clips"
// Check console for clip mapping
```

### Group behaviors not working

**Check:**
1. Is `GroupAnimationManager` in the scene?
2. Is "Auto Register Units" enabled?
3. Are units within the effect radius?
4. Is the feature enabled? (enableGroupVictory, enableGroupScanning)

**Debug:**
```csharp
// Check registered units
int count = GroupAnimationManager.Instance.GetRegisteredUnitCount();
Debug.Log($"Registered units: {count}");
```

### Look-at not working

**Check:**
1. Is Animation Rigging package installed?
2. Is Rig Builder component on unit?
3. Is look-at rig assigned in UnitPersonalityController?
4. Is enableLookAt true in profile?
5. Is LookWeight > 0?

---

## Best Practices

### 1. Profile Organization
```
Assets/
  Data/
    AnimationProfiles/
      Infantry/
        ├── ArcherProfile.asset
        ├── SpearmanProfile.asset
        └── SwordmanProfile.asset
      Cavalry/
        └── KnightProfile.asset
      Magic/
        └── MageProfile.asset
```

### 2. Component Order
```
Unit Prefab
├── Animator (first)
├── UnitAnimationController (second)
├── UnitAnimatorProfileLoader (third)
├── UnitPersonalityController (fourth)
└── ... other components
```

### 3. Layer Naming
```
Animator Controller
├── Layer 0: "Base" (locomotion & combat)
├── Layer 1: "Personality" (idle variants, victory)
└── Layer 2: "LookAim" (optional, rig control)
```

### 4. Event Handling
```csharp
// Always check if the event is for this unit
private void OnSomeEvent(SomeEvent evt)
{
    if (evt.Unit != gameObject) return; // ← Important!

    // Handle event
}
```

### 5. Null Checks
```csharp
// Components are optional, always null-check
personalityController?.TriggerVictory();
profileLoader?.SwapProfile(newProfile);
```

---

## Migration from Old System

If you have existing units with `UnitAnimationController`:

1. **Nothing breaks** - Old system still works
2. **Add new components** - Add `UnitAnimatorProfileLoader` and `UnitPersonalityController`
3. **Create profiles** - Move animation clip assignments to profiles
4. **Optional** - Remove hardcoded clip references from prefabs
5. **Test** - Verify both systems work together

---

## API Reference

### UnitAnimationProfile (ScriptableObject)

```csharp
// Methods
AnimationClip GetRandomAttackAnimation()
AnimationClip GetRandomIdleVariant()
AnimationClip GetRandomSpecialIdleAction()
AnimationClip GetIdleVariant(int index)
void ValidateProfile()
```

### UnitAnimatorProfileLoader (Component)

```csharp
// Properties
UnitAnimationProfile Profile { get; }
AnimatorOverrideController OverrideController { get; }

// Methods
void LoadProfile(UnitAnimationProfile profile)
void SwapProfile(UnitAnimationProfile newProfile)
void RestoreOriginalController()
AnimationClip GetClipByName(string clipName)
```

### UnitPersonalityController (Component)

```csharp
// Methods
void TriggerVictory()
void TriggerRetreat(bool retreating)
void SetLookAtTarget(Transform target, float weight = 1f)
void ClearLookAtTarget()
void SetLookWeight(float weight)
void ForceIdleAction(int variantIndex = -1)
void SetPersonalityEnabled(bool enabled)

// Group callbacks (called by GroupAnimationManager)
void OnGroupVictory()
void OnGroupScan()
```

### GroupAnimationManager (Singleton)

```csharp
// Properties
static GroupAnimationManager Instance { get; }

// Methods
void RegisterUnit(UnitPersonalityController unit)
void UnregisterUnit(UnitPersonalityController unit)
void TriggerGroupVictory(Vector3 center)
void TriggerGlobalVictory()
void TriggerGroupScan(Vector3 center, float radius = -1f)
int GetRegisteredUnitCount()
IReadOnlyCollection<UnitPersonalityController> GetRegisteredUnits()
void ClearAllUnits()
```

### UnitAnimationController (Extended)

```csharp
// New methods
bool HasPersonalityController()
bool HasProfileLoader()
UnitPersonalityController GetPersonalityController()
UnitAnimatorProfileLoader GetProfileLoader()
```

---

## What's Next?

1. **Create your animation clips** or buy from Asset Store
2. **Set up Animator Controller** following ANIMATOR_CONTROLLER_SETUP.md
3. **Create profiles** for each unit type
4. **Add components** to your unit prefabs
5. **Test and iterate** on timing and probabilities
6. **Expand** with custom events and behaviors

---

## Support & Documentation

- **Setup Guide:** `ANIMATOR_CONTROLLER_SETUP.md` - Detailed Animator setup
- **System Guide:** `ANIMATION_SYSTEM_GUIDE.md` - Original system docs
- **Quick Reference:** `QUICK_REFERENCE.md` - Parameter reference
- **Source Code:** All scripts are heavily commented

For issues or questions, check the inline documentation in the source files.

---

## Credits

Built on top of the existing RTS animation system. Compatible with all existing functionality while adding modular, data-driven features for maximum flexibility.
