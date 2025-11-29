using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using VoxelRPG.Core;
using VoxelRPG.Player;
using VoxelRPG.Voxel;
using VoxelRPG.Voxel.Generation;

namespace VoxelRPG.Bootstrap
{
    /// <summary>
    /// Bootstraps a minimal game scene for testing.
    /// Attach to an empty GameObject in the scene.
    /// Creates world, player, and generates test terrain.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("World Settings")]
        [SerializeField] private Material _chunkMaterial;

        [Header("Generation Mode")]
        [Tooltip("Use procedural world generation instead of flat terrain")]
        [SerializeField] private bool _useWorldGenerator;

        [Header("World Generator Settings")]
        [Tooltip("Seed for world generation (0 = random)")]
        [SerializeField] private int _worldSeed;

        [Tooltip("Default biome for generation")]
        [SerializeField] private BiomeDefinition _defaultBiome;

        [Header("Flat Terrain (Legacy)")]
        [SerializeField] private int _groundHeight = 4;
        [SerializeField] private BlockType _groundBlock;

        [Header("Player Settings")]
        [Tooltip("X and Z position for spawn. Y is calculated from terrain if Auto Spawn Height is enabled.")]
        [SerializeField] private Vector3 _playerSpawnPosition = new Vector3(32f, 50f, 32f);

        [Tooltip("Automatically place player on top of terrain at spawn X,Z position")]
        [SerializeField] private bool _autoSpawnHeight = true;

        [Header("Prefabs (Optional)")]
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private GameObject _gameManagerPrefab;

        [Header("Input")]
        [Tooltip("Input Actions asset for player controls")]
        [SerializeField] private InputActionAsset _inputActions;

        private WorldGenerator _worldGenerator;

        private void Start()
        {
            SetupGameManager();
            SetupWorld();
            SetupLighting();
            // Player setup is done after terrain generation in the coroutine
        }

        private void SetupGameManager()
        {
            // Check if GameManager already exists
            if (FindFirstObjectByType<GameManager>() != null)
            {
                return;
            }

            if (_gameManagerPrefab != null)
            {
                Instantiate(_gameManagerPrefab);
            }
            else
            {
                var gmObject = new GameObject("GameManager");
                gmObject.AddComponent<GameManager>();
            }
        }

        private void SetupWorld()
        {
            // Verify BlockRegistry is initialized before creating world
            var blockRegistry = FindFirstObjectByType<BlockRegistry>();
            if (blockRegistry == null)
            {
                Debug.LogError("[GameBootstrap] BlockRegistry not found in scene! World generation will fail.");
                return;
            }

            if (BlockType.Air == null)
            {
                Debug.LogError("[GameBootstrap] BlockType.Air is null! BlockRegistry may not have initialized properly.");
                return;
            }

            Debug.Log($"[GameBootstrap] BlockRegistry found with {blockRegistry.GetAll().Count()} blocks. Air block: {BlockType.Air?.DisplayName ?? "NULL"}");

            // Check if VoxelWorld already exists
            if (FindFirstObjectByType<VoxelWorld>() != null)
            {
                return;
            }

            var worldObject = new GameObject("VoxelWorld");
            var world = worldObject.AddComponent<VoxelWorld>();

            // Configure world settings via reflection or serialized field access
            // For now, the world uses its serialized defaults

            // Generate terrain after world initializes
            StartCoroutine(GenerateTerrainDelayed(world));
        }

        private System.Collections.IEnumerator GenerateTerrainDelayed(VoxelWorld world)
        {
            // Wait one frame for world initialization
            yield return null;

            // Apply material to chunks
            if (_chunkMaterial != null)
            {
                world.SetChunkMaterial(_chunkMaterial);
            }

            if (_useWorldGenerator)
            {
                GenerateProceduralTerrain(world);
            }
            else
            {
                GenerateFlatTerrain(world);
            }

            // Setup player after terrain is generated
            SetupPlayer();

            Debug.Log("[GameBootstrap] Scene setup complete.");
        }

