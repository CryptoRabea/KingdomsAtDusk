# Animation System - Quick Reference

## Required Animator Parameters

```
Float:   Speed
Bool:    IsMoving, IsDead
Trigger: Attack, Death, Hit (optional)
```

## Setup Checklist (5 minutes)

1. ✅ Add `Animator` component with controller assigned
2. ✅ Add `UnitAnimationController` component
3. ✅ Add animation events to clips (OnAttackHit, OnFootstep, etc.)
4. ✅ (Optional) Add `UnitAnimationEvents` for audio
5. ✅ Test in play mode!

## Quick Setup via Menu

```
Tools > RTS > Setup Unit Animation
Tools > RTS > Validate Animator Parameters
```

## Animation Events to Add

**In Walk Animation:**
- `OnFootstep` at each foot contact frame

**In Attack Animation:**
- `OnAttackStart` at frame 0
- `OnAttackHit` at damage frame (when weapon connects)
- `OnAttackComplete` at last frame

**In Death Animation:**
- `OnDeath` at frame 0
- `OnDeathComplete` at last frame

## Component Hierarchy

```
Unit GameObject
├── Animator (Unity built-in)
├── UnitAnimationController (REQUIRED - core system)
├── UnitAnimationEvents (optional - for audio/effects)
└── UnitAnimationAdvanced (optional - for IK/layers)
```

## Automatic Behavior

| Your Code | Animation Result |
|-----------|-----------------|
| `movement.SetDestination()` | → Walk animation |
| `combat.TryAttack()` | → Attack animation |
| `health.TakeDamage()` | → Hit reaction |
| `health reaches 0` | → Death animation |
| `movement.Stop()` | → Idle animation |

**You don't need to call animation methods manually!**

## Manual Control (if needed)

```csharp
var anim = GetComponent<UnitAnimationController>();

anim.PlayAttack();                      // Trigger attack
anim.PlayCustomAnimation("Victory");    // Play custom
anim.SetAnimationSpeed(1.5f);          // Speed up 50%
anim.IsPlayingState("Attack");         // Check state
```

## Common Parameters

| Parameter | Type | Purpose |
|-----------|------|---------|
| Speed | Float | Movement speed (drives walk) |
| IsMoving | Bool | True when moving |
| Attack | Trigger | Triggers attack anim |
| Death | Trigger | Triggers death anim |
| IsDead | Bool | True after death |
| Hit | Trigger | Hit reaction (optional) |

## Audio Setup

```csharp
// On UnitAnimationEvents component:
footstepSounds[]  → Array of clips (plays random)
attackSounds[]    → Array of clips (plays random)
hitSounds[]       → Array of clips (plays random)
deathSound        → Single clip
```

## IK Setup (Optional)

```csharp
var advanced = GetComponent<UnitAnimationAdvanced>();

// Look at target
advanced.EnableLookAt(true);
advanced.SetLookAtTarget(enemy);

// Hand IK for weapons
advanced.EnableHandIK(true);
advanced.SetHandIKTarget(weaponGrip, isRightHand: true);
```

## Animator State Machine Example

```
[Any State] → Death (Death trigger, can't exit)
[Any State] → Attack (Attack trigger)

Idle → Walk (IsMoving = true)
Walk → Idle (IsMoving = false)
Attack → Idle (exit time 1.0)
```

## Performance Tips

- Use `Animator.StringToHash()` for parameters (done automatically)
- Set movement threshold > 0.1 to ignore micro-movements
- Disable animation on distant units if needed
- Use animation LOD for 100+ units

## Debugging

**In Editor:**
- Select unit → Inspector shows current state
- Button to manually test attack
- Tools > RTS > Validate Animator Parameters

**In Code:**
```csharp
Debug.Log(anim.CurrentState);           // Current state
Debug.Log(anim.GetCurrentAnimationTime()); // 0-1 progress
```

## Integration Points

| System | How It Connects |
|--------|-----------------|
| UnitMovement | Speed & IsMoving parameters |
| UnitCombat | DamageDealtEvent → Attack trigger |
| UnitHealth | UnitDiedEvent → Death trigger |
| UnitAIController | StateChangedEvent → Animation sync |

## File Locations

```
/Scripts/Units/Animation/
├── UnitAnimationController.cs    (Core - always needed)
├── AnimationConfigSO.cs          (Data - create via menu)
├── UnitAnimationEvents.cs        (Audio - optional)
├── UnitAnimationAdvanced.cs      (IK - optional)
└── AnimationSetupHelper.cs       (Editor - place in Editor folder)
```

## Common Issues

**Animations not playing?**
→ Check Animator Controller is assigned
→ Validate parameters (Tools > RTS > Validate)

**Attack too fast?**
→ Adjust attack rate to match animation length
→ Or adjust animation speed in clip

**Walk animation jittery?**
→ Increase movement threshold (0.1 → 0.3)
→ Reduce transition speed in inspector

**IK not working?**
→ Enable IK Pass in Animator layer settings
→ Ensure Avatar is Humanoid

## Quick Test

```csharp
// In play mode, run this to test:
var anim = GetComponent<UnitAnimationController>();
anim.PlayAttack(); // Should see attack animation
```

## Next Steps After Setup

1. Test all 4 animations (idle, walk, attack, death)
2. Adjust timing and transitions
3. Add audio clips to UnitAnimationEvents
4. Add particle effects for attacks/hits
5. (Optional) Set up IK for targeting
6. Optimize for your unit count

---

**Need Help?** See ANIMATION_SYSTEM_GUIDE.md for detailed documentation.
