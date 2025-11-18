# Wall System Implementation Analysis

## Overview
The wall system in Kingdoms At Dusk uses a **pole-to-pole** placement approach with mesh-based automatic segment fitting. Walls are stored as individual segments and automatically detect neighbors for connection.

---

## 1. Wall Segment Creation & Storage

### Where Segments are Created
**File**: `/home/user/KingdomsAtDusk/Assets/Scripts/RTSBuildingsSystems/WallPlacementController.cs`

**Method**: `PlaceWallSegments()` (lines 799-907)

### Data Structure
Segments are stored in the **PlacedWallSegment struct** (lines 255-292):

```csharp
private struct PlacedWallSegment
{
    public Vector3 center;      // World position at center of segment
    public float length;        // Length along the wall axis (in world units)
    public Quaternion rotation; // Rotation of the segment
    
    public PlacedWallSegment(Vector3 c, float l, Quaternion rot)
    {
        center = c;
        length = l;
        rotation = rot;
    }
}
```

### Storage Location
- **List**: `private List<PlacedWallSegment> placedWallSegments`
- **Usage**: Tracks all placed wall segments for overlap detection
- **Scope**: Instance-based (per WallPlacementController)

### Segment Calculation
**Method**: `CalculateWallSegmentsWithScaling()` (lines 586-642)

Creates wall segments with intelligent scaling:
- Calculates number of full-size segments that fit
- Creates remaining partial segment scaled to fit exact distance
- Returns `List<WallSegmentData>` with position, scale, and length info

```csharp
private struct WallSegmentData
{
    public Vector3 position;  // World position
    public Vector3 scale;     // Local scale (including adaptive scaling)
    public float length;      // World-space length along wall axis
}
```

---

## 2. Connection Points Determination

### Connection Point Calculation
**Method**: `GetStartPosition()` and `GetEndPosition()` (lines 268-290)

Each segment has **three key connection points**:

#### Start Point (One End)
```csharp
public Vector3 GetStartPosition(WallLengthAxis axis)
{
    Vector3 dir = rotation * Vector3.forward;
    switch (axis)
    {
        case WallLengthAxis.X: dir = rotation * Vector3.right; break;
        case WallLengthAxis.Y: dir = rotation * Vector3.up; break;
        case WallLengthAxis.Z: dir = rotation * Vector3.forward; break;
    }
    return center - dir * (length / 2f);  // One end of segment
}
```

#### End Point (Other End)
```csharp
public Vector3 GetEndPosition(WallLengthAxis axis)
{
    // ... (same logic as GetStartPosition but with +)
    return center + dir * (length / 2f);  // Other end of segment
}
```

#### Midpoint (Center of Segment)
```csharp
// From TrySnapToNearbyWall() lines 1004-1006
Vector3 midpoint = (start + end) * 0.5f;
float distMid = Vector3.Distance(position, midpoint);
```

### Connection Points Summary
For a segment with:
- Center: `(10, 0, 5)`
- Length: `2.0`
- Direction: `+X`

**Connection points would be**:
- Start: `(9, 0, 5)` (center - length/2 along axis)
- End: `(11, 0, 5)` (center + length/2 along axis)
- Midpoint: `(10, 0, 5)` (center position)

---

## 3. Wall Placement & Connection Logic

### Starting a Wall (First Pole)
**Method**: `PlaceFirstPole()` (lines 773-797)
- Records first pole position: `firstPolePosition`
- Sets flag: `firstPoleSet = true`
- Creates visual pole if prefab available

### Ending a Wall (Second Pole - With Snapping)
**Method**: `UpdateWallPlacement()` (lines 494-521)

1. Gets mouse position
2. **Snaps to grid** if enabled
3. **Attempts to snap to nearby wall** via `TrySnapToNearbyWall()`

### Snapping to Wall Segments
**Method**: `TrySnapToNearbyWall()` (lines 987-1032)

