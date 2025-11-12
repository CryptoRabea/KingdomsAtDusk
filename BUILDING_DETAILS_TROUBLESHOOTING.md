# Building Details Panel Not Opening - Troubleshooting Guide

This guide will help you diagnose and fix issues with the Building Details Panel not opening when you click buildings.

## Quick Diagnosis

1. **Add the diagnostic script to your scene:**
   - Create an empty GameObject (e.g., "DebugHelper")
   - Add the `BuildingDetailsDiagnostic` component to it
   - The diagnostics will run automatically on Start, or you can right-click the component and select "Run Full Diagnostics"

2. **Check the Console** for detailed error messages and fixes

## Common Issues & Fixes

### Issue 1: BuildingDetailsUI Missing from Scene

**Symptom:** Console shows "BuildingDetailsUI component NOT found in scene!"

**Fix:**
1. Go to `Tools > RTS > Setup Building Training UI`
2. Assign your Canvas
3. Click "Create Both (Recommended)"
4. This creates:
   - BuildingDetailsPanel (on your Canvas)
   - TrainUnitButton prefab (in Assets/Prefabs/UI/)

### Issue 2: BuildingSelectionManager Missing or Not Configured

**Symptom:** Console shows "BuildingSelectionManager NOT found in scene!"

**Fix:**
1. Create or find your GameManager/InputManager GameObject
2. Add `BuildingSelectionManager` component
3. Configure it:
   - **Click Action:** Assign from your Input Action Asset (e.g., "Player/Click")
   - **Position Action:** Assign from your Input Action Asset (e.g., "Player/Position")
   - **Building Layer:** Set to your "Building" layer
   - **Main Camera:** Will auto-assign to Camera.main

### Issue 3: Buildings Missing Components

**Symptom:** Console shows "building(s) missing BuildingSelectable component!"

**Fix:**
For each building prefab/instance:
1. Add `BuildingSelectable` component
2. Add `Building` component
3. Assign a `BuildingDataSO` to the Building component
4. Add a `Collider` (BoxCollider, MeshCollider, etc.)
5. Set GameObject layer to "Building"

**Quick Setup for a Building:**
```
MyBuilding (GameObject)
├─ Building (component) - with BuildingDataSO assigned
├─ BuildingSelectable (component)
├─ BoxCollider (component) - or any collider
└─ Layer: Building
```

### Issue 4: Building Layer Not Set

**Symptom:** Console shows "buildingLayer is NOT SET (value = 0)!" or buildings on wrong layer

**Fix:**
1. Create a "Building" layer if it doesn't exist:
   - Edit > Project Settings > Tags and Layers
   - Add "Building" to an empty User Layer slot
2. Set all building GameObjects to the "Building" layer
3. Assign the "Building" layer to BuildingSelectionManager's `buildingLayer` field

### Issue 5: Input System Not Configured

**Symptom:** Console shows "New Input System is NOT enabled!"

**Fix:**
1. Go to `Edit > Project Settings > Player`
2. Under "Other Settings" find "Active Input Handling"
3. Set to "Input System Package (New)" or "Both"
4. Unity will ask to restart - click "Yes"

### Issue 6: Input Actions Not Assigned

**Symptom:** Clicks not detected, console shows "clickAction is NULL!"

**Fix:**
1. Make sure you have an Input Action Asset in your project
2. In the BuildingSelectionManager inspector:
   - Click the circle next to "Click Action"
   - Select your Input Action Asset > Player > Click (or similar)
   - Click the circle next to "Position Action"
   - Select your Input Action Asset > Player > Position (or similar)

### Issue 7: Buildings Have No Collider

**Symptom:** Console shows "building(s) missing Collider!"

**Fix:**
1. Select your building GameObject
2. Add Component > Physics > Box Collider (or Mesh Collider)
3. Adjust the collider size to match your building

### Issue 8: BuildingData Not Assigned

**Symptom:** Console shows "building(s) missing BuildingData!"

**Fix:**
1. Select your building GameObject
2. Find the `Building` component in Inspector
3. Assign a `BuildingDataSO` asset to the "Data" field
4. If you don't have one, create it:
   - Right-click in Project > Create > RTS > Building Data

## Testing After Fixes

1. **Enable Debug Logging:**
   - Select your BuildingSelectionManager
   - Check "Enable Debug Logs"

2. **Add BuildingSelectionDebugger:**
   - Create an empty GameObject called "SelectionDebugger"
   - Add the `BuildingSelectionDebugger` component
   - This will log all selection events

3. **Test Selection:**
   - Enter Play Mode
   - Click on a building
   - Check Console for:
     - "BuildingSelectionManager: Click detected at..."
     - "BuildingSelectable found on..."
     - "🟢 BuildingSelectedEvent received!"
     - "Selecting building: ..."

## Event Flow (How It Should Work)

1. **Player clicks** on building
2. **BuildingSelectionManager** detects click via Input System
3. **Raycasts** against Building layer
4. Finds **BuildingSelectable** component
5. Calls **BuildingSelectable.Select()**
6. **BuildingSelectedEvent** is published via EventBus
7. **BuildingDetailsUI** receives event (it's subscribed)
8. **Panel is shown** with building details

If ANY step fails, the panel won't open!

## Quick Setup Checklist

Use this checklist to ensure everything is configured:

- [ ] BuildingDetailsUI exists in scene (on Canvas)
- [ ] BuildingDetailsUI panelRoot is assigned
- [ ] BuildingSelectionManager exists in scene
- [ ] BuildingSelectionManager has Input Actions assigned
- [ ] BuildingSelectionManager has Building Layer assigned
- [ ] Building layer exists in project
- [ ] All buildings have BuildingSelectable component
- [ ] All buildings have Building component with Data assigned
- [ ] All buildings have Collider component
- [ ] All buildings are on the Building layer
- [ ] New Input System is enabled
- [ ] Input Action Asset exists with Click and Position actions

## Still Not Working?

If you've checked everything and it's still not working:

1. **Run the diagnostic tool** again to see what's still wrong
2. **Check the Console** for specific error messages
3. **Enable debug logging** on BuildingSelectionManager
4. **Add BuildingSelectionDebugger** to see event flow
5. **Test with a simple building** - create a cube, add all components, try that first

## Editor Tools Available

- `Tools > RTS > Setup Building Training UI` - Creates BuildingDetailsUI panel
- `Tools > RTS > Setup BuildingHUD` - Creates building placement HUD
- Right-click BuildingDetailsDiagnostic component > "Run Full Diagnostics"
- Right-click BuildingSelectionDebugger component > "Check BuildingDetailsUI in Scene"

## Need More Help?

Check these files for reference:
- `Assets/Scripts/UI/BuildingDetailsUI.cs` - Panel logic
- `Assets/Scripts/RTSBuildingsSystems/BuildingSelectionManager.cs` - Selection logic
- `Assets/Scripts/RTSBuildingsSystems/BuildingSelectable.cs` - Building component
- `Assets/Scripts/Debug/BuildingDetailsDiagnostic.cs` - Diagnostic tool
