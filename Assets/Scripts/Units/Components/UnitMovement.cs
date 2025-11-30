using UnityEngine;
using UnityEngine.AI;

namespace RTS.Units
{
    /// <summary>
    /// Component handling unit movement via NavMesh.
    /// Modular and reusable across different unit types.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class UnitMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float rotationSpeed = 120f;
        [SerializeField] private float stoppingDistance = 0.5f; // Increased from 0.1 to prevent units from constantly trying to reach exact point
        [SerializeField] private float pathUpdateInterval = 0.5f;
        [SerializeField] private float arrivalThreshold = 1.5f; // Distance to consider "close enough" to destination

        [Header("Avoidance Settings")]
        [SerializeField] private float avoidanceRadius = 0.5f;
        [SerializeField] private int avoidancePriority = 50;
        #pragma warning disable CS0414 // Field is assigned but never used - reserved for future separation force feature
        [SerializeField] private float separationWeight = 1.5f;
        #pragma warning restore CS0414

        [Header("Stuck Detection")]
        [SerializeField] private float stuckCheckInterval = 1.0f;
        [SerializeField] private float stuckThreshold = 0.1f; // Minimum distance to move per check
        [SerializeField] private int maxStuckChecks = 3; // Number of failed checks before considering stuck
        [SerializeField] private float pathFailureTimeout = 2.0f; // Time to wait for path calculation

        private NavMeshAgent agent;
        private Transform currentTarget;
        private Vector3 currentDestination;
        private float pathUpdateTimer;
        private bool hasDestination = false;
        private bool isEnabled = true;
        private bool hasMovementIntent = false; // Track movement intent before velocity updates

        // Stuck detection
        private Vector3 lastStuckCheckPosition;
        private float stuckCheckTimer;
        private int consecutiveStuckChecks;
        private bool isStuck = false;
        private float pathPendingTimer;

        // IsMoving is based on actual velocity and movement intent, not explicit stuck check
        // When unit gets stuck, it physically stops, making velocity = 0 and IsMoving naturally becomes false
        public bool IsMoving => agent != null && (hasMovementIntent || agent.velocity.sqrMagnitude > 0.01f);
        public bool HasReachedDestination => hasDestination && !agent.pathPending &&
            (agent.remainingDistance <= stoppingDistance || IsCloseEnoughToDestination());
        public bool IsStuck => isStuck;
        public bool HasValidPath => agent != null && agent.hasPath && agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathComplete;
        public float Speed => moveSpeed;
        public Vector3 Velocity => agent != null ? agent.velocity : Vector3.zero;

        /// <summary>
        /// Check if unit is close enough to destination to be considered "arrived"
        /// Helps prevent units from constantly trying to reach exact point when blocked by others
        /// </summary>
        private bool IsCloseEnoughToDestination()
        {
            if (!hasDestination || agent == null) return false;

            float distanceToDestination = Vector3.Distance(transform.position, currentDestination);
            return distanceToDestination <= arrivalThreshold;
        }

        private void Awake()
        {
            InitializeAgent();
        }

        private void InitializeAgent()
        {
            agent = GetComponent<NavMeshAgent>();

            if (agent != null)
            {
                agent.speed = moveSpeed;
                agent.angularSpeed = rotationSpeed;
                agent.stoppingDistance = stoppingDistance;
                agent.autoBraking = true;
                agent.updateRotation = true;

                // Configure avoidance for better group movement
                agent.radius = avoidanceRadius;
                agent.avoidancePriority = avoidancePriority;
                agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.HighQualityObstacleAvoidance;

                // Optimize for simultaneous group movement
                agent.acceleration = 100f; // High acceleration for instant response
                agent.autoRepath = true; // Auto recalculate path when blocked

                // Initialize stuck detection
                lastStuckCheckPosition = transform.position;
                stuckCheckTimer = 0f;
                consecutiveStuckChecks = 0;
                isStuck = false;
                pathPendingTimer = 0f;
            }
            else
            {
                Debug.LogError($"NavMeshAgent not found on {gameObject.name}!");
            }
        }

