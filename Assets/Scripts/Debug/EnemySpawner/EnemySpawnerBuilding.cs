using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using RTS.Core.Events;
using RTS.Core.Services;
using RTS.Units;

namespace RTS.DebugTools.EnemySpawner
{
    /// <summary>
    /// Debug building for spawning enemy units during gameplay testing.
    ///
    /// Features:
    /// - Selectable building with UI controls
    /// - Toggle spawning on/off
    /// - Configure quantity, intervals, incremental spawns
    /// - Support for multiple enemy types
    /// - Easy to add/remove from project (standalone in Debug folder)
    ///
    /// USAGE:
    /// 1. Create an empty GameObject and add this component
    /// 2. Assign an EnemySpawnerConfigSO with enemy prefabs
    /// 3. Optionally add a visual mesh as a child
    /// 4. Select the building in-game to access spawn controls
    ///
    /// TO REMOVE: Delete the entire Assets/Scripts/Debug folder
    /// </summary>
    public class EnemySpawnerBuilding : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private EnemySpawnerConfigSO config;

        [Header("Spawn Point")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform rallyPoint;

        [Header("Current Spawn Settings")]
        [SerializeField] private int selectedEnemyIndex = 0;
        [SerializeField] private int spawnQuantity = 5;
        [SerializeField] private float spawnInterval = 1f;
        [SerializeField] private float initialDelay = 0f;

        [Header("Incremental Spawning")]
        [SerializeField] private bool incrementalEnabled = false;
        [SerializeField] private int incrementalAmount = 1;
        [SerializeField] private float incrementalInterval = 30f;

        [Header("Continuous Spawning")]
        [SerializeField] private bool isSpawningActive = false;
        [SerializeField] private bool loopSpawning = false;

        [Header("Selection Visual")]
        [SerializeField] private GameObject selectionIndicator;
        [SerializeField] private Color selectedColor = Color.magenta;
        [SerializeField] private Color normalColor = Color.gray;

        [Header("Debug Display")]
        [SerializeField] private bool showDebugGizmos = true;

        // Runtime state
        private bool isSelected = false;
        private Coroutine activeSpawnCoroutine;
        private int currentWaveNumber = 0;
        private int totalSpawnedThisSession = 0;
        private List<GameObject> spawnedEnemies = new List<GameObject>();
        private Renderer[] renderers;
        private MaterialPropertyBlock propertyBlock;
        private IPoolService poolService;

        // Properties for UI access
        public bool IsSelected => isSelected;
        public bool IsSpawningActive => isSpawningActive;
        public int SelectedEnemyIndex { get => selectedEnemyIndex; set => selectedEnemyIndex = Mathf.Clamp(value, 0, config != null ? config.spawnableEnemies.Count - 1 : 0); }
        public int SpawnQuantity { get => spawnQuantity; set => spawnQuantity = Mathf.Max(1, value); }
        public float SpawnInterval { get => spawnInterval; set => spawnInterval = Mathf.Max(0.1f, value); }
        public float InitialDelay { get => initialDelay; set => initialDelay = Mathf.Max(0f, value); }
        public bool IncrementalEnabled { get => incrementalEnabled; set => incrementalEnabled = value; }
        public int IncrementalAmount { get => incrementalAmount; set => incrementalAmount = Mathf.Max(0, value); }
        public float IncrementalInterval { get => incrementalInterval; set => incrementalInterval = Mathf.Max(1f, value); }
        public bool LoopSpawning { get => loopSpawning; set => loopSpawning = value; }
        public int CurrentWaveNumber => currentWaveNumber;
        public int TotalSpawnedThisSession => totalSpawnedThisSession;
        public int ActiveEnemyCount => CountActiveEnemies();
        public EnemySpawnerConfigSO Config => config;

        private void Awake()
        {
            // Setup spawn point if not assigned
            if (spawnPoint == null)
            {
                GameObject spawnObj = new GameObject("SpawnPoint");
                spawnObj.transform.SetParent(transform);
                spawnObj.transform.localPosition = Vector3.forward * 3f;
                spawnPoint = spawnObj.transform;
            }

            // Cache renderers for selection highlighting
            renderers = GetComponentsInChildren<Renderer>();
            propertyBlock = new MaterialPropertyBlock();

            // Apply default settings from config
            ApplyDefaultSettings();
        }

        private void Start()
        {
            poolService = ServiceLocator.TryGet<IPoolService>();
        }

        private void ApplyDefaultSettings()
        {
            if (config != null)
            {
                spawnQuantity = config.defaultQuantity;
                spawnInterval = config.defaultSpawnInterval;
                initialDelay = config.defaultInitialDelay;
                incrementalEnabled = config.defaultIncrementalEnabled;
                incrementalAmount = config.defaultIncrementalAmount;
                incrementalInterval = config.defaultIncrementalInterval;
            }
        }

        #region Selection

        /// <summary>
        /// Select this spawner building.
        /// </summary>
        public void Select()
        {
            if (isSelected) return;
            isSelected = true;

            // Visual feedback
            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(true);
            }

            ApplyHighlightColor(selectedColor);

            // Publish selection event for UI
            EventBus.Publish(new EnemySpawnerSelectedEvent(this));
        }

