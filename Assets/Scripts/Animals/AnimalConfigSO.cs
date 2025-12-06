using UnityEngine;

namespace RTS.Animals
{
    /// <summary>
    /// ScriptableObject defining animal configuration.
    /// Similar to UnitConfigSO but tailored for animal behavior.
    /// </summary>
    [CreateAssetMenu(fileName = "AnimalConfig", menuName = "RTS/Animals/Animal Config")]
    public class AnimalConfigSO : ScriptableObject
    {
        [Header("Identity")]
        public AnimalType animalType;
        public string animalName = "Animal";
        [TextArea(2, 4)]
        public string description = "A wild animal";

        [Header("Health")]
        public float maxHealth = 50f;

        [Header("Movement")]
        public float moveSpeed = 2.5f;
        [Tooltip("Roaming radius from spawn point")]
        public float roamingRadius = 15f;
        [Tooltip("Time between roaming movements")]
        public float roamingInterval = 5f;

        [Header("Behavior")]
        [Tooltip("Can this animal be hunted by players?")]
        public bool canBeHunted = true;
        [Tooltip("Does this animal flee when attacked?")]
        public bool fleesWhenAttacked = true;
        [Tooltip("Detection range for threats")]
        public float detectionRange = 8f;
        [Tooltip("Flee distance when threatened")]
        public float fleeDistance = 15f;

        [Header("Biome Preferences")]
        [Tooltip("Biomes where this animal can spawn")]
        public BiomeSpawnPreference[] biomePreferences;

        [Header("Visual")]
        public GameObject animalPrefab;
        public Sprite animalIcon;

        [Header("Resources (Future)")]
        [Tooltip("Resources dropped when hunted")]
        public int foodValue = 10;
    }

    /// <summary>
    /// Defines spawn probability for each biome type.
    /// </summary>
    [System.Serializable]
    public class BiomeSpawnPreference
    {
        public BiomeType biome;
        [Range(0f, 1f)]
        [Tooltip("Spawn probability in this biome (0 = never, 1 = always)")]
        public float spawnProbability = 0.5f;
    }
}
