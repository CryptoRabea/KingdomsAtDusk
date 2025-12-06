# Animal Spawning System

A comprehensive animal spawning system for Kingdoms At Dusk that spawns wildlife based on biome/terrain types with configurable spawn probabilities.

## Features

- **Biome-Based Spawning**: Animals spawn in specific terrain types (grasslands, rivers, swamps, etc.)
- **Spawn Probabilities**: Each animal has configurable spawn chances for different biomes
- **Roaming Behavior**: Animals wander around their spawn point
- **Flee Behavior**: Animals flee when attacked or threatened
- **Event-Driven**: Integrated with the game's EventBus system
- **Object Pooling**: Uses the existing object pool for performance
- **Service Architecture**: Follows the game's service locator pattern

## Components

### Core Classes

1. **AnimalConfigSO** - ScriptableObject defining animal stats and behavior
   - Health, speed, roaming settings
   - Biome spawn preferences with probabilities
   - Flee and detection settings

2. **BiomeData** - ScriptableObject defining biome characteristics
   - Terrain detection rules (height, slope, textures)
   - Allowed animals and population limits
   - Spawn rates

3. **AnimalBehavior** - Component controlling animal AI
   - State machine (Idle, Roaming, Fleeing)
   - Threat detection
   - Uses existing UnitMovement and UnitHealth components

4. **BiomeManager** - Manages terrain biome detection
   - Samples terrain to determine biome type
   - Validates spawn positions
   - Provides random spawn positions per biome

5. **AnimalSpawner** - Service managing animal spawning
   - Spawns animals based on biome preferences
   - Tracks population limits
   - Respawns animals over time

### Enums

- **BiomeType**: Grassland, Forest, River, Swamp, Mountain, Desert, Tundra, Beach
- **AnimalType**: Sheep, Cow, Pig, Chicken, Horse, Deer, Wolf, Bear, Boar, Rabbit, Fox, Crocodile, Snake, Eagle, Crow
- **AnimalState**: Idle, Roaming, Fleeing, Grazing

## Setup Instructions

### 1. Create Biome Data

1. Right-click in Project > Create > RTS > Animals > Biome Data
2. Configure biome settings:
   - Set biome type (e.g., Grassland, River, Swamp)
   - Define terrain texture names
   - Set height and slope ranges
   - Assign allowed animals
   - Configure spawn rate and max population

**Example Configurations:**

**River Biome:**
- Height Range: 0-20
- Slope Range: 0-15
- Allowed Animals: Crocodile (high probability)
- Spawn Rate: 0.5

**Grassland Biome:**
- Height Range: 0-50
- Slope Range: 0-30
- Allowed Animals: Sheep (high), Cow (medium), Rabbit (medium)
- Spawn Rate: 1.0

### 2. Create Animal Configs

1. Right-click in Project > Create > RTS > Animals > Animal Config
2. Configure animal settings:
   - Set animal type and name
   - Set health, speed, roaming radius
   - Configure biome preferences with spawn probabilities
   - Assign prefab and icon

**Example: Crocodile Config**
```
Animal Type: Crocodile
Max Health: 150
Move Speed: 2.0
Roaming Radius: 10
Flees When Attacked: false

Biome Preferences:
- River: 0.9 (90% chance)
- Swamp: 0.7 (70% chance)
- Beach: 0.3 (30% chance)
```

**Example: Sheep Config**
```
Animal Type: Sheep
Max Health: 50
Move Speed: 2.5
Roaming Radius: 15
Flees When Attacked: true

Biome Preferences:
- Grassland: 0.8 (80% chance)
- Forest: 0.3 (30% chance)
```

### 3. Setup Scene

1. **Create BiomeManager**:
   - Add empty GameObject named "BiomeManager"
   - Add BiomeManager component
   - Assign your BiomeData configs
   - Reference the Terrain object

2. **Create AnimalSpawner**:
   - Add empty GameObject named "AnimalSpawner"
   - Add AnimalSpawner component
   - Assign your AnimalConfigSO assets
   - Reference the BiomeManager
   - Configure spawn settings:
     - Spawn Interval: 10 seconds (adjust as needed)
     - Max Total Animals: 100
     - Spawn Radius: 100
     - Spawn Center: Set to world center or desired location
     - Initial Animal Count: 20

3. **Create Animal Prefabs**:
   - Create prefab for each animal
   - Add required components:
     - NavMeshAgent
     - UnitMovement
     - UnitHealth
     - AnimalBehavior
   - Configure NavMeshAgent settings
   - Set up visuals (model, animations)

## Usage

### Starting Animal Spawning

