using UnityEngine;
using System.Collections.Generic;

namespace RTS.Animals
{
    /// <summary>
    /// Manages biome detection and terrain classification.
    /// Uses terrain sampling to determine biome types at specific positions.
    /// </summary>
    public class BiomeManager : MonoBehaviour
    {
        [Header("Biome Configuration")]
        [SerializeField] private BiomeData[] biomeConfigs;
        [SerializeField] private Terrain terrain;

        [Header("Sampling Settings")]
        // TODO: Implement sampling radius feature in future update
        // [SerializeField] private float sampleRadius = 5f;
        [SerializeField] private int samplePoints = 4;

        private Dictionary<BiomeType, BiomeData> biomeLookup = new Dictionary<BiomeType, BiomeData>();

        private void Awake()
        {
            // Build biome lookup
            if (biomeConfigs != null)
            {
                foreach (var biome in biomeConfigs)
                {
                    if (!biomeLookup.ContainsKey(biome.biomeType))
                    {
                        biomeLookup.Add(biome.biomeType, biome);
                    }
                }
            }

            // Find terrain if not assigned
            if (terrain == null)
            {
                terrain = FindFirstObjectByType<Terrain>();
            }

        }

        /// <summary>
        /// Determine the biome type at a specific world position.
        /// </summary>
        public BiomeType GetBiomeAtPosition(Vector3 worldPosition)
        {
            if (terrain == null)
            {
                return BiomeType.Grassland;
            }

            // Get terrain height and slope
            float height = worldPosition.y;
            float slope = GetSlopeAtPosition(worldPosition);

            // Sample terrain textures
            float[] textureMix = GetTerrainTexturesAtPosition(worldPosition);

            // Determine biome based on rules
            BiomeType detectedBiome = DetermineBiomeFromSample(height, slope, textureMix);

            return detectedBiome;
        }

        /// <summary>
        /// Get biome data for a specific biome type.
        /// </summary>
        public BiomeData GetBiomeData(BiomeType biomeType)
        {
            if (biomeLookup.TryGetValue(biomeType, out BiomeData data))
            {
                return data;
            }

            return null;
        }

        /// <summary>
        /// Get all configured biomes.
        /// </summary>
        public BiomeData[] GetAllBiomes()
        {
            return biomeConfigs;
        }

        #region Terrain Sampling

        private float GetSlopeAtPosition(Vector3 worldPosition)
        {
            if (terrain == null) return 0f;

            // Sample terrain normal to calculate slope
            TerrainData terrainData = terrain.terrainData;
            Vector3 terrainPosition = worldPosition - terrain.transform.position;

            // Normalize position to terrain coordinates (0-1)
            float normalizedX = Mathf.Clamp01(terrainPosition.x / terrainData.size.x);
            float normalizedZ = Mathf.Clamp01(terrainPosition.z / terrainData.size.z);

            // Get interpolated normal
            Vector3 normal = terrainData.GetInterpolatedNormal(normalizedX, normalizedZ);

            // Calculate slope from normal (angle from up vector)
            float slope = Vector3.Angle(normal, Vector3.up);

            return slope;
        }

        private float[] GetTerrainTexturesAtPosition(Vector3 worldPosition)
        {
            if (terrain == null) return new float[0];

            TerrainData terrainData = terrain.terrainData;
            Vector3 terrainPosition = worldPosition - terrain.transform.position;

            // Get terrain texture mix at position
            int mapX = (int)((terrainPosition.x / terrainData.size.x) * terrainData.alphamapWidth);
            int mapZ = (int)((terrainPosition.z / terrainData.size.z) * terrainData.alphamapHeight);

            // Clamp to valid range
            mapX = Mathf.Clamp(mapX, 0, terrainData.alphamapWidth - 1);
            mapZ = Mathf.Clamp(mapZ, 0, terrainData.alphamapHeight - 1);

            // Get the splat map data
            float[,,] splatmapData = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);

            // Extract texture weights
            int textureCount = terrainData.alphamapLayers;
            float[] textureMix = new float[textureCount];

            for (int i = 0; i < textureCount; i++)
            {
                textureMix[i] = splatmapData[0, 0, i];
            }

