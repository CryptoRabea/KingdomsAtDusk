# 🎯 Auto-Fit Layout Container

**Universal UI layout constraint system for Unity**
Never let your UI overflow again! Works with any layout type and any container shape.

---

## ✨ Features

- **✅ Universal**: Works with Grid, Horizontal, Vertical, or manual layouts
- **✅ Multi-Shape**: Square, Rectangle, Circle, Triangle, Custom
- **✅ Auto-Scaling**: Automatically scales contents to fit perfectly
- **✅ Zero Overflow**: Contents never exceed container bounds
- **✅ Editor Integration**: Visual tools and custom inspector
- **✅ Reusable Package**: Copy to any Unity project
- **✅ Performance**: Minimal overhead, smart caching

---

## 🚀 Quick Start

### Method 1: Create New Container

1. In Unity Editor: `GameObject > UI > Auto-Fit Layout Container`
2. Drag UI elements into the container
3. Done! Contents automatically fit

### Method 2: Add to Existing Container

1. Right-click on any UI object with `RectTransform`
2. Select `Add Auto-Fit Layout Container`
3. Configure shape and size in Inspector
4. Done!

---

## 📦 Package Contents

```
AutoFitLayout/
├── AutoFitLayoutContainer.cs          # Main component
├── Editor/
│   └── AutoFitLayoutContainerEditor.cs # Custom inspector & tools
└── README.md                           # This file
```

---

## 🎨 Supported Shapes

### Square
- Perfect square container
- Ideal for: Unit grids, ability icons, inventory slots
- Width = Height automatically

### Rectangle
- Custom width × height
- Ideal for: Toolbars, item lists, skill bars
- Full control over aspect ratio

### Circle
- Circular boundary
- Ideal for: Radial menus, circular displays
- Uses inscribed square for content (adjustable)

### Triangle
- Triangular boundary
- Ideal for: Warning displays, directional indicators
- Configurable aspect ratio

### Custom
- User-defined shape
- Maximum flexibility

---

## 🔧 Inspector Settings

### Container Settings
- **Shape**: Select container shape (Square, Rectangle, Circle, Triangle)
- **Max Size/Width/Height**: Maximum dimensions in pixels
- **Padding**: Inner padding around contents

### Content Scaling
- **Min Content Size**: Minimum size for elements (prevents tiny icons)
- **Max Content Size**: Maximum size for elements (prevents huge icons)
- **Auto-Detect Layout**: Automatically find layout component

### Shape-Specific
- **Circle Inscribe Factor**: How much to reduce usable area (0.707 = inscribed square)
- **Triangle Aspect Ratio**: Height relative to width (0.866 = equilateral)

### Advanced
- **Update in Edit Mode**: Live preview in editor
- **Debug Mode**: Show calculation logs

---

## 💡 Usage Examples

### Example 1: Square Unit Selection Grid

```csharp
// Already set up in scene
GameObject container = GameObject.Find("UnitSelectionContainer");
AutoFitLayoutContainer autoFit = container.GetComponent<AutoFitLayoutContainer>();

// Change to square, 300x300px
autoFit.SetShape(AutoFitLayoutContainer.ContainerShape.Square);
autoFit.SetMaxSize(300f, 300f);
autoFit.SetPadding(10f);
```

### Example 2: Circular Radial Menu

```csharp
AutoFitLayoutContainer radialMenu = GetComponent<AutoFitLayoutContainer>();
radialMenu.SetShape(AutoFitLayoutContainer.ContainerShape.Circle);
radialMenu.SetMaxSize(250f, 250f);
// Contents will fit within inscribed square
```

### Example 3: Rectangular Toolbar

```csharp
AutoFitLayoutContainer toolbar = GetComponent<AutoFitLayoutContainer>();
toolbar.SetShape(AutoFitLayoutContainer.ContainerShape.Rectangle);
toolbar.SetMaxSize(800f, 60f); // Wide toolbar
// Horizontal layout automatically adjusts
```

---

## 🎮 Works With All Layout Types

### GridLayoutGroup
- Automatically calculates optimal grid dimensions (columns × rows)
- Scales cell size to fit within container
- Maintains square cells

### HorizontalLayoutGroup
- Divides width equally among children
- Adds `LayoutElement` components automatically
- Respects spacing

### VerticalLayoutGroup
- Divides height equally among children
- Adds `LayoutElement` components automatically
- Respects spacing

### No Layout (Manual)
- Arranges children in grid pattern
- Sets positions and sizes automatically
- Fallback when no layout group present

---

## 📋 How It Works

1. **Detects** container shape and size
2. **Calculates** usable area (accounting for padding and shape)
3. **Identifies** layout type (Grid, Horizontal, Vertical, or Manual)
4. **Computes** optimal content sizes
5. **Applies** sizing to layout components
6. **Updates** continuously or on-demand

