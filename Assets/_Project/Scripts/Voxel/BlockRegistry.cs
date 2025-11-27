using UnityEngine;
using VoxelRPG.Core;
using VoxelRPG.Core.Registry;

namespace VoxelRPG.Voxel
{
    /// <summary>
    /// Registry for all block types in the game.
    /// Inherits from DirectRegistry for simple serialized reference loading.
    /// </summary>
    public class BlockRegistry : DirectRegistry<BlockType>
    {
        [Header("Special Blocks")]
        [Tooltip("The air/empty block type (must be first in list or explicitly set)")]
        [SerializeField] private BlockType _airBlock;

        /// <summary>
        /// The air/empty block type.
        /// </summary>
        public BlockType AirBlock => _airBlock;

        protected override void Awake()
        {
            base.Awake();

            // Register with ServiceLocator
            ServiceLocator.Register<IContentRegistry<BlockType>>(this);
            ServiceLocator.Register<BlockRegistry>(this);

            // Set static Air reference for convenience
            if (_airBlock != null)
            {
                BlockType.Air = _airBlock;
            }
            else
            {
                Debug.LogWarning("[BlockRegistry] No air block configured!");
            }
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IContentRegistry<BlockType>>();
            ServiceLocator.Unregister<BlockRegistry>();
        }

        /// <summary>
        /// Gets the unique identifier for a block type.
        /// </summary>
        protected override string GetId(BlockType item)
        {
            return item.Id;
        }

        /// <summary>
        /// Gets a block by ID, returning air if not found.
        /// Safer than Get() for world queries.
        /// </summary>
        /// <param name="id">The block ID.</param>
        /// <returns>The block type, or air if not found.</returns>
        public BlockType GetOrAir(string id)
        {
            if (TryGet(id, out var block))
            {
                return block;
            }

            return _airBlock;
        }
    }
}
