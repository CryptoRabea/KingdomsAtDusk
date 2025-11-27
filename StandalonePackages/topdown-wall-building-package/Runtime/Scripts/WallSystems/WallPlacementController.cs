using UnityEngine;
using UnityEngine.InputSystem;
using TopDownWallBuilding.Core.Services;
using TopDownWallBuilding.Core.Events;
using System.Collections.Generic;

namespace TopDownWallBuilding.WallSystems
{
    /// <summary>
    /// Which axis represents the wall's length (the direction walls connect).
    /// </summary>
    public enum WallLengthAxis
    {
        X,  // Width/Side-to-side
        Y,  // Height/Up-down (rare)
        Z   // Depth/Forward-back
    }

    /// <summary>
    /// Handles pole-to-pole wall placement with perfect mesh-based fitting.
    /// Walls are placed end-to-end using actual mesh dimensions.
    /// Last segment scales to fit remaining distance perfectly - NO GAPS, NO OVERLAPS!
    /// </summary>
    public class WallPlacementController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private LayerMask groundLayer;

        [Header("Visual Settings")]
        [SerializeField] private Material validPreviewMaterial;
        [SerializeField] private Material invalidPreviewMaterial;
        [SerializeField] private LineRenderer linePreviewRenderer;
        [SerializeField] private float lineWidth = 0.2f;
        [SerializeField] private Color validLineColor = Color.green;
        [SerializeField] private Color invalidLineColor = Color.red;

        [Header("Placement Settings")]
        [SerializeField] private bool useGridSnapping = false;
        [SerializeField] private float gridSize = 1f;
        [SerializeField] private float wallSnapDistance = 2f;
        [SerializeField] private bool autoCompleteOnSnap = true;
        [SerializeField] private float minParallelOverlap = 0.5f;


        [Header("Mesh-Based Placement")]
        [Tooltip("Minimum scale for last segment (0.3 = 30% of original size minimum)")]
        [SerializeField] private float minScaleFactor = 0.3f;
        [Tooltip("Use mesh bounds for automatic wall sizing")]
        [SerializeField] private bool useAutoMeshSize = true;
        [Tooltip("Which axis represents the wall's length (the direction walls connect)")]
        [SerializeField] private WallLengthAxis wallLengthAxis = WallLengthAxis.X;

        [Header("Pole Settings")]
        [SerializeField] private GameObject polePrefab;
        [SerializeField] private float poleHeight = 2f;

        // State
        private bool isPlacingWall = false;
        private bool firstPoleSet = false;
        private Vector3 firstPolePosition;
        private GameObject firstPoleVisual;
        private GameObject currentPolePreview;
        private List<GameObject> wallSegmentPreviews = new List<GameObject>();

        // Track placed walls
        private List<PlacedWallSegment> placedWallSegments = new List<PlacedWallSegment>();
        private bool isSnappedToWall = false;
        private Vector3 snappedWallPosition;

        // Mesh-based sizing
        private float wallMeshLength = 1f; // Auto-detected from mesh

        // Current building data
        private BuildingDataSO currentWallData;

        // Services
        private IResourcesService resourceService;

        // Input
        private Mouse mouse;
        private Keyboard keyboard;

        // Resource calculation
        private int requiredSegments = 0;
        private Dictionary<ResourceType, int> totalCost = new Dictionary<ResourceType, int>();
        private bool canAfford = false;

        // Current wall direction for rotation
        private Quaternion currentWallRotation = Quaternion.identity;

        private List<Vector3> placedWallPositions = new List<Vector3>();

