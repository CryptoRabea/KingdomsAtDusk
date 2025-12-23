using UnityEngine;
using System.Collections.Generic;

namespace RTS.Debug.EnemySpawner
{
    /// <summary>
    /// Configuration for an enemy type that can be spawned.
    /// </summary>
    [System.Serializable]
    public class SpawnableEnemyEntry
    {
        [Tooltip("Display name for this enemy type")]
        public string displayName = "Enemy";

        [Tooltip("The enemy prefab to spawn")]
        public GameObject prefab;

        [Tooltip("Optional icon for UI")]
        public Sprite icon;

        [Tooltip("Health multiplier for this enemy type")]
        [Range(0.1f, 10f)]
        public float healthMultiplier = 1f;

        [Tooltip("Damage multiplier for this enemy type")]
        [Range(0.1f, 10f)]
        public float damageMultiplier = 1f;
    }

    /// <summary>
    /// ScriptableObject containing enemy spawner configuration.
    /// Create via: Right-click in Project > Create > RTS/Debug/EnemySpawnerConfig
    ///
    /// This is a standalone debug tool - delete the entire Debug folder when not needed.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemySpawnerConfig", menuName = "RTS/Debug/EnemySpawnerConfig")]
    public class EnemySpawnerConfigSO : ScriptableObject
    {
        [Header("Spawnable Enemies")]
        [Tooltip("List of enemy types that can be spawned from this building")]
        public List<SpawnableEnemyEntry> spawnableEnemies = new List<SpawnableEnemyEntry>();

        [Header("Default Spawn Settings")]
        [Tooltip("Default quantity to spawn")]
        [Range(1, 100)]
        public int defaultQuantity = 5;

        [Tooltip("Default interval between spawns (seconds)")]
        [Range(0.1f, 30f)]
        public float defaultSpawnInterval = 1f;

        [Tooltip("Default delay before first spawn (seconds)")]
        [Range(0f, 60f)]
        public float defaultInitialDelay = 0f;

        [Header("Incremental Spawn Settings")]
        [Tooltip("Enable incremental spawning by default")]
        public bool defaultIncrementalEnabled = false;

        [Tooltip("How much to increase quantity each wave")]
        [Range(0, 20)]
        public int defaultIncrementalAmount = 1;

        [Tooltip("Time between incremental waves (seconds)")]
        [Range(1f, 300f)]
        public float defaultIncrementalInterval = 30f;

        [Header("Spawn Area Settings")]
        [Tooltip("Radius around spawn point where enemies can appear")]
        [Range(0f, 20f)]
        public float spawnRadius = 3f;

        [Tooltip("Randomize spawn positions within radius")]
        public bool randomizeSpawnPosition = true;

        /// <summary>
        /// Get a spawnable enemy entry by index.
        /// </summary>
        public SpawnableEnemyEntry GetEnemy(int index)
        {
            if (index >= 0 && index < spawnableEnemies.Count)
            {
                return spawnableEnemies[index];
            }
            return null;
        }

        /// <summary>
        /// Get all enemy display names for UI.
        /// </summary>
        public string[] GetEnemyNames()
        {
            var names = new string[spawnableEnemies.Count];
            for (int i = 0; i < spawnableEnemies.Count; i++)
            {
                names[i] = spawnableEnemies[i].displayName;
            }
            return names;
        }
    }
}
