# Construction Visual System

A flexible and extensible system for creating construction animations in the Kingdoms at Dusk RTS game.

## Overview

The Construction Visual System provides multiple visual effects that play during building construction. These effects automatically track the parent Building's construction progress and update their visuals accordingly.

## Quick Start

### Basic Setup

1. Create an empty GameObject as a child of your building prefab
2. Name it "ConstructionVisual"
3. Add one of the construction visual components to it
4. Reference this GameObject in the Building component's `constructionVisual` field

The Building component will automatically show/hide this GameObject during construction.

### Example Hierarchy

```
HousePrefab (Building component attached)
├── Model
│   └── HouseMesh
└── ConstructionVisual (One of the visual components attached)
    └── VisualContent (meshes, particles, etc.)
```

## Available Visual Effects

### 1. GroundUpConstructionVisual

**Effect**: Building appears to grow from the ground upward.

**Features**:
- Supports two modes: Clipping Plane (shader-based) or Scale (transform-based)
- Optional construction tint color
- Smooth upward reveal

**Settings**:
- `useClippingPlane`: Use shader-based clipping (requires compatible shader with `_ClipHeight` property)
- `useScale`: Alternative mode using Y-axis scaling
- `heightOffset`: Offset from ground level
- `constructionTint`: Color tint during construction
- `useTint`: Enable/disable color tinting

**Best For**: Buildings with vertical architecture like houses, towers, walls

**Setup Example**:
1. Add `GroundUpConstructionVisual` to your ConstructionVisual GameObject
2. Copy your building's mesh to be a child of ConstructionVisual
3. Set `useScale = true` for simple setup (no shader required)
4. Set construction tint to orange/yellow for a warm building effect

---

### 2. FadeInConstructionVisual

**Effect**: Building materializes by fading from transparent to solid.

**Features**:
- Smooth alpha transition with animation curve
- Optional construction tint color
- Material instancing or property blocks
- Optional particle effects during materialization

**Settings**:
- `startAlpha`: Starting transparency (0 = invisible, 1 = solid)
- `endAlpha`: Ending transparency
- `fadeCurve`: Animation curve controlling fade timing
- `createMaterialInstances`: Create material instances (true) or use property blocks (false)
- `constructionTint`: Color during construction
- `materializationParticlePrefab`: Optional particle effect
- `particleSpawnRate`: How often to spawn particles (0-1 range)

**Best For**: Magical/mystical buildings, sci-fi structures, special buildings

**Setup Example**:
1. Add `FadeInConstructionVisual` to your ConstructionVisual GameObject
2. Copy your building's mesh to be a child of ConstructionVisual
3. Set `createMaterialInstances = true` if you want per-building materials
4. Assign a particle prefab for sparkles/magic effects
5. Adjust the `fadeCurve` to control the materialization timing

---

### 3. ScaffoldingConstructionVisual

**Effect**: Shows wireframe scaffolding that transitions to solid building.

**Features**:
- Procedural wireframe generation
- Optional grid overlay
- Construction particles (sparks, welding effects)
- Audio support for construction sounds
- Smooth transition from wireframe to solid

**Settings**:
- `showWireframe`: Enable wireframe visualization
- `wireframeColor`: Color of the wireframe (default: orange)
- `wireframeThickness`: Thickness of wireframe lines
- `transitionPoint`: When to start transitioning to solid (0-1, default: 0.7)
- `showGridOverlay`: Show grid lines on the building
- `gridSize`: Size of grid squares
- `spawnConstructionParticles`: Enable spark particles
- `sparkParticlePrefab`: Particle effect for welding sparks
- `constructionSounds`: Array of construction audio clips
- `soundInterval`: How often to play sounds

**Best For**: Realistic construction, industrial buildings, fortifications

**Setup Example**:
1. Add `ScaffoldingConstructionVisual` to your ConstructionVisual GameObject
2. Copy your building's mesh to be a child of ConstructionVisual
3. Assign spark particle prefabs and construction sounds
4. Adjust `transitionPoint` to control when the building becomes solid
5. Tune `wireframeThickness` based on your building size

---

### 4. ParticleAssemblyConstructionVisual

**Effect**: Particles swarm from all directions and assemble the building.

**Features**:
- Particles fly in from random directions
- Smooth particle movement with physics option
- Building gradually becomes opaque as particles arrive
- Customizable particle appearance and behavior

**Settings**:
- `particlePrefab`: Custom particle prefab (optional - creates cubes if null)
- `particleCount`: Number of particles to spawn
- `particleSpeed`: How fast particles move
- `spawnRadius`: How far away particles spawn
- `particleColor`: Color of default particles
- `particleSpawnCurve`: Controls particle spawn timing
- `particleLifetime`: How long particles take to reach building
- `usePhysics`: Use physics-based movement
- `revealCurve`: Controls building opacity reveal

**Best For**: Futuristic buildings, nanobot assembly, magical construction, sci-fi

**Setup Example**:
1. Add `ParticleAssemblyConstructionVisual` to your ConstructionVisual GameObject
2. Copy your building's mesh to be a child of ConstructionVisual
3. Optionally assign a custom particle prefab
4. Adjust `particleCount` based on building size (50-200 works well)
5. Tune `spawnRadius` and `particleSpeed` for desired effect
6. Enable `usePhysics = true` for more organic movement

---

## Advanced Usage

### Creating Custom Construction Visuals

All construction visuals inherit from `BaseConstructionVisual`. To create your own:

