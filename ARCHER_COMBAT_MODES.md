# 🎯 Archer Combat Modes - Developer Guide

**Flexible combat behavior system for tactical archer control**

---

## 📋 Overview

The Archer Combat Mode system gives you full control over whether archers can shoot while moving or must stand still to attack. This is perfect for:

- **Different archer types** (light skirmishers vs heavy crossbowmen)
- **Tactical gameplay** (positioning matters vs micro-management)
- **Balance adjustments** (kiting control)
- **Special abilities** (temporary mode changes)

---

## 🎮 Three Combat Modes

### 1. **Must Stand Still** ⚓
```csharp
CombatMovementMode.MustStandStill
```

**Behavior:**
- Archer MUST stop moving completely before shooting
- Unit auto-stops when target is in range
- Settle time before first shot (configurable)
- Auto-faces target when stationary
- More realistic, requires positioning

**Best For:**
- Heavy crossbowmen
- Realistic RTS gameplay
- Positioning-focused tactics
- Preventing kiting abuse

**Settings:**
```csharp
stationaryAttackRange = 8f;     // Stop distance from target
aimSettleTime = 0.2f;           // Time to settle before shooting
autoFaceTarget = true;          // Auto-rotate to target
```

---

### 2. **Can Shoot While Moving** 🏃‍♂️
```csharp
CombatMovementMode.CanShootWhileMoving
```

**Behavior:**
- Archer can shoot freely while moving
- Enables kiting tactics
- Optional speed reduction while shooting
- More arcade-style gameplay
- Better for micro-management

**Best For:**
- Light skirmishers
- Mobile archers
- Kiting gameplay
- Fast-paced combat

**Settings:**
```csharp
reduceSpeedWhileShooting = true;    // Slow down when shooting
combatSpeedMultiplier = 0.5f;       // Speed when shooting (50%)
```

---

### 3. **Adaptive** 🔄
```csharp
CombatMovementMode.Adaptive
```

**Behavior:**
- Uses standing animations when stationary
- Uses moving animations when moving
- Best of both worlds
- Natural transitions

**Best For:**
- Versatile archer units
- Balanced gameplay
- Players who want options

---

## 🛠️ Quick Setup

### Step 1: Add Component

When you use `Tools` → `RTS` → `Archer` → `Setup Archer Animation System`, the `ArcherCombatMode` component is automatically added!

Or manually:
```
1. Select archer GameObject
2. Add Component → Archer Combat Mode
```

---

### Step 2: Configure in Inspector

```
Combat Mode Settings:
├─ Movement Mode: CanShootWhileMoving (or other mode)
│
Stationary Combat Settings:
├─ Stationary Attack Range: 8.0
├─ Aim Settle Time: 0.2
├─ Auto Face Target: ✓
│
Moving Combat Settings:
├─ Reduce Speed While Shooting: ✓
├─ Combat Speed Multiplier: 0.5
│
Animation Blending:
├─ Use Standing Animations While Moving: ✓
├─ Standing Animation Weight: 1.0
│
Runtime Control:
└─ Allow Runtime Mode Change: ✓
```

---

## 🎨 Using Standing Animations While Moving

One of your key requests! You can use the standing draw/aim/release animations even while the archer is walking.

### Option 1: Animation Blending (Recommended)

**In Animator Controller:**

1. Create a **blend tree on Upper Body Layer**
2. Blend between:
   - Standing combat animations (weight when stationary)
   - Moving combat animations (weight when moving)

**In ArcherCombatMode component:**
```
Use Standing Animations While Moving: ✓
Standing Animation Weight: 0.7
```

This creates a **70% standing / 30% moving blend** = realistic looking aim while walking!

---

### Option 2: Force Standing Animations

**Set weight to 1.0** to always use standing animations:
```
Standing Animation Weight: 1.0
```

The lower body will still animate walking, but upper body uses standing combat anims.

---

### Option 3: Runtime Control (Code)