            return textureMix;
        }

        private BiomeType DetermineBiomeFromSample(float height, float slope, float[] textureMix)
        {
            // Simple biome determination logic
            // This can be expanded with more complex rules

            // Check each biome's conditions
            foreach (var kvp in biomeLookup)
            {
                BiomeData biomeData = kvp.Value;

                // Check height range
                if (height < biomeData.heightRange.x || height > biomeData.heightRange.y)
                    continue;

                // Check slope range
                if (slope < biomeData.slopeRange.x || slope > biomeData.slopeRange.y)
                    continue;

                // If we reach here, this biome matches
                return kvp.Key;
            }

            // Default to grassland if no match
            return BiomeType.Grassland;
        }

        #endregion

        #region Spawn Position Validation

        /// <summary>
        /// Check if a position is valid for spawning in a specific biome.
        /// </summary>
        public bool IsValidSpawnPosition(Vector3 position, BiomeType requiredBiome)
        {
            // Check if position is on terrain
            if (!IsPositionOnTerrain(position))
                return false;

            // Check if biome matches
            BiomeType biomeAtPosition = GetBiomeAtPosition(position);
            if (biomeAtPosition != requiredBiome)
                return false;

            // Check if not too steep
            float slope = GetSlopeAtPosition(position);
            if (slope > 45f)
                return false;

            return true;
        }

        /// <summary>
        /// Find a random valid spawn position within a radius.
        /// </summary>
        public bool TryGetRandomSpawnPosition(Vector3 center, float radius, BiomeType preferredBiome, out Vector3 spawnPosition)
        {
            const int maxAttempts = 20;

            for (int i = 0; i < maxAttempts; i++)
            {
                // Get random point in circle
                Vector2 randomCircle = Random.insideUnitCircle * radius;
                Vector3 randomPoint = center + new Vector3(randomCircle.x, 0f, randomCircle.y);

                // Sample terrain height
                if (Physics.Raycast(randomPoint + Vector3.up * 100f, Vector3.down, out RaycastHit hit, 200f, LayerMask.GetMask("Ground", "Terrain")))
                {
                    randomPoint.y = hit.point.y;

                    // Check if valid
                    if (IsValidSpawnPosition(randomPoint, preferredBiome))
                    {
                        spawnPosition = randomPoint;
                        return true;
                    }
                }
            }

            spawnPosition = Vector3.zero;
            return false;
        }

        private bool IsPositionOnTerrain(Vector3 position)
        {
            if (terrain == null) return true; // Assume valid if no terrain

            Vector3 terrainPosition = position - terrain.transform.position;
            TerrainData terrainData = terrain.terrainData;

            return terrainPosition.x >= 0 && terrainPosition.x <= terrainData.size.x &&
                   terrainPosition.z >= 0 && terrainPosition.z <= terrainData.size.z;
        }

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            // Visualize biome sampling
            if (terrain != null && biomeLookup.Count > 0)
            {
                // Draw sample grid
                TerrainData terrainData = terrain.terrainData;
                int gridSize = 10;
                float cellSizeX = terrainData.size.x / gridSize;
                float cellSizeZ = terrainData.size.z / gridSize;

                for (int x = 0; x < gridSize; x++)
                {
                    for (int z = 0; z < gridSize; z++)
                    {
                        Vector3 worldPos = terrain.transform.position + new Vector3(
                            x * cellSizeX + cellSizeX * 0.5f,
                            0f,
                            z * cellSizeZ + cellSizeZ * 0.5f
                        );

                        BiomeType biome = GetBiomeAtPosition(worldPos);
                        BiomeData biomeData = GetBiomeData(biome);

                        if (biomeData != null)
                        {
                            Gizmos.color = biomeData.biomeColor;
                        }
                        else
                        {
                            Gizmos.color = Color.gray;
                        }

                        // Sample height
                        float height = terrain.SampleHeight(worldPos);
                        worldPos.y = height;

                        Gizmos.DrawWireCube(worldPos, new Vector3(cellSizeX * 0.8f, 0.5f, cellSizeZ * 0.8f));
                    }
                }
            }
        }

        #endregion
    }
}