        private void Update()
        {
            if (!isEnabled || agent == null) return;

            // Clear movement intent when destination is reached
            if (hasMovementIntent && HasReachedDestination)
            {
                hasMovementIntent = false;
                isStuck = false;
                consecutiveStuckChecks = 0;

                // Physically stop the agent when destination reached
                if (agent.enabled && agent.isOnNavMesh && !agent.isStopped)
                {
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                }
            }

            // Update path to moving targets periodically
            if (currentTarget != null)
            {
                pathUpdateTimer += Time.deltaTime;
                if (pathUpdateTimer >= pathUpdateInterval)
                {
                    pathUpdateTimer = 0f;
                    SetDestination(currentTarget.position);
                }
            }

            // Stuck detection - only check if we haven't reached destination yet
            if (hasDestination && hasMovementIntent && !HasReachedDestination)
            {
                UpdateStuckDetection();
                UpdatePathFailureDetection();
            }
        }

        #region Movement Control

        /// <summary>
        /// Move to a specific position.
        /// </summary>
        public void SetDestination(Vector3 destination)
        {
            if (!isEnabled)
            {
                Debug.LogWarning($"[WARNING] UnitMovement: Cannot set destination for {gameObject.name} - movement is disabled!");
                return;
            }

            if (agent == null)
            {
                Debug.LogError($"[ERROR] UnitMovement: Cannot set destination for {gameObject.name} - NavMeshAgent is null!");
                return;
            }

            currentDestination = destination;
            currentTarget = null;
            hasDestination = true;
            hasMovementIntent = true; // Signal movement intent immediately

            // Clear stuck state and resume agent if it was stopped
            if (isStuck)
            {
                ResetStuckState();
            }

            if (agent.enabled && agent.isOnNavMesh)
            {
                // Ensure agent is not stopped before setting destination
                if (agent.isStopped)
                {
                    agent.isStopped = false;
                }

                // Set destination - high acceleration ensures immediate response
                agent.SetDestination(destination);
            }
            else
            {
                Debug.LogWarning($"[WARNING] UnitMovement: Cannot set destination for {gameObject.name} - NavMeshAgent is disabled!");
            }
        }

        /// <summary>
        /// Follow a moving target.
        /// </summary>
        public void FollowTarget(Transform target)
        {
            if (!isEnabled || agent == null || target == null) return;

            currentTarget = target;
            hasDestination = true;
            hasMovementIntent = true; // Signal movement intent immediately
            pathUpdateTimer = pathUpdateInterval; // Update immediately

            // Clear stuck state and resume agent if it was stopped
            if (isStuck)
            {
                ResetStuckState();
            }

            if (agent.enabled && agent.isOnNavMesh)
            {
                // Ensure agent is not stopped before setting destination
                if (agent.isStopped)
                {
                    agent.isStopped = false;
                }

                agent.SetDestination(target.position);
            }
        }

        /// <summary>
        /// Stop all movement.
        /// </summary>
        public void Stop()
        {
            if (agent == null) return;

            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
                // Ensure agent is stopped
                if (!agent.isStopped)
                {
                    agent.isStopped = true;
                }
                agent.velocity = Vector3.zero;
            }

