using UnityEngine;
using System.Collections.Generic;

namespace RTS.Managers
{
    /// <summary>
    /// Helper class to generate interesting enemy wave compositions.
    /// Creates varied waves with different enemy types for engaging gameplay.
    /// </summary>
    public class EnemyWaveGenerator : MonoBehaviour
    {
        [Header("Enemy Prefabs")]
        [SerializeField] private GameObject enemyFootmanPrefab;
        [SerializeField] private GameObject enemyOrcWarriorPrefab;
        [SerializeField] private GameObject enemyBerserkerPrefab;
        [SerializeField] private GameObject enemyTankPrefab;
        [SerializeField] private GameObject enemyArcherPrefab;
        [SerializeField] private GameObject bossPrefab;

        [Header("Wave Composition Settings")]
        [SerializeField] private int waveNumberForBerserkers = 3;
        [SerializeField] private int waveNumberForTanks = 5;
        [SerializeField] private int waveNumberForArchers = 4;
        [SerializeField] private int waveNumberForBoss = 10;
        [SerializeField] private int bossWaveInterval = 10; // Boss every 10 waves

        /// <summary>
        /// Generate a wave configuration for a given wave number with progressive difficulty
        /// </summary>
        public WaveConfig GenerateWave(int waveNumber)
        {
            List<GameObject> enemyList = new List<GameObject>();
            int baseEnemies = 3 + (waveNumber - 1) * 2;

            // Early waves: Just footmen and orcs
            if (waveNumber < waveNumberForBerserkers)
            {
                enemyList.AddRange(GetBasicEnemies(baseEnemies));
            }
            // Wave 3-4: Introduce berserkers
            else if (waveNumber < waveNumberForArchers)
            {
                enemyList.AddRange(GetBasicEnemies(baseEnemies / 2));
                enemyList.AddRange(GetBerserkers(baseEnemies / 4));
            }
            // Wave 4-5: Add archers
            else if (waveNumber < waveNumberForTanks)
            {
                enemyList.AddRange(GetBasicEnemies(baseEnemies / 3));
                enemyList.AddRange(GetBerserkers(baseEnemies / 4));
                enemyList.AddRange(GetArchers(baseEnemies / 3));
            }
            // Wave 5-9: Mixed composition with tanks
            else if (waveNumber < waveNumberForBoss)
            {
                enemyList.AddRange(GetBasicEnemies(baseEnemies / 4));
                enemyList.AddRange(GetBerserkers(baseEnemies / 4));
                enemyList.AddRange(GetArchers(baseEnemies / 4));
                enemyList.AddRange(GetTanks(baseEnemies / 6));
            }
            // Boss waves
            else if (waveNumber % bossWaveInterval == 0)
            {
                // Boss wave with elite support
                enemyList.AddRange(GetBosses(1));
                enemyList.AddRange(GetTanks(2));
                enemyList.AddRange(GetBerserkers(3));
                enemyList.AddRange(GetArchers(3));
            }
            // Late game: Full mixed compositions
            else
            {
                int portion = baseEnemies / 5;
                enemyList.AddRange(GetBasicEnemies(portion));
                enemyList.AddRange(GetBerserkers(portion));
                enemyList.AddRange(GetArchers(portion));
                enemyList.AddRange(GetTanks(portion));
                enemyList.AddRange(GetBasicEnemies(portion)); // Extra basic units
            }

            // Calculate difficulty scaling
            float healthMultiplier = 1f + (waveNumber - 1) * 0.05f; // +5% per wave
            float damageMultiplier = 1f + (waveNumber - 1) * 0.03f; // +3% per wave

            return new WaveConfig
            {
                TotalEnemyCount = enemyList.Count,
                EnemyPrefabs = enemyList.ToArray(),
                HealthMultiplier = healthMultiplier,
                DamageMultiplier = damageMultiplier
            };
        }

        private List<GameObject> GetBasicEnemies(int count)
        {
            List<GameObject> enemies = new List<GameObject>();
            for (int i = 0; i < count; i++)
            {
                // Mix footmen and orcs
                enemies.Add(Random.value > 0.5f ? enemyFootmanPrefab : enemyOrcWarriorPrefab);
            }
            return enemies;
        }

        private List<GameObject> GetBerserkers(int count)
        {
            List<GameObject> enemies = new List<GameObject>();
            for (int i = 0; i < count; i++)
            {
                if (enemyBerserkerPrefab != null)
                    enemies.Add(enemyBerserkerPrefab);
            }
            return enemies;
        }

        private List<GameObject> GetTanks(int count)
        {
            List<GameObject> enemies = new List<GameObject>();
            for (int i = 0; i < count; i++)
            {
                if (enemyTankPrefab != null)
                    enemies.Add(enemyTankPrefab);
            }
            return enemies;
        }

        private List<GameObject> GetArchers(int count)
        {
            List<GameObject> enemies = new List<GameObject>();
            for (int i = 0; i < count; i++)
            {
                if (enemyArcherPrefab != null)
                    enemies.Add(enemyArcherPrefab);
            }
            return enemies;
        }

        private List<GameObject> GetBosses(int count)
        {
            List<GameObject> enemies = new List<GameObject>();
            for (int i = 0; i < count; i++)
            {
                if (bossPrefab != null)
                    enemies.Add(bossPrefab);
            }
            return enemies;
        }

        /// <summary>
        /// Get a description of what enemy types appear in this wave
        /// </summary>
        public string GetWaveDescription(int waveNumber)
        {
            if (waveNumber % bossWaveInterval == 0)
            {
                return $"⚠️ BOSS WAVE {waveNumber}! Prepare for battle!";
            }
            else if (waveNumber >= waveNumberForTanks)
            {
                return $"Wave {waveNumber}: Mixed Forces - All enemy types!";
            }
            else if (waveNumber >= waveNumberForArchers)
            {
                return $"Wave {waveNumber}: Ranged units detected!";
            }
            else if (waveNumber >= waveNumberForBerserkers)
            {
                return $"Wave {waveNumber}: Berserker units incoming!";
            }
            else
            {
                return $"Wave {waveNumber}: Basic forces";
            }
        }
    }
}
