using System;
using System.Collections.Generic;
using UnityEngine;
using VoxelRPG.Core;
using VoxelRPG.Voxel.Generation;

namespace VoxelRPG.Voxel
{
    /// <summary>
    /// Main voxel world implementation.
    /// Manages chunks and handles block operations.
    /// Single-player implementation - requests are applied immediately.
    /// </summary>
    public class VoxelWorld : MonoBehaviour, IVoxelWorld
    {
        [Header("World Settings")]
        [SerializeField] private int _worldSizeInChunks = 4;
        [SerializeField] private int _worldHeightInChunks = 2;

        [Header("Generation")]
        [Tooltip("Enable procedural world generation using WorldGenerator")]
        [SerializeField] private bool _useWorldGenerator;

        [Header("References")]
        [SerializeField] private Material _chunkMaterial;

        private Dictionary<Vector3Int, VoxelChunk> _chunks;
        private IChunkMeshBuilder _meshBuilder;
        private IWorldGenerator _worldGenerator;
        private bool _isInitialized;

        /// <summary>
        /// World size in chunks for X and Z dimensions.
        /// </summary>
        public int WorldSizeInChunks => _worldSizeInChunks;

        /// <summary>
        /// World height in chunks for Y dimension.
        /// </summary>
        public int WorldHeightInChunks => _worldHeightInChunks;

        /// <summary>
        /// World size in blocks for X and Z dimensions.
        /// </summary>
        public int WorldSizeInBlocks => _worldSizeInChunks * VoxelChunk.SIZE;

        /// <summary>
        /// World height in blocks for Y dimension.
        /// </summary>
        public int WorldHeightInBlocks => _worldHeightInChunks * VoxelChunk.SIZE;

        /// <summary>
        /// Whether the world uses procedural generation.
        /// </summary>
        public bool UseWorldGenerator => _useWorldGenerator;

        /// <inheritdoc/>
        public event Action<Vector3Int, BlockType, BlockType> OnBlockChanged;

        private void Awake()
        {
            _chunks = new Dictionary<Vector3Int, VoxelChunk>();
            _meshBuilder = new NaiveChunkMeshBuilder();

            // Register with ServiceLocator
            ServiceLocator.Register<IVoxelWorld>(this);
            ServiceLocator.Register<VoxelWorld>(this);
        }

        private void Start()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IVoxelWorld>();
            ServiceLocator.Unregister<VoxelWorld>();
        }

