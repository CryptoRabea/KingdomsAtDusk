using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using RTS.Core.Services;
using RTS.Core.Events;
using RTS.Units;

namespace RTS.Animals
{
    /// <summary>
    /// Service managing animal spawning across different biomes.
    /// Spawns animals based on terrain type and spawn probabilities.
    /// </summary>
    public class AnimalSpawner : MonoBehaviour, IAnimalSpawnerService
    {
        [Header("Spawn Configuration")]
        [SerializeField] private AnimalConfigSO[] animalConfigs;
        [SerializeField] private BiomeManager biomeManager;

        [Header("Spawn Settings")]
        [SerializeField] private float spawnInterval = 10f;
        [SerializeField] private int maxTotalAnimals = 100;
        [SerializeField] private float spawnRadius = 100f;
        [SerializeField] private Vector3 spawnCenter = Vector3.zero;

        [Header("Initial Spawn")]
        [SerializeField] private bool spawnOnStart = true;
        [SerializeField] private int initialAnimalCount = 20;

        private IPoolService poolService;
        private bool isSpawning = false;
        private float spawnTimer;

        // Track spawned animals
        private List<GameObject> spawnedAnimals = new List<GameObject>();
        private Dictionary<AnimalType, int> animalCounts = new Dictionary<AnimalType, int>();

        private void Awake()
        {
            // Register as service
            ServiceLocator.Register<IAnimalSpawnerService>(this);

            // Find biome manager if not assigned
            if (biomeManager == null)
            {
                biomeManager = FindFirstObjectByType<BiomeManager>();
            }

        }

        private void Start()
        {
            poolService = ServiceLocator.Get<IPoolService>();

            if (spawnOnStart)
            {
                SpawnInitialAnimals();
                StartSpawning();
            }

            // Subscribe to animal death events
            EventBus.Subscribe<AnimalDiedEvent>(OnAnimalDied);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<AnimalDiedEvent>(OnAnimalDied);
        }

        private void Update()
        {
            if (!isSpawning) return;

            spawnTimer += Time.deltaTime;

            if (spawnTimer >= spawnInterval)
            {
                spawnTimer = 0f;
                TrySpawnRandomAnimal();
            }
        }

        #region Spawning

        /// <summary>
        /// Spawn initial population of animals.
        /// </summary>
        private void SpawnInitialAnimals()
        {

            for (int i = 0; i < initialAnimalCount; i++)
            {
                TrySpawnRandomAnimal();
            }

        }

        /// <summary>
        /// Try to spawn a random animal based on biome preferences.
        /// </summary>
        private void TrySpawnRandomAnimal()
        {
            // Check population limit
            if (spawnedAnimals.Count >= maxTotalAnimals)
            {
                return;
            }

            // Pick a random animal config
            if (animalConfigs == null || animalConfigs.Length == 0)
            {
                return;
            }

            AnimalConfigSO randomConfig = animalConfigs[Random.Range(0, animalConfigs.Length)];

            // Find suitable spawn position based on animal's biome preferences
            if (TryFindSpawnPosition(randomConfig, out Vector3 spawnPosition, out BiomeType biome))
            {
                SpawnAnimal(randomConfig, spawnPosition);
            }
        }

        /// <summary>
        /// Find a suitable spawn position for an animal based on its biome preferences.
        /// </summary>
        private bool TryFindSpawnPosition(AnimalConfigSO config, out Vector3 position, out BiomeType biome)
        {
            // If no biome preferences, spawn anywhere
            if (config.biomePreferences == null || config.biomePreferences.Length == 0)
            {
                return TryGetRandomPosition(out position, out biome);
            }

            // Try each preferred biome based on probability
            var preferredBiomes = config.biomePreferences
                .OrderByDescending(bp => bp.spawnProbability)
                .ToList();

            foreach (var biomePref in preferredBiomes)
            {
                // Roll for spawn chance
                if (Random.value > biomePref.spawnProbability)
                    continue;

                // Try to find spawn position in this biome
                if (biomeManager != null)
                {
                    if (biomeManager.TryGetRandomSpawnPosition(spawnCenter, spawnRadius, biomePref.biome, out position))
                    {
                        biome = biomePref.biome;
                        return true;
                    }
                }
            }

            // Fallback: spawn anywhere
            return TryGetRandomPosition(out position, out biome);
        }

