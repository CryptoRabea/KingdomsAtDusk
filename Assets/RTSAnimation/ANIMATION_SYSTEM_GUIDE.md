# RTS Unit Animation System - Setup Guide

## Overview
This modular animation system integrates seamlessly with your existing RTS architecture. It automatically syncs animations with unit states, movement, combat, and health.

## Components

### 1. UnitAnimationController (Core)
- **Purpose**: Main animation controller that syncs with unit behavior
- **Required Components**: Animator
- **Auto-detects**: UnitMovement, UnitCombat, UnitHealth, UnitAIController
- **Key Features**:
  - Automatic state detection (Idle, Walk, Attack, Death)
  - Event-driven updates
  - Performance optimized with parameter hashing
  - Animation event callbacks

### 2. AnimationConfigSO (Data)
- **Purpose**: Designer-friendly configuration for animation settings
- **Location**: Create via `Create > RTS > AnimationConfig`
- **Customizable**:
  - Transition speeds
  - Movement thresholds
  - Attack timing
  - Root motion settings

### 3. UnitAnimationAdvanced (Optional)
- **Purpose**: Advanced features for sophisticated units
- **Features**:
  - Look-at IK (units look at targets)
  - Hand IK (weapon positioning)
  - Animation layers (upper/lower body separation)
  - Smooth weight transitions

### 4. UnitAnimationEvents (Audio & Effects)
- **Purpose**: Handles audio and visual effects triggered by animations
- **Features**:
  - Footstep sounds
  - Attack/hit sounds
  - Particle effects
  - Random pitch variation

---

## Setup Instructions

### Step 1: Prepare Your Animator Controller

Create an Animator Controller with these **required parameters**:

**Float Parameters:**
- `Speed` - Current movement speed (0 = idle, >0 = moving)

**Bool Parameters:**
- `IsMoving` - True when unit is moving
- `IsDead` - True when unit dies

**Trigger Parameters:**
- `Attack` - Triggers attack animation
- `Death` - Triggers death animation
- `Hit` - Triggers hit reaction (optional)
- `Idle` - Triggers idle (optional)

### Step 2: Create Animation States

Your Animator should have these states:

1. **Idle** (default state)
2. **Walk** (transition when IsMoving = true OR Speed > 0.1)
3. **Attack** (transition on Attack trigger)
4. **Death** (transition on Death trigger)

**Example Transitions:**
```
Idle -> Walk: IsMoving = true
Walk -> Idle: IsMoving = false
Any State -> Attack: Attack trigger
Any State -> Death: Death trigger
Attack -> Idle: Exit time or duration complete
```

### Step 3: Add Components to Unit

Add these components to your unit GameObject:

1. **Animator** (Unity built-in)
   - Assign your Animator Controller
   - Set Avatar if using humanoid

2. **UnitAnimationController** (required)
   - Drag Animator reference
   - Set movement threshold (default: 0.1)
   - Configure root motion if needed

3. **UnitAnimationEvents** (optional, for audio/effects)
   - Assign AudioSource
   - Add footstep sounds
   - Add attack/hit sounds
   - Add particle effect prefabs

4. **UnitAnimationAdvanced** (optional, for IK)
   - Enable Look At for units to face targets
   - Enable Hand IK for weapon positioning
   - Configure animation layers if needed

### Step 4: Set Up Animation Events

In your animation clips, add Animation Events at key frames:

**Walk Animation:**
- Add `OnFootstep` event at each footfall frame
  - Handled by: UnitAnimationEvents

**Attack Animation:**
- Add `OnAttackStart` event at frame 0
  - Plays attack sound
- Add `OnAttackHit` event at the damage frame (e.g., frame 15)
  - Handled by: UnitAnimationController (applies damage)
  - Also handled by: UnitAnimationEvents (plays effects)
- Add `OnAttackComplete` event at last frame
  - Notifies attack finished

**Death Animation:**
- Add `OnDeath` event at frame 0
  - Plays death sound
- Add `OnDeathComplete` event at last frame
  - Triggers cleanup

---

## Integration with Existing Systems

### With UnitAIController
The animation system automatically detects AI state changes:
- `IdleState` → Idle animation
- `MovingState` → Walk animation  
- `AttackingState` → Attack animation
- `DeadState` → Death animation

### With UnitCombat
- Attacks automatically trigger attack animation
- Damage timing synced with animation events
- Combat range checked before playing attack

