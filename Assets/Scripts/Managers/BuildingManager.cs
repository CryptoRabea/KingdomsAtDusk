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
    /// Manages building placement, construction, and destruction using the new resource system.
    /// Handles player input for placing buildings.
    /// </summary>
    public class BuildingManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private LayerMask groundLayer;

        [Header("Placement Settings")]
        [SerializeField] private Material validPlacementMaterial;   // Green material
        [SerializeField] private Material invalidPlacementMaterial; // Red material
        [SerializeField] private float gridSize = 1f;               // Snap to grid
        [SerializeField] private bool useGridSnapping = true;

        [Header("Building Prefabs")]
        [SerializeField] private GameObject[] buildingPrefabs; // Assign in Inspector

        // Current state
        private GameObject currentBuildingPrefab;
        private GameObject previewBuilding;
        private bool isPlacingBuilding = false;
        private Material[] originalMaterials;
        private bool canPlace = false;

        // Input
        private Mouse mouse;
        private Keyboard keyboard;

        // Services
        private IResourceService resourceService;

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            mouse = Mouse.current;
            keyboard = Keyboard.current;
        }

        private void Start()
        {
            resourceService = ServiceLocator.TryGet<IResourceService>();

            if (resourceService == null)
            {
                Debug.LogError("BuildingManager: ResourceService not available!");
            }
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
        /// Start placing a building. Call this when player clicks a building button.
        /// </summary>
        public void StartPlacingBuilding(int buildingIndex)
        {
            if (buildingIndex < 0 || buildingIndex >= buildingPrefabs.Length)
            {
                Debug.LogError($"Invalid building index: {buildingIndex}");
                return;
            }

            StartPlacingBuilding(buildingPrefabs[buildingIndex]);
        }

        /// <summary>
        /// Start placing a specific building prefab.
        /// </summary>
        public void StartPlacingBuilding(GameObject buildingPrefab)
        {
            if (buildingPrefab == null)
            {
                Debug.LogError("Building prefab is null!");
                return;
            }

            // Cancel any existing placement
            CancelPlacement();

            currentBuildingPrefab = buildingPrefab;
            isPlacingBuilding = true;

            // Create preview
            CreateBuildingPreview();

            Debug.Log($"Started placing: {buildingPrefab.name}");
        }

        /// <summary>
        /// Cancel current building placement.
        /// </summary>
        public void CancelPlacement()
        {
            if (previewBuilding != null)
            {
                Destroy(previewBuilding);
                previewBuilding = null;
            }

            currentBuildingPrefab = null;
            isPlacingBuilding = false;
            originalMaterials = null;
        }

        #endregion

        #region Placement Logic

        private void CreateBuildingPreview()
        {
            if (currentBuildingPrefab == null) return;

            // Instantiate preview
            previewBuilding = Instantiate(currentBuildingPrefab);

            // Disable scripts on preview
            var building = previewBuilding.GetComponent<Building>();
            if (building != null)
                building.enabled = false;

            // Store original materials
            var renderers = previewBuilding.GetComponentsInChildren<Renderer>();
            originalMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    originalMaterials[i] = renderers[i].material;
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

                previewBuilding.transform.position = position;

                // Check if placement is valid
                canPlace = IsValidPlacement(position);

                // Update preview material
                SetPreviewMaterial(canPlace ? validPlacementMaterial : invalidPlacementMaterial);
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
            if (previewBuilding == null || currentBuildingPrefab == null) return;

            Vector3 position = previewBuilding.transform.position;
            Quaternion rotation = previewBuilding.transform.rotation;

            // Check if player can afford it using the new resource system
            var building = currentBuildingPrefab.GetComponent<Building>();
            if (building != null && building.Data != null)
            {
                if (resourceService != null)
                {
                    // Get costs using the new system
                    var costs = building.Data.GetCosts();

                    if (!resourceService.CanAfford(costs))
                    {
                        Debug.Log("Not enough resources!");

                        // Optional: Show notification to player
                        EventBus.Publish(new ResourcesSpentEvent(
                            costs.GetValueOrDefault(ResourceType.Wood, 0),
                            costs.GetValueOrDefault(ResourceType.Food, 0),
                            costs.GetValueOrDefault(ResourceType.Gold, 0),
                            costs.GetValueOrDefault(ResourceType.Stone, 0),
                            false
                        ));

                        return;
                    }

                    // ✅ CRITICAL FIX: ACTUALLY SPEND THE RESOURCES!
                    bool success = resourceService.SpendResources(costs);
                    if (success)
                    {
                        Debug.Log($"✅ Spent resources: {string.Join(", ", costs.Select(c => $"{c.Key}:{c.Value}"))}");
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
            }

            // Place the actual building
            GameObject newBuilding = Instantiate(currentBuildingPrefab, position, rotation);

            // Publish event
            EventBus.Publish(new BuildingPlacedEvent(newBuilding, position));

            Debug.Log($"✅ Placed building: {currentBuildingPrefab.name}");

            // Cancel placement (or continue placing same building)
            CancelPlacement();

            // Uncomment this to place multiple of same building:
            // StartPlacingBuilding(currentBuildingPrefab);
        }

        private bool IsValidPlacement(Vector3 position)
        {
            if (previewBuilding == null) return false;

            // Check if overlapping with other buildings
            Collider[] colliders = Physics.OverlapBox(
                position,
                previewBuilding.transform.localScale / 2f,
                previewBuilding.transform.rotation
            );

            foreach (var col in colliders)
            {
                // Check if it's another building (ignore self)
                if (col.gameObject != previewBuilding && col.GetComponent<Building>() != null)
                {
                    return false; // Overlapping with another building
                }
            }

            return true;
        }

        private void SetPreviewMaterial(Material material)
        {
            if (previewBuilding == null || material == null) return;

            var renderers = previewBuilding.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.material = material;
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Check if currently placing a building.
        /// </summary>
        public bool IsPlacing => isPlacingBuilding;

        /// <summary>
        /// Get the current building being placed (if any).
        /// </summary>
        public GameObject CurrentBuildingPrefab => currentBuildingPrefab;

        /// <summary>
        /// Check if a specific building can be afforded.
        /// </summary>
        public bool CanAffordBuilding(GameObject buildingPrefab)
        {
            if (buildingPrefab == null || resourceService == null) return false;

            var building = buildingPrefab.GetComponent<Building>();
            if (building == null || building.Data == null) return false;

            var costs = building.Data.GetCosts();
            return resourceService.CanAfford(costs);
        }

        /// <summary>
        /// Get the cost of a building as a dictionary.
        /// </summary>
        public Dictionary<ResourceType, int> GetBuildingCost(GameObject buildingPrefab)
        {
            if (buildingPrefab == null) return new Dictionary<ResourceType, int>();

            var building = buildingPrefab.GetComponent<Building>();
            if (building == null || building.Data == null) return new Dictionary<ResourceType, int>();

            return building.Data.GetCosts();
        }

        #endregion

        #region Debug

        [ContextMenu("Test Place Building 0")]
        private void TestPlaceBuilding()
        {
            if (buildingPrefabs.Length > 0)
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
            foreach (var prefab in buildingPrefabs)
            {
                if (prefab == null) continue;

                var building = prefab.GetComponent<Building>();
                if (building == null || building.Data == null) continue;

                var costs = building.Data.GetCosts();
                string costString = building.Data.buildingName + ": ";
                foreach (var cost in costs)
                {
                    costString += $"{cost.Key}={cost.Value} ";
                }
                Debug.Log(costString);
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
        }
    }
}