# Fog of War System

A professional fog of war system for Kingdoms at Dusk that provides real-time vision tracking for both the game view and minimap.

## Features

- **Three Vision States**: Unexplored (black), Explored (dark/grey), and Visible (full color)
- **Grid-Based Tracking**: Efficient cell-based vision calculation
- **Automatic Integration**: Auto-adds components to newly spawned units and buildings
- **Game View Rendering**: Mesh-based overlay with shader support
- **Minimap Integration**: Real-time fog overlay on minimap
- **Entity Visibility Control**: Automatically hides enemy units/buildings in fog
- **Performance Optimized**: Chunked updates, dirty cell tracking, spatial optimization
- **Team-Based**: Supports multiple players/teams
- **Configurable**: Extensive configuration options via ScriptableObject-like settings
- **Render Pipeline Support**: Works with both Built-in RP and URP

## ⚠️ Important: Render Pipeline

This system supports both **Built-in RP** and **URP (Universal Render Pipeline)**:

- **For URP projects**: See **[URP_FOG_OF_WAR_SETUP.md](../../../../URP_FOG_OF_WAR_SETUP.md)** for complete URP setup guide
- **For Built-in RP projects**: Continue with this README

**How to check your render pipeline:**
1. **Edit → Project Settings → Graphics**
2. Check **Scriptable Render Pipeline Settings**
3. If it shows a URP asset → **Use URP guide**
4. If it's "None" → Use this guide (Built-in RP)

---

## Quick Start

### Method 1: Using the Setup Tool (Recommended for Built-in RP)

1. Go to `Kingdoms at Dusk > Fog of War > Setup Tool` in the Unity menu
2. Click "Create Fog of War Manager in Scene"
3. Click "Add Vision Providers to Existing Entities"
4. Click "Add Visibility Control to Enemies"
5. Click "Setup Minimap Fog of War"
6. Done! The fog of war system is now active

### Method 2: Manual Setup (Recommended for New Projects)

#### Step 1: Setup Camera Dimming Effect

1. Select your main camera in the scene
2. Add the `FogOfWarCameraEffect` component
3. Create a new material:
   - Right-click in Project window → Create → Material
   - Name it "FogOfWarCameraEffectMaterial"
   - Set shader to `KingdomsAtDusk/FogOfWarCameraEffect`
4. Assign this material to the `FogOfWarCameraEffect` component
5. Adjust `Dim Strength` slider (0.7 is a good default)

#### Step 2: Create Fog of War Manager

1. Create an empty GameObject in your scene named "FogOfWarManager"
2. Add the `FogOfWarManager` component
3. Create a child object for minimap (optional):
   - **MinimapFogRenderer**: Add `FogOfWarMinimapRenderer`
4. Assign the minimap renderer reference to the FogOfWarManager

**Note:** The legacy `FogOfWarRenderer` (mesh-based) is no longer recommended. Use `FogOfWarCameraEffect` instead for better camera compatibility.

#### Step 3: Configure Settings

In the FogOfWarManager component, configure:
- **Cell Size**: Size of each grid cell (default: 2 units)
- **World Bounds**: The playable area bounds
- **Vision Settings**: Default vision radii
- **Visual Settings**: Colors for unexplored/explored areas (not used by camera effect)
- **Performance**: Update intervals and optimization settings

#### Step 4: Add Vision Providers

Add the `VisionProvider` component to:
- All units (friendly and enemy)
- All buildings

The component will automatically:
- Detect vision radius from UnitConfigSO
- Register with the FogOfWarManager
- Determine ownership from MinimapEntity

#### Step 5: Add Visibility Control

Add the `FogOfWarEntityVisibility` component to:
- Enemy units (to hide them in fog)
- Enemy buildings (optional)

Set `isPlayerOwned` to false for enemies.

#### Step 6: Setup Minimap Fog (Optional)

1. Find your minimap UI element
2. Add a child RawImage named "FogOverlay"
3. Stretch it to fill the minimap (anchor to edges)
4. Assign this RawImage to the `FogOfWarMinimapRenderer` component

#### Step 7: Add Auto-Integrator (Optional but Recommended)

Add the `FogOfWarAutoIntegrator` component to the FogOfWarManager GameObject to automatically add vision components to newly spawned units and buildings.

## Component Reference

### FogOfWarManager

The main manager that coordinates the entire fog of war system.

**Key Methods:**
- `RegisterVisionProvider(IVisionProvider)`: Register a vision provider
- `UnregisterVisionProvider(IVisionProvider)`: Unregister a vision provider
- `IsVisible(Vector3)`: Check if a position is visible
- `IsExplored(Vector3)`: Check if a position is explored
- `GetVisionState(Vector3)`: Get the vision state at a position
- `ForceUpdate()`: Force immediate vision update
- `RevealAll()`: Reveal entire map (debug/cheat)
- `HideAll()`: Hide entire map (reset)

### FogOfWarGrid

Grid-based tracking system for vision states.

