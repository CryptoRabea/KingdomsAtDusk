# Building LOD Progression System

A comprehensive building construction visualization system that smoothly transitions through LOD (Level of Detail) stages, with support for particle effects, audio, floating numbers, and progress bars.

## Overview

The **BuildingLODProgression** system provides:
- **LOD Progression**: Smooth transitions from LOD 7 (base foundation) to LOD 0 (complete building) based on construction progress
- **Particle Effects**: Array-based particle system with individual timing and duration controls
- **Audio Effects**: Array-based audio system with individual timing and duration controls
- **Floating Numbers**: Visual feedback showing construction progress percentages
- **Progress Bars**: Blue construction bar (fills right-to-left) and green health bar (after construction)
- **UI Integration**: Displays construction details in the Building Details UI when selected

---

## Components

### 1. BuildingLODProgression.cs
**Location**: `Assets/Scripts/RTSBuildingsSystems/ConstructionVisuals/BuildingLODProgression.cs`

Main component that manages the entire construction visualization system.

### 2. BuildingProgressUI.cs
**Location**: `Assets/Scripts/RTSBuildingsSystems/BuildingProgressUI.cs`

World-space canvas UI for displaying construction progress and health bars above buildings.

### 3. FloatingText.cs
**Location**: `Assets/Scripts/UI/FloatingText.cs`

Component for animated floating text that shows construction progress percentages.

### 4. BuildingDetailsUI.cs (Enhanced)
**Location**: `Assets/Scripts/UI/BuildingDetailsUI.cs`

Updated to show construction progress in the building detail panel when a building under construction is selected.

---

## Setup Instructions

### Step 1: Prepare LOD Meshes

1. Create 8 LOD versions of your building model:
   - **LOD 7** (Index 0): Base/foundation only
   - **LOD 6** (Index 1): Foundation + basic structure
   - **LOD 5** (Index 2): ~25% complete
   - **LOD 4** (Index 3): ~37.5% complete
   - **LOD 3** (Index 4): ~50% complete
   - **LOD 2** (Index 5): ~62.5% complete
   - **LOD 1** (Index 6): ~87.5% complete
   - **LOD 0** (Index 7): Fully complete building

2. Place all LOD meshes as children of your building prefab
3. Each LOD should be a separate GameObject

### Step 2: Add BuildingLODProgression Component

1. Select your building prefab
2. Add the **BuildingLODProgression** component
3. The component will automatically attach to your building's construction system

### Step 3: Configure LOD Meshes

In the **BuildingLODProgression** inspector:

1. Expand the **LOD Configuration** section
2. Drag your LOD meshes into the **Lod Meshes** array (size 8):
   - Element 0 → LOD 7 GameObject (base)
   - Element 1 → LOD 6 GameObject
   - ...
   - Element 7 → LOD 0 GameObject (complete)
3. Configure transition settings:
   - **Smooth Transition**: Enable for crossfade between LODs
   - **Transition Duration**: Time for smooth transitions (default 0.3s)

### Step 4: Setup World-Space Progress Bar (Optional)

1. Create a Canvas GameObject as a child of your building
2. Set Canvas **Render Mode** to **World Space**
3. Add the **BuildingProgressUI** component to the Canvas
4. Create UI elements:
   - **Background Image**: Dark semi-transparent bar
   - **Fill Image**: Colored bar (set to Filled type, Horizontal fill method)
   - **Progress Text**: TextMeshPro text for percentage
   - **Building Name Text**: TextMeshPro text for building name

5. Assign references in **BuildingProgressUI**:
   - World Canvas
   - Progress Bar Background
   - Progress Bar Fill
   - Progress Text
   - Building Name Text

6. In **BuildingLODProgression**, assign:
   - **Progress UI**: The BuildingProgressUI component
   - **World Space Canvas**: The canvas GameObject
   - **Progress Bar Image**: The fill image
   - **Progress Text**: The TextMeshPro text

### Step 5: Add Particle Effects (Optional)

1. In **BuildingLODProgression**, expand **Construction Effects**
2. Set the size of **Particle Effects** array to desired count
3. For each particle effect, configure:
   - **Name**: Identifier (e.g., "Dust Cloud", "Sparks")
   - **Particle System**: Reference to the particle system
   - **Start Time**: When to start (0-1 normalized, or absolute seconds)
   - **Duration**: How long to play (0 = play once)
   - **Use Absolute Time**: Check to use seconds instead of normalized time
   - **Loop**: Enable for looping particles
   - **Attach To LOD**: Optionally attach to specific LOD (-1 for none)

**Example**:
```
Particle Effect 0:
  Name: "Foundation Dust"
  Start Time: 0.0 (starts immediately)
  Duration: 2.0 seconds
  Use Absolute Time: true
  Attach To LOD: 7 (only shows on base)

Particle Effect 1:
  Name: "Construction Sparks"
  Start Time: 0.3 (30% through construction)
  Duration: 0 (plays once)
  Loop: false
```

