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
        [SerializeField] private float stoppingDistance = 0.1f;
        [SerializeField] private float pathUpdateInterval = 0.5f;

        private NavMeshAgent agent;
        private Transform currentTarget;
        private Vector3 currentDestination;
        private float pathUpdateTimer;
        private bool hasDestination = false;
        private bool isEnabled = true;

        public bool IsMoving => agent != null && agent.velocity.sqrMagnitude > 0.01f;
        public bool HasReachedDestination => hasDestination && !agent.pathPending && agent.remainingDistance <= stoppingDistance;
        public float Speed => moveSpeed;
        public Vector3 Velocity => agent != null ? agent.velocity : Vector3.zero;

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
            }
            else
            {
                Debug.LogError($"NavMeshAgent not found on {gameObject.name}!");
            }
        }

        private void Update()
        {
            if (!isEnabled || agent == null) return;

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
        }

        #region Movement Control

        /// <summary>
        /// Move to a specific position.
        /// </summary>
        public void SetDestination(Vector3 destination)
        {
            if (!isEnabled)
            {
                Debug.LogWarning($"⚠️ UnitMovement: Cannot set destination for {gameObject.name} - movement is disabled!");
                return;
            }

            if (agent == null)
            {
                Debug.LogError($"❌ UnitMovement: Cannot set destination for {gameObject.name} - NavMeshAgent is null!");
                return;
            }

            currentDestination = destination;
            currentTarget = null;
            hasDestination = true;

            if (agent.enabled)
            {
                agent.SetDestination(destination);
                Debug.Log($"✅ UnitMovement: {gameObject.name} moving from {transform.position} to {destination} (distance: {Vector3.Distance(transform.position, destination):F2})");
            }
            else
            {
                Debug.LogWarning($"⚠️ UnitMovement: Cannot set destination for {gameObject.name} - NavMeshAgent is disabled!");
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
            pathUpdateTimer = pathUpdateInterval; // Update immediately

            if (agent.enabled)
            {
                agent.SetDestination(target.position);
            }
        }

        /// <summary>
        /// Stop all movement.
        /// </summary>
        public void Stop()
        {
            if (agent == null) return;

            if (agent.enabled)
            {
                agent.ResetPath();
            }
            
            currentTarget = null;
            hasDestination = false;
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
            
            if (agent != null)
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
        }
    }
}