```csharp
ArcherCombatMode combatMode = archer.GetComponent<ArcherCombatMode>();

// Use standing animations while moving
combatMode.SetUseStandingAnimations(true);

// Set blend weight (0 = moving anims, 1 = standing anims)
combatMode.SetStandingAnimationWeight(0.8f);
```

---

## 🎯 Changing Modes at Runtime

### Via Editor Menu (Select archers first!)

```
Tools → RTS → Archer → Combat Mode →
├─ Set: Must Stand Still
├─ Set: Can Shoot While Moving
├─ Set: Adaptive
└─ Toggle Mode
```

**Quick Toggle**: Select archer(s) → `Tools` → `RTS` → `Archer` → `Combat Mode` → `Toggle Mode`

---

### Via Code

```csharp
ArcherCombatMode combatMode = archer.GetComponent<ArcherCombatMode>();

// Change mode
combatMode.SetCombatMode(CombatMovementMode.MustStandStill);

// Toggle between stationary and moving
combatMode.ToggleCombatMode();

// Temporary mode change (for abilities, etc.)
combatMode.ForceStationaryMode(3f); // Force stationary for 3 seconds
combatMode.ForceMovingMode(5f);     // Force moving for 5 seconds
```

---

### Via Ability System Example

```csharp
// "Rooted Shot" ability - must stand still
public class RootedShotAbility : MonoBehaviour
{
    public void Activate()
    {
        ArcherCombatMode mode = GetComponent<ArcherCombatMode>();

        // Force stationary for ability duration
        mode.ForceStationaryMode(2f);

        // Increase damage while stationary (your damage system)
        // damageBoost = 1.5f;
    }
}
```

```csharp
// "Kiting Shot" ability - shoot while moving
public class KitingShotAbility : MonoBehaviour
{
    public void Activate()
    {
        ArcherCombatMode mode = GetComponent<ArcherCombatMode>();

        // Force moving mode for duration
        mode.ForceMovingMode(3f);

        // Increase movement speed (already built-in!)
        UnitMovement movement = GetComponent<UnitMovement>();
        movement.SetSpeedMultiplier(1.5f);
    }
}
```

---

## ⚙️ Advanced Configuration

### Speed Reduction While Shooting

**When enabled**, archer slows down during combat:

```csharp
// In ArcherCombatMode inspector
Reduce Speed While Shooting: ✓
Combat Speed Multiplier: 0.5    // 50% speed
```

**Result**: Archer moves at half speed while drawing/aiming/releasing

**Use Case**: Prevents archers from running full speed while shooting

---

### Custom Settle Time

**For stationary mode**, how long to wait after stopping before shooting:

```csharp
Aim Settle Time: 0.2
```

- `0.0` = Instant shooting (less realistic)
- `0.2` = Quick settle (balanced)
- `0.5+` = Slow settle (very realistic)

---

### Auto-Face Target

**For stationary mode**, automatically rotate to face target:

```csharp
Auto Face Target: ✓
```

- `true` = Smooth rotation toward enemy
- `false` = Manual rotation only

---

## 📊 Mode Comparison Table

| Feature | Must Stand Still | Can Shoot While Moving | Adaptive |
|---------|------------------|------------------------|----------|
| Shoot while moving | ❌ No | ✅ Yes | ✅ Both |
| Kiting possible | ❌ No | ✅ Yes | ⚠️ Limited |
| Positioning important | ✅ Yes | ❌ Less | ⚠️ Medium |
| Micro-management | Low | High | Medium |
| Realism | High | Low | Medium |
| Speed reduction | N/A | ✅ Optional | ✅ Optional |
| Best for | Heavy archers | Light archers | Versatile |

---

## 🎬 Animation Setup for Each Mode

### Must Stand Still

**Required Animations:**
- Standing: Idle, Draw, Aim, Release
- Movement: 8-way walking
- Transitions: Walk → Stand → Draw

**Animator Setup:**
1. Standing combat on Upper Body Layer
2. Movement on Base Layer
3. Transition to combat when `IsMoving = false`

---

### Can Shoot While Moving

