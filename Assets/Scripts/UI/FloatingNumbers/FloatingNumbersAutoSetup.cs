using UnityEngine;
using TMPro;
using RTS.Units.Components;
using RTS.Buildings.Components;

namespace RTS.UI.FloatingNumbers
{
    /// <summary>
    /// Automatically sets up floating numbers on objects with health components
    /// Attach this to any GameObject with UnitHealth or BuildingHealth
    /// </summary>
    [RequireComponent(typeof(Transform))]
    public class FloatingNumbersAutoSetup : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool showDamageNumbers = true;
        [SerializeField] private bool showHealNumbers = true;
        [SerializeField] private Vector3 spawnOffset = Vector3.up * 2f;

        [Header("Critical Hits")]
        [SerializeField] private bool enableCriticalHits = false;
        [SerializeField] private float criticalChance = 0.15f;
        [SerializeField] private float criticalMultiplier = 1.5f;

        private UnitHealth unitHealth;
        private BuildingHealth buildingHealth;
        private Transform cachedTransform;

        private void Awake()
        {
            cachedTransform = transform;
            unitHealth = GetComponent<UnitHealth>();
            buildingHealth = GetComponent<BuildingHealth>();

            if (unitHealth == null && buildingHealth == null)
            {
                Debug.LogWarning($"FloatingNumbersAutoSetup on {gameObject.name}: No UnitHealth or BuildingHealth found!");
            }
        }

        private void OnEnable()
        {
            if (unitHealth != null)
            {
                unitHealth.OnDamageDealt += HandleDamage;
                unitHealth.OnHealingApplied += HandleHealing;
            }

            if (buildingHealth != null)
            {
                buildingHealth.OnDamageDealt += HandleDamage;
                buildingHealth.OnHealingApplied += HandleHealing;
            }
        }

        private void OnDisable()
        {
            if (unitHealth != null)
            {
                unitHealth.OnDamageDealt -= HandleDamage;
                unitHealth.OnHealingApplied -= HandleHealing;
            }

            if (buildingHealth != null)
            {
                buildingHealth.OnDamageDealt -= HandleDamage;
                buildingHealth.OnHealingApplied -= HandleHealing;
            }
        }

        private void HandleDamage(GameObject attacker, GameObject target, float amount)
        {
            if (!showDamageNumbers || FloatingNumbersManager.Instance == null)
                return;

            Vector3 spawnPosition = cachedTransform.position + spawnOffset;

            // Check for critical hit
            bool isCritical = enableCriticalHits && Random.value <= criticalChance;

            if (isCritical)
            {
                float criticalDamage = amount * criticalMultiplier;
                FloatingNumbersManager.Instance.SpawnCritical(criticalDamage, spawnPosition);
            }
            else
            {
                FloatingNumbersManager.Instance.SpawnDamage(amount, spawnPosition);
            }
        }

        private void HandleHealing(GameObject healer, GameObject target, float amount)
        {
            if (!showHealNumbers || FloatingNumbersManager.Instance == null)
                return;

            Vector3 spawnPosition = cachedTransform.position + spawnOffset;
            FloatingNumbersManager.Instance.SpawnHeal(amount, spawnPosition);
        }

        #region Editor Utilities

        [ContextMenu("Auto Setup on All Units")]
        private void AutoSetupAllUnits()
        {
            #if UNITY_EDITOR
            UnitHealth[] allUnits = FindObjectsOfType<UnitHealth>();
            int added = 0;

            foreach (var unit in allUnits)
            {
                if (unit.GetComponent<FloatingNumbersAutoSetup>() == null)
                {
                    unit.gameObject.AddComponent<FloatingNumbersAutoSetup>();
                    added++;
                }
            }

            Debug.Log($"Added FloatingNumbersAutoSetup to {added} units");
            #endif
        }

        [ContextMenu("Auto Setup on All Buildings")]
        private void AutoSetupAllBuildings()
        {
            #if UNITY_EDITOR
            BuildingHealth[] allBuildings = FindObjectsOfType<BuildingHealth>();
            int added = 0;

            foreach (var building in allBuildings)
            {
                if (building.GetComponent<FloatingNumbersAutoSetup>() == null)
                {
                    building.gameObject.AddComponent<FloatingNumbersAutoSetup>();
                    added++;
                }
            }

            Debug.Log($"Added FloatingNumbersAutoSetup to {added} buildings");
            #endif
        }

        #endregion
    }
}