        /// <summary>
        /// Get a random spawn position (fallback method).
        /// </summary>
        private bool TryGetRandomPosition(out Vector3 position, out BiomeType biome)
        {
            const int maxAttempts = 10;

            for (int i = 0; i < maxAttempts; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                Vector3 randomPoint = spawnCenter + new Vector3(randomCircle.x, 0f, randomCircle.y);

                // Raycast to find ground
                if (Physics.Raycast(randomPoint + Vector3.up * 100f, Vector3.down, out RaycastHit hit, 200f, LayerMask.GetMask("Ground", "Terrain")))
                {
                    position = hit.point;
                    biome = biomeManager != null ? biomeManager.GetBiomeAtPosition(position) : BiomeType.Grassland;
                    return true;
                }
            }

            position = Vector3.zero;
            biome = BiomeType.Grassland;
            return false;
        }

        /// <summary>
        /// Spawn a specific animal at a position.
        /// </summary>
        public void SpawnAnimal(AnimalConfigSO config, Vector3 position)
        {
            if (config == null || config.animalPrefab == null)
            {
                return;
            }

            // Instantiate animal (use pooling if available)
            GameObject animalObj;

            if (poolService != null)
            {
                // Use object pooling
                if (config.animalPrefab.TryGetComponent<Transform>(out var prefabComponent))
                {
                }
                var instance = poolService.Get(prefabComponent);
                animalObj = instance.gameObject;
                animalObj.transform.position = position;
                animalObj.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            }
            else
            {
                // Instantiate directly
                animalObj = Instantiate(config.animalPrefab, position, Quaternion.Euler(0, Random.Range(0f, 360f), 0));
            }

            // Initialize animal behavior
            if (animalObj.TryGetComponent<AnimalBehavior>(out var animalBehavior))
            {
                animalBehavior.Initialize(config, position);
            }
            else
            {
            }

            // Track spawned animal
            spawnedAnimals.Add(animalObj);

            // Update counts
            if (!animalCounts.ContainsKey(config.animalType))
            {
                animalCounts[config.animalType] = 0;
            }
            animalCounts[config.animalType]++;

            // Publish spawn event
            EventBus.Publish(new AnimalSpawnedEvent(animalObj, config.animalType, position));

        }

        #endregion

        #region Event Handlers

        private void OnAnimalDied(AnimalDiedEvent evt)
        {
            // Remove from tracking
            if (spawnedAnimals.Contains(evt.Animal))
            {
                spawnedAnimals.Remove(evt.Animal);
            }

            // Update counts
            if (animalCounts.ContainsKey(evt.AnimalType))
            {
                animalCounts[evt.AnimalType]--;
                if (animalCounts[evt.AnimalType] < 0)
                    animalCounts[evt.AnimalType] = 0;
            }

        }

        #endregion

        #region IAnimalSpawnerService Implementation

        public void StartSpawning()
        {
            isSpawning = true;
            spawnTimer = 0f;
        }

        public void StopSpawning()
        {
            isSpawning = false;
        }

        public int GetAnimalCount()
        {
            return spawnedAnimals.Count;
        }

        public int GetAnimalCount(AnimalType type)
        {
            if (animalCounts.TryGetValue(type, out int count))
            {
                return count;
            }
            return 0;
        }

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            // Draw spawn radius
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(spawnCenter, spawnRadius);

            // Draw spawned animals
            Gizmos.color = Color.green;
            foreach (var animal in spawnedAnimals)
            {
                if (animal != null)
                {
                    Gizmos.DrawWireSphere(animal.transform.position, 0.5f);
                }
            }
        }

        #endregion
    }
}
