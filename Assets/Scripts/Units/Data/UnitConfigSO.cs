using UnityEngine;
using UnityEngine.UIElements;
using KingdomsAtDusk.Units;

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
        [TextArea(2, 4)]
        public string description = "A unit";

        [Header("Worker Configuration")]
        [Tooltip("If this is a worker unit, specify the worker type. None for combat units.")]
        public WorkerUnitType workerType = WorkerUnitType.None;
        [Tooltip("Is this unit a worker that gathers resources?")]
        public bool isWorker = false;

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
        [Tooltip("Defence value - reduces incoming damage")]
        public int defence = 0;

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

        [Header("Tooltip Display Options")]
        [Tooltip("Show HP in tooltip?")]
        public bool showHP = true;

        [Tooltip("Show Defence in tooltip?")]
        public bool showDefence = false;

        [Tooltip("Show Attack Damage in tooltip?")]
        public bool showAttackDamage = true;

        [Tooltip("Show Attack Range in tooltip?")]
        public bool showAttackRange = true;

        [Tooltip("Show Attack Speed in tooltip?")]
        public bool showAttackSpeed = true;
    }
}
