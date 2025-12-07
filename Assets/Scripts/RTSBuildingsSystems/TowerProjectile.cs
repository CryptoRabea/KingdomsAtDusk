using UnityEngine;
using System.Collections.Generic;
using RTS.Units;

namespace RTS.Buildings
{
    /// <summary>
    /// Tower projectile with different attack types.
    /// Handles Arrow (single target), Fire (DOT + AOE), and Catapult (AOE).
    /// </summary>
    public class TowerProjectile : MonoBehaviour
    {
        private Transform target;
        private float damage;
        private GameObject attacker;
        private float speed = 10f;
        private float lifetime = 5f;
        private TowerType towerType = TowerType.Arrow;

        // Area damage settings
        private bool hasAreaDamage = false;
        private float aoeRadius = 0f;

        // Fire tower DOT settings
        private float dotDamage = 0f;
        private float dotDuration = 0f;

        // Visual settings
        private bool useArcTrajectory = false;
        private float arcHeight = 3f;
        private Vector3 startPosition;
        private Vector3 targetPosition;
        private float journeyTime = 0f;
        private float journeyDuration = 1f;

        public void Initialize(
            Transform targetTransform,
            float dmg,
            GameObject attackerObject,
            float projectileSpeed,
            TowerType type,
            float aoe = 0f,
            float dot = 0f,
            float dotDur = 0f,
            bool useArc = false)
        {
            target = targetTransform;
            damage = dmg;
            attacker = attackerObject;
            speed = projectileSpeed;
            towerType = type;

            // Area damage
            hasAreaDamage = aoe > 0;
            aoeRadius = aoe;

            // Fire DOT
            dotDamage = dot;
            dotDuration = dotDur;

            // Arc trajectory (for catapult)
            useArcTrajectory = useArc;
            if (useArcTrajectory && target != null)
            {
                startPosition = transform.position;
                targetPosition = target.position;
                float distance = Vector3.Distance(startPosition, targetPosition);
                journeyDuration = distance / speed;
            }

            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            if (target == null)
            {
                // For AOE projectiles, still explode at last known position
                if (hasAreaDamage && targetPosition != Vector3.zero)
                {
                    ExplodeAtPosition(targetPosition);
                }
                else
                {
                    Destroy(gameObject);
                }
                return;
            }

            if (useArcTrajectory)
            {
                UpdateArcMovement();
            }
            else
            {
                UpdateStraightMovement();
            }
        }

        private void UpdateStraightMovement()
        {
            // Move straight toward target
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;
            transform.forward = direction;

            // Check if close enough to hit
            if (Vector3.Distance(transform.position, target.position) < 0.5f)
            {
                HitTarget();
            }
        }

        private void UpdateArcMovement()
        {
            journeyTime += Time.deltaTime;
            float progress = journeyTime / journeyDuration;

            if (progress >= 1f)
            {
                HitTarget();
                return;
            }

            // Calculate arc position
            Vector3 currentPos = Vector3.Lerp(startPosition, target.position, progress);
            currentPos.y += arcHeight * Mathf.Sin(progress * Mathf.PI);

            transform.position = currentPos;

            // Orient toward movement direction
            if (journeyTime > 0)
            {
                Vector3 lookDirection = (currentPos - transform.position).normalized;
                if (lookDirection != Vector3.zero)
                {
                    transform.forward = lookDirection;
                }
            }
        }

        private void HitTarget()
        {
            if (hasAreaDamage)
            {
                // Area damage (Fire & Catapult)
                DealAreaDamage(target.position);
            }
            else
            {
                // Single target damage (Arrow)
                DealSingleTargetDamage(target);
            }

            Destroy(gameObject);
        }

        private void DealSingleTargetDamage(Transform targetTransform)
        {
            if (targetTransform.TryGetComponent<UnitHealth>(out var health) && !health.IsDead)
            {
                health.TakeDamage(damage, attacker);

                // Apply DOT for fire tower
                if (towerType == TowerType.Fire && dotDamage > 0)
                {
                    ApplyBurnEffect(targetTransform.gameObject);
                }
            }
        }

        private void DealAreaDamage(Vector3 explosionCenter)
        {
            Collider[] hits = Physics.OverlapSphere(explosionCenter, aoeRadius);

            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<UnitHealth>(out var health) && !health.IsDead)
                {
                    // Calculate distance-based damage falloff (optional)
                    float distance = Vector3.Distance(explosionCenter, hit.transform.position);
                    float damageMultiplier = 1f - (distance / aoeRadius) * 0.5f; // 50% damage at edge
                    float finalDamage = damage * Mathf.Max(0.5f, damageMultiplier);

                    health.TakeDamage(finalDamage, attacker);

                    // Apply burn effect for fire tower
                    if (towerType == TowerType.Fire && dotDamage > 0)
                    {
                        ApplyBurnEffect(hit.gameObject);
                    }
                }
            }
        }

        private void ExplodeAtPosition(Vector3 position)
        {
            if (hasAreaDamage)
            {
                DealAreaDamage(position);
            }
            Destroy(gameObject);
        }

        private void ApplyBurnEffect(GameObject targetObject)
        {
            // Add or refresh burn effect component
            if (targetObject.TryGetComponent<BurnEffect>(out var burnEffect))
            {
            }
            if (burnEffect == null)
            {
                burnEffect = targetObject.AddComponent<BurnEffect>();
            }

            burnEffect.ApplyBurn(dotDamage, dotDuration, attacker);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.transform == target)
            {
                HitTarget();
            }
        }

        private void OnDrawGizmos()
        {
            if (hasAreaDamage && Application.isPlaying)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
                Gizmos.DrawWireSphere(transform.position, aoeRadius);
            }
        }
    }

    /// <summary>
    /// Burn effect component for fire tower projectiles.
    /// Deals damage over time to units.
    /// </summary>
    public class BurnEffect : MonoBehaviour
    {
        private float damagePerSecond;
        private float duration;
        private float remainingTime;
        private GameObject source;
        private UnitHealth targetHealth;

        public void ApplyBurn(float dps, float dur, GameObject src)
        {
            damagePerSecond = dps;
            duration = dur;
            remainingTime = dur; // Refresh duration if already burning
            source = src;

            if (targetHealth == null)
            {
                targetHealth = GetComponent<UnitHealth>();
            }
        }

        private void Update()
        {
            if (targetHealth == null || targetHealth.IsDead)
            {
                Destroy(this);
                return;
            }

            remainingTime -= Time.deltaTime;

            if (remainingTime <= 0)
            {
                Destroy(this);
                return;
            }

            // Apply damage over time
            float damageThisFrame = damagePerSecond * Time.deltaTime;
            targetHealth.TakeDamage(damageThisFrame, source);
        }
    }
}
