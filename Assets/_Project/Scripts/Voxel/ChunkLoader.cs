using System.Collections.Generic;
using UnityEngine;
using VoxelRPG.Core;

namespace VoxelRPG.Voxel
{
    /// <summary>
    /// Manages distance-based chunk loading and unloading around a target transform.
    /// Phase 0B implementation for dynamic world streaming.
    /// </summary>
    public class ChunkLoader : MonoBehaviour
    {
        [Header("Loading Settings")]
        [Tooltip("Distance in chunks to load around the target")]
        [SerializeField] private int _loadDistance = 4;

        [Tooltip("Distance in chunks before unloading (should be > loadDistance)")]
        [SerializeField] private int _unloadDistance = 6;

        [Tooltip("Maximum chunks to load per frame")]
        [SerializeField] private int _chunksPerFrame = 2;

        [Tooltip("Maximum chunks to generate meshes for per frame")]
        [SerializeField] private int _meshesPerFrame = 4;

        [Header("References")]
        [Tooltip("Transform to load chunks around (usually player)")]
        [SerializeField] private Transform _target;

        private VoxelWorld _world;
        private Vector3Int _lastTargetChunk;
        private bool _initialized;

        private readonly Queue<Vector3Int> _loadQueue = new Queue<Vector3Int>();
        private readonly Queue<VoxelChunk> _meshQueue = new Queue<VoxelChunk>();
        private readonly HashSet<Vector3Int> _queuedPositions = new HashSet<Vector3Int>();

        /// <summary>
        /// Current load distance in chunks.
        /// </summary>
        public int LoadDistance
        {
            get => _loadDistance;
            set => _loadDistance = Mathf.Max(1, value);
        }

        /// <summary>
        /// Current unload distance in chunks.
        /// </summary>
        public int UnloadDistance
        {
            get => _unloadDistance;
            set => _unloadDistance = Mathf.Max(_loadDistance + 1, value);
        }

        /// <summary>
        /// Number of chunks currently in load queue.
        /// </summary>
        public int LoadQueueCount => _loadQueue.Count;

        /// <summary>
        /// Number of chunks waiting for mesh generation.
        /// </summary>
        public int MeshQueueCount => _meshQueue.Count;

        private void Start()
        {
            if (ServiceLocator.TryGet<VoxelWorld>(out var world))
            {
                _world = world;
                _initialized = true;
            }
            else
            {
                Debug.LogWarning("[ChunkLoader] VoxelWorld not found in ServiceLocator.");
            }

            // Validate distances
            if (_unloadDistance <= _loadDistance)
            {
                _unloadDistance = _loadDistance + 2;
                Debug.LogWarning($"[ChunkLoader] Unload distance adjusted to {_unloadDistance}");
            }
        }

        private void Update()
        {
            if (!_initialized || _world == null || _target == null)
            {
                return;
            }

            var currentChunk = WorldToChunkPosition(_target.position);

            // Check if target moved to a new chunk
            if (currentChunk != _lastTargetChunk)
            {
                _lastTargetChunk = currentChunk;
                UpdateChunkQueues(currentChunk);
            }

            // Process load queue
            ProcessLoadQueue();

            // Process mesh queue
            ProcessMeshQueue();
        }

        /// <summary>
        /// Sets the target transform to load chunks around.
        /// </summary>
        public void SetTarget(Transform target)
        {
            _target = target;
            if (target != null)
            {
                _lastTargetChunk = WorldToChunkPosition(target.position);
                UpdateChunkQueues(_lastTargetChunk);
            }
        }

        /// <summary>
        /// Forces an immediate update of chunk loading.
        /// </summary>
        public void ForceUpdate()
        {
            if (_target != null)
            {
                var currentChunk = WorldToChunkPosition(_target.position);
                UpdateChunkQueues(currentChunk);
            }
        }

