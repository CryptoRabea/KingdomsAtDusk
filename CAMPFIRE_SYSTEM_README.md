# Campfire Peasant System - Complete Guide

A modular campfire and peasant management system inspired by Stronghold. Peasants gather around a campfire based on happiness, housing, reputation, and military strength. They can be allocated to various tasks like building construction, training troops, and collecting resources.

## 🎯 Features

✅ **Dynamic Peasant Gathering** - Peasants come and go based on:
- Happiness (0-100)
- Reputation (0-100)
- Housing capacity and utilization
- Military strength

✅ **Modular Worker Allocation** - Optional systems for:
- **Building Workers** - Speed up construction
- **Training Workers** - Speed up unit training
- **Resource Workers** - Boost resource production

✅ **Event-Driven Architecture** - Fully integrated with existing EventBus
✅ **Service Locator Pattern** - Clean dependency injection
✅ **Highly Configurable** - All settings exposed in ScriptableObjects
✅ **Visual Feedback** - Optional peasant visuals and particle effects

---

## 📦 Components Overview

### Core Services
- **PopulationManager** - Tracks total population, housing, and peasant allocation
- **ReputationManager** - Manages kingdom reputation/fame
- **PeasantWorkforceManager** - Coordinates peasant worker requests

### Building Components
- **CampfireDataSO** - ScriptableObject configuration for campfire settings
- **Campfire** - Main campfire component that manages peasant gathering
- **BuildingWorkerModule** - Optional module for construction workers
- **TrainingWorkerModule** - Optional module for training workers
- **ResourceWorkerModule** - Optional module for resource workers

---

## 🚀 Quick Setup

### Step 1: Add Managers to Scene

1. Open your main scene (usually the scene with GameManager)
2. Create three new empty GameObjects as children of GameManager:
   - `PopulationManager`
   - `ReputationManager`
   - `PeasantWorkforceManager`

3. Add the corresponding components to each:
   ```
   PopulationManager GameObject → Add Component → PopulationManager
   ReputationManager GameObject → Add Component → ReputationManager
   PeasantWorkforceManager GameObject → Add Component → PeasantWorkforceManager
   ```

4. Assign them in GameManager Inspector:
   - Drag `PopulationManager` to GameManager's `Population Manager` field
   - Drag `ReputationManager` to GameManager's `Reputation Manager` field
   - Drag `PeasantWorkforceManager` to GameManager's `Peasant Workforce Manager` field

### Step 2: Create Campfire Data ScriptableObject

1. Right-click in Project window (Assets/Prefabs/BuildingPrefabs&Data/)
2. Create → RTS → CampfireData
3. Name it `CampfireData_Basic`

4. Configure the settings:

   **Campfire Settings:**
   - Max Peasant Capacity: `20`
   - Gather Radius: `10`
   - Gather Update Interval: `2`

   **Attraction Factors:**
   - Minimum Happiness For Gathering: `30`
   - Minimum Reputation For Gathering: `20`
   - Happiness Influence: `0.3`
   - Housing Influence: `0.3`
   - Reputation Influence: `0.2`
   - Strength Influence: `0.2`

   **Bonuses:**
   - Happiness Bonus Per Peasant: `0.1`
   - Reputation Bonus Per Peasant: `0.05`

   **Worker Allocation (Optional):**
   - Enable Building Workers: ✓
   - Enable Training Workers: ✓
   - Enable Resource Workers: ✓
   - Peasants Per Building: `2`
   - Peasants Per Training: `1`
   - Peasants Per Resource Building: `3`

### Step 3: Create Campfire Prefab

1. Create a new empty GameObject in the scene
2. Name it `Campfire`
3. Add visual elements (fire particle system, props, etc.)

4. Add required components:
   ```
   Add Component → Building (from RTS.Buildings)
   Add Component → Campfire (from RTS.Buildings)
   ```

5. Configure Building Component:
   - Create a BuildingDataSO for the campfire (or use CampfireDataSO directly)
   - Set construction settings

6. Configure Campfire Component:
   - Assign your `CampfireData_Basic` ScriptableObject
   - Optionally assign Fire Effect (ParticleSystem)
   - Optionally assign Peasant Visual Prefab

