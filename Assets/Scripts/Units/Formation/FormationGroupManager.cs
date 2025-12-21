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
        private static FormationGroupManager instance;
        public static FormationGroupManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<FormationGroupManager>();
                }
                return instance;
            }
        }

        [Header("Current Formation")]
        [SerializeField] private FormationType currentFormation = FormationType.Box;
        private string currentCustomFormationId = null;

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
                    // Clear custom formation when switching to preset
                    currentCustomFormationId = null;

                    // Publish event for UI updates
                    EventBus.Publish(new FormationChangedEvent(currentFormation));

                    // Immediately reshape units if any are selected
                    ReshapeSelectedUnits();
                }
            }
        }

        public string CurrentCustomFormationId => currentCustomFormationId;

        public bool IsUsingCustomFormation => !string.IsNullOrEmpty(currentCustomFormationId);

        public FormationSettingsSO FormationSettings => defaultFormationSettings;

        /// <summary>
        /// Set the current formation to a custom formation.
        /// </summary>
        public void SetCustomFormation(string formationId)
        {
            CustomFormationData formation = CustomFormationManager.Instance.GetFormation(formationId);
            if (formation != null)
            {
                currentCustomFormationId = formationId;
                // Set to None to indicate custom formation is active
                currentFormation = FormationType.None;

                // Immediately reshape units if any are selected
                ReshapeSelectedUnits();
            }
            else
            {
            }
        }

        /// <summary>
        /// Get the current custom formation data.
        /// </summary>
        public CustomFormationData GetCurrentCustomFormation()
        {
            if (string.IsNullOrEmpty(currentCustomFormationId))
                return null;

            return CustomFormationManager.Instance.GetFormation(currentCustomFormationId);
        }

        /// <summary>
        /// Clear custom formation and revert to preset formation.
        /// </summary>
        public void ClearCustomFormation()
        {
            if (!string.IsNullOrEmpty(currentCustomFormationId))
            {
                currentCustomFormationId = null;
                CurrentFormation = FormationType.Box; // Revert to default
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

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
                selectionManager = Object.FindAnyObjectByType<UnitSelectionManager>();
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

            List<Vector3> formationPositions;

            // Check if using custom formation
            if (IsUsingCustomFormation)
            {
                CustomFormationData customFormation = GetCurrentCustomFormation();
                if (customFormation != null)
                {
                    formationPositions = FormationManager.CalculateCustomFormationPositions(
                        centerPosition,
                        unitCount,
                        customFormation,
                        spacing,
                        facingDirection
                    );
                }
                else
                {
                    formationPositions = FormationManager.CalculateFormationPositions(
                        centerPosition,
                        unitCount,
                        FormationType.Box,
                        spacing,
                        facingDirection
                    );
                }
            }
            else
            {
                // Don't reshape if formation is None and no custom formation
                if (currentFormation == FormationType.None)
                    return;

                formationPositions = FormationManager.CalculateFormationPositions(
                    centerPosition,
                    unitCount,
                    currentFormation,
                    spacing,
                    facingDirection
                );
            }

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
                if (unit.TryGetComponent<RTS.Units.AI.UnitAIController>(out var aiController))
                {
                    aiController.SetForcedMove(true, newPosition);
                }

                if (unit.TryGetComponent<UnitMovement>(out var movement))
                {
                    movement.SetDestination(newPosition);
                }

                index++;
            }

            string formationName = IsUsingCustomFormation ? GetCurrentCustomFormation()?.name : currentFormation.ToString();
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
            return currentFormation != FormationType.None || IsUsingCustomFormation;
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
