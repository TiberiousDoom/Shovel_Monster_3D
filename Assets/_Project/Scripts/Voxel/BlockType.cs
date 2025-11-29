using UnityEngine;

namespace VoxelRPG.Voxel
{
    /// <summary>
    /// Defines the properties of a block type.
    /// ScriptableObject for data-driven block configuration.
    /// </summary>
    [CreateAssetMenu(menuName = "VoxelRPG/Blocks/Block Type", fileName = "NewBlockType")]
    public class BlockType : ScriptableObject
    {
        [Header("Identification")]
        [Tooltip("Unique identifier for this block type")]
        [SerializeField] private string _id;

        [Tooltip("Display name shown to players")]
        [SerializeField] private string _displayName;

        [Header("Visual")]
        [Tooltip("Color used for this block (Phase 0A - materials come later)")]
        [SerializeField] private Color _color = Color.white;

        [Tooltip("Material for rendering (optional - uses color if null)")]
        [SerializeField] private Material _material;

        [Header("Physical Properties")]
        [Tooltip("Whether the block is solid (has collision)")]
        [SerializeField] private bool _isSolid = true;

        [Tooltip("Whether light passes through this block")]
        [SerializeField] private bool _isTransparent;

        [Header("Interaction")]
        [Tooltip("Time multiplier for mining (1.0 = normal, 2.0 = twice as long)")]
        [SerializeField] private float _hardness = 1f;

        [Tooltip("Whether this block can be placed by players")]
        [SerializeField] private bool _isPlaceable = true;

        /// <summary>
        /// Unique identifier for this block type.
        /// </summary>
        public string Id => _id;

        /// <summary>
        /// Display name shown to players.
        /// </summary>
        public string DisplayName => _displayName;

        /// <summary>
        /// Color used for rendering.
        /// </summary>
        public Color Color => _color;

        /// <summary>
        /// Material for rendering (may be null).
        /// </summary>
        public Material Material => _material;

        /// <summary>
        /// Whether the block is solid (has collision).
        /// </summary>
        public virtual bool IsSolid => _isSolid;

        /// <summary>
        /// Whether light passes through this block.
        /// </summary>
        public virtual bool IsTransparent => _isTransparent;

        /// <summary>
        /// Mining time multiplier.
        /// </summary>
        public float Hardness => _hardness;

        /// <summary>
        /// Whether this block can be placed by players.
        /// </summary>
        public bool IsPlaceable => _isPlaceable;

        /// <summary>
        /// Static reference to air (null/empty) block type.
        /// Set by BlockRegistry during initialization.
        /// </summary>
        public static BlockType Air { get; internal set; }

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

            // Clamp hardness
            _hardness = Mathf.Max(0.1f, _hardness);
        }
#endif
    }
}
