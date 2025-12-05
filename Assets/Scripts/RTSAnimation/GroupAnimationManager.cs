using UnityEngine;
using System.Collections.Generic;
using RTS.Core.Events;

namespace RTS.Units.Animation
{
    /// <summary>
    /// Manages synchronized group animations and behaviors.
    /// Automatically tracks units with UnitPersonalityController and coordinates group actions.
    /// Singleton pattern for global access.
    /// </summary>
    public class GroupAnimationManager : MonoBehaviour
    {
        public static GroupAnimationManager Instance { get; private set; }

        [Header("Group Behavior Settings")]
        [SerializeField] private bool enableGroupVictory = true;
        [SerializeField] private bool enableGroupScanning = true;
        [SerializeField] private bool autoRegisterUnits = true;

        [Header("Victory Settings")]
        [SerializeField] private float victoryRadius = 15f;
        [SerializeField] private float victoryDelay = 0.5f;

        [Header("Scanning Settings")]
        [SerializeField] private float scanInterval = 10f;
        [SerializeField] private float scanRadius = 20f;
        [SerializeField] private float scanChance = 0.3f;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        // Registered units
        private HashSet<UnitPersonalityController> registeredUnits = new HashSet<UnitPersonalityController>();
        private List<UnitPersonalityController> unitCache = new List<UnitPersonalityController>();

        // Timers
        private float scanTimer = 0f;

        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnEnable()
        {
            SubscribeToEvents();

            // Auto-register existing units
            if (autoRegisterUnits)
            {
                RegisterExistingUnits();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            if (enableGroupScanning)
            {
                UpdateGroupScanning();
            }
        }

        #region Initialization

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<WaveCompletedEvent>(OnWaveCompleted);
            EventBus.Subscribe<UnitSpawnedEvent>(OnUnitSpawned);
            EventBus.Subscribe<UnitDiedEvent>(OnUnitDied);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<WaveCompletedEvent>(OnWaveCompleted);
            EventBus.Unsubscribe<UnitSpawnedEvent>(OnUnitSpawned);
            EventBus.Unsubscribe<UnitDiedEvent>(OnUnitDied);
        }

        /// <summary>
        /// Find and register all existing units in the scene.
        /// </summary>
        private void RegisterExistingUnits()
        {
            var allPersonalityControllers = FindObjectsOfType<UnitPersonalityController>();

            foreach (var controller in allPersonalityControllers)
            {
                RegisterUnit(controller);
            }

            if (showDebugInfo)
            {
                Debug.Log($"[GroupAnimationManager] Auto-registered {registeredUnits.Count} units");
            }
        }

        #endregion

        #region Unit Registration

        /// <summary>
        /// Register a unit for group behaviors.
        /// </summary>
        public void RegisterUnit(UnitPersonalityController unit)
        {
            if (unit == null) return;

            if (registeredUnits.Add(unit))
            {
                unitCache.Add(unit);

                if (showDebugInfo)
                {
                    Debug.Log($"[GroupAnimationManager] Registered unit: {unit.gameObject.name}");
                }
            }
        }

        /// <summary>
        /// Unregister a unit from group behaviors.
        /// </summary>
        public void UnregisterUnit(UnitPersonalityController unit)
        {
            if (unit == null) return;

            if (registeredUnits.Remove(unit))
            {
                unitCache.Remove(unit);

                if (showDebugInfo)
                {
                    Debug.Log($"[GroupAnimationManager] Unregistered unit: {unit.gameObject.name}");
                }
            }
        }

        /// <summary>
        /// Clean up null references from the registry.
        /// </summary>
        private void CleanupNullUnits()
        {
            unitCache.RemoveAll(u => u == null || !u.enabled);
            registeredUnits.RemoveWhere(u => u == null || !u.enabled);
        }

        #endregion

        #region Group Victory

        /// <summary>
        /// Trigger a group victory celebration for nearby units.
        /// </summary>
        public void TriggerGroupVictory(Vector3 center)
        {
            if (!enableGroupVictory)
                return;

            CleanupNullUnits();

            int celebratingUnits = 0;

            foreach (var unit in unitCache)
            {
                if (unit == null || !unit.enabled)
                    continue;

                // Check if unit is within victory radius
                float distance = Vector3.Distance(unit.transform.position, center);
                if (distance <= victoryRadius)
                {
                    unit.OnGroupVictory();
                    celebratingUnits++;
                }
            }

            if (showDebugInfo)
            {
                Debug.Log($"[GroupAnimationManager] Group victory triggered for {celebratingUnits} units at {center}");
            }
        }

        /// <summary>
        /// Trigger group victory for all registered units.
        /// </summary>
        public void TriggerGlobalVictory()
        {
            if (!enableGroupVictory)
                return;

            CleanupNullUnits();

            foreach (var unit in unitCache)
            {
                if (unit != null && unit.enabled)
                {
                    unit.OnGroupVictory();
                }
            }

            if (showDebugInfo)
            {
                Debug.Log($"[GroupAnimationManager] Global victory triggered for {unitCache.Count} units");
            }
        }

