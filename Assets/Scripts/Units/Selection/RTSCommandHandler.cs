using UnityEngine;
using UnityEngine.InputSystem;

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
        
        [Header("Settings")]
        [SerializeField] private LayerMask groundLayer; // What counts as ground
        [SerializeField] private LayerMask unitLayer;   // What counts as units
        
        [Header("Visual Feedback")]
        [SerializeField] private GameObject moveMarkerPrefab; // Optional: shows where units will move
        [SerializeField] private float markerLifetime = 1f;

        [Header("Double-Click Settings")]
        [SerializeField] private float doubleClickTime = 0.3f; // Time window for double-click

        private Mouse mouse;
        private float lastRightClickTime = -1f;

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
                if (Physics.Raycast(ray, out RaycastHit hitGround, 1000f, groundLayer))
                {
                    IssueForcedMoveCommand(hitGround.point);
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

            // Move all selected units
            foreach (var unit in selectionManager.SelectedUnits)
            {
                if (unit == null) continue;

                var movement = unit.GetComponent<UnitMovement>();
                if (movement != null)
                {
                    movement.SetDestination(destination);
                }
            }

            // Show visual feedback
            if (moveMarkerPrefab != null)
            {
                // Build the spawn position: X/Z from destination, Y from prefab
                Vector3 spawnPosition = new Vector3(
                    destination.x,
                    moveMarkerPrefab.transform.position.y,
                    destination.z
                );

                // Use the prefab's rotation
                Quaternion spawnRotation = moveMarkerPrefab.transform.rotation;

                // Instantiate the marker
                GameObject marker = Instantiate(moveMarkerPrefab, spawnPosition, spawnRotation);

                Destroy(marker, markerLifetime);

            }

            Debug.Log($"Moving {selectionManager.SelectionCount} units to {destination}");
        }

        private void IssueForcedMoveCommand(Vector3 destination)
        {
            if (selectionManager == null || selectionManager.SelectionCount == 0)
                return;

            // Force move all selected units (ignore combat/aggro)
            foreach (var unit in selectionManager.SelectedUnits)
            {
                if (unit == null) continue;

                // Clear AI target and set forced move flag
                var aiController = unit.GetComponent<RTS.Units.AI.UnitAIController>();
                if (aiController != null)
                {
                    aiController.SetForcedMove(true);
                }

                var movement = unit.GetComponent<UnitMovement>();
                if (movement != null)
                {
                    movement.SetDestination(destination);
                }
            }

            // Show visual feedback
            if (moveMarkerPrefab != null)
            {
                // Build the spawn position: X/Z from destination, Y from prefab
                Vector3 spawnPosition = new Vector3(
                    destination.x,
                    moveMarkerPrefab.transform.position.y,
                    destination.z
                );

                // Use the prefab's rotation
                Quaternion spawnRotation = moveMarkerPrefab.transform.rotation;

                // Instantiate the marker
                GameObject marker = Instantiate(moveMarkerPrefab, spawnPosition, spawnRotation);

                Destroy(marker, markerLifetime);
            }

            Debug.Log($"FORCED Moving {selectionManager.SelectionCount} units to {destination}");
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
