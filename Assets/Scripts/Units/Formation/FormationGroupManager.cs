using UnityEngine;
using RTS.Core.Events;
using System.Collections.Generic;

namespace RTS.Units.Formation
{
    /// <summary>
    /// Manages formation settings for the currently selected group of units.
    /// Persists formation type across movement commands.
    /// </summary>
    public class FormationGroupManager : MonoBehaviour
    {
        [Header("Current Formation")]
        [SerializeField] private FormationType currentFormation = FormationType.Box;

        [Header("Settings")]
        [SerializeField] private FormationSettingsSO defaultFormationSettings;

        [Header("References")]
        [SerializeField] private UnitSelectionManager selectionManager;
        [SerializeField] private Camera mainCamera;

        public FormationType CurrentFormation
        {
            get => currentFormation;
            set
            {
                if (currentFormation != value)
                {
                    currentFormation = value;
                    Debug.Log($"Formation changed to: {currentFormation}");

                    // Publish event for UI updates
                    EventBus.Publish(new FormationChangedEvent(currentFormation));

                    // Immediately reshape units if any are selected
                    ReshapeSelectedUnits();
                }
            }
        }

        public FormationSettingsSO FormationSettings => defaultFormationSettings;

        private void OnEnable()
        {
            EventBus.Subscribe<SelectionChangedEvent>(OnSelectionChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<SelectionChangedEvent>(OnSelectionChanged);
        }

        private void Start()
        {
            // Initialize with default formation from settings if available
            if (defaultFormationSettings != null)
            {
                currentFormation = defaultFormationSettings.defaultFormationType;
            }

            // Find selection manager if not assigned
            if (selectionManager == null)
            {
                selectionManager = FindObjectOfType<UnitSelectionManager>();
            }

            // Find main camera if not assigned
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        private void OnSelectionChanged(SelectionChangedEvent evt)
        {
            // When selection changes, could reset to default or maintain current
            // For now, we maintain the current formation
        }

        /// <summary>
        /// Reshape currently selected units into the current formation.
        /// Called when formation type changes.
        /// </summary>
        private void ReshapeSelectedUnits()
        {
            if (selectionManager == null || selectionManager.SelectionCount == 0)
                return;

            // Don't reshape if formation is None
            if (currentFormation == FormationType.None)
                return;

            // Calculate center of currently selected units
            Vector3 centerPosition = CalculateGroupCenter();

            // Calculate new formation positions
            int unitCount = selectionManager.SelectionCount;
            float spacing = GetSpacing(unitCount);

            // Calculate facing direction (use camera facing or forward)
            Vector3 facingDirection = Vector3.forward;
            if (mainCamera != null)
            {
                facingDirection = mainCamera.transform.forward;
                facingDirection.y = 0;
                facingDirection.Normalize();
            }

            List<Vector3> formationPositions = FormationManager.CalculateFormationPositions(
                centerPosition,
                unitCount,
                currentFormation,
                spacing,
                facingDirection
            );

            // Validate positions if enabled
            if (defaultFormationSettings != null && defaultFormationSettings.validatePositions)
            {
                formationPositions = FormationManager.ValidateFormationPositions(
                    formationPositions,
                    defaultFormationSettings.maxValidationDistance
                );
            }

            // Move units to their new formation positions
            int index = 0;
            foreach (var unitSelectable in selectionManager.SelectedUnits)
            {
                if (unitSelectable == null || index >= formationPositions.Count) continue;

                var unit = unitSelectable.gameObject;
                Vector3 newPosition = formationPositions[index];

                // Set forced move to new formation position
                var aiController = unit.GetComponent<RTS.Units.AI.UnitAIController>();
                if (aiController != null)
                {
                    aiController.SetForcedMove(true, newPosition);
                }

                var movement = unit.GetComponent<UnitMovement>();
                if (movement != null)
                {
                    movement.SetDestination(newPosition);
                }

                index++;
            }

            Debug.Log($"Reshaped {selectionManager.SelectionCount} units into {currentFormation} formation");
        }

        /// <summary>
        /// Calculate the center position of all currently selected units.
        /// </summary>
        private Vector3 CalculateGroupCenter()
        {
            if (selectionManager == null || selectionManager.SelectionCount == 0)
                return Vector3.zero;

            Vector3 sum = Vector3.zero;
            int count = 0;

            foreach (var unitSelectable in selectionManager.SelectedUnits)
            {
                if (unitSelectable != null)
                {
                    sum += unitSelectable.transform.position;
                    count++;
                }
            }

            return count > 0 ? sum / count : Vector3.zero;
        }

        /// <summary>
        /// Get the spacing to use for current formation.
        /// </summary>
        public float GetSpacing(int unitCount)
        {
            if (defaultFormationSettings != null)
            {
                return defaultFormationSettings.GetSpacingForUnitCount(unitCount);
            }
            return 2.5f; // Default fallback
        }

        /// <summary>
        /// Check if current formation should use position calculation.
        /// </summary>
        public bool ShouldUseFormation()
        {
            return currentFormation != FormationType.None;
        }

        /// <summary>
        /// Reset to default formation.
        /// </summary>
        public void ResetToDefault()
        {
            if (defaultFormationSettings != null)
            {
                CurrentFormation = defaultFormationSettings.defaultFormationType;
            }
            else
            {
                CurrentFormation = FormationType.Box;
            }
        }
    }
}
