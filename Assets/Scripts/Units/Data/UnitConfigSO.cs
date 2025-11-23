using UnityEngine;
using UnityEngine.UIElements;
namespace RTS.Units
{
    /// <summary>
    /// ScriptableObject containing unit configuration data.
    /// Designer-friendly way to configure unit stats.
    /// </summary>
    [CreateAssetMenu(fileName = "UnitConfig", menuName = "RTS/UnitConfig")]
    public class UnitConfigSO : ScriptableObject
    {
        [Header("Identity")]
        public string unitName = "Unit";
        
        [Header("Health")]
        public float maxHealth = 100f;
        [Header("Retreat Settings")]
        [Tooltip("If false, the unit will never retreat.")]
        public bool canRetreat = true;
        [Range(0f, 100f)]
        [Tooltip("Health percentage threshold to trigger retreat (only used if Can Retreat = true).")]
        public float retreatThreshold = 20f; // HP % to retreat


        [Header("Movement")]
        public float speed = 3.5f;
        
        [Header("Combat")]
        public float attackRange = 2f;
        public float attackDamage = 10f;
        public float attackRate = 1f; // attacks per second
        
        [Header("AI")]
        public float detectionRange = 10f; // how far the unit can see enemies
        public float visionRevealRange;
        [Header("Aggro Settings")]
        [Tooltip("Maximum distance from origin position the unit will chase a target")]
        public float maxChaseDistance = 20f;
        [Tooltip("How long (in seconds) the unit will chase an out-of-range target before giving up")]
        public float chaseTimeout = 5f;
        [Tooltip("If true, unit will return to origin position after losing aggro")]
        public bool returnToOriginAfterAggro = true;

        [Header("Visual")]
        public GameObject unitPrefab;
        public Sprite unitIcon;
    }
}
