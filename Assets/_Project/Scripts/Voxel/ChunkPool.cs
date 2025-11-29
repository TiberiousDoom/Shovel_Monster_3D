using System.Collections.Generic;
using UnityEngine;

namespace VoxelRPG.Voxel
{
    /// <summary>
    /// Object pool for VoxelChunk GameObjects to reduce allocation overhead.
    /// Phase 0B optimization for chunk loading/unloading.
    /// </summary>
    public class ChunkPool : MonoBehaviour
    {
        [Header("Pool Settings")]
        [Tooltip("Initial number of chunks to pre-allocate")]
        [SerializeField] private int _initialPoolSize = 16;

        [Tooltip("Maximum pool size (0 = unlimited)")]
        [SerializeField] private int _maxPoolSize = 128;

        [Header("References")]
        [SerializeField] private Material _chunkMaterial;

        private readonly Stack<VoxelChunk> _pool = new Stack<VoxelChunk>();
        private Transform _poolContainer;
        private int _totalCreated;

        /// <summary>
        /// Number of chunks currently available in pool.
        /// </summary>
        public int AvailableCount => _pool.Count;

        /// <summary>
        /// Total chunks created by this pool.
        /// </summary>
        public int TotalCreated => _totalCreated;

        /// <summary>
        /// Material used for chunk rendering.
        /// </summary>
        public Material ChunkMaterial
        {
            get => _chunkMaterial;
            set => _chunkMaterial = value;
        }

        private void Awake()
        {
            // Create container for pooled objects
            var containerObj = new GameObject("ChunkPool_Inactive");
            containerObj.transform.SetParent(transform);
            containerObj.SetActive(false);
            _poolContainer = containerObj.transform;
        }

        private void Start()
        {
            // Pre-allocate initial pool
            PreAllocate(_initialPoolSize);
        }

        /// <summary>
        /// Pre-allocates chunks to avoid runtime allocation spikes.
        /// </summary>
        /// <param name="count">Number of chunks to pre-allocate.</param>
        public void PreAllocate(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (_maxPoolSize > 0 && _pool.Count >= _maxPoolSize)
                {
                    break;
                }

                var chunk = CreateNewChunk();
                chunk.gameObject.SetActive(false);
                chunk.transform.SetParent(_poolContainer);
                _pool.Push(chunk);
            }
        }

        /// <summary>
        /// Gets a chunk from the pool or creates a new one.
        /// </summary>
        /// <param name="parent">Parent transform for the chunk.</param>
        /// <returns>A VoxelChunk ready for initialization.</returns>
        public VoxelChunk Get(Transform parent = null)
        {
            VoxelChunk chunk;

            if (_pool.Count > 0)
            {
                chunk = _pool.Pop();
                chunk.gameObject.SetActive(true);
            }
            else
            {
                chunk = CreateNewChunk();
            }

            if (parent != null)
            {
                chunk.transform.SetParent(parent);
            }

            return chunk;
        }

        /// <summary>
        /// Returns a chunk to the pool for reuse.
        /// </summary>
        /// <param name="chunk">The chunk to return.</param>
        public void Return(VoxelChunk chunk)
        {
            if (chunk == null)
            {
                return;
            }

            // Check pool size limit
            if (_maxPoolSize > 0 && _pool.Count >= _maxPoolSize)
            {
                // Pool is full, destroy the chunk
                Destroy(chunk.gameObject);
                return;
            }

            // Reset chunk state
            chunk.Reset();

            // Clear the mesh
            var meshFilter = chunk.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.mesh != null)
            {
                meshFilter.mesh.Clear();
            }

            var meshCollider = chunk.GetComponent<MeshCollider>();
            if (meshCollider != null)
            {
                meshCollider.sharedMesh = null;
            }

            // Return to pool
            chunk.gameObject.SetActive(false);
            chunk.transform.SetParent(_poolContainer);
            _pool.Push(chunk);
        }

        /// <summary>
        /// Clears the pool and destroys all pooled chunks.
        /// </summary>
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                var chunk = _pool.Pop();
                if (chunk != null)
                {
                    Destroy(chunk.gameObject);
                }
            }
        }

        private VoxelChunk CreateNewChunk()
        {
            var chunkObject = new GameObject("PooledChunk");

            // Add required components
            var meshFilter = chunkObject.AddComponent<MeshFilter>();
            var meshRenderer = chunkObject.AddComponent<MeshRenderer>();
            var meshCollider = chunkObject.AddComponent<MeshCollider>();

            // Set material
            if (_chunkMaterial != null)
            {
                meshRenderer.material = _chunkMaterial;
            }

            // Add VoxelChunk component
            var chunk = chunkObject.AddComponent<VoxelChunk>();

            _totalCreated++;

            return chunk;
        }

        private void OnDestroy()
        {
            Clear();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _initialPoolSize = Mathf.Max(0, _initialPoolSize);
            _maxPoolSize = Mathf.Max(0, _maxPoolSize);
        }
#endif
    }
}