7. **(Optional)** Add Worker Modules:
   ```
   Add Component → BuildingWorkerModule
   Add Component → TrainingWorkerModule
   Add Component → ResourceWorkerModule
   ```

8. Save as prefab in `Assets/Prefabs/BuildingPrefabs&Data/`

### Step 4: Configure Population Manager

In the Inspector for PopulationManager:
- Starting Population: `10`
- Base Housing Capacity: `20`
- Enable Natural Growth: ✓ (optional)
- Growth Rate: `0.1`
- Minimum Happiness For Growth: `50`

### Step 5: Configure Reputation Manager

In the Inspector for ReputationManager:
- Starting Reputation: `50`
- Min Reputation: `0`
- Max Reputation: `100`
- Affects Peasant Attraction: ✓

---

## 🎮 How It Works

### Peasant Calculation Formula

The campfire calculates the ideal number of peasants based on weighted factors:

```
idealPeasants = maxCapacity × (
    (happiness/100 × happinessInfluence) +
    (reputation/100 × reputationInfluence) +
    (housingUtilization × housingInfluence) +
    (militaryStrength × strengthInfluence)
) / totalInfluence
```

Example with default settings (all at 75%):
- Happiness: 75/100 = 0.75 × 0.3 = 0.225
- Reputation: 75/100 = 0.75 × 0.2 = 0.15
- Housing: 0.75 × 0.3 = 0.225
- Strength: 0.75 × 0.2 = 0.15
- **Total: 0.75** → 75% of max capacity = **15 peasants**

### Worker Allocation

When worker modules are enabled:

1. **Building Workers**: Automatically assigned when a building is placed
   - Speeds up construction by `constructionSpeedBonus` multiplier
   - Released when construction completes

2. **Training Workers**: Automatically assigned when units are queued
   - Speeds up training by `trainingSpeedBonus` multiplier
   - Released when training queue is empty

3. **Resource Workers**: Automatically assigned to resource buildings
   - Boosts production by `resourceProductionBonus` multiplier
   - Continuously assigned while building exists

---

## 🔧 Configuration Tips

### High Peasant Attraction
For a thriving campfire with many peasants:
- Keep happiness > 70
- Maintain high reputation > 75
- Build houses (increase housing capacity)
- Train military units

### Low Peasant Attraction (Challenging Mode)
For a harder game where peasants are scarce:
- Set higher minimum thresholds:
  - Minimum Happiness: `50` or higher
  - Minimum Reputation: `40` or higher
- Reduce influence weights

### Disable Worker Allocation
To have peasants only for visual effect:
- Uncheck all "Enable X Workers" options in CampfireDataSO
- Or don't add the worker module components to the prefab

### Multiple Campfires
You can have multiple campfires:
- Each tracks its own peasant count
- All draw from the same population pool
- Place them strategically around your kingdom

---

## 📊 Events Published

The campfire system publishes these events (subscribe via EventBus):

```csharp
// Population events
PopulationChangedEvent - When population/housing changes
PeasantAssignedEvent - When peasants assigned to work
PeasantReleasedEvent - When peasants released from work
CampfireGatheringChangedEvent - When campfire peasant count changes

// Reputation events
ReputationChangedEvent - When reputation changes
```

### Example Event Subscription

```csharp
using RTS.Core.Events;

void Start()
{
    EventBus.Subscribe<PopulationChangedEvent>(OnPopulationChanged);
    EventBus.Subscribe<CampfireGatheringChangedEvent>(OnCampfireChanged);
}

void OnPopulationChanged(PopulationChangedEvent evt)
{
    Debug.Log($"Population: {evt.TotalPopulation}, Available: {evt.AvailablePeasants}");
}

void OnCampfireChanged(CampfireGatheringChangedEvent evt)
{
    Debug.Log($"Campfire now has {evt.PeasantCount} peasants");
}

void OnDestroy()
{
    EventBus.Unsubscribe<PopulationChangedEvent>(OnPopulationChanged);
    EventBus.Unsubscribe<CampfireGatheringChangedEvent>(OnCampfireChanged);
}
```

---

## 🎨 Visual Customization

### Peasant Visuals

