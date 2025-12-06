using UnityEngine;
using System.Collections.Generic;

namespace RTS.Animals
{
    /// <summary>
    /// ScriptableObject defining biome characteristics and animal populations.
    /// </summary>
    [CreateAssetMenu(fileName = "BiomeData", menuName = "RTS/Animals/Biome Data")]
    public class BiomeData : ScriptableObject
    {
        [Header("Biome Settings")]
        public BiomeType biomeType;
        public string biomeName;
        [TextArea(2, 4)]
        public string description;
        public Color biomeColor = Color.green;

        [Header("Terrain Detection")]
        [Tooltip("Terrain texture names that identify this biome")]
        public string[] terrainTextureNames;
        [Tooltip("Height range for this biome (min, max)")]
        public Vector2 heightRange = new Vector2(0f, 100f);
        [Tooltip("Slope range for this biome (min degrees, max degrees)")]
        public Vector2 slopeRange = new Vector2(0f, 45f);

        [Header("Animal Population")]
        [Tooltip("Animals that can spawn in this biome")]
        public List<AnimalConfigSO> allowedAnimals = new List<AnimalConfigSO>();
        [Tooltip("Base spawn rate (animals per minute per square kilometer)")]
        public float spawnRate = 1f;
        [Tooltip("Maximum animal population density")]
        public int maxPopulation = 20;
    }
}
