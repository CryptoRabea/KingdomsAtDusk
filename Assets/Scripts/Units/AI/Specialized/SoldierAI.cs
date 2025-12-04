using UnityEngine;

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
}