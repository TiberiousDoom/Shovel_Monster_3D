using UnityEngine;

namespace VoxelRPG.Core.Items
{
    /// <summary>
    /// Defines the properties of an item type.
    /// ScriptableObject for data-driven item configuration.
    /// </summary>
    [CreateAssetMenu(menuName = "VoxelRPG/Items/Item Definition", fileName = "NewItem")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identification")]
        [Tooltip("Unique identifier for this item type")]
        [SerializeField] private string _id;

        [Tooltip("Display name shown to players")]
        [SerializeField] private string _displayName;

        [Tooltip("Description shown in tooltips")]
        [TextArea(2, 4)]
        [SerializeField] private string _description;

        [Header("Visual")]
        [Tooltip("Icon displayed in inventory UI")]
        [SerializeField] private Sprite _icon;

        [Tooltip("Prefab for world drop representation")]
        [SerializeField] private GameObject _dropPrefab;

        [Tooltip("Prefab used when item is equipped (weapons/tools). Falls back to DropPrefab if not set.")]
        [SerializeField] private GameObject _equippedPrefab;

        [Header("Stacking")]
        [Tooltip("Maximum stack size (1 = non-stackable)")]
        [SerializeField] private int _maxStackSize = 64;

        [Header("Category")]
        [Tooltip("Item category for sorting and filtering")]
        [SerializeField] private ItemCategory _category = ItemCategory.Miscellaneous;

        [Header("Usage")]
        [Tooltip("Whether this item can be consumed/used")]
        [SerializeField] private bool _isConsumable;

        [Tooltip("Whether this item can be equipped")]
        [SerializeField] private bool _isEquippable;

        [Tooltip("Whether this item can be placed as a block")]
        [SerializeField] private bool _isPlaceable;

        [Header("Value")]
        [Tooltip("Base value for trading")]
        [SerializeField] private int _baseValue = 1;

        #region Properties

        /// <summary>
        /// Unique identifier for this item type.
        /// </summary>
        public string Id => _id;

        /// <summary>
        /// Display name shown to players.
        /// </summary>
        public string DisplayName => _displayName;

        /// <summary>
        /// Description shown in tooltips.
        /// </summary>
        public string Description => _description;

        /// <summary>
        /// Icon displayed in inventory UI.
        /// </summary>
        public Sprite Icon => _icon;

        /// <summary>
        /// Prefab for world drop representation.
        /// </summary>
        public GameObject DropPrefab => _dropPrefab;

        /// <summary>
        /// Prefab used when item is equipped. Falls back to DropPrefab if not set.
        /// </summary>
        public GameObject EquippedPrefab => _equippedPrefab != null ? _equippedPrefab : _dropPrefab;

        /// <summary>
        /// Maximum stack size (1 = non-stackable).
        /// </summary>
        public int MaxStackSize => _maxStackSize;

        /// <summary>
        /// Item category for sorting and filtering.
        /// </summary>
        public ItemCategory Category => _category;

        /// <summary>
        /// Whether this item can be consumed/used.
        /// </summary>
        public bool IsConsumable => _isConsumable;

        /// <summary>
        /// Whether this item can be equipped.
        /// </summary>
        public bool IsEquippable => _isEquippable;

        /// <summary>
        /// Whether this item can be placed as a block.
        /// </summary>
        public bool IsPlaceable => _isPlaceable;

        /// <summary>
        /// Whether this item can stack with others of the same type.
        /// </summary>
        public bool IsStackable => _maxStackSize > 1;

        /// <summary>
        /// Base value for trading.
        /// </summary>
        public int BaseValue => _baseValue;

        #endregion

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

            // Clamp stack size
            _maxStackSize = Mathf.Max(1, _maxStackSize);

            // Clamp value
            _baseValue = Mathf.Max(0, _baseValue);
        }
#endif
    }

    /// <summary>
    /// Categories for organizing items.
    /// </summary>
    public enum ItemCategory
    {
        Miscellaneous,
        Resource,
        Tool,
        Weapon,
        Armor,
        Food,
        Building,
        Crafting,
        Quest
    }
}
