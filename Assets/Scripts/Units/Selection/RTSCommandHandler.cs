using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using RTS.Units.Formation;

namespace RTS.Units
{
    /// <summary>
    /// Handles RTS-style commands: right-click to move units.
    /// Attach this to your SelectionManager GameObject.
    /// </summary>
    public class RTSCommandHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UnitSelectionManager selectionManager;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private FormationGroupManager formationGroupManager;

        [Header("Settings")]
        [SerializeField] private LayerMask groundLayer; // What counts as ground
        [SerializeField] private LayerMask unitLayer;   // What counts as units

        [Header("Viewport Settings")]
        [Tooltip("Camera viewport height (0-1). Should match RTSCameraController settings.")]
        [SerializeField] private float viewportHeight = 0.8f;
        [Tooltip("Camera viewport Y offset (0-1). Should match RTSCameraController settings.")]
        [SerializeField] private float viewportYOffset = 0.2f;
        
        [Header("Visual Feedback")]
        [SerializeField] private GameObject moveMarkerPrefab; // Optional: shows where units will move
        [SerializeField] private float markerLifetime = 1f;
        [SerializeField] private float markerHeightOffset = 0.1f; // Height above ground to spawn marker

        [Header("Double-Click Settings")]
        [SerializeField] private float doubleClickTime = 0.3f; // Time window for double-click

        private Mouse mouse;
        private float lastRightClickTime = -1f;

        // Cached for UI detection
        private PointerEventData cachedPointerEventData;
        private List<RaycastResult> cachedRaycastResults = new List<RaycastResult>();

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            mouse = Mouse.current;

            if (mouse == null)
            {
                Debug.LogError("No mouse detected! RTSCommandHandler won't work.");
                enabled = false;
            }