**Required Animations:**
- Walking while shooting (all 8 directions)
  - Walk_Forward_Draw, Walk_Forward_Aim, Walk_Forward_Release
  - Walk_Left_Draw, Walk_Left_Aim, etc.

**Animator Setup:**
1. Movement combat blend tree
2. Blend by DirectionX/DirectionY + CombatState
3. **OR** use standing anims on upper body layer (recommended!)

---

### Adaptive (Recommended!)

**Required Animations:**
- Standing combat (Draw, Aim, Release)
- 8-way movement
- Optional: Moving combat variations

**Animator Setup:**
1. Use **Standing anims on Upper Body Layer**
2. Movement on Base Layer
3. Set `Use Standing Animations While Moving = true`
4. System handles the rest automatically!

---

## 🧪 Testing Different Modes

### Test Scenario 1: Stationary Archer

```
1. Set mode to "Must Stand Still"
2. Place enemy nearby
3. Order archer to attack
4. Observe: Archer stops moving, settles, then shoots
5. Try moving during combat: Shooting pauses
```

---

### Test Scenario 2: Kiting Archer

```
1. Set mode to "Can Shoot While Moving"
2. Place enemy nearby
3. Order archer to attack while retreating
4. Observe: Archer shoots while walking backward
5. Check speed: Should be reduced if enabled
```

---

### Test Scenario 3: Adaptive Archer

```
1. Set mode to "Adaptive"
2. Order archer to attack
3. Let it stand: Uses standing animations
4. Order it to move: Continues shooting while moving
5. Observe smooth transitions
```

---

## 🎮 Gameplay Use Cases

### Example 1: Different Archer Types

```csharp
// Heavy Crossbowman
void SetupCrossbowman(GameObject unit)
{
    var mode = unit.GetComponent<ArcherCombatMode>();
    mode.SetCombatMode(CombatMovementMode.MustStandStill);
    mode.aimSettleTime = 0.5f; // Slower to aim
}

// Light Skirmisher
void SetupSkirmisher(GameObject unit)
{
    var mode = unit.GetComponent<ArcherCombatMode>();
    mode.SetCombatMode(CombatMovementMode.CanShootWhileMoving);
    mode.reduceSpeedWhileShooting = false; // Full speed kiting!
}

// Versatile Longbowman
void SetupLongbowman(GameObject unit)
{
    var mode = unit.GetComponent<ArcherCombatMode>();
    mode.SetCombatMode(CombatMovementMode.Adaptive);
    mode.useStandingAnimationsWhileMoving = true;
}
```

---

### Example 2: Formation Bonuses

```csharp
public class ArcherFormation : MonoBehaviour
{
    void OnFormationEntered()
    {
        // In formation: must stand still for accuracy bonus
        var archers = GetComponentsInChildren<ArcherCombatMode>();
        foreach (var archer in archers)
        {
            archer.SetCombatMode(CombatMovementMode.MustStandStill);
        }
    }

    void OnFormationBroken()
    {
        // Out of formation: free movement
        var archers = GetComponentsInChildren<ArcherCombatMode>();
        foreach (var archer in archers)
        {
            archer.SetCombatMode(CombatMovementMode.CanShootWhileMoving);
        }
    }
}
```

---

### Example 3: Upgrade System

```csharp
public class ArcherUpgrade : MonoBehaviour
{
    public void UnlockMobileArchery()
    {
        // Unlock ability to shoot while moving
        var archers = FindObjectsOfType<ArcherCombatMode>();
        foreach (var archer in archers)
        {
            if (archer.CurrentMode == CombatMovementMode.MustStandStill)
            {
                archer.SetCombatMode(CombatMovementMode.Adaptive);
            }
        }

        Debug.Log("Mobile Archery unlocked!");
    }
}
```

---

## 🔍 Debugging

### Gizmos (Select archer in Scene view)

**Must Stand Still Mode:**
- Green sphere = Settled and ready to shoot
- Yellow sphere = Waiting to settle
- Sphere radius = Stationary attack range

