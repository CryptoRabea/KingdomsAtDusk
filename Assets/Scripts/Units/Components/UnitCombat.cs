using UnityEngine;

namespace RTS.Units
{
    /// <summary>
    /// Component handling unit combat mechanics (attacking).
    /// Works with UnitHealth for damage application.
    /// </summary>
    public class UnitCombat : MonoBehaviour
    {
        [Header("Combat Settings")]
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackRate = 1f; // attacks per second
        [SerializeField] private LayerMask targetLayers;

        [Header("Visual Settings")]
        [SerializeField] private Transform projectileSpawnPoint;
        [SerializeField] private GameObject projectilePrefab;

        private float lastAttackTime = -999f;
        private Transform currentTarget;
        private bool canAttack = true;

        public float AttackRange => attackRange;
        public float AttackDamage => attackDamage;
        public float AttackRate => attackRate;
        public Transform CurrentTarget => currentTarget;
        public bool IsInAttackRange => currentTarget != null && IsTargetInRange(currentTarget);
        public bool CanAttackNow => canAttack && Time.time >= lastAttackTime + (1f / attackRate);

        #region Target Management

        /// <summary>
        /// Set the current attack target.
        /// </summary>
        public void SetTarget(Transform target)
        {
            currentTarget = target;
        }

        /// <summary>
        /// Clear the current target.
        /// </summary>
        public void ClearTarget()
        {
            currentTarget = null;
        }

        /// <summary>
        /// Check if a target is within attack range.
        /// </summary>
        public bool IsTargetInRange(Transform target)
        {
            if (target == null) return false;
            return Vector3.Distance(transform.position, target.position) <= attackRange;
        }

        #endregion

        #region Attacking

        /// <summary>
        /// Attempt to attack the current target.
        /// Returns true if attack was executed.
        /// </summary>
        public bool TryAttack()
        {
            if (!CanAttack()) return false;

            PerformAttack();
            return true;
        }

        /// <summary>
        /// Check if unit can attack right now.
        /// </summary>
        public bool CanAttack()
        {
            if (!canAttack) return false;
            if (currentTarget == null) return false;
            if (!IsTargetInRange(currentTarget)) return false;
            if (Time.time < lastAttackTime + (1f / attackRate)) return false;

            // Check if target is still valid and alive
            var targetHealth = currentTarget.GetComponent<UnitHealth>();
            if (targetHealth != null && targetHealth.IsDead) return false;

            return true;
        }

        private void PerformAttack()
        {
            lastAttackTime = Time.time;

            // Apply damage to target
            var targetHealth = currentTarget.GetComponent<UnitHealth>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(attackDamage, gameObject);
            }

            // Spawn projectile if configured
            if (projectilePrefab != null && projectileSpawnPoint != null)
            {
                SpawnProjectile();
            }

            // Animation trigger (if you have an animator)
            SendMessage("OnAttackPerformed", SendMessageOptions.DontRequireReceiver);
        }

        private void SpawnProjectile()
        {
            Vector3 spawnPos = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;
            GameObject projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

            // Configure projectile to move toward target
            var projectileScript = projectile.GetComponent<Projectile>();
            if (projectileScript != null)
            {
                projectileScript.Initialize(currentTarget, attackDamage, gameObject);
            }
        }

        #endregion

        #region Target Finding

        /// <summary>
        /// Find nearest enemy within detection range.
        /// </summary>
        public Transform FindNearestEnemy(float detectionRange)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange, targetLayers);
            
            Transform nearest = null;
            float minDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                // Skip self
                if (hit.gameObject == gameObject) continue;

                // Skip dead targets
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

        /// <summary>
        /// Find all enemies within range.
        /// </summary>
        public Collider[] FindEnemiesInRange(float range)
        {
            return Physics.OverlapSphere(transform.position, range, targetLayers);
        }

        #endregion

        #region Configuration

        public void SetAttackDamage(float damage)
        {
            attackDamage = Mathf.Max(0, damage);
        }

        public void SetAttackRange(float range)
        {
            attackRange = Mathf.Max(0, range);
        }

        public void SetAttackRate(float rate)
        {
            attackRate = Mathf.Max(0.1f, rate);
        }

        public void SetCanAttack(bool enabled)
        {
            canAttack = enabled;
        }

        #endregion

        #region Callbacks

        private void OnDestroy()
        {
            // Critical: Clear target references to prevent memory leaks
            ClearTarget();
            canAttack = false;
        }

      /*  private void OnUnitDied()
        {
            // Disable combat when unit dies
            canAttack = false;
            currentTarget = null;
        }*/

        #endregion

        private void OnDrawGizmosSelected()
        {
            // Draw attack range
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // Draw line to current target
            if (currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, currentTarget.position);
            }
        }
    }

    /// <summary>
    /// Simple projectile script (optional, for ranged units).
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        private Transform target;
        private float damage;
        private GameObject attacker;
        private float speed = 10f;
        private float lifetime = 5f;

        public void Initialize(Transform targetTransform, float dmg, GameObject attackerObject)
        {
            target = targetTransform;
            damage = dmg;
            attacker = attackerObject;
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }

            // Move toward target
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;
            transform.forward = direction;

            // Check if close enough to hit
            if (Vector3.Distance(transform.position, target.position) < 0.5f)
            {
                HitTarget();
            }
        }

        private void HitTarget()
        {
            var health = target.GetComponent<UnitHealth>();
            if (health != null)
            {
                health.TakeDamage(damage, attacker);
            }

            Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.transform == target)
            {
                HitTarget();
            }
        }
    }
}
