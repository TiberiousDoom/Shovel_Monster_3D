using System;
using UnityEngine;
using VoxelRPG.Core;
using VoxelRPG.Core.Events;

namespace VoxelRPG.Player
{
    /// <summary>
    /// Handles player death and respawn logic.
    /// Coordinates with HealthSystem and manages respawn behavior.
    /// </summary>
    public class DeathHandler : MonoBehaviour
    {
        [Header("Respawn Settings")]
        [SerializeField] private float _respawnDelay = 3f;
        [SerializeField] private float _respawnHealthPercent = 1f;
        [SerializeField] private float _respawnHungerPercent = 0.5f;
        [SerializeField] private Transform _defaultSpawnPoint;
        [SerializeField] private bool _returnToSpawnPoint = true;

        [Header("References")]
        [SerializeField] private HealthSystem _healthSystem;
        [SerializeField] private HungerSystem _hungerSystem;
        [SerializeField] private PlayerController _playerController;

        [Header("Event Channels")]
        [SerializeField] private VoidEventChannel _onPlayerDeath;
        [SerializeField] private VoidEventChannel _onPlayerRespawn;

        private Vector3 _lastSpawnPosition;
        private bool _isWaitingToRespawn;
        private float _respawnTimer;

        #region Properties

        /// <summary>
        /// Whether the player is currently dead and waiting to respawn.
        /// </summary>
        public bool IsWaitingToRespawn => _isWaitingToRespawn;

        /// <summary>
        /// Time remaining until respawn.
        /// </summary>
        public float RespawnTimeRemaining => _isWaitingToRespawn ? Mathf.Max(0, _respawnDelay - _respawnTimer) : 0f;

        /// <summary>
        /// The spawn point where player will respawn.
        /// </summary>
        public Vector3 SpawnPoint
        {
            get => _defaultSpawnPoint != null ? _defaultSpawnPoint.position : _lastSpawnPosition;
            set => _lastSpawnPosition = value;
        }

        #endregion

        #region Events

        /// <summary>
        /// Raised when player dies.
        /// </summary>
        public event Action OnDeath;

        /// <summary>
        /// Raised when player respawns.
        /// </summary>
        public event Action OnRespawn;

        /// <summary>
        /// Raised during respawn countdown. Parameter: time remaining.
        /// </summary>
        public event Action<float> OnRespawnCountdown;

        #endregion

        private void Awake()
        {
            CacheReferences();
            _lastSpawnPosition = transform.position;

            ServiceLocator.Register<DeathHandler>(this);
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            ServiceLocator.Unregister<DeathHandler>();
        }

        private void Start()
        {
            SubscribeToEvents();
        }

        private void Update()
        {
            if (_isWaitingToRespawn)
            {
                UpdateRespawnTimer();
            }
        }

        private void CacheReferences()
        {
            if (_healthSystem == null)
            {
                _healthSystem = GetComponent<HealthSystem>();
            }

            if (_hungerSystem == null)
            {
                _hungerSystem = GetComponent<HungerSystem>();
            }

            if (_playerController == null)
            {
                _playerController = GetComponent<PlayerController>();
            }
        }

        private void SubscribeToEvents()
        {
            if (_healthSystem != null)
            {
                _healthSystem.OnDeath += HandleDeath;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_healthSystem != null)
            {
                _healthSystem.OnDeath -= HandleDeath;
            }
        }

        private void HandleDeath()
        {
            if (_isWaitingToRespawn) return;

            Debug.Log("[DeathHandler] Player died");

            _isWaitingToRespawn = true;
            _respawnTimer = 0f;

            // Disable player controls during death
            if (_playerController != null)
            {
                _playerController.enabled = false;
            }

            // Disable hunger decay during death
            if (_hungerSystem != null)
            {
                _hungerSystem.IsActive = false;
            }

            OnDeath?.Invoke();
            _onPlayerDeath?.RaiseEvent();
        }

        private void UpdateRespawnTimer()
        {
            _respawnTimer += Time.deltaTime;
            OnRespawnCountdown?.Invoke(RespawnTimeRemaining);

            if (_respawnTimer >= _respawnDelay)
            {
                ExecuteRespawn();
            }
        }

        private void ExecuteRespawn()
        {
            Debug.Log("[DeathHandler] Respawning player");

            _isWaitingToRespawn = false;
            _respawnTimer = 0f;

            // Revive health
            if (_healthSystem != null)
            {
                _healthSystem.Revive(_respawnHealthPercent);
            }

            // Reset hunger
            if (_hungerSystem != null)
            {
                _hungerSystem.SetHunger(_hungerSystem.MaxHunger * _respawnHungerPercent);
                _hungerSystem.IsActive = true;
            }

            // Teleport to spawn point
            if (_returnToSpawnPoint && _playerController != null)
            {
                _playerController.Teleport(SpawnPoint);
            }

            // Re-enable player controls
            if (_playerController != null)
            {
                _playerController.enabled = true;
            }

            OnRespawn?.Invoke();
            _onPlayerRespawn?.RaiseEvent();
        }

        /// <summary>
        /// Forces immediate respawn, skipping the delay.
        /// </summary>
        public void ForceRespawn()
        {
            if (!_isWaitingToRespawn) return;

            _respawnTimer = _respawnDelay;
            ExecuteRespawn();
        }

        /// <summary>
        /// Sets the spawn point for respawning.
        /// </summary>
        /// <param name="spawnPoint">The spawn point transform.</param>
        public void SetSpawnPoint(Transform spawnPoint)
        {
            _defaultSpawnPoint = spawnPoint;
        }

        /// <summary>
        /// Sets the spawn position directly.
        /// </summary>
        /// <param name="position">The spawn position.</param>
        public void SetSpawnPosition(Vector3 position)
        {
            _lastSpawnPosition = position;
        }

        /// <summary>
        /// Updates spawn point to current player position.
        /// Call this when player sleeps, sets home, etc.
        /// </summary>
        public void SaveCurrentPositionAsSpawn()
        {
            _lastSpawnPosition = transform.position;
        }

        /// <summary>
        /// Kills the player immediately (for hazards, falling, etc.).
        /// </summary>
        public void KillPlayer()
        {
            if (_healthSystem != null && _healthSystem.IsAlive)
            {
                _healthSystem.TakeDamage(_healthSystem.CurrentHealth + 1);
            }
        }

        #region Save/Load Support

        /// <summary>
        /// Gets save data for this component.
        /// </summary>
        public DeathHandlerSaveData GetSaveData()
        {
            return new DeathHandlerSaveData
            {
                SpawnPosition = _lastSpawnPosition
            };
        }

        /// <summary>
        /// Loads save data into this component.
        /// </summary>
        public void LoadSaveData(DeathHandlerSaveData data)
        {
            _lastSpawnPosition = data.SpawnPosition;
        }

        #endregion
    }

    /// <summary>
    /// Serializable save data for DeathHandler.
    /// </summary>
    [System.Serializable]
    public class DeathHandlerSaveData
    {
        public Vector3 SpawnPosition;
    }
}
