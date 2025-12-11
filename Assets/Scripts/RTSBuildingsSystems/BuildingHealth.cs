using UnityEngine;
using RTS.Core.Events;
using RTS.Core.Services;

namespace RTS.Buildings
{
    /// <summary>
    /// Health component for buildings - allows buildings to take damage and be destroyed.
    /// Essential for Stronghold and other destructible buildings.
    /// </summary>
    [RequireComponent(typeof(Building))]
    public class BuildingHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 1000f;
        [SerializeField] private float currentHealth;
        [SerializeField] private bool isInvulnerable = false;

        [Header("Visual Feedback")]
        [SerializeField] private GameObject destructionEffectPrefab;
        [SerializeField] private bool hideOnDestroy = true;

        private Building building;
        private bool isDead = false;
        private bool hpBarRegistered = false;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthPercent => maxHealth > 0 ? currentHealth / maxHealth : 0f;
        public bool IsDead => isDead;
        public bool IsInvulnerable => isInvulnerable;

        private void Awake()
        {
            building = GetComponent<Building>();
            currentHealth = maxHealth;
        }

        private void Start()
        {
            // Initialize health from building data if available
            if (building?.Data != null && building.Data.maxHealth > 0)
            {
                maxHealth = building.Data.maxHealth;
                currentHealth = maxHealth;
            }

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

        /// <summary>
        /// Apply damage to the building
        /// </summary>
        public void TakeDamage(float amount, GameObject attacker = null)
        {
            if (isDead || isInvulnerable || amount <= 0)
                return;

            currentHealth -= amount;
            currentHealth = Mathf.Max(0f, currentHealth);

            // Publish damage event
            EventBus.Publish(new BuildingDamagedEvent(
                gameObject,
                building?.Data?.buildingName ?? "Unknown Building",
                currentHealth,
                maxHealth,
                -amount
            ));


            // Check for death
            if (currentHealth <= 0f && !isDead)
            {
                Die();
            }
        }

        /// <summary>
        /// Heal/repair the building
        /// </summary>
        public void Heal(float amount)
        {
            if (isDead || amount <= 0)
                return;

            float oldHealth = currentHealth;
            currentHealth += amount;
            currentHealth = Mathf.Min(currentHealth, maxHealth);

            float actualHealing = currentHealth - oldHealth;

            if (actualHealing > 0)
            {
                // Publish healing event
                EventBus.Publish(new BuildingDamagedEvent(
                    gameObject,
                    building?.Data?.buildingName ?? "Unknown Building",
                    currentHealth,
                    maxHealth,
                    actualHealing
                ));

            }
        }

        /// <summary>
        /// Set max health and optionally heal to full
        /// </summary>
        public void SetMaxHealth(float newMaxHealth, bool healToFull = false)
        {
            maxHealth = newMaxHealth;

            if (healToFull)
            {
                currentHealth = maxHealth;
            }
            else
            {
                currentHealth = Mathf.Min(currentHealth, maxHealth);
            }
        }

        /// <summary>
        /// Instantly destroy the building
        /// </summary>
        public void Die()
        {
            if (isDead) return;

            isDead = true;

            // Unregister HP bar
            UnregisterHPBar();


            // Spawn destruction effect
            if (destructionEffectPrefab != null)
            {
                Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
            }

            // Publish destroyed event
            EventBus.Publish(new BuildingDestroyedEvent(
                gameObject,
                building?.Data?.buildingName ?? "Unknown Building"
            ));

            // Hide or destroy the building
            if (hideOnDestroy)
            {
                gameObject.SetActive(false);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Instantly kill the building (for debug/testing)
        /// </summary>
        public void Kill()
        {
            currentHealth = 0f;
            Die();
        }

        private void OnDestroy()
        {
            UnregisterHPBar();
        }
    }

    // New event for building damage
    public struct BuildingDamagedEvent
    {
        public GameObject Building { get; }
        public string BuildingName { get; }
        public float CurrentHealth { get; }
        public float MaxHealth { get; }
        public float Delta { get; }

        public BuildingDamagedEvent(GameObject building, string buildingName, float current, float max, float delta)
        {
            Building = building;
            BuildingName = buildingName;
            CurrentHealth = current;
            MaxHealth = max;
            Delta = delta;
        }
    }
}
