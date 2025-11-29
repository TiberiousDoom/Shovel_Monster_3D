using UnityEngine;

namespace VoxelRPG.Voxel.Generation
{
    /// <summary>
    /// Generates ore veins within the world using noise-based placement.
    /// Supports multiple ore types with configurable density and depth ranges.
    /// </summary>
    public class OreGenerator
    {
        private readonly int _seed;
        private readonly float _seedOffsetX;
        private readonly float _seedOffsetY;
        private readonly float _seedOffsetZ;

        /// <summary>
        /// Creates a new ore generator with the specified seed.
        /// </summary>
        /// <param name="seed">Seed for deterministic ore placement.</param>
        public OreGenerator(int seed)
        {
            _seed = seed;
            var random = new System.Random(seed + 7919); // Different offset from terrain
            _seedOffsetX = (float)(random.NextDouble() * 10000);
            _seedOffsetY = (float)(random.NextDouble() * 10000);
            _seedOffsetZ = (float)(random.NextDouble() * 10000);
        }

        /// <summary>
        /// Determines if an ore should be placed at the given world position.
        /// </summary>
        /// <param name="worldPosition">The world position to check.</param>
        /// <param name="oreConfig">The ore configuration to check against.</param>
        /// <param name="surfaceHeight">The surface height at this X,Z position.</param>
        /// <returns>True if ore should be placed here.</returns>
        public bool ShouldPlaceOre(Vector3Int worldPosition, OreConfig oreConfig, int surfaceHeight)
        {
            if (oreConfig == null || oreConfig.OreBlock == null)
            {
                return false;
            }

            // Check depth constraints
            int depth = surfaceHeight - worldPosition.y;
            if (depth < oreConfig.MinDepth || depth > oreConfig.MaxDepth)
            {
                return false;
            }

            // Use 3D noise for ore vein generation
            float noiseValue = GetOreNoise(worldPosition, oreConfig.NoiseScale);

            // Apply threshold based on ore rarity
            // With multiplied noise (clusters near 0), spawn ore where noise < spawnChance
            // This creates vein-like pockets at positions with consistently low noise
            return noiseValue < oreConfig.SpawnChance;
        }

        /// <summary>
        /// Gets the ore type that should be placed at this position, if any.
        /// Checks all ore configs and returns the first match (priority order).
        /// </summary>
        /// <param name="worldPosition">The world position to check.</param>
        /// <param name="oreConfigs">Array of ore configurations to check.</param>
        /// <param name="surfaceHeight">The surface height at this X,Z position.</param>
        /// <param name="stoneBlock">The stone block type to replace.</param>
        /// <param name="currentBlock">The current block at this position.</param>
        /// <returns>The ore block type to place, or null if no ore should be placed.</returns>
        public BlockType GetOreAt(Vector3Int worldPosition, OreConfig[] oreConfigs,
            int surfaceHeight, BlockType stoneBlock, BlockType currentBlock)
        {
            // Only replace stone blocks
            if (currentBlock != stoneBlock)
            {
                return null;
            }

            if (oreConfigs == null || oreConfigs.Length == 0)
            {
                return null;
            }

            // Check each ore type in priority order
            foreach (var oreConfig in oreConfigs)
            {
                if (ShouldPlaceOre(worldPosition, oreConfig, surfaceHeight))
                {
                    return oreConfig.OreBlock;
                }
            }

            return null;
        }

        /// <summary>
        /// Generates a 3D noise value for ore placement.
        /// Uses Perlin noise with configurable scale.
        /// </summary>
        private float GetOreNoise(Vector3Int position, float scale)
        {
            // Use 3D noise by combining two 2D Perlin noise samples
            float sampleX = (position.x + _seedOffsetX) * scale;
            float sampleY = (position.y + _seedOffsetY) * scale;
            float sampleZ = (position.z + _seedOffsetZ) * scale;

            // Combine XY and YZ planes for pseudo-3D noise
            float noiseXY = Mathf.PerlinNoise(sampleX, sampleY);
            float noiseYZ = Mathf.PerlinNoise(sampleY, sampleZ);
            float noiseXZ = Mathf.PerlinNoise(sampleX, sampleZ);

            // Multiply the noise samples instead of averaging
            // This creates pockets where all three values are high (ore veins)
            // Result ranges 0-1, but ore only spawns where all three samples are high
            return noiseXY * noiseYZ * noiseXZ;
        }

        /// <summary>
        /// Generates ore veins within a chunk.
        /// Should be called after terrain generation but before vegetation.
        /// </summary>
        /// <param name="chunk">The chunk to generate ores in.</param>
        /// <param name="biome">The biome definition containing ore configs.</param>
        /// <param name="getSurfaceHeight">Function to get surface height at x,z.</param>
        public void GenerateOresInChunk(VoxelChunk chunk, BiomeDefinition biome,
            System.Func<int, int, int> getSurfaceHeight)
        {
            if (biome?.OreConfigs == null || biome.OreConfigs.Length == 0)
            {
                return;
            }

            var chunkWorldPos = chunk.ChunkPosition * VoxelChunk.SIZE;

            for (int x = 0; x < VoxelChunk.SIZE; x++)
            {
                for (int z = 0; z < VoxelChunk.SIZE; z++)
                {
                    int worldX = chunkWorldPos.x + x;
                    int worldZ = chunkWorldPos.z + z;
                    int surfaceHeight = getSurfaceHeight(worldX, worldZ);

                    for (int y = 0; y < VoxelChunk.SIZE; y++)
                    {
                        int worldY = chunkWorldPos.y + y;
                        var localPos = new Vector3Int(x, y, z);
                        var worldPos = new Vector3Int(worldX, worldY, worldZ);

                        var currentBlock = chunk.GetBlockLocal(localPos);

                        // Only replace stone blocks with ore
                        if (currentBlock == biome.StoneBlock)
                        {
                            var oreBlock = GetOreAt(worldPos, biome.OreConfigs,
                                surfaceHeight, biome.StoneBlock, currentBlock);

                            if (oreBlock != null)
                            {
                                chunk.SetBlockLocal(localPos, oreBlock);
                            }
                        }
                    }
                }
            }
        }
    }
}
