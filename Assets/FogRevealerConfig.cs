/*
 * FogRevealerConfig.cs
 * Optional component for overriding sight range on individual units/buildings
 * 
 * Usage: Add to unit/building prefab to give it a custom sight range
 * If not present, FogOfWarView will use default or type-based sight ranges
 */

using UnityEngine;

namespace RTS.FogOfWar
{
    /// <summary>
    /// Optional component that allows individual units or buildings to override
    /// their fog of war sight range.
    /// 
    /// Add this to a unit/building prefab if you want that specific entity
    /// to have a different sight range than the default.
    /// </summary>
    public class FogRevealerConfig : MonoBehaviour
    {
        [Header("Sight Range Override")]
        [SerializeField] private bool overrideSightRange = true;
        [SerializeField]
        [Tooltip("Custom sight range for this specific entity (in world units)")]
        private int customSightRange = 10;

        [Header("Update Behavior Override (Optional)")]
        [SerializeField] private bool overrideUpdateBehavior = false;
        [SerializeField]
        [Tooltip("If true, only updates fog when this entity moves")]
        private bool updateOnlyOnMove = true;

        [Header("Debug")]
        [SerializeField] private bool showGizmo = false;
        [SerializeField] private Color gizmoColor = Color.yellow;

        /// <summary>
        /// Whether this entity should use a custom sight range.
        /// </summary>
        public bool OverrideSightRange => overrideSightRange;

        /// <summary>
        /// The custom sight range for this entity.
        /// </summary>
        public int CustomSightRange => customSightRange;

        /// <summary>
        /// Whether this entity overrides the update behavior.
        /// </summary>
        public bool OverrideUpdateBehavior => overrideUpdateBehavior;

        /// <summary>
        /// Whether fog should only update when this entity moves.
        /// </summary>
        public bool UpdateOnlyOnMove => updateOnlyOnMove;

        #region Gizmos

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!showGizmo || !overrideSightRange) return;

            // Draw a wire sphere showing the sight range
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, customSightRange);

            // Draw a label
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2f,
                $"Sight Range: {customSightRange}",
                new GUIStyle()
                {
                    normal = new GUIStyleState() { textColor = gizmoColor },
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                }
            );
        }
#endif

        #endregion

        #region Validation

        private void OnValidate()
        {
            // Ensure sight range is positive
            if (customSightRange < 0)
            {
                customSightRange = 0;
                Debug.LogWarning($"[FogRevealerConfig] Sight range cannot be negative on {gameObject.name}");
            }

            // Warn if sight range is very large
            if (customSightRange > 100)
            {
                Debug.LogWarning($"[FogRevealerConfig] Very large sight range ({customSightRange}) on {gameObject.name}. " +
                                "This may impact performance.");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Change the sight range at runtime.
        /// Requires refreshing the fog revealer to take effect.
        /// </summary>
        public void SetSightRange(int newRange)
        {
            if (newRange < 0)
            {
                Debug.LogWarning($"[FogRevealerConfig] Cannot set negative sight range on {gameObject.name}");
                return;
            }

            customSightRange = newRange;

            // Notify FogOfWarView to refresh this revealer
            var fogView = FindFirstObjectByType<FogOfWarView>();
            if (fogView != null)
            {
                fogView.ManuallyUnregisterRevealer(gameObject);
                fogView.ManuallyRegisterRevealer(gameObject, customSightRange);
            }
        }

        /// <summary>
        /// Enable or disable sight range override.
        /// </summary>
        public void SetOverrideEnabled(bool enabled)
        {
            overrideSightRange = enabled;
        }

        #endregion
    }
}