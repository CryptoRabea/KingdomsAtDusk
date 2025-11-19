using UnityEngine;
using RTS.Core.Events;
using RTS.Units;

namespace RTS.Units.AI
{
    /// <summary>
    /// Main AI controller for units using the State Machine pattern.
    /// Coordinates between UnitHealth, UnitMovement, and UnitCombat components.
    /// </summary>
    [RequireComponent(typeof(UnitHealth))]
    [RequireComponent(typeof(UnitMovement))]
    [RequireComponent(typeof(UnitCombat))]
    public class UnitAIController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private UnitConfigSO config;
        [SerializeField] private AISettingsSO aiSettings;

        [Header("AI Behavior")]
        [SerializeField] private AIBehaviorType behaviorType = AIBehaviorType.Aggressive;

        [SerializeField] private UnitState currentState;
        [SerializeField] private Transform currentTarget;

        // Component references
        private UnitHealth healthComponent;
        private UnitMovement movementComponent;
        private UnitCombat combatComponent;

        // Aggro tracking
        private Vector3? aggroOriginPosition = null;
        private bool isOnForcedMove = false;
        private Vector3? forcedMoveDestination = null;
        private const float DESTINATION_REACHED_DISTANCE = 3f; // Distance to consider destination reached

        // Public accessors
        public UnitHealth Health => healthComponent;
        public UnitMovement Movement => movementComponent;
        public UnitCombat Combat => combatComponent;
        public Transform CurrentTarget => currentTarget;
        public UnitStateType CurrentStateType => currentState?.GetStateType() ?? UnitStateType.Dead;
        public UnitConfigSO Config => config;
        public AISettingsSO AISettings => aiSettings;
        public Vector3? AggroOriginPosition => aggroOriginPosition;
        public bool IsOnForcedMove => isOnForcedMove;
        public Vector3? ForcedMoveDestination => forcedMoveDestination;

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            InitializeFromConfig();
            ChangeState(new IdleState(this));
        }

        protected virtual void Update()
        {
            currentState?.OnUpdate();
        }

        #region Initialization

        private void InitializeComponents()
        {
            healthComponent = GetComponent<UnitHealth>();
            movementComponent = GetComponent<UnitMovement>();
            combatComponent = GetComponent<UnitCombat>();

            if (healthComponent == null || movementComponent == null || combatComponent == null)
            {
                Debug.LogError($"UnitAIController on {gameObject.name} is missing required components!");
            }
        }

        private void InitializeFromConfig()
        {
            if (config == null)
            {
                Debug.LogWarning($"No UnitConfig assigned to {gameObject.name}");
                return;
            }

            // Apply config to components
            healthComponent?.SetMaxHealth(config.maxHealth);
            movementComponent?.SetSpeed(config.speed);
            combatComponent?.SetAttackDamage(config.attackDamage);
            combatComponent?.SetAttackRange(config.attackRange);
            combatComponent?.SetAttackRate(config.attackRate);
        }

        #endregion

        #region State Machine

        /// <summary>
        /// Change to a new state.
        /// </summary>
        public void ChangeState(UnitState newState)
        {
            if (newState == null) return;

            // Don't change if already dead
            if (currentState != null && currentState.GetStateType() == UnitStateType.Dead)
                return;

            UnitStateType oldStateType = currentState?.GetStateType() ?? UnitStateType.Idle;
            
            currentState?.OnExit();
            currentState = newState;
            currentState.OnEnter();

            // Publish state change event
            EventBus.Publish(new UnitStateChangedEvent(
                gameObject,
                (int)oldStateType,
                (int)currentState.GetStateType()
            ));
        }

        #endregion

        #region Target Management

        /// <summary>
        /// Set the current target.
        /// </summary>
        public void SetTarget(Transform target)
        {
            currentTarget = target;
            combatComponent?.SetTarget(target);

            // Record aggro origin position when first getting a target
            if (target != null && !aggroOriginPosition.HasValue)
            {
                aggroOriginPosition = transform.position;
            }
        }

        /// <summary>
        /// Clear the current target.
        /// </summary>
        public void ClearTarget()
        {
            currentTarget = null;
            combatComponent?.ClearTarget();
        }

        /// <summary>
        /// Clear aggro origin position (called when returning to origin or getting new orders)
        /// </summary>
        public void ClearAggroOrigin()
        {
            aggroOriginPosition = null;
        }

        /// <summary>
        /// Set forced move flag (player overriding AI behavior)
        /// </summary>
        public void SetForcedMove(bool forced, Vector3? destination = null)
        {
            isOnForcedMove = forced;
            if (forced)
            {
                ClearTarget();
                ClearAggroOrigin();
                forcedMoveDestination = destination;
            }
            else
            {
                forcedMoveDestination = null;
            }
        }

        /// <summary>
        /// Check if unit has reached its forced move destination
        /// </summary>
        public bool HasReachedForcedMoveDestination()
        {
            if (!isOnForcedMove || !forcedMoveDestination.HasValue)
                return false;

            float distance = Vector3.Distance(transform.position, forcedMoveDestination.Value);
            return distance <= DESTINATION_REACHED_DISTANCE;
        }

        /// <summary>
        /// Check if unit has exceeded max chase distance from aggro origin
        /// </summary>
        public bool HasExceededChaseDistance()
        {
            if (!aggroOriginPosition.HasValue || config == null)
                return false;

            float distanceFromOrigin = Vector3.Distance(transform.position, aggroOriginPosition.Value);
            return distanceFromOrigin > config.maxChaseDistance;
        }

        /// <summary>
        /// Find a suitable target based on behavior type and AI settings.
        /// </summary>
        public virtual Transform FindTarget()
        {
            if (config == null || aiSettings == null) return null;

            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                config.detectionRange,
                aiSettings.enemyLayer
            );

            if (hits.Length == 0) return null;

            // Different target selection based on behavior type
            return behaviorType switch
            {
                AIBehaviorType.Aggressive => FindNearestTarget(hits),
                AIBehaviorType.Defensive => FindWeakestTarget(hits),
                AIBehaviorType.Support => FindAllyToHeal(hits),
                _ => FindNearestTarget(hits)
            };
        }

        private Transform FindNearestTarget(Collider[] hits)
        {
            Transform nearest = null;
            float minDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                var health = hit.GetComponent<UnitHealth>();
                if (health != null && health.IsDead) continue;

                float distance = Vector3.Distance(transform.position, hit.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = hit.transform;
                }
            }

            return nearest;
        }

        private Transform FindWeakestTarget(Collider[] hits)
        {
            Transform weakest = null;
            float lowestHealth = float.MaxValue;

            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                var health = hit.GetComponent<UnitHealth>();
                if (health == null || health.IsDead) continue;

                if (health.CurrentHealth < lowestHealth)
                {
                    lowestHealth = health.CurrentHealth;
                    weakest = hit.transform;
                }
            }

            return weakest ?? FindNearestTarget(hits);
        }

        private Transform FindAllyToHeal(Collider[] hits)
        {
            // For healers, look for injured allies
            Collider[] allies = Physics.OverlapSphere(
                transform.position,
                config.detectionRange,
                aiSettings.allyLayer
            );

            Transform mostInjured = null;
            float lowestHealthPercent = 1f;

            foreach (var ally in allies)
            {
                if (ally.gameObject == gameObject) continue;

                var health = ally.GetComponent<UnitHealth>();
                if (health == null || health.IsDead) continue;

                float healthPercent = health.HealthPercent;
                if (healthPercent < lowestHealthPercent && healthPercent < 0.8f)
                {
                    lowestHealthPercent = healthPercent;
                    mostInjured = ally.transform;
                }
            }

            return mostInjured;
        }

        #endregion

        #region Combat Logic

        /// <summary>
        /// Check if unit should retreat based on health threshold.
        /// </summary>
        public virtual bool ShouldRetreat()
        {
            // Safety checks
            if (config == null || healthComponent == null)
                return false;

            // If retreat is disabled in config, always return false
            if (!config.canRetreat)
                return false;

            // Otherwise, check health threshold
            return healthComponent.HealthPercent * 100f <= config.retreatThreshold;
        }

        #endregion

        #region Callbacks

        private void OnUnitDied()
        {
            ChangeState(new DeadState(this));
        }

        #endregion

        #region Editor/Debug

        public void SetBehaviorType(AIBehaviorType type)
        {
            behaviorType = type;
        }

        private void OnDrawGizmos()
        {
            if (config == null) return;

            // Draw detection range
            Gizmos.color = new Color(0f, 1f, 0f, 0.1f);
            Gizmos.DrawWireSphere(transform.position, config.detectionRange);

            // Draw state indicator
            Color stateColor = CurrentStateType switch
            {
                UnitStateType.Idle => Color.blue,
                UnitStateType.Moving => Color.yellow,
                UnitStateType.Attacking => Color.red,
                UnitStateType.Retreating => Color.cyan,
                UnitStateType.Healing => Color.green,
                UnitStateType.Dead => Color.black,
                UnitStateType.ReturningToOrigin => Color.magenta,
                _ => Color.white
            };

            Gizmos.color = stateColor;
            Gizmos.DrawSphere(transform.position + Vector3.up * 2f, 0.3f);

            // Draw aggro origin position if it exists
            if (aggroOriginPosition.HasValue)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // Orange
                Gizmos.DrawWireSphere(aggroOriginPosition.Value, 0.5f);
                Gizmos.DrawLine(transform.position, aggroOriginPosition.Value);
            }
        }

        #endregion
    }

    public enum AIBehaviorType
    {
        Aggressive,   // Targets nearest enemy
        Defensive,    // Targets weakest enemy
        Support       // Heals allies (for healers)
    }
}
