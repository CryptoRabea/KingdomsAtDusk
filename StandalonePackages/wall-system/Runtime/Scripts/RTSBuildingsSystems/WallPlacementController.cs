using UnityEngine;
using UnityEngine.InputSystem;
using RTS.Core.Services;
using RTS.Core.Events;
using System.Collections.Generic;

namespace RTS.Buildings
{
    /// <summary>
    /// Handles pole-to-pole wall placement with preview and resource counting.
    /// First click places a pole, second click places all wall segments between poles.
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
        [SerializeField] private float gridSize = 1f;
        [SerializeField] private bool useGridSnapping = true;
        [SerializeField] private LayerMask wallLayer; // Layer for walls to exclude from overlap check

        [Header("Pole Settings")]
        [SerializeField] private GameObject polePrefab; // Pole visual prefab
        [SerializeField] private float poleHeight = 2f;

        [Header("Wall Segment Settings")]
        [SerializeField] private bool autoCalculateSegmentSize = true;
        private float wallSegmentSize = 1f; // Calculated from mesh bounds

        // State
        private bool isPlacingWall = false;
        private bool firstPoleSet = false;
        private Vector3 firstPolePosition;
        private GameObject firstPoleVisual;
        private GameObject currentPolePreview;
        private List<GameObject> wallSegmentPreviews = new List<GameObject>();

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

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            mouse = Mouse.current;
            keyboard = Keyboard.current;

            // Setup line renderer
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

        /// <summary>
        /// Start placing walls with pole-to-pole system.
        /// </summary>
        public void StartPlacingWalls(BuildingDataSO wallData)
        {
            if (wallData == null || wallData.buildingPrefab == null)
            {
                Debug.LogError("Invalid wall data!");
                return;
            }

            // Cancel any existing placement
            CancelWallPlacement();

            currentWallData = wallData;
            isPlacingWall = true;
            firstPoleSet = false;

            // Calculate wall segment size from mesh bounds
            if (autoCalculateSegmentSize)
            {
                CalculateWallSegmentSize();
            }

            // Create current pole preview
            CreatePolePreview();

            Debug.Log($"Started placing walls: {wallData.buildingName} (segment size: {wallSegmentSize})");
        }

        /// <summary>
        /// Cancel current wall placement.
        /// </summary>
        public void CancelWallPlacement()
        {
            // Destroy first pole visual
            if (firstPoleVisual != null)
            {
                Destroy(firstPoleVisual);
                firstPoleVisual = null;
            }

            // Destroy pole preview
            if (currentPolePreview != null)
            {
                Destroy(currentPolePreview);
                currentPolePreview = null;
            }

            // Destroy all wall segment previews
            foreach (var preview in wallSegmentPreviews)
            {
                if (preview != null)
                    Destroy(preview);
            }
            wallSegmentPreviews.Clear();

            // Hide line renderer
            if (linePreviewRenderer != null)
                linePreviewRenderer.enabled = false;

            isPlacingWall = false;
            firstPoleSet = false;
            currentWallData = null;
            requiredSegments = 0;
            totalCost.Clear();
        }

        /// <summary>
        /// Check if currently placing walls.
        /// </summary>
        public bool IsPlacingWalls => isPlacingWall;

        /// <summary>
        /// Get the total cost for the current wall segments.
        /// </summary>
        public Dictionary<ResourceType, int> GetTotalCost() => new Dictionary<ResourceType, int>(totalCost);

        /// <summary>
        /// Get the number of required segments.
        /// </summary>
        public int GetRequiredSegments() => requiredSegments;

        #endregion

        #region Placement Logic

        private void CalculateWallSegmentSize()
        {
            if (currentWallData == null || currentWallData.buildingPrefab == null)
            {
                wallSegmentSize = gridSize;
                return;
            }

            // Get the mesh bounds to determine the actual size of the wall segment
            Renderer[] renderers = currentWallData.buildingPrefab.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }

                // Use the larger of X or Z axis as segment size (walls extend along one axis)
                wallSegmentSize = Mathf.Max(bounds.size.x, bounds.size.z);

                // Round to nearest grid size
                wallSegmentSize = Mathf.Round(wallSegmentSize / gridSize) * gridSize;

