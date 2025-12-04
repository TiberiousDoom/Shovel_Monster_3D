using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VoxelRPG.Core;
using VoxelRPG.Core.Items;
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

        [Header("Player Collision")]
        [Tooltip("Reference to player's CharacterController for placement collision check")]
        [SerializeField] private CharacterController _characterController;

        [Header("References")]
        [SerializeField] private Camera _playerCamera;

        [Header("Item Drops")]
        [Tooltip("Prefab for spawning dropped items in the world")]
        [SerializeField] private GameObject _itemDropPrefab;

        [Header("Debug")]
        [SerializeField] private bool _showDebugRay;

        private IVoxelWorld _voxelWorld;
        private BlockRegistry _blockRegistry;

        private bool _breakBlockPressed;
        private bool _placeBlockPressed;
        private bool _interactPressed;

        /// <summary>
        /// Raised when player interacts with a crafting station block.
        /// Provides station type and block position.
        /// </summary>
        public event Action<string, Vector3Int> OnCraftingStationInteract;

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

            if (_characterController == null)
            {
                _characterController = GetComponent<CharacterController>();
                if (_characterController == null)
                {
                    _characterController = GetComponentInParent<CharacterController>();
                }
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
            // Scale offset by half of BLOCK_SIZE to move into the correct block
            float halfBlock = VoxelChunk.BLOCK_SIZE * 0.5f;
            Vector3 offset = forPlacement ? normal * halfBlock : -normal * halfBlock;
            Vector3 offsetPoint = hitPoint + offset;

            // Convert from Unity world coordinates to block coordinates
            // by dividing by BLOCK_SIZE
            return new Vector3Int(
                Mathf.FloorToInt(offsetPoint.x / VoxelChunk.BLOCK_SIZE),
                Mathf.FloorToInt(offsetPoint.y / VoxelChunk.BLOCK_SIZE),
                Mathf.FloorToInt(offsetPoint.z / VoxelChunk.BLOCK_SIZE)
            );
        }

        private void HandleInteraction()
        {
            if (_voxelWorld == null)
            {
                return;
            }

            // Don't process interactions when UI is open (cursor unlocked means UI is active)
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                // Clear any queued inputs
                if (_breakBlockPressed || _placeBlockPressed || _interactPressed)
                {
                    Debug.Log($"[BlockInteraction] Blocking input - cursor unlocked. LockState={Cursor.lockState}");
                }
                _breakBlockPressed = false;
                _placeBlockPressed = false;
                _interactPressed = false;
                return;
            }

            if (_breakBlockPressed)
            {
                Debug.Log($"[BlockInteraction] Processing break - cursor IS locked. LockState={Cursor.lockState}");
                TryBreakBlock();
                _breakBlockPressed = false;
            }

            if (_placeBlockPressed)
            {
                TryPlaceBlock();
                _placeBlockPressed = false;
            }

            if (_interactPressed)
            {
                TryInteractWithBlock();
                _interactPressed = false;
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

            // Add dropped item to inventory
            if (currentBlock.DroppedItem != null && currentBlock.DropAmount > 0)
            {
                if (ServiceLocator.TryGet<PlayerInventory>(out var inventory))
                {
                    if (inventory.TryAddItem(currentBlock.DroppedItem, currentBlock.DropAmount))
                    {
                        Debug.Log($"[BlockInteraction] Added {currentBlock.DropAmount}x {currentBlock.DroppedItem.DisplayName} to inventory");
                    }
                    else
                    {
                        Debug.Log("[BlockInteraction] Inventory full - spawning world drop");
                        SpawnItemDrop(currentBlock.DroppedItem, currentBlock.DropAmount, position);
                    }
                }
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

            // Check if block would overlap with player
            if (WouldBlockOverlapPlayer(position))
            {
                Debug.Log("[BlockInteraction] Cannot place block - would overlap with player");
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
        /// Checks if placing a block at the given position would overlap the player.
        /// </summary>
        private bool WouldBlockOverlapPlayer(Vector3Int blockPosition)
        {
            if (_characterController == null) return false;

            // Get the block's bounding box in Unity world coordinates
            // Block positions are in block coords, multiply by BLOCK_SIZE for world coords
            float bs = VoxelChunk.BLOCK_SIZE;
            var blockMin = new Vector3(blockPosition.x * bs, blockPosition.y * bs, blockPosition.z * bs);
            var blockMax = blockMin + Vector3.one * bs;
            var blockBounds = new Bounds((blockMin + blockMax) * 0.5f, Vector3.one * bs);

            // Get player's bounding box from CharacterController
            var playerCenter = _characterController.transform.position + _characterController.center;
            var playerBounds = new Bounds(
                playerCenter,
                new Vector3(
                    _characterController.radius * 2f,
                    _characterController.height,
                    _characterController.radius * 2f
                )
            );

            // Check for intersection
            return blockBounds.Intersects(playerBounds);
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
        /// Called by Input System for interact action (use crafting station, etc.).
        /// </summary>
        public void OnInteract(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                _interactPressed = true;
                Debug.Log($"[BlockInteraction] Interact pressed. HasTarget={HasTarget}, TargetPos={TargetBlockPosition}");
            }
        }

        /// <summary>
        /// Attempts to interact with the targeted block (e.g., open furnace).
        /// </summary>
        private void TryInteractWithBlock()
        {
            if (!TargetBlockPosition.HasValue) return;

            var position = TargetBlockPosition.Value;
            var block = _voxelWorld.GetBlock(position);

            if (block == null || block == BlockType.Air) return;

            // Check if block is a crafting station
            if (block.IsCraftingStation && !string.IsNullOrEmpty(block.StationType))
            {
                Debug.Log($"[BlockInteraction] Interacting with {block.DisplayName} (station: {block.StationType})");
                OnCraftingStationInteract?.Invoke(block.StationType, position);
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

        /// <summary>
        /// Spawns an item drop in the world at the specified block position.
        /// </summary>
        /// <param name="item">The item to drop.</param>
        /// <param name="amount">Amount of items.</param>
        /// <param name="blockPosition">Block position to spawn at.</param>
        private void SpawnItemDrop(ItemDefinition item, int amount, Vector3Int blockPosition)
        {
            if (item == null || amount <= 0) return;

            // Calculate world position (center of block, slightly above)
            Vector3 worldPos = new Vector3(
                (blockPosition.x + 0.5f) * VoxelChunk.BLOCK_SIZE,
                (blockPosition.y + 0.5f) * VoxelChunk.BLOCK_SIZE + 0.25f,
                (blockPosition.z + 0.5f) * VoxelChunk.BLOCK_SIZE
            );

            // Use item's drop prefab if available, otherwise use generic prefab
            GameObject prefabToSpawn = item.DropPrefab != null ? item.DropPrefab : _itemDropPrefab;

            if (prefabToSpawn == null)
            {
                Debug.LogWarning($"[BlockInteraction] Cannot spawn drop for {item.DisplayName} - no drop prefab configured");
                return;
            }

            // Spawn the drop
            GameObject dropObj = Instantiate(prefabToSpawn, worldPos, Quaternion.identity);

            // Initialize the ItemDrop component
            ItemDrop itemDrop = dropObj.GetComponent<ItemDrop>();
            if (itemDrop == null)
            {
                itemDrop = dropObj.AddComponent<ItemDrop>();
            }

            itemDrop.Initialize(new ItemStack(item, amount));
            Debug.Log($"[BlockInteraction] Spawned world drop: {amount}x {item.DisplayName} at {worldPos}");
        }
    }
}
