using UnityEngine;
using RTS.Core.Events;

namespace RTS.Units.AI
{
    /// <summary>
    /// Berserker enemy AI - Fast, aggressive unit that gets stronger as it takes damage.
    /// Gains increased attack speed and damage when health drops below 50%.
    /// </summary>
    public class BerserkerAI : UnitAIController
    {
        [Header("Berserker Settings")]
        [SerializeField] private float enrageHealthThreshold = 0.5f; // 50%
        [SerializeField] private float enrageDamageMultiplier = 1.5f;
        [SerializeField] private float enrageAttackSpeedMultiplier = 1.5f;
        [SerializeField] private ParticleSystem enrageEffectPrefab;

        private bool isEnraged = false;
        private float originalDamage;
        private float originalAttackRate;
        private ParticleSystem enrageEffect;

        protected override void Update()
        {
            base.Update();
            CheckEnrageState();
        }

        /// <summary>
        /// Berserkers never retreat - they fight to the death!
        /// </summary>
        public override bool ShouldRetreat()
        {
            return false; // Berserkers never retreat!
        }

        private void CheckEnrageState()
        {
            if (Health == null || Health.IsDead) return;

            float healthPercent = Health.HealthPercent;

            // Enter enraged state
            if (!isEnraged && healthPercent <= enrageHealthThreshold)
            {
                EnterEnragedState();
            }
        }

        private void EnterEnragedState()
        {
            isEnraged = true;

            // Store original values if not already stored
            if (originalDamage == 0)
            {
                originalDamage = Combat.AttackDamage;
                originalAttackRate = Combat.AttackRate;
            }

            // Boost combat stats
            Combat.SetAttackDamage(originalDamage * enrageDamageMultiplier);
            Combat.SetAttackRate(originalAttackRate * enrageAttackSpeedMultiplier);

            // Visual effect
            if (enrageEffectPrefab != null)
            {
                enrageEffect = Instantiate(enrageEffectPrefab, transform.position, Quaternion.identity, transform);
            }

            Debug.Log($"{gameObject.name} has entered ENRAGED state! Damage: {Combat.AttackDamage}, Attack Rate: {Combat.AttackRate}");
        }

        private void OnDestroy()
        {
            if (enrageEffect != null)
            {
                Destroy(enrageEffect.gameObject);
            }
        }
    }
}
