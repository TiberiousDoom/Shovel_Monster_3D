using System;
using UnityEngine;

namespace VoxelRPG.Voxel
{
    /// <summary>
    /// Interface for voxel world operations.
    /// Uses request pattern for multiplayer compatibility - block changes are requested,
    /// not directly set. In single-player, requests are applied immediately.
    /// In multiplayer, requests go to the server for validation.
    /// </summary>
    public interface IVoxelWorld
    {
        /// <summary>
        /// Gets the block at the specified world position.
        /// </summary>
        /// <param name="worldPosition">World position in block coordinates.</param>
        /// <returns>The block type at that position, or Air if out of bounds.</returns>
        BlockType GetBlock(Vector3Int worldPosition);

        /// <summary>
        /// Requests a block change at the specified position.
        /// In single-player: applied immediately.
        /// In multiplayer: sent to server for validation.
        /// </summary>
        /// <param name="worldPosition">World position in block coordinates.</param>
        /// <param name="blockType">The block type to place.</param>
        /// <returns>True if the request was accepted (single-player always true).</returns>
        bool RequestBlockChange(Vector3Int worldPosition, BlockType blockType);

        /// <summary>
        /// Checks if a position is within the world bounds.
        /// </summary>
        /// <param name="worldPosition">World position to check.</param>
        /// <returns>True if position is valid.</returns>
        bool IsPositionValid(Vector3Int worldPosition);

        /// <summary>
        /// Gets the chunk containing the specified world position.
        /// </summary>
        /// <param name="worldPosition">World position in block coordinates.</param>
        /// <returns>The chunk, or null if not loaded.</returns>
        VoxelChunk GetChunkAt(Vector3Int worldPosition);

        /// <summary>
        /// Event raised when a block changes.
        /// Parameters: position, old block, new block.
        /// </summary>
        event Action<Vector3Int, BlockType, BlockType> OnBlockChanged;
    }
}
