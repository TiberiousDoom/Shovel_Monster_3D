using System;
using UnityEngine;
using VoxelRPG.Core;
using VoxelRPG.Core.Items;
using VoxelRPG.Core.Events;

namespace VoxelRPG.Player
{
    /// <summary>
    /// Player-specific inventory management.
    /// Adds hotbar support and item pickup/drop functionality.
    /// </summary>
    public class PlayerInventory : MonoBehaviour, IInventory
    {
        [Header("Inventory Settings")]
        [SerializeField] private int _inventorySlots = 20;
        [SerializeField] private int _hotbarSlots = 9;

        [Header("Pickup Settings")]
        [SerializeField] private float _pickupRadius = 2f;
        [SerializeField] private LayerMask _itemDropLayer;
        [SerializeField] private bool _autoPickup = true;

        [Header("Drop Settings")]
        [SerializeField] private Transform _dropPoint;
        [SerializeField] private float _dropForce = 5f;

        [Header("Event Channels")]
        [SerializeField] private VoidEventChannel _onInventoryChanged;

        private Inventory _inventory;
        private int _selectedHotbarSlot;

        #region Properties

        /// <summary>
        /// Total slot count (inventory + hotbar).
        /// </summary>
        public int SlotCount => _inventory.SlotCount;

        /// <summary>
        /// Number of hotbar slots.
        /// </summary>
        public int HotbarSlotCount => _hotbarSlots;

        /// <summary>
        /// Currently selected hotbar slot index.
        /// </summary>
        public int SelectedHotbarSlot
        {
            get => _selectedHotbarSlot;
            set
            {
                int newSlot = Mathf.Clamp(value, 0, _hotbarSlots - 1);
                if (newSlot != _selectedHotbarSlot)
                {
                    _selectedHotbarSlot = newSlot;
                    OnHotbarSelectionChanged?.Invoke(_selectedHotbarSlot);
                }
            }
        }

        /// <summary>
        /// The item stack in the currently selected hotbar slot.
        /// </summary>
        public ItemStack SelectedItem => GetSlot(_selectedHotbarSlot);

        /// <summary>
        /// Whether auto-pickup is enabled.
        /// </summary>
        public bool AutoPickupEnabled
        {
            get => _autoPickup;
            set => _autoPickup = value;
        }

        #endregion

        #region Events

        /// <summary>
        /// Raised when a slot's contents change.
        /// </summary>
        public event Action<int, ItemStack> OnSlotChanged;

        /// <summary>
        /// Raised when the inventory changes.
        /// </summary>
        public event Action OnInventoryChanged;

        /// <summary>
        /// Raised when the selected hotbar slot changes.
        /// </summary>
        public event Action<int> OnHotbarSelectionChanged;

        /// <summary>
        /// Raised when an item is picked up.
        /// </summary>
        public event Action<ItemStack> OnItemPickedUp;

        /// <summary>
        /// Raised when an item is dropped.
        /// </summary>
        public event Action<ItemStack> OnItemDropped;

        #endregion

        private void Awake()
        {
            _inventory = new Inventory(_inventorySlots + _hotbarSlots);

            // Forward events
            _inventory.OnSlotChanged += HandleSlotChanged;
            _inventory.OnInventoryChanged += HandleInventoryChanged;

            ServiceLocator.Register<PlayerInventory>(this);
        }

        private void OnDestroy()
        {
            if (_inventory != null)
            {
                _inventory.OnSlotChanged -= HandleSlotChanged;
                _inventory.OnInventoryChanged -= HandleInventoryChanged;
            }

            ServiceLocator.Unregister<PlayerInventory>();
        }

        private void Update()
        {
            if (_autoPickup)
            {
                TryAutoPickup();
            }
        }

        #region IInventory Implementation

        public ItemStack GetSlot(int slotIndex)
        {
            return _inventory.GetSlot(slotIndex);
        }

        public bool TryAddItem(ItemDefinition item, int amount)
        {
            return _inventory.TryAddItem(item, amount);
        }

        public bool TryAddItem(ItemStack stack)
        {
            return _inventory.TryAddItem(stack);
        }

        public bool TryRemoveItem(ItemDefinition item, int amount)
        {
            return _inventory.TryRemoveItem(item, amount);
        }

        public bool TrySetSlot(int slotIndex, ItemStack stack)
        {
            return _inventory.TrySetSlot(slotIndex, stack);
        }

        public bool HasItem(ItemDefinition item, int amount = 1)
        {
            return _inventory.HasItem(item, amount);
        }

        public int GetItemCount(ItemDefinition item)
        {
            return _inventory.GetItemCount(item);
        }

        public int FindItem(ItemDefinition item)
        {
            return _inventory.FindItem(item);
        }

        public int FindEmptySlot()
        {
            return _inventory.FindEmptySlot();
        }

        public bool HasSpaceFor(ItemDefinition item, int amount)
        {
            return _inventory.HasSpaceFor(item, amount);
        }

        public void Clear()
        {
            _inventory.Clear();
        }

        public bool SwapSlots(int slotA, int slotB)
        {
            return _inventory.SwapSlots(slotA, slotB);
        }

        #endregion

        #region Hotbar Methods

        /// <summary>
        /// Gets a hotbar slot's contents.
        /// </summary>
        /// <param name="hotbarIndex">Index within the hotbar (0 to HotbarSlotCount-1).</param>
        /// <returns>The item stack in that hotbar slot.</returns>
        public ItemStack GetHotbarSlot(int hotbarIndex)
        {
            if (hotbarIndex < 0 || hotbarIndex >= _hotbarSlots) return ItemStack.Empty;
            return GetSlot(hotbarIndex);
        }

