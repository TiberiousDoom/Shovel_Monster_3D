using UnityEngine;

namespace VoxelRPG.Voxel.Generation
{
    /// <summary>
    /// Main world generator that uses noise-based terrain generation.
    /// Coordinates biome selection, terrain shaping, and vegetation placement.
    /// </summary>
    public class WorldGenerator : MonoBehaviour, IWorldGenerator
    {
        [Header("Generation Settings")]
        [Tooltip("Seed for deterministic generation (0 = random)")]
        [SerializeField] private int _seed;

        [Tooltip("Scale of the terrain noise (smaller = more stretched terrain)")]
        [SerializeField] private float _terrainScale = 0.02f;

        [Tooltip("Number of octaves for terrain noise")]
        [SerializeField] private int _terrainOctaves = 4;

        [Tooltip("Persistence for terrain noise octaves")]
        [SerializeField] private float _terrainPersistence = 0.5f;

        [Tooltip("Lacunarity for terrain noise octaves")]
        [SerializeField] private float _terrainLacunarity = 2f;

        [Header("References")]
        [Tooltip("Biome manager for biome selection (optional)")]
        [SerializeField] private BiomeManager _biomeManager;

        [Tooltip("Default biome if no biome manager is assigned")]
        [SerializeField] private BiomeDefinition _defaultBiome;

        private System.Random _random;
        private VoxelWorld _world;
        private VegetationGenerator _vegetationGenerator;
        private float _seedOffsetX;
        private float _seedOffsetZ;

        /// <inheritdoc/>
        public int Seed => _seed;

        private bool _isInitialized;

