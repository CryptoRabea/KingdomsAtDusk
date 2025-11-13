# Wall System - Quick Start

Get your modular wall system up and running in 5 minutes!

## Step 1: Create Wall Prefab (Automatic)

1. In Unity, go to **Tools > RTS > Setup Wall Prefab**
2. Click **"Create New Wall Prefab GameObject"**
3. Enable **"Auto-Create Test Variants"** (checked by default)
4. Adjust dimensions if needed (default: 2m high, 1m wide, 0.2m thick)
5. Click **"Setup Wall Prefab"** button
6. Save as prefab in your Project window

**Done!** You now have a wall prefab with all 16 connection states.

## Step 2: Create Wall Data

1. Right-click in Project → **Create > RTS > BuildingData**
2. Name it **"StoneWall"**
3. Set these values:
   - Building Name: `Stone Wall`
   - Building Type: **Defensive**
   - Building Prefab: *drag your wall prefab here*
   - Stone Cost: `10`
   - Construction Time: `3`
4. Save

## Step 3: Add to Building Manager

1. Find **BuildingManager** in your scene
2. In Inspector, expand **Building Data Array**
3. Increase size by 1
4. Drag your **StoneWall** data into the new slot

## Step 4: Test It!

1. Play the scene
2. Select the wall from the building menu (or use BuildingManager's test button)
3. Place walls next to each other
4. Watch them automatically connect!

## Visual Guide

### Connection Examples

```
Single Wall:        Straight Line:      Corner:         T-Junction:     4-Way:
     ╨                  ║                 ╚═               ╠═              ╬
```

### Color Coding (Test Variants)

- **Gray**: No connections (isolated)
- **Brown**: 1 connection (end piece)
- **Green**: 2 connections (straight or corner)
- **Blue**: 3 connections (T-junction)
- **Orange**: 4 connections (intersection)

## Troubleshooting

**Walls not connecting?**
- Make sure grid size in WallConnectionSystem matches BuildingManager (default: 1)
- Ensure walls are placed on grid points (they should snap)

**Wrong mesh showing?**
- Check Inspector: all 16 variants should be assigned in order

**Can't place walls?**
- Ensure Building component is on the prefab
- Check resource costs in BuildingDataSO

## Next Steps

1. **Replace test meshes**: Create custom 3D models for each variant
2. **Add materials**: Stone textures, weathering, etc.
3. **Add details**: Torches, banners, gate segments
4. **Create variants**: Wood walls, metal walls, etc.

## Full Documentation

See `WALL_SYSTEM_GUIDE.md` for complete documentation including:
- Architecture details
- API reference
- Advanced customization
- Performance optimization

---

**Need Help?** Check the full guide or inspect the WallConnectionSystem component in Unity - the custom editor shows connection diagrams and debug info!
