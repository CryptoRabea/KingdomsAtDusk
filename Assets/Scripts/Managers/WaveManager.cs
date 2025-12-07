using UnityEngine;
using RTS.Core.Services;
using RTS.Core.Events;
using System.Collections.Generic;

namespace RTS.Managers
{
    /// <summary>
    /// Manages enemy wave spawning using object pooling and events.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        [Header("Wave Configuration")]
        [SerializeField] private WaveConfigSO[] waveConfigs;
        [SerializeField] private bool useInfiniteWaves = true;

        [Header("Spawn Settings")]
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float timeBetweenWaves = 30f;
        [SerializeField] private float spawnInterval = 0.5f; // Time between spawning each unit

        [Header("Scaling (for infinite waves)")]
        [SerializeField] private int baseEnemyCount = 3;
        [SerializeField] private int enemiesPerWave = 2;
        [SerializeField] private float difficultyScaling = 1.1f;

        private int currentWaveNumber = 0;
        private float waveTimer = 0f;
        private bool isSpawningWave = false;
        private int activeEnemies = 0;
        private IPoolService poolService;

        private void Start()
        {
            poolService = ServiceLocator.TryGet<IPoolService>();
            
            if (poolService == null)
            {
            }

            // Subscribe to enemy death events
            EventBus.Subscribe<UnitDiedEvent>(OnUnitDied);

            // Start first wave timer
            waveTimer = timeBetweenWaves;
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<UnitDiedEvent>(OnUnitDied);
        }

        private void Update()
        {
            if (isSpawningWave) return;

            waveTimer += Time.deltaTime;
            if (waveTimer >= timeBetweenWaves)
            {
                StartWave();
                waveTimer = 0f;
            }
        }

        private void StartWave()
        {
            currentWaveNumber++;

            WaveConfig config = GetWaveConfig(currentWaveNumber);
            
            EventBus.Publish(new WaveStartedEvent(currentWaveNumber, config.TotalEnemyCount));

            StartCoroutine(SpawnWaveCoroutine(config));
        }

        private WaveConfig GetWaveConfig(int waveNumber)
        {
            // Use predefined config if available
            if (waveConfigs != null && waveNumber <= waveConfigs.Length)
            {
                return WaveConfig.FromScriptableObject(waveConfigs[waveNumber - 1]);
            }

            // Generate procedural wave for infinite mode
            if (useInfiniteWaves)
            {
                return GenerateProceduralWave(waveNumber);
            }

            // No more waves
            return new WaveConfig { TotalEnemyCount = 0 };
        }

        private WaveConfig GenerateProceduralWave(int waveNumber)
        {
            int enemyCount = baseEnemyCount + (waveNumber - 1) * enemiesPerWave;
            float healthMultiplier = Mathf.Pow(difficultyScaling, waveNumber - 1);

            // For now, simple config - could be expanded
            return new WaveConfig
            {
                TotalEnemyCount = enemyCount,
                HealthMultiplier = healthMultiplier,
                DamageMultiplier = healthMultiplier
            };
        }

        private System.Collections.IEnumerator SpawnWaveCoroutine(WaveConfig config)
        {
            isSpawningWave = true;

            for (int i = 0; i < config.TotalEnemyCount; i++)
            {
                SpawnEnemy(config);
                yield return new WaitForSeconds(spawnInterval);
            }

            isSpawningWave = false;
        }

        private void SpawnEnemy(WaveConfig config)
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                return;
            }

            // Select random spawn point
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            
            // Get enemy prefab (would normally come from config)
            GameObject enemyPrefab = config.GetRandomEnemyPrefab();
            if (enemyPrefab == null)
            {
                return;
            }

            // Spawn using pool if available, otherwise instantiate
            GameObject enemy;
            if (poolService != null)
            {
                enemy = poolService.Get(enemyPrefab.transform).gameObject;
                enemy.transform.position = spawnPoint.position;
                enemy.transform.rotation = spawnPoint.rotation;
            }
            else
            {
                enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
            }

            // Apply difficulty scaling
            ApplyDifficultyScaling(enemy, config);

            activeEnemies++;
            EventBus.Publish(new UnitSpawnedEvent(enemy, spawnPoint.position));
        }

        private void ApplyDifficultyScaling(GameObject enemy, WaveConfig config)
        {
            var health = enemy.GetComponent<Units.UnitHealth>();
            if (health != null && config.HealthMultiplier > 1f)
            {
                health.SetMaxHealth(health.MaxHealth * config.HealthMultiplier);
            }

            var combat = enemy.GetComponent<Units.UnitCombat>();
            if (combat != null && config.DamageMultiplier > 1f)
            {
                combat.SetAttackDamage(combat.AttackDamage * config.DamageMultiplier);
            }
        }

        private void OnUnitDied(UnitDiedEvent evt)
        {
            if (evt.WasEnemy)
            {
                activeEnemies--;

                if (activeEnemies <= 0 && !isSpawningWave)
                {
                    EventBus.Publish(new WaveCompletedEvent(currentWaveNumber));
                }
            }
        }

        #region Debug

        [ContextMenu("Spawn Wave Now")]
        private void DebugSpawnWave()
        {
            waveTimer = timeBetweenWaves;
        }

        #endregion
    }

    /// <summary>
    /// Configuration for a wave (can be procedural or from ScriptableObject).
    /// </summary>
    public class WaveConfig
    {
        public int TotalEnemyCount;
        public float HealthMultiplier = 1f;
        public float DamageMultiplier = 1f;
        public GameObject[] EnemyPrefabs;

        public GameObject GetRandomEnemyPrefab()
        {
            if (EnemyPrefabs == null || EnemyPrefabs.Length == 0)
                return null;

            return EnemyPrefabs[Random.Range(0, EnemyPrefabs.Length)];
        }

        public static WaveConfig FromScriptableObject(WaveConfigSO so)
        {
            return new WaveConfig
            {
                TotalEnemyCount = so.enemyCount,
                HealthMultiplier = so.healthMultiplier,
                DamageMultiplier = so.damageMultiplier,
                EnemyPrefabs = so.enemyPrefabs
            };
        }
    }

    /// <summary>
    /// ScriptableObject for designing waves in the editor.
    /// </summary>
    [CreateAssetMenu(fileName = "WaveConfig", menuName = "RTS/WaveConfig")]
    public class WaveConfigSO : ScriptableObject
    {
        public int enemyCount = 5;
        public GameObject[] enemyPrefabs;
        public float healthMultiplier = 1f;
        public float damageMultiplier = 1f;
        [TextArea] public string waveDescription;
    }
}
