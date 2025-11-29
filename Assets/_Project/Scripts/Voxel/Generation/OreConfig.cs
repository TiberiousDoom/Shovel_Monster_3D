using System;
using UnityEngine;

namespace VoxelRPG.Voxel.Generation
{
    /// <summary>
    /// Configuration for ore vein generation within a biome.
    /// Defines spawn rates, depth ranges, and vein characteristics.
    /// </summary>
    [Serializable]
    public class OreConfig
    {
        [Tooltip("The ore block type to generate")]
        public BlockType OreBlock;

        [Tooltip("Minimum depth below surface where ore can spawn")]
        [Range(0, 128)]
        public int MinDepth = 5;

        [Tooltip("Maximum depth below surface where ore can spawn")]
        [Range(0, 256)]
        public int MaxDepth = 64;

        [Tooltip("Chance of ore spawning where conditions are met (0-1)")]
        [Range(0f, 1f)]
        public float SpawnChance = 0.05f;

        [Tooltip("Scale of noise for vein generation (smaller = larger veins)")]
        [Range(0.01f, 0.5f)]
        public float NoiseScale = 0.1f;

        /// <summary>
        /// Creates a default ore configuration.
        /// </summary>
        public OreConfig()
        {
            MinDepth = 5;
            MaxDepth = 64;
            SpawnChance = 0.05f;
            NoiseScale = 0.1f;
        }

        /// <summary>
        /// Creates an ore configuration with specified parameters.
        /// </summary>
        /// <param name="oreBlock">The ore block type.</param>
        /// <param name="minDepth">Minimum spawn depth.</param>
        /// <param name="maxDepth">Maximum spawn depth.</param>
        /// <param name="spawnChance">Spawn probability (0-1).</param>
        /// <param name="noiseScale">Noise scale for vein shape.</param>
        public OreConfig(BlockType oreBlock, int minDepth, int maxDepth,
            float spawnChance, float noiseScale = 0.1f)
        {
            OreBlock = oreBlock;
            MinDepth = minDepth;
            MaxDepth = maxDepth;
            SpawnChance = Mathf.Clamp01(spawnChance);
            NoiseScale = noiseScale;
        }
    }
}