        /// <summary>
        /// Initializes the world by creating all chunks.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            CreateChunks();
            _isInitialized = true;
        }

        /// <summary>
        /// Sets the world generator for procedural terrain.
        /// </summary>
        /// <param name="generator">The world generator to use.</param>
        public void SetWorldGenerator(IWorldGenerator generator)
        {
            _worldGenerator = generator;
        }

        /// <summary>
        /// Sets the material used for chunk rendering.
        /// </summary>
        /// <param name="material">The material to apply to all chunks.</param>
        public void SetChunkMaterial(Material material)
        {
            _chunkMaterial = material;

            // Apply to existing chunks
            foreach (var chunk in _chunks.Values)
            {
                var renderer = chunk.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.material = material;
                }
            }
        }

        /// <summary>
        /// Generates terrain using the assigned world generator.
        /// </summary>
        public void GenerateWithWorldGenerator()
        {
            if (_worldGenerator == null)
            {
                Debug.LogWarning("[VoxelWorld] No world generator assigned.");
                return;
            }

            Debug.Log("[VoxelWorld] Generating terrain with world generator...");

            foreach (var chunk in _chunks.Values)
            {
                _worldGenerator.GenerateChunk(chunk);
            }

            RebuildAllChunks();
            Debug.Log("[VoxelWorld] Terrain generation complete.");
        }

        private void CreateChunks()
        {
            for (int x = 0; x < _worldSizeInChunks; x++)
            {
                for (int y = 0; y < _worldHeightInChunks; y++)
                {
                    for (int z = 0; z < _worldSizeInChunks; z++)
                    {
                        CreateChunk(new Vector3Int(x, y, z));
                    }
                }
            }
        }

        private VoxelChunk CreateChunk(Vector3Int chunkPosition)
        {
            var chunkObject = new GameObject($"Chunk_{chunkPosition.x}_{chunkPosition.y}_{chunkPosition.z}");
            chunkObject.transform.SetParent(transform);

            var chunk = chunkObject.AddComponent<VoxelChunk>();
            chunk.Initialize(chunkPosition, this);

            // Set material
            var renderer = chunkObject.GetComponent<MeshRenderer>();
            if (renderer != null && _chunkMaterial != null)
            {
                renderer.material = _chunkMaterial;
            }

            // Subscribe to block changes
            chunk.OnBlockChanged += HandleChunkBlockChanged;

            _chunks[chunkPosition] = chunk;

            return chunk;
        }

        private void HandleChunkBlockChanged(Vector3Int worldPosition, BlockType oldBlock, BlockType newBlock)
        {
            OnBlockChanged?.Invoke(worldPosition, oldBlock, newBlock);

            // Mark neighboring chunks as dirty if the block is on a chunk boundary
            MarkNeighboringChunksDirtyIfNeeded(worldPosition);
        }

        private void MarkNeighboringChunksDirtyIfNeeded(Vector3Int worldPosition)
        {
            var chunkPos = VoxelChunk.WorldToChunkPosition(worldPosition);
            var localPos = VoxelChunk.WorldToLocal(worldPosition, chunkPos);

            // Check each face to see if we're on a chunk boundary
            if (localPos.x == 0)
            {
                MarkChunkDirty(chunkPos + Vector3Int.left);
            }
            else if (localPos.x == VoxelChunk.SIZE - 1)
            {
                MarkChunkDirty(chunkPos + Vector3Int.right);
            }

            if (localPos.y == 0)
            {
                MarkChunkDirty(chunkPos + Vector3Int.down);
            }
            else if (localPos.y == VoxelChunk.SIZE - 1)
            {
                MarkChunkDirty(chunkPos + Vector3Int.up);
            }

            if (localPos.z == 0)
            {
                MarkChunkDirty(chunkPos + Vector3Int.back);
            }
            else if (localPos.z == VoxelChunk.SIZE - 1)
            {
                MarkChunkDirty(chunkPos + Vector3Int.forward);
            }
        }

        private void MarkChunkDirty(Vector3Int chunkPosition)
        {
            if (_chunks.TryGetValue(chunkPosition, out var chunk))
            {
                chunk.MarkDirty();
            }
        }

        /// <inheritdoc/>
        public BlockType GetBlock(Vector3Int worldPosition)
        {
            var chunk = GetChunkAt(worldPosition);
            if (chunk == null)
            {
                return BlockType.Air;
            }

            var localPos = VoxelChunk.WorldToLocal(worldPosition, chunk.ChunkPosition);
            return chunk.GetBlockLocal(localPos);
        }

        /// <inheritdoc/>
        public bool RequestBlockChange(Vector3Int worldPosition, BlockType blockType)
        {
            // Single-player implementation: apply immediately
            var chunk = GetChunkAt(worldPosition);
            if (chunk == null)
            {
                return false;
            }

            var localPos = VoxelChunk.WorldToLocal(worldPosition, chunk.ChunkPosition);
            return chunk.SetBlockLocal(localPos, blockType);
        }

        /// <inheritdoc/>
        public bool IsPositionValid(Vector3Int worldPosition)
        {
            return worldPosition.x >= 0 && worldPosition.x < WorldSizeInBlocks &&
                   worldPosition.y >= 0 && worldPosition.y < WorldHeightInBlocks &&
                   worldPosition.z >= 0 && worldPosition.z < WorldSizeInBlocks;
        }

        /// <inheritdoc/>
        public VoxelChunk GetChunkAt(Vector3Int worldPosition)
        {
            var chunkPosition = VoxelChunk.WorldToChunkPosition(worldPosition);
            return GetChunk(chunkPosition);
        }

        /// <summary>
        /// Gets a chunk by its chunk coordinates.
        /// </summary>
        /// <param name="chunkPosition">Position in chunk coordinates.</param>
        /// <returns>The chunk, or null if not loaded.</returns>
        public VoxelChunk GetChunk(Vector3Int chunkPosition)
        {
            _chunks.TryGetValue(chunkPosition, out var chunk);
            return chunk;
        }

        /// <summary>
        /// Rebuilds all dirty chunk meshes.
        /// Call this after making batch block changes.
        /// </summary>
        public void RebuildDirtyChunks()
        {
            foreach (var chunk in _chunks.Values)
            {
                chunk.RebuildMeshIfDirty(_meshBuilder);
            }
        }

        /// <summary>
        /// Forces a rebuild of all chunk meshes.
        /// </summary>
        public void RebuildAllChunks()
        {
            foreach (var chunk in _chunks.Values)
            {
                chunk.RebuildMesh(_meshBuilder);
            }
        }

        /// <summary>
        /// Generates a simple flat terrain for testing.
        /// </summary>
        /// <param name="groundHeight">Height of the ground in blocks.</param>
        /// <param name="groundBlock">Block type to use for ground.</param>
        public void GenerateFlatTerrain(int groundHeight, BlockType groundBlock)
        {
            if (groundBlock == null)
            {
                Debug.LogWarning("[VoxelWorld] Cannot generate terrain with null block type.");
                return;
            }

            for (int x = 0; x < WorldSizeInBlocks; x++)
            {
                for (int z = 0; z < WorldSizeInBlocks; z++)
                {
                    for (int y = 0; y < groundHeight && y < WorldHeightInBlocks; y++)
                    {
                        var position = new Vector3Int(x, y, z);
                        RequestBlockChange(position, groundBlock);
                    }
                }
            }

            RebuildDirtyChunks();
        }

        /// <summary>
        /// Gets all loaded chunks.
        /// </summary>
        public IEnumerable<VoxelChunk> GetAllChunks()
        {
            return _chunks.Values;
        }

        /// <summary>
        /// Unloads a chunk at the specified position.
        /// </summary>
        /// <param name="chunkPosition">Position in chunk coordinates.</param>
        public void UnloadChunk(Vector3Int chunkPosition)
        {
            if (_chunks.TryGetValue(chunkPosition, out var chunk))
            {
                chunk.OnBlockChanged -= HandleChunkBlockChanged;
                _chunks.Remove(chunkPosition);
                Destroy(chunk.gameObject);
            }
        }

        /// <summary>
        /// Loads or creates a chunk at the specified position.
        /// </summary>
        /// <param name="chunkPosition">Position in chunk coordinates.</param>
        /// <returns>The loaded or created chunk.</returns>
        public VoxelChunk LoadOrCreateChunk(Vector3Int chunkPosition)
        {
            if (_chunks.TryGetValue(chunkPosition, out var existingChunk))
            {
                return existingChunk;
            }

            return CreateChunk(chunkPosition);
        }

        /// <summary>
        /// Rebuilds the mesh for a specific chunk.
        /// </summary>
        /// <param name="chunk">The chunk to rebuild.</param>
        public void RebuildChunkMesh(VoxelChunk chunk)
        {
            if (chunk != null)
            {
                chunk.RebuildMesh(_meshBuilder);
            }
        }
    }
}