The AnimalSpawner service is automatically registered and starts spawning if "Spawn On Start" is enabled.

To manually control spawning:
```csharp
using RTS.Core.Services;
using RTS.Animals;

// Get the service
var animalSpawner = ServiceLocator.Get<IAnimalSpawnerService>();

// Start spawning
animalSpawner.StartSpawning();

// Stop spawning
animalSpawner.StopSpawning();

// Get animal count
int totalAnimals = animalSpawner.GetAnimalCount();
int sheepCount = animalSpawner.GetAnimalCount(AnimalType.Sheep);
```

### Spawning Specific Animals

```csharp
using RTS.Animals;
using UnityEngine;

// Spawn a specific animal at a position
animalSpawner.SpawnAnimal(crocodileConfig, new Vector3(100, 0, 100));
```

### Listening to Animal Events

```csharp
using RTS.Core.Events;
using RTS.Animals;

// Subscribe to animal spawn events
EventBus.Subscribe<AnimalSpawnedEvent>(OnAnimalSpawned);

// Subscribe to animal death events
EventBus.Subscribe<AnimalDiedEvent>(OnAnimalDied);

void OnAnimalSpawned(AnimalSpawnedEvent evt)
{
    Debug.Log($"Animal spawned: {evt.AnimalType} at {evt.Position}");
}

void OnAnimalDied(AnimalDiedEvent evt)
{
    Debug.Log($"Animal died: {evt.AnimalType}");
}
```

## How It Works

### Spawn Process

1. **Spawn Timer**: AnimalSpawner checks spawn interval
2. **Select Animal**: Randomly picks an animal config
3. **Find Biome**: Checks animal's biome preferences
4. **Roll Probability**: For each preferred biome, rolls spawn chance
5. **Find Position**: BiomeManager finds valid spawn position in selected biome
6. **Spawn**: Creates animal using object pool and initializes behavior

### Biome Detection

BiomeManager samples terrain to determine biome:
1. Gets terrain height at position
2. Calculates slope from terrain normal
3. Samples terrain textures (splat map)
4. Matches against biome data rules
5. Returns biome type

### Animal Behavior

Animals operate on a simple state machine:

**Idle State**:
- Stands still for 2-5 seconds
- Transitions to Roaming

**Roaming State**:
- Picks random point within roaming radius
- Walks to destination
- Returns to Idle when reached

**Fleeing State**:
- Triggered by damage or nearby threats
- Runs away from threat
- Returns to Idle after flee duration

## Configuration Tips

### For Territorial Animals (Crocodiles near water)
- Set high spawn probability (0.8-0.9) for River/Swamp biomes
- Set low roaming radius (5-10)
- Set `fleesWhenAttacked = false`

### For Roaming Animals (Sheep on grasslands)
- Set medium-high probability (0.6-0.8) for preferred biome
- Set larger roaming radius (15-20)
- Set `fleesWhenAttacked = true`
- Higher flee distance (15-20)

### Population Control
- Adjust `maxTotalAnimals` in AnimalSpawner
- Set `maxPopulation` per biome in BiomeData
- Increase/decrease `spawnInterval` for respawn rate

## Future Enhancements

Potential features to add:
- Hunting system (players can hunt animals for food)
- Predator/prey relationships (wolves hunt sheep)
- Breeding and reproduction
- Animal sounds and animations
- Seasonal migration
- Pack behavior
- Grazing animations
- Resource drops when hunted

## Architecture Integration

The system integrates with existing game systems:

- **UnitMovement**: Reuses movement component for consistency
- **UnitHealth**: Reuses health system with HP bars
- **EventBus**: Publishes AnimalSpawned/AnimalDied events
- **ObjectPool**: Uses pooling for performance
- **ServiceLocator**: Registered as IAnimalSpawnerService
- **FlowField**: Animals use NavMesh for pathfinding

## Files Created

```
Assets/Scripts/Animals/
├── AnimalType.cs              # Animal species enum
├── BiomeType.cs               # Terrain biome types enum
├── AnimalConfigSO.cs          # Animal configuration ScriptableObject
├── BiomeData.cs               # Biome configuration ScriptableObject
├── AnimalBehavior.cs          # Animal AI component
├── BiomeManager.cs            # Terrain biome detection
├── AnimalSpawner.cs           # Main spawning service
├── IAnimalSpawnerService.cs   # Service interface
└── README.md                  # This file

Assets/Scripts/Core/
├── GameEvents.cs              # Added AnimalSpawnedEvent, AnimalDiedEvent
└── IServices.cs               # Added IAnimalSpawnerService interface
```