        /// <summary>
        /// Checks if a slot index is in the hotbar.
        /// </summary>
        public bool IsHotbarSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < _hotbarSlots;
        }

        /// <summary>
        /// Scrolls the hotbar selection.
        /// </summary>
        /// <param name="direction">Positive for next, negative for previous.</param>
        public void ScrollHotbar(int direction)
        {
            int newSlot = (_selectedHotbarSlot + direction) % _hotbarSlots;
            if (newSlot < 0) newSlot += _hotbarSlots;
            SelectedHotbarSlot = newSlot;
        }

        /// <summary>
        /// Uses/consumes the currently selected item.
        /// </summary>
        /// <returns>True if an item was consumed.</returns>
        public bool UseSelectedItem()
        {
            var selected = SelectedItem;
            if (selected.IsEmpty) return false;

            if (selected.Item.IsConsumable)
            {
                return TryRemoveItem(selected.Item, 1);
            }

            return false;
        }

        #endregion

        #region Pickup & Drop

        /// <summary>
        /// Attempts to pick up a nearby item drop.
        /// </summary>
        public void TryAutoPickup()
        {
            var colliders = Physics.OverlapSphere(transform.position, _pickupRadius, _itemDropLayer);

            foreach (var col in colliders)
            {
                var itemDrop = col.GetComponent<ItemDrop>();
                if (itemDrop != null && itemDrop.CanPickup)
                {
                    TryPickup(itemDrop);
                }
            }
        }

        /// <summary>
        /// Attempts to pick up a specific item drop.
        /// </summary>
        /// <param name="itemDrop">The item drop to pick up.</param>
        /// <returns>True if picked up successfully.</returns>
        public bool TryPickup(ItemDrop itemDrop)
        {
            if (itemDrop == null || !itemDrop.CanPickup) return false;

            var stack = itemDrop.Stack;
            if (TryAddItem(stack))
            {
                OnItemPickedUp?.Invoke(stack);
                itemDrop.OnPickedUp();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Drops the item in the specified slot.
        /// </summary>
        /// <param name="slotIndex">The slot to drop from.</param>
        /// <param name="amount">Amount to drop (0 = entire stack).</param>
        /// <returns>True if item was dropped.</returns>
        public bool DropItem(int slotIndex, int amount = 0)
        {
            var stack = GetSlot(slotIndex);
            if (stack.IsEmpty) return false;

            int toDrop = amount > 0 ? Mathf.Min(amount, stack.Amount) : stack.Amount;
            var dropStack = new ItemStack(stack.Item, toDrop);

            // Remove from inventory
            int remaining = stack.Amount - toDrop;
            TrySetSlot(slotIndex, remaining > 0 ? stack.WithAmount(remaining) : ItemStack.Empty);

            // Spawn world item
            SpawnItemDrop(dropStack);

            OnItemDropped?.Invoke(dropStack);
            return true;
        }

        /// <summary>
        /// Drops the currently selected hotbar item.
        /// </summary>
        /// <param name="amount">Amount to drop (0 = entire stack).</param>
        /// <returns>True if item was dropped.</returns>
        public bool DropSelectedItem(int amount = 0)
        {
            return DropItem(_selectedHotbarSlot, amount);
        }

        private void SpawnItemDrop(ItemStack stack)
        {
            if (stack.IsEmpty || stack.Item.DropPrefab == null) return;

            Vector3 spawnPos = _dropPoint != null ? _dropPoint.position : transform.position + Vector3.up;
            Vector3 dropDirection = _dropPoint != null ? _dropPoint.forward : transform.forward;

            var dropObj = Instantiate(stack.Item.DropPrefab, spawnPos, Quaternion.identity);
            var itemDrop = dropObj.GetComponent<ItemDrop>();

            if (itemDrop != null)
            {
                itemDrop.Initialize(stack);
            }

            // Apply force if has rigidbody
            var rb = dropObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(dropDirection * _dropForce + Vector3.up * 2f, ForceMode.Impulse);
            }
        }

        #endregion

        #region Event Handlers

        private void HandleSlotChanged(int slotIndex, ItemStack stack)
        {
            OnSlotChanged?.Invoke(slotIndex, stack);
        }

        private void HandleInventoryChanged()
        {
            OnInventoryChanged?.Invoke();
            _onInventoryChanged?.RaiseEvent();
        }

        #endregion

        #region Save/Load Support

        /// <summary>
        /// Gets save data for this inventory.
        /// </summary>
        public PlayerInventorySaveData GetSaveData()
        {
            return new PlayerInventorySaveData
            {
                InventoryData = _inventory.GetSaveData(),
                SelectedHotbarSlot = _selectedHotbarSlot
            };
        }

        /// <summary>
        /// Loads save data.
        /// </summary>
        public void LoadSaveData(PlayerInventorySaveData data, Func<string, ItemDefinition> itemResolver)
        {
            if (data == null) return;

            _inventory.LoadSaveData(data.InventoryData, itemResolver);
            _selectedHotbarSlot = Mathf.Clamp(data.SelectedHotbarSlot, 0, _hotbarSlots - 1);

            OnHotbarSelectionChanged?.Invoke(_selectedHotbarSlot);
        }

        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw pickup radius
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _pickupRadius);
        }
#endif
    }

    /// <summary>
    /// Serializable save data for PlayerInventory.
    /// </summary>
    [Serializable]
    public class PlayerInventorySaveData
    {
        public InventorySaveData InventoryData;
        public int SelectedHotbarSlot;
    }
}
