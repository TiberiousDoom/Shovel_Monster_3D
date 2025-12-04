using System.Collections.Generic;
using UnityEngine;

namespace VoxelRPG.Voxel
{
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

        // Face vertices in CLOCKWISE order when viewed from OUTSIDE the block
        // This ensures proper front-face rendering with Unity's backface culling
        private static readonly Vector3[][] FaceVertices =
        {
            // +X face (right) - viewer at +X looking toward -X
            // CW from outside: bottom-right → top-right → top-left → bottom-left
            new[]
            {
                new Vector3(1, 0, 0), new Vector3(1, 1, 0),
                new Vector3(1, 1, 1), new Vector3(1, 0, 1)
            },
            // -X face (left) - viewer at -X looking toward +X
            // CW from outside: bottom-left → top-left → top-right → bottom-right
            new[]
            {
                new Vector3(0, 0, 1), new Vector3(0, 1, 1),
                new Vector3(0, 1, 0), new Vector3(0, 0, 0)
            },
            // +Y face (top) - viewer above looking down
            // CW from outside: front-left → front-right → back-right → back-left
            new[]
            {
                new Vector3(0, 1, 1), new Vector3(1, 1, 1),
                new Vector3(1, 1, 0), new Vector3(0, 1, 0)
            },
            // -Y face (bottom) - viewer below looking up
            // CW from outside: front-left → back-left → back-right → front-right
            new[]
            {
                new Vector3(0, 0, 1), new Vector3(0, 0, 0),
                new Vector3(1, 0, 0), new Vector3(1, 0, 1)
            },
            // +Z face (front) - viewer at +Z looking toward -Z
            // CW from outside: bottom-left → top-left → top-right → bottom-right
            new[]
            {
                new Vector3(1, 0, 1), new Vector3(1, 1, 1),
                new Vector3(0, 1, 1), new Vector3(0, 0, 1)
            },
            // -Z face (back) - viewer at -Z looking toward +Z
            // CW from outside: bottom-right → top-right → top-left → bottom-left
            new[]
            {
                new Vector3(0, 0, 0), new Vector3(0, 1, 0),
                new Vector3(1, 1, 0), new Vector3(1, 0, 0)
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

            // Note: Empty meshes are normal for:
            // - Air chunks above terrain (solidBlocks == 0)
            // - Fully enclosed underground chunks (all neighbors are solid)

            return mesh;
        }

        private void AddBlockFaces(VoxelChunk chunk, int x, int y, int z, BlockType block)
        {
            var blockPos = new Vector3(x, y, z);

            for (int face = 0; face < 6; face++)
            {
                var neighborPos = new Vector3Int(x, y, z) + FaceDirections[face];

                // Check if neighbor blocks this face
                if (IsNeighborSolidAndOpaque(chunk, neighborPos))
                {
                    continue;
                }

                AddFace(blockPos, face, block.Color);
            }
        }

        /// <summary>
        /// Checks if a neighbor position contains a solid, opaque block that would hide a face.
        /// Returns true if the face should NOT be rendered (neighbor is solid and opaque).
        /// </summary>
        private bool IsNeighborSolidAndOpaque(VoxelChunk chunk, Vector3Int localPos)
        {
            // Check if within chunk bounds
            if (chunk.IsLocalPositionValid(localPos.x, localPos.y, localPos.z))
            {
                var neighbor = chunk.GetBlockLocal(localPos);
                return neighbor != null && neighbor.IsSolid && !neighbor.IsTransparent;
            }

            // Get from neighboring chunk via world
            if (chunk.World != null)
            {
                var worldPos = chunk.WorldOrigin + localPos;

                // Check if position is outside world bounds
                if (!chunk.World.IsPositionValid(worldPos))
                {
                    // Positions below Y=0 are treated as solid (don't render bottom faces)
                    // This prevents seeing through the bottom of the world
                    if (worldPos.y < 0)
                    {
                        return true; // Virtual solid - hide this face
                    }
                    // At horizontal world edges or above - treat as air (show face)
                    return false;
                }

                var neighbor = chunk.World.GetBlock(worldPos);
                return neighbor != null && neighbor.IsSolid && !neighbor.IsTransparent;
            }

            // No world reference, assume air at chunk boundaries (show face)
            return false;
        }

        private void AddFace(Vector3 blockPos, int faceIndex, Color color)
        {
            int vertexStart = _vertices.Count;

            var faceVerts = FaceVertices[faceIndex];
            var normal = FaceNormals[faceIndex];

            // Scale by BLOCK_SIZE for smaller/larger blocks
            float blockSize = VoxelChunk.BLOCK_SIZE;

            for (int i = 0; i < 4; i++)
            {
                _vertices.Add((blockPos + faceVerts[i]) * blockSize);
                _normals.Add(normal);
                _colors.Add(color);
            }

            // Add triangles (two triangles per face)
            // Vertices are in CW order when viewed from outside, triangles: 0-1-2, 0-2-3
            _triangles.Add(vertexStart);
            _triangles.Add(vertexStart + 1);
            _triangles.Add(vertexStart + 2);

            _triangles.Add(vertexStart);
            _triangles.Add(vertexStart + 2);
            _triangles.Add(vertexStart + 3);
        }
    }
}