        private void GenerateProceduralTerrain(VoxelWorld world)
        {
            if (_defaultBiome == null)
            {
                Debug.LogWarning("[GameBootstrap] No default biome assigned. Cannot generate procedural terrain.");
                return;
            }

            // Create WorldGenerator
            var generatorObject = new GameObject("WorldGenerator");
            _worldGenerator = generatorObject.AddComponent<WorldGenerator>();

            // Configure the generator
            _worldGenerator.SetDefaultBiome(_defaultBiome);
            if (_worldSeed != 0)
            {
                _worldGenerator.SetSeed(_worldSeed);
            }

            // Initialize and generate
            world.SetWorldGenerator(_worldGenerator);
            _worldGenerator.SetWorld(world);
            _worldGenerator.GenerateWorld();

            Debug.Log("[GameBootstrap] Generated procedural terrain.");
        }

        private void GenerateFlatTerrain(VoxelWorld world)
        {
            if (_groundBlock != null)
            {
                world.GenerateFlatTerrain(_groundHeight, _groundBlock);
                Debug.Log($"[GameBootstrap] Generated flat terrain at height {_groundHeight}.");
            }
            else
            {
                Debug.LogWarning("[GameBootstrap] No ground block assigned. Skipping terrain generation.");
            }
        }

        private void SetupPlayer()
        {
            // Check if player already exists
            if (FindFirstObjectByType<PlayerController>() != null)
            {
                return;
            }

            // Calculate spawn position
            var spawnPos = CalculateSpawnPosition();

            if (_playerPrefab != null)
            {
                var player = Instantiate(_playerPrefab, spawnPos, Quaternion.identity);
                player.name = "Player";
            }
            else
            {
                CreateDefaultPlayer(spawnPos);
            }
        }

        private Vector3 CalculateSpawnPosition()
        {
            var spawnPos = _playerSpawnPosition;

            if (_autoSpawnHeight)
            {
                int spawnX = Mathf.RoundToInt(_playerSpawnPosition.x);
                int spawnZ = Mathf.RoundToInt(_playerSpawnPosition.z);

                // Get terrain height from world generator or use flat terrain height
                int terrainHeight;
                if (_useWorldGenerator && _worldGenerator != null)
                {
                    terrainHeight = _worldGenerator.GetSurfaceHeight(spawnX, spawnZ);
                }
                else
                {
                    terrainHeight = _groundHeight;
                }

                // Spawn player 2 blocks above terrain surface
                spawnPos.y = terrainHeight + 2f;

                // Clear area around spawn point to avoid spawning inside trees
                ClearSpawnArea(spawnX, terrainHeight, spawnZ);

                Debug.Log($"[GameBootstrap] Auto spawn height: terrain={terrainHeight}, spawn Y={spawnPos.y}");
            }

            return spawnPos;
        }

        private void ClearSpawnArea(int centerX, int groundY, int centerZ)
        {
            var world = FindFirstObjectByType<VoxelWorld>();
            if (world == null) return;

            // Clear a 3x3x4 area above the ground (player is about 2 blocks tall)
            for (int x = centerX - 1; x <= centerX + 1; x++)
            {
                for (int z = centerZ - 1; z <= centerZ + 1; z++)
                {
                    for (int y = groundY + 1; y <= groundY + 4; y++)
                    {
                        var pos = new Vector3Int(x, y, z);
                        if (world.IsPositionValid(pos))
                        {
                            var block = world.GetBlock(pos);
                            // Only clear non-terrain blocks (leaves, wood from trees)
                            if (block != null && block != BlockType.Air &&
                                block.Id != "grass" && block.Id != "dirt" && block.Id != "stone")
                            {
                                world.RequestBlockChange(pos, BlockType.Air);
                            }
                        }
                    }
                }
            }

            world.RebuildDirtyChunks();
            Debug.Log($"[GameBootstrap] Cleared spawn area around ({centerX}, {groundY}, {centerZ})");
        }

