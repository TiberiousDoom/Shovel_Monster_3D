using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using VoxelRPG.Combat;
using VoxelRPG.Core;
using VoxelRPG.Core.Items;
using VoxelRPG.Player;
using VoxelRPG.UI;
using VoxelRPG.Voxel;
using VoxelRPG.Voxel.Generation;
using VoxelRPG.World;

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
        [Tooltip("If true, spawn player at the center of the world. Otherwise use manual spawn position.")]
        [SerializeField] private bool _spawnAtWorldCenter = true;

        [Tooltip("Manual spawn position (only used if Spawn At World Center is false). Y is calculated from terrain if Auto Spawn Height is enabled.")]
        [SerializeField] private Vector3 _playerSpawnPosition = new Vector3(32f, 50f, 32f);

        [Tooltip("Automatically place player on top of terrain at spawn X,Z position")]
        [SerializeField] private bool _autoSpawnHeight = true;

        [Header("Prefabs (Optional)")]
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private GameObject _gameManagerPrefab;

        [Header("Input")]
        [Tooltip("Input Actions asset for player controls")]
        [SerializeField] private InputActionAsset _inputActions;

        [Header("Starting Items")]
        [Tooltip("Items to give the player at start")]
        [SerializeField] private ItemDefinition _startingItem;

        [Header("Monster Spawning")]
        [Tooltip("Monster types that can spawn")]
        [SerializeField] private MonsterDefinition[] _monsterTypes;

        private WorldGenerator _worldGenerator;

        private void Start()
        {
            SetupGameManager();
            SetupWorld();
            SetupLighting();
            SetupTimeSystem();
            SetupMonsterSpawner();
            SetupUI();
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

            // Wait additional frames for chunks to be fully generated and meshed
            yield return null;
            yield return null;

            // Setup player after terrain is generated and chunks are ready
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
            var world = FindFirstObjectByType<VoxelWorld>();
            var spawnPos = _playerSpawnPosition;

            // Calculate spawn position at world center if enabled
            if (_spawnAtWorldCenter && world != null)
            {
                // Get world center in block coordinates
                int centerBlockX = world.WorldSizeInBlocks / 2;
                int centerBlockZ = world.WorldSizeInBlocks / 2;

                // Convert to Unity world coordinates (accounting for BLOCK_SIZE)
                spawnPos.x = centerBlockX * VoxelChunk.BLOCK_SIZE;
                spawnPos.z = centerBlockZ * VoxelChunk.BLOCK_SIZE;

                Debug.Log($"[GameBootstrap] Spawning at world center: block ({centerBlockX}, {centerBlockZ}) -> Unity ({spawnPos.x}, {spawnPos.z})");
            }

            if (_autoSpawnHeight)
            {
                // Convert Unity position back to block coordinates for terrain lookup
                int spawnX = Mathf.RoundToInt(spawnPos.x / VoxelChunk.BLOCK_SIZE);
                int spawnZ = Mathf.RoundToInt(spawnPos.z / VoxelChunk.BLOCK_SIZE);

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

                // Clear area around spawn point FIRST, before calculating final spawn height
                ClearSpawnArea(spawnX, terrainHeight, spawnZ);

                // Find the actual surface height by scanning upward from terrain
                // This accounts for any blocks that weren't cleared
                int actualSurface = terrainHeight;
                if (world != null)
                {
                    // Scan upward to find the highest solid block
                    for (int y = terrainHeight; y < terrainHeight + 20; y++)
                    {
                        var block = world.GetBlock(new Vector3Int(spawnX, y, spawnZ));
                        if (block != null && block.IsSolid)
                        {
                            actualSurface = y;
                        }
                        else
                        {
                            // Found air, stop scanning
                            break;
                        }
                    }
                }

                // Spawn player 1 block above the actual surface (feet on ground)
                // actualSurface is the Y where solid block exists, so +1 puts feet on top of it
                // Convert to Unity world coordinates by multiplying by BLOCK_SIZE
                spawnPos.y = (actualSurface + 1) * VoxelChunk.BLOCK_SIZE;

                Debug.Log($"[GameBootstrap] Auto spawn height: terrain={terrainHeight}, actualSurface={actualSurface}, spawn Y={spawnPos.y}");
            }

            return spawnPos;
        }

        private void ClearSpawnArea(int centerX, int groundY, int centerZ)
        {
            var world = FindFirstObjectByType<VoxelWorld>();
            if (world == null) return;

            // Clear a 5x5x10 area above the ground (trees can be up to 8 blocks tall with leaves)
            int clearRadius = 2;
            int clearHeight = 10;

            for (int x = centerX - clearRadius; x <= centerX + clearRadius; x++)
            {
                for (int z = centerZ - clearRadius; z <= centerZ + clearRadius; z++)
                {
                    for (int y = groundY + 1; y <= groundY + clearHeight; y++)
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
            Debug.Log($"[GameBootstrap] Cleared spawn area around ({centerX}, {groundY}, {centerZ}), radius={clearRadius}, height={clearHeight}");
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
            var playerController = playerObject.AddComponent<PlayerController>();

            // Create camera holder
            var cameraHolder = new GameObject("CameraHolder");
            cameraHolder.transform.SetParent(playerObject.transform);
            cameraHolder.transform.localPosition = new Vector3(0, 1.6f, 0);

            // Create camera
            var cameraObject = new GameObject("PlayerCamera");
            cameraObject.transform.SetParent(cameraHolder.transform);
            cameraObject.transform.localPosition = Vector3.zero;
            cameraObject.transform.localRotation = Quaternion.identity;
            cameraObject.tag = "MainCamera"; // Required for Camera.main to work

            var camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 500f;
            camera.fieldOfView = 70f;

            // Add audio listener
            cameraObject.AddComponent<AudioListener>();

            // Add PlayerCamera component
            var playerCamera = cameraObject.AddComponent<PlayerCamera>();
            playerCamera.SetPlayerBody(playerObject.transform);

            // Wire up camera holder for crouch
            playerController.SetCameraHolder(cameraHolder.transform);

            // Add BlockInteraction
            playerObject.AddComponent<BlockInteraction>();

            // Add survival systems
            var healthSystem = playerObject.AddComponent<HealthSystem>();
            var hungerSystem = playerObject.AddComponent<HungerSystem>();
            var playerStats = playerObject.AddComponent<PlayerStats>();
            var playerInventory = playerObject.AddComponent<PlayerInventory>();
            var deathHandler = playerObject.AddComponent<DeathHandler>();

            // Configure hunger drain: 1 unit per minute = 1/60 per second
            hungerSystem.HungerDecayRate = 1f / 60f;

            // Give starting item if configured
            if (_startingItem != null)
            {
                playerInventory.TryAddItem(_startingItem, 1);
                Debug.Log($"[GameBootstrap] Gave player starting item: {_startingItem.DisplayName}");
            }

            // Tag player for easy finding
            playerObject.tag = "Player";

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

                // Get the block interaction to wire up input events
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
                            playerController.OnMove(context);
                            break;
                        case "Look":
                            playerCamera.OnLook(context);
                            break;
                        case "Jump":
                            playerController.OnJump(context);
                            break;
                        case "Sprint":
                            playerController.OnSprint(context);
                            break;
                        case "Crouch":
                            playerController.OnCrouch(context);
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

        private void SetupTimeSystem()
        {
            // Check if TimeManager already exists
            if (FindFirstObjectByType<TimeManager>() != null)
            {
                return;
            }

            // Create TimeManager
            var timeObject = new GameObject("TimeManager");
            timeObject.AddComponent<TimeManager>();
            Debug.Log("[GameBootstrap] Created TimeManager.");

            // Add SunController to directional light
            var light = FindFirstObjectByType<Light>();
            if (light != null && light.type == LightType.Directional)
            {
                if (light.GetComponent<SunController>() == null)
                {
                    light.gameObject.AddComponent<SunController>();
                    Debug.Log("[GameBootstrap] Added SunController to directional light.");
                }
            }

            // Add weather stub
            var weatherObject = new GameObject("WeatherSystem");
            weatherObject.AddComponent<StubWeatherSystem>();
            Debug.Log("[GameBootstrap] Created StubWeatherSystem.");
        }

        private void SetupMonsterSpawner()
        {
            // Check if MonsterSpawner already exists
            if (FindFirstObjectByType<MonsterSpawner>() != null)
            {
                return;
            }

            // Create MonsterSpawner
            var spawnerObject = new GameObject("MonsterSpawner");
            var spawner = spawnerObject.AddComponent<MonsterSpawner>();

            // Monster types will be set via inspector since we need prefab references
            Debug.Log("[GameBootstrap] Created MonsterSpawner.");
        }

        private void SetupUI()
        {
            // Check if UI already exists
            if (FindFirstObjectByType<UIManager>() != null)
            {
                return;
            }

            // Create UI builder and build the UI
            var builderObject = new GameObject("UIBuilder");
            var builder = builderObject.AddComponent<RuntimeUIBuilder>();
            builder.BuildUI();

            // Destroy the builder after use (it's not needed anymore)
            Destroy(builderObject);

            Debug.Log("[GameBootstrap] Created game UI.");
        }
    }
}
