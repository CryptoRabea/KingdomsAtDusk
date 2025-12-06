# 🏹 Archer Animation System - Quick Start

**Get your archer up and running in 10 minutes!**

---

## ⚡ Quick Setup (5 Steps)

### Step 1: Import Your Animations
```
Drag your 100 animation files into:
Assets/Animations/Archer/
```

---

### Step 2: Auto-Setup Components

1. **Select** your archer GameObject in Hierarchy
2. **Menu**: `Tools` → `RTS` → `Archer` → `Setup Archer Animation System`
3. **Click** "OK" through the dialogs

✅ This automatically adds all required components!

---

### Step 3: Create Animator Controller

1. **Right-click** in Project → `Create` → `Animator Controller`
2. **Name it**: `ArcherAnimatorController`
3. **Select** your archer → **Animator** component
4. **Assign** `ArcherAnimatorController` to the Controller field

---

### Step 4: Add Parameters to Animator

**Open** the Animator Controller and add these parameters:

| Name | Type | Default |
|------|------|---------|
| DirectionX | Float | 0 |
| DirectionY | Float | 0 |
| Speed | Float | 0 |
| IsMoving | Bool | false |
| CombatState | Int | 0 |
| Draw | Trigger | - |
| Aim | Trigger | - |
| Release | Trigger | - |
| Death | Trigger | - |
| Hit | Trigger | - |

**Quick Tip**: Use `Tools` → `RTS` → `Archer` → `Create Upper Body Mask` for the upper body layer!

---

### Step 5: Set Up Basic States

**In Animator Controller, create these states:**

#### Base Layer (Layer 0):
- **Idle** (default state) → your idle animation
- **Locomotion** (blend tree) → 8-way movement
- **Death** → death animation

#### Upper Body Layer (Layer 1):
- **Drawing** → draw animation
- **Aiming** → aim animation (loop)
- **Releasing** → release animation

**Connect with triggers**: Draw → Aim → Release → loop

---

## 🎯 Blend Tree Setup (For 8-Way Movement)

1. **Right-click** → `Create State` → `From New Blend Tree`
2. **Name**: `Locomotion`
3. **Type**: `2D Freeform Directional`
4. **Parameters**: DirectionX (horizontal), DirectionY (vertical)

**Add your 8 walk animations at these positions:**

```
         Forward (0, 1)
              ↑
   FL(-0.7,0.7)   FR(0.7,0.7)
        ↖     ↗
Left(-1,0) ← + → Right(1,0)
        ↙     ↘
   BL(-0.7,-0.7) BR(0.7,-0.7)
              ↓
        Backward (0, -1)
```

---

## 🧪 Test It!

1. **Press Play**
2. **Click** to move your archer
3. **Watch** the animations blend in 8 directions
4. **Engage** an enemy to see Draw → Aim → Release

---

## ✅ Validate Your Setup

**Menu**: `Tools` → `RTS` → `Archer` → `Validate Archer Setup`

This checks if everything is configured correctly!

---

## ⚙️ Recommended Settings

**ArcherAnimationController**:
- Draw Duration: `0.5`
- Aim Duration: `0.3`
- Release Duration: `0.4`
- Enable LOD: `✓`
- Use 8-Way Movement: `✓`

**ArcherAimIK**:
- Enable IK: `✓`
- Body Weight: `0.3`
- Head Weight: `0.8`
- Smooth Time: `0.2`

---

## 📊 Animation Events

**CRITICAL**: Add this event to your **Release** animation:

1. Open Animation window
2. Select `Archer_Standing_Release`
3. Find frame where arrow leaves bow
4. Add event: `OnArrowRelease`

This is when the arrow fires!

---

## 🚀 Performance Tips

For **100+ archers**:

✅ Enable LOD (enabled by default)
✅ Enable Culling (enabled by default)
✅ Set LOD distances: 30m, 60m, 100m
✅ Use GPU Instancing on materials

Expected FPS: **60+ with 100 archers**

---

## 🔍 Troubleshooting

### Animations not playing?
→ Check Animator Controller is assigned

### Wrong animation timing?
→ Adjust `drawDuration`, `aimDuration`, `releaseDuration`

### Unit sliding?
→ Ensure `Apply Root Motion` = **false**

### Performance issues?
→ Enable LOD and check culling settings

---

## 📖 Full Documentation

For detailed setup, blend trees, IK configuration, and advanced features:

**Read**: `ARCHER_ANIMATION_SETUP_GUIDE.md`

---

## 🛠️ Handy Tools Menu

All under: `Tools` → `RTS` → `Archer` →

- ✨ **Setup Archer Animation System** - Auto-setup
- 🎭 **Create Upper Body Mask** - For Layer 1
- ⚙️ **Create Archer Animation Config** - Settings SO
- ✅ **Validate Archer Setup** - Check everything
- 📖 **Open Setup Guide** - Full documentation

---

## 📝 Minimum Required Animations

You need **at least these 24 animations** to start:

**Movement (8)**:
- Walk: Forward, Backward, Left, Right
- Walk: ForwardLeft, ForwardRight, BackwardLeft, BackwardRight

**Combat (3)**:
- Standing_Draw, Standing_Aim, Standing_Release

**Idle (1)**:
- Idle_Standing

**Reactions (4)**:
- Hit_Front, Hit_Left, Hit_Right, Hit_Back

**Death (1)**:
- Death_Forward

---

## 🎨 System Features

✅ **8-Way Directional Movement** - Smooth blending
✅ **Combat State Machine** - Draw → Aim → Release
✅ **IK Aiming** - Realistic target tracking
✅ **LOD System** - Distance-based optimization
✅ **Automatic Culling** - Disables when not visible
✅ **Animation Layering** - Aim while moving
✅ **Performance Optimized** - 100+ units at 60 FPS

---

## 🎯 What You Built

```
ArcherAnimationController.cs      → Main animation brain
ArcherAimIK.cs                     → IK system for aiming
ArcherAnimationConfig.cs           → Configuration SO
ArcherAnimationSetupHelper.cs      → Editor tools
```

---

**You're ready to go! 🎉**

Questions? Check `ARCHER_ANIMATION_SETUP_GUIDE.md` for details.

---

**System Version**: 1.0
**Performance Target**: 100+ units @ 60 FPS
**Unity Version**: 2020.3+
