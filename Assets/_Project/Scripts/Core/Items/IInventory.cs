using System;

namespace VoxelRPG.Core.Items
{
    /// <summary>
    /// Generic container contract for inventory systems.
    /// Multiplayer-ready: all modification methods return success/failure
    /// to allow server validation in networked scenarios.
    /// </summary>
    public interface IInventory
    {
        /// <summary>
        /// Number of slots in this inventory.
        /// </summary>
        int SlotCount { get; }

        /// <summary>
        /// Raised when a slot's contents change.
        /// Parameters: slot index, new stack contents.
        /// </summary>
        event Action<int, ItemStack> OnSlotChanged;

        /// <summary>
        /// Raised when the inventory contents change (any slot).
        /// </summary>
        event Action OnInventoryChanged;

        /// <summary>
        /// Gets the contents of a specific slot.
        /// </summary>
        /// <param name="slotIndex">Index of the slot to query.</param>
        /// <returns>The ItemStack in that slot.</returns>
        ItemStack GetSlot(int slotIndex);

        /// <summary>
        /// Attempts to add an item to the inventory.
        /// Will stack with existing items if possible, then use empty slots.
        /// </summary>
        /// <param name="item">The item type to add.</param>
        /// <param name="amount">The quantity to add.</param>
        /// <returns>True if all items were added, false if some/all couldn't fit.</returns>
        bool TryAddItem(ItemDefinition item, int amount);

        /// <summary>
        /// Attempts to add an item stack to the inventory.
        /// </summary>
        /// <param name="stack">The stack to add.</param>
        /// <returns>True if all items were added.</returns>
        bool TryAddItem(ItemStack stack);

        /// <summary>
        /// Attempts to remove an item from the inventory.
        /// Removes from any slots containing the item.
        /// </summary>
        /// <param name="item">The item type to remove.</param>
        /// <param name="amount">The quantity to remove.</param>
        /// <returns>True if the full amount was removed.</returns>
        bool TryRemoveItem(ItemDefinition item, int amount);

        /// <summary>
        /// Attempts to set a specific slot's contents.
        /// </summary>
        /// <param name="slotIndex">The slot to modify.</param>
        /// <param name="stack">The new contents.</param>
        /// <returns>True if the slot was set successfully.</returns>
        bool TrySetSlot(int slotIndex, ItemStack stack);

        /// <summary>
        /// Checks if the inventory contains at least the specified amount of an item.
        /// </summary>
        /// <param name="item">The item type to check.</param>
        /// <param name="amount">The minimum quantity required.</param>
        /// <returns>True if the inventory has enough.</returns>
        bool HasItem(ItemDefinition item, int amount = 1);

        /// <summary>
        /// Gets the total count of a specific item across all slots.
        /// </summary>
        /// <param name="item">The item type to count.</param>
        /// <returns>Total quantity of that item.</returns>
        int GetItemCount(ItemDefinition item);

        /// <summary>
        /// Gets the index of the first slot containing the specified item.
        /// </summary>
        /// <param name="item">The item type to find.</param>
        /// <returns>Slot index, or -1 if not found.</returns>
        int FindItem(ItemDefinition item);

        /// <summary>
        /// Gets the index of the first empty slot.
        /// </summary>
        /// <returns>Slot index, or -1 if no empty slots.</returns>
        int FindEmptySlot();

        /// <summary>
        /// Checks if the inventory has space for the specified items.
        /// </summary>
        /// <param name="item">The item type.</param>
        /// <param name="amount">The quantity.</param>
        /// <returns>True if there's room.</returns>
        bool HasSpaceFor(ItemDefinition item, int amount);

        /// <summary>
        /// Clears all slots in the inventory.
        /// </summary>
        void Clear();

        /// <summary>
        /// Swaps the contents of two slots.
        /// </summary>
        /// <param name="slotA">First slot index.</param>
        /// <param name="slotB">Second slot index.</param>
        /// <returns>True if swap was successful.</returns>
        bool SwapSlots(int slotA, int slotB);
    }
}