This method attempts to snap the second pole to:
1. **Start points** of existing segments (line 1009-1014)
2. **End points** of existing segments (line 1016-1021)
3. **Midpoints** of existing segments (line 1023-1028) - **NEW FEATURE**

```csharp
private bool TrySnapToNearbyWall(Vector3 position, out Vector3 snappedPosition)
{
    snappedPosition = position;
    float closestDistance = float.MaxValue;
    bool found = false;
    
    foreach (var seg in placedWallSegments)
    {
        Vector3 start = seg.GetStartPosition(wallLengthAxis);
        Vector3 end = seg.GetEndPosition(wallLengthAxis);
        
        // NEW: Midpoint snapping
        Vector3 midpoint = (start + end) * 0.5f;
        float distMid = Vector3.Distance(position, midpoint);
        
        // Check start point
        if (distStart < wallSnapDistance && distStart < closestDistance)
        {
            closestDistance = distStart;
            snappedPosition = start;
            found = true;
        }
        
        // Check end point
        if (distEnd < wallSnapDistance && distEnd < closestDistance)
        {
            closestDistance = distEnd;
            snappedPosition = end;
            found = true;
        }
        
        // NEW: Check midpoint
        if (distMid < wallSnapDistance && distMid < closestDistance)
        {
            closestDistance = distMid;
            snappedPosition = midpoint;
            found = true;
        }
    }
    
    return found;
}
```

**Snap Distance**: `wallSnapDistance = 2f` (default, configurable)

---

## 4. Validation & Connection Prevention

### Overlap Validation
**Method**: `WouldOverlapExistingWall()` (lines 154-184)

This method determines if a new wall segment would overlap an existing one:

```csharp
private bool WouldOverlapExistingWall(Vector3 start, Vector3 end)
{
    foreach (var seg in placedWallSegments)
    {
        Vector3 existingStart = seg.GetStartPosition(wallLengthAxis);
        Vector3 existingEnd = seg.GetEndPosition(wallLengthAxis);
        
        // 1. ENDPOINT CONNECTIONS ALLOWED
        // Checks if NEW wall start/end touches EXISTING wall start/end
        if (Vector3.Distance(start, existingStart) < 0.01f ||
            Vector3.Distance(start, existingEnd) < 0.01f ||
            Vector3.Distance(end, existingStart) < 0.01f ||
            Vector3.Distance(end, existingEnd) < 0.01f)
        {
            continue;  // ✅ ALLOWED - connects at endpoint
        }
        
        // 2. INTERSECTION CHECK
        if (SegmentsIntersect2D(start, end, existingStart, existingEnd))
        {
            return true;  // ❌ BLOCKED - segments intersect
        }
        
        // 3. COLLINEAR & OVERLAPPING CHECK
        if (AreCollinearAndOverlapping(start, end, existingStart, existingEnd))
        {
            return true;  // ❌ BLOCKED - parallel overlap detected
        }
    }
    
    return false;  // ✅ No overlap detected
}
```

### The Critical Issue: Midpoint Snapping Not Validated
**PROBLEM IDENTIFIED:**

