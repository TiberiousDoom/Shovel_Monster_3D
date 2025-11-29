using System.Collections.Generic;
using UnityEngine;

namespace VoxelRPG.Voxel
{
    /// <summary>
    /// Greedy mesh builder that merges coplanar faces to reduce vertex count.
    /// Phase 0B optimization - significantly reduces draw calls and improves performance.
    /// </summary>
    public class GreedyMeshBuilder : IChunkMeshBuilder
    {
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<int> _triangles = new List<int>();
        private readonly List<Vector3> _normals = new List<Vector3>();
        private readonly List<Color> _colors = new List<Color>();
        private readonly List<Vector2> _uvs = new List<Vector2>();

        // Reusable mask for greedy meshing
        private readonly int[] _mask = new int[VoxelChunk.SIZE * VoxelChunk.SIZE];
        private readonly BlockType[] _maskBlocks = new BlockType[VoxelChunk.SIZE * VoxelChunk.SIZE];

        /// <inheritdoc/>
        public Mesh BuildMesh(VoxelChunk chunk)
        {
            _vertices.Clear();
            _triangles.Clear();
            _normals.Clear();
            _colors.Clear();
            _uvs.Clear();

            // Process each axis (X, Y, Z) in both directions
            GreedySweep(chunk, 0); // X axis (right/left faces)
            GreedySweep(chunk, 1); // Y axis (top/bottom faces)
            GreedySweep(chunk, 2); // Z axis (front/back faces)

            var mesh = new Mesh
            {
                name = $"Chunk_{chunk.ChunkPosition}_Greedy"
            };

            // Use 32-bit indices if needed for large meshes
            if (_vertices.Count > 65535)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            mesh.SetVertices(_vertices);
            mesh.SetTriangles(_triangles, 0);
            mesh.SetNormals(_normals);
            mesh.SetColors(_colors);
            mesh.SetUVs(0, _uvs);

            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Performs greedy meshing sweep along one axis.
        /// </summary>
        private void GreedySweep(VoxelChunk chunk, int axis)
        {
            // Determine the two other axes
            int axis1 = (axis + 1) % 3;
            int axis2 = (axis + 2) % 3;

            int[] pos = new int[3];
            int[] dir = new int[3];

            // Size along each axis
            int axisSize = VoxelChunk.SIZE;
            int axis1Size = VoxelChunk.SIZE;
            int axis2Size = VoxelChunk.SIZE;

            dir[axis] = 1;

            // Sweep through each slice perpendicular to the axis
            for (pos[axis] = -1; pos[axis] < axisSize;)
            {
                // Build the mask for this slice
                int maskIndex = 0;

                for (pos[axis2] = 0; pos[axis2] < axis2Size; pos[axis2]++)
                {
                    for (pos[axis1] = 0; pos[axis1] < axis1Size; pos[axis1]++)
                    {
                        // Get blocks on either side of the face
                        var blockA = GetBlockAt(chunk, pos[0], pos[1], pos[2]);
                        var blockB = GetBlockAt(chunk,
                            pos[0] + dir[0],
                            pos[1] + dir[1],
                            pos[2] + dir[2]);

                        bool solidA = blockA != null && blockA.IsSolid && !blockA.IsTransparent;
                        bool solidB = blockB != null && blockB.IsSolid && !blockB.IsTransparent;

                        // Determine if we need a face here
                        if (solidA == solidB)
                        {
                            // Both solid or both empty - no face needed
                            _mask[maskIndex] = 0;
                            _maskBlocks[maskIndex] = null;
                        }
                        else if (solidA)
                        {
                            // Face pointing in positive direction
                            _mask[maskIndex] = 1;
                            _maskBlocks[maskIndex] = blockA;
                        }
                        else
                        {
                            // Face pointing in negative direction
                            _mask[maskIndex] = -1;
                            _maskBlocks[maskIndex] = blockB;
                        }

                        maskIndex++;
                    }
                }

                pos[axis]++;

                // Generate mesh from mask using greedy algorithm
                maskIndex = 0;

                for (int j = 0; j < axis2Size; j++)
                {
                    for (int i = 0; i < axis1Size;)
                    {
                        int maskValue = _mask[maskIndex];

                        if (maskValue != 0)
                        {
                            var currentBlock = _maskBlocks[maskIndex];

                            // Calculate width (extend along axis1)
                            int width = 1;
                            while (i + width < axis1Size &&
                                   _mask[maskIndex + width] == maskValue &&
                                   CanMergeBlocks(currentBlock, _maskBlocks[maskIndex + width]))
                            {
                                width++;
                            }

                            // Calculate height (extend along axis2)
                            int height = 1;
                            bool done = false;

                            while (j + height < axis2Size && !done)
                            {
                                for (int k = 0; k < width; k++)
                                {
                                    int checkIndex = maskIndex + k + height * axis1Size;
                                    if (_mask[checkIndex] != maskValue ||
                                        !CanMergeBlocks(currentBlock, _maskBlocks[checkIndex]))
                                    {
                                        done = true;
                                        break;
                                    }
                                }

                                if (!done)
                                {
                                    height++;
                                }
                            }

                            // Create the quad
                            int[] x = new int[3];
                            x[axis] = pos[axis];
                            x[axis1] = i;
                            x[axis2] = j;

                            int[] du = new int[3];
                            int[] dv = new int[3];
                            du[axis1] = width;
                            dv[axis2] = height;

                            AddQuad(x, du, dv, axis, maskValue > 0, currentBlock);

                            // Clear the mask for the merged region
                            for (int l = 0; l < height; l++)
                            {
                                for (int k = 0; k < width; k++)
                                {
                                    _mask[maskIndex + k + l * axis1Size] = 0;
                                }
                            }

                            i += width;
                            maskIndex += width;
                        }
                        else
                        {
                            i++;
                            maskIndex++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets block at position, handling chunk boundaries via world reference.
        /// </summary>
        private BlockType GetBlockAt(VoxelChunk chunk, int x, int y, int z)
        {
            // Check if within chunk bounds
            if (x >= 0 && x < VoxelChunk.SIZE &&
                y >= 0 && y < VoxelChunk.SIZE &&
                z >= 0 && z < VoxelChunk.SIZE)
            {
                return chunk.GetBlockLocal(x, y, z);
            }

            // Get from neighboring chunk via world
            if (chunk.World != null)
            {
                var worldPos = chunk.WorldOrigin + new Vector3Int(x, y, z);
                return chunk.World.GetBlock(worldPos);
            }

            // No world reference, assume air at chunk boundaries
            return BlockType.Air;
        }

        /// <summary>
        /// Checks if two blocks can be merged into the same quad.
        /// </summary>
        private bool CanMergeBlocks(BlockType a, BlockType b)
        {
            if (a == null || b == null)
            {
                return a == b;
            }

            // Merge if same block type (same color/texture)
            return a.Id == b.Id;
        }

        /// <summary>
        /// Adds a quad to the mesh.
        /// </summary>
        private void AddQuad(int[] pos, int[] du, int[] dv, int axis, bool backFace, BlockType block)
        {
            int vertexStart = _vertices.Count;

            // Calculate the four corners
            Vector3 v0 = new Vector3(pos[0], pos[1], pos[2]);
            Vector3 v1 = new Vector3(pos[0] + du[0], pos[1] + du[1], pos[2] + du[2]);
            Vector3 v2 = new Vector3(pos[0] + du[0] + dv[0], pos[1] + du[1] + dv[1], pos[2] + du[2] + dv[2]);
            Vector3 v3 = new Vector3(pos[0] + dv[0], pos[1] + dv[1], pos[2] + dv[2]);

            // Calculate normal
            Vector3 normal = Vector3.zero;
            normal[axis] = backFace ? 1 : -1;

            // Get block color
            Color color = block?.Color ?? Color.magenta;

            // Calculate UV size based on quad dimensions
            float uSize = Mathf.Sqrt(du[0] * du[0] + du[1] * du[1] + du[2] * du[2]);
            float vSize = Mathf.Sqrt(dv[0] * dv[0] + dv[1] * dv[1] + dv[2] * dv[2]);

            if (backFace)
            {
                _vertices.Add(v0);
                _vertices.Add(v1);
                _vertices.Add(v2);
                _vertices.Add(v3);

                _uvs.Add(new Vector2(0, 0));
                _uvs.Add(new Vector2(uSize, 0));
                _uvs.Add(new Vector2(uSize, vSize));
                _uvs.Add(new Vector2(0, vSize));
            }
            else
            {
                _vertices.Add(v3);
                _vertices.Add(v2);
                _vertices.Add(v1);
                _vertices.Add(v0);

                _uvs.Add(new Vector2(0, vSize));
                _uvs.Add(new Vector2(uSize, vSize));
                _uvs.Add(new Vector2(uSize, 0));
                _uvs.Add(new Vector2(0, 0));
            }

            // Add normals and colors for all 4 vertices
            for (int i = 0; i < 4; i++)
            {
                _normals.Add(normal);
                _colors.Add(color);
            }

            // Add triangles
            _triangles.Add(vertexStart);
            _triangles.Add(vertexStart + 1);
            _triangles.Add(vertexStart + 2);

            _triangles.Add(vertexStart);
            _triangles.Add(vertexStart + 2);
            _triangles.Add(vertexStart + 3);
        }
    }
}
