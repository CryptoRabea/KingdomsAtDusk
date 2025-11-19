using UnityEngine;
using UnityEngine.InputSystem;
using RTS.Core.Services;
using RTS.Core.Events;
using RTS.Buildings;
using System.Collections.Generic;
using System.Linq;

namespace RTS.Managers
{
    /// <summary>
    /// Manages building placement, construction, and destruction using BuildingDataSO as source of truth.
    /// Handles player input for placing buildings.
    /// </summary>
    public class BuildingManager : MonoBehaviour, IBuildingService
    {
        [Header("References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private LayerMask groundLayer;

        [Header("Placement Settings")]
        [SerializeField] private Material validPlacementMaterial;   // Green material
        [SerializeField] private Material invalidPlacementMaterial; // Red material
        [SerializeField] private float gridSize = 1f;               // Snap to grid
        [SerializeField] private bool useGridSnapping = true;

        [Header("Terrain Validation")]
        [SerializeField] private float maxHeightDifference = 2f;    // Max slope tolerance
        [SerializeField] private bool requireFlatGround = true;      // Enforce flat ground check
        [SerializeField] private int groundSamples = 5;              // Number of ground check points

        [Header("Wall Placement")]
        [SerializeField] private WallPlacementController wallPlacementController;
        [SerializeField] private bool useWallPlacementForWalls = true; // Use pole-to-pole placement for walls

        [Header("Tower Placement")]
        [SerializeField] private TowerPlacementHelper towerPlacementHelper;
        [SerializeField] private bool enableTowerWallSnapping = true; // Enable tower snapping to walls
        [SerializeField] private bool autoReplaceWalls = true; // Automatically replace walls when placing towers

        [Header("Building Data - SOURCE OF TRUTH")]
        [Tooltip("Assign BuildingDataSO assets here, NOT prefabs!")]
        [SerializeField] private BuildingDataSO[] buildingDataArray; // ✅ USE DATA, NOT PREFABS

        // Current state
        private BuildingDataSO currentBuildingData;  // ✅ Store data, not prefab
        private GameObject previewBuilding;
        private bool isPlacingBuilding = false;
        private Material[] originalMaterials;
        private bool canPlace = false;

        // Tower placement state
        private bool isPlacingTower = false;
        private TowerDataSO currentTowerData;
        private GameObject wallToReplace;
        private bool isSnappedToWall = false;

        // Input
        private Mouse mouse;
        private Keyboard keyboard;

        // Services
        private IResourcesService resourceService;

        // Selection Manager
        private RTS.Buildings.BuildingSelectionManager buildingSelectionManager;

        // Public property to check if currently placing a building
        public bool IsPlacingBuilding => isPlacingBuilding;

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            mouse = Mouse.current;
            keyboard = Keyboard.current;
        }

        private void Start()
        {
            resourceService = ServiceLocator.TryGet<IResourcesService>();

            if (resourceService == null)
            {
                Debug.LogError("BuildingManager: ResourceService not available!");
            }

            // Find BuildingSelectionManager
            buildingSelectionManager = Object.FindAnyObjectByType<RTS.Buildings.BuildingSelectionManager>();

            // Validate building data
            ValidateBuildingData();
        }

        private void Update()
        {
            if (isPlacingBuilding)
            {
                UpdateBuildingPreview();
                HandlePlacementInput();
            }
        }

        #region Building Selection

        /// <summary>
        /// Start placing a building by index. Call this when player clicks a building button.
        /// </summary>
        public void StartPlacingBuilding(int buildingIndex)
        {
            if (buildingIndex < 0 || buildingIndex >= buildingDataArray.Length)
            {
                Debug.LogError($"Invalid building index: {buildingIndex}");
                return;
            }

            StartPlacingBuilding(buildingDataArray[buildingIndex]);
        }

        /// <summary>
        /// Start placing a building from BuildingDataSO (SOURCE OF TRUTH).
        /// </summary>
        public void StartPlacingBuilding(BuildingDataSO buildingData)
        {
            if (buildingData == null)
            {
                Debug.LogError("Building data is null!");
                return;
            }

            if (buildingData.buildingPrefab == null)
            {
                Debug.LogError($"Building data '{buildingData.buildingName}' has no prefab assigned!");
                return;
            }

            // Cancel any existing placement
            CancelPlacement();

            // Deselect any currently selected building to prevent UI interference
            if (buildingSelectionManager != null)
            {
                buildingSelectionManager.DeselectBuilding();
            }

            // Check if this is a wall and should use wall placement controller
            bool isWall = buildingData.buildingType == BuildingType.Defensive &&
                          buildingData.buildingPrefab.GetComponent<WallConnectionSystem>() != null;

            if (isWall && useWallPlacementForWalls && wallPlacementController != null)
            {
                // Use wall placement controller for walls
                wallPlacementController.StartPlacingWalls(buildingData);
                Debug.Log($"Started placing walls (pole-to-pole mode): {buildingData.buildingName}");
            }
            else
            {
                // Use regular building placement
                currentBuildingData = buildingData;  // ✅ Store the data
                isPlacingBuilding = true;

                // Check if this is a tower
                if (buildingData is TowerDataSO towerData)
                {
                    isPlacingTower = true;
                    currentTowerData = towerData;
                    Debug.Log($"Started placing tower: {buildingData.buildingName} (Type: {towerData.towerType})");
                }
                else
                {
                    isPlacingTower = false;
                    currentTowerData = null;
                }

                // Create preview from the prefab referenced in the data
                CreateBuildingPreview();

                Debug.Log($"Started placing: {buildingData.buildingName}");
            }
        }

        /// <summary>
        /// Cancel current building placement.
        /// </summary>
        public void CancelPlacement()
        {
            // Cancel wall placement if active
            if (wallPlacementController != null && wallPlacementController.IsPlacingWalls)
            {
                wallPlacementController.CancelWallPlacement();
            }

            // Cancel regular building placement
            if (previewBuilding != null)
            {
                Destroy(previewBuilding);
                previewBuilding = null;
            }

            currentBuildingData = null;
            isPlacingBuilding = false;
            originalMaterials = null;

            // Clear tower-specific state
            isPlacingTower = false;
            currentTowerData = null;
            wallToReplace = null;
            isSnappedToWall = false;
        }

        #endregion

        #region Placement Logic

        private void CreateBuildingPreview()
        {
            if (currentBuildingData == null || currentBuildingData.buildingPrefab == null) return;

            // Instantiate preview as INACTIVE to prevent OnEnable() from running
            // This prevents VisionProvider from registering before we can disable it
            previewBuilding = Instantiate(currentBuildingData.buildingPrefab);
            previewBuilding.SetActive(false);

            // Destroy VisionProvider on preview to prevent fog of war reveal at wrong position
            // Use DestroyImmediate because Destroy() only marks for deletion at end of frame
            var visionProvider = previewBuilding.GetComponent<KingdomsAtDusk.FogOfWar.VisionProvider>();
            if (visionProvider != null)
            {
                DestroyImmediate(visionProvider);
                Debug.Log($"[BuildingManager] Destroyed VisionProvider on preview: {previewBuilding.name}");
            }

            // Disable scripts on preview
            var building = previewBuilding.GetComponent<Building>();
            if (building != null)
                building.enabled = false;

            // Disable BuildingSelectable to prevent preview from being selected
            var buildingSelectable = previewBuilding.GetComponent<BuildingSelectable>();
            if (buildingSelectable != null)
                buildingSelectable.enabled = false;

            // Reactivate preview now that components are cleaned up
            previewBuilding.SetActive(true);

            // Store original materials
            var renderers = previewBuilding.GetComponentsInChildren<Renderer>();
            originalMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    // ✅ FIX: Use sharedMaterial to avoid creating instances
                    originalMaterials[i] = renderers[i].sharedMaterial;
            }

            // Set to semi-transparent
            SetPreviewMaterial(validPlacementMaterial);
        }

