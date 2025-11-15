using UnityEngine;
using RTS.Units;

namespace RTS.Buildings
{
    /// <summary>
    /// Handles tower combat - targeting, firing, and projectile spawning.
    /// Automatically finds and attacks enemies within range.
    /// </summary>
    public class TowerCombat : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TowerDataSO towerData;
        [SerializeField] private Transform projectileSpawnPoint;
        [SerializeField] private Transform turretTransform; // Optional: rotates to face target

        [Header("Runtime Settings")]
        [SerializeField] private bool autoTarget = true;
        [SerializeField] private float targetUpdateInterval = 0.5f; // How often to search for new targets

        // State
        private Transform currentTarget;
        private float lastAttackTime = -999f;
        private float lastTargetUpdateTime = -999f;
        private bool isActive = false;

        // Properties
        public TowerDataSO TowerData => towerData;
        public Transform CurrentTarget => currentTarget;
        public bool IsActive => isActive;
        public float AttackRange => towerData != null ? towerData.attackRange : 0f;

        private void Start()
        {
            // Tower is active after construction completes
            var building = GetComponent<Building>();
            if (building != null)
            {
                // Wait for construction to complete
                if (building.IsConstructed)
                {
                    ActivateTower();
                }
                else
                {
                    // Check periodically for construction completion
                    InvokeRepeating(nameof(CheckConstructionComplete), 0.5f, 0.5f);
                }
            }
            else
            {
                // No building component, activate immediately
                ActivateTower();
            }

            // Setup spawn point if not assigned
            if (projectileSpawnPoint == null)
            {
                projectileSpawnPoint = transform;
            }
        }

        private void Update()
        {
            if (!isActive || towerData == null) return;

            // Update target periodically
            if (autoTarget && Time.time >= lastTargetUpdateTime + targetUpdateInterval)
            {
                FindNewTarget();
                lastTargetUpdateTime = Time.time;
            }

            // Check if current target is still valid
            if (currentTarget != null)
            {
                if (!IsValidTarget(currentTarget))
                {
                    currentTarget = null;
                    return;
                }

                // Rotate turret to face target
                RotateTurretToTarget();

                // Try to attack
                TryAttack();
            }
        }

        #region Activation

        private void CheckConstructionComplete()
        {
            var building = GetComponent<Building>();
            if (building != null && building.IsConstructed)
            {
                ActivateTower();
                CancelInvoke(nameof(CheckConstructionComplete));
            }
        }

        private void ActivateTower()
        {
            isActive = true;
            Debug.Log($"Tower activated: {towerData?.buildingName ?? "Unknown"}");
        }

        public void DeactivateTower()
        {
            isActive = false;
            currentTarget = null;
        }

        #endregion

        #region Targeting

        private void FindNewTarget()
        {
            if (towerData == null) return;

            // Find all enemies in range
            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                towerData.attackRange,
                towerData.targetLayers
            );

            Transform nearest = null;
            float minDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                // Skip if no health component or dead
                var health = hit.GetComponent<UnitHealth>();
                if (health == null || health.IsDead) continue;

                float distance = Vector3.Distance(transform.position, hit.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = hit.transform;
                }
            }

            currentTarget = nearest;
        }

        private bool IsValidTarget(Transform target)
        {
            if (target == null) return false;

            // Check if target is still alive
            var health = target.GetComponent<UnitHealth>();
            if (health == null || health.IsDead) return false;

            // Check if target is still in range
            float distance = Vector3.Distance(transform.position, target.position);
            if (distance > towerData.attackRange) return false;

            return true;
        }

        #endregion

        #region Attacking

        private void TryAttack()
        {
            if (!CanAttack()) return;

            PerformAttack();
        }

        private bool CanAttack()
        {
            if (!isActive) return false;
            if (towerData == null) return false;
            if (currentTarget == null) return false;
            if (Time.time < lastAttackTime + (1f / towerData.attackRate)) return false;

            return true;
        }

        private void PerformAttack()
        {
            lastAttackTime = Time.time;

            // Spawn projectile
            SpawnProjectile();

            // Optional: Trigger attack animation/effects
            SendMessage("OnTowerFired", SendMessageOptions.DontRequireReceiver);
        }

        private void SpawnProjectile()
        {
            if (towerData.projectilePrefab == null)
            {
                Debug.LogWarning($"Tower {towerData.buildingName} has no projectile prefab assigned!");
                return;
            }

            // Calculate spawn position
            Vector3 spawnPos = projectileSpawnPoint != null
                ? projectileSpawnPoint.position
                : transform.position + towerData.projectileSpawnOffset;

            // Instantiate projectile
            GameObject projectileObj = Instantiate(towerData.projectilePrefab, spawnPos, Quaternion.identity);

            // Orient toward target
            if (currentTarget != null)
            {
                Vector3 direction = (currentTarget.position - spawnPos).normalized;
                projectileObj.transform.forward = direction;
            }

            // Initialize projectile based on tower type
            var projectile = projectileObj.GetComponent<TowerProjectile>();
            if (projectile != null)
            {
                bool useArc = (towerData.towerType == TowerType.Catapult);

                projectile.Initialize(
                    currentTarget,
                    towerData.attackDamage,
                    gameObject,
                    towerData.projectileSpeed,
                    towerData.towerType,
                    towerData.hasAreaDamage ? towerData.aoeRadius : 0f,
                    towerData.dotDamage,
                    towerData.dotDuration,
                    useArc
                );
            }
            else
            {
                Debug.LogWarning($"Projectile prefab is missing TowerProjectile component!");
            }
        }

        #endregion

        #region Turret Rotation

        private void RotateTurretToTarget()
        {
            if (turretTransform == null || currentTarget == null) return;

            Vector3 direction = (currentTarget.position - turretTransform.position).normalized;
            direction.y = 0; // Keep rotation on horizontal plane

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                turretTransform.rotation = Quaternion.Slerp(
                    turretTransform.rotation,
                    targetRotation,
                    Time.deltaTime * 5f // Rotation speed
                );
            }
        }

        #endregion

        #region Public API

        public void SetTowerData(TowerDataSO data)
        {
            towerData = data;
        }

        public void SetTarget(Transform target)
        {
            currentTarget = target;
        }

        public void ClearTarget()
        {
            currentTarget = null;
        }

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            if (towerData == null) return;

            // Draw attack range
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, towerData.attackRange);

            // Draw line to current target
            if (currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, currentTarget.position);
            }

            // Draw projectile spawn point
            if (projectileSpawnPoint != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(projectileSpawnPoint.position, 0.3f);
            }
        }

        [ContextMenu("Find Target Now")]
        private void DebugFindTarget()
        {
            FindNewTarget();
            Debug.Log($"Found target: {currentTarget?.name ?? "None"}");
        }

        [ContextMenu("Fire Once")]
        private void DebugFire()
        {
            if (currentTarget != null)
            {
                PerformAttack();
            }
            else
            {
                Debug.Log("No target to fire at!");
            }
        }

        #endregion
    }
}
