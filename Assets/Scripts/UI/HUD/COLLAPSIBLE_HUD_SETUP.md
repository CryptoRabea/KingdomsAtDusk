# Collapsible HUD System Setup Guide

This guide explains how to set up the collapsible HUD buttons for both the main HUD panel and the top panel.

## Overview

The collapsible HUD system provides two components:
- **CollapsibleHUDButton**: Toggles the main HUD panel (bottom) in/out of view
- **CollapsibleTopPanelButton**: Toggles the top panel in/out of view

Both components:
- Cache the initial camera viewport values on start
- Adjust the viewport to full screen (0, 0, 1, 1) when collapsed
- Restore the cached viewport when expanded
- Keep the toggle button visible even when the panel is collapsed
- Support customizable keyboard shortcuts
- Animate the panel sliding smoothly

## Setup Instructions

### 1. Main HUD Panel Setup (Bottom)

1. **Create the Toggle Button**:
   - In your main HUD GameObject, create a UI Button as a child
   - Name it something like "HUD Toggle Button"
   - Position it where you want it to appear (typically bottom of screen)
   - This button will stay visible even when the HUD collapses

2. **Add the Script**:
   - Select your main HUD GameObject
   - Add the `CollapsibleHUDButton` component

3. **Configure the Component**:

   **Panel References:**
   - `HUD Panel`: Drag your main HUD RectTransform here (the panel that contains all UI elements)
   - `Toggle Button`: Drag the button you created in step 1
   - `Button Icon`: Drag the Image component from the button (if you want icon switching)

   **Button Icons:**
   - `Expanded Icon`: The sprite to show when HUD is expanded (e.g., a "<" or down arrow)
   - `Collapsed Icon`: The sprite to show when HUD is collapsed (e.g., a ">" or up arrow)

   **Keyboard Shortcut:**
   - `Toggle Shortcut`: Set the key to toggle the HUD (default: Comma key for "<")
   - Common options:
     - Comma (KeyCode.Comma) for "<"
     - Less (KeyCode.Less) for "<"
     - Any custom key you prefer

   **Camera References:**
   - `Main Camera`: Leave empty to auto-find, or drag your main camera

   **Animation Settings:**
   - `Animation Speed`: How fast the panel slides (default: 5)
   - `Slide Offset`: How far down to slide in pixels (default: 500)

### 2. Top Panel Setup

1. **Create the Toggle Button**:
   - In your top panel GameObject, create a UI Button as a child
   - Name it something like "Top Panel Toggle Button"
   - Position it at the top of screen where you want it visible
   - This button will stay visible even when the top panel collapses

2. **Add the Script**:
   - Select your top panel GameObject (or create a manager GameObject)
   - Add the `CollapsibleTopPanelButton` component

3. **Configure the Component**:

   **Panel References:**
   - `Top Panel`: Drag your top panel RectTransform here
   - `Toggle Button`: Drag the button you created in step 1
   - `Button Icon`: Drag the Image component from the button

   **Button Icons:**
   - `Expanded Icon`: The sprite to show when panel is expanded (e.g., a ">" or up arrow)
   - `Collapsed Icon`: The sprite to show when panel is collapsed (e.g., a "<" or down arrow)

   **Keyboard Shortcut:**
   - `Toggle Shortcut`: Set the key to toggle the panel (default: Period key for ">")
   - Common options:
     - Period (KeyCode.Period) for ">"
     - Greater (KeyCode.Greater) for ">"
     - Any custom key you prefer

   **Camera References:**
   - `Main Camera`: Leave empty to auto-find, or drag your main camera

   **Animation Settings:**
   - `Animation Speed`: How fast the panel slides (default: 5)
   - `Slide Offset`: How far up to slide in pixels (default: 100)

## Key Features

### Viewport Management
- The scripts automatically cache the camera's initial viewport rect on Start()
- When collapsed: viewport is set to `Rect(0, 0, 1, 1)` for full screen view
- When expanded: viewport is restored to the cached initial values
- Use `RecacheViewport()` if you need to update the cached viewport during gameplay

### Animation
- Smooth lerp-based animation for panel sliding
- Configurable speed via inspector
- Automatic animation completion detection

### Button State
- Button icon automatically switches based on panel state
- Visual feedback for collapsed/expanded states
- Button remains interactive and visible at all times

