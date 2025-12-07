using UnityEngine;
using RTS.Units;
using RTS.Core.Events;

namespace RTS.Animals
{
    /// <summary>
    /// Main component controlling animal AI behavior.
    /// Handles roaming, fleeing, and reactions to threats.
    /// </summary>
    [RequireComponent(typeof(UnitMovement))]
    [RequireComponent(typeof(UnitHealth))]
    public class AnimalBehavior : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private AnimalConfigSO config;

        [Header("Roaming Settings")]
        [SerializeField] private float roamingRadius = 15f;
        [SerializeField] private float roamingInterval = 5f;
        [SerializeField] private float idleTimeMin = 2f;
        [SerializeField] private float idleTimeMax = 5f;

        [Header("Detection Settings")]
        [SerializeField] private float detectionRange = 8f;
        [SerializeField] private float detectionCheckInterval = 0.5f;
        [SerializeField] private LayerMask threatLayers;

        [Header("Flee Settings")]
        [SerializeField] private bool fleesWhenAttacked = true;
        [SerializeField] private float fleeDistance = 15f;
        [SerializeField] private float fleeDuration = 5f;

        private UnitMovement movement;
        private UnitHealth health;
        private Vector3 spawnPosition;

        // State
        private AnimalState currentState = AnimalState.Idle;
        private float stateTimer;
        private float detectionTimer;
        private Vector3 fleeTarget;
        private float previousHealth;

        public AnimalConfigSO Config => config;
        public AnimalState CurrentState => currentState;

        private void Awake()
        {
            movement = GetComponent<UnitMovement>();
            health = GetComponent<UnitHealth>();
            spawnPosition = transform.position;
            previousHealth = health.MaxHealth;
        }

        private void Start()
        {
            // Initialize from config if available
            if (config != null)
            {
                roamingRadius = config.roamingRadius;
                roamingInterval = config.roamingInterval;
                detectionRange = config.detectionRange;
                fleesWhenAttacked = config.fleesWhenAttacked;
                fleeDistance = config.fleeDistance;

                movement.SetSpeed(config.moveSpeed);
                health.SetMaxHealth(config.maxHealth);
            }

            // Start in idle state
            ChangeState(AnimalState.Idle);
        }

        private void Update()
        {
            // Check for damage
            if (health.CurrentHealth < previousHealth && fleesWhenAttacked)
            {
                // Animal was damaged, flee!
                StartFleeing();
            }
            previousHealth = health.CurrentHealth;

            // Detection check
            detectionTimer += Time.deltaTime;
            if (detectionTimer >= detectionCheckInterval)
            {
                detectionTimer = 0f;
                CheckForThreats();
            }

            // State machine
            stateTimer += Time.deltaTime;

            switch (currentState)
            {
                case AnimalState.Idle:
                    UpdateIdle();
                    break;
                case AnimalState.Roaming:
                    UpdateRoaming();
                    break;
                case AnimalState.Fleeing:
                    UpdateFleeing();
                    break;
            }
        }

        #region State Machine

        private void ChangeState(AnimalState newState)
        {
            currentState = newState;
            stateTimer = 0f;

            switch (newState)
            {
                case AnimalState.Idle:
                    movement.Stop();
                    break;
                case AnimalState.Roaming:
                    StartRoaming();
                    break;
                case AnimalState.Fleeing:
                    // Flee logic handled in StartFleeing()
                    break;
            }
        }

        private void UpdateIdle()
        {
            float idleTime = Random.Range(idleTimeMin, idleTimeMax);

            if (stateTimer >= idleTime)
            {
                ChangeState(AnimalState.Roaming);
            }
        }

        private void UpdateRoaming()
        {
            if (movement.HasReachedDestination || stateTimer >= roamingInterval * 2f)
            {
                ChangeState(AnimalState.Idle);
            }
        }

        private void UpdateFleeing()
        {
            if (movement.HasReachedDestination || stateTimer >= fleeDuration)
            {
                // Stop fleeing, return to normal behavior
                ChangeState(AnimalState.Idle);
            }
        }

