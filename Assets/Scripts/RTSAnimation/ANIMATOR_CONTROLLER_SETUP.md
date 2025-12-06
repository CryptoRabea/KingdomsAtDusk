# Animator Controller Setup Guide

## Overview

This guide explains how to set up the Animator Controller for the modular unit animation system. The system uses a layered approach with:

1. **Base Layer** - Core locomotion and combat animations
2. **Personality Layer** - Idle variations, victory, and retreat animations
3. **Look/Aim Layer** - Look-at/aiming rig control (optional)

---

## Animator Parameters

### Required Parameters

#### Float Parameters
| Parameter | Type | Description | Default | Used By |
|-----------|------|-------------|---------|---------|
| `Speed` | Float | Movement speed for blend trees | 0.0 | Base Layer |
| `LookWeight` | Float | Look-at rig weight (0-1) | 0.0 | Look Layer |

#### Bool Parameters
| Parameter | Type | Description | Default | Used By |
|-----------|------|-------------|---------|---------|
| `IsMoving` | Bool | Whether unit is moving | false | Base Layer |
| `IsDead` | Bool | Permanent death state | false | Base Layer |
| `Retreat` | Bool | Retreat/fear state | false | Personality Layer |

#### Trigger Parameters
| Parameter | Type | Description | Used By |
|-----------|------|-------------|---------|
| `Attack` | Trigger | Trigger attack animation | Base Layer |
| `Hit` | Trigger | Trigger hit/damage reaction | Base Layer |
| `Death` | Trigger | Trigger death animation | Base Layer |
| `DoIdleAction` | Trigger | Trigger idle variation | Personality Layer |
| `Victory` | Trigger | Trigger victory/celebration | Personality Layer |

#### Int Parameters
| Parameter | Type | Description | Range | Used By |
|-----------|------|-------------|-------|---------|
| `IdleVariant` | Int | Select idle variation (0-3) | 0-3 | Personality Layer |

---

## Layer Structure

### Layer 0: Base Locomotion

**Purpose:** Core movement and combat animations
**Weight:** 1.0 (always active)
**Blend Mode:** Override

#### States

```
┌─────────────────────────────────────────────────────────────┐
│                      Base Layer                              │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Entry ──► Idle ◄──► Walk/Run ◄──► Attack                   │
│              │                        │                      │
│              │                        │                      │
│              ▼                        ▼                      │
│            Hit ◄──────────────────── Death                   │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

#### State Details

**Idle State**
- Animation: `Idle` clip
- Transitions:
  - → Walk when `IsMoving == true`
  - → Attack when `Attack` trigger
  - → Hit when `Hit` trigger
  - → Death when `Death` trigger

**Walk/Run State** (Blend Tree)
- Blend Parameter: `Speed`
- Contains:
  - Idle (Speed: 0)
  - Walk (Speed: 3-5)
  - Run (Speed: 5-10)
- Transitions:
  - → Idle when `IsMoving == false`
  - → Attack when `Attack` trigger
  - → Hit when `Hit` trigger
  - → Death when `Death` trigger

**Attack State**
- Animation: `Attack` clip
- Exit Time: 0.9 (90% complete)
- Transitions:
  - → Idle when animation completes
  - → Death when `Death` trigger

**Hit State**
- Animation: `Hit` clip
- Exit Time: 0.8 (80% complete)
- Transitions:
  - → Previous state when complete
  - → Death when `Death` trigger

**Death State**
- Animation: `Death` clip
- Condition: `IsDead == true`
- No exit (terminal state)

#### Transition Settings

```
All transitions to Death:
- Duration: 0.1s
- Has Exit Time: false
- Condition: Death trigger OR IsDead == true

Normal transitions:
- Duration: 0.1-0.2s
- Has Exit Time: varies
- Interruption Source: Current State Then Next State
```

---

### Layer 1: Personality Override

**Purpose:** Idle variations, victory, and retreat animations
**Weight:** 1.0
**Blend Mode:** Override
**Avatar Mask:** Optional (can mask to upper body only)

#### States

```
┌─────────────────────────────────────────────────────────────┐
│                  Personality Layer                           │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Entry ──► Empty State                                       │
│              │                                               │
│              ├──► IdleVariant0 ◄──┐                          │
│              ├──► IdleVariant1    │                          │
│              ├──► IdleVariant2    │ (DoIdleAction trigger)   │
│              ├──► IdleVariant3 ───┘                          │
│              │                                               │
│              ├──► Victory ◄────── (Victory trigger)          │
│              │                                               │
│              └──► Retreat ◄────── (Retreat bool)             │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

#### State Details