        private void UpdateBuildingPreview()
        {
            if (previewBuilding == null || mouse == null) return;

            // Get mouse position in world
            Vector2 mousePos = mouse.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
            {
                // Position building at hit point
                Vector3 position = hit.point;

                // Snap to grid if enabled
                if (useGridSnapping)
                {
                    position.x = Mathf.Round(position.x / gridSize) * gridSize;
                    position.z = Mathf.Round(position.z / gridSize) * gridSize;
                }

                // ✅ TOWER: Check for wall snapping
                if (isPlacingTower && currentTowerData != null && enableTowerWallSnapping && towerPlacementHelper != null)
                {
                    if (towerPlacementHelper.TrySnapToWall(position, currentTowerData, out Vector3 wallSnapPos, out GameObject wall))
                    {
                        position = wallSnapPos;
                        wallToReplace = wall;
                        isSnappedToWall = true;
                    }
                    else
                    {
                        wallToReplace = null;
                        isSnappedToWall = false;
                    }
                }

                // ✅ FIX: Adjust Y position to place building bottom on ground (not pivot)
                position.y = hit.point.y + GetBuildingGroundOffset(previewBuilding);

                previewBuilding.transform.position = position;

                // Check if placement is valid
                canPlace = IsValidPlacement(position);

                // Update preview material (cyan if snapped to wall, green/red otherwise)
                Material previewMat = canPlace ? validPlacementMaterial : invalidPlacementMaterial;
                if (isSnappedToWall && canPlace && wallToReplace != null)
                {
                    // Optional: Use different color for wall replacement (you can create a third material)
                    // For now, just use valid material
                }
                SetPreviewMaterial(previewMat);
            }
        }