                // Ensure minimum size
                if (wallSegmentSize < gridSize)
                    wallSegmentSize = gridSize;

                Debug.Log($"Wall segment size calculated: {wallSegmentSize} (from bounds: {bounds.size})");
            }
            else
            {
                // Fallback to grid size
                wallSegmentSize = gridSize;
                Debug.LogWarning("No renderers found on wall prefab, using default grid size");
            }
        }

        private void CreatePolePreview()
        {
            if (polePrefab != null)
            {
                currentPolePreview = Instantiate(polePrefab);
            }
            else
            {
                // Create a simple pole if no prefab is provided
                currentPolePreview = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                currentPolePreview.transform.localScale = new Vector3(0.3f, poleHeight / 2f, 0.3f);
            }

            // Disable collider on preview
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

            // Snap to grid
            Vector3 snappedPos = SnapToGrid(mouseWorldPos);

            if (!firstPoleSet)
            {
                // Update current pole preview position
                if (currentPolePreview != null)
                {
                    currentPolePreview.transform.position = snappedPos + Vector3.up * (poleHeight / 2f);
                }
            }
            else
            {
                // Update second pole position and show wall preview
                UpdateWallPreview(snappedPos);
            }
        }

        private void UpdateWallPreview(Vector3 secondPolePos)
        {
            // Update current pole preview
            if (currentPolePreview != null)
            {
                currentPolePreview.transform.position = secondPolePos + Vector3.up * (poleHeight / 2f);
            }

            // Calculate wall segments needed
            List<Vector3> segmentPositions = CalculateWallSegments(firstPolePosition, secondPolePos);
            requiredSegments = segmentPositions.Count;

            // Update line preview
            UpdateLinePreview(firstPolePosition, secondPolePos);

            // Update wall segment previews
            UpdateWallSegmentPreviews(segmentPositions);

            // Calculate total cost
            CalculateTotalCost();

            // Check if player can afford it
            canAfford = resourceService != null && resourceService.CanAfford(totalCost);

            // Update visual based on affordability
            Color lineColor = canAfford ? validLineColor : invalidLineColor;
            linePreviewRenderer.startColor = lineColor;
            linePreviewRenderer.endColor = lineColor;

            Material previewMat = canAfford ? validPreviewMaterial : invalidPreviewMaterial;
            SetPreviewMaterial(currentPolePreview, previewMat);
            foreach (var preview in wallSegmentPreviews)
            {
                SetPreviewMaterial(preview, previewMat);
            }
        }

        private List<Vector3> CalculateWallSegments(Vector3 start, Vector3 end)
        {
            List<Vector3> segments = new List<Vector3>();

            // Calculate direction and distance
            Vector3 direction = end - start;
            float distance = direction.magnitude;

            // Determine if wall is horizontal or vertical based on dominant axis
            bool isHorizontal = Mathf.Abs(direction.x) > Mathf.Abs(direction.z);

            // Use wallSegmentSize for proper spacing
            float segmentSize = wallSegmentSize;

            if (isHorizontal)
            {
                // Horizontal wall (along X axis)
                int startX = Mathf.RoundToInt(start.x / segmentSize);
                int endX = Mathf.RoundToInt(end.x / segmentSize);
                int step = startX < endX ? 1 : -1;

                for (int x = startX; x != endX; x += step)
                {
                    Vector3 segmentPos = new Vector3(x * segmentSize, start.y, start.z);
                    segments.Add(segmentPos);
                }
            }
            else
            {
                // Vertical wall (along Z axis)
                int startZ = Mathf.RoundToInt(start.z / segmentSize);
                int endZ = Mathf.RoundToInt(end.z / segmentSize);
                int step = startZ < endZ ? 1 : -1;

                for (int z = startZ; z != endZ; z += step)
                {
                    Vector3 segmentPos = new Vector3(start.x, start.y, z * segmentSize);
                    segments.Add(segmentPos);
                }
            }

            return segments;
        }

        private void UpdateLinePreview(Vector3 start, Vector3 end)
        {
            if (linePreviewRenderer == null) return;

            linePreviewRenderer.enabled = true;
            linePreviewRenderer.positionCount = 2;
            linePreviewRenderer.SetPosition(0, start + Vector3.up * 0.5f);
            linePreviewRenderer.SetPosition(1, end + Vector3.up * 0.5f);
        }

        private void UpdateWallSegmentPreviews(List<Vector3> segmentPositions)
        {
            // Remove excess previews
            while (wallSegmentPreviews.Count > segmentPositions.Count)
            {
                int lastIndex = wallSegmentPreviews.Count - 1;
                if (wallSegmentPreviews[lastIndex] != null)
                    Destroy(wallSegmentPreviews[lastIndex]);
                wallSegmentPreviews.RemoveAt(lastIndex);
            }

            // Add or update previews
            for (int i = 0; i < segmentPositions.Count; i++)
            {
                if (i < wallSegmentPreviews.Count)
                {
                    // Update existing preview
                    wallSegmentPreviews[i].transform.position = segmentPositions[i];
                }
                else
                {
                    // Create new preview
                    GameObject preview = Instantiate(currentWallData.buildingPrefab, segmentPositions[i], Quaternion.identity);

                    // Disable scripts on preview
                    var building = preview.GetComponent<Building>();
                    if (building != null)
                        building.enabled = false;

                    var wallSystem = preview.GetComponent<WallConnectionSystem>();
                    if (wallSystem != null)
                        wallSystem.enabled = false;

                    // Disable colliders
                    var colliders = preview.GetComponentsInChildren<Collider>();
                    foreach (var col in colliders)
                        col.enabled = false;

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

            // Left click to place pole or confirm wall placement
            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (!firstPoleSet)
                {
                    // Place first pole
                    PlaceFirstPole();
                }
                else
                {
                    // Place all wall segments
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

            // Right click or ESC to cancel
            if (mouse.rightButton.wasPressedThisFrame ||
                (keyboard != null && keyboard.escapeKey.wasPressedThisFrame))
            {
                if (firstPoleSet)
                {
                    // Cancel second pole, go back to first pole placement
                    ResetToFirstPole();
                }
                else
                {
                    // Cancel entire wall placement
                    CancelWallPlacement();
                }
            }
        }

        private void PlaceFirstPole()
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            if (mouseWorldPos == Vector3.zero) return;

            firstPolePosition = SnapToGrid(mouseWorldPos);
            firstPoleSet = true;

            // Create visual pole at first position
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

            // Disable collider on pole
            var collider = firstPoleVisual.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            Debug.Log($"First pole placed at {firstPolePosition}");
        }

        private void ResetToFirstPole()
        {
            // Remove first pole visual
            if (firstPoleVisual != null)
            {
                Destroy(firstPoleVisual);
                firstPoleVisual = null;
            }

            // Clear wall previews
            foreach (var preview in wallSegmentPreviews)
            {
                if (preview != null)
                    Destroy(preview);
            }
            wallSegmentPreviews.Clear();

            // Hide line renderer
            if (linePreviewRenderer != null)
                linePreviewRenderer.enabled = false;

            firstPoleSet = false;
            requiredSegments = 0;
            totalCost.Clear();

            Debug.Log("Reset to first pole placement");
        }

        private void PlaceWallSegments()
        {
            if (!canAfford || requiredSegments == 0 || currentWallData == null)
            {
                Debug.Log("Cannot place walls!");
                return;
            }

            // Calculate wall segments
            List<Vector3> segmentPositions = CalculateWallSegments(firstPolePosition, SnapToGrid(GetMouseWorldPosition()));

            // Validate all positions before spending resources
            List<GameObject> placedWalls = new List<GameObject>();
            foreach (var position in segmentPositions)
            {
                if (!IsValidWallPosition(position, placedWalls))
                {
                    Debug.LogWarning($"Cannot place wall at {position} - position is blocked!");

                    // Clean up any walls we already placed
                    foreach (var wall in placedWalls)
                    {
                        if (wall != null)
                            Destroy(wall);
                    }

                    return;
                }
            }

            // Spend resources
            bool success = resourceService.SpendResources(totalCost);
            if (!success)
            {
                Debug.LogError("Failed to spend resources!");
                return;
            }

            // Place all wall segments
            foreach (var position in segmentPositions)
            {
                GameObject newWall = Instantiate(currentWallData.buildingPrefab, position, Quaternion.identity);

                // Set data reference
                var buildingComponent = newWall.GetComponent<Building>();
                if (buildingComponent != null)
                {
                    buildingComponent.SetData(currentWallData);
                }

                placedWalls.Add(newWall);

                // Publish placement event for each segment
                EventBus.Publish(new BuildingPlacedEvent(newWall, position));
            }

            Debug.Log($"✅ Placed {segmentPositions.Count} wall segments. Total cost: {string.Join(", ", totalCost)}");

            // Publish resource spent event
            EventBus.Publish(new ResourcesSpentEvent(
                totalCost.GetValueOrDefault(ResourceType.Wood, 0),
                totalCost.GetValueOrDefault(ResourceType.Food, 0),
                totalCost.GetValueOrDefault(ResourceType.Gold, 0),
                totalCost.GetValueOrDefault(ResourceType.Stone, 0),
                true
            ));

            // Reset for next wall placement
            ResetToFirstPole();
        }

        /// <summary>
        /// Check if a wall can be placed at the given position.
        /// Excludes other walls from the overlap check.
        /// </summary>
        private bool IsValidWallPosition(Vector3 position, List<GameObject> existingWalls)
        {
            if (currentWallData == null || currentWallData.buildingPrefab == null)
                return false;

            // Get bounds from wall prefab
            Bounds bounds = GetWallBounds(currentWallData.buildingPrefab);
            bounds.center = position + (bounds.center - currentWallData.buildingPrefab.transform.position);

            // Check for overlapping objects (excluding ground and walls)
            Collider[] colliders = Physics.OverlapBox(
                bounds.center,
                bounds.extents,
                Quaternion.identity,
                ~(groundLayer | wallLayer)
            );

            foreach (var col in colliders)
            {
                // Skip if it's one of the walls we just placed
                bool isExistingWall = false;
                foreach (var wall in existingWalls)
                {
                    if (wall != null && (col.gameObject == wall || col.transform.IsChildOf(wall.transform)))
                    {
                        isExistingWall = true;
                        break;
                    }
                }

                if (isExistingWall)
                    continue;

                // Skip terrain colliders
                if (col is TerrainCollider)
                    continue;

                // Found a blocking object
                Debug.Log($"Wall position blocked by: {col.gameObject.name} at {position}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get the bounds of a wall prefab.
        /// </summary>
        private Bounds GetWallBounds(GameObject wallPrefab)
        {
            // Try to get bounds from collider
            Collider col = wallPrefab.GetComponent<Collider>();
            if (col != null)
            {
                return col.bounds;
            }

            // Fallback: Use renderer bounds
            Renderer[] renderers = wallPrefab.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
                return bounds;
            }

            // Last resort: Use transform scale
            return new Bounds(wallPrefab.transform.position, wallPrefab.transform.localScale);
        }

        #endregion

        #region Helper Methods

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

            // Snap to wall segment size for proper alignment
            float snapSize = wallSegmentSize > 0 ? wallSegmentSize : gridSize;
            position.x = Mathf.Round(position.x / snapSize) * snapSize;
            position.z = Mathf.Round(position.z / snapSize) * snapSize;
            return position;
        }

        private void SetPreviewMaterial(GameObject obj, Material material)
        {
            if (obj == null || material == null) return;

            var renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.material = material;
                }
            }
        }

        #endregion

        #region Debug

        private void OnDrawGizmos()
        {
            if (!isPlacingWall) return;

            // Draw first pole position
            if (firstPoleSet)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(firstPolePosition + Vector3.up * 0.5f, 0.3f);
            }

            // Draw wall segment positions
            if (wallSegmentPreviews.Count > 0)
            {
                Gizmos.color = canAfford ? Color.green : Color.red;
                foreach (var preview in wallSegmentPreviews)
                {
                    if (preview != null)
                    {
                        Gizmos.DrawWireCube(preview.transform.position, Vector3.one * 0.5f);
                    }
                }
            }
        }

        #endregion
    }
}
