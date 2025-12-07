using RTS.Units;
using RTS.Units.AI;
using UnityEngine;

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

            if (hit.TryGetComponent<UnitHealth>(out var health))
            {
            }
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