### With UnitMovement
- Movement speed drives walk animation speed
- Smooth blending based on velocity
- Stops animation when destination reached

### With UnitHealth
- Hit reactions on damage
- Death animation on health = 0
- Health UI can react to animation events

---

## Advanced Usage

### Custom Animation States

Add new states to the `AnimationState` enum:
```csharp
public enum AnimationState
{
    Idle,
    Walk,
    Attack,
    Death,
    Hit,
    Victory,      // Add new states here
    Celebrate,
    Custom
}
```

Then implement in `UnitAnimationController`:
```csharp
case AnimationState.Victory:
    animator.SetTrigger("Victory");
    break;
```

### Animation Speed Modifiers

Modify animation speed based on game state:
```csharp
animController.SetAnimationSpeed(1.5f); // 50% faster
```

### Manual Animation Triggers

Trigger animations manually when needed:
```csharp
animController.PlayAttack();
animController.PlayCustomAnimation("Celebrate");
```

### Using Animation Layers

For units with upper/lower body separation:
```csharp
animAdvanced.SetLayerWeight(1, 0.7f); // 70% blend
animAdvanced.PlayOnLayer("Reload", 1); // Play on layer 1
```

---

## Performance Optimization

The system is optimized for RTS games with many units:

1. **Parameter Hashing**: Uses `Animator.StringToHash()` for performance
2. **Event-Driven**: Only updates when state changes
3. **Threshold-Based**: Ignores micro-movements below threshold
4. **Smart Transitions**: Uses `SetFloat` with dampening for smooth blends

---

## Common Issues & Solutions

### Issue: Animations not playing
**Solution**: 
- Check Animator Controller is assigned
- Verify parameter names match exactly (case-sensitive)
- Ensure transitions are set up correctly

### Issue: Attack animation too fast/slow
**Solution**:
- Adjust `attackSpeedMultiplier` in AnimationConfigSO
- Or modify animation clip speed in Unity Animation window

### Issue: Walk animation jittery
**Solution**:
- Increase `movementThreshold` in UnitAnimationController
- Reduce `animationTransitionSpeed` for smoother blending
- Check NavMeshAgent acceleration settings

### Issue: Unit walks in T-pose
**Solution**:
- Assign Avatar to Animator (for humanoid)
- Check animation clips are correctly imported
- Verify Animator Controller has valid states

### Issue: IK not working
**Solution**:
- Enable IK Pass on animation layer in Animator
- Ensure Avatar is set to Humanoid
- Check `OnAnimatorIK` is being called

---

## Example Unit Setup Checklist

- [ ] Animator component with controller assigned
- [ ] UnitAnimationController component added
- [ ] Movement threshold configured (0.1 recommended)
- [ ] Animation events added to clips:
  - [ ] OnFootstep in walk animation
  - [ ] OnAttackHit in attack animation
  - [ ] OnDeath in death animation
- [ ] UnitAnimationEvents added (if using audio)
- [ ] Audio clips assigned:
  - [ ] Footsteps
  - [ ] Attack sounds
  - [ ] Hit sounds
  - [ ] Death sound
- [ ] Particle effects assigned (optional)
- [ ] Test in play mode!

---

## Event Flow Example

**When unit attacks:**
1. `UnitCombat.TryAttack()` called
2. `DamageDealtEvent` published
3. `UnitAnimationController` receives event
4. Attack trigger set in Animator
5. Attack animation plays
6. `OnAttackHit()` animation event fires
7. Damage applied to target
8. Attack sound plays (via UnitAnimationEvents)
9. Animation completes, returns to idle/walk

---

## Best Practices

1. **Keep animations short**: RTS units need responsive controls
2. **Use blend trees**: For smooth walk/run transitions
3. **Normalize attack timing**: Make attack rate match animation length
4. **Test with many units**: Ensure performance is good at scale
5. **Use LOD**: Consider simpler animations for distant units

---

## Next Steps

1. Create your Animator Controller with required parameters
2. Add animation clips (Idle, Walk, Attack, Death)
3. Set up transitions between states
4. Add UnitAnimationController to your unit prefab
5. Test and iterate on timing and blending
6. Add audio and effects with UnitAnimationEvents
7. (Optional) Add IK with UnitAnimationAdvanced

---

## Support

For issues or questions, check:
- Unity Animator documentation
- Animation event callback documentation  
- Your unit's existing components (UnitMovement, UnitCombat, etc.)
