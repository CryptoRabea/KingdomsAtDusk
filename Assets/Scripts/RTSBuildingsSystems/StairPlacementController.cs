using UnityEngine;
using UnityEngine.InputSystem;
using RTS.Core.Services;
using KingdomsAtDusk.FogOfWar;
namespace RTS.Buildings
{
    /// <summary>
    /// Handles placement of stairs on walls.
    /// Stairs allow units to traverse from ground to wall top.
    /// </summary>
    public class StairPlacementController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask wallLayer;

        [Header("Stair Prefab")]
        [SerializeField] private GameObject stairPrefab;

        [Header("Placement Settings")]
        [SerializeField] private float snapDistance = 2f;
        [SerializeField] private float minDistanceFromWall = 0.5f;
        [SerializeField] private float maxDistanceFromWall = 5f;

        [Header("Visual Settings")]
        [SerializeField] private Material validPreviewMaterial;
        [SerializeField] private Material invalidPreviewMaterial;

        // State
        private bool isPlacingStair = false;
        private GameObject stairPreview;
        private GameObject targetWall;
        private bool isValidPlacement = false;

        // Input
        private Mouse mouse;
        private Keyboard keyboard;

        // Cost (optional - could be added to a StairDataSO)
        private int woodCost = 50;
        private int stoneCost = 20;
        private IResourcesService resourceService;

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
        }

        private void Update()
        {
            if (isPlacingStair)
            {
                UpdateStairPlacement();
                HandleStairPlacementInput();
            }
        }

        #region Public API

        public void StartPlacingStairs()
        {
            if (stairPrefab == null)
            {
                Debug.LogError("StairPlacementController: No stair prefab assigned!");
                return;
            }

            CancelStairPlacement();

            isPlacingStair = true;
            CreateStairPreview();

            Debug.Log("Started placing stairs");
        }

        public void CancelStairPlacement()
        {
            if (stairPreview != null)
            {
                Destroy(stairPreview);
                stairPreview = null;
            }

            isPlacingStair = false;
            targetWall = null;
            isValidPlacement = false;
        }

        public bool IsPlacingStairs => isPlacingStair;

        #endregion

        #region Placement Logic

        private void CreateStairPreview()
        {
            // Instantiate as INACTIVE to prevent VisionProvider.OnEnable() from running
            stairPreview = Instantiate(stairPrefab);
            stairPreview.SetActive(false);

            // Destroy VisionProvider on preview to prevent fog of war reveal
            // Use DestroyImmediate because Destroy() only marks for deletion at end of frame
            if (stairPreview.TryGetComponent<KingdomsAtDusk.FogOfWar.IVisionProvider>(out var visionProvider))
            {
                // Cast the interface back to a component
                var component = visionProvider as Component;
                if (component != null)
                    DestroyImmediate(component);
            }


            // Disable components for preview
            var stairComponent = stairPreview.GetComponent<WallStairs>();
            if (stairComponent != null)
                stairComponent.enabled = false;

            // Reactivate preview now that components are cleaned up
            stairPreview.SetActive(true);

            foreach (var collider in stairPreview.GetComponentsInChildren<Collider>())
                collider.enabled = false;

            SetPreviewMaterial(stairPreview, validPreviewMaterial);
        }

        private void UpdateStairPlacement()
        {
            if (mouse == null || stairPreview == null) return;

            Vector3 mouseWorldPos = GetMouseWorldPosition();
            if (mouseWorldPos == Vector3.zero)
            {
                isValidPlacement = false;
                SetPreviewMaterial(stairPreview, invalidPreviewMaterial);
                return;
            }

            // Find nearby wall
            targetWall = FindNearestWall(mouseWorldPos);

            if (targetWall != null)
            {
                // Position stair next to wall
                Vector3 stairPosition = CalculateStairPosition(mouseWorldPos, targetWall);
                Quaternion stairRotation = CalculateStairRotation(stairPosition, targetWall);

                stairPreview.transform.position = stairPosition;
                stairPreview.transform.rotation = stairRotation;

                // Check if placement is valid
                isValidPlacement = IsValidStairPlacement(stairPosition, targetWall);
            }
            else
            {
                // No wall nearby, just follow mouse
                stairPreview.transform.position = mouseWorldPos;
                isValidPlacement = false;
            }

            // Update preview material
            Material previewMat = isValidPlacement ? validPreviewMaterial : invalidPreviewMaterial;
            SetPreviewMaterial(stairPreview, previewMat);
        }

        private GameObject FindNearestWall(Vector3 position)
        {
            Collider[] nearbyColliders = Physics.OverlapSphere(position, snapDistance, wallLayer);

            GameObject nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (var col in nearbyColliders)
            {
                // Check if it's a wall
                WallConnectionSystem wallSystem = col.GetComponentInParent<WallConnectionSystem>();
                if (wallSystem != null)
                {
                    float distance = Vector3.Distance(position, col.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = col.gameObject;
                    }
                }
            }

            return nearest;
        }

        private Vector3 CalculateStairPosition(Vector3 mousePos, GameObject wall)
        {
            // Get the closest point on the wall
            Collider wallCollider = wall.GetComponent<Collider>();
            if (wallCollider != null)
            {
                Vector3 closestPoint = wallCollider.ClosestPoint(mousePos);

                // Offset slightly away from wall
                Vector3 directionFromWall = (mousePos - closestPoint).normalized;
                return closestPoint + directionFromWall * minDistanceFromWall;
            }

            return mousePos;
        }

        private Quaternion CalculateStairRotation(Vector3 stairPos, GameObject wall)
        {
            // Face the stairs toward the wall
            Vector3 toWall = wall.transform.position - stairPos;
            toWall.y = 0;

            if (toWall != Vector3.zero)
            {
                return Quaternion.LookRotation(toWall);
            }

            return Quaternion.identity;
        }

        private bool IsValidStairPlacement(Vector3 position, GameObject wall)
        {
            if (wall == null)
                return false;

            // Check distance from wall
            float distanceToWall = Vector3.Distance(position, wall.transform.position);
            if (distanceToWall < minDistanceFromWall || distanceToWall > maxDistanceFromWall)
                return false;

            // Check if there's already a stair nearby
            Collider[] nearbyStairs = Physics.OverlapSphere(position, 2f);
            foreach (var col in nearbyStairs)
            {
                if (col.GetComponent<WallStairs>() != null && col.gameObject != stairPreview)
                {
                    Debug.Log("Stair too close to existing stair");
                    return false;
                }
            }

            // Check resources
            if (resourceService != null)
            {
                if (resourceService.GetResource(ResourceType.Wood) < woodCost ||
                    resourceService.GetResource(ResourceType.Stone) < stoneCost)
                {
                    return false;
                }
            }

            return true;
        }

        private void HandleStairPlacementInput()
        {
            if (mouse == null) return;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (isValidPlacement)
                {
                    PlaceStair();
                }
                else
                {
                    Debug.Log("Cannot place stair here!");
                }
            }

            if (mouse.rightButton.wasPressedThisFrame ||
                (keyboard != null && keyboard.escapeKey.wasPressedThisFrame))
            {
                CancelStairPlacement();
                Debug.Log("Stair placement canceled");
            }
        }

        private void PlaceStair()
        {
            if (!isValidPlacement || stairPreview == null)
                return;

            // Spend resources
            if (resourceService != null)
            {
                var cost = new System.Collections.Generic.Dictionary<ResourceType, int>
                {
                    { ResourceType.Wood, woodCost },
                    { ResourceType.Stone, stoneCost }
                };

                if (!resourceService.SpendResources(cost))
                {
                    Debug.LogError("Failed to spend resources for stair!");
                    return;
                }
            }

            // Instantiate the actual stair
            GameObject newStair = Instantiate(
                stairPrefab,
                stairPreview.transform.position,
                stairPreview.transform.rotation
            );

            // Ensure WallStairs component is enabled
            var stairComponent = newStair.GetComponent<WallStairs>();
            if (stairComponent != null)
            {
                stairComponent.enabled = true;
            }

            Debug.Log($"✅ Placed stair at {newStair.transform.position}");

            // Continue placing or cancel
            CancelStairPlacement();
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

        private void SetPreviewMaterial(GameObject obj, Material material)
        {
            if (obj == null || material == null) return;

            var renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.sharedMaterial = material;
                }
            }
        }

        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (isPlacingStair && targetWall != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(stairPreview.transform.position, targetWall.transform.position);
                Gizmos.DrawWireSphere(targetWall.transform.position, snapDistance);
            }
        }
#endif
    }
}