        private void UpdateChunkQueues(Vector3Int centerChunk)
        {
            // Find chunks that need loading
            for (int x = -_loadDistance; x <= _loadDistance; x++)
            {
                for (int z = -_loadDistance; z <= _loadDistance; z++)
                {
                    for (int y = 0; y < _world.WorldHeightInChunks; y++)
                    {
                        var chunkPos = new Vector3Int(
                            centerChunk.x + x,
                            y,
                            centerChunk.z + z
                        );

                        // Skip if outside world bounds
                        if (!IsChunkInWorldBounds(chunkPos))
                        {
                            continue;
                        }

                        // Skip if already loaded or queued
                        if (_world.GetChunk(chunkPos) != null || _queuedPositions.Contains(chunkPos))
                        {
                            continue;
                        }

                        // Check if within load distance (circular)
                        float distance = Mathf.Sqrt(x * x + z * z);
                        if (distance <= _loadDistance)
                        {
                            _loadQueue.Enqueue(chunkPos);
                            _queuedPositions.Add(chunkPos);
                        }
                    }
                }
            }

            // Find chunks that need unloading
            var chunksToUnload = new List<Vector3Int>();
            foreach (var chunk in _world.GetAllChunks())
            {
                var pos = chunk.ChunkPosition;
                float dx = pos.x - centerChunk.x;
                float dz = pos.z - centerChunk.z;
                float distance = Mathf.Sqrt(dx * dx + dz * dz);

                if (distance > _unloadDistance)
                {
                    chunksToUnload.Add(pos);
                }
            }

            // Unload distant chunks
            foreach (var pos in chunksToUnload)
            {
                _world.UnloadChunk(pos);
            }
        }

        private void ProcessLoadQueue()
        {
            int processed = 0;

            while (_loadQueue.Count > 0 && processed < _chunksPerFrame)
            {
                var chunkPos = _loadQueue.Dequeue();
                _queuedPositions.Remove(chunkPos);

                // Verify still in range before loading
                if (_target != null)
                {
                    var targetChunk = WorldToChunkPosition(_target.position);
                    float dx = chunkPos.x - targetChunk.x;
                    float dz = chunkPos.z - targetChunk.z;
                    float distance = Mathf.Sqrt(dx * dx + dz * dz);

                    if (distance > _loadDistance)
                    {
                        continue;
                    }
                }

                var chunk = _world.LoadOrCreateChunk(chunkPos);
                if (chunk != null)
                {
                    _meshQueue.Enqueue(chunk);
                }

                processed++;
            }
        }

        private void ProcessMeshQueue()
        {
            int processed = 0;

            while (_meshQueue.Count > 0 && processed < _meshesPerFrame)
            {
                var chunk = _meshQueue.Dequeue();

                if (chunk != null && chunk.IsDirty)
                {
                    _world.RebuildChunkMesh(chunk);
                }

                processed++;
            }
        }

        private Vector3Int WorldToChunkPosition(Vector3 worldPosition)
        {
            return new Vector3Int(
                Mathf.FloorToInt(worldPosition.x / VoxelChunk.SIZE),
                Mathf.FloorToInt(worldPosition.y / VoxelChunk.SIZE),
                Mathf.FloorToInt(worldPosition.z / VoxelChunk.SIZE)
            );
        }

        private bool IsChunkInWorldBounds(Vector3Int chunkPos)
        {
            return chunkPos.x >= 0 && chunkPos.x < _world.WorldSizeInChunks &&
                   chunkPos.y >= 0 && chunkPos.y < _world.WorldHeightInChunks &&
                   chunkPos.z >= 0 && chunkPos.z < _world.WorldSizeInChunks;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _loadDistance = Mathf.Max(1, _loadDistance);
            _unloadDistance = Mathf.Max(_loadDistance + 1, _unloadDistance);
            _chunksPerFrame = Mathf.Max(1, _chunksPerFrame);
            _meshesPerFrame = Mathf.Max(1, _meshesPerFrame);
        }

        private void OnDrawGizmosSelected()
        {
            if (_target == null)
            {
                return;
            }

            var center = _target.position;
            center.y = 0;

            // Draw load distance
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireCube(center,
                new Vector3(_loadDistance * 2 * VoxelChunk.SIZE, VoxelChunk.SIZE, _loadDistance * 2 * VoxelChunk.SIZE));

            // Draw unload distance
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawWireCube(center,
                new Vector3(_unloadDistance * 2 * VoxelChunk.SIZE, VoxelChunk.SIZE, _unloadDistance * 2 * VoxelChunk.SIZE));
        }
#endif
    }
}
