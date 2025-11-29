# 🎮 Kingdoms at Dusk - Gameplay Features

## New Enemy Types & Win/Lose Conditions

This document describes the exciting new gameplay features added to Kingdoms at Dusk!

---

## 🗡️ New Enemy Types

### 1. **Berserker** (`BerserkerAI.cs`)
- **Role**: Glass cannon melee attacker
- **Special Ability**: **ENRAGE** - Gains 50% bonus damage and attack speed when health drops below 50%
- **Behavior**: Never retreats, fights to the death
- **Strategy**: High risk, high reward. Focus fire early before they enrage!

**Stats Suggested**:
- Health: 80 (lower than basic units)
- Damage: 15
- Speed: 5 (faster than normal)
- Attack Rate: 1.5

---

### 2. **Tank** (`TankAI.cs`)
- **Role**: Heavy frontline unit
- **Special Ability**: **TAUNT** - Forces nearby player units to attack it for 3 seconds (10 second cooldown)
- **Behavior**: Never retreats, absorbs damage
- **Strategy**: High priority target, or use to waste enemy attacks

**Stats Suggested**:
- Health: 300 (very tanky)
- Damage: 8 (low)
- Speed: 2 (very slow)
- Attack Rate: 0.8

---

### 3. **Enemy Archer** (`EnemyArcherAI.cs`)
- **Role**: Ranged DPS
- **Special Ability**: **KITING** - Maintains 10 unit distance, retreats if enemies get too close
- **Behavior**: Prefers distant targets, intelligent positioning
- **Strategy**: Fast units or flanking maneuvers needed

**Stats Suggested**:
- Health: 60 (fragile)
- Damage: 12
- Speed: 4
- Attack Rate: 1.2
- Attack Range: 12 (ranged!)

---

### 4. **Boss** (`BossAI.cs`)
- **Role**: Epic encounter, appears every 10 waves
- **Special Abilities**:
  - **PHASE 2** (66% HP): +25% damage, summons 3 minions
  - **PHASE 3** (33% HP): +50% damage, +50% attack speed, area attacks
  - **SUMMON**: Spawns minions every 15 seconds
  - **AREA ATTACK**: 30 damage to all units in 10 unit radius (20 second cooldown, Phase 2+)
- **Behavior**: Never retreats, phases get progressively harder
- **Strategy**: Requires coordinated attacks, healing, and unit management

**Stats Suggested**:
- Health: 2000+ (boss level)
- Damage: 40 (scales up in phases)
- Speed: 3
- Attack Rate: 0.8 (scales up in phase 3)

---

## 🏆 Victory Conditions

Victory conditions are modular - you can mix and match!

### **Survive Waves Victory** (`SurviveWavesVictory.cs`)
- **Objective**: Survive a specified number of waves
- **Configurable**: Set target wave count (default: 10)
- **Progress Tracking**: Shows current wave / target wave

### **Defeat Boss Victory** (`DefeatBossVictory.cs`)
- **Objective**: Defeat the boss enemy
- **Detection**: Automatically detects units with `BossAI` component or "Boss" tag
- **Binary**: Either completed or not

---

## ☠️ Defeat Conditions

### **Stronghold Destroyed** (`StrongholdDestroyedDefeat.cs`)
- **Lose Condition**: Player's main base (Stronghold) is destroyed
- **Auto-Detection**: Finds building named "Stronghold" automatically
- **Critical**: Your #1 priority to protect!

### **All Units Dead** (`AllUnitsDeadDefeat.cs`)
- **Lose Condition**: All player units dead AND insufficient resources to train more
- **Smart Detection**: Checks if you have 50 food + 25 gold to train basic unit
- **Periodic Check**: Evaluates every 2 seconds

---

## 🎯 Game Conditions Manager (`GameConditionsManager.cs`)

The central system managing all win/lose conditions.

### **Features**:
- Automatically discovers and initializes all attached conditions
- Checks conditions every second
- Defeat conditions take priority over victory conditions
- Two check modes:
  - `AnyVictory`: Any single victory condition triggers win
  - `AllVictory`: All victory conditions must be met

### **Usage**:
1. Create empty GameObject: "GameConditions"
2. Add `GameConditionsManager` component
3. Add child GameObjects with condition components:
   - `SurviveWavesVictory`
   - `DefeatBossVictory`
   - `StrongholdDestroyedDefeat`
   - `AllUnitsDeadDefeat`

The manager auto-discovers all conditions and manages them!

---

## 🏰 Stronghold Building System

### **BuildingHealth** (`BuildingHealth.cs`)
- Health component for buildings
- Allows buildings to take damage and be destroyed
- Publishes `BuildingDamagedEvent` and `BuildingDestroyedEvent`
- Optional destruction effects

### **Stronghold** (`Stronghold.cs`)
- Main player base building
- Has high health (configurable, default 1000+)
- Provides vision range (30 units)
- Rally point system for spawned units
- Critical damage warnings when health is low

**To Create a Stronghold**:
1. Create building GameObject
2. Add `Building` component with BuildingDataSO
3. Add `BuildingHealth` component
4. Add `Stronghold` component
5. Configure health in BuildingDataSO (maxHealth: 1000+)

---

## 🌊 Enhanced Wave System

### **EnemyWaveGenerator** (`EnemyWaveGenerator.cs`)

Creates progressively harder waves with varied enemy compositions.

**Wave Progression**:
- **Waves 1-2**: Footmen & Orcs only
- **Wave 3**: Berserkers introduced
- **Wave 4**: Archers join the fight
- **Wave 5**: Tanks enter the battlefield
- **Waves 6-9**: Mixed compositions
- **Wave 10, 20, 30...**: BOSS WAVES with elite support
- **Late Game**: All enemy types in balanced mixes

