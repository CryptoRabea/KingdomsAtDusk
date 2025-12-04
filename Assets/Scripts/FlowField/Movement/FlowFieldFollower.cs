using UnityEngine;
using FlowField.Core;
using Debug = UnityEngine.Debug;

namespace FlowField.Movement
{
    /// <summary>
    /// Unit component that follows flow fields with smooth, natural movement
    /// Replaces NavMeshAgent for large-scale RTS unit movement
    /// Features: velocity smoothing, local avoidance, formation offsets
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class FlowFieldFollower : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float maxSpeed = 5f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float turnSpeed = 720f; // degrees per second
        [SerializeField] private float stoppingDistance = 0.5f;

        [Header("Avoidance")]
        [SerializeField] private bool enableLocalAvoidance = true;
        [SerializeField] private float avoidanceRadius = 2f;
        [SerializeField] private float separationWeight = 1.5f;
        [SerializeField] private float unitRadius = 0.5f;

        [Header("Smoothing")]
        [SerializeField] private float velocityDamping = 0.15f; // Critical damping coefficient
        [SerializeField] private bool enableMovementSmoothing = true;
        [SerializeField] private float arrivalSlowdownRadius = 3f;

        [Header("Formation")]
        [SerializeField] private Vector3 formationOffset = Vector3.zero;
        [SerializeField] private float formationWeight = 0.8f;

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = false;

        // Components
        private Rigidbody rb;
        private FlowFieldManager flowFieldManager;
        private LocalAvoidance localAvoidance;

        // Movement state
        private Vector3 currentVelocity;
        private Vector3 desiredVelocity;
        private Vector3 currentDestination;
        private bool hasDestination;
        private bool hasReachedDestination;

        // Velocity smoothing (critical damping)
        private Vector3 velocitySmoothDampVelocity;

        // Public properties
        public Vector3 CurrentVelocity => currentVelocity;
        public float Radius => unitRadius;
        public bool IsMoving => currentVelocity.sqrMagnitude > 0.01f;
        public bool HasReachedDestination => hasReachedDestination;
        public Vector3 Destination => currentDestination;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();

            // Configure Rigidbody for kinematic movement
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotationX |
                            RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void Start()
        {
            flowFieldManager = FlowFieldManager.Instance;

            if (flowFieldManager == null)
            {
                UnityEngine.Debug.LogError("FlowFieldManager not found! Add FlowFieldManager to scene.");
                enabled = false;
                return;
            }

            // Create local avoidance instance (can be shared across units for performance)
            if (enableLocalAvoidance)
            {
                localAvoidance = new LocalAvoidance(avoidanceRadius, 10, 1.5f);
            }
        }

        private void FixedUpdate()
        {
            if (!hasDestination)
            {
                // No destination, gradually stop
                SlowDown();
                return;
            }

            // Check if reached destination
            float distanceToDestination = Vector3.Distance(transform.position, currentDestination);

            if (distanceToDestination < stoppingDistance)
            {
                hasReachedDestination = true;
                hasDestination = false;
                SlowDown();
                return;
            }

            // Calculate movement
            CalculateMovement();

            // Apply movement
            ApplyMovement();
        }

        /// <summary>
        /// Set movement destination
        /// </summary>
        public void SetDestination(Vector3 destination)
        {
            currentDestination = destination;
            hasDestination = true;
            hasReachedDestination = false;

            // Request flow field generation
            flowFieldManager.GenerateFlowField(destination);
        }

        /// <summary>
        /// Set formation offset for this unit
        /// </summary>
        public void SetFormationOffset(Vector3 offset)
        {
            formationOffset = offset;
        }

        /// <summary>
        /// Stop movement
        /// </summary>
        public void Stop()
        {
            hasDestination = false;
            hasReachedDestination = false;
            desiredVelocity = Vector3.zero;
        }

