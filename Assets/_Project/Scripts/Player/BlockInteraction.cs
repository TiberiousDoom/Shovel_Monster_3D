using UnityEngine;
using UnityEngine.InputSystem;
using VoxelRPG.Core;
using VoxelRPG.Voxel;

namespace VoxelRPG.Player
{
    /// <summary>
    /// Handles player block breaking and placing interactions.
    /// Raycasts from camera to detect block targets.
    /// </summary>
    public class BlockInteraction : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private float _interactionRange = 5f;
        [Tooltip("Layer mask for block raycasting. Use Everything (-1) if unset.")]
        [SerializeField] private LayerMask _blockLayerMask = -1;

        [Header("References")]
        [SerializeField] private Camera _playerCamera;

        [Header("Debug")]
        [SerializeField] private bool _showDebugRay;

        private IVoxelWorld _voxelWorld;
        private BlockRegistry _blockRegistry;

        private bool _breakBlockPressed;
        private bool _placeBlockPressed;

        /// <summary>
        /// The currently selected block type for placing.
        /// </summary>
        public BlockType SelectedBlockType { get; set; }

        /// <summary>
        /// Current target block position (if any).
        /// </summary>
        public Vector3Int? TargetBlockPosition { get; private set; }

        /// <summary>
        /// Position adjacent to target block for placing.
        /// </summary>
        public Vector3Int? PlacePosition { get; private set; }

        /// <summary>
        /// Whether the player is currently looking at a block.
        /// </summary>
        public bool HasTarget => TargetBlockPosition.HasValue;

        private void Start()
        {
            _voxelWorld = ServiceLocator.Get<IVoxelWorld>();
            _blockRegistry = ServiceLocator.Get<BlockRegistry>();

            if (_playerCamera == null)
            {
                _playerCamera = Camera.main;
            }

            // Set a default block for placing if registry is available
            if (_blockRegistry != null && SelectedBlockType == null)
            {
                // Try to get a non-air block as default
                foreach (var block in _blockRegistry.GetAll())
                {
                    if (block != null && block != BlockType.Air && block.IsPlaceable)
                    {
                        SelectedBlockType = block;
                        Debug.Log($"[BlockInteraction] Default block set to: {block.DisplayName}");
                        break;
                    }
                }
            }
        }

        private void Update()
        {
            UpdateTargetBlock();
            HandleInteraction();
        }

        private void UpdateTargetBlock()
        {
            TargetBlockPosition = null;
            PlacePosition = null;

            if (_playerCamera == null || _voxelWorld == null)
            {
                return;
            }

            var ray = new Ray(_playerCamera.transform.position, _playerCamera.transform.forward);

            if (_showDebugRay)
            {
                Debug.DrawRay(ray.origin, ray.direction * _interactionRange, Color.yellow);
            }

            if (!Physics.Raycast(ray, out var hit, _interactionRange, _blockLayerMask))
            {
                return;
            }

            // Get the block position from hit point
            // Offset slightly into the block to get correct position
            var blockPos = GetBlockPositionFromHit(hit.point, hit.normal, false);
            var placePos = GetBlockPositionFromHit(hit.point, hit.normal, true);

            if (_voxelWorld.IsPositionValid(blockPos))
            {
                TargetBlockPosition = blockPos;
            }

            if (_voxelWorld.IsPositionValid(placePos))
            {
                PlacePosition = placePos;
            }
        }

        private Vector3Int GetBlockPositionFromHit(Vector3 hitPoint, Vector3 normal, bool forPlacement)
        {
            // Offset into or out of the block based on whether we're breaking or placing
            Vector3 offset = forPlacement ? normal * 0.5f : -normal * 0.5f;
            Vector3 blockCenter = hitPoint + offset;

            return new Vector3Int(
                Mathf.FloorToInt(blockCenter.x),
                Mathf.FloorToInt(blockCenter.y),
                Mathf.FloorToInt(blockCenter.z)
            );
        }

        private void HandleInteraction()
        {
            if (_voxelWorld == null)
            {
                return;
            }

            if (_breakBlockPressed)
            {
                TryBreakBlock();
                _breakBlockPressed = false;
            }

            if (_placeBlockPressed)
            {
                TryPlaceBlock();
                _placeBlockPressed = false;
            }
        }

        private void TryBreakBlock()
        {
            if (!TargetBlockPosition.HasValue)
            {
                return;
            }

            var position = TargetBlockPosition.Value;
            var currentBlock = _voxelWorld.GetBlock(position);

            // Don't break air
            if (currentBlock == null || currentBlock == BlockType.Air)
            {
                return;
            }

            _voxelWorld.RequestBlockChange(position, BlockType.Air);

            // Rebuild meshes after block change
            if (_voxelWorld is VoxelWorld world)
            {
                world.RebuildDirtyChunks();
            }
        }

        private void TryPlaceBlock()
        {
            if (!PlacePosition.HasValue || SelectedBlockType == null)
            {
                return;
            }

            var position = PlacePosition.Value;

            // Check if position is empty
            var existingBlock = _voxelWorld.GetBlock(position);
            if (existingBlock != null && existingBlock != BlockType.Air)
            {
                return;
            }

            _voxelWorld.RequestBlockChange(position, SelectedBlockType);

            // Rebuild meshes after block change
            if (_voxelWorld is VoxelWorld world)
            {
                world.RebuildDirtyChunks();
            }
        }

        /// <summary>
        /// Called by Input System for primary action (break block).
        /// </summary>
        public void OnPrimaryAction(InputAction.CallbackContext context)
        {
            Debug.Log($"[BlockInteraction] OnPrimaryAction called, phase={context.phase}, started={context.started}");
            if (context.started)
            {
                _breakBlockPressed = true;
                Debug.Log($"[BlockInteraction] Break block pressed. HasTarget={HasTarget}, TargetPos={TargetBlockPosition}");
            }
        }

        /// <summary>
        /// Called by Input System for secondary action (place block).
        /// </summary>
        public void OnSecondaryAction(InputAction.CallbackContext context)
        {
            Debug.Log($"[BlockInteraction] OnSecondaryAction called, phase={context.phase}, started={context.started}");
            if (context.started)
            {
                _placeBlockPressed = true;
                Debug.Log($"[BlockInteraction] Place block pressed. PlacePos={PlacePosition}, SelectedBlock={SelectedBlockType?.DisplayName ?? "NULL"}");
            }
        }

        /// <summary>
        /// Sets the selected block type by ID.
        /// </summary>
        /// <param name="blockId">Block ID from registry.</param>
        public void SelectBlock(string blockId)
        {
            if (_blockRegistry != null)
            {
                SelectedBlockType = _blockRegistry.GetOrAir(blockId);
            }
        }
    }
}