**Can Shoot While Moving Mode:**
- Blue ray = Moving at normal speed
- Red ray = Moving at reduced speed (in combat)

---

### Debug Logs

Enable logs in `ArcherCombatMode.cs`:

```csharp
Debug.Log($"Combat Mode: {movementMode}");
Debug.Log($"Can Shoot: {CanShootNow}");
Debug.Log($"Is Settled: {isSettled}");
Debug.Log($"Speed Multiplier: {speedMultiplier}");
```

---

## 💡 Pro Tips

### Tip 1: Blend Standing and Moving Animations

**For most realistic look:**
```
Use Standing Animations While Moving: ✓
Standing Animation Weight: 0.7
```

This creates a natural blend where upper body mostly uses standing pose, but sways slightly with walking motion.

---

### Tip 2: Balance Kiting

**To prevent OP kiting:**
```
Can Shoot While Moving: ✓
Reduce Speed While Shooting: ✓
Combat Speed Multiplier: 0.3  (very slow)
```

Archer can kite, but moves very slowly = balanced!

---

### Tip 3: Realistic Heavy Archers

**For slow, powerful crossbowmen:**
```
Mode: Must Stand Still
Stationary Attack Range: 10.0  (stop farther away)
Aim Settle Time: 0.8           (slow to aim)
Auto Face Target: ✓
```

---

### Tip 4: Fast Skirmishers

**For mobile hit-and-run:**
```
Mode: Can Shoot While Moving
Reduce Speed While Shooting: ✗  (keep full speed)
Combat Speed Multiplier: 1.0    (ignored when disabled)
```

---

### Tip 5: Use Adaptive for Player Units

**Best for player-controlled units:**
```
Mode: Adaptive
Allow Runtime Mode Change: ✓
```

Players can choose to stand for accuracy or move for safety!

---

## 🎯 Quick Reference

### Component Public API

```csharp
ArcherCombatMode mode = GetComponent<ArcherCombatMode>();

// Properties
CombatMovementMode currentMode = mode.CurrentMode;
bool canShoot = mode.CanShootNow;
bool settled = mode.IsSettled;

// Methods
mode.SetCombatMode(CombatMovementMode.MustStandStill);
mode.ToggleCombatMode();
mode.SetUseStandingAnimations(true);
mode.SetStandingAnimationWeight(0.8f);
mode.ForceStationaryMode(2f);
mode.ForceMovingMode(3f);
bool shouldAttack = mode.ShouldAllowAttack();
```

---

### Editor Menu

```
Tools → RTS → Archer → Combat Mode →
├─ Set: Must Stand Still
├─ Set: Can Shoot While Moving
├─ Set: Adaptive
└─ Toggle Mode
```

---

## ❓ FAQ

**Q: Can I change mode during gameplay?**
A: Yes! Set `Allow Runtime Mode Change = true` and use `SetCombatMode()`.

**Q: How do I use standing animations while walking?**
A: Set `Use Standing Animations While Moving = true` and configure the blend weight.

**Q: Can different archers have different modes?**
A: Absolutely! Each archer has its own `ArcherCombatMode` component.

**Q: Does this affect performance?**
A: Minimal impact. The mode check is a simple boolean comparison.

**Q: Can I create custom modes?**
A: Yes! Extend the `CombatMovementMode` enum and modify update logic.

---

## 📖 Related Documentation

- `ARCHER_ANIMATION_SETUP_GUIDE.md` - Full animation system guide
- `ARCHER_QUICK_START.md` - Quick setup guide
- `ArcherCombatMode.cs` - Source code with comments

---

**Created by:** Archer Animation System v1.0
**Feature:** Combat Mode System
**Unity Version:** 2020.3+

---

## Summary

✅ Three combat modes: Stationary, Moving, Adaptive
✅ Use standing animations while walking
✅ Easy editor toggles
✅ Runtime mode changes
✅ Speed reduction system
✅ Perfect for different archer types
✅ Balanced kiting control

**You now have full control over archer combat behavior!** 🎯