        private void HandlePlacementInput()
        {
            if (mouse == null) return;

            // Left click to place
            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (canPlace)
                {
                    PlaceBuilding();
                }
                else
                {
                    Debug.Log("Cannot place building here!");
                }
            }

            // Right click or ESC to cancel
            if (mouse.rightButton.wasPressedThisFrame ||
                (keyboard != null && keyboard.escapeKey.wasPressedThisFrame))
            {
                CancelPlacement();
            }
        }

        private void PlaceBuilding()
        {
            if (previewBuilding == null || currentBuildingData == null) return;

            Vector3 position = previewBuilding.transform.position;
            Quaternion rotation = previewBuilding.transform.rotation;

            // ✅ CHECK COSTS DIRECTLY FROM DATA (not from prefab)
            if (resourceService != null)
            {
                var costs = currentBuildingData.GetCosts();

                if (!resourceService.CanAfford(costs))
                {
                    Debug.Log($"Not enough resources for {currentBuildingData.buildingName}!");

                    // Publish failure event
                    EventBus.Publish(new ResourcesSpentEvent(
                        costs.GetValueOrDefault(ResourceType.Wood, 0),
                        costs.GetValueOrDefault(ResourceType.Food, 0),
                        costs.GetValueOrDefault(ResourceType.Gold, 0),
                        costs.GetValueOrDefault(ResourceType.Stone, 0),
                        false
                    ));

                    return;
                }

                // ✅ SPEND THE RESOURCES!
                bool success = resourceService.SpendResources(costs);
                if (success)
                {
                    Debug.Log($"✅ Spent resources for {currentBuildingData.buildingName}: {string.Join(", ", costs.Select(c => $"{c.Key}:{c.Value}"))}");
                }
                else
                {
                    Debug.LogError("Failed to spend resources even though CanAfford returned true!");
                    return;
                }
            }
            else
            {
                Debug.LogWarning("ResourceService not available, placing building anyway!");
            }

            // ✅ TOWER: Replace wall if snapped to one
            if (isPlacingTower && wallToReplace != null && autoReplaceWalls && isSnappedToWall)
            {
                Debug.Log($"Replacing wall at {wallToReplace.transform.position} with tower {currentTowerData.buildingName}");
                Destroy(wallToReplace);
                wallToReplace = null;
            }

            // ✅ PLACE THE ACTUAL BUILDING (from data's prefab)
            GameObject newBuilding = Instantiate(currentBuildingData.buildingPrefab, position, rotation);

            // ✅ ENSURE BUILDING HAS DATA REFERENCE
            var buildingComponent = newBuilding.GetComponent<Building>();
            if (buildingComponent != null)
            {
                buildingComponent.SetData(currentBuildingData);
                Debug.Log($"✅ Assigned {currentBuildingData.buildingName} data to building component");
            }
            else
            {
                Debug.LogWarning($"Building prefab for {currentBuildingData.buildingName} doesn't have Building component!");
            }

            // ✅ ADD NAVMESH OBSTACLE FOR NAVIGATION BLOCKING
            if (newBuilding.GetComponent<BuildingNavMeshObstacle>() == null)
            {
                newBuilding.AddComponent<BuildingNavMeshObstacle>();
            }

            // ✅ TOWER: Set tower-specific data
            if (isPlacingTower)
            {
                var towerComponent = newBuilding.GetComponent<Tower>();
                if (towerComponent != null && currentTowerData != null)
                {
                    towerComponent.SetTowerData(currentTowerData);
                    Debug.Log($"✅ Assigned {currentTowerData.buildingName} tower data (Type: {currentTowerData.towerType})");
                }
            }

            // Publish success event
            EventBus.Publish(new BuildingPlacedEvent(newBuilding, position));

            Debug.Log($"✅ Placed building: {currentBuildingData.buildingName} at {position}");

            // Cancel placement (or continue placing same building)
            CancelPlacement();

            // Uncomment this to place multiple of same building:
            // StartPlacingBuilding(currentBuildingData);
        }

