using System.Collections.Generic;
using UnityEngine;

namespace VoxelRPG.Voxel
{
    /// <summary>
    /// A virtual block type that acts as solid for mesh generation purposes.
    /// Used to prevent rendering faces at world boundaries.
    /// </summary>
    internal class VirtualSolidBlockType : BlockType
    {
        /// <summary>
        /// Always returns true - this virtual block is treated as solid.
        /// </summary>
        public override bool IsSolid => true;

        /// <summary>
        /// Always returns false - this virtual block is opaque.
        /// </summary>
        public override bool IsTransparent => false;
    }

    /// <summary>
    /// Simple mesh builder that creates visible faces for each solid block.
    /// Only renders faces adjacent to transparent/air blocks.
    /// Phase 0A implementation - will be replaced with greedy meshing in Phase 0B.
    /// </summary>
    public class NaiveChunkMeshBuilder : IChunkMeshBuilder
    {
        private static readonly Vector3Int[] FaceDirections =
        {
            Vector3Int.right,   // +X
            Vector3Int.left,    // -X
            Vector3Int.up,      // +Y
            Vector3Int.down,    // -Y
            Vector3Int.forward, // +Z
            Vector3Int.back     // -Z
        };

        private static readonly Vector3[][] FaceVertices =
        {
            // +X face
            new[]
            {
                new Vector3(1, 0, 0), new Vector3(1, 1, 0),
                new Vector3(1, 1, 1), new Vector3(1, 0, 1)
            },
            // -X face
            new[]
            {
                new Vector3(0, 0, 1), new Vector3(0, 1, 1),
                new Vector3(0, 1, 0), new Vector3(0, 0, 0)
            },
            // +Y face
            new[]
            {
                new Vector3(0, 1, 0), new Vector3(0, 1, 1),
                new Vector3(1, 1, 1), new Vector3(1, 1, 0)
            },
            // -Y face
            new[]
            {
                new Vector3(0, 0, 1), new Vector3(0, 0, 0),
                new Vector3(1, 0, 0), new Vector3(1, 0, 1)
            },
            // +Z face
            new[]
            {
                new Vector3(0, 0, 1), new Vector3(0, 1, 1),
                new Vector3(1, 1, 1), new Vector3(1, 0, 1)
            },
            // -Z face
            new[]
            {
                new Vector3(1, 0, 0), new Vector3(1, 1, 0),
                new Vector3(0, 1, 0), new Vector3(0, 0, 0)
            }
        };

        private static readonly Vector3[] FaceNormals =
        {
            Vector3.right,
            Vector3.left,
            Vector3.up,
            Vector3.down,
            Vector3.forward,
            Vector3.back
        };

        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<int> _triangles = new List<int>();
        private readonly List<Vector3> _normals = new List<Vector3>();
        private readonly List<Color> _colors = new List<Color>();

        /// <inheritdoc/>
        public Mesh BuildMesh(VoxelChunk chunk)
        {
            _vertices.Clear();
            _triangles.Clear();
            _normals.Clear();
            _colors.Clear();

            int solidBlocks = 0;

            for (int x = 0; x < VoxelChunk.SIZE; x++)
            {
                for (int y = 0; y < VoxelChunk.SIZE; y++)
                {
                    for (int z = 0; z < VoxelChunk.SIZE; z++)
                    {
                        var block = chunk.GetBlockLocal(x, y, z);

                        if (block == null || !block.IsSolid)
                        {
                            continue;
                        }

                        solidBlocks++;
                        AddBlockFaces(chunk, x, y, z, block);
                    }
                }
            }

            var mesh = new Mesh
            {
                name = $"Chunk_{chunk.ChunkPosition}"
            };

            mesh.SetVertices(_vertices);
            mesh.SetTriangles(_triangles, 0);
            mesh.SetNormals(_normals);
            mesh.SetColors(_colors);

            mesh.RecalculateBounds();

            if (_vertices.Count == 0)
            {
                Debug.LogWarning($"[NaiveChunkMeshBuilder] Chunk {chunk.ChunkPosition} has no visible faces! Solid blocks found: {solidBlocks}");
            }

            return mesh;
        }

        private void AddBlockFaces(VoxelChunk chunk, int x, int y, int z, BlockType block)
        {
            var blockPos = new Vector3(x, y, z);

            for (int face = 0; face < 6; face++)
            {
                var neighborPos = new Vector3Int(x, y, z) + FaceDirections[face];
                var neighbor = GetNeighborBlock(chunk, neighborPos);

                // Only add face if neighbor is transparent or air
                if (neighbor != null && neighbor.IsSolid && !neighbor.IsTransparent)
                {
                    continue;
                }

                AddFace(blockPos, face, block.Color);
            }
        }

        private BlockType GetNeighborBlock(VoxelChunk chunk, Vector3Int localPos)
        {
            // Check if within chunk bounds
            if (chunk.IsLocalPositionValid(localPos.x, localPos.y, localPos.z))
            {
                return chunk.GetBlockLocal(localPos);
            }

            // Get from neighboring chunk via world
            if (chunk.World != null)
            {
                var worldPos = chunk.WorldOrigin + localPos;

                // Check if position is outside world bounds
                if (!chunk.World.IsPositionValid(worldPos))
                {
                    // Return a "virtual solid" for positions outside world bounds
                    // This prevents interior faces from rendering at world edges
                    // Only treat as solid if below the surface (Y < world height - some buffer)
                    // Above world or at horizontal edges, treat as air
                    if (worldPos.y < 0)
                    {
                        // Below world - treat as solid (don't render bottom faces)
                        return _virtualSolidBlock;
                    }
                    // At horizontal world edges or above - treat as air
                    return BlockType.Air;
                }

                return chunk.World.GetBlock(worldPos);
            }

            // No world reference, assume air at chunk boundaries
            return BlockType.Air;
        }

        // Virtual solid block used to prevent rendering interior faces at world boundaries
        private static readonly VirtualSolidBlockType _virtualSolidBlock = new VirtualSolidBlockType();

        private void AddFace(Vector3 blockPos, int faceIndex, Color color)
        {
            int vertexStart = _vertices.Count;

            // Add vertices
            var faceVerts = FaceVertices[faceIndex];
            for (int i = 0; i < 4; i++)
            {
                _vertices.Add(blockPos + faceVerts[i]);
                _normals.Add(FaceNormals[faceIndex]);
                _colors.Add(color);
            }

            // Add triangles (two triangles per face)
            _triangles.Add(vertexStart);
            _triangles.Add(vertexStart + 1);
            _triangles.Add(vertexStart + 2);

            _triangles.Add(vertexStart);
            _triangles.Add(vertexStart + 2);
            _triangles.Add(vertexStart + 3);
        }
    }
}
