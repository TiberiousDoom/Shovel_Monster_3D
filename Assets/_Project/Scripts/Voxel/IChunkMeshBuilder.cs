using UnityEngine;

namespace VoxelRPG.Voxel
{
    /// <summary>
    /// Interface for building chunk meshes.
    /// Allows swapping meshing algorithms (naive, greedy, etc.).
    /// </summary>
    public interface IChunkMeshBuilder
    {
        /// <summary>
        /// Builds a mesh for the given chunk.
        /// </summary>
        /// <param name="chunk">The chunk to build a mesh for.</param>
        /// <returns>The generated mesh.</returns>
        Mesh BuildMesh(VoxelChunk chunk);
    }
}
