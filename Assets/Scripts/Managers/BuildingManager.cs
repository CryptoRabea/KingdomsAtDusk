using KAD.RTSBuildingsSystems;
using RTS.Buildings;
using RTS.Core.Events;
using RTS.Core.Services;
using RTS.FogOfWar;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

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

        [Header("Fog of War")]
        [SerializeField] private RTS_FogOfWar fogWarSystem;
        [Tooltip("If null, will search for RTS_FogOfWar in scene")]
        [SerializeField] private int localPlayerId = 0;
        [Tooltip("Player ID for fog of war visibility checks")]

        [Header("Placement Settings")]
        [SerializeField] private Material validPlacementMaterial;
        [SerializeField] private Material invalidPlacementMaterial;
        [SerializeField] private float gridSize = 1f;
        [SerializeField] private bool useGridSnapping = true;

        [Header("Terrain Validation")]
        [SerializeField] private float maxHeightDifference = 2f;
        [SerializeField] private bool requireFlatGround = true;
        [SerializeField] private int groundSamples = 5;

        [Header("Wall Placement")]
        [SerializeField] private WallPlacementController wallPlacementController;
        [SerializeField] private bool useWallPlacementForWalls = true;

        [Header("Tower Placement")]
        [SerializeField] private TowerPlacementHelper towerPlacementHelper;
        [SerializeField] private bool enableTowerWallSnapping = true;
        [SerializeField] private bool autoReplaceWalls = true;

        [Header("Gate Placement")]
        [SerializeField] private GatePlacementHelper gatePlacementHelper;
        [SerializeField] private bool enableGateWallSnapping = true;
        [SerializeField] private bool autoReplaceWallsForGates = true;

        [Header("Building Data - SOURCE OF TRUTH")]
        [Tooltip("Assign BuildingDataSO assets here, NOT prefabs!")]
        [SerializeField] private BuildingDataSO[] buildingDataArray;

        // Current state
        private BuildingDataSO currentBuildingData;
        private GameObject previewBuilding;
        private bool isPlacingBuilding = false;
        private Material[] originalMaterials;
        private bool canPlace = false;
        private BuildingPlacementGridVisualizer gridVisualizer;

        // Rotation state
        private float currentBuildingRotation = 0f;
        private const float rotationStep = 1f;
        private Quaternion baseRotation;

        // Tower placement state
        private bool isPlacingTower = false;
        private TowerDataSO currentTowerData;
        private List<GameObject> wallsToReplace = new List<GameObject>();  // Support multi-segment replacement
        private bool isSnappedToWall = false;
        private Quaternion towerSnappedRotation = Quaternion.identity;  // Tower rotation from wall alignment
        private WallReplacementData wallReplacementData;

        // Gate placement state
        private bool isPlacingGate = false;
        private GateDataSO currentGateData;
        private GameObject wallToReplaceForGate;
        private bool isGateSnappedToWall = false;
        private WallReplacementData gateWallReplacementData;
        private Quaternion gateSnappedRotation = Quaternion.identity;

        // Input
        private Mouse mouse;
        private Keyboard keyboard;
        private InputSystem_Actions inputActions;

        // Services
        private IResourcesService resourceService;

        // Selection Manager
        private RTS.Buildings.BuildingSelectionManager buildingSelectionManager;

        // Public property to check if currently placing a building
        public bool IsPlacingBuilding => isPlacingBuilding;

        // Cached array for NonAlloc physics queries (avoid GC allocations)
        private static readonly Collider[] _overlapResults = new Collider[32];

        #region Fog of War Visibility Check

        /// <summary>
        /// Check if a position is currently visible (not in fog of war).
        /// Uses legacy csFogWar system.
        /// </summary>
        private bool IsPositionVisible(Vector3 worldPosition)
        {
            if (fogWarSystem == null) return true; // No fog system = always visible
            return fogWarSystem.CheckVisibility(worldPosition, localPlayerId);
        }

        #endregion

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            mouse = Mouse.current;
            keyboard = Keyboard.current;
            inputActions = new InputSystem_Actions();
        }

        private void OnEnable()
        {
            inputActions.Player.Enable();
        }

        private void OnDisable()
        {
            inputActions.Player.Disable();
        }

        private void Start()
        {
            resourceService = ServiceLocator.TryGet<IResourcesService>();

            if (resourceService == null)
            {
            }

            // Find fog of war system if not assigned
            if (fogWarSystem == null)
            {
                fogWarSystem = FindFirstObjectByType<RTS_FogOfWar>();
                if (fogWarSystem == null)
                {
                }
            }

            // Find BuildingSelectionManager
            buildingSelectionManager = Object.FindAnyObjectByType<RTS.Buildings.BuildingSelectionManager>();

            // Auto-create placement helpers if missing
            if (towerPlacementHelper == null)
            {
                towerPlacementHelper = GetComponent<TowerPlacementHelper>();
                if (towerPlacementHelper == null)
                {
                    towerPlacementHelper = gameObject.AddComponent<TowerPlacementHelper>();
                }
            }

            if (gatePlacementHelper == null)
            {
                gatePlacementHelper = GetComponent<GatePlacementHelper>();
                if (gatePlacementHelper == null)
                {
                    gatePlacementHelper = gameObject.AddComponent<GatePlacementHelper>();
                }
            }

            if (wallPlacementController == null)
            {
                wallPlacementController = GetComponent<WallPlacementController>();
                if (wallPlacementController == null)
                {
                }
            }

            // Validate building data
            ValidateBuildingData();
        }

        private void Update()
        {
            if (isPlacingBuilding)
            {
                HandleRotationInput();
                UpdateBuildingPreview();
                HandlePlacementInput();
            }
        }

        #region Building Selection

        /// <summary>
        /// Start placing a building by index.
        /// </summary>
        public void StartPlacingBuilding(int buildingIndex)
        {
            if (buildingIndex < 0 || buildingIndex >= buildingDataArray.Length)
            {
                return;
            }

            StartPlacingBuilding(buildingDataArray[buildingIndex]);
        }

        /// <summary>
        /// Start placing a building from BuildingDataSO.
        /// </summary>
        public void StartPlacingBuilding(BuildingDataSO buildingData)
        {
            if (buildingData == null)
            {
                return;
            }

            if (buildingData.buildingPrefab == null)
            {
                return;
            }

            CancelPlacement();

            if (buildingSelectionManager != null)
            {
                buildingSelectionManager.DeselectBuilding();
            }

            bool isWall = buildingData.buildingType == BuildingType.Defensive &&
                          buildingData.buildingPrefab.GetComponent<WallConnectionSystem>() != null;

            if (isWall && useWallPlacementForWalls && wallPlacementController != null)
            {
                wallPlacementController.StartPlacingWalls(buildingData);
            }
            else
            {
                currentBuildingData = buildingData;
                isPlacingBuilding = true;

                currentBuildingRotation = 0f;

                if (buildingData is TowerDataSO towerData)
                {
                    isPlacingTower = true;
                    currentTowerData = towerData;
                    isPlacingGate = false;
                    currentGateData = null;
                }
                else if (buildingData is GateDataSO gateData)
                {
                    isPlacingGate = true;
                    currentGateData = gateData;
                    isPlacingTower = false;
                    currentTowerData = null;
                }
                else
                {
                    isPlacingTower = false;
                    currentTowerData = null;
                    isPlacingGate = false;
                    currentGateData = null;
                }

                CreateBuildingPreview();

            }
        }

        /// <summary>
        /// Cancel current building placement.
        /// </summary>
        public void CancelPlacement()
        {
            if (wallPlacementController != null && wallPlacementController.IsPlacingWalls)
            {
                wallPlacementController.CancelWallPlacement();
            }

            if (previewBuilding != null)
            {
                // Restore original materials before destroying to clean up material instances
                RestoreOriginalMaterials();
                Destroy(previewBuilding);
                previewBuilding = null;
            }

            currentBuildingData = null;
            isPlacingBuilding = false;
            originalMaterials = null;
            gridVisualizer = null;

            isPlacingTower = false;
            currentTowerData = null;
            wallsToReplace.Clear();
            isSnappedToWall = false;
            towerSnappedRotation = Quaternion.identity;
            wallReplacementData = null;

            isPlacingGate = false;
            currentGateData = null;
            wallToReplaceForGate = null;
            isGateSnappedToWall = false;
            gateWallReplacementData = null;
            gateSnappedRotation = Quaternion.identity;

            currentBuildingRotation = 0f;
        }

        #endregion

        #region Placement Logic

        private void CreateBuildingPreview()
        {
            if (currentBuildingData == null || currentBuildingData.buildingPrefab == null) return;

            previewBuilding = Instantiate(
                currentBuildingData.buildingPrefab,
                Vector3.zero,
                currentBuildingData.buildingPrefab.transform.rotation
            );
            previewBuilding.SetActive(false);

            baseRotation = currentBuildingData.buildingPrefab.transform.rotation;
            currentBuildingRotation = 0f;

            // Remove any vision provider components to prevent fog reveal during preview
            var providers = previewBuilding.GetComponents<MonoBehaviour>();
            foreach (var c in providers)
            {
                // Check for IVisionProvider interface (works with both old and new systems)
                if (c != null && c.GetType().GetInterface("IVisionProvider") != null)
                    DestroyImmediate(c);
            }

            if (previewBuilding.TryGetComponent<Building>(out var building))
                building.enabled = false;

            if (previewBuilding.TryGetComponent<BuildingSelectable>(out var buildingSelectable))
                buildingSelectable.enabled = false;

            previewBuilding.SetActive(true);

            var renderers = previewBuilding.GetComponentsInChildren<Renderer>();
            originalMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    originalMaterials[i] = renderers[i].sharedMaterial;
            }

            gridVisualizer = previewBuilding.GetComponent<BuildingPlacementGridVisualizer>();
            if (gridVisualizer != null)
            {
                gridVisualizer.Show();
            }

            // Force LOD 0 (final building) for preview so player sees completed building
            var meshRenderers = previewBuilding.GetComponentsInChildren<MeshRenderer>();
            foreach (var meshRenderer in meshRenderers)
            {
                if (meshRenderer != null)
                {
                    meshRenderer.forceMeshLod = 0; // Force highest detail LOD
                }
            }

            SetPreviewMaterial(validPlacementMaterial);
        }

        private void UpdateBuildingPreview()
        {
            if (previewBuilding == null || mouse == null) return;

            Vector2 mousePos = mouse.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
            {
                Vector3 position = hit.point;

                if (useGridSnapping)
                {
                    position.x = Mathf.Round(position.x / gridSize) * gridSize;
                    position.z = Mathf.Round(position.z / gridSize) * gridSize;
                }

                // Handle tower wall snapping
                if (isPlacingTower && currentTowerData != null && enableTowerWallSnapping && towerPlacementHelper != null)
                {
                    if (towerPlacementHelper.TrySnapToWall(position, currentTowerData, out Vector3 wallSnapPos, out Quaternion wallSnapRot, out List<GameObject> walls))
                    {
                        position = wallSnapPos;
                        wallsToReplace = walls;
                        isSnappedToWall = true;
                        towerSnappedRotation = wallSnapRot;

                        // Update preview rotation to match wall
                        previewBuilding.transform.rotation = wallSnapRot;
                    }
                    else
                    {
                        wallsToReplace.Clear();
                        isSnappedToWall = false;
                    }
                }

                // Handle gate wall snapping
                if (isPlacingGate && currentGateData != null && enableGateWallSnapping && gatePlacementHelper != null)
                {
                    if (gatePlacementHelper.TrySnapToWall(position, currentGateData, out Vector3 gateSnapPos, out Quaternion gateSnapRot, out GameObject gateWall))
                    {
                        position = gateSnapPos;
                        wallToReplaceForGate = gateWall;
                        isGateSnappedToWall = true;
                        gateSnappedRotation = gateSnapRot;

                        // Update preview rotation to match wall
                        previewBuilding.transform.rotation = gateSnapRot;
                    }
                    else
                    {
                        wallToReplaceForGate = null;
                        isGateSnappedToWall = false;
                    }
                }

                position.y = hit.point.y + GetBuildingGroundOffset(previewBuilding);

                previewBuilding.transform.position = position;

                canPlace = IsValidPlacement(position);

                Material previewMat = canPlace ? validPlacementMaterial : invalidPlacementMaterial;
                SetPreviewMaterial(previewMat);

                if (gridVisualizer != null)
                {
                    gridVisualizer.UpdatePlacementIndicator(canPlace);
                }
            }
        }

        private void HandlePlacementInput()
        {
            if (mouse == null) return;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (canPlace)
                {
                    PlaceBuilding();
                }
                else
                {
                    if (previewBuilding != null)
                    {
                        Vector3 pos = previewBuilding.transform.position;

                        // Check fog of war first using legacy system
                        if (fogWarSystem != null && !IsPositionVisible(pos))
                        {
                            EventBus.Publish(new BuildingPlacementFailedEvent("Cannot build in unexplored or hidden areas!"));
                        }
                        else
                        {
                            EventBus.Publish(new BuildingPlacementFailedEvent("Invalid placement location!"));
                        }
                    }
                }
            }

            if (inputActions.Player.Click1.WasPerformedThisFrame() ||
                inputActions.Player.Cancel.WasPerformedThisFrame())
            {
                CancelPlacement();
            }
        }

        public void SetPreview(GameObject prefabInstance)
        {
            previewBuilding = prefabInstance;
            baseRotation = prefabInstance.transform.rotation;
            currentBuildingRotation = 0f;
        }

        private void HandleRotationInput()
        {
            if (isPlacingGate||isPlacingTower)
                return; // Gates use wall-aligned rotation when snapped, skip manual rotation
            float scrollDelta = inputActions.Player.Zoom.ReadValue<float>();

            if (scrollDelta > 0f)
            {
                currentBuildingRotation += rotationStep;
            }
            else if (scrollDelta < 0f)
            {
                currentBuildingRotation -= rotationStep;
            }

            currentBuildingRotation = Mathf.Repeat(currentBuildingRotation, 360f);

            if (previewBuilding != null)
            {
                previewBuilding.transform.rotation =
                    baseRotation * Quaternion.Euler(0, 0, currentBuildingRotation);
            }
        }

        private void PlaceBuilding()
        {
            if (previewBuilding == null || currentBuildingData == null) return;

            previewBuilding.transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);

            if (resourceService != null)
            {
                var costs = currentBuildingData.GetCosts();

                if (!resourceService.CanAfford(costs))
                {

                    EventBus.Publish(new ResourcesSpentEvent(
                        costs.GetValueOrDefault(ResourceType.Wood, 0),
                        costs.GetValueOrDefault(ResourceType.Food, 0),
                        costs.GetValueOrDefault(ResourceType.Gold, 0),
                        costs.GetValueOrDefault(ResourceType.Stone, 0),
                        false
                    ));

                    return;
                }

                bool success = resourceService.SpendResources(costs);
                if (success)
                {
                }
                else
                {
                    return;
                }
            }
            else
            {
            }

            // Handle wall replacement for towers
            if (isPlacingTower && wallsToReplace != null && wallsToReplace.Count > 0 && autoReplaceWalls && isSnappedToWall && towerPlacementHelper != null)
            {
                // Store wall connection data before destroying
                wallReplacementData = towerPlacementHelper.ReplaceWallWithTower(wallsToReplace, currentTowerData);

                // Destroy all wall segments being replaced
                foreach (var wall in wallsToReplace)
                {
                    if (wall != null)
                    {
                        Destroy(wall);
                    }
                }
                wallsToReplace.Clear();

                // Use the snapped rotation for the tower (like gates do)
                rotation = towerSnappedRotation;
            }

            // Handle wall replacement for gates
            if (isPlacingGate && wallToReplaceForGate != null && autoReplaceWallsForGates && isGateSnappedToWall && gatePlacementHelper != null)
            {

                // Store wall connection data before destroying
                gateWallReplacementData = gatePlacementHelper.ReplaceWallWithGate(wallToReplaceForGate, currentGateData);

                // Destroy the wall
                Destroy(wallToReplaceForGate);
                wallToReplaceForGate = null;

                // Use the snapped rotation for the gate
                rotation = gateSnappedRotation;
            }

            GameObject newBuilding = Instantiate(currentBuildingData.buildingPrefab, position, rotation);

            if (newBuilding.TryGetComponent<Building>(out var buildingComponent))
            {
                buildingComponent.SetData(currentBuildingData);
            }
            else
            {
            }

            // Add NavMesh obstacle (legacy support)
            if (newBuilding.GetComponent<BuildingNavMeshObstacle>() == null)
            {
                newBuilding.AddComponent<BuildingNavMeshObstacle>();
            }

            // Add FlowField obstacle (new system)
            if (newBuilding.GetComponent<FlowField.Obstacles.BuildingFlowFieldObstacle>() == null)
            {
                newBuilding.AddComponent<FlowField.Obstacles.BuildingFlowFieldObstacle>();
            }

            if (isPlacingTower)
            {
                if (newBuilding.TryGetComponent<Tower>(out var towerComponent) && currentTowerData != null)
                {
                    towerComponent.SetTowerData(currentTowerData);
                }

                // Apply wall connections to tower if it replaced a wall
                if (wallReplacementData != null && towerPlacementHelper != null)
                {
                    towerPlacementHelper.ApplyWallConnectionsToTower(newBuilding, wallReplacementData);
                    wallReplacementData = null;
                }
            }

            if (isPlacingGate)
            {
                if (newBuilding.TryGetComponent<Gate>(out var gateComponent) && currentGateData != null)
                {
                    gateComponent.SetGateData(currentGateData);
                }

                // Apply wall connections to gate if it replaced a wall
                if (gateWallReplacementData != null && gatePlacementHelper != null)
                {
                    gatePlacementHelper.ApplyWallConnectionsToGate(newBuilding, gateWallReplacementData);
                    gateWallReplacementData = null;
                }
            }

            EventBus.Publish(new BuildingPlacedEvent(newBuilding, position));


            CancelPlacement();
        }

        private bool IsValidPlacement(Vector3 position)
        {
            if (previewBuilding == null) return false;

            // CHECK FOG OF WAR: Only allow placement in currently visible areas using legacy system
            if (fogWarSystem != null)
            {
                if (!IsPositionVisible(position))
                {
                    return false;
                }
            }

            Bounds buildingBounds = GetBuildingBounds(previewBuilding);

            int hitCount = Physics.OverlapBoxNonAlloc(
                buildingBounds.center,
                buildingBounds.extents,
                _overlapResults,
                previewBuilding.transform.rotation,
                ~groundLayer
            );

            for (int i = 0; i < hitCount; i++)
            {
                var col = _overlapResults[i];
                if (col.gameObject == previewBuilding || col.transform.IsChildOf(previewBuilding.transform))
                {
                    continue;
                }

                if (col is TerrainCollider)
                {
                    continue;
                }

                // IMPORTANT: Skip all walls from collision detection
                // Walls use their own specialized overlap detection (WouldOverlapExistingWall)
                // which uses geometric segment-based checks instead of physics colliders.
                // This allows proper wall-to-wall connections and tower placement near walls.
                WallConnectionSystem wallSystem = col.GetComponentInParent<WallConnectionSystem>();
                if (wallSystem != null)
                {
                    // Special case: If placing a tower/gate that will replace THIS specific wall, allow it
                    if (isPlacingTower && wallsToReplace != null && wallsToReplace.Count > 0)
                    {
                        // Check if this wall is one of the walls being replaced
                        bool isBeingReplaced = false;
                        foreach (var wall in wallsToReplace)
                        {
                            if (col.gameObject == wall || col.transform.IsChildOf(wall.transform))
                            {
                                isBeingReplaced = true;
                                break;
                            }
                        }
                        if (isBeingReplaced)
                        {
                            continue; // Skip - we're replacing this wall
                        }
                    }
                    if (isPlacingGate && wallToReplaceForGate != null &&
                        (col.gameObject == wallToReplaceForGate || col.transform.IsChildOf(wallToReplaceForGate.transform)))
                    {
                        continue; // Skip - we're replacing this wall
                    }

                    // Skip all other walls - they have their own validation system
                    continue;
                }

                return false;
            }

            if (!IsGroundSuitable(position))
            {
                return false;
            }

            return true;
        }

        private bool IsGroundSuitable(Vector3 centerPosition)
        {
            if (previewBuilding == null) return true;
            if (!requireFlatGround) return true;

            Bounds bounds = GetBuildingBounds(previewBuilding);

            Vector3[] samplePoints = new Vector3[groundSamples];
            samplePoints[0] = centerPosition;

            if (groundSamples >= 5)
            {
                Vector3 halfExtents = bounds.extents;
                samplePoints[1] = centerPosition + new Vector3(halfExtents.x, 0, halfExtents.z);
                samplePoints[2] = centerPosition + new Vector3(-halfExtents.x, 0, halfExtents.z);
                samplePoints[3] = centerPosition + new Vector3(halfExtents.x, 0, -halfExtents.z);
                samplePoints[4] = centerPosition + new Vector3(-halfExtents.x, 0, -halfExtents.z);
            }

            float minHeight = float.MaxValue;
            float maxHeight = float.MinValue;
            int validSamples = 0;

            for (int i = 0; i < Mathf.Min(groundSamples, samplePoints.Length); i++)
            {
                Vector3 point = samplePoints[i];

                if (Physics.Raycast(point + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, groundLayer))
                {
                    float height = hit.point.y;
                    minHeight = Mathf.Min(minHeight, height);
                    maxHeight = Mathf.Max(maxHeight, height);
                    validSamples++;
                }
            }

            int requiredSamples = Mathf.Max(1, groundSamples / 2);
            if (validSamples < requiredSamples)
            {
                return false;
            }

            float heightDifference = maxHeight - minHeight;
            return heightDifference <= maxHeightDifference;
        }

        private float GetBuildingGroundOffset(GameObject building)
        {
            if (building == null) return 0f;

            Bounds bounds = GetBuildingBounds(building);

            float pivotY = building.transform.position.y;
            float bottomY = bounds.min.y;
            float offset = pivotY - bottomY;

            return offset;
        }

        private Bounds GetBuildingBounds(GameObject building)
        {
            if (building.TryGetComponent<Collider>(out var col))
            {
                return col.bounds;
            }

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
                    // Use material (instance) instead of sharedMaterial to avoid modifying the prefab asset
                    renderer.material = material;
                }
            }
        }

        private void RestoreOriginalMaterials()
        {
            if (previewBuilding == null || originalMaterials == null) return;

            var renderers = previewBuilding.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length && i < originalMaterials.Length; i++)
            {
                if (renderers[i] != null && originalMaterials[i] != null)
                {
                    renderers[i].sharedMaterial = originalMaterials[i];
                }
            }
        }

        #endregion

        #region Validation

        private void ValidateBuildingData()
        {
            if (buildingDataArray == null || buildingDataArray.Length == 0)
            {
                return;
            }


            for (int i = 0; i < buildingDataArray.Length; i++)
            {
                var data = buildingDataArray[i];
                if (data == null)
                {
                    continue;
                }

                if (data.buildingPrefab == null)
                {
                    continue;
                }

                if (!data.buildingPrefab.TryGetComponent<Building>(out var building))
                {
                }
            }
        }

        #endregion

        #region Public API

        public bool IsPlacing => isPlacingBuilding || (wallPlacementController != null && wallPlacementController.IsPlacingWalls);
        public BuildingDataSO CurrentBuildingData => currentBuildingData;
        public BuildingDataSO[] GetAllBuildingData() => buildingDataArray;

        public bool CanAffordBuilding(BuildingDataSO buildingData)
        {
            if (buildingData == null || resourceService == null) return false;

            var costs = buildingData.GetCosts();
            return resourceService.CanAfford(costs);
        }

        public Dictionary<ResourceType, int> GetBuildingCost(BuildingDataSO buildingData)
        {
            if (buildingData == null) return new Dictionary<ResourceType, int>();
            return buildingData.GetCosts();
        }

        public BuildingDataSO[] GetBuildingsByType(BuildingType type)
        {
            return buildingDataArray.Where(b => b != null && b.buildingType == type).ToArray();
        }

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
            foreach (var data in buildingDataArray)
            {
                if (data == null) continue;

                var costs = data.GetCosts();
                string costString = $"{data.buildingName} ({data.buildingType}): ";
                foreach (var cost in costs)
                {
                    costString += $"{cost.Key}={cost.Value} ";
                }
            }
        }

        [ContextMenu("List Buildings By Type")]
        private void ListBuildingsByType()
        {
            var types = System.Enum.GetValues(typeof(BuildingType));
            foreach (BuildingType type in types)
            {
                var buildings = GetBuildingsByType(type);
                if (buildings.Length > 0)
                {
                }
            }
        }

        #endregion

        private void OnDrawGizmos()
        {
            if (isPlacingBuilding && previewBuilding != null)
            {
                Bounds bounds = GetBuildingBounds(previewBuilding);

                Gizmos.color = canPlace ? Color.green : Color.red;
                Gizmos.DrawWireCube(bounds.center, bounds.size);
                Gizmos.DrawSphere(bounds.center, 0.2f);
            }
        }
    }
}