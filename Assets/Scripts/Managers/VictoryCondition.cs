using UnityEngine;

namespace RTS.Core
{
    /// <summary>
    /// Abstract base class for victory conditions.
    /// Each victory condition type inherits from this and implements its own logic.
    /// </summary>
    public abstract class VictoryCondition : MonoBehaviour
    {
        [Header("Victory Condition Settings")]
        [SerializeField] protected string conditionName = "Victory Condition";
        [SerializeField] protected string conditionDescription = "Complete this objective to win";

        public string ConditionName => conditionName;
        public string ConditionDescription => conditionDescription;
        public abstract bool IsCompleted { get; }
        public abstract float Progress { get; } // 0-1 for UI display

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
