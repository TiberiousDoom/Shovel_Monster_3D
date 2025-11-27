using System.Collections.Generic;
using UnityEngine;

namespace VoxelRPG.Core.Registry
{
    /// <summary>
    /// Interface for content registries that manage ScriptableObject assets.
    /// Abstracts over loading strategy - can be swapped from DirectRegistry to AddressableRegistry later.
    /// </summary>
    /// <typeparam name="T">The ScriptableObject type managed by this registry.</typeparam>
    public interface IContentRegistry<T> where T : ScriptableObject
    {
        /// <summary>
        /// Gets an item by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier.</param>
        /// <returns>The item if found.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if item not found.</exception>
        T Get(string id);

        /// <summary>
        /// Gets all items in the registry.
        /// </summary>
        /// <returns>All registered items.</returns>
        IEnumerable<T> GetAll();

        /// <summary>
        /// Tries to get an item without throwing.
        /// </summary>
        /// <param name="id">The unique identifier.</param>
        /// <param name="item">The item if found, null otherwise.</param>
        /// <returns>True if item was found.</returns>
        bool TryGet(string id, out T item);

        /// <summary>
        /// Gets the count of items in the registry.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Checks if an item with the given ID exists.
        /// </summary>
        /// <param name="id">The unique identifier.</param>
        /// <returns>True if item exists.</returns>
        bool Contains(string id);
    }
}
