using UnityEngine;
using RTS.Core.Events;
using RTS.Core.Services;

namespace RTS.Units
{
    /// <summary>
    /// Component handling unit health in a modular way.
    /// Uses events for decoupled communication.
    /// </summary>
    public class UnitHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private bool isInvulnerable = false;

        private float currentHealth;
        private bool isDead = false;
        private bool hpBarRegistered = false;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthPercent => currentHealth / maxHealth;
        public bool IsDead => isDead;
        public bool IsInvulnerable => isInvulnerable;

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        private void Start()
        {
            // Register HP bar with floating numbers service
            RegisterHPBar();
        }

        private void RegisterHPBar()
        {
            if (hpBarRegistered) return;

            var floatingNumberService = ServiceLocator.TryGet<IFloatingNumberService>();
            if (floatingNumberService != null)
            {
                floatingNumberService.RegisterHPBar(
                    gameObject,
                    () => currentHealth,
                    () => maxHealth
                );
                hpBarRegistered = true;
            }
        }

        #region Damage & Healing

        /// <summary>
        /// Apply damage to this unit.
        /// </summary>
        public void TakeDamage(float amount, GameObject attacker = null)
        {
            if (isDead || isInvulnerable || amount <= 0) return;

            float previousHealth = currentHealth;
            currentHealth -= amount;
            currentHealth = Mathf.Max(0, currentHealth);

            float actualDamage = previousHealth - currentHealth;

            // Publish health changed event
            EventBus.Publish(new UnitHealthChangedEvent(
                gameObject,
                currentHealth,
                maxHealth,
                -actualDamage
            ));

            // Publish damage dealt event
            if (attacker != null)
            {
                EventBus.Publish(new DamageDealtEvent(attacker, gameObject, actualDamage));
            }

            // Check for death
            if (currentHealth <= 0 && !isDead)
            {
                Die();
            }
        }

        /// <summary>
        /// Heal this unit.
        /// </summary>
        public void Heal(float amount, GameObject healer = null)
        {
            if (isDead || amount <= 0) return;

            float previousHealth = currentHealth;
            currentHealth += amount;
            currentHealth = Mathf.Min(maxHealth, currentHealth);

            float actualHealing = currentHealth - previousHealth;

            if (actualHealing > 0)
            {
                // Publish health changed event
                EventBus.Publish(new UnitHealthChangedEvent(
                    gameObject,
                    currentHealth,
                    maxHealth,
                    actualHealing
                ));

                // Publish healing event
                if (healer != null)
                {
                    EventBus.Publish(new HealingAppliedEvent(healer, gameObject, actualHealing));
                }

                // Stop blood dripping if healed above threshold
                var floatingNumberService = ServiceLocator.TryGet<IFloatingNumberService>();
                if (floatingNumberService != null && floatingNumberService.Settings != null)
                {
                    float healthPercent = currentHealth / maxHealth;
                    if (healthPercent > floatingNumberService.Settings.BloodDrippingThreshold)
                    {
                        floatingNumberService.StopBloodDripping(gameObject);
                    }
                }
            }
        }

        /// <summary>
        /// Set health to a specific value (for initialization or special effects).
        /// </summary>
        public void SetHealth(float value)
        {
            if (isDead) return;

            float previousHealth = currentHealth;
            currentHealth = Mathf.Clamp(value, 0, maxHealth);

            if (!Mathf.Approximately(previousHealth, currentHealth))
            {
                EventBus.Publish(new UnitHealthChangedEvent(
                    gameObject,
                    currentHealth,
                    maxHealth,
                    currentHealth - previousHealth
                ));
            }

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Instantly kill this unit.
        /// </summary>
        public void Kill()
        {
            if (isDead) return;
            currentHealth = 0;
            Die();
        }

        #endregion

        #region Death

        private void Die()
        {
            if (isDead) return;

            isDead = true;

            // Unregister HP bar
            UnregisterHPBar();

            // Determine if this was an enemy (check layer or tag)
            bool wasEnemy = gameObject.layer == LayerMask.NameToLayer("Enemy");

            EventBus.Publish(new UnitDiedEvent(gameObject, wasEnemy));

            // Notify other components
            SendMessage("OnUnitDied", SendMessageOptions.DontRequireReceiver);
        }

        private void UnregisterHPBar()
        {
            if (!hpBarRegistered) return;

            var floatingNumberService = ServiceLocator.TryGet<IFloatingNumberService>();
            if (floatingNumberService != null)
            {
                floatingNumberService.UnregisterHPBar(gameObject);
                hpBarRegistered = false;
            }
        }

        private void OnDestroy()
        {
            UnregisterHPBar();
        }

        #endregion

        #region Editor Methods

        public void SetMaxHealth(float value)
        {
            maxHealth = Mathf.Max(1f, value);
            currentHealth = Mathf.Min(currentHealth, maxHealth);
        }

        public void SetInvulnerable(bool invulnerable)
        {
            isInvulnerable = invulnerable;
        }

        #endregion

        #region Debug

        [ContextMenu("Take 20 Damage")]
        private void DebugTakeDamage()
        {
            TakeDamage(20f);
        }

        [ContextMenu("Heal 50")]
        private void DebugHeal()
        {
            Heal(50f);
        }

        [ContextMenu("Kill Unit")]
        private void DebugKill()
        {
            Kill();
        }

        #endregion
    }
}
