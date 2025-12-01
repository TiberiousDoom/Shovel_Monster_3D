using System;
using UnityEngine;

namespace VoxelRPG.Voxel
{
    /// <summary>
    /// Represents a 16x16x16 chunk of blocks.
    /// Handles block storage, mesh generation, and rendering.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public class VoxelChunk : MonoBehaviour
    {
        /// <summary>
        /// Size of chunk in each dimension (number of blocks).
        /// </summary>
        public const int SIZE = 16;

        /// <summary>
        /// Size of each block in Unity units.
        /// Smaller values = smaller blocks = player feels bigger.
        /// Default: 1.0f, Half-size: 0.5f
        /// </summary>
        public const float BLOCK_SIZE = 0.5f;

        /// <summary>
        /// Total number of blocks in a chunk.
        /// </summary>
        public const int TOTAL_BLOCKS = SIZE * SIZE * SIZE;

        private BlockType[,,] _blocks;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;

        private bool _isDirty;
        private bool _isInitialized;

        /// <summary>
        /// Chunk position in chunk coordinates (not world blocks).
        /// </summary>
        public Vector3Int ChunkPosition { get; private set; }

        /// <summary>
        /// World position of chunk origin (0,0,0 corner) in block coordinates.
        /// </summary>
        public Vector3Int WorldOrigin => ChunkPosition * SIZE;

        /// <summary>
        /// Reference to the parent voxel world.
        /// </summary>
        public VoxelWorld World { get; private set; }

        /// <summary>
        /// Whether the chunk needs mesh rebuilding.
        /// </summary>
        public bool IsDirty => _isDirty;

        /// <summary>
        /// Event raised when a block in this chunk changes.
        /// </summary>
        public event Action<Vector3Int, BlockType, BlockType> OnBlockChanged;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshCollider = GetComponent<MeshCollider>();
        }

        /// <summary>
        /// Initializes the chunk at the specified position.
        /// </summary>
        /// <param name="chunkPosition">Position in chunk coordinates.</param>
        /// <param name="world">Parent voxel world.</param>
        public void Initialize(Vector3Int chunkPosition, VoxelWorld world)
        {
            ChunkPosition = chunkPosition;
            World = world;

            // Initialize block array with air
            _blocks = new BlockType[SIZE, SIZE, SIZE];
            var air = BlockType.Air;

            if (air == null)
            {
                Debug.LogError($"[VoxelChunk] BlockType.Air is NULL during chunk initialization! ChunkPos: {chunkPosition}");
            }

            for (int x = 0; x < SIZE; x++)
            {
                for (int y = 0; y < SIZE; y++)
                {
                    for (int z = 0; z < SIZE; z++)
                    {
                        _blocks[x, y, z] = air;
                    }
                }
            }

            // Position the chunk in world space (scaled by BLOCK_SIZE)
            transform.position = new Vector3(
                chunkPosition.x * SIZE * BLOCK_SIZE,
                chunkPosition.y * SIZE * BLOCK_SIZE,
                chunkPosition.z * SIZE * BLOCK_SIZE
            );

            _isInitialized = true;
            _isDirty = true;
        }

        /// <summary>
        /// Gets a block at local chunk coordinates.
        /// </summary>
        /// <param name="x">Local X (0-15).</param>
        /// <param name="y">Local Y (0-15).</param>
        /// <param name="z">Local Z (0-15).</param>
        /// <returns>The block type, or Air if out of bounds.</returns>
        public BlockType GetBlockLocal(int x, int y, int z)
        {
            if (!IsLocalPositionValid(x, y, z))
            {
                return BlockType.Air;
            }

            return _blocks[x, y, z] ?? BlockType.Air;
        }

        /// <summary>
        /// Gets a block at local chunk coordinates.
        /// </summary>
        /// <param name="localPosition">Local position (0-15 in each axis).</param>
        /// <returns>The block type, or Air if out of bounds.</returns>
        public BlockType GetBlockLocal(Vector3Int localPosition)
        {
            return GetBlockLocal(localPosition.x, localPosition.y, localPosition.z);
        }

