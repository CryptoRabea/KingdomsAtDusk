using UnityEngine;

namespace RTS.Units.AI
{
    /// <summary>
    /// Enemy Archer AI - Ranged unit that maintains distance from enemies.
    /// Uses kiting tactics to stay at optimal range and avoid melee combat.
    /// </summary>
    public class EnemyArcherAI : UnitAIController
    {
        [Header("Archer Settings")]
        [SerializeField] private float preferredDistance = 10f; // Preferred attack distance
        [SerializeField] private float minSafeDistance = 5f; // Distance to start retreating
        [SerializeField] private float retreatSpeed = 5f; // Speed when kiting

        private float originalSpeed;

        private void Start()
        {
            if (Movement != null)
            {
                originalSpeed = Movement.Speed;
            }
        }

        protected override void Update()
        {
            base.Update();
            MaintainDistance();
        }

        /// <summary>
        /// Archers have custom retreat logic based on distance, not health.
        /// </summary>
        public override bool ShouldRetreat()
        {
            // Archers retreat if enemy is too close, regardless of health
            if (CurrentTarget != null)
            {
                float distance = Vector3.Distance(transform.position, CurrentTarget.position);
                return distance < minSafeDistance;
            }

            // Also retreat if health is low
            if (Config != null && Config.canRetreat && Health != null)
            {
                return Health.HealthPercent * 100f <= Config.retreatThreshold;
            }

            return false;
        }

        /// <summary>
        /// Override target finding to prefer distant targets.
        /// </summary>
        public override Transform FindTarget()
        {
            if (Config == null || AISettings == null) return null;

            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                Config.detectionRange,
                AISettings.enemyLayer
            );

            if (hits.Length == 0) return null;

            // Find the farthest target that's still in range
            return FindOptimalDistanceTarget(hits);
        }

        private Transform FindOptimalDistanceTarget(Collider[] hits)
        {
            Transform bestTarget = null;
            float bestScore = float.MinValue;

            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                var health = hit.GetComponent<UnitHealth>();
                if (health != null && health.IsDead) continue;

                float distance = Vector3.Distance(transform.position, hit.transform.position);

                // Prefer targets at preferred distance
                // Score is higher for targets closer to preferred distance
                float distanceDiff = Mathf.Abs(distance - preferredDistance);
                float score = 1f / (1f + distanceDiff);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = hit.transform;
                }
            }

            return bestTarget;
        }

        private void MaintainDistance()
        {
            if (CurrentTarget == null || Movement == null || Health == null || Health.IsDead)
                return;

            float distance = Vector3.Distance(transform.position, CurrentTarget.position);

            // If enemy is too close, kite backwards
            if (distance < minSafeDistance)
            {
                Vector3 directionAway = (transform.position - CurrentTarget.position).normalized;
                Vector3 retreatPosition = transform.position + directionAway * 5f;

                Movement.SetSpeed(retreatSpeed);
                Movement.SetDestination(retreatPosition);
            }
            // If enemy is too far, move closer
            else if (distance > preferredDistance * 1.5f)
            {
                Movement.SetSpeed(originalSpeed);
                // The attacking state will handle movement
            }
            else
            {
                // At good distance, use original speed
                Movement.SetSpeed(originalSpeed);
            }
        }
    }
}