### Keyboard Shortcuts
- Fully customizable in the inspector
- Default "<" (Comma) for bottom panel
- Default ">" (Period) for top panel
- Can be changed to any KeyCode

## Usage Examples

### In Code

```csharp
// Get reference to the collapsible HUD button
CollapsibleHUDButton hudButton = GetComponent<CollapsibleHUDButton>();

// Check current state
if (hudButton.IsExpanded)
{
    Debug.Log("HUD is expanded");
}

// Programmatically collapse HUD
hudButton.CollapseHUD();

// Programmatically expand HUD
hudButton.ExpandHUD();

// Toggle HUD
hudButton.ToggleHUD();

// Re-cache viewport if camera viewport changed
hudButton.RecacheViewport();
```

### For Top Panel

```csharp
// Get reference to the collapsible top panel button
CollapsibleTopPanelButton topButton = GetComponent<CollapsibleTopPanelButton>();

// Check current state
if (topButton.IsExpanded)
{
    Debug.Log("Top panel is expanded");
}

// Programmatically collapse panel
topButton.CollapsePanel();

// Programmatically expand panel
topButton.ExpandPanel();

// Toggle panel
topButton.TogglePanel();

// Re-cache viewport if camera viewport changed
topButton.RecacheViewport();
```

## Tips and Best Practices

1. **Button Positioning**:
   - For the bottom HUD button, anchor it to the bottom-center or bottom-left
   - For the top panel button, anchor it to the top-center or top-right
   - Use anchors to ensure it stays in position across screen resolutions

2. **Slide Offset**:
   - Set the slide offset to be slightly larger than your panel height
   - This ensures the panel fully moves off-screen when collapsed
   - You can adjust this value in the inspector to fine-tune

3. **Animation Speed**:
   - Higher values = faster animation (more responsive)
   - Lower values = slower animation (more cinematic)
   - Recommended range: 3-10

4. **Icon Design**:
   - Use clear, simple icons (arrows, chevrons, or text symbols)
   - Ensure icons are readable at small sizes
   - Consider using different colors for collapsed/expanded states

5. **Viewport Caching**:
   - The initial viewport is cached on Start()
   - If you change the viewport during gameplay, call `RecacheViewport()` to update
   - Both panels share the same viewport, so collapsing either will expand to full screen

## Integration with Existing HUD System

These components work alongside the existing HUD system:
- They don't interfere with `MainHUDFramework` or `HUDController`
- You can still use hotkeys like F1 to toggle all UI
- The collapsible buttons provide an additional layer of UI control
- Compatible with all existing HUD layouts and presets

## Troubleshooting

**Panel doesn't slide correctly:**
- Check that the HUD Panel RectTransform is assigned
- Verify the Slide Offset is appropriate for your panel size
- Ensure the panel's anchor and pivot are set correctly

**Button doesn't stay visible:**
- Make sure the button is NOT a child of the panel being collapsed
- Or, if it is a child, ensure it has a higher sibling index (renders on top)
- Consider placing the button outside the collapsing panel hierarchy

**Viewport doesn't restore correctly:**
- Check that Main Camera is assigned (or can be auto-found)
- Verify the camera has a Camera component
- Call `RecacheViewport()` after any manual viewport changes

**Keyboard shortcut doesn't work:**
- Verify the KeyCode is set correctly in the inspector
- Check that no other system is consuming the same input
- Ensure the component is enabled and the GameObject is active

## Script Locations

- `Assets/Scripts/UI/HUD/CollapsibleHUDButton.cs` - Main HUD toggle
- `Assets/Scripts/UI/HUD/CollapsibleTopPanelButton.cs` - Top panel toggle

## API Reference

### CollapsibleHUDButton

**Public Methods:**
- `void ToggleHUD()` - Toggles between expanded and collapsed states
- `void CollapseHUD()` - Collapses the HUD panel
- `void ExpandHUD()` - Expands the HUD panel
- `void RecacheViewport()` - Re-caches the current viewport values

**Public Properties:**
- `bool IsExpanded` - Returns true if HUD is currently expanded

### CollapsibleTopPanelButton

**Public Methods:**
- `void TogglePanel()` - Toggles between expanded and collapsed states
- `void CollapsePanel()` - Collapses the top panel
- `void ExpandPanel()` - Expands the top panel
- `void RecacheViewport()` - Re-caches the current viewport values

**Public Properties:**
- `bool IsExpanded` - Returns true if panel is currently expanded
