using UnityEngine;

namespace VoxelRPG.Voxel.Generation
{
    /// <summary>
    /// Interface for world generation strategies.
    /// Allows swapping between different generation algorithms.
    /// </summary>
    public interface IWorldGenerator
    {
        /// <summary>
        /// The seed used for deterministic generation.
        /// </summary>
        int Seed { get; }

        /// <summary>
        /// Generates terrain for the specified chunk.
        /// </summary>
        /// <param name="chunk">The chunk to populate with blocks.</param>
        void GenerateChunk(VoxelChunk chunk);

        /// <summary>
        /// Gets the block type at a specific world position.
        /// Used for cross-chunk queries during generation.
        /// </summary>
        /// <param name="worldPosition">World position to query.</param>
        /// <returns>The block type at that position.</returns>
        BlockType GetBlockAt(Vector3Int worldPosition);

        /// <summary>
        /// Gets the surface height at a specific X,Z position.
        /// </summary>
        /// <param name="x">World X coordinate.</param>
        /// <param name="z">World Z coordinate.</param>
        /// <returns>The Y coordinate of the surface.</returns>
        int GetSurfaceHeight(int x, int z);

        /// <summary>
        /// Gets the biome at a specific X,Z position.
        /// </summary>
        /// <param name="x">World X coordinate.</param>
        /// <param name="z">World Z coordinate.</param>
        /// <returns>The biome definition at that position.</returns>
        BiomeDefinition GetBiomeAt(int x, int z);
    }
}
