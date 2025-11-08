using UnityEngine;
using RTS.Units;

namespace RTS.Units.AI
{
    /// <summary>
    /// Soldier AI - Focuses on weak targets, aggressive melee combat.
    /// </summary>
    public class SoldierAI : UnitAIController
    {
        [Header("Soldier Settings")]
        [SerializeField] private float chargeSpeedMultiplier = 1.5f;
        [SerializeField] private bool preferWeakTargets = true;

        public override Transform FindTarget()
        {
            if (!preferWeakTargets)
                return base.FindTarget();

            // Soldiers prefer targeting weak enemies
            if (Config == null || AISettings == null) return null;

            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                Config.detectionRange,
                AISettings.enemyLayer
            );

            if (hits.Length == 0) return null;

            return FindWeakestEnemy(hits);
        }

        private Transform FindWeakestEnemy(Collider[] hits)
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

            return weakest;
        }

        // Add charge mechanic when moving to attack
        protected override void Update()
        {
            base.Update();

            // Increase speed when charging toward enemy
            if (CurrentStateType == UnitStateType.Moving && CurrentTarget != null)
            {
                float distance = Vector3.Distance(transform.position, CurrentTarget.position);
                if (distance < Config.detectionRange * 0.5f)
                {
                    Movement?.SetSpeed(Config.speed * chargeSpeedMultiplier);
                }
                else
                {
                    Movement?.SetSpeed(Config.speed);
                }
            }
        }
    }

    /// <summary>
    /// Archer AI - Ranged combat, maintains distance, targets far enemies.
    /// </summary>
    public class ArcherAI : UnitAIController
    {
        [Header("Archer Settings")]
        [SerializeField] private float preferredCombatRange = 8f;
        [SerializeField] private bool preferDistantTargets = true;
        [SerializeField] private float kiteDistance = 3f; // Distance to maintain from melee enemies

        public override Transform FindTarget()
        {
            if (!preferDistantTargets)
                return base.FindTarget();

            // Archers prefer targets at range
            if (Config == null || AISettings == null) return null;

            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                Config.detectionRange,
                AISettings.enemyLayer
            );

            if (hits.Length == 0) return null;

            return FindDistantEnemy(hits);
        }

        private Transform FindDistantEnemy(Collider[] hits)
        {
            Transform farthest = null;
            float maxDistance = 0f;

            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                var health = hit.GetComponent<UnitHealth>();
                if (health == null || health.IsDead) continue;

                float distance = Vector3.Distance(transform.position, hit.transform.position);
                
                // Prefer targets within preferred range, but farther than melee
                if (distance > kiteDistance && distance <= preferredCombatRange)
                {
                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                        farthest = hit.transform;
                    }
                }
            }

            // If no target in preferred range, just get nearest
            return farthest ?? base.FindTarget();
        }

        // Kiting behavior - maintain distance from enemies
        protected override void Update()
        {
            base.Update();

            if (CurrentStateType == UnitStateType.Attacking && CurrentTarget != null)
            {
                float distance = Vector3.Distance(transform.position, CurrentTarget.position);
                
                // If enemy gets too close, back up while shooting
                if (distance < kiteDistance)
                {
                    Vector3 retreatDirection = (transform.position - CurrentTarget.position).normalized;
                    Vector3 kitePosition = transform.position + retreatDirection * 2f;
                    Movement?.SetDestination(kitePosition);
                }
            }
        }
    }

    /// <summary>
    /// Healer AI - Focuses on healing injured allies, avoids combat.
    /// </summary>
    public class HealerAI : UnitAIController
    {
        [Header("Healer Settings")]
        [SerializeField] private float healAmount = 10f;
        [SerializeField] private float healThreshold = 0.8f; // Only heal if ally below 80% health
        [SerializeField] private bool avoidCombat = true;
        [SerializeField] private float dangerAvoidanceRange = 5f;

        private float nextHealTime;

        public override Transform FindTarget()
        {
            // Healers look for injured allies
            if (Config == null || AISettings == null) return null;

            Collider[] allies = Physics.OverlapSphere(
                transform.position,
                Config.detectionRange,
                AISettings.allyLayer
            );

            if (allies.Length == 0) return null;

            return FindMostInjuredAlly(allies);
        }

        private Transform FindMostInjuredAlly(Collider[] allies)
        {
            Transform mostInjured = null;
            float lowestHealthPercent = 1f;

            foreach (var ally in allies)
            {
                // Skip self
                if (ally.gameObject == gameObject) continue;

                var health = ally.GetComponent<UnitHealth>();
                if (health == null || health.IsDead) continue;

                float healthPercent = health.HealthPercent;
                
                // Only consider allies below heal threshold
                if (healthPercent < healThreshold && healthPercent < lowestHealthPercent)
                {
                    lowestHealthPercent = healthPercent;
                    mostInjured = ally.transform;
                }
            }

            return mostInjured;
        }

        // Override to use healing instead of attacking
        protected override void Update()
        {
            base.Update();

            // Avoid danger if enemies are nearby
            if (avoidCombat)
            {
                AvoidNearbyEnemies();
            }

            // Heal target instead of attacking
            if (CurrentStateType == UnitStateType.Attacking && CurrentTarget != null)
            {
                if (Time.time >= nextHealTime && Combat != null && Combat.IsTargetInRange(CurrentTarget))
                {
                    PerformHeal();
                    nextHealTime = Time.time + (1f / Combat.AttackRate);
                }
            }
        }

        private void PerformHeal()
        {
            var targetHealth = CurrentTarget.GetComponent<UnitHealth>();
            if (targetHealth != null && !targetHealth.IsDead)
            {
                // Heal using negative damage
                targetHealth.Heal(healAmount, gameObject);
            }
        }

        private void AvoidNearbyEnemies()
        {
            if (Config == null || AISettings == null) return;

            // Check for nearby enemies
            Collider[] enemies = Physics.OverlapSphere(
                transform.position,
                dangerAvoidanceRange,
                AISettings.enemyLayer
            );

            if (enemies.Length > 0)
            {
                // Find safest direction (away from enemies)
                Vector3 avoidanceDirection = Vector3.zero;
                foreach (var enemy in enemies)
                {
                    if (enemy != null)
                    {
                        avoidanceDirection += (transform.position - enemy.transform.position).normalized;
                    }
                }

                if (avoidanceDirection.sqrMagnitude > 0.1f)
                {
                    Vector3 safePosition = transform.position + avoidanceDirection.normalized * 5f;
                    Movement?.SetDestination(safePosition);
                }
            }
        }

        public override bool ShouldRetreat()
        {
            // Healers retreat earlier (at higher health threshold)
            if (Config == null || Health == null) return false;
            return Health.HealthPercent < 0.5f;
        }
    }
}
