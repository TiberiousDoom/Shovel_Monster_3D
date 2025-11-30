using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelRPG.Core.Items
{
    /// <summary>
    /// Slot-based inventory implementation.
    /// Can be used standalone or as a component.
    /// </summary>
    [Serializable]
    public class Inventory : IInventory
    {
        [SerializeField] private int _slotCount = 20;
        [SerializeField] private ItemStack[] _slots;

        /// <summary>
        /// Number of slots in this inventory.
        /// </summary>
        public int SlotCount => _slotCount;

        /// <summary>
        /// Raised when a slot's contents change.
        /// </summary>
        public event Action<int, ItemStack> OnSlotChanged;

        /// <summary>
        /// Raised when any inventory change occurs.
        /// </summary>
        public event Action OnInventoryChanged;

        /// <summary>
        /// Creates a new inventory with the specified number of slots.
        /// </summary>
        /// <param name="slotCount">Number of slots.</param>
        public Inventory(int slotCount = 20)
        {
            _slotCount = Mathf.Max(1, slotCount);
            _slots = new ItemStack[_slotCount];

            // Initialize all slots as empty
            for (int i = 0; i < _slotCount; i++)
            {
                _slots[i] = ItemStack.Empty;
            }
        }

        /// <summary>
        /// Initializes the inventory (call after deserialization).
        /// </summary>
        public void Initialize()
        {
            if (_slots == null || _slots.Length != _slotCount)
            {
                _slots = new ItemStack[_slotCount];
                for (int i = 0; i < _slotCount; i++)
                {
                    _slots[i] = ItemStack.Empty;
                }
            }
        }

        /// <summary>
        /// Gets the contents of a specific slot.
        /// </summary>
        public ItemStack GetSlot(int slotIndex)
        {
            if (!IsValidSlot(slotIndex)) return ItemStack.Empty;
            return _slots[slotIndex];
        }

        /// <summary>
        /// Attempts to add an item to the inventory.
        /// </summary>
        public bool TryAddItem(ItemDefinition item, int amount)
        {
            if (item == null || amount <= 0) return false;
            return TryAddItem(new ItemStack(item, amount));
        }

        /// <summary>
        /// Attempts to add an item stack to the inventory.
        /// </summary>
        public bool TryAddItem(ItemStack stack)
        {
            if (stack.IsEmpty) return true;

            int remaining = stack.Amount;

            // First, try to stack with existing items
            if (stack.Item.IsStackable)
            {
                for (int i = 0; i < _slotCount && remaining > 0; i++)
                {
                    if (_slots[i].Item == stack.Item && !_slots[i].IsFull)
                    {
                        int canAdd = _slots[i].SpaceRemaining;
                        int toAdd = Mathf.Min(canAdd, remaining);

                        _slots[i] = _slots[i].AddAmount(toAdd);
                        remaining -= toAdd;

                        RaiseSlotChanged(i);
                    }
                }
            }

            // Then, use empty slots
            for (int i = 0; i < _slotCount && remaining > 0; i++)
            {
                if (_slots[i].IsEmpty)
                {
                    int toAdd = Mathf.Min(stack.Item.MaxStackSize, remaining);
                    _slots[i] = new ItemStack(stack.Item, toAdd);
                    remaining -= toAdd;

                    RaiseSlotChanged(i);
                }
            }

            if (remaining < stack.Amount)
            {
                OnInventoryChanged?.Invoke();
            }

            return remaining == 0;
        }

        /// <summary>
        /// Attempts to remove an item from the inventory.
        /// </summary>
        public bool TryRemoveItem(ItemDefinition item, int amount)
        {
            if (item == null || amount <= 0) return false;
            if (!HasItem(item, amount)) return false;

            int remaining = amount;

            for (int i = 0; i < _slotCount && remaining > 0; i++)
            {
                if (_slots[i].Item == item)
                {
                    int toRemove = Mathf.Min(_slots[i].Amount, remaining);
                    int newAmount = _slots[i].Amount - toRemove;

                    _slots[i] = newAmount > 0
                        ? _slots[i].WithAmount(newAmount)
                        : ItemStack.Empty;

                    remaining -= toRemove;
                    RaiseSlotChanged(i);
                }
            }

            OnInventoryChanged?.Invoke();
            return remaining == 0;
        }

        /// <summary>
        /// Attempts to set a specific slot's contents.
        /// </summary>
        public bool TrySetSlot(int slotIndex, ItemStack stack)
        {
            if (!IsValidSlot(slotIndex)) return false;

            _slots[slotIndex] = stack;
            RaiseSlotChanged(slotIndex);
            OnInventoryChanged?.Invoke();

            return true;
        }

        /// <summary>
        /// Checks if the inventory contains at least the specified amount.
        /// </summary>
        public bool HasItem(ItemDefinition item, int amount = 1)
        {
            return GetItemCount(item) >= amount;
        }

        /// <summary>
        /// Gets the total count of a specific item.
        /// </summary>
        public int GetItemCount(ItemDefinition item)
        {
            if (item == null) return 0;

            int count = 0;
            for (int i = 0; i < _slotCount; i++)
            {
                if (_slots[i].Item == item)
                {
                    count += _slots[i].Amount;
                }
            }
            return count;
        }

        /// <summary>
        /// Finds the first slot containing the specified item.
        /// </summary>
        public int FindItem(ItemDefinition item)
        {
            if (item == null) return -1;

            for (int i = 0; i < _slotCount; i++)
            {
                if (_slots[i].Item == item)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Finds the first empty slot.
        /// </summary>
        public int FindEmptySlot()
        {
            for (int i = 0; i < _slotCount; i++)
            {
                if (_slots[i].IsEmpty)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Checks if there's space for the specified items.
        /// </summary>
        public bool HasSpaceFor(ItemDefinition item, int amount)
        {
            if (item == null || amount <= 0) return true;

            int remaining = amount;

            // Check stackable space
            if (item.IsStackable)
            {
                for (int i = 0; i < _slotCount && remaining > 0; i++)
                {
                    if (_slots[i].Item == item)
                    {
                        remaining -= _slots[i].SpaceRemaining;
                    }
                }
            }

            // Check empty slots
            for (int i = 0; i < _slotCount && remaining > 0; i++)
            {
                if (_slots[i].IsEmpty)
                {
                    remaining -= item.MaxStackSize;
                }
            }

            return remaining <= 0;
        }

        /// <summary>
        /// Clears all slots.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < _slotCount; i++)
            {
                if (!_slots[i].IsEmpty)
                {
                    _slots[i] = ItemStack.Empty;
                    RaiseSlotChanged(i);
                }
            }
            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// Swaps two slots.
        /// </summary>
        public bool SwapSlots(int slotA, int slotB)
        {
            if (!IsValidSlot(slotA) || !IsValidSlot(slotB)) return false;
            if (slotA == slotB) return true;

            var temp = _slots[slotA];
            _slots[slotA] = _slots[slotB];
            _slots[slotB] = temp;

            RaiseSlotChanged(slotA);
            RaiseSlotChanged(slotB);
            OnInventoryChanged?.Invoke();

            return true;
        }

        /// <summary>
        /// Gets all non-empty stacks in the inventory.
        /// </summary>
        public IEnumerable<ItemStack> GetAllItems()
        {
            for (int i = 0; i < _slotCount; i++)
            {
                if (!_slots[i].IsEmpty)
                {
                    yield return _slots[i];
                }
            }
        }

        /// <summary>
        /// Counts how many slots are occupied.
        /// </summary>
        public int GetOccupiedSlotCount()
        {
            int count = 0;
            for (int i = 0; i < _slotCount; i++)
            {
                if (!_slots[i].IsEmpty) count++;
            }
            return count;
        }

        /// <summary>
        /// Counts how many slots are empty.
        /// </summary>
        public int GetEmptySlotCount()
        {
            return _slotCount - GetOccupiedSlotCount();
        }

        private bool IsValidSlot(int index)
        {
            return index >= 0 && index < _slotCount;
        }

        private void RaiseSlotChanged(int slotIndex)
        {
            OnSlotChanged?.Invoke(slotIndex, _slots[slotIndex]);
        }

        #region Save/Load Support

        /// <summary>
        /// Gets save data for this inventory.
        /// </summary>
        public InventorySaveData GetSaveData()
        {
            var data = new InventorySaveData
            {
                SlotCount = _slotCount,
                Slots = new List<InventorySlotSaveData>()
            };

            for (int i = 0; i < _slotCount; i++)
            {
                if (!_slots[i].IsEmpty)
                {
                    data.Slots.Add(new InventorySlotSaveData
                    {
                        SlotIndex = i,
                        ItemId = _slots[i].Item.Id,
                        Amount = _slots[i].Amount
                    });
                }
            }

            return data;
        }

        /// <summary>
        /// Loads save data. Requires an item registry to resolve item IDs.
        /// </summary>
        /// <param name="data">The save data to load.</param>
        /// <param name="itemResolver">Function to resolve item IDs to definitions.</param>
        public void LoadSaveData(InventorySaveData data, Func<string, ItemDefinition> itemResolver)
        {
            if (data == null || itemResolver == null) return;

            // Resize if needed
            if (data.SlotCount != _slotCount)
            {
                _slotCount = data.SlotCount;
                _slots = new ItemStack[_slotCount];
            }

            // Clear all slots
            for (int i = 0; i < _slotCount; i++)
            {
                _slots[i] = ItemStack.Empty;
            }

            // Load saved slots
            foreach (var slotData in data.Slots)
            {
                if (slotData.SlotIndex >= 0 && slotData.SlotIndex < _slotCount)
                {
                    var item = itemResolver(slotData.ItemId);
                    if (item != null)
                    {
                        _slots[slotData.SlotIndex] = new ItemStack(item, slotData.Amount);
                    }
                }
            }

            OnInventoryChanged?.Invoke();
        }

        #endregion
    }

    /// <summary>
    /// Serializable save data for an inventory.
    /// </summary>
    [Serializable]
    public class InventorySaveData
    {
        public int SlotCount;
        public List<InventorySlotSaveData> Slots;
    }

    /// <summary>
    /// Serializable save data for a single inventory slot.
    /// </summary>
    [Serializable]
    public class InventorySlotSaveData
    {
        public int SlotIndex;
        public string ItemId;
        public int Amount;
    }
}
