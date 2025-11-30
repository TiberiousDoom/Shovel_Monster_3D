using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VoxelRPG.Core.Registry;

namespace VoxelRPG.Core.Items
{
    /// <summary>
    /// Registry for item definitions.
    /// ScriptableObject-based for easy configuration in the editor.
    /// Allows lookup by ID for save/load and content management.
    /// </summary>
    [CreateAssetMenu(menuName = "VoxelRPG/Items/Item Registry", fileName = "ItemRegistry")]
    public class ItemRegistry : ScriptableObject, IContentRegistry<ItemDefinition>
    {
        [Header("Items")]
        [SerializeField] private ItemDefinition[] _items;

        private Dictionary<string, ItemDefinition> _lookup;
        private bool _isInitialized;

        /// <summary>
        /// Number of items in the registry.
        /// </summary>
        public int Count => _items?.Length ?? 0;

        /// <summary>
        /// Initializes the registry lookup dictionary.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            if (_items == null || _items.Length == 0)
            {
                _lookup = new Dictionary<string, ItemDefinition>();
                Debug.LogWarning("[ItemRegistry] No items configured");
            }
            else
            {
                _lookup = _items
                    .Where(item => item != null)
                    .ToDictionary(item => item.Id, item => item);

                Debug.Log($"[ItemRegistry] Initialized with {_lookup.Count} items");
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Gets an item by its unique identifier.
        /// </summary>
        public ItemDefinition Get(string id)
        {
            EnsureInitialized();

            if (_lookup.TryGetValue(id, out var item))
            {
                return item;
            }

            throw new KeyNotFoundException(
                $"[ItemRegistry] Item not found: '{id}'. " +
                $"Available: {string.Join(", ", _lookup.Keys)}"
            );
        }

        /// <summary>
        /// Gets all items in the registry.
        /// </summary>
        public IEnumerable<ItemDefinition> GetAll()
        {
            EnsureInitialized();
            return _lookup.Values;
        }

        /// <summary>
        /// Tries to get an item without throwing.
        /// </summary>
        public bool TryGet(string id, out ItemDefinition item)
        {
            EnsureInitialized();
            return _lookup.TryGetValue(id, out item);
        }

        /// <summary>
        /// Checks if an item with the given ID exists.
        /// </summary>
        public bool Contains(string id)
        {
            EnsureInitialized();
            return _lookup.ContainsKey(id);
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
        }

        private void OnEnable()
        {
            // Reset initialization state when loaded
            _isInitialized = false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_items == null) return;

            var ids = new HashSet<string>();
            foreach (var item in _items)
            {
                if (item == null) continue;

                var id = item.Id;
                if (string.IsNullOrEmpty(id))
                {
                    Debug.LogWarning($"[ItemRegistry] Item '{item.name}' has no ID", item);
                    continue;
                }

                if (!ids.Add(id))
                {
                    Debug.LogError($"[ItemRegistry] Duplicate ID: '{id}'", this);
                }
            }
        }
#endif
    }
}
