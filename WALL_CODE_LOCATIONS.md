# Wall System - Code Locations & Problem Areas

## File Locations Summary

### Main Implementation File
**Path**: `/home/user/KingdomsAtDusk/Assets/Scripts/RTSBuildingsSystems/WallPlacementController.cs`

### Key Methods & Line Numbers

#### 1. SEGMENT CREATION
```
Lines 586-642    CalculateWallSegmentsWithScaling()
                 ↓
                 Creates WallSegmentData structs
                 ↓
Lines 799-907    PlaceWallSegments()
                 ↓
                 Instantiates actual walls in scene
                 ↓
Lines 869-871    Tracks segments in placedWallSegments list
```

#### 2. CONNECTION POINT DEFINITION
```
Lines 255-292    PlacedWallSegment struct definition
                 ├─ center: Vector3
                 ├─ length: float
                 └─ rotation: Quaternion
                 
Lines 268-290    GetStartPosition() & GetEndPosition() methods
                 ↓
                 Calculate start/end points from center + rotation + length
                 
Lines 1004-1006  Midpoint calculation in TrySnapToNearbyWall()
                 ↓
                 midpoint = (start + end) * 0.5f
```

#### 3. SNAPPING TO WALL POINTS
```
Lines 987-1032   TrySnapToNearbyWall() method
                 ├─ Lines 1009-1014: Snaps to START points ✅
                 ├─ Lines 1016-1021: Snaps to END points ✅
                 └─ Lines 1023-1028: Snaps to MIDPOINTS ✅
```

#### 4. VALIDATION (THE PROBLEM AREA)
```
Lines 154-184    WouldOverlapExistingWall() method
                 │
                 ├─ Lines 161-168: ⚠️ ONLY checks ENDPOINT connections
                 │                  (Does NOT validate midpoint snaps!)
                 │
                 ├─ Lines 170-174: Checks 2D intersection
                 │                  (FAILS for midpoint snaps)
                 │
                 └─ Lines 176-180: Checks collinear overlap
                                    (FAILS for midpoint snaps)

Lines 531-533    UpdateWallPreview() calls WouldOverlapExistingWall()
                 ↓
                 Shows RED (invalid) when midpoint snapped
```

---

## The Problem Visualized

### What Happens When Snapping to Midpoint

```
EXISTING SEGMENT:
    Start (9,0,5) ────── Midpoint (10,0,5) ────── End (11,0,5)
    
USER SNAPS TO MIDPOINT:
    Second Pole → (10,0,5) [midpoint]
    
NEW WALL WOULD BE:
    From (5,0,5) to (10,0,5)
    
VALIDATION PROCESS:
    1. Check if (5,0,5) or (10,0,5) == (9,0,5) or (11,0,5)? NO ❌
    2. Do segments intersect? YES (they overlap) ❌
    3. Are they collinear and overlapping? YES ❌
    
RESULT: Placement marked as INVALID (RED)
```

---

## Code Snippet: The Exact Problem

```csharp
// FILE: WallPlacementController.cs
// LINES: 154-184

private bool WouldOverlapExistingWall(Vector3 start, Vector3 end)
{
    foreach (var seg in placedWallSegments)
    {
        Vector3 existingStart = seg.GetStartPosition(wallLengthAxis);
        Vector3 existingEnd = seg.GetEndPosition(wallLengthAxis);
        
        // ❌ PROBLEM: This only checks for endpoint connections
        // It does NOT check if start/end is at the MIDPOINT
        if (Vector3.Distance(start, existingStart) < 0.01f ||
            Vector3.Distance(start, existingEnd) < 0.01f ||
            Vector3.Distance(end, existingStart) < 0.01f ||
            Vector3.Distance(end, existingEnd) < 0.01f)
        {
            continue; // Allow endpoint connections
        }
        
        // These checks then block the midpoint snap:
        if (SegmentsIntersect2D(start, end, existingStart, existingEnd))
        {
            return true; // ❌ Blocks midpoint snap
        }
        
        if (AreCollinearAndOverlapping(start, end, existingStart, existingEnd))
        {
            return true; // ❌ Blocks midpoint snap
        }
    }
    
    return false;
}
```

---

## Data Flow: From Placement to Validation

```
StartPlacingWalls(wallData)
    │
    ├─ PlaceFirstPole()
    │  └─ firstPolePosition = user click position
    │
    └─ Update() → UpdateWallPlacement()
       │
       ├─ GetMouseWorldPosition()
       ├─ SnapToGrid()
       │
       ├─ TrySnapToNearbyWall(snappedPos)  ← SNAPPING LOGIC
       │  │
       │  └─ For each segment:
       │     ├─ GetStartPosition()
       │     ├─ GetEndPosition()
       │     └─ Calculate midpoint & check distance
       │
       └─ UpdateWallPreview(secondPolePos)
          │
          ├─ WouldOverlapExistingWall() ← VALIDATION (PROBLEM HERE!)
          │  │
          │  └─ For each segment:
          │     ├─ Check endpoint matches (ONLY)
          │     ├─ Check intersection (BLOCKS midpoint)
          │     └─ Check collinear overlap (BLOCKS midpoint)
          │
          └─ Set canAfford = false (shows RED preview)
```

---

## Storage & Lifecycle

### How Segments are Stored
```
private List<PlacedWallSegment> placedWallSegments

Populated by:
├─ PlaceWallSegments() line 870
│  └─ Added to list for overlap detection
│
└─ Used for:
   ├─ TrySnapToNearbyWall() - check snap points
   ├─ WouldOverlapExistingWall() - validate placement
   ├─ CalculateWallSegmentsWithScaling() - check existing segments
   └─ OnDrawGizmos() - debug visualization
```

### Persistence
- **Scope**: Instance of WallPlacementController
- **Duration**: Remains while controller exists (until scene reload)
- **Cleared**: Never explicitly cleared in code
- **Separate**: This is separate from actual instantiated GameObjects

---

## Related Files

### For Connection Validation
- `/home/user/KingdomsAtDusk/Assets/Scripts/RTSBuildingsSystems/WallConnectionSystem.cs`
  - Uses proximity-based connection (connectionDistance = 1.5f)
  - Works on wall centers, not segment endpoints
  - Separate from placement validation

### For Building Instantiation
- Building.cs - Actual GameObject creation & storage
- BuildingManager.cs - High-level building management