        /// <summary>
        /// Initializes the generator with the configured seed.
        /// Called automatically before generation if not already initialized.
        /// </summary>
        public void Initialize()
        {
            if (_seed == 0)
            {
                _seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            }

            _random = new System.Random(_seed);
            _seedOffsetX = (float)(_random.NextDouble() * 10000);
            _seedOffsetZ = (float)(_random.NextDouble() * 10000);

            _vegetationGenerator = new VegetationGenerator(_seed);
            _isInitialized = true;

            Debug.Log($"[WorldGenerator] Initialized with seed: {_seed}");
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Sets the voxel world reference for generation.
        /// </summary>
        /// <param name="world">The voxel world to generate into.</param>
        public void SetWorld(VoxelWorld world)
        {
            _world = world;
        }

        /// <summary>
        /// Sets the default biome for generation.
        /// </summary>
        /// <param name="biome">The default biome to use.</param>
        public void SetDefaultBiome(BiomeDefinition biome)
        {
            _defaultBiome = biome;
        }

        /// <summary>
        /// Sets the generation seed.
        /// </summary>
        /// <param name="seed">Seed value (0 = random).</param>
        public void SetSeed(int seed)
        {
            _seed = seed;
            Initialize();
        }

        /// <inheritdoc/>
        public void GenerateChunk(VoxelChunk chunk)
        {
            var chunkWorldPos = chunk.ChunkPosition * VoxelChunk.SIZE;

            // First pass: Generate terrain
            for (int x = 0; x < VoxelChunk.SIZE; x++)
            {
                for (int z = 0; z < VoxelChunk.SIZE; z++)
                {
                    int worldX = chunkWorldPos.x + x;
                    int worldZ = chunkWorldPos.z + z;

                    var biome = GetBiomeAt(worldX, worldZ);
                    int surfaceHeight = GetSurfaceHeight(worldX, worldZ);

                    for (int y = 0; y < VoxelChunk.SIZE; y++)
                    {
                        int worldY = chunkWorldPos.y + y;
                        var block = DetermineBlock(worldX, worldY, worldZ, surfaceHeight, biome);

                        if (block != null && block != BlockType.Air)
                        {
                            chunk.SetBlockLocal(new Vector3Int(x, y, z), block);
                        }
                    }
                }
            }

            // Second pass: Generate vegetation (trees, etc.)
            GenerateVegetation(chunk, chunkWorldPos);
        }

        private void GenerateVegetation(VoxelChunk chunk, Vector3Int chunkWorldPos)
        {
            // Use chunk position as additional seed for consistent vegetation
            var chunkRandom = new System.Random(_seed + chunkWorldPos.x * 73856093 + chunkWorldPos.z * 19349663);

            for (int x = 0; x < VoxelChunk.SIZE; x++)
            {
                for (int z = 0; z < VoxelChunk.SIZE; z++)
                {
                    int worldX = chunkWorldPos.x + x;
                    int worldZ = chunkWorldPos.z + z;

                    var biome = GetBiomeAt(worldX, worldZ);
                    if (biome == null || biome.TreeChance <= 0)
                    {
                        continue;
                    }

                    // Check if we should spawn a tree
                    if (chunkRandom.NextDouble() < biome.TreeChance)
                    {
                        int surfaceHeight = GetSurfaceHeight(worldX, worldZ);
                        int worldY = surfaceHeight + 1;

                        // Only spawn if surface is within this chunk's Y range
                        int localY = worldY - chunkWorldPos.y;
                        if (localY >= 0 && localY < VoxelChunk.SIZE)
                        {
                            // Check if surface block is appropriate (top block, not water)
                            var surfaceBlock = GetBlockAt(new Vector3Int(worldX, surfaceHeight, worldZ));
                            if (surfaceBlock == biome.TopBlock)
                            {
                                var treeType = biome.GetRandomTreeType(chunkRandom);
                                if (treeType != null)
                                {
                                    _vegetationGenerator.GenerateTree(
                                        _world,
                                        new Vector3Int(worldX, worldY, worldZ),
                                        treeType,
                                        chunkRandom
                                    );
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public BlockType GetBlockAt(Vector3Int worldPosition)
        {
            var biome = GetBiomeAt(worldPosition.x, worldPosition.z);
            int surfaceHeight = GetSurfaceHeight(worldPosition.x, worldPosition.z);

            return DetermineBlock(worldPosition.x, worldPosition.y, worldPosition.z, surfaceHeight, biome);
        }

        /// <inheritdoc/>
        public int GetSurfaceHeight(int x, int z)
        {
            var biome = GetBiomeAt(x, z);
            if (biome == null)
            {
                return 32; // Default height
            }

            float noise = GetTerrainNoise(x, z);
            return biome.BaseHeight + Mathf.RoundToInt(noise * biome.HeightVariation);
        }

        /// <inheritdoc/>
        public BiomeDefinition GetBiomeAt(int x, int z)
        {
            if (_biomeManager != null)
            {
                return _biomeManager.GetBiomeAt(x, z);
            }

            return _defaultBiome;
        }

        private BlockType DetermineBlock(int worldX, int worldY, int worldZ, int surfaceHeight, BiomeDefinition biome)
        {
            if (biome == null)
            {
                return BlockType.Air;
            }

            // Above surface
            if (worldY > surfaceHeight)
            {
                // Check for water
                if (worldY <= biome.WaterLevel && biome.WaterBlock != null)
                {
                    return biome.WaterBlock;
                }
                return BlockType.Air;
            }

            // At surface
            if (worldY == surfaceHeight)
            {
                // Beach near water level
                if (worldY <= biome.WaterLevel + biome.BeachHeight && biome.BeachBlock != null)
                {
                    return biome.BeachBlock;
                }
                return biome.TopBlock;
            }

            // Below surface - filler layer
            if (worldY > surfaceHeight - biome.FillerDepth && worldY > biome.StoneStartHeight)
            {
                // Beach filler near water
                if (surfaceHeight <= biome.WaterLevel + biome.BeachHeight && biome.BeachBlock != null)
                {
                    return biome.BeachBlock;
                }
                return biome.FillerBlock;
            }

            // Deep underground - stone
            return biome.StoneBlock;
        }

        private float GetTerrainNoise(int x, int z)
        {
            float amplitude = 1f;
            float frequency = 1f;
            float noiseValue = 0f;
            float maxValue = 0f;

            for (int i = 0; i < _terrainOctaves; i++)
            {
                float sampleX = (x + _seedOffsetX) * _terrainScale * frequency;
                float sampleZ = (z + _seedOffsetZ) * _terrainScale * frequency;

                noiseValue += Mathf.PerlinNoise(sampleX, sampleZ) * amplitude;
                maxValue += amplitude;

                amplitude *= _terrainPersistence;
                frequency *= _terrainLacunarity;
            }

            // Normalize to 0-1 range
            return noiseValue / maxValue;
        }

        /// <summary>
        /// Generates the entire world using the assigned VoxelWorld.
        /// </summary>
        public void GenerateWorld()
        {
            if (_world == null)
            {
                Debug.LogError("[WorldGenerator] No VoxelWorld assigned. Call SetWorld() first.");
                return;
            }

            if (_defaultBiome == null && _biomeManager == null)
            {
                Debug.LogError("[WorldGenerator] No biome assigned. Call SetDefaultBiome() first.");
                return;
            }

            EnsureInitialized();

            Debug.Log("[WorldGenerator] Starting world generation...");

            foreach (var chunk in _world.GetAllChunks())
            {
                GenerateChunk(chunk);
            }

            _world.RebuildAllChunks();

            Debug.Log("[WorldGenerator] World generation complete.");
        }
    }
}
