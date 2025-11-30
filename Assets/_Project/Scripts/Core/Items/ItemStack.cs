using System;

namespace VoxelRPG.Core.Items
{
    /// <summary>
    /// Represents a stack of items (item type + quantity).
    /// Immutable struct for safe passing between systems.
    /// </summary>
    [Serializable]
    public struct ItemStack : IEquatable<ItemStack>
    {
        /// <summary>
        /// The item definition for this stack.
        /// </summary>
        public ItemDefinition Item;

        /// <summary>
        /// The quantity of items in this stack.
        /// </summary>
        public int Amount;

        /// <summary>
        /// Creates a new item stack.
        /// </summary>
        /// <param name="item">The item definition.</param>
        /// <param name="amount">The quantity.</param>
        public ItemStack(ItemDefinition item, int amount)
        {
            Item = item;
            Amount = amount;
        }

        /// <summary>
        /// Alias for Amount for API consistency.
        /// </summary>
        public int Quantity => Amount;

        /// <summary>
        /// Whether this stack is empty (no item or zero amount).
        /// </summary>
        public bool IsEmpty => Item == null || Amount <= 0;

        /// <summary>
        /// Whether this stack is at maximum capacity.
        /// </summary>
        public bool IsFull => Item != null && Amount >= Item.MaxStackSize;

        /// <summary>
        /// How many more items can be added to this stack.
        /// </summary>
        public int SpaceRemaining => Item != null ? Item.MaxStackSize - Amount : 0;

        /// <summary>
        /// An empty item stack.
        /// </summary>
        public static ItemStack Empty => new ItemStack(null, 0);

        /// <summary>
        /// Creates a copy of this stack with a different amount.
        /// </summary>
        /// <param name="newAmount">The new amount.</param>
        /// <returns>A new ItemStack with the specified amount.</returns>
        public ItemStack WithAmount(int newAmount)
        {
            return new ItemStack(Item, newAmount);
        }

        /// <summary>
        /// Creates a copy of this stack with amount adjusted by delta.
        /// </summary>
        /// <param name="delta">Amount to add (can be negative).</param>
        /// <returns>A new ItemStack with adjusted amount.</returns>
        public ItemStack AddAmount(int delta)
        {
            return new ItemStack(Item, Amount + delta);
        }

        /// <summary>
        /// Creates a copy with increased quantity.
        /// </summary>
        public ItemStack AddQuantity(int qty)
        {
            return new ItemStack(Item, Amount + qty);
        }

        /// <summary>
        /// Creates a copy with decreased quantity.
        /// </summary>
        public ItemStack RemoveQuantity(int qty)
        {
            int newAmount = Amount - qty;
            return newAmount <= 0 ? Empty : new ItemStack(Item, newAmount);
        }

        /// <summary>
        /// Checks if this stack can accept additional items.
        /// </summary>
        public bool CanStack(int additionalAmount)
        {
            if (Item == null) return true;
            return Item.IsStackable && (Amount + additionalAmount) <= Item.MaxStackSize;
        }

        /// <summary>
        /// Checks if this stack can merge with another stack.
        /// </summary>
        /// <param name="other">The other stack to check.</param>
        /// <returns>True if stacks can merge.</returns>
        public bool CanMergeWith(ItemStack other)
        {
            if (IsEmpty || other.IsEmpty) return true;
            return Item == other.Item && Item.IsStackable;
        }

        /// <summary>
        /// Calculates how many items can be transferred from another stack.
        /// </summary>
        /// <param name="from">The source stack.</param>
        /// <returns>Number of items that can be transferred.</returns>
        public int CanAcceptFrom(ItemStack from)
        {
            if (from.IsEmpty) return 0;
            if (IsEmpty) return from.Amount;
            if (Item != from.Item) return 0;
            return Math.Min(SpaceRemaining, from.Amount);
        }

        public bool Equals(ItemStack other)
        {
            return Item == other.Item && Amount == other.Amount;
        }

        public override bool Equals(object obj)
        {
            return obj is ItemStack other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Item, Amount);
        }

        public static bool operator ==(ItemStack left, ItemStack right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ItemStack left, ItemStack right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            if (IsEmpty) return "Empty";
            return $"{Item.DisplayName} x{Amount}";
        }
    }
}
