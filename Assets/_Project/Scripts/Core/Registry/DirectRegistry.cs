using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VoxelRPG.Core.Registry
{
    /// <summary>
    /// Direct registry implementation that loads all items via serialized references.
    /// Use this for < 100 items per type. For larger content sets, swap to AddressableRegistry.
    /// </summary>
    /// <typeparam name="T">The ScriptableObject type managed by this registry.</typeparam>
    public abstract class DirectRegistry<T> : MonoBehaviour, IContentRegistry<T> where T : ScriptableObject
    {
        [Header("Content")]
        [SerializeField] private T[] _items;

        private Dictionary<string, T> _lookup;
        private bool _isInitialized;

        /// <summary>
        /// Number of items in the registry.
        /// </summary>
        public int Count => _items?.Length ?? 0;

        protected virtual void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes the registry lookup dictionary.
        /// Called automatically in Awake, but can be called manually for testing.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            if (_items == null || _items.Length == 0)
            {
                _lookup = new Dictionary<string, T>();
                Debug.LogWarning($"[{GetType().Name}] No items configured");
            }
            else
            {
                _lookup = _items
                    .Where(item => item != null)
                    .ToDictionary(GetId, item => item);

                Debug.Log($"[{GetType().Name}] Initialized with {_lookup.Count} items");
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Gets the unique identifier for an item.
        /// Override this to use a custom ID field.
        /// </summary>
        /// <param name="item">The item to get the ID for.</param>
        /// <returns>The unique identifier.</returns>
        protected virtual string GetId(T item)
        {
            return item.name;
        }

        /// <summary>
        /// Gets an item by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier.</param>
        /// <returns>The item.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if item not found.</exception>
        public T Get(string id)
        {
            EnsureInitialized();

            if (_lookup.TryGetValue(id, out var item))
            {
                return item;
            }

            throw new KeyNotFoundException(
                $"[{GetType().Name}] Item not found: '{id}'. " +
                $"Available: {string.Join(", ", _lookup.Keys)}"
            );
        }

        /// <summary>
        /// Gets all items in the registry.
        /// </summary>
        /// <returns>All registered items.</returns>
        public IEnumerable<T> GetAll()
        {
            EnsureInitialized();
            return _lookup.Values;
        }

        /// <summary>
        /// Tries to get an item without throwing.
        /// </summary>
        /// <param name="id">The unique identifier.</param>
        /// <param name="item">The item if found, null otherwise.</param>
        /// <returns>True if item was found.</returns>
        public bool TryGet(string id, out T item)
        {
            EnsureInitialized();
            return _lookup.TryGetValue(id, out item);
        }

        /// <summary>
        /// Checks if an item with the given ID exists.
        /// </summary>
        /// <param name="id">The unique identifier.</param>
        /// <returns>True if item exists.</returns>
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

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only: Validates all items have unique IDs.
        /// </summary>
        protected virtual void OnValidate()
        {
            if (_items == null)
            {
                return;
            }

            var ids = new HashSet<string>();
            foreach (var item in _items)
            {
                if (item == null)
                {
                    continue;
                }

                var id = GetId(item);
                if (!ids.Add(id))
                {
                    Debug.LogError($"[{GetType().Name}] Duplicate ID: '{id}'", this);
                }
            }
        }
#endif
    }
}
