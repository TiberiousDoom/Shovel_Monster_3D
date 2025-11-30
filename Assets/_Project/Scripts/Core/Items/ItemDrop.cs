using System;
using UnityEngine;

namespace VoxelRPG.Core.Items
{
    /// <summary>
    /// World entity representing a dropped item stack.
    /// Can be picked up by players.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ItemDrop : MonoBehaviour
    {
        [Header("Item Data")]
        [SerializeField] private ItemDefinition _item;
        [SerializeField] private int _amount = 1;

        [Header("Pickup Settings")]
        [Tooltip("Delay before item can be picked up (prevents instant re-pickup)")]
        [SerializeField] private float _pickupDelay = 0.5f;

        [Tooltip("Time before item despawns (0 = never)")]
        [SerializeField] private float _despawnTime = 300f;

        [Header("Visual")]
        [SerializeField] private bool _bob = true;
        [SerializeField] private float _bobSpeed = 2f;
        [SerializeField] private float _bobHeight = 0.2f;
        [SerializeField] private bool _rotate = true;
        [SerializeField] private float _rotateSpeed = 90f;

        private float _spawnTime;
        private Vector3 _startPosition;
        private bool _isInitialized;

        /// <summary>
        /// The item stack this drop represents.
        /// </summary>
        public ItemStack Stack => new ItemStack(_item, _amount);

        /// <summary>
        /// Whether this item can currently be picked up.
        /// </summary>
        public bool CanPickup => Time.time >= _spawnTime + _pickupDelay && _item != null && _amount > 0;

        /// <summary>
        /// The item definition.
        /// </summary>
        public ItemDefinition Item => _item;

        /// <summary>
        /// The amount of items.
        /// </summary>
        public int Amount => _amount;

        /// <summary>
        /// Raised when the item is picked up.
        /// </summary>
        public event Action OnPickup;

        private void Awake()
        {
            _spawnTime = Time.time;
            _startPosition = transform.position;
        }

        private void Start()
        {
            if (!_isInitialized)
            {
                Initialize(new ItemStack(_item, _amount));
            }
        }

        private void Update()
        {
            HandleVisualEffects();
            HandleDespawn();
        }

        /// <summary>
        /// Initializes this item drop with the specified stack.
        /// </summary>
        /// <param name="stack">The item stack to represent.</param>
        public void Initialize(ItemStack stack)
        {
            _item = stack.Item;
            _amount = stack.Amount;
            _spawnTime = Time.time;
            _startPosition = transform.position;
            _isInitialized = true;

            // Update visual representation if needed
            UpdateVisual();
        }

        /// <summary>
        /// Called when the item is picked up.
        /// Destroys the game object.
        /// </summary>
        public void OnPickedUp()
        {
            OnPickup?.Invoke();
            Destroy(gameObject);
        }

        /// <summary>
        /// Attempts to merge another item drop into this one.
        /// </summary>
        /// <param name="other">The other item drop.</param>
        /// <returns>True if merged successfully.</returns>
        public bool TryMergeWith(ItemDrop other)
        {
            if (other == null || other._item != _item) return false;
            if (_item == null || !_item.IsStackable) return false;

            int spaceRemaining = _item.MaxStackSize - _amount;
            if (spaceRemaining <= 0) return false;

            int toMerge = Mathf.Min(spaceRemaining, other._amount);
            _amount += toMerge;
            other._amount -= toMerge;

            if (other._amount <= 0)
            {
                Destroy(other.gameObject);
            }

            return true;
        }

        /// <summary>
        /// Splits off a portion of this item drop.
        /// </summary>
        /// <param name="splitAmount">Amount to split off.</param>
        /// <returns>The new item drop, or null if can't split.</returns>
        public ItemDrop Split(int splitAmount)
        {
            if (splitAmount <= 0 || splitAmount >= _amount) return null;
            if (_item == null || _item.DropPrefab == null) return null;

            _amount -= splitAmount;

            var newDrop = Instantiate(_item.DropPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            var newItemDrop = newDrop.GetComponent<ItemDrop>();

            if (newItemDrop != null)
            {
                newItemDrop.Initialize(new ItemStack(_item, splitAmount));
            }

            return newItemDrop;
        }

        private void HandleVisualEffects()
        {
            if (_bob)
            {
                float bobOffset = Mathf.Sin(Time.time * _bobSpeed) * _bobHeight;
                transform.position = new Vector3(
                    transform.position.x,
                    _startPosition.y + bobOffset,
                    transform.position.z
                );
            }

            if (_rotate)
            {
                transform.Rotate(Vector3.up, _rotateSpeed * Time.deltaTime);
            }
        }

        private void HandleDespawn()
        {
            if (_despawnTime > 0 && Time.time >= _spawnTime + _despawnTime)
            {
                Destroy(gameObject);
            }
        }

        private void UpdateVisual()
        {
            // Update mesh/sprite based on item
            // This would be expanded based on your visual system
            if (_item != null && _item.Icon != null)
            {
                var spriteRenderer = GetComponentInChildren<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = _item.Icon;
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Optional: Auto-merge with other item drops
            var otherDrop = other.GetComponent<ItemDrop>();
            if (otherDrop != null && otherDrop._item == _item)
            {
                TryMergeWith(otherDrop);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _amount = Mathf.Max(1, _amount);
            _pickupDelay = Mathf.Max(0, _pickupDelay);
            _despawnTime = Mathf.Max(0, _despawnTime);
        }

        private void OnDrawGizmosSelected()
        {
            // Show pickup status
            Gizmos.color = CanPickup ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
#endif
    }
}