        #endregion

        #region Group Scanning

        private void UpdateGroupScanning()
        {
            scanTimer += Time.deltaTime;

            if (scanTimer >= scanInterval)
            {
                scanTimer = 0f;
                TriggerRandomGroupScan();
            }
        }

        /// <summary>
        /// Trigger a random group of units to scan/look around.
        /// </summary>
        private void TriggerRandomGroupScan()
        {
            if (unitCache.Count == 0)
                return;

            CleanupNullUnits();

            // Pick a random unit as the scan center
            var randomUnit = unitCache[Random.Range(0, unitCache.Count)];
            if (randomUnit == null || !randomUnit.enabled)
                return;

            Vector3 scanCenter = randomUnit.transform.position;
            int scanningUnits = 0;

            foreach (var unit in unitCache)
            {
                if (unit == null || !unit.enabled)
                    continue;

                // Check if unit is within scan radius
                float distance = Vector3.Distance(unit.transform.position, scanCenter);
                if (distance <= scanRadius && Random.value < scanChance)
                {
                    unit.OnGroupScan();
                    scanningUnits++;
                }
            }

            if (showDebugInfo && scanningUnits > 0)
            {
                Debug.Log($"[GroupAnimationManager] Group scan triggered for {scanningUnits} units near {scanCenter}");
            }
        }

        /// <summary>
        /// Manually trigger group scanning around a position.
        /// </summary>
        public void TriggerGroupScan(Vector3 center, float radius = -1f)
        {
            if (!enableGroupScanning)
                return;

            if (radius < 0)
                radius = scanRadius;

            CleanupNullUnits();

            int scanningUnits = 0;

            foreach (var unit in unitCache)
            {
                if (unit == null || !unit.enabled)
                    continue;

                float distance = Vector3.Distance(unit.transform.position, center);
                if (distance <= radius)
                {
                    unit.OnGroupScan();
                    scanningUnits++;
                }
            }

            if (showDebugInfo)
            {
                Debug.Log($"[GroupAnimationManager] Manual group scan triggered for {scanningUnits} units at {center}");
            }
        }

        #endregion

        #region Event Handlers

        private void OnWaveCompleted(WaveCompletedEvent evt)
        {
            // Trigger global victory celebration when a wave completes
            if (enableGroupVictory)
            {
                TriggerGlobalVictory();
            }
        }

        private void OnUnitSpawned(UnitSpawnedEvent evt)
        {
            if (!autoRegisterUnits || evt.Unit == null)
                return;

            // Try to register the newly spawned unit
            var personalityController = evt.Unit.GetComponent<UnitPersonalityController>();
            if (personalityController != null)
            {
                RegisterUnit(personalityController);
            }
        }

        private void OnUnitDied(UnitDiedEvent evt)
        {
            if (evt.Unit == null)
                return;

            // Unregister dead units
            var personalityController = evt.Unit.GetComponent<UnitPersonalityController>();
            if (personalityController != null)
            {
                UnregisterUnit(personalityController);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get the number of registered units.
        /// </summary>
        public int GetRegisteredUnitCount()
        {
            CleanupNullUnits();
            return registeredUnits.Count;
        }

        /// <summary>
        /// Get all registered units (read-only).
        /// </summary>
        public IReadOnlyCollection<UnitPersonalityController> GetRegisteredUnits()
        {
            CleanupNullUnits();
            return registeredUnits;
        }

        /// <summary>
        /// Clear all registered units.
        /// </summary>
        public void ClearAllUnits()
        {
            registeredUnits.Clear();
            unitCache.Clear();

            if (showDebugInfo)
            {
                Debug.Log("[GroupAnimationManager] Cleared all registered units");
            }
        }

        #endregion

        #region Debug

#if UNITY_EDITOR
        [ContextMenu("Debug: Trigger Global Victory")]
        private void DebugTriggerGlobalVictory()
        {
            TriggerGlobalVictory();
        }

        [ContextMenu("Debug: Trigger Random Scan")]
        private void DebugTriggerRandomScan()
        {
            TriggerRandomGroupScan();
        }

        [ContextMenu("Debug: List Registered Units")]
        private void DebugListRegisteredUnits()
        {
            CleanupNullUnits();
            Debug.Log($"[GroupAnimationManager] {registeredUnits.Count} registered units:");
            foreach (var unit in unitCache)
            {
                if (unit != null)
                {
                    Debug.Log($"  - {unit.gameObject.name}");
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!showDebugInfo)
                return;

            // Draw victory radius
            Gizmos.color = Color.green;
            foreach (var unit in unitCache)
            {
                if (unit != null)
                {
                    Gizmos.DrawWireSphere(unit.transform.position, victoryRadius);
                }
            }

            // Draw scan radius
            Gizmos.color = Color.yellow;
            if (unitCache.Count > 0)
            {
                var firstUnit = unitCache[0];
                if (firstUnit != null)
                {
                    Gizmos.DrawWireSphere(firstUnit.transform.position, scanRadius);
                }
            }
        }
#endif

        #endregion

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
