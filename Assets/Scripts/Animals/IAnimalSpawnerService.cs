namespace RTS.Animals
{
    /// <summary>
    /// Interface for animal spawning system.
    /// </summary>
    public interface IAnimalSpawnerService
    {
        /// <summary>
        /// Start spawning animals based on configured biomes.
        /// </summary>
        void StartSpawning();

        /// <summary>
        /// Stop all animal spawning.
        /// </summary>
        void StopSpawning();

        /// <summary>
        /// Manually spawn a specific animal at a position.
        /// </summary>
        void SpawnAnimal(AnimalConfigSO config, UnityEngine.Vector3 position);

        /// <summary>
        /// Get current animal population count.
        /// </summary>
        int GetAnimalCount();

        /// <summary>
        /// Get animal count for a specific type.
        /// </summary>
        int GetAnimalCount(AnimalType type);
    }
}