1. Create a simple peasant model (capsule, sprite, etc.)
2. Save as prefab
3. Assign to `Peasant Visual Prefab` in CampfireDataSO

The campfire will spawn these at predefined positions around the fire.

### Peasant Gather Positions

Configure in CampfireDataSO's `Peasant Gather Positions` array:
```
Position 0: (2, 0, 0)   - Right side
Position 1: (-2, 0, 0)  - Left side
Position 2: (0, 0, 2)   - Front
Position 3: (0, 0, -2)  - Back
...and so on for circular arrangement
```

---

## 🐛 Debug Commands

### Context Menu Commands (Right-click component in Inspector)

**PopulationManager:**
- Add 5 Peasants
- Remove 5 Peasants
- Increase Housing +10

**ReputationManager:**
- Add 10 Reputation
- Remove 10 Reputation
- Set Max Reputation

**Campfire:**
- Add 5 Peasants
- Remove 5 Peasants
- Fill to Max Capacity
- Clear All Peasants

**Worker Modules:**
- Show Assigned Workers

---

## 🔌 Integration with Existing Systems

### Buildings
The campfire system integrates automatically with:
- Building construction (BuildingPlacedEvent, BuildingCompletedEvent)
- Resource generation (checks BuildingDataSO.generatesResources)
- Housing (uses BuildingDataSO.providesHousing and housingCapacity)

### Units
- Training integration via UnitTrainingQueue
- Military strength calculated from units with "AllyUnit" tag

### Resources
- No changes needed to ResourceManager
- Workers boost production through multipliers

---

## 📝 File Locations

```
Assets/Scripts/
├── Core/
│   ├── GameEvents.cs (updated with new events)
│   └── IServices.cs (updated with new service interfaces)
├── Managers/
│   ├── GameManager.cs (updated to register services)
│   ├── PopulationManager.cs (new)
│   ├── ReputationManager.cs (new)
│   └── PeasantWorkforceManager.cs (new)
└── RTSBuildingsSystems/
    ├── CampfireDataSO.cs (new)
    ├── Campfire.cs (new)
    └── WorkerModules/
        ├── BuildingWorkerModule.cs (new)
        ├── TrainingWorkerModule.cs (new)
        └── ResourceWorkerModule.cs (new)
```

---

## 🎯 Example Use Cases

### 1. Stronghold-Style Kingdom
- Place campfire in front of main castle
- Enable all worker modules
- Set high influence weights
- Peasants gather when kingdom is thriving

### 2. Survival Mode
- Single campfire is critical
- Set strict minimum thresholds
- Peasants leave if happiness/reputation drops
- Must maintain balance to keep workers

### 3. Visual Only
- Disable all worker modules
- Use only for atmosphere
- Peasants as morale indicator
- No gameplay impact

---

## ⚠️ Known Limitations

1. **Worker Speed Bonuses** are logged but not fully implemented
   - BuildingWorkerModule needs Building.cs extension for speed multiplier
   - TrainingWorkerModule needs UnitTrainingQueue extension
   - ResourceWorkerModule needs Building.cs extension

2. **Military Strength** currently counts ally units
   - Can be extended to track specific unit types or building presence

3. **Peasant Visuals** are optional
   - System works without visuals (data-only)

---

## 🚀 Future Extensions

**Easy Additions:**
- Peasant names/identities
- Day/night cycle integration (peasants gather at night)
- Seasonal events affecting gathering
- Multiple campfire types (military camp, market, etc.)

**Advanced:**
- Path-finding for peasant movement
- Peasant mood/behavior states
- Mini-games or interactions
- Peasant skills/specializations

---

## ❓ Troubleshooting

**No peasants appearing:**
- Check PopulationManager has starting population
- Verify happiness and reputation meet minimum thresholds
- Ensure building is constructed (not under construction)

**Workers not assigned:**
- Check PeasantWorkforceManager is registered in GameManager
- Verify worker modules are enabled in CampfireDataSO
- Ensure enough available peasants exist

**Services not found:**
- Open GameManager Inspector
- Verify all three managers are assigned
- Check console for "registered as IXService" messages

---

## 📄 License

This system is part of the Kingdoms at Dusk project.

---

**Enjoy your campfire peasant system! 🔥👥**