**Empty State** (Default)
- No animation
- Weight: 0.0
- Transitions to all personality states

**IdleVariant States** (0-3)
- Animations: `IdleVariant0` through `IdleVariant3`
- Trigger: `DoIdleAction` + `IdleVariant == [index]`
- Exit Time: 0.95
- Auto-returns to Empty State

**Victory State**
- Animation: `Victory` clip
- Trigger: `Victory`
- Exit Time: 0.95
- Auto-returns to Empty State

**Retreat State**
- Animation: `Retreat` clip (looping)
- Condition: `Retreat == true`
- Exits when `Retreat == false`

#### Transition Settings

```
To Personality States:
- Duration: 0.2s
- Has Exit Time: false
- Interruption: Current State

From Personality States:
- Duration: 0.2s
- Has Exit Time: true (0.95)
- Interruption: None
```

---

### Layer 2: Look/Aim Rig (Optional)

**Purpose:** Animation Rigging control for look-at/aiming
**Weight:** Variable (controlled by `LookWeight` parameter)
**Blend Mode:** Additive

#### Setup

1. Add Unity Animation Rigging package
2. Create Rig Builder on unit root
3. Add Multi-Aim Constraint or Multi-Parent Constraint for look
4. Control rig weight via animator parameter

#### States

```
┌─────────────────────────────────────────────────────────────┐
│                    Look/Aim Layer                            │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Entry ──► Look Rig State                                    │
│              │                                               │
│              └─ Continuously active                          │
│                 Weight controlled by LookWeight parameter    │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

**Look Rig State**
- Empty animation or pose
- Rig weight: Driven by `LookWeight` (0-1)
- Always active, weight determines influence

---

## Creating the Animator Controller

### Step 1: Create Base Controller

1. In Unity, right-click in Project window
2. Select `Create > Animator Controller`
3. Name it `UnitAnimatorController`

### Step 2: Add Parameters

In the Animator window, click "Parameters" tab and add all parameters listed above:

```
+ Speed (Float, default: 0)
+ IsMoving (Bool, default: false)
+ IsDead (Bool, default: false)
+ Attack (Trigger)
+ Hit (Trigger)
+ Death (Trigger)
+ DoIdleAction (Trigger)
+ IdleVariant (Int, default: 0)
+ Victory (Trigger)
+ Retreat (Bool, default: false)
+ LookWeight (Float, default: 0)
```

### Step 3: Build Base Layer

1. Rename "Base Layer" if needed
2. Create states:
   - Right-click → `Create State > Empty` for Idle
   - Assign animation clips to each state
   - Create blend tree for Walk/Run
3. Set up transitions as shown in diagram above

### Step 4: Add Personality Layer

1. Click "Layers" tab → "+"
2. Name: "Personality"
3. Set weight: 1.0
4. Set blending: Override
5. Create Empty State (default)
6. Create IdleVariant0-3 states
7. Create Victory state
8. Create Retreat state
9. Set up transitions

### Step 5: Add Look Layer (Optional)

1. Click "Layers" tab → "+"
2. Name: "LookAim"
3. Set weight: 1.0 (will be controlled by parameter)
4. Set blending: Additive
5. Create empty state for rig control

---

## Animation Clip Requirements

### Minimum Required Clips

| Clip Name | Description | Loop | Length |
|-----------|-------------|------|--------|
| `Idle` | Standing idle pose | Yes | 2-5s |
| `Walk` | Walking animation | Yes | 1-2s |
| `Attack` | Primary attack | No | 0.5-1.5s |
| `Death` | Death animation | No | 1-3s |

### Recommended Additional Clips

| Clip Name | Description | Loop | Length |
|-----------|-------------|------|--------|
| `Run` | Running animation | Yes | 0.8-1.2s |
| `Hit` | Damage reaction | No | 0.3-0.6s |
| `IdleVariant0-3` | Idle actions | No | 2-4s |
| `Victory` | Celebration | No | 2-4s |
| `Retreat` | Fear/retreat | Yes | 1-2s |

---

## Animation Events

### Attack Animation

Add animation event at damage frame:
- Function: `OnAttackHit`
- Time: When weapon hits (usually 40-60% through animation)

Add animation event at end:
- Function: `OnAttackComplete`
- Time: 0.95 normalized

### Walk/Run Animations

Add footstep events:
- Function: `OnFootstep`
- Time: At each foot contact with ground

### Death Animation

Add animation event at end:
- Function: `OnDeathComplete`
- Time: 0.95 normalized

---

## Using AnimatorOverrideController

The modular system uses `AnimatorOverrideController` to swap animations at runtime.

### Setup

1. Create your base `UnitAnimatorController` with placeholder animations
2. Add `UnitAnimatorProfileLoader` component to unit prefab
3. Create animation profiles using `RTS > Animation > Create Example Profiles`
4. Assign profile to the loader
5. Loader automatically creates override controller and swaps clips

### Clip Name Mapping

The loader maps profile clips to controller clips by name:

| Profile Clip | Maps To Controller Clips |
|--------------|--------------------------|
| `idleAnimation` | Idle |
| `walkAnimation` | Walk, Walking |
| `runAnimation` | Run, Running |
| `attackAnimation` | Attack |
| `hitAnimation` | Hit, GetHit, Damaged |
| `deathAnimation` | Death, Die |
| `victoryAnimation` | Victory, Celebrate |
| `retreatAnimation` | Retreat, Fear |
| `idleVariants[n]` | IdleVariant{n}, Idle{n}, IdleAction{n} |

**Important:** Name your animation states in the controller to match these patterns!

---

## Testing Your Setup

### 1. Verify Parameters

Open Animator window and check all parameters exist with correct types.

### 2. Test State Transitions

1. Enter Play Mode
2. Select unit in hierarchy
3. Open Animator window
4. Watch state transitions as unit moves and attacks

### 3. Test Personality System

1. Add `UnitPersonalityController` to unit
2. Wait 5-20 seconds while idle
3. Unit should trigger idle variations automatically

### 4. Test Profile Loader

1. Create test profile: `Assets > Create > RTS > Animation Profile > Archer Profile`
2. Assign animation clips in Inspector
3. Add `UnitAnimatorProfileLoader` to unit
4. Assign profile
5. Enter Play Mode
6. Verify clips are swapped

---

## Common Issues & Solutions

### Issue: Animations don't play

**Solution:**
- Check Animator component is enabled
- Verify animation clips are assigned
- Check layer weights are set correctly
- Ensure parameters are being set (use Animator window to debug)

### Issue: Transitions are jerky

**Solution:**
- Reduce transition duration (0.1-0.2s is good)
- Enable "Has Exit Time" for natural transitions
- Check animation speeds match expected gameplay speeds

### Issue: Personality animations don't play

**Solution:**
- Verify Personality layer weight is 1.0
- Check `UnitPersonalityController` is added and enabled
- Ensure profile has personality animations assigned
- Check `enableIdleActions` is true in Inspector

### Issue: Override controller not working

**Solution:**
- Verify clip names match in controller and profile
- Check `UnitAnimatorProfileLoader` is enabled
- Ensure profile is assigned in Inspector
- Use "Debug: List Override Clips" context menu on loader

### Issue: Death animation won't play

**Solution:**
- Check `IsDead` bool is set to true
- Ensure Death trigger is also set
- Verify transitions to Death state exist from all states
- Check death animation clip is assigned

---

## Performance Tips

1. **Use parameter hashing:** Scripts already use `Animator.StringToHash()` for performance
2. **Minimize layers:** Only use layers you need (Look layer is optional)
3. **Optimize blend trees:** Use 1D blend trees where possible
4. **Cache references:** Components cache animator reference in Awake()
5. **Event-driven updates:** System only updates on state changes, not every frame
6. **Culling:** Consider disabling animators for distant units

---

## Advanced: Avatar Masks

To limit personality animations to upper body:

1. Create Avatar Mask: `Create > Avatar Mask`
2. Name it `UpperBodyMask`
3. Enable only upper body bones (spine, arms, head)
4. Assign mask to Personality layer
5. Now personality animations won't affect legs

---

## Integration Checklist

- [ ] Animator Controller created with all parameters
- [ ] Base layer configured with locomotion states
- [ ] Personality layer added with idle variants
- [ ] Animation clips assigned to all states
- [ ] Animation events added to clips
- [ ] UnitAnimationController component on unit prefab
- [ ] UnitAnimatorProfileLoader component added
- [ ] UnitPersonalityController component added
- [ ] Animation profile created and assigned
- [ ] GroupAnimationManager singleton in scene
- [ ] Tested in Play Mode

---

## Next Steps

1. Create your animation clips or import from asset store
2. Set up the Animator Controller following this guide
3. Create animation profiles for each unit type
4. Test individual units first
5. Add GroupAnimationManager for group behaviors
6. Fine-tune timing and transitions

For more information, see:
- `UnitAnimationProfile.cs` - Profile ScriptableObject
- `UnitAnimatorProfileLoader.cs` - Runtime loading system
- `UnitPersonalityController.cs` - Personality behaviors
- `GroupAnimationManager.cs` - Group coordination
- `ANIMATION_SYSTEM_GUIDE.md` - Existing system documentation
