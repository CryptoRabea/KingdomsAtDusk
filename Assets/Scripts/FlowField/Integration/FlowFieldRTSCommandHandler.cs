using FlowField.Formation;
using FlowField.Movement;
using RTS.Units;
using RTS.Units.Formation;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.FlowField.Integration
{
    /// <summary>
    /// Integration adapter for RTS command system
    /// Replaces NavMesh-based movement with Flow Field movement
    /// Works with existing UnitSelectionManager and RTSCommandHandler
    /// NOTE: This should NOT be active at the same time as RTSCommandHandler
    /// </summary>
    public class FlowFieldRTSCommandHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FlowFieldFormationController formationController;
        [SerializeField] private FormationGroupManager formationGroupManager;

        [Header("Input")]
        [SerializeField] private bool doubleClickForForcedMove = true;
        [SerializeField] private float doubleClickTime = 0.3f;

        [Header("Visual Feedback")]
        [SerializeField] private GameObject moveMarkerPrefab;
        [SerializeField] private float markerLifetime = 2f;

        private Camera mainCamera;
        private float lastClickTime;

        // Cache for selected units
        private List<FlowFieldFollower> selectedUnits = new List<FlowFieldFollower>();

        private void Awake()
        {
            mainCamera = Camera.main;

            if (formationController == null)
            {
                formationController = gameObject.AddComponent<FlowFieldFormationController>();
            }

            if (formationGroupManager == null)
            {
                formationGroupManager = FindFirstObjectByType<FormationGroupManager>();
            }
        }

        private void Update()
        {
            HandleRightClickCommand();
        }

        /// <summary>
        /// Convert FormationGroupManager's FormationType to FlowFieldFormationController's FormationType
        /// </summary>
        private FlowFieldFormationController.FormationType ConvertFormationType(FormationType type)
        {
            switch (type)
            {
                case FormationType.None: return FlowFieldFormationController.FormationType.None;
                case FormationType.Line: return FlowFieldFormationController.FormationType.Line;
                case FormationType.Column: return FlowFieldFormationController.FormationType.Column;
                case FormationType.Box: return FlowFieldFormationController.FormationType.Box;
                case FormationType.Wedge: return FlowFieldFormationController.FormationType.Wedge;
                case FormationType.Circle: return FlowFieldFormationController.FormationType.Circle;
                case FormationType.Scatter: return FlowFieldFormationController.FormationType.Scatter;
                default: return FlowFieldFormationController.FormationType.Box;
            }
        }

        /// <summary>
        /// Get current formation from FormationGroupManager
        /// </summary>
        private FlowFieldFormationController.FormationType GetCurrentFormation()
        {
            if (formationGroupManager != null)
            {
                return ConvertFormationType(formationGroupManager.CurrentFormation);
            }
            return FlowFieldFormationController.FormationType.Box;
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

            // Move units in formation using the shared FormationGroupManager setting
            formationController.MoveUnitsInFormation(
                selectedUnits,
                destination,
                GetCurrentFormation()
            );

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
                GetCurrentFormation()
            );

        }

        /// <summary>
        /// Get currently selected units that have FlowFieldFollower components
        /// </summary>
        private void GetSelectedFlowFieldUnits()
        {
            selectedUnits.Clear();

            // Integration with existing selection system
            // Try to find UnitSelectionManager
            var selectionManager = FindFirstObjectByType<UnitSelectionManager>();

            if (selectionManager != null)
            {
                // Use the public SelectedUnits property instead of GetSelectedUnits()
                var selectedObjects = selectionManager.SelectedUnits;

                foreach (var obj in selectedObjects)
                {
                    if (obj.TryGetComponent<FlowFieldFollower>(out var follower))
                    {
                        selectedUnits.Add(follower);
                    }
                }
            }
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