### Step 6: Add Audio Effects (Optional)

1. In **BuildingLODProgression**, expand **Audio Effects** array
2. For each audio effect, configure:
   - **Name**: Identifier (e.g., "Hammering", "Sawing")
   - **Audio Clip**: The sound clip to play
   - **Audio Source**: Optional (will create one if null)
   - **Start Time**: When to start (0-1 normalized, or absolute seconds)
   - **Duration**: How long to play (0 = play full clip)
   - **Use Absolute Time**: Check to use seconds instead of normalized time
   - **Loop**: Enable for looping audio
   - **Volume**: Volume level (0-1)
   - **Spatial Blend**: 2D/3D mix (0 = 2D, 1 = 3D)

**Example**:
```
Audio Effect 0:
  Name: "Hammering"
  Audio Clip: hammer_sound
  Start Time: 0.1
  Duration: 0 (full clip)
  Loop: true
  Volume: 0.7
  Spatial Blend: 1.0 (3D)

Audio Effect 1:
  Name: "Completion Fanfare"
  Audio Clip: complete_sound
  Start Time: 0.95
  Duration: 0
  Loop: false
  Volume: 1.0
```

### Step 7: Setup Floating Numbers (Optional)

1. Create a **Floating Number Prefab**:
   - Create a GameObject with **TextMeshPro** component
   - Add the **FloatingText** component
   - Configure animation settings in FloatingText
   - Save as prefab

2. In **BuildingLODProgression**, configure **Floating Numbers**:
   - **Show Floating Numbers**: Enable
   - **Floating Number Interval**: Time between spawns (default 1s)
   - **Floating Number Prefab**: Your floating text prefab
   - **Floating Number Offset**: Position offset (default Y+2)

### Step 8: Configure Progress Bar Settings

In **BuildingLODProgression**, under **Progress Bar Settings**:

- **Show Construction Progress Bar**: Enable to show progress during construction
- **Construction Bar Color**: Blue color (default: rgba(0.2, 0.5, 1, 0.8))
- **Show Health Bar**: Enable to show HP after construction
- **Health Bar Color**: Green for healthy (default: rgba(0, 1, 0, 0.8))
- **Low Health Bar Color**: Red for low HP (default: rgba(1, 0, 0, 0.8))
- **Fill Right To Left**: Enable for construction (fills from right to left)

### Step 9: Setup Building Detail UI

The BuildingDetailsUI has been enhanced to show construction progress:

1. In your BuildingDetailsUI prefab, add a **Construction Progress Panel**:
   - Create a new panel GameObject
   - Add child elements:
     - **Construction Progress Bar**: Image (Filled type)
     - **Construction Progress Text**: TextMeshPro ("Construction: X%")
     - **Construction Time Remaining Text**: TextMeshPro ("Time Remaining: Xs")

2. Assign references in **BuildingDetailsUI** component:
   - **Construction Progress Panel**: The panel GameObject
   - **Construction Progress Bar**: The fill image
   - **Construction Progress Text**: The percentage text
   - **Construction Time Remaining Text**: The time text

---

## How It Works

### LOD Progression

The system calculates which LOD to display based on construction progress:

```
Progress (0-1) → LOD Level
0.00 (0%)      → LOD 7 (base)
0.14 (14%)     → LOD 6
0.28 (28%)     → LOD 5
0.42 (42%)     → LOD 4
0.57 (57%)     → LOD 3
0.71 (71%)     → LOD 2
0.85 (85%)     → LOD 1
1.00 (100%)    → LOD 0 (complete)
```

**Example**: A building with 7 second construction time:
- At 0s: LOD 7 (base)
- At 1s: LOD 6
- At 2s: LOD 5
- At 3s: LOD 4
- At 4s: LOD 3
- At 5s: LOD 2
- At 6s: LOD 1
- At 7s: LOD 0 (complete)

### Particle Effect Timing

**Normalized Timing (0-1)**:
- `startTime = 0.0`: Starts immediately
- `startTime = 0.5`: Starts at 50% construction
- `startTime = 1.0`: Starts at completion

**Absolute Timing (seconds)**:
- `startTime = 2.0, useAbsoluteTime = true`: Starts after 2 seconds
- Works independently of construction time

**Duration**:
- `duration = 0`: Plays once and stops
- `duration = 3.0`: Plays for 3 seconds from start time
- `loop = true`: Loops continuously while active

### Audio Effect Timing

Works identically to particle effects. Audio sources are automatically created if not assigned.

### Progress Bar Behavior

**During Construction**:
- Color: Blue (`constructionBarColor`)
- Fill Direction: Right to Left
- Shows: Percentage (0-100%)

