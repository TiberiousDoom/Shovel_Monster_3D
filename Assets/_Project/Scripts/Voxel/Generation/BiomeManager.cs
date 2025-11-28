using UnityEngine;

namespace VoxelRPG.Voxel.Generation
{
    /// <summary>
    /// Manages biome selection based on world position.
    /// Uses noise to determine which biome to use at each location.
    /// </summary>
    public class BiomeManager : MonoBehaviour
    {
        [Header("Biomes")]
        [Tooltip("Available biomes for generation")]
        [SerializeField] private BiomeDefinition[] _biomes;

        [Header("Biome Selection")]
        [Tooltip("Scale of biome noise (smaller = larger biome regions)")]
        [SerializeField] private float _biomeScale = 0.005f;

        [Tooltip("Seed offset for biome noise")]
        [SerializeField] private float _seedOffset;

        private void Awake()
        {
            if (_seedOffset == 0)
            {
                _seedOffset = Random.Range(0f, 10000f);
            }
        }

        /// <summary>
        /// Gets the biome at the specified world position.
        /// </summary>
        /// <param name="x">World X coordinate.</param>
        /// <param name="z">World Z coordinate.</param>
        /// <returns>The biome at that position.</returns>
        public BiomeDefinition GetBiomeAt(int x, int z)
        {
            if (_biomes == null || _biomes.Length == 0)
            {
                Debug.LogWarning("[BiomeManager] No biomes configured!");
                return null;
            }

            // For single biome, just return it
            if (_biomes.Length == 1)
            {
                return _biomes[0];
            }

            // Use noise to select biome
            float noise = Mathf.PerlinNoise(
                (x + _seedOffset) * _biomeScale,
                (z + _seedOffset) * _biomeScale
            );

            // Map noise to biome index
            int biomeIndex = Mathf.FloorToInt(noise * _biomes.Length);
            biomeIndex = Mathf.Clamp(biomeIndex, 0, _biomes.Length - 1);

            return _biomes[biomeIndex];
        }

        /// <summary>
        /// Sets the biomes available for generation.
        /// </summary>
        /// <param name="biomes">Array of biome definitions.</param>
        public void SetBiomes(BiomeDefinition[] biomes)
        {
            _biomes = biomes;
        }

        /// <summary>
        /// Gets all configured biomes.
        /// </summary>
        public BiomeDefinition[] GetAllBiomes()
        {
            return _biomes;
        }

        /// <summary>
        /// Sets the seed offset for biome noise.
        /// </summary>
        /// <param name="seed">Seed value.</param>
        public void SetSeed(int seed)
        {
            _seedOffset = seed * 0.1f;
        }
    }
}