        private void CreateDefaultPlayer(Vector3 spawnPosition)
        {
            // Create player root
            var playerObject = new GameObject("Player");
            playerObject.transform.position = spawnPosition;
            playerObject.layer = LayerMask.NameToLayer("Default");

            // Add CharacterController
            var characterController = playerObject.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.3f;
            characterController.center = new Vector3(0, 0.9f, 0);

            // Add PlayerController
            playerObject.AddComponent<PlayerController>();

            // Create camera holder
            var cameraHolder = new GameObject("CameraHolder");
            cameraHolder.transform.SetParent(playerObject.transform);
            cameraHolder.transform.localPosition = new Vector3(0, 1.6f, 0);

            // Create camera
            var cameraObject = new GameObject("PlayerCamera");
            cameraObject.transform.SetParent(cameraHolder.transform);
            cameraObject.transform.localPosition = Vector3.zero;
            cameraObject.transform.localRotation = Quaternion.identity;

            var camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 500f;
            camera.fieldOfView = 70f;

            // Add audio listener
            cameraObject.AddComponent<AudioListener>();

            // Add PlayerCamera component
            var playerCamera = cameraObject.AddComponent<PlayerCamera>();
            playerCamera.SetPlayerBody(playerObject.transform);

            // Add BlockInteraction
            playerObject.AddComponent<BlockInteraction>();

            // Create ground check point
            var groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(playerObject.transform);
            groundCheck.transform.localPosition = new Vector3(0, 0, 0);

            // Add PlayerInput for input handling
            if (_inputActions != null)
            {
                var playerInput = playerObject.AddComponent<PlayerInput>();
                playerInput.actions = _inputActions;
                playerInput.defaultActionMap = "Player";
                playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

                // Get the player controller and block interaction to wire up input events
                var controller = playerObject.GetComponent<PlayerController>();
                var blockInteraction = playerObject.GetComponent<BlockInteraction>();

                // Subscribe to input events
                playerInput.onActionTriggered += context =>
                {
                    // Debug: Log all triggered actions to verify input is working
                    if (context.action.name == "PrimaryAction" || context.action.name == "SecondaryAction" || context.action.name == "Crouch")
                    {
                        Debug.Log($"[GameBootstrap] Input action triggered: {context.action.name}, phase={context.phase}");
                    }

                    switch (context.action.name)
                    {
                        case "Move":
                            controller.OnMove(context);
                            break;
                        case "Look":
                            playerCamera.OnLook(context);
                            break;
                        case "Jump":
                            controller.OnJump(context);
                            break;
                        case "Sprint":
                            controller.OnSprint(context);
                            break;
                        case "Crouch":
                            controller.OnCrouch(context);
                            break;
                        case "PrimaryAction":
                            blockInteraction.OnPrimaryAction(context);
                            break;
                        case "SecondaryAction":
                            blockInteraction.OnSecondaryAction(context);
                            break;
                    }
                };

                // Ensure the Player action map is enabled
                var playerActionMap = playerInput.actions.FindActionMap("Player");
                if (playerActionMap != null)
                {
                    playerActionMap.Enable();
                    Debug.Log($"[GameBootstrap] Player action map enabled. Actions: {playerActionMap.actions.Count}");
                }
                else
                {
                    Debug.LogWarning("[GameBootstrap] Could not find 'Player' action map in input actions.");
                }

                // Also enable the entire asset to be safe
                _inputActions.Enable();
                Debug.Log("[GameBootstrap] Input actions asset enabled.");
            }
            else
            {
                Debug.LogWarning("[GameBootstrap] No Input Actions assigned. Player will not respond to input.");
            }

            Debug.Log("[GameBootstrap] Created default player.");
        }

        private void SetupLighting()
        {
            // Check if directional light exists
            var existingLight = FindFirstObjectByType<Light>();
            if (existingLight != null && existingLight.type == LightType.Directional)
            {
                return;
            }

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.84f); // Warm sunlight
            light.intensity = 1f;
            light.shadows = LightShadows.Soft;

            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            Debug.Log("[GameBootstrap] Created directional light.");
        }
    }
}
