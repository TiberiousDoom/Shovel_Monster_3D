using System;
using UnityEngine;

namespace VoxelRPG.Voxel.Generation
{
    /// <summary>
    /// Defines a tree type that can spawn in a biome.
    /// </summary>
    [Serializable]
    public class TreeType
    {
        [Tooltip("Block type used for the tree trunk")]
        public BlockType TrunkBlock;

        [Tooltip("Block type used for leaves")]
        public BlockType LeavesBlock;

        [Tooltip("Minimum trunk height")]
        [Range(3, 10)]
        public int MinTrunkHeight = 4;

        [Tooltip("Maximum trunk height")]
        [Range(3, 15)]
        public int MaxTrunkHeight = 6;

        [Tooltip("Radius of the leaf canopy")]
        [Range(1, 4)]
        public int LeafRadius = 2;
    }

    /// <summary>
    /// Defines the properties of a biome for world generation.
    /// ScriptableObject for data-driven biome configuration.
    /// </summary>
    [CreateAssetMenu(menuName = "VoxelRPG/Biomes/Biome Definition", fileName = "NewBiome")]
    public class BiomeDefinition : ScriptableObject
    {
        [Header("Identification")]
        [Tooltip("Unique identifier for this biome")]
        [SerializeField] private string _id;

        [Tooltip("Display name shown to players")]
        [SerializeField] private string _displayName;

        [Header("Terrain Blocks")]
        [Tooltip("Block type for the top layer (e.g., Grass)")]
        [SerializeField] private BlockType _topBlock;

        [Tooltip("Block type for filler layers below top (e.g., Dirt)")]
        [SerializeField] private BlockType _fillerBlock;

        [Tooltip("Block type for deep stone layer")]
        [SerializeField] private BlockType _stoneBlock;

        [Header("Water & Beach")]
        [Tooltip("Block type for water")]
        [SerializeField] private BlockType _waterBlock;

        [Tooltip("Block type for beaches near water")]
        [SerializeField] private BlockType _beachBlock;

        [Header("Terrain Shape")]
        [Tooltip("Base terrain height before noise")]
        [SerializeField] private int _baseHeight = 32;

        [Tooltip("Maximum height variation from noise")]
        [SerializeField] private int _heightVariation = 16;

        [Tooltip("Depth of filler blocks below top block")]
        [SerializeField] private int _fillerDepth = 3;

        [Tooltip("Height at which stone starts regardless of filler")]
        [SerializeField] private int _stoneStartHeight = 20;

        [Header("Vegetation")]
        [Tooltip("Chance of a tree spawning per surface block (0-1)")]
        [Range(0f, 0.1f)]
        [SerializeField] private float _treeChance = 0.02f;

        [Tooltip("Tree types that can spawn in this biome")]
        [SerializeField] private TreeType[] _treeTypes;

        [Header("Water Settings")]
        [Tooltip("Water level height")]
        [SerializeField] private int _waterLevel = 28;

        [Tooltip("Beach height above water level")]
        [SerializeField] private int _beachHeight = 2;

        // Properties
        public string Id => _id;
        public string DisplayName => _displayName;
        public BlockType TopBlock => _topBlock;
        public BlockType FillerBlock => _fillerBlock;
        public BlockType StoneBlock => _stoneBlock;
        public BlockType WaterBlock => _waterBlock;
        public BlockType BeachBlock => _beachBlock;
        public int BaseHeight => _baseHeight;
        public int HeightVariation => _heightVariation;
        public int FillerDepth => _fillerDepth;
        public int StoneStartHeight => _stoneStartHeight;
        public float TreeChance => _treeChance;
        public TreeType[] TreeTypes => _treeTypes;
        public int WaterLevel => _waterLevel;
        public int BeachHeight => _beachHeight;

        /// <summary>
        /// Gets a random tree type from this biome's available trees.
        /// </summary>
        /// <param name="random">Random instance to use.</param>
        /// <returns>A tree type, or null if no trees defined.</returns>
        public TreeType GetRandomTreeType(System.Random random)
        {
            if (_treeTypes == null || _treeTypes.Length == 0)
            {
                return null;
            }

            return _treeTypes[random.Next(_treeTypes.Length)];
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-generate ID from asset name if not set
            if (string.IsNullOrEmpty(_id))
            {
                _id = name.ToLowerInvariant().Replace(" ", "_");
            }

            // Auto-generate display name if not set
            if (string.IsNullOrEmpty(_displayName))
            {
                _displayName = name;
            }

            // Ensure sensible values
            _baseHeight = Mathf.Max(1, _baseHeight);
            _heightVariation = Mathf.Max(0, _heightVariation);
            _fillerDepth = Mathf.Max(1, _fillerDepth);
            _stoneStartHeight = Mathf.Max(0, _stoneStartHeight);
            _waterLevel = Mathf.Max(0, _waterLevel);
            _beachHeight = Mathf.Max(0, _beachHeight);
        }
#endif
    }
}
