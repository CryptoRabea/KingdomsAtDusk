using UnityEngine;

namespace RTS.Core
{
    /// <summary>
    /// Abstract base class for defeat conditions.
    /// Each defeat condition type inherits from this and implements its own logic.
    /// </summary>
    public abstract class DefeatCondition : MonoBehaviour
    {
        [Header("Defeat Condition Settings")]
        [SerializeField] protected string conditionName = "Defeat Condition";
        [SerializeField] protected string conditionDescription = "If this happens, you lose";

        public string ConditionName => conditionName;
        public string ConditionDescription => conditionDescription;
        public abstract bool IsFailed { get; }

        /// <summary>
        /// Called when the condition is initialized
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// Called when the condition should clean up
        /// </summary>
        public abstract void Cleanup();

        /// <summary>
        /// Get a user-friendly status string
        /// </summary>
        public abstract string GetStatusText();

        protected virtual void OnDestroy()
        {
            Cleanup();
        }
    }
}