        /// <summary>
        /// Checks if a wall segment placed between (start→end) would overlap any buildings.
        /// Uses capsule collision detection along the wall path.
        /// </summary>
        private bool WouldOverlapBuildings(Vector3 start, Vector3 end)
        {
            Vector3 direction = end - start;
            float distance = direction.magnitude;

            if (distance < 0.01f)
                return false;

            // Check along the wall path using a capsule collider
            // Capsule radius should be slightly larger than wall width to ensure proper detection
            float capsuleRadius = 0.5f; // Adjust based on wall width
            Vector3 capsulePoint1 = start + Vector3.up * 0.5f;
            Vector3 capsulePoint2 = end + Vector3.up * 0.5f;

            // Check for overlapping colliders (excluding ground layer)
            Collider[] colliders = Physics.OverlapCapsule(
                capsulePoint1,
                capsulePoint2,
                capsuleRadius,
                ~groundLayer // Exclude ground layer
            );

            foreach (var col in colliders)
            {
                // Ignore preview objects
                if (col.transform.IsChildOf(transform))
                    continue;

                // Ignore terrain
                if (col is TerrainCollider)
                    continue;

                // Check if it's a building
                Building building = col.GetComponentInParent<Building>();
                if (building != null)
                {
                    Debug.Log($"Wall would overlap building: {col.gameObject.name}");
                    return true;
                }

                // Also check for any wall connection system (to catch walls that might not have Building component)
                WallConnectionSystem wallSystem = col.GetComponentInParent<WallConnectionSystem>();
                if (wallSystem != null)
                {
                    // This is a wall segment, which is handled by WouldOverlapExistingWall
                    continue;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a wall segment placed between (start→end) would overlap an existing wall.
        /// Allows connecting EXACTLY at endpoints or midpoints, but blocks overlapping the body.
        /// </summary>
        private bool WouldOverlapExistingWall(Vector3 start, Vector3 end)
        {
            // ✅ Allow closing a square: if we're trying to connect back to our first pole, that's okay!
            bool closingLoop = firstPoleSet && (Vector3.Distance(end, firstPolePosition) < 0.5f || Vector3.Distance(start, firstPolePosition) < 0.5f);

            // First check against walls we're currently placing (placedWallSegments)
            foreach (var seg in placedWallSegments)
            {
                Vector3 existingStart = seg.GetStartPosition(wallLengthAxis);
                Vector3 existingEnd = seg.GetEndPosition(wallLengthAxis);

                // 1. If connecting exactly to endpoints → allowed
                float endpointTolerance = closingLoop ? 0.5f : 0.01f; // More lenient when closing loops
                if (Vector3.Distance(start, existingStart) < endpointTolerance ||
                    Vector3.Distance(start, existingEnd) < endpointTolerance ||
                    Vector3.Distance(end, existingStart) < endpointTolerance ||
                    Vector3.Distance(end, existingEnd) < endpointTolerance)
                {
                    continue; // endpoint connections allowed
                }

                // 1b. If connecting exactly to midpoint → allowed
                Vector3 existingMid = (existingStart + existingEnd) * 0.5f;
                if (Vector3.Distance(start, existingMid) < endpointTolerance ||
                    Vector3.Distance(end, existingMid) < endpointTolerance)
                {
                    continue; // midpoint connections allowed
                }

                // 2. Check segment intersection in 2D (X/Z)
                if (SegmentsIntersect2D(start, end, existingStart, existingEnd))
                {
                    return true; // Overlap or crossing detected
                }

                // 3. Check if new segment lies on top of an existing one (collinear & overlapping)
                if (AreCollinearAndOverlapping(start, end, existingStart, existingEnd))
                {
                    return true;
                }
            }

            // Also check against existing walls in the scene (not just from current session)
            // Use physics overlap to find any walls along this path
            Vector3 direction = end - start;
            float distance = direction.magnitude;

            if (distance < 0.01f)
                return false;

            // ✅ When closing a loop, we don't need to check physics overlap since we already verified endpoint connections
            if (closingLoop)
            {
                Debug.Log("🔒 Closing loop detected - allowing connection back to first pole");
                return false;
            }

            // Check along the wall path using capsule collider
            float capsuleRadius = 0.3f; // Slightly smaller than wall width for endpoint connections
            Vector3 capsulePoint1 = start + Vector3.up * 0.5f;
            Vector3 capsulePoint2 = end + Vector3.up * 0.5f;

            Collider[] colliders = Physics.OverlapCapsule(
                capsulePoint1,
                capsulePoint2,
                capsuleRadius,
                ~groundLayer
            );

            foreach (var col in colliders)
            {
                // Ignore preview objects
                if (col.transform.IsChildOf(transform))
                    continue;

                // Ignore terrain
                if (col is TerrainCollider)
                    continue;

                // Check if it's a wall
                WallConnectionSystem wallSystem = col.GetComponentInParent<WallConnectionSystem>();
                if (wallSystem != null)
                {
                    // ✅ Calculate actual endpoints considering the wall's scale
                    if (TryGetWallEndpoints(wallSystem, out Vector3 existingStart, out Vector3 existingEnd))
                    {
                        // 1. If connecting exactly to endpoints → allowed
                        float endpointTolerance = closingLoop ? 0.5f : 0.01f;
                        if (Vector3.Distance(start, existingStart) < endpointTolerance ||
                            Vector3.Distance(start, existingEnd) < endpointTolerance ||
                            Vector3.Distance(end, existingStart) < endpointTolerance ||
                            Vector3.Distance(end, existingEnd) < endpointTolerance)
                        {
                            continue; // endpoint connections allowed
                        }

                        // 1b. If connecting exactly to midpoint → allowed
                        Vector3 existingMid = (existingStart + existingEnd) * 0.5f;
                        if (Vector3.Distance(start, existingMid) < endpointTolerance ||
                            Vector3.Distance(end, existingMid) < endpointTolerance)
                        {
                            continue; // midpoint connections allowed
                        }

                        // 2. Check segment intersection in 2D (X/Z)
                        if (SegmentsIntersect2D(start, end, existingStart, existingEnd))
                        {
                            Debug.Log($"Wall would intersect existing wall: {col.gameObject.name}");
                            return true; // Overlap or crossing detected
                        }

                        // 3. Check if new segment lies on top of an existing one (collinear & overlapping)
                        if (AreCollinearAndOverlapping(start, end, existingStart, existingEnd))
                        {
                            Debug.Log($"Wall would overlap existing wall (collinear): {col.gameObject.name}");
                            return true;
                        }
                    }
                    else
                    {
                        // Fallback: use center position if we can't calculate endpoints
                        Vector3 wallPos = wallSystem.transform.position;

                        // Allow connection at endpoints (within tolerance)
                        if (Vector3.Distance(start, wallPos) < 0.5f || Vector3.Distance(end, wallPos) < 0.5f)
                        {
                            continue; // Connection at endpoint is allowed
                        }

                        Debug.Log($"Wall would overlap existing wall: {col.gameObject.name} at {wallPos}");
                        return true;
                    }
                }
            }

            return false;
        }

        private bool SegmentsIntersect2D(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2)
        {
            if (ShareEndpoint(a1, a2, b1, b2))
                return false; // touching is allowed

            Vector2 A1 = new Vector2(a1.x, a1.z);
            Vector2 A2 = new Vector2(a2.x, a2.z);
            Vector2 B1 = new Vector2(b1.x, b1.z);
            Vector2 B2 = new Vector2(b2.x, b2.z);

            // Direction vectors
            Vector2 dA = (A2 - A1).normalized;
            Vector2 dB = (B2 - B1).normalized;

            // 1. If they are parallel → NOT intersecting
            float cross = dA.x * dB.y - dA.y * dB.x;
            if (Mathf.Abs(cross) < 0.0001f)
                return false; // Parallel or nearly parallel → allowed

            // 2. Standard intersection only if not parallel
            bool ccw(Vector2 p1, Vector2 p2, Vector2 p3) =>
                (p3.y - p1.y) * (p2.x - p1.x) > (p2.y - p1.y) * (p3.x - p1.x);

            return (ccw(A1, B1, B2) != ccw(A2, B1, B2)) &&
                   (ccw(A1, A2, B1) != ccw(A1, A2, B2));
        }


        private bool AreCollinearAndOverlapping(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2)
        {
            // If they only touch at endpoints, this is allowed
            if (ShareEndpoint(a1, a2, b1, b2))
                return false;

            // Convert to 2D
            Vector2 A1 = new Vector2(a1.x, a1.z);
            Vector2 A2 = new Vector2(a2.x, a2.z);
            Vector2 B1 = new Vector2(b1.x, b1.z);
            Vector2 B2 = new Vector2(b2.x, b2.z);

            // Check collinearity (cross product == 0)
            Vector2 dirA = (A2 - A1).normalized;
            Vector2 dirB = (B2 - B1).normalized;

            if (Mathf.Abs(Vector2.Perpendicular(dirA).x * dirB.x + Vector2.Perpendicular(dirA).y * dirB.y) > 0.01f)
                return false; // Not collinear

            // Project B onto A line
            float aMin = Vector2.Dot(A1, dirA);
            float aMax = Vector2.Dot(A2, dirA);

            float bMin = Vector2.Dot(B1, dirA);
            float bMax = Vector2.Dot(B2, dirA);

            // ✅ Sort min/max in case walls are oriented in opposite directions
            if (aMin > aMax) { float temp = aMin; aMin = aMax; aMax = temp; }
            if (bMin > bMax) { float temp = bMin; bMin = bMax; bMax = temp; }

            float overlap = Mathf.Min(aMax, bMax) - Mathf.Max(aMin, bMin);

            // No overlap
            if (overlap <= 0)
                return false;

            // 🔥 NEW RULE — ignore tiny overlaps
            if (overlap < minParallelOverlap)
                return false;

            return true; // Real overlap → block building
        }



        private struct PlacedWallSegment
        {
            public Vector3 center;
            public float length; // along the wall axis
            public Quaternion rotation;

            public PlacedWallSegment(Vector3 c, float l, Quaternion rot)
            {
                center = c;
                length = l;
                rotation = rot;
            }

            public Vector3 GetStartPosition(WallLengthAxis axis)
            {
                Vector3 dir = rotation * Vector3.forward;
                switch (axis)
                {
                    case WallLengthAxis.X: dir = rotation * Vector3.right; break;
                    case WallLengthAxis.Y: dir = rotation * Vector3.up; break;
                    case WallLengthAxis.Z: dir = rotation * Vector3.forward; break;
                }
                return center - dir * (length / 2f);
            }

            public Vector3 GetEndPosition(WallLengthAxis axis)
            {
                Vector3 dir = rotation * Vector3.forward;
                switch (axis)
                {
                    case WallLengthAxis.X: dir = rotation * Vector3.right; break;
                    case WallLengthAxis.Y: dir = rotation * Vector3.up; break;
                    case WallLengthAxis.Z: dir = rotation * Vector3.forward; break;
                }
                return center + dir * (length / 2f);
            }

        }

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            mouse = Mouse.current;
            keyboard = Keyboard.current;

            SetupLineRenderer();
        }

        private void Start()
        {
            resourceService = ServiceLocator.TryGet<IResourcesService>();

            if (resourceService == null)
            {
                Debug.LogError("WallPlacementController: ResourceService not available!");
            }
        }

        private void Update()
        {
            if (isPlacingWall)
            {
                UpdateWallPlacement();
                HandleWallPlacementInput();
            }
        }

        #region Setup

        private void SetupLineRenderer()
        {
            if (linePreviewRenderer == null)
            {
                GameObject lineObj = new GameObject("WallLinePreview");
                lineObj.transform.SetParent(transform);
                linePreviewRenderer = lineObj.AddComponent<LineRenderer>();
                linePreviewRenderer.startWidth = lineWidth;
                linePreviewRenderer.endWidth = lineWidth;
                linePreviewRenderer.material = new Material(Shader.Find("Sprites/Default"));
                linePreviewRenderer.enabled = false;
            }
        }

        #endregion

        #region Public API

        public void StartPlacingWalls(BuildingDataSO wallData)
        {
            if (wallData == null || wallData.buildingPrefab == null)
            {
                Debug.LogError("Invalid wall data!");
                return;
            }

            CancelWallPlacement();

            currentWallData = wallData;
            isPlacingWall = true;
            firstPoleSet = false;

            // ✅ Auto-detect wall mesh length
            if (useAutoMeshSize)
            {
                wallMeshLength = DetectWallMeshLength(wallData.buildingPrefab);
                Debug.Log($"Auto-detected wall mesh length: {wallMeshLength}");
            }

            placedWallPositions.Clear();
            isSnappedToWall = false;

            CreatePolePreview();

            Debug.Log($"Started placing walls: {wallData.buildingName}");
        }

        public void CancelWallPlacement()
        {
            if (firstPoleVisual != null)
            {
                Destroy(firstPoleVisual);
                firstPoleVisual = null;
            }

            if (currentPolePreview != null)
            {
                Destroy(currentPolePreview);
                currentPolePreview = null;
            }

            foreach (var preview in wallSegmentPreviews)
            {
                if (preview != null)
                    Destroy(preview);
            }
            wallSegmentPreviews.Clear();

            if (linePreviewRenderer != null)
                linePreviewRenderer.enabled = false;

            isPlacingWall = false;
            firstPoleSet = false;
            currentWallData = null;
            requiredSegments = 0;
            totalCost.Clear();
            isSnappedToWall = false;
        }

        public bool IsPlacingWalls => isPlacingWall;
        public Dictionary<ResourceType, int> GetTotalCost() => new Dictionary<ResourceType, int>(totalCost);
        public int GetRequiredSegments() => requiredSegments;

        #endregion

        #region Mesh Detection

        /// <summary>
        /// ✅ Detect the actual length of the wall mesh along the configured axis.
        /// This is used to place walls end-to-end perfectly.
        /// </summary>
        private float DetectWallMeshLength(GameObject wallPrefab)
        {
            // Try to get mesh from MeshFilter
            MeshFilter meshFilter = wallPrefab.GetComponentInChildren<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                Bounds bounds = meshFilter.sharedMesh.bounds;
                Transform meshTransform = meshFilter.transform;

                // ✅ Get the length along the configured axis
                float boundsSize = 0f;
                float meshScale = 1f;
                float prefabScale = 1f;

                switch (wallLengthAxis)
                {
                    case WallLengthAxis.X:
                        boundsSize = bounds.size.x;
                        meshScale = meshTransform.localScale.x;
                        prefabScale = wallPrefab.transform.localScale.x;
                        break;
                    case WallLengthAxis.Y:
                        boundsSize = bounds.size.y;
                        meshScale = meshTransform.localScale.y;
                        prefabScale = wallPrefab.transform.localScale.y;
                        break;
                    case WallLengthAxis.Z:
                        boundsSize = bounds.size.z;
                        meshScale = meshTransform.localScale.z;
                        prefabScale = wallPrefab.transform.localScale.z;
                        break;
                }

                float length = boundsSize * meshScale * prefabScale;
                return Mathf.Max(length, 0.1f); // Minimum 0.1 to avoid issues
            }

            // Fallback: try collider
            Collider collider = wallPrefab.GetComponent<Collider>();
            if (collider != null)
            {
                Bounds bounds = collider.bounds;
                float size = wallLengthAxis == WallLengthAxis.X ? bounds.size.x :
                            (wallLengthAxis == WallLengthAxis.Y ? bounds.size.y : bounds.size.z);
                return Mathf.Max(size, 0.1f);
            }

            // Last resort: use scale
            Debug.LogWarning($"Could not detect wall mesh length, using transform scale {wallLengthAxis}");
            float scaleValue = wallLengthAxis == WallLengthAxis.X ? wallPrefab.transform.localScale.x :
                              (wallLengthAxis == WallLengthAxis.Y ? wallPrefab.transform.localScale.y : wallPrefab.transform.localScale.z);
            return Mathf.Max(scaleValue, 1f);
        }

        #endregion

        #region Placement Logic

        private void CreatePolePreview()
        {
            if (polePrefab != null)
            {
                currentPolePreview = Instantiate(polePrefab);
            }
            else
            {
                currentPolePreview = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                currentPolePreview.transform.localScale = new Vector3(0.3f, poleHeight / 2f, 0.3f);
            }

            var collider = currentPolePreview.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            SetPreviewMaterial(currentPolePreview, validPreviewMaterial);
        }

        private void UpdateWallPlacement()
        {
            if (mouse == null) return;

            Vector3 mouseWorldPos = GetMouseWorldPosition();
            if (mouseWorldPos == Vector3.zero) return;

            Vector3 snappedPos = SnapToGrid(mouseWorldPos);

            isSnappedToWall = TrySnapToNearbyWall(snappedPos, out Vector3 wallSnappedPos);
            if (isSnappedToWall)
            {
                snappedPos = wallSnappedPos;
                snappedWallPosition = wallSnappedPos;
            }

            if (!firstPoleSet)
            {
                if (currentPolePreview != null)
                {
                    currentPolePreview.transform.position = snappedPos + Vector3.up * (poleHeight / 2f);
                }
            }
            else
            {
                UpdateWallPreview(snappedPos);
            }
        }

        private void UpdateWallPreview(Vector3 secondPolePos)
        {
            // ✅ CHECK FOG OF WAR: Only allow placement in currently visible areas
            bool notVisible = false;
            if (null != null)
            {
                bool firstPoleVisible = null.IsVisible(firstPolePosition);
                bool secondPoleVisible = null.IsVisible(secondPolePos);
                notVisible = !firstPoleVisible || !secondPoleVisible;

                if (notVisible)
                {
                    Debug.Log("Cannot place wall: area not currently visible");
                }
            }

            // Check for overlaps
            bool overlapsWall = WouldOverlapExistingWall(firstPolePosition, secondPolePos);
            bool overlapsBuilding = WouldOverlapBuildings(firstPolePosition, secondPolePos);

            if (currentPolePreview != null)
            {
                currentPolePreview.transform.position = secondPolePos + Vector3.up * (poleHeight / 2f);
            }

            currentWallRotation = CalculateWallRotation(firstPolePosition, secondPolePos);

            // ✅ Calculate segments with perfect mesh-based fitting
            List<WallSegmentData> segmentData = CalculateWallSegmentsWithScaling(firstPolePosition, secondPolePos);
            requiredSegments = segmentData.Count;

            UpdateLinePreview(firstPolePosition, secondPolePos);
            UpdateWallSegmentPreviews(segmentData);
            CalculateTotalCost();

            // Check affordability AFTER calculating costs
            canAfford = resourceService != null && resourceService.CanAfford(totalCost);

            // Determine material: show invalid (red) if overlapping OR can't afford OR not visible
            bool isInvalid = overlapsWall || overlapsBuilding || !canAfford || notVisible;
            Material previewMat = isInvalid ? invalidPreviewMaterial : validPreviewMaterial;

            // Determine line color: cyan if snapped, red if invalid, green if valid
            Color lineColor = isSnappedToWall ? Color.cyan : (isInvalid ? invalidLineColor : validLineColor);
            linePreviewRenderer.startColor = lineColor;
            linePreviewRenderer.endColor = lineColor;

            SetPreviewMaterial(currentPolePreview, previewMat);
            foreach (var preview in wallSegmentPreviews)
            {
                SetPreviewMaterial(preview, previewMat);
            }
        }

        /// <summary>
        /// Helper struct to store position AND scale for each wall segment.
        /// </summary>
        private struct WallSegmentData
        {
            public Vector3 position;
            public Vector3 scale;
            public float length; // world-space length along the wall axis

            public WallSegmentData(Vector3 pos, Vector3 scl, float len)
            {
                position = pos;
                scale = scl;
                length = len;
            }
        }

        /// <summary>
        /// ✅ Calculate wall segments with perfect end-to-end placement and adaptive scaling.
        /// Last segment scales to fill remaining distance exactly.
        /// </summary>
        private List<WallSegmentData> CalculateWallSegmentsWithScaling(Vector3 start, Vector3 end)
        {
            List<WallSegmentData> segments = new List<WallSegmentData>();

            Vector3 diff = end - start;
            float totalDistance = diff.magnitude;

            if (totalDistance < wallMeshLength * minScaleFactor)
                return segments; // Too short for even a minimum scaled piece

            Vector3 direction = diff.normalized;

            // Original prefab scale (we only scale the length axis)
            Vector3 baseScale = currentWallData.buildingPrefab.transform.localScale;

            // Number of full-size segments that fit
            int fullCount = Mathf.FloorToInt(totalDistance / wallMeshLength);

            float used = fullCount * wallMeshLength;
            float remaining = totalDistance - used;

            // Determine which axis we scale
            int axisIndex = 0;
            switch (wallLengthAxis)
            {
                case WallLengthAxis.X: axisIndex = 0; break;
                case WallLengthAxis.Y: axisIndex = 1; break;
                case WallLengthAxis.Z: axisIndex = 2; break;
            }

            // 1) Place all full-size segments
            for (int i = 0; i < fullCount; i++)
            {
                float centerOffset = (i * wallMeshLength) + (wallMeshLength * 0.5f);
                Vector3 pos = start + direction * centerOffset;

                // full-size: scale stays as baseScale, length is wallMeshLength (world units)
                segments.Add(new WallSegmentData(pos, baseScale, wallMeshLength));
            }

            // 2) Handle final SCALING segment
            if (remaining > wallMeshLength * minScaleFactor)
            {
                float scaleFactor = remaining / wallMeshLength;

                float centerOffset = used + (remaining * 0.5f);
                Vector3 pos = start + direction * centerOffset;

                Vector3 scaled = baseScale;
                scaled[axisIndex] = baseScale[axisIndex] * scaleFactor;

                // length is remaining (world units)
                segments.Add(new WallSegmentData(pos, scaled, remaining));
            }

            return segments;
        }


        private Quaternion CalculateWallRotation(Vector3 start, Vector3 end)
        {
            Vector3 direction = end - start;

            if (direction.sqrMagnitude < 0.01f)
                return Quaternion.identity;

            direction.y = 0;
            direction.Normalize();

            if (direction != Vector3.zero)
            {
                return Quaternion.LookRotation(direction);
            }

            return Quaternion.identity;
        }

        private void UpdateLinePreview(Vector3 start, Vector3 end)
        {
            if (linePreviewRenderer == null) return;

            linePreviewRenderer.enabled = true;
            linePreviewRenderer.positionCount = 2;
            linePreviewRenderer.SetPosition(0, start + Vector3.up * 0.5f);
            linePreviewRenderer.SetPosition(1, end + Vector3.up * 0.5f);
        }

        private void UpdateWallSegmentPreviews(List<WallSegmentData> segmentData)
        {
            while (wallSegmentPreviews.Count > segmentData.Count)
            {
                int lastIndex = wallSegmentPreviews.Count - 1;
                if (wallSegmentPreviews[lastIndex] != null) Destroy(wallSegmentPreviews[lastIndex]);
                wallSegmentPreviews.RemoveAt(lastIndex);
            }

            if (currentWallData == null || currentWallData.buildingPrefab == null) return;

            Vector3 prefabEulerOffset = currentWallData.buildingPrefab.transform.localEulerAngles;
            Vector3 baseEuler = currentWallRotation.eulerAngles;
            Quaternion finalRotation = Quaternion.Euler(baseEuler + prefabEulerOffset);

            for (int i = 0; i < segmentData.Count; i++)
            {
                WallSegmentData data = segmentData[i];
                if (i < wallSegmentPreviews.Count)
                {
                    var preview = wallSegmentPreviews[i];
                    if (preview != null)
                    {
                        preview.transform.position = data.position;
                        preview.transform.rotation = finalRotation;
                        preview.transform.localScale = data.scale;
                    }
                }
                else
                {
                    // Instantiate as INACTIVE to prevent VisionProvider.OnEnable() from running
                    GameObject preview = Instantiate(currentWallData.buildingPrefab, data.position, finalRotation, this.transform);
                    preview.SetActive(false);
                    preview.transform.localScale = data.scale;

                    // Destroy VisionProvider on preview to prevent fog of war reveal
                    // Use DestroyImmediate because Destroy() only marks for deletion at end of frame
                    var providers = preview.GetComponents<MonoBehaviour>();

                    foreach (var c in providers)
                    {
                        if (c is KingdomsAtDusk.FogOfWar.IVisionProvider)
                            DestroyImmediate(c);
                    }


                    var building = preview.GetComponent<Building>();
                    if (building != null) building.enabled = false;

                    var wallSystem = preview.GetComponent<WallConnectionSystem>();
                    if (wallSystem != null) wallSystem.enabled = false;

                    // Reactivate preview now that components are cleaned up
                    preview.SetActive(true);

                    foreach (var col in preview.GetComponentsInChildren<Collider>())
                        col.enabled = false;

                    SetPreviewMaterial(preview, canAfford ? validPreviewMaterial : invalidPreviewMaterial);
                    wallSegmentPreviews.Add(preview);
                }
            }
        }

        private void CalculateTotalCost()
        {
            totalCost.Clear();

            if (currentWallData == null || requiredSegments == 0) return;

            var singleWallCost = currentWallData.GetCosts();

            foreach (var cost in singleWallCost)
            {
                totalCost[cost.Key] = cost.Value * requiredSegments;
            }
        }

        private void HandleWallPlacementInput()
        {
            if (mouse == null) return;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (!firstPoleSet)
                {
                    PlaceFirstPole();
                }
                else
                {
                    if (canAfford)
                    {
                        PlaceWallSegments();
                    }
                    else
                    {
                        Debug.Log("Not enough resources to build walls!");
                        EventBus.Publish(new ResourcesSpentEvent(
                            totalCost.GetValueOrDefault(ResourceType.Wood, 0),
                            totalCost.GetValueOrDefault(ResourceType.Food, 0),
                            totalCost.GetValueOrDefault(ResourceType.Gold, 0),
                            totalCost.GetValueOrDefault(ResourceType.Stone, 0),
                            false
                        ));
                    }
                }
            }

            if (mouse.rightButton.wasPressedThisFrame ||
                (keyboard != null && keyboard.escapeKey.wasPressedThisFrame))
            {
                CancelWallPlacement();
                Debug.Log("Wall placement canceled");
            }
        }