**After Construction**:
- Color: Green (healthy), Orange (damaged), Red (critical)
- Fill Direction: Left to Right
- Shows: Current HP / Max HP

### UI Integration

When a building under construction is selected:
1. BuildingDetailsUI shows the construction progress panel
2. Progress bar fills from 0% to 100%
3. Time remaining counts down
4. Upon completion, panel switches to show training options (if applicable)

---

## Advanced Features

### Smooth Transitions

Enable **Smooth Transition** for crossfade effects between LODs:
- Current LOD fades out
- Next LOD fades in
- Duration controlled by **Transition Duration**

### LOD-Specific Particles

Attach particles to specific LODs:
```
Particle Effect:
  Attach To LOD: 4
```
This particle will only play when LOD 4 is active.

### Dynamic Control

Use public methods to control the system at runtime:

```csharp
BuildingLODProgression lodSystem = building.GetComponent<BuildingLODProgression>();

// Enable/disable health bar
lodSystem.EnableHealthBar(true);

// Enable/disable progress bar
lodSystem.EnableProgressBar(false);

// Check construction status
if (lodSystem.IsConstructionComplete)
{
    // Building is complete
}

// Get current LOD
int currentLOD = lodSystem.CurrentLOD;

// Get construction progress
float progress = lodSystem.ConstructionProgress;
```

---

## Performance Tips

1. **LOD Meshes**: Keep poly counts appropriate for each LOD level
2. **Particle Effects**: Use `duration` to limit particle lifetime
3. **Audio Effects**: Don't use too many simultaneous sounds
4. **Floating Numbers**: Adjust `floatingNumberInterval` for performance
5. **Smooth Transitions**: Disable if you need better performance

---

## Troubleshooting

### LODs not switching
- Ensure all LOD meshes are assigned in the correct array order
- Check that the Building component has `requiresConstruction = true`
- Verify that `constructionTime` is set correctly

### Particles not playing
- Check that particle systems are assigned
- Verify start time is within construction duration
- Ensure particles are not attached to inactive LODs

### Audio not playing
- Check that audio clips are assigned
- Verify `spatialBlend` settings (1.0 for 3D sound)
- Ensure Audio Listener exists in scene

### Progress bar not showing
- Verify UI references are assigned
- Check that world canvas is enabled
- Ensure camera is assigned to world canvas

### Floating numbers not spawning
- Check that prefab is assigned
- Verify prefab has FloatingText component
- Ensure TextMeshPro is set up correctly

---

## Example Configurations

### Simple Building (Quick Construction)
```
Construction Time: 3 seconds

LOD Progression: Smooth (0.2s transitions)
Particle Effects: 1 (dust cloud, full duration)
Audio Effects: 1 (hammering, looped)
Floating Numbers: Enabled (1s interval)
Progress Bar: Enabled
Health Bar: Enabled
```

### Complex Building (Long Construction)
```
Construction Time: 15 seconds

LOD Progression: Smooth (0.3s transitions)
Particle Effects:
  - Foundation dust (LOD 7, 0-3s)
  - Scaffolding sparks (LOD 4-2, 5-10s)
  - Completion effect (LOD 0, 14-15s)
Audio Effects:
  - Hammering (0-5s, looped)
  - Sawing (5-10s, looped)
  - Stone placing (10-15s, looped)
  - Completion fanfare (14-15s, once)
Floating Numbers: Enabled (2s interval)
Progress Bar: Enabled
Health Bar: Enabled
```

### Minimal Setup (No Effects)
```
Construction Time: 5 seconds

LOD Progression: Instant (no smooth transition)
Particle Effects: None
Audio Effects: None
Floating Numbers: Disabled
Progress Bar: Enabled
Health Bar: Disabled
```

---

## Integration with Existing Systems

This system integrates seamlessly with:

✅ **Building.cs** - Reads construction progress automatically
✅ **BuildingHealth.cs** - Displays HP after construction
✅ **BuildingDetailsUI.cs** - Shows construction details when selected
✅ **BaseConstructionVisual.cs** - Extends the base construction visual system
✅ **BuildingManager.cs** - Works with existing placement system
✅ **Event System** - Responds to BuildingSelectedEvent, etc.

No modifications to existing building systems are required!

---

## Summary

The Building LOD Progression system provides a complete solution for visualizing building construction with:
- ✅ 8-stage LOD progression (LOD 7 → LOD 0)
- ✅ Array-based particle effects with individual timing
- ✅ Array-based audio effects with individual timing
- ✅ Floating progress numbers
- ✅ Blue construction progress bar (fills right-to-left)
- ✅ Green health bar after construction
- ✅ Integration with Building Details UI
- ✅ Optional features (all can be disabled)
- ✅ Performance optimized
- ✅ Easy to configure per building

Enjoy building! 🏗️
