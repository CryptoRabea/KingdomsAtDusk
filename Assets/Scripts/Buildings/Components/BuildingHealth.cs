using UnityEngine;
using RTS.Core.Events;
using System;

namespace RTS.Buildings.Components
{
    /// <summary>
    /// Health component for buildings - handles damage, healing, and destruction
    /// Similar to UnitHealth but tailored for buildings
    /// </summary>
    public class BuildingHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 500f;
        [SerializeField] private float currentHealth;
        [SerializeField] private bool isInvulnerable = false;

        [Header("Destruction Settings")]
        [SerializeField] private GameObject destructionEffectPrefab;
        [SerializeField] private float destructionDelay = 0.5f;

        [Header("Visual Feedback")]
        [SerializeField] private Material damagedMaterial;
        [SerializeField] private float damageThreshold = 0.5f; // Switch material when health below 50%

        private MeshRenderer meshRenderer;
        private Material originalMaterial;
        private bool isDead = false;

        // Events
        public event Action<float, float> OnHealthChanged;
        public event Action<GameObject, GameObject, float> OnDamageDealt;
        public event Action<GameObject, GameObject, float> OnHealingApplied;
        public event Action OnBuildingDestroyed;

        // Properties
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => isDead;
        public bool IsInvulnerable => isInvulnerable;
        public float HealthPercentage => maxHealth > 0 ? currentHealth / maxHealth : 0f;

        private void Awake()
        {
            currentHealth = maxHealth;
            meshRenderer = GetComponentInChildren<MeshRenderer>();

            if (meshRenderer != null && meshRenderer.material != null)
            {
                originalMaterial = meshRenderer.material;
            }
        }

        public void TakeDamage(float amount, GameObject attacker = null)
        {
            if (isDead || isInvulnerable || amount <= 0)
                return;

            float previousHealth = currentHealth;
            currentHealth = Mathf.Max(0, currentHealth - amount);

            // Update visuals if damaged enough
            UpdateDamageVisuals();

            // Publish events
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnDamageDealt?.Invoke(attacker, gameObject, amount);

            EventBus.Publish(new BuildingHealthChangedEvent(gameObject, currentHealth, maxHealth));
            EventBus.Publish(new BuildingDamageDealtEvent(attacker, gameObject, amount));

            Debug.Log($"{gameObject.name} took {amount} damage. Health: {currentHealth}/{maxHealth}");

            // Check for death
            if (currentHealth <= 0 && !isDead)
            {
                Die();
            }
        }

        public void Heal(float amount, GameObject healer = null)
        {
            if (isDead || amount <= 0)
                return;

            float previousHealth = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

            // Update visuals if healed above threshold
            UpdateDamageVisuals();

            // Publish events
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnHealingApplied?.Invoke(healer, gameObject, amount);

            EventBus.Publish(new BuildingHealthChangedEvent(gameObject, currentHealth, maxHealth));
            EventBus.Publish(new BuildingHealingAppliedEvent(healer, gameObject, amount));

            Debug.Log($"{gameObject.name} healed {amount}. Health: {currentHealth}/{maxHealth}");
        }

        public void SetHealth(float health)
        {
            currentHealth = Mathf.Clamp(health, 0, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            UpdateDamageVisuals();

            if (currentHealth <= 0 && !isDead)
            {
                Die();
            }
        }

        public void SetMaxHealth(float newMaxHealth, bool healToFull = false)
        {
            maxHealth = Mathf.Max(1, newMaxHealth);

            if (healToFull)
            {
                currentHealth = maxHealth;
            }
            else
            {
                currentHealth = Mathf.Min(currentHealth, maxHealth);
            }

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void UpdateDamageVisuals()
        {
            if (meshRenderer == null || originalMaterial == null)
                return;

            // Switch to damaged material if health is low
            if (HealthPercentage <= damageThreshold && damagedMaterial != null)
            {
                meshRenderer.material = damagedMaterial;
            }
            else
            {
                meshRenderer.material = originalMaterial;
            }
        }

        private void Die()
        {
            if (isDead) return;

            isDead = true;

            // Publish death event
            OnBuildingDestroyed?.Invoke();
            EventBus.Publish(new BuildingDestroyedEvent(gameObject, gameObject.name));

            Debug.Log($"💀 {gameObject.name} has been destroyed!");

            // Spawn destruction effect
            if (destructionEffectPrefab != null)
            {
                Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
            }

            // Destroy the building after delay
            Destroy(gameObject, destructionDelay);
        }

        #region Public API

        public void Kill()
        {
            SetHealth(0);
        }

        public void SetInvulnerable(bool invulnerable)
        {
            isInvulnerable = invulnerable;
        }

        #endregion

        #region Debug

        [ContextMenu("Take 100 Damage")]
        private void DebugTakeDamage()
        {
            TakeDamage(100f);
        }

        [ContextMenu("Heal 50")]
        private void DebugHeal()
        {
            Heal(50f);
        }

        [ContextMenu("Kill Building")]
        private void DebugKill()
        {
            Kill();
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            if (isDead) return;

            // Draw health bar in scene view
            Vector3 position = transform.position + Vector3.up * 3f;
            float barWidth = 2f;
            float healthWidth = barWidth * HealthPercentage;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(position - Vector3.right * barWidth / 2f, position + Vector3.right * barWidth / 2f);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(position - Vector3.right * barWidth / 2f, position - Vector3.right * barWidth / 2f + Vector3.right * healthWidth);
        }
    }
}