**Key Methods:**
- `WorldToGrid(Vector3)`: Convert world position to grid coordinates
- `GridToWorld(Vector2Int)`: Convert grid coordinates to world position
- `GetState(Vector2Int/Vector3)`: Get vision state at position
- `SetState(Vector2Int, VisionState)`: Set vision state at position
- `RevealCircle(Vector3, float)`: Reveal circular area

### VisionProvider

Component that provides vision for entities.

**Configuration:**
- `visionRadius`: How far this entity can see
- `isActive`: Whether vision is currently active
- `ownerId`: Owner/team ID (0 = player)
- `autoDetectRadius`: Auto-detect from UnitConfigSO
- `useUnitDetectionRange`: Use unit detection range as vision

**Key Methods:**
- `SetVisionRadius(float)`: Set vision radius
- `SetActive(bool)`: Enable/disable vision
- `SetOwnerId(int)`: Set owner ID

### FogOfWarEntityVisibility

Controls entity visibility based on fog state.

**Configuration:**
- `isPlayerOwned`: Is this a player-owned entity?
- `updateInterval`: How often to check visibility (performance)
- `hideInExplored`: Hide in explored (dark) areas?

**Key Methods:**
- `SetPlayerOwned(bool)`: Set player ownership
- `AddRenderer(Renderer)`: Add renderer to control
- `AddCanvas(Canvas)`: Add canvas to control

### FogOfWarRenderer

Renders fog overlay on the game view using a mesh (legacy approach).

**Configuration:**
- `fogMaterial`: Material with FogOfWar shader
- `fogHeight`: Height of fog plane
- `chunksPerUpdate`: Performance tuning

**Note:** For better camera dimming that works with camera rotation and movement, use `FogOfWarCameraEffect` instead.

### FogOfWarCameraEffect (Recommended)

Camera-based post-processing effect that dims non-visible areas. Attaches directly to the camera and uses depth-based world position reconstruction.

**Configuration:**
- `fogManager`: Reference to FogOfWarManager (auto-detected if null)
- `fogEffectMaterial`: Material using FogOfWarCameraEffect shader
- `enableEffect`: Enable/disable the dimming effect
- `dimStrength`: Strength of dimming (0-1, default: 0.7)

**Advantages over FogOfWarRenderer:**
- Properly follows camera rotation and movement
- Works with any camera angle (not just top-down)
- Uses screen-space rendering for consistent coverage
- Depth-based world position reconstruction
- Better performance for dynamic cameras

**Setup:**
1. Add `FogOfWarCameraEffect` component to your main camera
2. Create a material using the `KingdomsAtDusk/FogOfWarCameraEffect` shader
3. Assign the material to the component
4. The fog manager will be auto-detected
5. Adjust `dimStrength` to control how dark unexplored areas appear

### FogOfWarMinimapRenderer

Renders fog overlay on the minimap.

**Configuration:**
- `fogOverlay`: RawImage UI element for fog
- `enableMinimapFog`: Enable/disable minimap fog
- `textureSize`: Resolution of fog texture (default: 512)

### FogOfWarAutoIntegrator

Automatically adds fog of war components to newly spawned entities.

**Configuration:**
- `autoAddVisionToUnits`: Auto-add VisionProvider to units
- `autoAddVisionToBuildings`: Auto-add VisionProvider to buildings
- `autoAddVisibilityControl`: Auto-add FogOfWarEntityVisibility
- `defaultUnitVision`: Default vision for units
- `defaultBuildingVision`: Default vision for buildings

## IVisionProvider Interface

Implement this interface for custom vision providers:

```csharp
public interface IVisionProvider
{
    Vector3 Position { get; }
    float VisionRadius { get; }
    bool IsActive { get; }
    int OwnerId { get; }
    GameObject GameObject { get; }
}
```

## Vision States

```csharp
public enum VisionState
{
    Unexplored = 0,  // Never seen (black)
    Explored = 1,    // Previously seen (dark/grey)
    Visible = 2      // Currently visible (full color)
}
```

## Configuration

### FogOfWarConfig

All fog of war settings are in the `FogOfWarConfig` class:

**Grid Settings:**
- `cellSize`: Size of each grid cell (smaller = more precise, higher cost)
- `worldBounds`: The playable world area

**Vision Settings:**
- `defaultVisionRadius`: Default vision for entities without specific radius
- `buildingVisionMultiplier`: Multiplier for building vision
- `updateInterval`: How often to update fog (seconds)

**Visual Settings:**
- `unexploredColor`: Color for unexplored areas (default: black)
- `exploredColor`: Color for explored areas (default: dark grey)
- `fadeSpeed`: Transition speed between states

**Performance:**
- `maxCellUpdatesPerFrame`: Limit cell updates per frame
- `enableDebugVisualization`: Show debug visualization in Scene view

## Performance Considerations

### Optimization Tips

1. **Grid Cell Size**: Larger cells = better performance, less precision
   - Recommended: 2-5 units for most games
   - Small maps: Can use smaller cells (1-2)
   - Large maps: Use larger cells (5-10)

2. **Update Interval**: Don't update every frame
   - Recommended: 0.1 - 0.2 seconds
   - Fast-paced: 0.05 - 0.1
   - Slow-paced: 0.2 - 0.5

