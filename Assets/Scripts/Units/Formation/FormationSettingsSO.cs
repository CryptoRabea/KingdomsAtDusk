using UnityEngine;

namespace RTS.Units.Formation
{
    /// <summary>
    /// ScriptableObject for configuring formation behavior.
    /// Designer-friendly way to set default formations and spacing.
    /// </summary>
    [CreateAssetMenu(fileName = "FormationSettings", menuName = "RTS/Formation Settings")]
    public class FormationSettingsSO : ScriptableObject
    {
        [Header("Default Formation")]
        [Tooltip("Default formation type when moving multiple units")]
        public FormationType defaultFormationType = FormationType.Box;

        [Header("Spacing")]
        [Tooltip("Minimum spacing between units in formation (meters)")]
        [Range(1f, 10f)]
        public float defaultSpacing = 2.5f;

        [Tooltip("Additional spacing multiplier for larger groups")]
        [Range(1f, 2f)]
        public float largeGroupSpacingMultiplier = 1.2f;

        [Tooltip("Unit count threshold for 'large group' spacing")]
        [Range(5, 50)]
        public int largeGroupThreshold = 15;

        [Header("Formation Validation")]
        [Tooltip("Validate formation positions are on NavMesh")]
        public bool validatePositions = true;

        [Tooltip("Max distance to search for valid NavMesh position")]
        [Range(1f, 10f)]
        public float maxValidationDistance = 5f;

        [Header("Dynamic Adjustment")]
        [Tooltip("Automatically adjust formation based on terrain")]
        public bool adaptToTerrain = true;

        [Tooltip("Prefer wider formations in open areas")]
        public bool preferWideInOpen = true;

        [Tooltip("Prefer narrow formations in tight spaces")]
        public bool preferNarrowInTight = true;

        /// <summary>
        /// Get spacing for a specific unit count.
        /// </summary>
        public float GetSpacingForUnitCount(int unitCount)
        {
            if (unitCount >= largeGroupThreshold)
            {
                return defaultSpacing * largeGroupSpacingMultiplier;
            }
            return defaultSpacing;
        }

        /// <summary>
        /// Get formation type based on unit count and preferences.
        /// </summary>
        public FormationType GetFormationForUnitCount(int unitCount)
        {
            // Single unit - no formation needed
            if (unitCount == 1)
                return FormationType.Box;

            // Small group (2-4 units) - use line
            if (unitCount <= 4)
                return FormationType.Line;

            // Medium group (5-10 units) - use default or box
            if (unitCount <= 10)
                return defaultFormationType == FormationType.Circle ? FormationType.Box : defaultFormationType;

            // Large group - use box for organization
            return FormationType.Box;
        }
    }
}