            currentTarget = null;
            hasDestination = false;
            hasMovementIntent = false; // Clear movement intent when stopped
            isStuck = false;
            consecutiveStuckChecks = 0;
        }

        /// <summary>
        /// Set movement speed.
        /// </summary>
        public void SetSpeed(float speed)
        {
            moveSpeed = Mathf.Max(0, speed);
            if (agent != null)
            {
                agent.speed = moveSpeed;
            }
        }

        /// <summary>
        /// Enable or disable movement.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;

            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = !enabled;
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// Get distance to current destination.
        /// </summary>
        public float GetDistanceToDestination()
        {
            if (!hasDestination || agent == null || agent.pathPending)
                return float.MaxValue;

            return agent.remainingDistance;
        }

        /// <summary>
        /// Get distance to a specific position.
        /// </summary>
        public float GetDistanceTo(Vector3 position)
        {
            return Vector3.Distance(transform.position, position);
        }

        /// <summary>
        /// Check if within range of a position.
        /// </summary>
        public bool IsInRangeOf(Vector3 position, float range)
        {
            return GetDistanceTo(position) <= range;
        }

        /// <summary>
        /// Check if within range of a target transform.
        /// </summary>
        public bool IsInRangeOf(Transform target, float range)
        {
            return target != null && IsInRangeOf(target.position, range);
        }

        /// <summary>
        /// Look at a target over time.
        /// </summary>
        public void LookAt(Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0; // Keep on horizontal plane

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }

        #endregion

        #region Callbacks

        private void OnDestroy()
        {
            // Critical: Clean up NavMeshAgent and references to prevent memory leaks
            // Must check isOnNavMesh to avoid errors when agent is not placed on NavMesh
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            currentTarget = null;
            hasDestination = false;
            hasMovementIntent = false;
        }

        private void OnUnitDied()
        {
            // Disable movement when unit dies
            SetEnabled(false);
            Stop();
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            if (agent != null && agent.hasPath)
            {
                Gizmos.color = Color.yellow;
                Vector3[] corners = agent.path.corners;

                for (int i = 0; i < corners.Length - 1; i++)
                {
                    Gizmos.DrawLine(corners[i], corners[i + 1]);
                }
            }

            if (currentTarget != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, currentTarget.position);
            }

            // Draw stuck indicator
            if (isStuck)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position + Vector3.up, 0.5f);
            }
        }

        

        #region Stuck Detection

        private void UpdateStuckDetection()
        {
            stuckCheckTimer += Time.deltaTime;

            if (stuckCheckTimer >= stuckCheckInterval)
            {
                stuckCheckTimer = 0f;

                // Check if we're close enough to destination - if so, not stuck, just arrived
                if (IsCloseEnoughToDestination())
                {
                    consecutiveStuckChecks = 0;
                    return;
                }

                // Check if unit has moved significantly since last check
                float distanceMoved = Vector3.Distance(transform.position, lastStuckCheckPosition);

                if (distanceMoved < stuckThreshold && hasMovementIntent && !HasReachedDestination)
                {
                    consecutiveStuckChecks++;

                    if (consecutiveStuckChecks >= maxStuckChecks)
                    {
                        // Unit is stuck - physically stop it
                        if (!isStuck)
                        {
                            isStuck = true;
                            Debug.LogWarning($"Unit {gameObject.name} detected as stuck at {transform.position}");

                            // Stop the agent so velocity goes to 0 and animations naturally transition to idle
                            if (agent != null && agent.enabled && agent.isOnNavMesh)
                            {
                                agent.isStopped = true;
                                agent.velocity = Vector3.zero;
                            }
                            hasMovementIntent = false;
                        }
                    }
                }
                else
                {
                    // Unit is moving, reset stuck counter
                    consecutiveStuckChecks = 0;
                    if (isStuck && distanceMoved >= stuckThreshold)
                    {
                        isStuck = false;
                        Debug.Log($"Unit {gameObject.name} is no longer stuck");

                        // Resume agent movement
                        if (agent != null && agent.enabled && agent.isOnNavMesh)
                        {
                            agent.isStopped = false;
                        }
                    }
                }

                lastStuckCheckPosition = transform.position;
            }
        }

        private void UpdatePathFailureDetection()
        {
            if (agent == null || !agent.enabled) return;

            // Check if path is pending for too long
            if (agent.pathPending)
            {
                pathPendingTimer += Time.deltaTime;

                if (pathPendingTimer >= pathFailureTimeout)
                {
                    Debug.LogWarning($"Unit {gameObject.name} - path calculation timeout. Path may be invalid.");
                    isStuck = true;
                    pathPendingTimer = 0f;

                    // Physically stop the agent
                    if (agent.isOnNavMesh)
                    {
                        agent.isStopped = true;
                        agent.velocity = Vector3.zero;
                    }
                    hasMovementIntent = false;
                }
            }
            else
            {
                pathPendingTimer = 0f;

                // Check if path is invalid
                if (agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathInvalid)
                {
                    Debug.LogWarning($"Unit {gameObject.name} - invalid path to {currentDestination}");
                    isStuck = true;

                    // Physically stop the agent
                    if (agent.isOnNavMesh)
                    {
                        agent.isStopped = true;
                        agent.velocity = Vector3.zero;
                    }
                    hasMovementIntent = false;
                }
                else if (agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathPartial)
                {
                    Debug.LogWarning($"Unit {gameObject.name} - partial path to {currentDestination}");
                    // Partial paths are acceptable, but log for debugging
                }
            }
        }

        /// <summary>
        /// Reset stuck state and try to resume movement.
        /// </summary>
        public void ResetStuckState()
        {
            isStuck = false;
            consecutiveStuckChecks = 0;
            stuckCheckTimer = 0f;
            lastStuckCheckPosition = transform.position;

            // Resume agent if it was stopped
            if (agent != null && agent.enabled && agent.isOnNavMesh && agent.isStopped)
            {
                agent.isStopped = false;
            }

            Debug.Log($"Unit {gameObject.name} stuck state reset");
        }

        #endregion
    }
}