**Auto-Scaling**:
- Health: +5% per wave
- Damage: +3% per wave
- Enemy count: Base 3 + 2 per wave

---

## 🎮 How to Set Up Complete Gameplay

### **1. Create Game Conditions Manager**
```
Hierarchy:
  GameConditions
    ├─ GameConditionsManager (component)
    ├─ SurviveWavesVictory (component)
    ├─ DefeatBossVictory (component)
    ├─ StrongholdDestroyedDefeat (component)
    └─ AllUnitsDeadDefeat (component)
```

### **2. Configure Victory Conditions**
- **SurviveWavesVictory**: Set "Target Waves" (e.g., 10)
- **DefeatBossVictory**: Set "Boss Tag" (default: "Boss")

### **3. Configure Defeat Conditions**
- **StrongholdDestroyedDefeat**: Assign or auto-find Stronghold GameObject
- **AllUnitsDeadDefeat**: Set "Player Unit Layer" mask

### **4. Create Stronghold**
- Place Stronghold building in scene
- Add BuildingHealth (set max health 1000+)
- Add Stronghold component

### **5. Setup Wave Manager**
- Add enemy prefabs to WaveManager or use EnemyWaveGenerator
- Configure spawn points
- Set time between waves

### **6. Create Enemy Prefabs**

For each enemy type, create prefab with:
- UnitHealth component
- UnitMovement component
- UnitCombat component
- Appropriate AI: `BerserkerAI`, `TankAI`, `EnemyArcherAI`, or `BossAI`
- UnitConfigSO with appropriate stats
- AISettingsSO reference
- Layer set to "Enemy"

---

## 📊 Suggested Game Balance

### **Easy Mode**:
- Target Waves: 5
- Stronghold Health: 1500
- Starting Resources: High
- Time Between Waves: 45 seconds

### **Normal Mode**:
- Target Waves: 10
- Stronghold Health: 1000
- Starting Resources: Medium
- Time Between Waves: 30 seconds

### **Hard Mode**:
- Target Waves: 15
- Stronghold Health: 750
- Starting Resources: Low
- Time Between Waves: 20 seconds

### **Survival Mode**:
- Target Waves: Infinite (no SurviveWavesVictory)
- Victory: Defeat Boss Victory only
- Lose: Stronghold or All Units Dead

---

## 🔧 Customization Tips

### **Creating Custom Victory Conditions**:
1. Extend `VictoryCondition` abstract class
2. Implement `IsCompleted`, `Progress`, `Initialize()`, `Cleanup()`, `GetStatusText()`
3. Subscribe to relevant events in `Initialize()`
4. Update completion state based on game events

### **Creating Custom Defeat Conditions**:
1. Extend `DefeatCondition` abstract class
2. Implement `IsFailed`, `Initialize()`, `Cleanup()`, `GetStatusText()`
3. Listen for failure triggers

### **Custom Boss Abilities**:
- Modify `BossAI.cs` to add new abilities
- Use coroutines for complex ability sequences
- Integrate with particle systems for visual effects

---

## 🎯 Testing Checklist

- [ ] Enemies spawn correctly with new AI types
- [ ] Berserker enrages at low health
- [ ] Tank taunt pulls player units
- [ ] Archer kites and maintains distance
- [ ] Boss phases work correctly
- [ ] Boss summons minions
- [ ] Boss area attack hits multiple units
- [ ] Survive X waves triggers victory
- [ ] Defeating boss triggers victory
- [ ] Stronghold destruction triggers defeat
- [ ] All units dead triggers defeat
- [ ] Game state changes to Victory/GameOver correctly

---

## 📝 Events Published

All systems integrate with the existing event bus:

**New Events**:
- `BuildingDamagedEvent` - Building takes damage or is healed

**Used Events**:
- `WaveCompletedEvent` - Wave victory condition
- `UnitDiedEvent` - Boss defeat, unit counting
- `BuildingDestroyedEvent` - Stronghold destruction

---

## 🚀 Future Enhancements

Potential additions:
- More enemy types (Assassins, Mages, Siege units)
- More victory conditions (Reputation threshold, Time survival, Collect artifacts)
- More defeat conditions (Happiness zero, Resource depletion)
- Boss loot drops
- Special wave events (Ambush, Reinforcements)
- Dynamic difficulty adjustment
- Achievement system

---

## 📞 Component Reference

| Component | Location | Purpose |
|-----------|----------|---------|
| `BerserkerAI` | Units/AI/ | Berserker enemy AI |
| `TankAI` | Units/AI/ | Tank enemy AI |
| `EnemyArcherAI` | Units/AI/ | Archer enemy AI |
| `BossAI` | Units/AI/ | Boss enemy AI |
| `VictoryCondition` | Managers/ | Abstract base for victory |
| `DefeatCondition` | Managers/ | Abstract base for defeat |
| `SurviveWavesVictory` | Managers/Conditions/ | Wave survival victory |
| `DefeatBossVictory` | Managers/Conditions/ | Boss defeat victory |
| `StrongholdDestroyedDefeat` | Managers/Conditions/ | Stronghold loss defeat |
| `AllUnitsDeadDefeat` | Managers/Conditions/ | No units defeat |
| `GameConditionsManager` | Managers/ | Victory/defeat orchestrator |
| `BuildingHealth` | RTSBuildingsSystems/ | Building damage system |
| `Stronghold` | RTSBuildingsSystems/ | Main base building |
| `EnemyWaveGenerator` | Managers/ | Progressive wave generator |

---

**Made with ❤️ for epic RTS battles!**

*Defend your kingdom, survive the waves, defeat the bosses!*
