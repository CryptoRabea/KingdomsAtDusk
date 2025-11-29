using UnityEngine;
using RTS.Core.Events;
using RTS.Core;
using RTS.Core.Services;

namespace RTS.Units.AI
{
    /// <summary>
    /// Boss enemy AI - Powerful unit with multiple phases and special abilities.
    /// Can summon minions, has area attacks, and changes behavior based on health phases.
    /// </summary>
    public class BossAI : UnitAIController
    {
        [Header("Boss Settings")]
        [SerializeField] private GameObject minionPrefab;
        [SerializeField] private int minionsPerSummon = 3;
        [SerializeField] private float summonCooldown = 15f;
        [SerializeField] private float summonRadius = 5f;

        [Header("Boss Phases")]
        [SerializeField] private float phase2HealthThreshold = 0.66f; // 66% health
        [SerializeField] private float phase3HealthThreshold = 0.33f; // 33% health
        [SerializeField] private float phase2DamageMultiplier = 1.25f;
        [SerializeField] private float phase3DamageMultiplier = 1.5f;
        [SerializeField] private float phase3AttackSpeedMultiplier = 1.5f;

        [Header("Area Attack")]
        [SerializeField] private float areaAttackRadius = 10f;
        [SerializeField] private float areaAttackDamage = 30f;
        [SerializeField] private float areaAttackCooldown = 20f;
        [SerializeField] private LayerMask playerUnitsLayer;

        private BossPhase currentPhase = BossPhase.Phase1;
        private float summonTimer = 0f;
        private float areaAttackTimer = 0f;
        private float originalDamage;
        private float originalAttackRate;
        private IPoolService poolService;

        private void Start()
        {
            poolService = ServiceLocator.Get<IPoolService>();

            if (Combat != null)
            {
                originalDamage = Combat.AttackDamage;
                originalAttackRate = Combat.AttackRate;
            }
        }

        protected override void Update()
        {
            base.Update();
            UpdatePhase();
            UpdateAbilities();
        }

        /// <summary>
        /// Bosses never retreat!
        /// </summary>
        public override bool ShouldRetreat()
        {
            return false;
        }

        private void UpdatePhase()
        {
            if (Health == null || Health.IsDead) return;

            float healthPercent = Health.HealthPercent;

            // Check for phase transitions
            if (currentPhase == BossPhase.Phase1 && healthPercent <= phase2HealthThreshold)
            {
                EnterPhase2();
            }
            else if (currentPhase == BossPhase.Phase2 && healthPercent <= phase3HealthThreshold)
            {
                EnterPhase3();
            }
        }

        private void EnterPhase2()
        {
            currentPhase = BossPhase.Phase2;

            // Increase damage
            if (Combat != null)
            {
                Combat.SetAttackDamage(originalDamage * phase2DamageMultiplier);
            }

            Debug.Log($"{gameObject.name} entered PHASE 2! Damage increased!");

            // Immediate summon on phase transition
            SummonMinions();
        }

        private void EnterPhase3()
        {
            currentPhase = BossPhase.Phase3;

            // Further increase damage and attack speed
            if (Combat != null)
            {
                Combat.SetAttackDamage(originalDamage * phase3DamageMultiplier);
                Combat.SetAttackRate(originalAttackRate * phase3AttackSpeedMultiplier);
            }

            Debug.Log($"{gameObject.name} entered PHASE 3! MAXIMUM POWER!");

            // Immediate summon and area attack on phase transition
            SummonMinions();
            PerformAreaAttack();
        }

        private void UpdateAbilities()
        {
            if (Health == null || Health.IsDead) return;

            // Update summon timer
            summonTimer += Time.deltaTime;
            if (summonTimer >= summonCooldown)
            {
                SummonMinions();
                summonTimer = 0f;
            }

            // Update area attack timer (only in phase 2 and 3)
            if (currentPhase >= BossPhase.Phase2)
            {
                areaAttackTimer += Time.deltaTime;
                if (areaAttackTimer >= areaAttackCooldown)
                {
                    PerformAreaAttack();
                    areaAttackTimer = 0f;
                }
            }
        }

        private void SummonMinions()
        {
            if (minionPrefab == null)
            {
                Debug.LogWarning($"{gameObject.name} has no minion prefab assigned!");
                return;
            }

            for (int i = 0; i < minionsPerSummon; i++)
            {
                // Random position around boss
                Vector2 randomCircle = Random.insideUnitCircle * summonRadius;
                Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

                // Try to use object pool if available
                GameObject minion;
                if (poolService != null)
                {
                    minion = poolService.Get(minionPrefab, spawnPosition, Quaternion.identity);
                }
                else
                {
                    minion = Instantiate(minionPrefab, spawnPosition, Quaternion.identity);
                }

                // Publish spawn event
                EventBus.Publish(new UnitSpawnedEvent(minion));
            }

            Debug.Log($"{gameObject.name} summoned {minionsPerSummon} minions!");
        }

        private void PerformAreaAttack()
        {
            // Find all player units in radius
            Collider[] hits = Physics.OverlapSphere(transform.position, areaAttackRadius, playerUnitsLayer);

            int hitCount = 0;
            foreach (var hit in hits)
            {
                var health = hit.GetComponent<UnitHealth>();
                if (health != null && !health.IsDead)
                {
                    health.TakeDamage(areaAttackDamage, gameObject);
                    hitCount++;
                }
            }

            if (hitCount > 0)
            {
                Debug.Log($"{gameObject.name} performed AREA ATTACK hitting {hitCount} units!");
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw summon radius
            Gizmos.color = new Color(1f, 0f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, summonRadius);

            // Draw area attack radius
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, areaAttackRadius);
        }

        private enum BossPhase
        {
            Phase1,
            Phase2,
            Phase3
        }
    }
}