        private bool IsValidPlacement(Vector3 position)
        {
            if (previewBuilding == null) return false;

            // Get bounds for overlap check
            Bounds buildingBounds = GetBuildingBounds(previewBuilding);

            // Check for overlapping with OTHER BUILDINGS and objects (ignore terrain!)
            // NOTE: buildingBounds.center is already in world space, don't add position again!
            Collider[] colliders = Physics.OverlapBox(
                buildingBounds.center,
                buildingBounds.extents,
                previewBuilding.transform.rotation,
                ~groundLayer // ✅ Exclude ground layer from check
            );

            foreach (var col in colliders)
            {
                // ✅ FIRST: Ignore colliders on the preview building itself (must check BEFORE anything else!)
                if (col.gameObject == previewBuilding || col.transform.IsChildOf(previewBuilding.transform))
                {
                    continue;
                }

                // ✅ SECOND: Ignore terrain colliders specifically (in case terrain is on a different layer)
                if (col is TerrainCollider)
                {
                    continue;
                }

                // ✅ THIRD: Block placement for ANY other collider (buildings, trees, rocks, units, etc.)
                // This includes:
                // - Other buildings (col.GetComponent<Building>() != null)
                // - Any other objects with colliders (obstacles, resources, units, etc.)
                Debug.Log($"Cannot place: colliding with {col.gameObject.name} (Layer: {LayerMask.LayerToName(col.gameObject.layer)})");
                return false;
            }

            // ✅ Check if ground is too steep/uneven
            if (!IsGroundSuitable(position))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check if the ground is suitable for building (not too steep).
        /// </summary>
        private bool IsGroundSuitable(Vector3 centerPosition)
        {
            if (previewBuilding == null) return true;
            if (!requireFlatGround) return true; // Skip check if not required

            // Get building size
            Bounds bounds = GetBuildingBounds(previewBuilding);

            // Sample points: center + 4 corners
            Vector3[] samplePoints = new Vector3[groundSamples];
            samplePoints[0] = centerPosition; // Center

            if (groundSamples >= 5)
            {
                // Four corners
                Vector3 halfExtents = bounds.extents;
                samplePoints[1] = centerPosition + new Vector3(halfExtents.x, 0, halfExtents.z);
                samplePoints[2] = centerPosition + new Vector3(-halfExtents.x, 0, halfExtents.z);
                samplePoints[3] = centerPosition + new Vector3(halfExtents.x, 0, -halfExtents.z);
                samplePoints[4] = centerPosition + new Vector3(-halfExtents.x, 0, -halfExtents.z);
            }

            // Get heights at each sample point
            float minHeight = float.MaxValue;
            float maxHeight = float.MinValue;
            int validSamples = 0;

            for (int i = 0; i < Mathf.Min(groundSamples, samplePoints.Length); i++)
            {
                Vector3 point = samplePoints[i];

                // Raycast down to find ground
                if (Physics.Raycast(point + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, groundLayer))
                {
                    float height = hit.point.y;
                    minHeight = Mathf.Min(minHeight, height);
                    maxHeight = Mathf.Max(maxHeight, height);
                    validSamples++;
                }
            }

            // Need at least half the samples to be valid
            int requiredSamples = Mathf.Max(1, groundSamples / 2);
            if (validSamples < requiredSamples)
            {
                return false; // Not enough ground detected
            }

            // Check if height difference is acceptable
            float heightDifference = maxHeight - minHeight;
            return heightDifference <= maxHeightDifference;
        }

        /// <summary>
        /// Calculate the Y offset needed to place building bottom on ground.
        /// Returns the distance from pivot to bottom of the building.
        /// </summary>
        private float GetBuildingGroundOffset(GameObject building)
        {
            if (building == null) return 0f;

            Bounds bounds = GetBuildingBounds(building);

            // Calculate offset: distance from building pivot to bottom of bounds
            // If pivot is at center, this will be bounds.extents.y (half height)
            // If pivot is at bottom, this will be near 0
            float pivotY = building.transform.position.y;
            float bottomY = bounds.min.y;
            float offset = pivotY - bottomY;

            return offset;
        }

        /// <summary>
        /// Get the bounds of a building (works with or without collider).
        /// </summary>
        private Bounds GetBuildingBounds(GameObject building)
        {
            // Try to get bounds from collider
            Collider col = building.GetComponent<Collider>();
            if (col != null)
            {
                return col.bounds;
            }

            // Fallback: Use renderer bounds
            Renderer[] renderers = building.GetComponentsInChildren<Renderer>();
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
            return new Bounds(building.transform.position, building.transform.localScale);
        }

        private void SetPreviewMaterial(Material material)
        {
            if (previewBuilding == null || material == null) return;

            var renderers = previewBuilding.GetComponentsInChildren<Renderer>();
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

        #region Validation

        private void ValidateBuildingData()
        {
            if (buildingDataArray == null || buildingDataArray.Length == 0)
            {
                Debug.LogWarning("BuildingManager: No building data assigned!");
                return;
            }

            Debug.Log($"BuildingManager: Loaded {buildingDataArray.Length} building types");

            for (int i = 0; i < buildingDataArray.Length; i++)
            {
                var data = buildingDataArray[i];
                if (data == null)
                {
                    Debug.LogError($"BuildingManager: Building data at index {i} is null!");
                    continue;
                }

                if (data.buildingPrefab == null)
                {
                    Debug.LogError($"BuildingManager: '{data.buildingName}' has no prefab assigned!");
                    continue;
                }

                var building = data.buildingPrefab.GetComponent<Building>();
                if (building == null)
                {
                    Debug.LogWarning($"BuildingManager: '{data.buildingName}' prefab doesn't have Building component!");
                }

            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Check if currently placing a building.
        /// </summary>
        public bool IsPlacing => isPlacingBuilding || (wallPlacementController != null && wallPlacementController.IsPlacingWalls);

        /// <summary>
        /// Get the current building data being placed (if any).
        /// </summary>
        public BuildingDataSO CurrentBuildingData => currentBuildingData;

        /// <summary>
        /// Get all available building data.
        /// </summary>
        public BuildingDataSO[] GetAllBuildingData() => buildingDataArray;

        /// <summary>
        /// Check if a specific building can be afforded.
        /// </summary>
        public bool CanAffordBuilding(BuildingDataSO buildingData)
        {
            if (buildingData == null || resourceService == null) return false;

            var costs = buildingData.GetCosts();
            return resourceService.CanAfford(costs);
        }

        /// <summary>
        /// Get the cost of a building as a dictionary.
        /// </summary>
        public Dictionary<ResourceType, int> GetBuildingCost(BuildingDataSO buildingData)
        {
            if (buildingData == null) return new Dictionary<ResourceType, int>();
            return buildingData.GetCosts();
        }

        /// <summary>
        /// Get buildings of a specific type.
        /// </summary>
        public BuildingDataSO[] GetBuildingsByType(BuildingType type)
        {
            return buildingDataArray.Where(b => b != null && b.buildingType == type).ToArray();
        }

        /// <summary>
        /// Get a building by name.
        /// </summary>
        public BuildingDataSO GetBuildingByName(string buildingName)
        {
            return buildingDataArray.FirstOrDefault(b => b != null && b.buildingName == buildingName);
        }

        #endregion

        #region Debug

        [ContextMenu("Test Place Building 0")]
        private void TestPlaceBuilding()
        {
            if (buildingDataArray.Length > 0)
            {
                StartPlacingBuilding(0);
            }
        }

        [ContextMenu("Cancel Current Placement")]
        private void TestCancelPlacement()
        {
            CancelPlacement();
        }

        [ContextMenu("Print Building Costs")]
        private void PrintBuildingCosts()
        {
            Debug.Log("=== Building Costs ===");
            foreach (var data in buildingDataArray)
            {
                if (data == null) continue;

                var costs = data.GetCosts();
                string costString = $"{data.buildingName} ({data.buildingType}): ";
                foreach (var cost in costs)
                {
                    costString += $"{cost.Key}={cost.Value} ";
                }
                Debug.Log(costString);
            }
        }

        [ContextMenu("List Buildings By Type")]
        private void ListBuildingsByType()
        {
            Debug.Log("=== Buildings By Type ===");
            var types = System.Enum.GetValues(typeof(BuildingType));
            foreach (BuildingType type in types)
            {
                var buildings = GetBuildingsByType(type);
                if (buildings.Length > 0)
                {
                    Debug.Log($"{type}: {string.Join(", ", buildings.Select(b => b.buildingName))}");
                }
            }
        }

        #endregion

        private void OnDrawGizmos()
        {
            // Draw grid if snapping is enabled
            if (useGridSnapping && Application.isPlaying)
            {
                // Optional: Draw placement grid for debugging
            }

            // Draw collision detection box for preview building
            if (isPlacingBuilding && previewBuilding != null)
            {
                Bounds bounds = GetBuildingBounds(previewBuilding);

                // Draw the overlap box that's being checked for collisions
                Gizmos.color = canPlace ? Color.green : Color.red;
                Gizmos.DrawWireCube(bounds.center, bounds.size);

                // Draw a small sphere at the center
                Gizmos.DrawSphere(bounds.center, 0.2f);
            }
        }
    }
}