```csharp
using UnityEngine;
using RTS.Buildings;

public class MyCustomConstructionVisual : BaseConstructionVisual
{
    protected override void Initialize()
    {
        // Called when the visual is first created
        // Set up your initial state here
    }

    protected override void UpdateVisual(float progress)
    {
        // Called every frame with progress value (0.0 to 1.0)
        // Update your visual based on construction progress

        // Example: Scale based on progress
        transform.localScale = Vector3.one * progress;
    }

    protected override void Cleanup()
    {
        // Called when construction completes
        // Clean up any temporary objects or reset state
    }
}
```

### Useful Base Class Properties

- `parentBuilding`: Reference to the parent Building component
- `currentProgress`: Current construction progress (0.0 to 1.0)
- `renderers`: Array of all Renderer components
- `meshFilters`: Array of all MeshFilter components
- `combinedBounds`: Bounds of all meshes combined
- `affectChildren`: Whether to include child renderers
- `updateInterval`: How often to update visuals (performance)

### Useful Helper Methods

```csharp
Vector3 size = GetBoundsSize();      // Get mesh bounds size
Vector3 center = GetBoundsCenter();  // Get mesh bounds center
```

## Performance Considerations

### Update Interval

All visuals have an `updateInterval` setting (default: 0.05 seconds). This controls how often the visual updates.

- Lower values (0.01s) = smoother animation, higher CPU cost
- Higher values (0.1s) = choppier animation, lower CPU cost
- Default (0.05s) = good balance for most cases

### Material Property Blocks

Use `MaterialPropertyBlock` instead of creating material instances when possible:

- ✅ `MaterialPropertyBlock` - No memory allocation, very fast
- ❌ Material instances - Allocates memory, slower, but allows more control

The `FadeInConstructionVisual` has a `createMaterialInstances` setting to choose between these approaches.

### Particle Limits

For `ParticleAssemblyConstructionVisual`, keep particle counts reasonable:

- Small buildings: 50-100 particles
- Medium buildings: 100-200 particles
- Large buildings: 200-500 particles

## Integration with Building System

### How It Works

1. When `Building.StartConstruction()` is called, it activates the `constructionVisual` GameObject
2. The construction visual component's `OnEnable()` is called, which calls `Initialize()`
3. Every frame, the visual queries `Building.ConstructionProgress` (0.0 to 1.0)
4. The visual updates based on this progress
5. When construction completes, `Building.CompleteConstruction()` deactivates the GameObject
6. The visual's `OnDisable()` is called, which calls `Cleanup()`

### Events

Construction visuals can respond to building events:

```csharp
// In your custom visual component
void OnEnable()
{
    EventBus.Subscribe<BuildingPlacedEvent>(OnBuildingPlaced);
    EventBus.Subscribe<BuildingCompletedEvent>(OnBuildingCompleted);
}

void OnDisable()
{
    EventBus.Unsubscribe<BuildingPlacedEvent>(OnBuildingPlaced);
    EventBus.Unsubscribe<BuildingCompletedEvent>(OnBuildingCompleted);
}
```

## Debugging

### Gizmos

All construction visuals draw debug gizmos in the editor when selected:

- **Cyan wireframe cube**: Combined bounds of all meshes
- **Yellow/Orange lines**: Construction progress indicators
- **Green spheres**: Particle target positions

### Common Issues

**Visual doesn't show up:**
- Ensure the GameObject is set in Building's `constructionVisual` field
- Check that Building has `requiresConstruction = true`
- Verify the visual has renderers or meshes as children

**Visual stutters:**
- Increase `updateInterval` for better performance
- Check particle count if using ParticleAssemblyConstructionVisual

**Materials look wrong:**
- For FadeInConstructionVisual, try toggling `createMaterialInstances`
- Ensure shaders support the properties being set
- Check shader compatibility (Standard, URP, HDRP)

**Building appears at wrong opacity:**
- The building mesh should be in the ConstructionVisual GameObject
- The main building mesh should be separate (or disabled during construction)

## Examples

### Example 1: Simple House with Ground-Up Effect

```
HousePrefab
├── Building (Component: constructionTime = 5s)
├── HouseModel (visible after construction)
└── ConstructionVisual (Building.constructionVisual references this)
    ├── GroundUpConstructionVisual (Component)
    │   - useScale = true
    │   - constructionTint = (1, 0.8, 0.4)
    └── HouseModelCopy (copy of HouseModel)
```

### Example 2: Magical Tower with Fade-In + Particles

```
TowerPrefab
├── Building (Component: constructionTime = 10s)
├── TowerModel
└── ConstructionVisual
    ├── FadeInConstructionVisual (Component)
    │   - fadeCurve = Ease In Out
    │   - constructionTint = (0.5, 0.5, 1, 1) // Blue
    │   - materializationParticlePrefab = MagicSparkles
    └── TowerModelCopy
```

### Example 3: Barracks with Scaffolding

```
BarracksPrefab
├── Building (Component: constructionTime = 15s)
├── BarracksModel
├── AudioSource (for construction sounds)
└── ConstructionVisual
    ├── ScaffoldingConstructionVisual (Component)
    │   - wireframeColor = Orange
    │   - transitionPoint = 0.7
    │   - sparkParticlePrefab = WeldingSparks
    │   - constructionSounds = [Hammer1, Hammer2, Saw1]
    └── BarracksModelCopy
```

## Credits

Built for Kingdoms at Dusk RTS game using the existing Building system architecture.

## License

Part of the Kingdoms at Dusk project.
