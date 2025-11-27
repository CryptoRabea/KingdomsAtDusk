using UnityEngine;
using RTS.Core.Events;

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
        }

        private void OnSelectionChanged(SelectionChangedEvent evt)
        {
            // When selection changes, could reset to default or maintain current
            // For now, we maintain the current formation
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
