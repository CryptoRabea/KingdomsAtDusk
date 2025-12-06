# 🏹 Archer Animation System - Complete Setup Guide

**High-Performance Directional Animation System for 100+ Animations**

This guide will walk you through setting up a realistic, responsive, and performant archer animation system that handles complex attack sequences and directional movement.

---

## 📋 Table of Contents

1. [System Overview](#system-overview)
2. [Animation File Organization](#animation-file-organization)
3. [Animator Controller Setup](#animator-controller-setup)
4. [Component Setup](#component-setup)
5. [Performance Optimization](#performance-optimization)
6. [Testing and Debugging](#testing-and-debugging)

---

## 🎯 System Overview

### Key Features

✅ **Directional Movement** - 8-way blended movement (walk left, right, forward, backward, diagonals)
✅ **Combat Sequences** - Draw → Aim → Release state machine
✅ **Performance Optimized** - LOD system, culling, and smart updates for 100+ units
✅ **IK Aiming** - Upper body aims at targets while moving
✅ **Layer System** - Separate upper/lower body animations
✅ **Reactive AI** - Immediate response to state changes

### Components Created

- `ArcherAnimationController.cs` - Main animation brain
- `ArcherAimIK.cs` - IK system for realistic aiming
- `ArcherAnimationConfig.cs` - Scriptable Object configuration

---

## 📁 Animation File Organization

### Recommended Folder Structure

```
Assets/
└── Animations/
    └── Archer/
        ├── Idle/
        │   ├── Archer_Idle_Standing.anim
        │   ├── Archer_Idle_Combat.anim
        │   └── Archer_Idle_Variations.anim
        │
        ├── Movement/
        │   ├── Archer_Walk_Forward.anim
        │   ├── Archer_Walk_Backward.anim
        │   ├── Archer_Walk_Left.anim
        │   ├── Archer_Walk_Right.anim
        │   ├── Archer_Walk_ForwardLeft.anim
        │   ├── Archer_Walk_ForwardRight.anim
        │   ├── Archer_Walk_BackwardLeft.anim
        │   └── Archer_Walk_BackwardRight.anim
        │
        ├── Combat/
        │   ├── Standing/
        │   │   ├── Archer_Standing_Draw.anim
        │   │   ├── Archer_Standing_Aim.anim
        │   │   ├── Archer_Standing_Release.anim
        │   │   └── Archer_Standing_Idle_Aimed.anim
        │   │
        │   ├── Moving/
        │   │   ├── Archer_Walk_Draw_Forward.anim
        │   │   ├── Archer_Walk_Aim_Forward.anim
        │   │   ├── Archer_Walk_Release_Forward.anim
        │   │   └── ... (other directions)
        │   │
        │   └── Transitions/
        │       ├── Archer_Draw_To_Aim.anim
        │       └── Archer_Aim_To_Release.anim
        │
        ├── Reactions/
        │   ├── Archer_Hit_Front.anim
        │   ├── Archer_Hit_Left.anim
        │   ├── Archer_Hit_Right.anim
        │   └── Archer_Hit_Back.anim
        │
        └── Death/
            ├── Archer_Death_Forward.anim
            ├── Archer_Death_Backward.anim
            └── Archer_Death_Left.anim
```

### Animation Naming Convention

Use this pattern: `Archer_[State]_[Direction]_[Variation].anim`

**Examples:**
- `Archer_Walk_Forward.anim`
- `Archer_Standing_Draw.anim`
- `Archer_Walk_Aim_ForwardLeft.anim`
- `Archer_Death_Backward.anim`

---

## 🎬 Animator Controller Setup

### Step 1: Create the Animator Controller

1. **Right-click** in Project → `Create` → `Animator Controller`
2. **Name it**: `ArcherAnimatorController`
3. **Double-click** to open Animator window

---

### Step 2: Create Parameters

Click **Parameters** tab and add these:

| Parameter Name | Type    | Default | Description |
|---------------|---------|---------|-------------|
| `DirectionX`  | Float   | 0       | Horizontal direction (-1 left, 1 right) |
| `DirectionY`  | Float   | 0       | Vertical direction (-1 back, 1 forward) |
| `Speed`       | Float   | 0       | Movement speed |
| `IsMoving`    | Bool    | false   | Is unit moving |
| `CombatState` | Int     | 0       | 0=Idle, 1=Draw, 2=Aim, 3=Release |
| `Draw`        | Trigger | -       | Trigger draw animation |
| `Aim`         | Trigger | -       | Trigger aim animation |
| `Release`     | Trigger | -       | Trigger release animation |
| `Death`       | Trigger | -       | Trigger death |
| `Hit`         | Trigger | -       | Trigger hit reaction |
| `LODLevel`    | Int     | 0       | Performance LOD level |

---

### Step 3: Create Animation Layers

Create **2 layers**:

#### **Layer 0: Base Layer** (Full Body Movement)
- Weight: 1.0
- IK Pass: No
- Purpose: Handles locomotion and full-body animations

#### **Layer 1: Upper Body** (Combat/Aiming)
- Weight: 1.0
- IK Pass: Yes
- Blending: Override
- Avatar Mask: Create "UpperBodyMask" (spine, arms, head)
- Purpose: Allows aiming while moving

---

### Step 4: Base Layer - Movement Blend Tree

1. **Right-click** in Animator → `Create State` → `From New Blend Tree`
2. **Name it**: `Locomotion`
3. **Double-click** to edit

#### Configure Blend Tree:

**Blend Type**: `2D Freeform Directional`
**Parameters**:
- Horizontal: `DirectionX`
- Vertical: `DirectionY`

**Add Motions** (8-way movement):

| Motion | Pos X | Pos Y | Animation |
|--------|-------|-------|-----------|
| Forward | 0 | 1 | Archer_Walk_Forward |
| Backward | 0 | -1 | Archer_Walk_Backward |
| Left | -1 | 0 | Archer_Walk_Left |
| Right | 1 | 0 | Archer_Walk_Right |
| Forward-Left | -0.707 | 0.707 | Archer_Walk_ForwardLeft |
| Forward-Right | 0.707 | 0.707 | Archer_Walk_ForwardRight |
| Backward-Left | -0.707 | -0.707 | Archer_Walk_BackwardLeft |
| Backward-Right | 0.707 | -0.707 | Archer_Walk_BackwardRight |

**Settings**:
- Compute Positions: `Velocity XZ`
- Compute Threshold: Checked

---

### Step 5: Base Layer - Idle State

1. **Create State**: `Idle`
2. **Assign Motion**: `Archer_Idle_Standing`
3. **Set as Default State**

**Transitions**:

**Idle → Locomotion**
- Condition: `IsMoving` = true
- Exit Time: Unchecked
- Transition Duration: 0.15s

**Locomotion → Idle**
- Condition: `IsMoving` = false
- Exit Time: Unchecked
- Transition Duration: 0.25s

---

### Step 6: Base Layer - Death State

1. **Create State**: `Death`
2. **Assign Motion**: `Archer_Death_Forward` (or blend tree for directional death)

**Transition from Any State**:
- Source: `Any State`
- Destination: `Death`
- Condition: `Death` trigger
- Can Transition To Self: No
- Priority: 0 (highest)

---

### Step 7: Upper Body Layer - Combat States

Create these states on **Layer 1**:

#### **States:**

1. **Idle** (default)
   - Motion: Empty or idle upper body

2. **Drawing**
   - Motion: `Archer_Standing_Draw`
   - Length: Match your `drawDuration` setting

3. **Aiming**
   - Motion: `Archer_Standing_Aim`
   - Loop: Yes
   - IK enabled here for look-at

4. **Releasing**
   - Motion: `Archer_Standing_Release`
   - Length: Match your `releaseDuration`

#### **Transitions:**

**Any State → Drawing**
- Condition: `Draw` trigger
- Exit Time: No
- Duration: 0.1s

**Drawing → Aiming**
- Condition: `Aim` trigger
- Exit Time: No
- Duration: 0.05s

**Aiming → Releasing**
- Condition: `Release` trigger
- Exit Time: No
- Duration: 0.05s

**Releasing → Idle**
- Condition: None (automatic after animation completes)
- Exit Time: Yes
- Exit Time Value: 0.95
- Duration: 0.15s

---

### Step 8: Add Animation Events

**Critical for timing!**

#### **In `Archer_Standing_Release` animation:**

1. Open **Animation window**
2. Select the `Archer_Standing_Release` clip
3. Find the **frame where arrow leaves the bow**
4. **Add Event**: `OnArrowRelease`
   - This fires the projectile/damage

#### **In `Archer_Walk_Forward` (and other walk animations):**

1. Add **footstep events**: `OnFootstep`
   - Place on frames where foot touches ground
   - Usually 2 events per walk cycle

#### **Optional Events:**

- `OnDrawComplete` - End of draw animation
- `OnAimComplete` - When aim is stable
- `OnReleaseComplete` - End of release animation

---

## 🔧 Component Setup

### Step 1: Prepare Your Archer Prefab

1. **Import** your archer 3D model
2. **Import** all 100 animation files
3. **Create** or open your archer prefab

---

### Step 2: Add Required Components

Select your archer GameObject and add:

```
Required Components:
├─ Animator (auto-added)
├─ ArcherAnimationController (new!)
├─ ArcherAimIK (new!)
├─ UnitMovement (existing)
├─ UnitCombat (existing)
├─ UnitHealth (existing)
└─ ArcherAI (existing)
```

#### **Add via Inspector:**

1. **Add Component** → `Archer Animation Controller`
2. **Add Component** → `Archer Aim IK`

---

### Step 3: Configure Animator Component

```
Animator Settings:
├─ Controller: ArcherAnimatorController
├─ Avatar: Auto-generated (or custom)
├─ Apply Root Motion: ❌ False
├─ Update Mode: Normal
├─ Culling Mode: Based on Renderers
```

---

### Step 4: Configure ArcherAnimationController

```
Combat Settings:
├─ Draw Duration: 0.5
├─ Aim Duration: 0.3
├─ Release Duration: 0.4
├─ Allow Aim While Moving: ✓

Movement Settings:
├─ Direction Smooth Time: 0.1
├─ Speed Smooth Time: 0.1
├─ Use 8 Way Movement: ✓

Performance Optimization:
├─ Enable LOD: ✓
├─ LOD Distance 1: 30
├─ LOD Distance 2: 60
├─ LOD Distance 3: 100
├─ Cull When Not Visible: ✓

Animation Layering:
├─ Use Upper Body Layer: ✓
├─ Upper Body Layer Index: 1
├─ Upper Body Layer Weight: 1.0
```

---

### Step 5: Configure ArcherAimIK

```
IK Settings:
├─ Enable IK: ✓
├─ Aim Target: (set by code automatically)

IK Weights:
├─ Body Weight: 0.3
├─ Head Weight: 0.8
├─ Eyes Weight: 0.9
├─ Clamp Weight: 0.5

Smoothing:
├─ Smooth Time: 0.2
├─ Only Aim When In Combat: ✓

Constraints:
├─ Max Aim Angle: 80
├─ Min Aim Angle: -40
├─ Max Aim Distance: 50
```

---

### Step 6: Create Avatar Mask for Upper Body

1. **Project Window** → Right-click → `Create` → `Avatar Mask`
2. **Name it**: `UpperBodyMask`
3. **Configure**:
   - ✅ Head
   - ✅ Upper Chest
   - ✅ Chest
   - ✅ Spine
   - ✅ Left Shoulder, Arm, Hand
   - ✅ Right Shoulder, Arm, Hand
   - ❌ Hips, Legs, Feet (uncheck these)

4. **Assign** to Layer 1 in Animator Controller

---

## ⚡ Performance Optimization

### LOD System Explanation

The system automatically adjusts animation quality based on distance:

| Distance | LOD | Behavior |
|----------|-----|----------|
| 0-30m | 0 | Full quality, 60 fps updates, all bones |
| 30-60m | 1 | Good quality, 30 fps updates, important bones |
| 60-100m | 2 | Reduced quality, 15 fps updates, minimal bones |
| 100m+ | 3 | Minimal, 5 fps updates, culling enabled |

### Culling System

- **Not Visible**: Animations pause completely
- **Off-screen**: Updates at reduced rate
- **On-screen**: Normal updates

### Batch Optimization Tips

For **100+ archers**:

1. **Use GPU Instancing** on materials
2. **Combine meshes** when possible
3. **Reduce bone count** for background units
4. **Enable LOD** on all archers
5. **Set `cullWhenNotVisible` = true**

---

## 🧪 Testing and Debugging

### Step 1: Validate Setup

1. Select archer in Hierarchy
2. Menu: **Tools** → **RTS** → **Validate Animator Parameters**
3. Ensure all parameters are ✅ green

---

### Step 2: Test Basic Movement

1. **Press Play**
2. **Select archer** and click to move
3. **Verify**:
   - Idle plays when stationary
   - Walk blends smoothly in all 8 directions
   - Transitions are smooth

---

### Step 3: Test Combat Sequence

1. **Place enemy** near archer
2. **Engage combat**
3. **Verify sequence**:
   - ✅ Draw animation plays
   - ✅ Transitions to Aim
   - ✅ Releases arrow on correct frame
   - ✅ Returns to idle or repeats

---

### Step 4: Test IK Aiming

1. **Enable Gizmos** in Scene view
2. **Engage combat**
3. **Look for**:
   - Yellow line showing aim direction
   - Green line to target when aiming
   - Upper body rotating toward target
   - Lower body still walking normally

---

### Step 5: Performance Testing

```csharp
// Add this to your test scene
for (int i = 0; i < 100; i++)
{
    Vector3 pos = Random.insideUnitSphere * 50f;
    pos.y = 0;
    Instantiate(archerPrefab, pos, Quaternion.identity);
}
```

**Monitor**:
- FPS should stay above 60
- Profiler shows animation optimization working
- Distant archers update less frequently

---

## 🎨 Advanced Customization

### Add More Combat States

Extend `ArcherCombatState` enum:

```csharp
public enum ArcherCombatState
{
    Idle = 0,
    Drawing = 1,
    Aiming = 2,
    Releasing = 3,
    Reloading = 4,  // NEW!
    PowerShot = 5    // NEW!
}
```

### Add Strafing

Modify the blend tree to include strafe animations while aiming.

### Add Combat Variations

Create multiple draw/release animations and randomly select them:

```csharp
animator.SetInteger("AttackVariation", Random.Range(0, 3));
```

---

## 📊 Animation List Reference

### Minimum Required Animations (24)

**Idle (1)**
- Archer_Idle_Standing

**Movement (8)**
- Archer_Walk_Forward
- Archer_Walk_Backward
- Archer_Walk_Left
- Archer_Walk_Right
- Archer_Walk_ForwardLeft
- Archer_Walk_ForwardRight
- Archer_Walk_BackwardLeft
- Archer_Walk_BackwardRight

**Combat Standing (3)**
- Archer_Standing_Draw
- Archer_Standing_Aim
- Archer_Standing_Release

**Reactions (4)**
- Archer_Hit_Front
- Archer_Hit_Left
- Archer_Hit_Right
- Archer_Hit_Back

**Death (1-3)**
- Archer_Death_Forward
- Archer_Death_Backward (optional)
- Archer_Death_Left (optional)

---

### Optional Animations for Enhanced Realism

**Combat While Moving (24)**
- Draw/Aim/Release for each 8 directions

**Idle Variations (5)**
- Archer_Idle_LookAround
- Archer_Idle_Stretch
- Archer_Idle_CheckBow
- Archer_Idle_Adjust_Quiver
- Archer_Idle_Shift_Weight

**Transitions (10)**
- Move_To_Combat
- Combat_To_Move
- Direction change variations

**Special Actions (10)**
- Archer_Reload
- Archer_Power_Draw
- Archer_Rapid_Fire
- etc.

---

## 🔍 Troubleshooting

### Issue: Animations not blending smoothly

**Solution**:
- Check `Direction Smooth Time` and `Speed Smooth Time`
- Increase to 0.2 for smoother (but slower) transitions
- Ensure blend tree positions are correct

---

### Issue: Arrow fires at wrong time

**Solution**:
- Open `Archer_Standing_Release` animation
- Adjust `OnArrowRelease` event timing
- Should be at frame when string is released

---

### Issue: Unit sliding during animations

**Solution**:
- Ensure `Apply Root Motion` is **FALSE**
- Check that NavMesh movement is working
- Velocity should match animation speed

---

### Issue: Performance drops with many archers

**Solution**:
- Enable LOD: `enableLOD = true`
- Enable culling: `cullWhenNotVisible = true`
- Reduce `lodDistance1/2/3` values
- Use GPU instancing on materials

---

### Issue: IK looks unnatural

**Solution**:
- Reduce IK weights (especially body weight)
- Increase `ikSmoothTime` to 0.3-0.5
- Check `maxAimAngle` constraints
- Ensure `UpperBodyMask` is correct

---

### Issue: Combat sequence gets stuck

**Solution**:
- Check transition conditions in Animator
- Ensure triggers are firing: `Debug.Log` in controller
- Verify animation lengths match duration settings
- Check `CombatState` integer is updating

---

## 📝 Best Practices

### ✅ DO:
- Use consistent naming conventions
- Test with 1 archer before spawning 100
- Profile your game with many units
- Use animation events for precise timing
- Keep transitions short (0.05-0.15s)
- Organize animations in folders

### ❌ DON'T:
- Enable root motion (unless you know what you're doing)
- Use long transition times (causes lag in response)
- Forget to add animation events
- Skip LOD optimization
- Put all 100 animations in one blend tree
- Update animations when not visible

---

## 🚀 Next Steps

1. **Import your 100 animations**
2. **Follow this guide step by step**
3. **Test with 1 archer**
4. **Spawn 100 archers and profile**
5. **Tweak settings for your needs**
6. **Add audio and VFX**

---

## 📞 Support

If you encounter issues:
1. Check the Troubleshooting section
2. Validate your Animator parameters
3. Enable Gizmos to visualize IK
4. Use Debug.Log in the controller
5. Profile to find bottlenecks

---

**Created by:** Archer Animation System v1.0
**Optimized for:** Unity 2020.3+
**Performance Target:** 100+ units at 60 FPS
**Complexity:** 100+ animations with directional blending

---

## Quick Reference Checklist

- [ ] Imported all animations
- [ ] Created `ArcherAnimatorController`
- [ ] Added all parameters
- [ ] Created 2 layers (Base + Upper Body)
- [ ] Set up locomotion blend tree
- [ ] Created combat states on Layer 1
- [ ] Added animation events
- [ ] Created `UpperBodyMask`
- [ ] Added `ArcherAnimationController` component
- [ ] Added `ArcherAimIK` component
- [ ] Configured all settings
- [ ] Tested movement (8 directions)
- [ ] Tested combat sequence (Draw-Aim-Release)
- [ ] Tested IK aiming
- [ ] Performance tested with 100 units
- [ ] Optimized LOD settings

**You're ready to go! 🏹**