---

## 🔄 Exporting to Other Projects

### Option 1: Copy Folder
1. Copy the entire `AutoFitLayout/` folder
2. Paste into your new project's `Assets/` folder
3. Ready to use!

### Option 2: Unity Package
1. Select `AutoFitLayout/` folder in Project window
2. Right-click → `Export Package...`
3. Save as `AutoFitLayout.unitypackage`
4. Import into other projects

### Option 3: Git Submodule (Advanced)
```bash
git submodule add <repo-url> Assets/AutoFitLayout
```

---

## ⚙️ API Reference

### Public Methods

```csharp
// Force immediate layout update
void ForceUpdate()

// Change container shape
void SetShape(ContainerShape newShape)

// Set maximum dimensions
void SetMaxSize(float width, float height)

// Set inner padding
void SetPadding(float newPadding)
```

### Public Enums

```csharp
enum ContainerShape
{
    Square,     // Equal width and height
    Rectangle,  // Custom width × height
    Circle,     // Circular boundary
    Triangle,   // Triangular boundary
    Custom      // User-defined
}
```

---

## 🎯 Use Cases

### Game UI
- ✅ Unit selection grids (Square)
- ✅ Inventory systems (Square/Rectangle)
- ✅ Ability bars (Rectangle)
- ✅ Radial menus (Circle)
- ✅ Quest markers (Triangle)

### Menu Systems
- ✅ Options menus (Rectangle)
- ✅ Character selection (Square)
- ✅ Settings panels (Rectangle)
- ✅ Tooltips (Rectangle)

### HUD Elements
- ✅ Minimap icons (Square)
- ✅ Buff/debuff displays (Square)
- ✅ Party members (Rectangle)
- ✅ Notifications (Rectangle)

---

## 🔍 Troubleshooting

### Contents still overflow?
- Check that `AutoFitLayoutContainer` is on the **parent** container
- Ensure `Update in Edit Mode` is enabled for live preview
- Click "Update Layout Now" button in Inspector

### Layout not detecting?
- Enable `Auto-Detect Layout` in Inspector
- Manually add a layout component (Grid, Horizontal, Vertical)
- Check that layout component is on the same GameObject

### Icons too small/large?
- Adjust `Min Content Size` (increases minimum)
- Adjust `Max Content Size` (decreases maximum)
- Reduce `Padding` for more space

### Circle/Triangle not working?
- Adjust `Circle Inscribe Factor` (higher = more space)
- Adjust `Triangle Aspect Ratio` (changes shape)
- Check that shape is selected correctly

---

## 🌟 Advanced Tips

### Tip 1: Nested Containers
You can nest Auto-Fit containers for complex layouts:
```
OuterContainer (Rectangle 800×600)
└── InnerContainer (Square 300×300)
    └── Items (auto-scaled)
```

### Tip 2: Runtime Adjustment
Change settings at runtime:
```csharp
// Expand container when more items added
if (itemCount > 12)
{
    autoFit.SetMaxSize(400f, 400f);
}
```

### Tip 3: Custom Shapes
For custom shapes, use `ContainerShape.Custom` and manually adjust the usable area calculation.

### Tip 4: Performance
- Disable `Update in Edit Mode` if working with many containers
- Use `ForceUpdate()` only when needed
- Cache the component reference

---

## 📝 Version History

### v1.0.0 (Current)
- Initial release
- Support for Square, Rectangle, Circle, Triangle shapes
- Auto-detection for Grid, Horizontal, Vertical layouts
- Custom editor with visual preview
- Package export ready

---

## 🤝 Integration Examples

### With Your Multi-Unit Selection UI

Already integrated! The Multi-Unit Selection UI uses this system:

```csharp
// In MultiUnitSelectionUI.cs
AutoFitLayoutContainer autoFit = GetComponent<AutoFitLayoutContainer>();
autoFit.SetShape(AutoFitLayoutContainer.ContainerShape.Square);
autoFit.SetMaxSize(300f, 300f);
```

### With Other Systems

Works seamlessly with:
- TextMeshPro elements
- Unity UI Image/RawImage
- Custom UI components
- Third-party UI frameworks

---

## 📞 Support

- **Menu**: `Tools > RTS > Create Auto-Fit Layout Package` for package info
- **Context Menu**: Right-click RectTransform → "Add Auto-Fit Layout Container"
- **GameObject Menu**: `GameObject > UI > Auto-Fit Layout Container`

---

## 📜 License

This package is part of the RTS UI system and can be freely used in any Unity project.

---

**Made with ❤️ for Unity developers who hate UI overflow issues!**

🎯 **Never let your UI overflow again!**