        /// <summary>
        /// Calculate desired velocity from flow field + formation + avoidance
        /// </summary>
        private void CalculateMovement()
        {
            // 1. Sample flow field direction
            Vector2 flowDirection = flowFieldManager.SampleFlowDirection(transform.position);
            Vector3 flowVelocity = new Vector3(flowDirection.x, 0, flowDirection.y);

            // 2. Add formation offset influence
            Vector3 formationTarget = currentDestination + formationOffset;
            Vector3 toFormationTarget = formationTarget - transform.position;
            toFormationTarget.y = 0;

            // Blend flow direction with direct path to formation position
            Vector3 blendedDirection = Vector3.Lerp(
                flowVelocity.normalized,
                toFormationTarget.normalized,
                formationWeight
            );

            // 3. Calculate base desired velocity
            desiredVelocity = blendedDirection * maxSpeed;

            // 4. Apply arrival slowdown (slow down near destination)
            float distanceToTarget = toFormationTarget.magnitude;
            if (distanceToTarget < arrivalSlowdownRadius)
            {
                float slowdownFactor = distanceToTarget / arrivalSlowdownRadius;
                desiredVelocity *= slowdownFactor;
            }

            // 5. Apply local avoidance
            if (enableLocalAvoidance && localAvoidance != null)
            {
                Vector3 avoidanceVelocity = localAvoidance.CalculateSeparationVelocity(this, avoidanceRadius);
                desiredVelocity += avoidanceVelocity * separationWeight * maxSpeed;
            }

            // 6. Clamp to max speed
            if (desiredVelocity.sqrMagnitude > maxSpeed * maxSpeed)
            {
                desiredVelocity = desiredVelocity.normalized * maxSpeed;
            }
        }

        /// <summary>
        /// Apply movement with acceleration and smoothing
        /// </summary>
        private void ApplyMovement()
        {
            // Smooth velocity change using critical damping
            if (enableMovementSmoothing)
            {
                currentVelocity = Vector3.SmoothDamp(
                    currentVelocity,
                    desiredVelocity,
                    ref velocitySmoothDampVelocity,
                    velocityDamping
                );
            }
            else
            {
                // Direct acceleration
                currentVelocity = Vector3.MoveTowards(
                    currentVelocity,
                    desiredVelocity,
                    acceleration * Time.fixedDeltaTime
                );
            }

            // Apply velocity to rigidbody
            rb.linearVelocity = new Vector3(currentVelocity.x, rb.linearVelocity.y, currentVelocity.z);

            // Rotate to face movement direction
            if (currentVelocity.sqrMagnitude > 0.01f)
            {
                RotateTowardsMovement();
            }
        }

        /// <summary>
        /// Gradually slow down to a stop
        /// </summary>
        private void SlowDown()
        {
            desiredVelocity = Vector3.zero;

            if (enableMovementSmoothing)
            {
                currentVelocity = Vector3.SmoothDamp(
                    currentVelocity,
                    Vector3.zero,
                    ref velocitySmoothDampVelocity,
                    velocityDamping
                );
            }
            else
            {
                currentVelocity = Vector3.MoveTowards(
                    currentVelocity,
                    Vector3.zero,
                    acceleration * Time.fixedDeltaTime
                );
            }

            rb.linearVelocity = new Vector3(currentVelocity.x, rb.linearVelocity.y, currentVelocity.z);
        }

        /// <summary>
        /// Smooth rotation towards movement direction
        /// </summary>
        private void RotateTowardsMovement()
        {
            Quaternion targetRotation = Quaternion.LookRotation(currentVelocity);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.fixedDeltaTime
            );
        }

        /// <summary>
        /// Check if unit has valid path to destination
        /// </summary>
        public bool HasPathToDestination()
        {
            if (!hasDestination)
                return false;

            return flowFieldManager.IsWalkable(currentDestination);
        }

        /// <summary>
        /// Get estimated distance to destination via flow field
        /// </summary>
        public float GetPathDistance()
        {
            if (!hasDestination)
                return float.MaxValue;

            return flowFieldManager.GetPathCost(transform.position);
        }

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos)
                return;

            // Draw avoidance radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, avoidanceRadius);

            // Draw velocity
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, currentVelocity);

            // Draw desired velocity
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, desiredVelocity);

            // Draw destination
            if (hasDestination)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, currentDestination);
                Gizmos.DrawWireSphere(currentDestination, stoppingDistance);

                // Draw formation offset target
                Gizmos.color = Color.cyan;
                Vector3 formationTarget = currentDestination + formationOffset;
                Gizmos.DrawWireSphere(formationTarget, 0.3f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw flow field direction at unit position
            if (Application.isPlaying && flowFieldManager != null)
            {
                Vector2 flow = flowFieldManager.SampleFlowDirection(transform.position);
                Vector3 flowDir = new Vector3(flow.x, 0, flow.y);

                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, flowDir * 2f);
            }
        }
    }
}