        /// <summary>
        /// Sets a block at local chunk coordinates.
        /// </summary>
        /// <param name="x">Local X (0-15).</param>
        /// <param name="y">Local Y (0-15).</param>
        /// <param name="z">Local Z (0-15).</param>
        /// <param name="blockType">The block type to set.</param>
        /// <returns>True if successful.</returns>
        public bool SetBlockLocal(int x, int y, int z, BlockType blockType)
        {
            if (!IsLocalPositionValid(x, y, z))
            {
                return false;
            }

            var oldBlock = _blocks[x, y, z];
            if (oldBlock == blockType)
            {
                return true; // No change needed
            }

            _blocks[x, y, z] = blockType ?? BlockType.Air;
            _isDirty = true;

            // Calculate world position for event
            var worldPos = new Vector3Int(
                WorldOrigin.x + x,
                WorldOrigin.y + y,
                WorldOrigin.z + z
            );

            OnBlockChanged?.Invoke(worldPos, oldBlock, blockType);

            return true;
        }

        /// <summary>
        /// Sets a block at local chunk coordinates.
        /// </summary>
        /// <param name="localPosition">Local position (0-15 in each axis).</param>
        /// <param name="blockType">The block type to set.</param>
        /// <returns>True if successful.</returns>
        public bool SetBlockLocal(Vector3Int localPosition, BlockType blockType)
        {
            return SetBlockLocal(localPosition.x, localPosition.y, localPosition.z, blockType);
        }

        /// <summary>
        /// Checks if local coordinates are within chunk bounds.
        /// </summary>
        public bool IsLocalPositionValid(int x, int y, int z)
        {
            return x >= 0 && x < SIZE &&
                   y >= 0 && y < SIZE &&
                   z >= 0 && z < SIZE;
        }

        /// <summary>
        /// Marks the chunk as needing mesh rebuild.
        /// </summary>
        public void MarkDirty()
        {
            _isDirty = true;
        }

        /// <summary>
        /// Rebuilds the chunk mesh if dirty.
        /// </summary>
        /// <param name="meshBuilder">The mesh builder to use.</param>
        public void RebuildMeshIfDirty(IChunkMeshBuilder meshBuilder)
        {
            if (!_isDirty || !_isInitialized)
            {
                return;
            }

            RebuildMesh(meshBuilder);
        }

        /// <summary>
        /// Forces a mesh rebuild.
        /// </summary>
        /// <param name="meshBuilder">The mesh builder to use.</param>
        public void RebuildMesh(IChunkMeshBuilder meshBuilder)
        {
            if (!_isInitialized)
            {
                return;
            }

            var mesh = meshBuilder.BuildMesh(this);

            _meshFilter.mesh = mesh;
            _meshCollider.sharedMesh = mesh;

            _isDirty = false;
        }

        /// <summary>
        /// Converts world position to local chunk position.
        /// </summary>
        public static Vector3Int WorldToLocal(Vector3Int worldPosition, Vector3Int chunkPosition)
        {
            return new Vector3Int(
                worldPosition.x - (chunkPosition.x * SIZE),
                worldPosition.y - (chunkPosition.y * SIZE),
                worldPosition.z - (chunkPosition.z * SIZE)
            );
        }

        /// <summary>
        /// Gets chunk position from world position.
        /// </summary>
        public static Vector3Int WorldToChunkPosition(Vector3Int worldPosition)
        {
            return new Vector3Int(
                Mathf.FloorToInt(worldPosition.x / (float)SIZE),
                Mathf.FloorToInt(worldPosition.y / (float)SIZE),
                Mathf.FloorToInt(worldPosition.z / (float)SIZE)
            );
        }

        /// <summary>
        /// Gets the raw block array for serialization.
        /// </summary>
        public BlockType[,,] GetBlocksForSerialization()
        {
            return _blocks;
        }

        /// <summary>
        /// Loads blocks from serialization.
        /// </summary>
        public void LoadBlocksFromSerialization(BlockType[,,] blocks)
        {
            if (blocks == null)
            {
                return;
            }

            _blocks = blocks;
            _isDirty = true;
        }

        /// <summary>
        /// Resets the chunk state for pooling/reuse.
        /// Clears all blocks and marks as dirty.
        /// </summary>
        public void Reset()
        {
            // Clear all blocks to air
            var air = BlockType.Air;
            for (int x = 0; x < SIZE; x++)
            {
                for (int y = 0; y < SIZE; y++)
                {
                    for (int z = 0; z < SIZE; z++)
                    {
                        _blocks[x, y, z] = air;
                    }
                }
            }

            ChunkPosition = Vector3Int.zero;
            World = null;
            _isDirty = true;
            _isInitialized = false;
        }
    }
}
