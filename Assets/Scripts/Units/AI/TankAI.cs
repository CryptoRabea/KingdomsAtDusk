using UnityEngine;
using System.Collections.Generic;

namespace RTS.Units.AI
{
    /// <summary>
    /// Tank enemy AI - Slow, heavily armored unit that can taunt nearby enemies.
    /// Periodically forces nearby player units to target it instead of other enemies.
    /// </summary>
    public class TankAI : UnitAIController
    {
        [Header("Tank Settings")]
        [SerializeField] private float tauntRadius = 8f;
        [SerializeField] private float tauntCooldown = 10f;
        [SerializeField] private float tauntDuration = 3f;
        [SerializeField] private LayerMask playerUnitsLayer;

        private float tauntTimer = 0f;
        private HashSet<UnitAIController> tauntedUnits = new HashSet<UnitAIController>();
        private Collider[] tauntHits = new Collider[16]; // Cached array for Physics queries

        protected override void Update()
        {
            base.Update();
            UpdateTaunt();
        }

        /// <summary>
        /// Tanks never retreat - they're the front line!
        /// </summary>
        public override bool ShouldRetreat()
        {
            return false; // Tanks never retreat!
        }

        private void UpdateTaunt()
        {
            if (Health == null || Health.IsDead) return;

            tauntTimer += Time.deltaTime;

            if (tauntTimer >= tauntCooldown)
            {
                PerformTaunt();
                tauntTimer = 0f;
            }
        }

        private void PerformTaunt()
        {
            // Use NonAlloc to prevent garbage allocation
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, tauntRadius, tauntHits, playerUnitsLayer);

            tauntedUnits.Clear();
            for (int i = 0; i < hitCount; i++)
            {
                var hit = tauntHits[i];
                if (hit == null) continue;

                var enemyAI = hit.GetComponent<UnitAIController>();
                if (enemyAI != null && enemyAI.Health != null && !enemyAI.Health.IsDead)
                {
                    // Force them to target this tank
                    enemyAI.SetTarget(transform);
                    tauntedUnits.Add(enemyAI);
                }
            }

            if (tauntedUnits.Count > 0)
            {
            }

            // Release taunt after duration
            Invoke(nameof(ReleaseTaunt), tauntDuration);
        }

        private void ReleaseTaunt()
        {
            foreach (var unit in tauntedUnits)
            {
                if (unit != null && unit.gameObject != null)
                {
                    // Clear their forced target so they can find new targets naturally
                    unit.ClearTarget();
                }
            }
            tauntedUnits.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            // Draw taunt radius
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, tauntRadius);
        }

        private void OnDestroy()
        {
            // Critical: Cancel any pending Invoke callbacks to prevent memory leaks
            CancelInvoke();

            // Clean up taunted units
            ReleaseTaunt();
        }
    }
}