        #endregion

        #region Roaming

        private void StartRoaming()
        {
            Vector3 randomPoint = GetRandomPointInRadius(spawnPosition, roamingRadius);
            movement.SetDestination(randomPoint);
        }

        private Vector3 GetRandomPointInRadius(Vector3 center, float radius)
        {
            // Get random point on XZ plane
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            Vector3 randomPoint = center + new Vector3(randomCircle.x, 0f, randomCircle.y);

            // Sample terrain height at this position
            if (Physics.Raycast(randomPoint + Vector3.up * 100f, Vector3.down, out RaycastHit hit, 200f, LayerMask.GetMask("Ground", "Terrain")))
            {
                randomPoint.y = hit.point.y;
            }

            return randomPoint;
        }

        #endregion

        #region Threat Detection & Fleeing

        private void CheckForThreats()
        {
            if (currentState == AnimalState.Fleeing) return; // Already fleeing

            Collider[] threats = Physics.OverlapSphere(transform.position, detectionRange, threatLayers);

            if (threats.Length > 0)
            {
                // Flee from nearest threat
                StartFleeingFrom(threats[0].transform.position);
            }
        }

        private void StartFleeing()
        {
            // Flee in opposite direction from last damage source
            // For now, just flee in a random direction away from spawn
            Vector3 fleeDirection = (transform.position - spawnPosition).normalized;
            if (fleeDirection.sqrMagnitude < 0.1f)
            {
                fleeDirection = Random.insideUnitSphere;
                fleeDirection.y = 0f;
                fleeDirection.Normalize();
            }

            Vector3 fleePoint = transform.position + fleeDirection * fleeDistance;
            fleeTarget = GetRandomPointInRadius(fleePoint, 3f);

            movement.SetDestination(fleeTarget);
            ChangeState(AnimalState.Fleeing);

        }

        private void StartFleeingFrom(Vector3 threatPosition)
        {
            Vector3 fleeDirection = (transform.position - threatPosition).normalized;
            Vector3 fleePoint = transform.position + fleeDirection * fleeDistance;
            fleeTarget = GetRandomPointInRadius(fleePoint, 3f);

            movement.SetDestination(fleeTarget);
            ChangeState(AnimalState.Fleeing);

        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize this animal with a specific config.
        /// </summary>
        public void Initialize(AnimalConfigSO animalConfig, Vector3 position)
        {
            config = animalConfig;
            spawnPosition = position;
            transform.position = position;

            if (config != null)
            {
                roamingRadius = config.roamingRadius;
                roamingInterval = config.roamingInterval;
                detectionRange = config.detectionRange;
                fleesWhenAttacked = config.fleesWhenAttacked;
                fleeDistance = config.fleeDistance;

                movement.SetSpeed(config.moveSpeed);
                health.SetMaxHealth(config.maxHealth);
            }

            ChangeState(AnimalState.Idle);
        }

        /// <summary>
        /// Force this animal to flee.
        /// </summary>
        public void Flee()
        {
            StartFleeing();
        }

        #endregion

        #region Death

        private void OnUnitDied()
        {
            // Animal died - publish event
            EventBus.Publish(new AnimalDiedEvent(gameObject, config?.animalType ?? AnimalType.Sheep));

            // Destroy after a delay (corpse persistence)
            Destroy(gameObject, 5f);
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            // Draw roaming radius
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnPosition, roamingRadius);

            // Draw detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // Draw flee target
            if (currentState == AnimalState.Fleeing)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, fleeTarget);
                Gizmos.DrawWireSphere(fleeTarget, 1f);
            }
        }

        #endregion
    }

    /// <summary>
    /// Animal AI states.
    /// </summary>
    public enum AnimalState
    {
        Idle,       // Standing still
        Roaming,    // Walking around randomly
        Fleeing,    // Running from threats
        Grazing     // Eating (future feature)
    }
}