3. **Max Cell Updates**: Limit updates per frame
   - Default: 500 cells/frame
   - High-end: 1000-2000
   - Low-end: 200-500

4. **Visibility Update Interval**: For FogOfWarEntityVisibility
   - Default: 0.2 seconds
   - Can increase to 0.5 for less critical entities

5. **Texture Resolution**: For minimap fog
   - Default: 512x512
   - Can reduce to 256x256 for better performance
   - Can increase to 1024x1024 for better quality

### Performance Benchmarks

On a typical RTS map (2000x2000 units):
- **Grid Size**: 1000x1000 cells at 2 units/cell
- **Memory**: ~2-3 MB for grid data
- **CPU**: <1ms per frame for 50 vision providers
- **Update Cost**: ~2-3ms every 0.1s during vision updates

## Debugging

### Debug Features

1. **Debug Visualization**: Enable `enableDebugVisualization` in config
   - Shows grid cells in Scene view
   - Color-coded: Black (unexplored), Grey (explored), Green (visible)

2. **Gizmos**: VisionProvider shows vision radius when selected

3. **Console Logs**: Enable debug logging for:
   - Vision provider registration
   - Grid initialization
   - Vision updates

### Common Issues

**Units not revealing fog:**
- Check if VisionProvider component is added
- Verify ownerId matches local player (default: 0)
- Ensure isActive is true
- Check if vision radius is > 0

**Fog not rendering:**
- Verify FogRenderer has material assigned
- Check if shader "KingdomsAtDusk/FogOfWar" exists
- Ensure fogHeight is above terrain
- Check if mesh renderer is enabled

**Minimap fog not showing:**
- Verify fogOverlay RawImage is assigned
- Check if texture is being created
- Ensure enableMinimapFog is true
- Verify RawImage is above other minimap elements

**Enemies not hiding:**
- Add FogOfWarEntityVisibility component
- Set isPlayerOwned to false
- Check if renderers are being detected
- Verify update interval isn't too high

## Advanced Usage

### Custom Vision Providers

Create custom vision providers by implementing `IVisionProvider`:

```csharp
public class CustomVisionProvider : MonoBehaviour, IVisionProvider
{
    public Vector3 Position => transform.position;
    public float VisionRadius => 25f;
    public bool IsActive => gameObject.activeInHierarchy;
    public int OwnerId => 0;
    public GameObject GameObject => gameObject;
}
```

### Manual Vision Control

```csharp
// Get vision state
VisionState state = FogOfWarManager.Instance.GetVisionState(position);

// Check visibility
bool isVisible = FogOfWarManager.Instance.IsVisible(position);

// Reveal area manually
FogOfWarManager.Instance.Grid.RevealCircle(position, radius);

// Force update
FogOfWarManager.Instance.ForceUpdate();
```

### Dynamic Vision Radius

```csharp
var visionProvider = GetComponent<VisionProvider>();
visionProvider.SetVisionRadius(newRadius);
```

### Team-Based Fog of War

```csharp
// Set different owner IDs for different teams
visionProvider.SetOwnerId(teamId); // 0 = player, 1+ = other teams

// Configure manager for specific team
FogOfWarManager.Instance.localPlayerId = myTeamId;
```

## Shader Details

The fog of war uses a custom shader: `KingdomsAtDusk/FogOfWar`

**Features:**
- Transparent rendering
- Texture-based fog mapping
- Tint color support
- Unity fog integration
- Optimized for mobile

**Properties:**
- `_FogTex`: The fog texture (auto-generated)
- `_Color`: Tint color (default: white)

## File Structure

```
Assets/Scripts/FogOfWar/
├── FogOfWarManager.cs           # Main manager
├── FogOfWarGrid.cs              # Grid tracking system
├── FogOfWarRenderer.cs          # Game view renderer (legacy mesh-based)
├── FogOfWarCameraEffect.cs      # Camera effect renderer (recommended)
├── FogOfWarMinimapRenderer.cs   # Minimap renderer
├── FogOfWarEnums.cs             # Enums and config
├── IVisionProvider.cs           # Vision provider interface
├── VisionProvider.cs            # Vision provider component
├── FogOfWarEntityVisibility.cs  # Entity visibility control
├── FogOfWarAutoIntegrator.cs    # Auto-integration
├── Editor/
│   └── FogOfWarSetupTool.cs    # Editor setup tool
└── README.md                    # This file

Assets/Shaders/
├── FogOfWar.shader              # Fog of war shader (legacy)
└── FogOfWarCameraEffect.shader  # Camera effect shader (recommended)
```

## Future Enhancements

Potential improvements for future versions:

1. **Line of Sight**: Proper line-of-sight calculations with obstacle blocking
2. **Height-Based Vision**: Different vision on different terrain heights
3. **Vision Sharing**: Share vision between allied units
4. **Fog Textures**: Use textures for more interesting fog patterns
5. **Minimap Icons**: Show last-seen positions of enemies
6. **Replay Support**: Record and replay fog of war states
7. **Networking**: Multiplayer fog of war synchronization

## Credits

Developed for Kingdoms at Dusk RTS game.

## License

Part of the Kingdoms at Dusk project.
