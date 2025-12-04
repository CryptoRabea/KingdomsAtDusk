using FlowField.Formation;
using FlowField.Movement;
using RTS.Units;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.FlowField.Integration
{
    /// <summary>
    /// Integration adapter for RTS command system
    /// Replaces NavMesh-based movement with Flow Field movement
    /// Works with existing UnitSelectionManager and RTSCommandHandler
    /// </summary>
    public class FlowFieldRTSCommandHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FlowFieldFormationController formationController;

        [Header("Formation")]
        [SerializeField]
        private FlowFieldFormationController.FormationType defaultFormation =
            FlowFieldFormationController.FormationType.Box;

        [Header("Input")]
        [SerializeField] private bool doubleClickForForcedMove = true;
        [SerializeField] private float doubleClickTime = 0.3f;

        [Header("Visual Feedback")]
        [SerializeField] private GameObject moveMarkerPrefab;
        [SerializeField] private float markerLifetime = 2f;

        private Camera mainCamera;
        private float lastClickTime;
        private FlowFieldFormationController.FormationType currentFormation;

        // Cache for selected units
        private List<FlowFieldFollower> selectedUnits = new List<FlowFieldFollower>();

        private void Awake()
        {
            mainCamera = Camera.main;
            currentFormation = defaultFormation;

            if (formationController == null)
            {
                formationController = gameObject.AddComponent<FlowFieldFormationController>();
            }
        }

        private void Update()
        {
            HandleRightClickCommand();
            HandleFormationHotkeys();
        }

        /// <summary>
        /// Handle right-click move/attack commands
        /// </summary>
        private void HandleRightClickCommand()
        {
            if (Input.GetMouseButtonDown(1)) // Right click
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    // Check for double-click (forced move)
                    bool isForcedMove = false;
                    if (doubleClickForForcedMove)
                    {
                        if (Time.time - lastClickTime < doubleClickTime)
                        {
                            isForcedMove = true;
                        }
                        lastClickTime = Time.time;
                    }

                    // Check what was clicked
                    if (hit.collider.CompareTag("Enemy") && !isForcedMove)
                    {
                        // Attack command
                        IssueAttackCommand(hit.collider.gameObject);
                    }
                    else
                    {
                        // Move command
                        IssueMoveCommand(hit.point, isForcedMove);
                    }

                    // Spawn visual marker
                    SpawnMoveMarker(hit.point);
                }
            }
        }

        /// <summary>
        /// Issue move command to selected units
        /// </summary>
        private void IssueMoveCommand(Vector3 destination, bool forcedMove = false)
        {
            GetSelectedFlowFieldUnits();

            if (selectedUnits.Count == 0)
                return;

            // Move units in formation
            formationController.MoveUnitsInFormation(
                selectedUnits,
                destination,
                currentFormation
            );

            UnityEngine.Debug.Log($"Moving {selectedUnits.Count} units to {destination} " +
                      $"in {currentFormation} formation (Forced: {forcedMove})");
        }

        /// <summary>
        /// Issue attack command to selected units
        /// </summary>
        private void IssueAttackCommand(GameObject target)
        {
            GetSelectedFlowFieldUnits();

            if (selectedUnits.Count == 0)
                return;

            // Move to target position (combat system will handle actual attacking)
            Vector3 targetPosition = target.transform.position;

            formationController.MoveUnitsInFormation(
                selectedUnits,
                targetPosition,
                currentFormation
            );

            UnityEngine.Debug.Log($"Attack command: {selectedUnits.Count} units attacking {target.name}");
        }

        /// <summary>
        /// Get currently selected units that have FlowFieldFollower components
        /// </summary>
        private void GetSelectedFlowFieldUnits()
        {
            selectedUnits.Clear();

            // Integration with existing selection system
            // Try to find UnitSelectionManager
            var selectionManager = FindObjectOfType<UnitSelectionManager>();

            if (selectionManager != null)
            {
                // Use the public SelectedUnits property instead of GetSelectedUnits()
                var selectedObjects = selectionManager.SelectedUnits;

                foreach (var obj in selectedObjects)
                {
                    var follower = obj.GetComponent<FlowFieldFollower>();
                    if (follower != null)
                    {
                        selectedUnits.Add(follower);
                    }
                }
            }
        }

        /// <summary>
        /// Handle formation hotkeys (F1-F6 for different formations)
        /// </summary>
        private void HandleFormationHotkeys()
        {
            if (Input.GetKeyDown(KeyCode.F1))
                SetFormation(FlowFieldFormationController.FormationType.Line);

            if (Input.GetKeyDown(KeyCode.F2))
                SetFormation(FlowFieldFormationController.FormationType.Column);

            if (Input.GetKeyDown(KeyCode.F3))
                SetFormation(FlowFieldFormationController.FormationType.Box);

            if (Input.GetKeyDown(KeyCode.F4))
                SetFormation(FlowFieldFormationController.FormationType.Wedge);

            if (Input.GetKeyDown(KeyCode.F5))
                SetFormation(FlowFieldFormationController.FormationType.Circle);

            if (Input.GetKeyDown(KeyCode.F6))
                SetFormation(FlowFieldFormationController.FormationType.Scatter);
        }

        /// <summary>
        /// Set current formation type
        /// </summary>
        public void SetFormation(FlowFieldFormationController.FormationType formation)
        {
            currentFormation = formation;
            UnityEngine.Debug.Log($"Formation changed to: {formation}");
        }

        /// <summary>
        /// Spawn visual feedback marker
        /// </summary>
        private void SpawnMoveMarker(Vector3 position)
        {
            if (moveMarkerPrefab == null)
                return;

            GameObject marker = Instantiate(moveMarkerPrefab, position, Quaternion.identity);
            Destroy(marker, markerLifetime);
        }

        /// <summary>
        /// Public API for external systems
        /// </summary>
        public void MoveSelectedUnits(Vector3 destination)
        {
            IssueMoveCommand(destination, false);
        }

        public void AttackTarget(GameObject target)
        {
            IssueAttackCommand(target);
        }

        public void StopSelectedUnits()
        {
            GetSelectedFlowFieldUnits();

            foreach (var unit in selectedUnits)
            {
                unit.Stop();
            }
        }
    }
}
