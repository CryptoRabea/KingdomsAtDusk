using RTS.Units;
using RTS.Units.AI;
using UnityEngine;

public class HealerAI : UnitAIController
{
    [Header("Healer Settings")]
    [SerializeField] private float healAmount = 10f;
    [SerializeField] private float healThreshold = 0.8f; // Only heal if ally below 80% health
    [SerializeField] private bool avoidCombat = true;
    [SerializeField] private float dangerAvoidanceRange = 5f;

    private float nextHealTime;
    private Collider[] cachedAllyHits = new Collider[16]; // Cached for ally detection
    private Collider[] cachedEnemyHits = new Collider[8]; // Cached for enemy avoidance

    public override Transform FindTarget()
    {
        // Healers look for injured allies using NonAlloc to prevent garbage
        if (Config == null || AISettings == null) return null;

        int allyCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            Config.detectionRange,
            cachedAllyHits,
            AISettings.allyLayer
        );

        if (allyCount == 0) return null;

        return FindMostInjuredAlly(cachedAllyHits, allyCount);
    }

    private Transform FindMostInjuredAlly(Collider[] allies, int count)
    {
        Transform mostInjured = null;
        float lowestHealthPercent = 1f;

        for (int i = 0; i < count; i++)
        {
            var ally = allies[i];
            if (ally == null || ally.gameObject == gameObject) continue;

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

        // Use NonAlloc to prevent garbage allocation (runs every frame!)
        int enemyCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            dangerAvoidanceRange,
            cachedEnemyHits,
            AISettings.enemyLayer
        );

        if (enemyCount > 0)
        {
            // Find safest direction (away from enemies)
            Vector3 avoidanceDirection = Vector3.zero;
            for (int i = 0; i < enemyCount; i++)
            {
                var enemy = cachedEnemyHits[i];
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

