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