        private void PlaceFirstPole()
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            if (mouseWorldPos == Vector3.zero) return;

            firstPolePosition = SnapToGrid(mouseWorldPos);

            // ✅ CHECK FOG OF WAR: Only allow placing first pole in currently visible areas
            if (null != null)
            {
                if (!null.IsVisible(firstPolePosition))
                {
                    EventBus.Publish(new BuildingPlacementFailedEvent("Cannot place wall in unexplored or hidden areas!"));
                    return;
                }
            }

            firstPoleSet = true;

            if (polePrefab != null)
            {
                firstPoleVisual = Instantiate(polePrefab, firstPolePosition + Vector3.up * (poleHeight / 2f), Quaternion.identity);
            }
            else
            {
                firstPoleVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                firstPoleVisual.transform.position = firstPolePosition + Vector3.up * (poleHeight / 2f);
                firstPoleVisual.transform.localScale = new Vector3(0.3f, poleHeight / 2f, 0.3f);
            }

            var collider = firstPoleVisual.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            Debug.Log($"First pole placed at {firstPolePosition}");
        }

        private void PlaceWallSegments()
        {


            if (!canAfford || requiredSegments == 0 || currentWallData == null)
            {
                Debug.Log("Cannot place walls!");
                return;
            }

            bool success = resourceService.SpendResources(totalCost);
            if (!success)
            {
                Debug.LogError("Failed to spend resources!");
                return;
            }

            Vector3 mouseWorld = GetMouseWorldPosition();
            if (mouseWorld == Vector3.zero)
            {
                Debug.LogWarning("PlaceWallSegments: mouse world position invalid.");
                return;
            }

            Vector3 secondPolePos = SnapToGrid(mouseWorld);
            bool snappedToExistingWall = TrySnapToNearbyWall(secondPolePos, out Vector3 finalSecondPolePos);
            secondPolePos = finalSecondPolePos;

            // ✅ CHECK FOG OF WAR: Only allow placement in currently visible areas
            if (null != null)
            {
                bool firstPoleVisible = null.IsVisible(firstPolePosition);
                bool secondPoleVisible = null.IsVisible(secondPolePos);
                if (!firstPoleVisible || !secondPoleVisible)
                {
                    EventBus.Publish(new BuildingPlacementFailedEvent("Cannot place wall in unexplored or hidden areas!"));
                    return;
                }
            }

            if (WouldOverlapExistingWall(firstPolePosition, secondPolePos))
            {
                EventBus.Publish(new BuildingPlacementFailedEvent("Cannot place wall: overlaps existing wall!"));
                return;
            }

            if (WouldOverlapBuildings(firstPolePosition, secondPolePos))
            {
                EventBus.Publish(new BuildingPlacementFailedEvent("Cannot place wall: overlaps existing building!"));
                return;
            }

            Quaternion baseRotation = CalculateWallRotation(firstPolePosition, secondPolePos);
            Vector3 baseEuler = baseRotation.eulerAngles;
            Vector3 prefabEulerOffset = currentWallData.buildingPrefab.transform.localEulerAngles;
            Vector3 finalEuler = baseEuler + prefabEulerOffset;
            Quaternion finalRotation = Quaternion.Euler(finalEuler);

            // ✅ Calculate segments with perfect scaling
            List<WallSegmentData> segmentData = CalculateWallSegmentsWithScaling(firstPolePosition, secondPolePos);

            float totalDist = Vector3.Distance(firstPolePosition, secondPolePos);
            Debug.Log($"[PlaceWalls] Distance: {totalDist:F2}m, Segments: {segmentData.Count}, Mesh Length: {wallMeshLength:F2}m");

            // Instantiate walls with proper scaling
            foreach (var data in segmentData)
            {
                GameObject newWall = Instantiate(currentWallData.buildingPrefab, data.position, finalRotation);
                newWall.transform.localScale = data.scale; // ✅ Apply scale

                if (newWall.TryGetComponent(out Building buildingComponent))
                {
                    buildingComponent.SetData(currentWallData);
                }

                // ✅ Add NavMeshObstacle for navigation blocking (it will calculate bounds from collider before we disable it)
                if (newWall.GetComponent<WallNavMeshObstacle>() == null)
                {
                    newWall.AddComponent<WallNavMeshObstacle>();
                }

                // ✅ Disable colliders on placed walls - they're not needed for gameplay
                // NavMeshObstacle has already calculated its bounds from the collider
                foreach (var col in newWall.GetComponentsInChildren<Collider>())
                {
                    col.enabled = false;
                }

                // Track placed wall segments for overlap detection
                placedWallSegments.Add(new PlacedWallSegment(data.position, data.length, finalRotation));
                placedWallPositions.Add(data.position);
            }

            // Publish events
            foreach (var data in segmentData)
            {
                Collider[] hits = Physics.OverlapSphere(data.position, 0.1f);
                foreach (var hit in hits)
                {
                    if (hit.GetComponent<WallConnectionSystem>() != null)
                    {
                        EventBus.Publish(new BuildingPlacedEvent(hit.gameObject, data.position));
                        break;
                    }
                }
            }

            Debug.Log($"✅ Placed {segmentData.Count} wall segments with perfect fit!");

            EventBus.Publish(new ResourcesSpentEvent(
                totalCost.GetValueOrDefault(ResourceType.Wood, 0),
                totalCost.GetValueOrDefault(ResourceType.Food, 0),
                totalCost.GetValueOrDefault(ResourceType.Gold, 0),
                totalCost.GetValueOrDefault(ResourceType.Stone, 0),
                true
            ));

            if (snappedToExistingWall && autoCompleteOnSnap)
            {
                Debug.Log("🔒 Wall loop completed! Auto-canceling placement.");
                CancelWallPlacement();
            }
            else
            {
                ContinueWallChain(secondPolePos);
            }
        }

        private void ContinueWallChain(Vector3 newFirstPolePos)
        {
            if (firstPoleVisual != null)
            {
                Destroy(firstPoleVisual);
                firstPoleVisual = null;
            }

            foreach (var preview in wallSegmentPreviews)
            {
                if (preview != null)
                    Destroy(preview);
            }
            wallSegmentPreviews.Clear();

            if (linePreviewRenderer != null)
                linePreviewRenderer.enabled = false;

            firstPolePosition = newFirstPolePos;

            if (polePrefab != null)
            {
                firstPoleVisual = Instantiate(polePrefab, firstPolePosition + Vector3.up * (poleHeight / 2f), Quaternion.identity);
            }
            else
            {
                firstPoleVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                firstPoleVisual.transform.position = firstPolePosition + Vector3.up * (poleHeight / 2f);
                firstPoleVisual.transform.localScale = new Vector3(0.3f, poleHeight / 2f, 0.3f);
            }

            var collider = firstPoleVisual.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            firstPoleSet = true;
            requiredSegments = 0;
            totalCost.Clear();

            Debug.Log($"Continuing wall chain from {firstPolePosition}");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Calculate the actual start and end positions of an existing wall considering its scale.
        /// This ensures seamless connections even when walls are scaled.
        /// </summary>
        private bool TryGetWallEndpoints(WallConnectionSystem wallSystem, out Vector3 startPos, out Vector3 endPos)
        {
            startPos = Vector3.zero;
            endPos = Vector3.zero;

            if (wallSystem == null) return false;

            Transform wallTransform = wallSystem.transform;
            Vector3 center = wallTransform.position;
            Quaternion rotation = wallTransform.rotation;
            Vector3 scale = wallTransform.localScale;

            // Detect the wall's mesh length along the configured axis
            float meshLength = DetectWallMeshLength(wallTransform.gameObject);

            // Get the scale factor along the wall length axis
            float scaleFactor = 1f;
            switch (wallLengthAxis)
            {
                case WallLengthAxis.X:
                    scaleFactor = scale.x;
                    break;
                case WallLengthAxis.Y:
                    scaleFactor = scale.y;
                    break;
                case WallLengthAxis.Z:
                    scaleFactor = scale.z;
                    break;
            }

            // Calculate actual world-space length considering scale
            float worldLength = meshLength * scaleFactor;

            // Calculate direction vector along the wall length axis
            Vector3 dir = Vector3.forward;
            switch (wallLengthAxis)
            {
                case WallLengthAxis.X:
                    dir = rotation * Vector3.right;
                    break;
                case WallLengthAxis.Y:
                    dir = rotation * Vector3.up;
                    break;
                case WallLengthAxis.Z:
                    dir = rotation * Vector3.forward;
                    break;
            }

            // Calculate endpoints
            startPos = center - dir * (worldLength / 2f);
            endPos = center + dir * (worldLength / 2f);

            return true;
        }

        /// <summary>
        /// Overload that works with GameObject instead of WallConnectionSystem
        /// </summary>
       
        private bool ShareEndpoint(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2)
        {
            return
                Vector3.Distance(a1, b1) < 0.05f ||
                Vector3.Distance(a1, b2) < 0.05f ||
                Vector3.Distance(a2, b1) < 0.05f ||
                Vector3.Distance(a2, b2) < 0.05f;
        }

        private Vector3 GetMouseWorldPosition()
        {
            if (mouse == null) return Vector3.zero;

            Vector2 mousePos = mouse.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
            {
                return hit.point;
            }

            return Vector3.zero;
        }

        private Vector3 SnapToGrid(Vector3 position)
        {
            if (!useGridSnapping) return position;

            position.x = Mathf.Round(position.x / gridSize) * gridSize;
            position.z = Mathf.Round(position.z / gridSize) * gridSize;
            return position;
        }

        private bool TrySnapToNearbyWall(Vector3 position, out Vector3 snappedPosition)
        {
            snappedPosition = position;
            float closestDistance = float.MaxValue;
            bool found = false;

            // Check against walls we're currently placing (placedWallSegments)
            foreach (var seg in placedWallSegments)
            {
                Vector3 start = seg.GetStartPosition(wallLengthAxis);
                Vector3 end = seg.GetEndPosition(wallLengthAxis);

                float distStart = Vector3.Distance(position, start);
                float distEnd = Vector3.Distance(position, end);

                // Midpoint snapping
                Vector3 midpoint = (start + end) * 0.5f;
                float distMid = Vector3.Distance(position, midpoint);

                if (distStart < wallSnapDistance && distStart < closestDistance)
                {
                    closestDistance = distStart;
                    snappedPosition = start;
                    found = true;
                }

                if (distEnd < wallSnapDistance && distEnd < closestDistance)
                {
                    closestDistance = distEnd;
                    snappedPosition = end;
                    found = true;
                }

                if (distMid < wallSnapDistance && distMid < closestDistance)
                {
                    closestDistance = distMid;
                    snappedPosition = midpoint;
                    found = true;
                }
            }

            // Also check for existing walls in the scene (not just current placement)
            Collider[] nearbyColliders = Physics.OverlapSphere(position, wallSnapDistance, ~groundLayer);

            foreach (var col in nearbyColliders)
            {
                // Ignore preview objects
                if (col.transform.IsChildOf(transform))
                    continue;

                // Ignore terrain
                if (col is TerrainCollider)
                    continue;

                // Check if it's a wall
                WallConnectionSystem wallSystem = col.GetComponentInParent<WallConnectionSystem>();
                if (wallSystem != null)
                {
                    // ✅ Calculate actual endpoints considering the wall's scale
                    if (TryGetWallEndpoints(wallSystem, out Vector3 wallStart, out Vector3 wallEnd))
                    {
                        Vector3 wallCenter = wallSystem.transform.position;
                        Vector3 wallMidpoint = (wallStart + wallEnd) * 0.5f;

                        // Check distance to start endpoint
                        float distStart = Vector3.Distance(position, wallStart);
                        if (distStart < wallSnapDistance && distStart < closestDistance)
                        {
                            closestDistance = distStart;
                            snappedPosition = wallStart;
                            found = true;
                        }

                        // Check distance to end endpoint
                        float distEnd = Vector3.Distance(position, wallEnd);
                        if (distEnd < wallSnapDistance && distEnd < closestDistance)
                        {
                            closestDistance = distEnd;
                            snappedPosition = wallEnd;
                            found = true;
                        }

                        // Check distance to midpoint
                        float distMid = Vector3.Distance(position, wallMidpoint);
                        if (distMid < wallSnapDistance && distMid < closestDistance)
                        {
                            closestDistance = distMid;
                            snappedPosition = wallMidpoint;
                            found = true;
                        }
                    }
                    else
                    {
                        // Fallback to center if we can't calculate endpoints
                        Vector3 wallPos = wallSystem.transform.position;
                        float dist = Vector3.Distance(position, wallPos);

                        if (dist < wallSnapDistance && dist < closestDistance)
                        {
                            closestDistance = dist;
                            snappedPosition = wallPos;
                            found = true;
                        }
                    }
                }
            }

            return found;
        }



        private void SetPreviewMaterial(GameObject obj, Material material)
        {
            if (obj == null || material == null) return;

            var renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    // ✅ FIX: Use sharedMaterial to avoid creating instances during render pass
                    renderer.sharedMaterial = material;
                }
            }
        }

        #endregion

        #region Debug

        private void OnDrawGizmos()
        {
            if (!isPlacingWall) return;

            if (firstPoleSet)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(firstPolePosition + Vector3.up * 0.5f, 0.3f);
            }

            if (wallSegmentPreviews.Count > 0)
            {
                Gizmos.color = canAfford ? Color.green : Color.red;
                foreach (var preview in wallSegmentPreviews)
                {
                    if (preview != null)
                    {
                        Gizmos.DrawWireCube(preview.transform.position, preview.transform.localScale);
                    }
                }
            }

            Gizmos.color = Color.blue;
            foreach (var wallPos in placedWallPositions)
            {
                Gizmos.DrawSphere(wallPos + Vector3.up * 0.5f, 0.15f);
            }

            if (isSnappedToWall)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(snappedWallPosition + Vector3.up * 0.5f, 0.5f);
                Gizmos.DrawLine(snappedWallPosition, snappedWallPosition + Vector3.up * 2f);
            }
        }

        #endregion
    }
}