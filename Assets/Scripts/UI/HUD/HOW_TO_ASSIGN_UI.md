# How to Assign Your UI Components to MainHUDFramework

## THE PROBLEM

You created the configuration and layout, but **your UI isn't moving** because the MainHUDFramework doesn't know which UI elements to reposition!

## THE SOLUTION

You need to **drag your existing UI GameObjects** into the MainHUDFramework component in the Inspector.

## STEP-BY-STEP

### 1. Find Your Existing UI in the Scene

First, let's locate your current UI. In your **Hierarchy**, look for:

```
Canvas
├── Minimap (or MiniMapController, MinimapPanel, etc.)
├── UnitDetails (or UnitDetailsUI, UnitInfoPanel, etc.)
├── BuildingDetails (or BuildingDetailsUI, BuildingInfoPanel, etc.)
├── BuildingHUD (or BuildingMenu, ConstructionMenu, etc.)
├── ResourcePanel (or ResourceUI, ResourceDisplay, etc.)
└── ... other UI elements
```

**Write down** what your UI elements are called. For example:
- Minimap → "MinimapPanel"
- Unit Details → "UnitDetailsUI"
- Building Details → "BuildingDetailsUI"
- etc.

### 2. Select HUDFramework in Hierarchy

Click on the **HUDFramework** GameObject (the one with MainHUDFramework component)

### 3. Look at the Inspector

You'll see something like this:

```
┌─────────────────────────────────────────────┐
│ Main HUD Framework (Script)                 │
├─────────────────────────────────────────────┤
│ Configuration                               │
│   ☑ DefaultHUDConfig                        │
│                                             │
│ Canvas                                      │
│   Main Canvas: [None (Canvas)]              │  ← Drag your Canvas here
│   Canvas Scaler: [None (Canvas Scaler)]     │
│                                             │
│ Core Components                             │
│   Minimap Panel: [None (GameObject)]        │  ← Drag your minimap here!
│   Unit Details UI: [None (UnitDetailsUI)]   │  ← Drag your unit details here!
│   Building Details UI: [None ...]           │  ← Drag your building details here!
│   Building HUD: [None (BuildingHUD)]        │  ← Drag your building HUD here!
│                                             │
│ Optional Components                         │
│   Top Bar UI: [None (TopBarUI)]             │
│   Inventory UI: [None (InventoryUI)]        │
│   Resource UI: [None (ResourceUI)]          │  ← Drag your resource panel here!
│   Happiness UI: [None (HappinessUI)]        │  ← Drag your happiness UI here!
│   Notification UI: [None (NotificationUI)]  │  ← Drag your notifications here!
│   Wall Preview UI: [None ...]               │
│                                             │
│ Cursor                                      │
│   Cursor State Manager: [None ...]          │
└─────────────────────────────────────────────┘
```

### 4. Drag Your UI Elements

Now, **drag each UI element from your Hierarchy** into the corresponding field:

**Example:**
1. Find "MinimapPanel" in Hierarchy
2. **Click and drag** it to the "Minimap Panel" field in Inspector
3. **Release** - you should see it change from "None" to "MinimapPanel"

**Repeat for all your UI elements:**
- Minimap → Minimap Panel field
- UnitDetailsUI → Unit Details UI field
- BuildingDetailsUI → Building Details UI field
- BuildingHUD → Building HUD field
- ResourceUI → Resource UI field
- HappinessUI → Happiness UI field
- NotificationUI → Notification UI field

### 5. Assign Canvas References

At the top, also assign:
- **Main Canvas**: Drag your Canvas GameObject
- **Canvas Scaler**: Drag the CanvasScaler component from your Canvas

### 6. Press Play and Check Console

**Press Play** ▶️

**Look at Console**. You should now see:

```
MainHUDFramework: Initializing HUD...
MainHUDFramework: Registered Minimap for layout          ← Good!
MainHUDFramework: Registered UnitDetails for layout      ← Good!
MainHUDFramework: Registered BuildingDetails for layout  ← Good!
MainHUDFramework: Registered ResourcePanel for layout    ← Good!
MainHUDFramework: Registered 4 UI components for layout management
MainHUDFramework: Applying layout preset 'DefaultLayout'
MainHUDFramework: Applied layout to MinimapPanel - Anchor: BottomLeft, Size: (200, 200), Offset: (10, 10)
MainHUDFramework: Applied layout to UnitDetailsUI - Anchor: BottomCenter, Size: (400, 150), Offset: (0, 10)
... etc.
```

**If you see**: "NO UI COMPONENTS ASSIGNED!" → You didn't drag anything, go back to step 4!

## VISUAL EXAMPLE

### Before Assignment (Won't Work):
```
Inspector (HUDFramework selected):
  Minimap Panel: [None (GameObject)]     ❌ Empty!
  Unit Details UI: [None]                ❌ Empty!
  Building HUD: [None]                   ❌ Empty!

Console when you play:
  ⚠️ MainHUDFramework: NO UI COMPONENTS ASSIGNED!
```

### After Assignment (Works!):
```
Inspector (HUDFramework selected):
  Minimap Panel: ☑ MinimapPanel          ✅ Assigned!
  Unit Details UI: ☑ UnitDetailsUI       ✅ Assigned!
  Building HUD: ☑ BuildingHUD            ✅ Assigned!

Console when you play:
  ✅ MainHUDFramework: Registered 3 UI components
  ✅ Applied layout to MinimapPanel
  ✅ Applied layout to UnitDetailsUI
```

## COMMON MISTAKES

### ❌ Mistake 1: Assigning the wrong component
**Wrong**: Dragging the Transform or RectTransform component
**Right**: Drag the **GameObject itself** from Hierarchy

### ❌ Mistake 2: Assigning from Project instead of Hierarchy
**Wrong**: Dragging a prefab from Project window
**Right**: Drag the **instance in your scene** from Hierarchy

### ❌ Mistake 3: Not assigning anything
**Wrong**: Leaving all fields as "None"
**Right**: Drag your UI elements into the fields!

## WHAT IF I DON'T HAVE SOME UI COMPONENTS?

**That's totally fine!** Just leave those fields empty.

For example, if you don't have:
- Inventory → Leave "Inventory UI" field empty
- Top Bar → Leave "Top Bar UI" field empty
- Minimap → Leave "Minimap Panel" field empty

The framework will only reposition the UI elements you **actually assign**.

## QUICK TEST

Want to test with just ONE element?

1. Assign **only** your Minimap to "Minimap Panel" field
2. Leave everything else empty
3. Press Play
4. Console should show: "Registered 1 UI components"
5. Your minimap should move to bottom-left corner (default position)
6. **If it moved** → It works! Now assign your other UI elements
7. **If it didn't move** → Check that you assigned the GameObject, not a component

## WHAT HAPPENS WHEN IT WORKS?

When you press Play, the MainHUDFramework will:
1. ✅ Read your layout preset
2. ✅ Find all assigned UI elements
3. ✅ Move them to the positions defined in the layout
4. ✅ Resize them to the sizes defined in the layout
5. ✅ Set their anchors correctly

**Your UI will automatically arrange itself!**

## NEXT STEPS

Once your UI is repositioning correctly:

1. **Customize the layout** - Edit the layout preset values in Inspector
2. **Create multiple layouts** - Make layouts for different screen sizes
3. **Enable optional features** - Try TopBar and Inventory
4. **Add hotkeys** - Use HUDController for runtime control

---

**Still not working?** Check the Console for the warning message. If you see "NO UI COMPONENTS ASSIGNED", you need to go back and drag your UI elements into the MainHUDFramework Inspector fields!
