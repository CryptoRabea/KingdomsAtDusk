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
        private Collider[] areaAttackHits = new Collider[32]; // Cached for area attack

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
                    if (minionPrefab.TryGetComponent<Transform>(out var minionComponent))
                    {
                        var pooledComponent = poolService.Get(minionComponent);
                        minion = pooledComponent.gameObject;
                        pooledComponent.position = spawnPosition;
                        pooledComponent.rotation = Quaternion.identity;
                    }
                    else
                    {
                        minion = Instantiate(minionPrefab, spawnPosition, Quaternion.identity);
                    }
                }
                else
                {
                    minion = Instantiate(minionPrefab, spawnPosition, Quaternion.identity);
                }

                // Publish spawn event
                EventBus.Publish(new UnitSpawnedEvent(minion, spawnPosition));
            }

        }

        private void PerformAreaAttack()
        {
            // Use NonAlloc to prevent garbage allocation
            int count = Physics.OverlapSphereNonAlloc(transform.position, areaAttackRadius, areaAttackHits, playerUnitsLayer);

            int hitCount = 0;
            for (int i = 0; i < count; i++)
            {
                var hit = areaAttackHits[i];
                if (hit == null) continue;

                if (hit.TryGetComponent<UnitHealth>(out var health) && !health.IsDead)
                {
                    health.TakeDamage(areaAttackDamage, gameObject);
                    hitCount++;
                }
            }

            if (hitCount > 0)
            {
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