        /// <summary>
        /// Deselect this spawner building.
        /// </summary>
        public void Deselect()
        {
            if (!isSelected) return;
            isSelected = false;

            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(false);
            }

            ApplyHighlightColor(normalColor);

            EventBus.Publish(new EnemySpawnerDeselectedEvent(this));
        }

        private void ApplyHighlightColor(Color color)
        {
            if (renderers == null || propertyBlock == null) return;

            foreach (var rend in renderers)
            {
                if (rend != null)
                {
                    rend.GetPropertyBlock(propertyBlock);
                    propertyBlock.SetColor("_Color", color);
                    propertyBlock.SetColor("_BaseColor", color);
                    rend.SetPropertyBlock(propertyBlock);
                }
            }
        }

        private void OnMouseDown()
        {
            Select();
        }

        #endregion

        #region Spawning Controls

        /// <summary>
        /// Start spawning enemies with current settings.
        /// </summary>
        public void StartSpawning()
        {
            if (isSpawningActive) return;
            if (config == null || config.spawnableEnemies.Count == 0)
            {
                UnityEngine.Debug.LogWarning("[EnemySpawner] No enemies configured!");
                return;
            }

            isSpawningActive = true;
            currentWaveNumber = 0;
            activeSpawnCoroutine = StartCoroutine(SpawnRoutine());
        }

        /// <summary>
        /// Stop all spawning.
        /// </summary>
        public void StopSpawning()
        {
            isSpawningActive = false;
            if (activeSpawnCoroutine != null)
            {
                StopCoroutine(activeSpawnCoroutine);
                activeSpawnCoroutine = null;
            }
        }

        /// <summary>
        /// Toggle spawning on/off.
        /// </summary>
        public void ToggleSpawning()
        {
            if (isSpawningActive)
            {
                StopSpawning();
            }
            else
            {
                StartSpawning();
            }
        }

        /// <summary>
        /// Spawn a single enemy immediately.
        /// </summary>
        public void SpawnSingleEnemy()
        {
            SpawnEnemy(config.GetEnemy(selectedEnemyIndex));
        }

        /// <summary>
        /// Spawn a specific quantity immediately.
        /// </summary>
        public void SpawnBatch(int quantity)
        {
            StartCoroutine(SpawnBatchCoroutine(quantity));
        }

        /// <summary>
        /// Kill all spawned enemies.
        /// </summary>
        public void KillAllSpawnedEnemies()
        {
            CleanupDeadReferences();

            foreach (var enemy in spawnedEnemies)
            {
                if (enemy != null)
                {
                    // Try to use health component for proper death
                    if (enemy.TryGetComponent<UnitHealth>(out var health))
                    {
                        health.TakeDamage(999999f, null);
                    }
                    else
                    {
                        Destroy(enemy);
                    }
                }
            }

            spawnedEnemies.Clear();
        }

        /// <summary>
        /// Destroy all spawned enemies immediately without death effects.
        /// </summary>
        public void DestroyAllSpawnedEnemies()
        {
            foreach (var enemy in spawnedEnemies)
            {
                if (enemy != null)
                {
                    if (poolService != null)
                    {
                        poolService.Return(enemy.transform);
                    }
                    else
                    {
                        Destroy(enemy);
                    }
                }
            }

            spawnedEnemies.Clear();
        }

        /// <summary>
        /// Reset session statistics.
        /// </summary>
        public void ResetStats()
        {
            totalSpawnedThisSession = 0;
            currentWaveNumber = 0;
        }

        #endregion

        #region Spawn Logic

        private IEnumerator SpawnRoutine()
        {
            // Initial delay
            if (initialDelay > 0)
            {
                yield return new WaitForSeconds(initialDelay);
            }

            do
            {
                currentWaveNumber++;
                int quantityThisWave = CalculateWaveQuantity();

                // Spawn this wave
                yield return StartCoroutine(SpawnWaveCoroutine(quantityThisWave));

                // If incremental, wait for next wave
                if (incrementalEnabled && loopSpawning)
                {
                    yield return new WaitForSeconds(incrementalInterval);
                }
                else if (loopSpawning && !incrementalEnabled)
                {
                    // Just loop the same wave after a short delay
                    yield return new WaitForSeconds(2f);
                }

            } while (loopSpawning && isSpawningActive);

            isSpawningActive = false;
            activeSpawnCoroutine = null;
        }

        private int CalculateWaveQuantity()
        {
            if (!incrementalEnabled)
            {
                return spawnQuantity;
            }

            // Incremental: base + (waveNumber - 1) * incrementAmount
            return spawnQuantity + (currentWaveNumber - 1) * incrementalAmount;
        }

        private IEnumerator SpawnWaveCoroutine(int quantity)
        {
            var enemyEntry = config.GetEnemy(selectedEnemyIndex);
            if (enemyEntry == null || enemyEntry.prefab == null)
            {
                UnityEngine.Debug.LogWarning("[EnemySpawner] Selected enemy has no prefab!");
                yield break;
            }

            for (int i = 0; i < quantity && isSpawningActive; i++)
            {
                SpawnEnemy(enemyEntry);
                yield return new WaitForSeconds(spawnInterval);
            }
        }

        private IEnumerator SpawnBatchCoroutine(int quantity)
        {
            var enemyEntry = config.GetEnemy(selectedEnemyIndex);
            if (enemyEntry == null || enemyEntry.prefab == null) yield break;

            for (int i = 0; i < quantity; i++)
            {
                SpawnEnemy(enemyEntry);
                yield return new WaitForSeconds(spawnInterval);
            }
        }

        private void SpawnEnemy(SpawnableEnemyEntry enemyEntry)
        {
            if (enemyEntry == null || enemyEntry.prefab == null) return;

            // Calculate spawn position
            Vector3 spawnPosition = GetSpawnPosition();

            // Spawn using pool if available
            GameObject enemy;
            if (poolService != null)
            {
                Transform spawnedTransform = poolService.Get(enemyEntry.prefab.transform);
                if (spawnedTransform == null)
                {
                    enemy = Instantiate(enemyEntry.prefab, spawnPosition, Quaternion.identity);
                }
                else
                {
                    enemy = spawnedTransform.gameObject;
                    enemy.transform.position = spawnPosition;
                    enemy.transform.rotation = Quaternion.identity;
                }
            }
            else
            {
                enemy = Instantiate(enemyEntry.prefab, spawnPosition, Quaternion.identity);
            }

            // Apply difficulty scaling
            ApplyDifficultyScaling(enemy, enemyEntry);

            // Move to rally point if set
            if (rallyPoint != null)
            {
                StartCoroutine(MoveToRallyPoint(enemy, rallyPoint.position));
            }

            // Track spawned enemy
            spawnedEnemies.Add(enemy);
            totalSpawnedThisSession++;

            // Publish spawn event
            EventBus.Publish(new UnitSpawnedEvent(enemy, spawnPosition));
        }

        private Vector3 GetSpawnPosition()
        {
            Vector3 basePosition = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.forward * 3f;

            if (config != null && config.randomizeSpawnPosition && config.spawnRadius > 0)
            {
                Vector2 randomOffset = Random.insideUnitCircle * config.spawnRadius;
                basePosition += new Vector3(randomOffset.x, 0, randomOffset.y);
            }

            return basePosition;
        }

        private void ApplyDifficultyScaling(GameObject enemy, SpawnableEnemyEntry entry)
        {
            if (entry.healthMultiplier != 1f && enemy.TryGetComponent<UnitHealth>(out var health))
            {
                health.SetMaxHealth(health.MaxHealth * entry.healthMultiplier);
            }

            if (entry.damageMultiplier != 1f && enemy.TryGetComponent<UnitCombat>(out var combat))
            {
                combat.SetAttackDamage(combat.AttackDamage * entry.damageMultiplier);
            }
        }

        private IEnumerator MoveToRallyPoint(GameObject unit, Vector3 destination)
        {
            yield return null; // Wait a frame for initialization

            if (unit == null) yield break;

            if (unit.TryGetComponent<UnitMovement>(out var movement))
            {
                movement.SetDestination(destination);
            }
            else if (unit.TryGetComponent<NavMeshAgent>(out var agent))
            {
                agent.SetDestination(destination);
            }
        }

        private int CountActiveEnemies()
        {
            CleanupDeadReferences();
            return spawnedEnemies.Count;
        }

        private void CleanupDeadReferences()
        {
            spawnedEnemies.RemoveAll(e => e == null);
        }

        #endregion

        #region Rally Point

        /// <summary>
        /// Set the rally point position.
        /// </summary>
        public void SetRallyPoint(Vector3 position)
        {
            if (rallyPoint == null)
            {
                GameObject rallyObj = new GameObject("RallyPoint_EnemySpawner");
                rallyPoint = rallyObj.transform;
            }
            rallyPoint.position = position;
        }

        /// <summary>
        /// Clear the rally point.
        /// </summary>
        public void ClearRallyPoint()
        {
            if (rallyPoint != null)
            {
                Destroy(rallyPoint.gameObject);
                rallyPoint = null;
            }
        }

        public Transform GetRallyPoint() => rallyPoint;
        public Transform GetSpawnPoint() => spawnPoint;

        #endregion

        #region Debug / Gizmos

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;

            // Draw spawn point
            Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.forward * 3f;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(spawnPos, 0.5f);
            Gizmos.DrawLine(transform.position, spawnPos);

            // Draw spawn radius
            if (config != null && config.spawnRadius > 0)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                DrawCircle(spawnPos, config.spawnRadius);
            }

            // Draw rally point
            if (rallyPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(rallyPoint.position, 0.5f);
                Gizmos.DrawLine(spawnPos, rallyPoint.position);
            }
        }

        private void DrawCircle(Vector3 center, float radius)
        {
            int segments = 32;
            float angle = 0f;
            Vector3 lastPoint = center + new Vector3(radius, 0, 0);

            for (int i = 1; i <= segments; i++)
            {
                angle = i * (360f / segments) * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(lastPoint, newPoint);
                lastPoint = newPoint;
            }
        }

        [ContextMenu("Spawn Single Enemy")]
        private void DebugSpawnSingle()
        {
            SpawnSingleEnemy();
        }

        [ContextMenu("Spawn Batch (Current Quantity)")]
        private void DebugSpawnBatch()
        {
            SpawnBatch(spawnQuantity);
        }

        [ContextMenu("Start Continuous Spawning")]
        private void DebugStartSpawning()
        {
            loopSpawning = true;
            StartSpawning();
        }

        [ContextMenu("Stop Spawning")]
        private void DebugStopSpawning()
        {
            StopSpawning();
        }

        [ContextMenu("Kill All Spawned")]
        private void DebugKillAll()
        {
            KillAllSpawnedEnemies();
        }

        #endregion
    }

    #region Events

    /// <summary>
    /// Event published when an enemy spawner is selected.
    /// </summary>
    public struct EnemySpawnerSelectedEvent
    {
        public EnemySpawnerBuilding Spawner { get; }

        public EnemySpawnerSelectedEvent(EnemySpawnerBuilding spawner)
        {
            Spawner = spawner;
        }
    }

    /// <summary>
    /// Event published when an enemy spawner is deselected.
    /// </summary>
    public struct EnemySpawnerDeselectedEvent
    {
        public EnemySpawnerBuilding Spawner { get; }

        public EnemySpawnerDeselectedEvent(EnemySpawnerBuilding spawner)
        {
            Spawner = spawner;
        }
    }

    #endregion
}