            // Initialize UI detection
            if (EventSystem.current != null)
            {
                cachedPointerEventData = new PointerEventData(EventSystem.current);
            }
        }

        private bool IsMouseOverUI()
        {
            if (EventSystem.current == null)
                return false;

            // Initialize if needed
            if (cachedPointerEventData == null)
            {
                cachedPointerEventData = new PointerEventData(EventSystem.current);
            }

            // Update position
            cachedPointerEventData.position = mouse.position.ReadValue();

            // Clear previous results and raycast
            cachedRaycastResults.Clear();
            EventSystem.current.RaycastAll(cachedPointerEventData, cachedRaycastResults);

            return cachedRaycastResults.Count > 0;
        }

        private bool IsMouseOutsideViewport()
        {
            if (mouse == null)
                return true;

            Vector2 mousePos = mouse.position.ReadValue();

            // Calculate viewport boundaries in screen space
            float viewportBottomY = Screen.height * viewportYOffset;
            float viewportTopY = Screen.height * (viewportYOffset + viewportHeight);

            // Check if mouse is outside screen bounds
            if (mousePos.x < 0 || mousePos.x > Screen.width ||
                mousePos.y < 0 || mousePos.y > Screen.height)
            {
                return true;
            }

            // Check if mouse is below viewport (in UI area) or above viewport
            if (mousePos.y < viewportBottomY || mousePos.y > viewportTopY)
            {
                return true;
            }

            return false;
        }

        private void Update()
        {
            // Check for right-click
            if (mouse != null && mouse.rightButton.wasPressedThisFrame)
            {
                HandleRightClick();
            }
        }

        private void HandleRightClick()
        {
            // Don't process right-click if mouse is over UI or outside viewport
            if (IsMouseOverUI() || IsMouseOutsideViewport())
            {
                return;
            }

            // Check for double-click
            bool isDoubleClick = false;
            float currentTime = Time.time;
            if (currentTime - lastRightClickTime <= doubleClickTime)
            {
                isDoubleClick = true;
            }
            lastRightClickTime = currentTime;

            // Get mouse position in world
            Vector2 mousePosition = mouse.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(mousePosition);

            // If double-click, force move even if clicking on enemy
            if (isDoubleClick)
            {
                if (Physics.Raycast(ray, out RaycastHit hitGround2, 1000f, groundLayer))
                {
                    IssueForcedMoveCommand(hitGround2.point);
                }
                return;
            }

            // First, check if we clicked an enemy (attack command)
            if (Physics.Raycast(ray, out RaycastHit hitUnit, 1000f, unitLayer))
            {
                // Check if it's an enemy
                var targetUnit = hitUnit.collider.GetComponent<UnitHealth>();
                if (targetUnit != null && IsEnemy(hitUnit.collider.gameObject))
                {
                    // Attack command
                    IssueAttackCommand(hitUnit.collider.transform);
                    return;
                }
            }

            // If not an enemy, check if we clicked ground (move command)
            if (Physics.Raycast(ray, out RaycastHit hitGround, 1000f, groundLayer))
            {
                // Move command
                IssueMoveCommand(hitGround.point);
            }
        }

        private void IssueMoveCommand(Vector3 destination)
        {
            if (selectionManager == null || selectionManager.SelectionCount == 0)
                return;

            int unitCount = selectionManager.SelectionCount;
            List<Vector3> formationPositions;

            // Calculate formation positions based on current formation setting
            if (unitCount > 1 && formationGroupManager != null && formationGroupManager.ShouldUseFormation())
            {
                FormationType formationType = formationGroupManager.CurrentFormation;
                float spacing = formationGroupManager.GetSpacing(unitCount);

                // Calculate facing direction (from camera to destination)
                Vector3 cameraPos = mainCamera.transform.position;
                Vector3 facingDirection = (destination - cameraPos);
                facingDirection.y = 0;
                facingDirection.Normalize();

                formationPositions = FormationManager.CalculateFormationPositions(
                    destination,
                    unitCount,
                    formationType,
                    spacing,
                    facingDirection
                );

                // Validate positions if enabled in settings
                if (formationGroupManager.FormationSettings != null && formationGroupManager.FormationSettings.validatePositions)
                {
                    formationPositions = FormationManager.ValidateFormationPositions(
                        formationPositions,
                        formationGroupManager.FormationSettings.maxValidationDistance
                    );
                }
            }
            else
            {
                // Single unit or no formation - all go to same point
                formationPositions = new List<Vector3>();
                for (int i = 0; i < unitCount; i++)
                {
                    formationPositions.Add(destination);
                }
            }

            // Move units to their formation positions
            int index = 0;
            foreach (var unit in selectionManager.SelectedUnits)
            {
                if (unit == null || index >= formationPositions.Count) continue;

                Vector3 unitDestination = formationPositions[index];

                // Set as forced move with destination so unit can resume aggro when it arrives
                var aiController = unit.GetComponent<RTS.Units.AI.UnitAIController>();
                if (aiController != null)
                {
                    aiController.SetForcedMove(true, unitDestination);
                }

                var movement = unit.GetComponent<UnitMovement>();
                if (movement != null)
                {
                    movement.SetDestination(unitDestination);
                }

                index++;
            }

            // Show visual feedback
            if (moveMarkerPrefab != null)
            {
                // Use the ground Y position with a small offset to keep marker above ground
                Vector3 spawnPosition = new Vector3(
                    destination.x,
                    destination.y + markerHeightOffset,
                    destination.z
                );

                // Use the prefab's rotation
                Quaternion spawnRotation = moveMarkerPrefab.transform.rotation;

                // Instantiate the marker
                GameObject marker = Instantiate(moveMarkerPrefab, spawnPosition, spawnRotation);

                Destroy(marker, markerLifetime);
            }

            Debug.Log($"Moving {selectionManager.SelectionCount} units to {destination} in formation");
        }

        private void IssueForcedMoveCommand(Vector3 destination)
        {
            if (selectionManager == null || selectionManager.SelectionCount == 0)
                return;

            int unitCount = selectionManager.SelectionCount;
            List<Vector3> formationPositions;

            // Calculate formation positions based on current formation setting
            if (unitCount > 1 && formationGroupManager != null && formationGroupManager.ShouldUseFormation())
            {
                FormationType formationType = formationGroupManager.CurrentFormation;
                float spacing = formationGroupManager.GetSpacing(unitCount);

                // Calculate facing direction (from camera to destination)
                Vector3 cameraPos = mainCamera.transform.position;
                Vector3 facingDirection = (destination - cameraPos);
                facingDirection.y = 0;
                facingDirection.Normalize();

                formationPositions = FormationManager.CalculateFormationPositions(
                    destination,
                    unitCount,
                    formationType,
                    spacing,
                    facingDirection
                );

                // Validate positions if enabled in settings
                if (formationGroupManager.FormationSettings != null && formationGroupManager.FormationSettings.validatePositions)
                {
                    formationPositions = FormationManager.ValidateFormationPositions(
                        formationPositions,
                        formationGroupManager.FormationSettings.maxValidationDistance
                    );
                }
            }
            else
            {
                // Single unit or no formation - all go to same point
                formationPositions = new List<Vector3>();
                for (int i = 0; i < unitCount; i++)
                {
                    formationPositions.Add(destination);
                }
            }

            // Force move all selected units (ignore combat/aggro, resume when reaching destination)
            int index = 0;
            foreach (var unit in selectionManager.SelectedUnits)
            {
                if (unit == null || index >= formationPositions.Count) continue;

                Vector3 unitDestination = formationPositions[index];

                // Clear AI target and set forced move flag with destination
                var aiController = unit.GetComponent<RTS.Units.AI.UnitAIController>();
                if (aiController != null)
                {
                    aiController.SetForcedMove(true, unitDestination);
                }

                var movement = unit.GetComponent<UnitMovement>();
                if (movement != null)
                {
                    movement.SetDestination(unitDestination);
                }

                index++;
            }

            // Show visual feedback
            if (moveMarkerPrefab != null)
            {
                // Use the ground Y position with a small offset to keep marker above ground
                Vector3 spawnPosition = new Vector3(
                    destination.x,
                    destination.y + markerHeightOffset,
                    destination.z
                );

                // Use the prefab's rotation
                Quaternion spawnRotation = moveMarkerPrefab.transform.rotation;

                // Instantiate the marker
                GameObject marker = Instantiate(moveMarkerPrefab, spawnPosition, spawnRotation);

                Destroy(marker, markerLifetime);
            }

            Debug.Log($"FORCED Moving {selectionManager.SelectionCount} units to {destination} in formation");
        }

        private void IssueAttackCommand(Transform target)
        {
            if (selectionManager == null || selectionManager.SelectionCount == 0)
                return;

            // Make all selected units attack the target
            foreach (var unit in selectionManager.SelectedUnits)
            {
                if (unit == null) continue;

                // Clear forced move flag when issuing attack command
                var aiController = unit.GetComponent<RTS.Units.AI.UnitAIController>();
                if (aiController != null)
                {
                    aiController.SetForcedMove(false);
                }

                var combat = unit.GetComponent<UnitCombat>();
                if (combat != null)
                {
                    combat.SetTarget(target);
                }

                var movement = unit.GetComponent<UnitMovement>();
                if (movement != null)
                {
                    movement.FollowTarget(target);
                }
            }

            Debug.Log($"Attacking target with {selectionManager.SelectionCount} units");
        }

        private bool IsEnemy(GameObject obj)
        {
            // Check if object is on enemy layer
            // Adjust layer numbers based on your setup
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            return obj.layer == enemyLayer;
        }

        #region Debug Visualization

        private void OnDrawGizmos()
        {
            // Draw what layers we're detecting
            // This helps debug if clicks aren't working
        }

        #endregion
    }
}
