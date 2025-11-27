using UnityEngine;
using VoxelRPG.Voxel;

namespace VoxelRPG.Core.Bootstrap
{
    /// <summary>
    /// Bootstraps a minimal game scene for testing.
    /// Attach to an empty GameObject in the scene.
    /// Creates world, player, and generates test terrain.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("World Settings")]
        [SerializeField] private int _worldSizeInChunks = 4;
        [SerializeField] private int _worldHeightInChunks = 2;
        [SerializeField] private Material _chunkMaterial;

        [Header("Terrain Generation")]
        [SerializeField] private int _groundHeight = 4;
        [SerializeField] private BlockType _groundBlock;

        [Header("Player Settings")]
        [SerializeField] private Vector3 _playerSpawnPosition = new Vector3(32f, 10f, 32f);

        [Header("Prefabs (Optional)")]
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private GameObject _gameManagerPrefab;

        private void Start()
        {
            SetupGameManager();
            SetupWorld();
            SetupPlayer();
            SetupLighting();

            Debug.Log("[GameBootstrap] Scene setup complete.");
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
            if (FindFirstObjectByType<Player.PlayerController>() != null)
            {
                return;
            }

            if (_playerPrefab != null)
            {
                var player = Instantiate(_playerPrefab, _playerSpawnPosition, Quaternion.identity);
                player.name = "Player";
            }
            else
            {
                CreateDefaultPlayer();
            }
        }

        private void CreateDefaultPlayer()
        {
            // Create player root
            var playerObject = new GameObject("Player");
            playerObject.transform.position = _playerSpawnPosition;
            playerObject.layer = LayerMask.NameToLayer("Default");

            // Add CharacterController
            var characterController = playerObject.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.3f;
            characterController.center = new Vector3(0, 0.9f, 0);

            // Add PlayerController
            playerObject.AddComponent<Player.PlayerController>();

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
            var playerCamera = cameraObject.AddComponent<Player.PlayerCamera>();
            // Set player body reference via serialized field would require reflection
            // For now, the PlayerCamera will need manual setup in inspector

            // Add BlockInteraction
            var blockInteraction = playerObject.AddComponent<Player.BlockInteraction>();

            // Create ground check point
            var groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(playerObject.transform);
            groundCheck.transform.localPosition = new Vector3(0, 0, 0);

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