When a user snaps to a **midpoint** of an existing segment:
1. ✅ Snapping works (TrySnapToNearbyWall finds the midpoint)
2. ❌ **Validation fails** (WouldOverlapExistingWall doesn't recognize midpoint as valid)

**Why?** The endpoint check (lines 161-168) only looks for:
- New wall start/end matching existing start/end
- Does NOT check if new start/end is at existing midpoint

**Result**: When snapping to midpoint, the placement looks for intersection, finds that it intersects the segment (because start and end straddle the midpoint), and blocks the placement.

### Validation Workflow
**Method**: `UpdateWallPreview()` (lines 523-563)

```csharp
private void UpdateWallPreview(Vector3 secondPolePos)
{
    // ... setup code ...
    
    // Check if would overlap (THIS IS THE PROBLEM)
    bool overlapsWall = WouldOverlapExistingWall(firstPolePosition, secondPolePos);
    bool overlapsBuilding = WouldOverlapBuildings(firstPolePosition, secondPolePos);
    
    if (overlapsWall || overlapsBuilding)
    {
        canAfford = false;  // Force preview to RED
    }
    
    // ... visualization code ...
}
```

---

## 5. Connection System (WallConnectionSystem)

### Proximity-Based Connections
**File**: `/home/user/KingdomsAtDusk/Assets/Scripts/RTSBuildingsSystems/WallConnectionSystem.cs`

This is SEPARATE from placement and handles visual connections:

```csharp
public void UpdateConnections()
{
    if (!enableConnections) return;
    
    connectedWalls.Clear();
    
    // Find nearby walls within connection distance
    Vector3 myPos = transform.position;
    foreach (var otherWall in allWalls)
    {
        if (otherWall == this || otherWall == null) continue;
        
        float distance = Vector3.Distance(myPos, otherWall.transform.position);
        if (distance <= connectionDistance)  // Default: 1.5f
        {
            connectedWalls.Add(otherWall);
        }
    }
    
    // Update visual based on connections
    UpdateVisualMesh();
}
```

**Connection Distance**: `1.5f` (configurable)

**Works on**: Wall centers, not segment endpoints!

---

## 6. Key Configuration Parameters

### WallPlacementController
| Parameter | Default | Purpose |
|-----------|---------|---------|
| `wallSnapDistance` | 2.0f | Distance to snap to wall points |
| `minParallelOverlap` | 0.5f | Minimum overlap to block placement |
| `minScaleFactor` | 0.3f | Minimum scale for last segment |
| `wallLengthAxis` | X | Which axis defines segment length |
| `useGridSnapping` | false | Enable grid-based snapping |
| `gridSize` | 1.0f | Grid cell size |

### WallConnectionSystem
| Parameter | Default | Purpose |
|-----------|---------|---------|
| `connectionDistance` | 1.5f | Distance to detect neighbors |
| `enableConnections` | true | Enable connection system |

---

## 7. The Problem & Solution

### Current Behavior
1. User places first pole ✅
2. User moves cursor to midpoint of wall segment
3. System snaps cursor to midpoint ✅
4. System validates placement
5. **Overlap check finds intersection** ❌
6. Placement shows as RED (invalid)
7. **User cannot place wall at midpoint** ❌

### Root Cause
The `WouldOverlapExistingWall()` method checks for endpoint connections but not midpoint connections.

When new segment start/end points are at existing segment's midpoint:
- They are NOT caught by endpoint check (lines 161-168)
- They trigger intersection/overlap detection (lines 170-180)
- Placement is blocked

### Required Fix
The validation logic needs to recognize midpoint snaps as valid connection points, similar to how it allows endpoint connections.

**Options**:
1. Add midpoint check to endpoint validation (lines 161-168)
2. Pass snapping info to validation to distinguish intentional snaps
3. Create special case for midpoint connections

---

## 8. Summary Table

| Aspect | Details |
|--------|---------|
| **Segments Created By** | `CalculateWallSegmentsWithScaling()` |
| **Segments Stored In** | `List<PlacedWallSegment>` |
| **Segment Duration** | Until BuildingManager cleared or scene reloaded |
| **Connection Points** | Start, End, Midpoint (calculated dynamically) |
| **Snapping Range** | 2.0 units (wallSnapDistance) |
| **Snapping Validation** | Endpoint-only (midpoint not validated) |
| **Validation Method** | `WouldOverlapExistingWall()` |
| **Connection System** | Separate proximity-based (1.5f distance) |

---

## Files to Modify for Fix

1. `/home/user/KingdomsAtDusk/Assets/Scripts/RTSBuildingsSystems/WallPlacementController.cs`
   - Update `WouldOverlapExistingWall()` to handle midpoint snaps
   
2. (Optional) Enhanced snapping info passing to validation

