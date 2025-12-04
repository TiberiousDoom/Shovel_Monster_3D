using UnityEngine;
using UnityEngine.InputSystem;
using VoxelRPG.Core;
using VoxelRPG.Voxel;

namespace VoxelRPG.Player
{
    /// <summary>
    /// Debug helper for testing survival systems.
    /// Displays stats in console and provides test controls.
    /// Remove or disable in production builds.
    /// </summary>
    public class PlayerSurvivalDebug : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool _enableDebugOutput = true;
        [SerializeField] private float _logInterval = 5f;
        [SerializeField] private float _testDamageAmount = 10f;
        [SerializeField] private float _testHealAmount = 25f;
        [SerializeField] private float _testFeedAmount = 50f;

        [Header("Fall Damage")]
        [SerializeField] private bool _enableFallDamage = true;
        [SerializeField] private float _minFallDistance = 4f;
        [SerializeField] private float _damagePerMeter = 10f;
        [SerializeField] private float _maxFallDamage = 100f;

        [Header("Key Bindings")]
        [SerializeField] private Key _damageKey = Key.F1;
        [SerializeField] private Key _healKey = Key.F2;
        [SerializeField] private Key _feedKey = Key.F3;
        [SerializeField] private Key _logStatsKey = Key.F4;
        [SerializeField] private Key _unstuckKey = Key.F5;

        [Header("Unstuck Settings")]
        [Tooltip("Height above current position to teleport when unstuck")]
        [SerializeField] private float _unstuckHeight = 3f;

        private PlayerStats _playerStats;
        private HealthSystem _healthSystem;
        private HungerSystem _hungerSystem;
        private PlayerController _playerController;
        private CharacterController _characterController;
        private IVoxelWorld _voxelWorld;

        private float _lastLogTime;
        private float _highestY;
        private bool _wasGrounded = true;
        private bool _isFalling;

        private void Start()
        {
            CacheReferences();
            SubscribeToEvents();

            if (_enableDebugOutput)
            {
                Debug.Log("[SurvivalDebug] === Debug Controls ===");
                Debug.Log($"  {_damageKey} - Take {_testDamageAmount} damage");
                Debug.Log($"  {_healKey} - Heal {_testHealAmount} HP");
                Debug.Log($"  {_feedKey} - Restore {_testFeedAmount} hunger");
                Debug.Log($"  {_logStatsKey} - Log current stats");
                Debug.Log($"  {_unstuckKey} - Teleport up if stuck in blocks");
                Debug.Log($"  Fall damage: {(_enableFallDamage ? "ON" : "OFF")} (min {_minFallDistance}m, {_damagePerMeter} dmg/m)");
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void CacheReferences()
        {
            _playerStats = GetComponent<PlayerStats>();
            _healthSystem = GetComponent<HealthSystem>();
            _hungerSystem = GetComponent<HungerSystem>();
            _playerController = GetComponent<PlayerController>();

            // Try ServiceLocator if not on same object
            if (_playerStats == null) ServiceLocator.TryGet(out _playerStats);
            if (_healthSystem == null && _playerStats != null) _healthSystem = _playerStats.Health;
            if (_hungerSystem == null && _playerStats != null) _hungerSystem = _playerStats.Hunger;
            if (_playerController == null) ServiceLocator.TryGet(out _playerController);

            // Get CharacterController for unstuck feature
            _characterController = GetComponent<CharacterController>();
            if (_characterController == null) _characterController = GetComponentInParent<CharacterController>();

            // Get VoxelWorld for block checking
            ServiceLocator.TryGet(out _voxelWorld);
        }

        private void SubscribeToEvents()
        {
            if (_healthSystem != null)
            {
                _healthSystem.OnHealthChanged += OnHealthChanged;
                _healthSystem.OnDeath += OnPlayerDeath;
            }

            if (_hungerSystem != null)
            {
                _hungerSystem.OnHungerChanged += OnHungerChanged;
                _hungerSystem.OnStarvationStarted += OnStarvationStarted;
                _hungerSystem.OnStarvationEnded += OnStarvationEnded;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_healthSystem != null)
            {
                _healthSystem.OnHealthChanged -= OnHealthChanged;
                _healthSystem.OnDeath -= OnPlayerDeath;
            }

            if (_hungerSystem != null)
            {
                _hungerSystem.OnHungerChanged -= OnHungerChanged;
                _hungerSystem.OnStarvationStarted -= OnStarvationStarted;
                _hungerSystem.OnStarvationEnded -= OnStarvationEnded;
            }
        }

        private void Update()
        {
            HandleDebugInput();
            HandleFallDamage();
            HandlePeriodicLog();
        }

        private void HandleDebugInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard[_damageKey].wasPressedThisFrame)
            {
                if (_healthSystem != null)
                {
                    float dealt = _healthSystem.TakeDamage(_testDamageAmount);
                    Debug.Log($"[SurvivalDebug] Dealt {dealt} damage");
                }
            }

            if (keyboard[_healKey].wasPressedThisFrame)
            {
                if (_healthSystem != null)
                {
                    float healed = _healthSystem.Heal(_testHealAmount);
                    Debug.Log($"[SurvivalDebug] Healed {healed} HP");
                }
            }

            if (keyboard[_feedKey].wasPressedThisFrame)
            {
                if (_hungerSystem != null)
                {
                    float fed = _hungerSystem.Feed(_testFeedAmount);
                    Debug.Log($"[SurvivalDebug] Restored {fed} hunger");
                }
            }

            if (keyboard[_logStatsKey].wasPressedThisFrame)
            {
                LogCurrentStats();
            }

            if (keyboard[_unstuckKey].wasPressedThisFrame)
            {
                TryUnstuck();
            }
        }

        private void HandleFallDamage()
        {
            if (!_enableFallDamage || _playerController == null || _healthSystem == null) return;

            bool isGrounded = _playerController.IsGrounded;

            // Track highest point while in air
            if (!isGrounded)
            {
                if (!_isFalling)
                {
                    // Just started falling
                    _isFalling = true;
                    _highestY = transform.position.y;
                }
                else if (transform.position.y > _highestY)
                {
                    _highestY = transform.position.y;
                }
            }

            // Check for landing
            if (isGrounded && !_wasGrounded && _isFalling)
            {
                float fallDistance = _highestY - transform.position.y;

                if (fallDistance >= _minFallDistance)
                {
                    float excessFall = fallDistance - _minFallDistance;
                    float damage = Mathf.Min(excessFall * _damagePerMeter, _maxFallDamage);

                    if (damage > 0)
                    {
                        _healthSystem.TakeDamage(damage);
                        Debug.Log($"[SurvivalDebug] Fall damage! Fell {fallDistance:F1}m, took {damage:F1} damage");
                    }
                }

                _isFalling = false;
            }

            _wasGrounded = isGrounded;
        }

        private void HandlePeriodicLog()
        {
            if (!_enableDebugOutput) return;

            if (Time.time - _lastLogTime >= _logInterval)
            {
                _lastLogTime = Time.time;
                LogCurrentStats();
            }
        }

        private void LogCurrentStats()
        {
            if (_healthSystem == null && _hungerSystem == null)
            {
                Debug.Log("[SurvivalDebug] No survival systems found!");
                return;
            }

            string healthStr = _healthSystem != null
                ? $"HP: {_healthSystem.CurrentHealth:F1}/{_healthSystem.MaxHealth:F1} ({_healthSystem.HealthNormalized * 100:F0}%)"
                : "HP: N/A";

            string hungerStr = _hungerSystem != null
                ? $"Hunger: {_hungerSystem.CurrentHunger:F1}/{_hungerSystem.MaxHunger:F1} ({_hungerSystem.HungerNormalized * 100:F0}%)"
                : "Hunger: N/A";

            string starvingStr = (_hungerSystem != null && _hungerSystem.IsStarving) ? " [STARVING!]" : "";
            string aliveStr = (_healthSystem != null && !_healthSystem.IsAlive) ? " [DEAD]" : "";

            Debug.Log($"[SurvivalDebug] {healthStr} | {hungerStr}{starvingStr}{aliveStr}");
        }

        #region Event Handlers

        private void OnHealthChanged(float current, float max)
        {
            if (!_enableDebugOutput) return;
            Debug.Log($"[SurvivalDebug] Health changed: {current:F1}/{max:F1}");
        }

        private void OnHungerChanged(float current, float max)
        {
            // Only log significant changes to avoid spam
            // The periodic log will show current values
        }

        private void OnStarvationStarted()
        {
            Debug.LogWarning("[SurvivalDebug] === STARVATION STARTED === Health will drain!");
        }

        private void OnStarvationEnded()
        {
            Debug.Log("[SurvivalDebug] Starvation ended - health drain stopped");
        }

        private void OnPlayerDeath()
        {
            Debug.LogError("[SurvivalDebug] === PLAYER DIED ===");
        }

        #endregion

        #region Unstuck Feature

        /// <summary>
        /// Attempts to teleport the player out of any blocks they're stuck in.
        /// </summary>
        private void TryUnstuck()
        {
            if (_characterController == null)
            {
                Debug.LogWarning("[SurvivalDebug] Cannot unstuck - no CharacterController found");
                return;
            }

            // Check if player is actually stuck inside blocks
            bool isStuck = IsPlayerStuckInBlocks();

            if (!isStuck)
            {
                Debug.Log("[SurvivalDebug] Player is not stuck in any blocks");
                return;
            }

            // Find a safe position above current location
            Vector3 safePosition = FindSafePosition();

            // Disable CharacterController temporarily to allow teleportation
            _characterController.enabled = false;
            transform.position = safePosition;
            _characterController.enabled = true;

            // Reset fall tracking so we don't take fall damage from teleport
            _isFalling = false;
            _highestY = safePosition.y;

            Debug.Log($"[SurvivalDebug] Teleported to safe position: {safePosition}");
        }

        /// <summary>
        /// Checks if the player's bounding box overlaps with any solid blocks.
        /// </summary>
        private bool IsPlayerStuckInBlocks()
        {
            if (_voxelWorld == null) return false;

            var playerCenter = _characterController.transform.position + _characterController.center;
            float radius = _characterController.radius;
            float halfHeight = _characterController.height * 0.5f;

            // Check blocks that could overlap with player bounds
            int minX = Mathf.FloorToInt(playerCenter.x - radius);
            int maxX = Mathf.FloorToInt(playerCenter.x + radius);
            int minY = Mathf.FloorToInt(playerCenter.y - halfHeight);
            int maxY = Mathf.FloorToInt(playerCenter.y + halfHeight);
            int minZ = Mathf.FloorToInt(playerCenter.z - radius);
            int maxZ = Mathf.FloorToInt(playerCenter.z + radius);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        var blockPos = new Vector3Int(x, y, z);
                        var block = _voxelWorld.GetBlock(blockPos);

                        if (block != null && block != BlockType.Air && block.IsSolid)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Finds a safe position above the player's current location.
        /// </summary>
        private Vector3 FindSafePosition()
        {
            Vector3 currentPos = transform.position;

            // Start searching from above current position
            float searchY = currentPos.y + _unstuckHeight;
            float maxSearchY = currentPos.y + 50f; // Don't search forever

            while (searchY < maxSearchY)
            {
                Vector3 testPos = new Vector3(currentPos.x, searchY, currentPos.z);

                if (IsPositionSafe(testPos))
                {
                    return testPos;
                }

                searchY += 1f;
            }

            // Fallback: just teleport up by unstuck height
            return currentPos + Vector3.up * _unstuckHeight;
        }

        /// <summary>
        /// Checks if a position is safe for the player (no solid blocks in player volume).
        /// </summary>
        private bool IsPositionSafe(Vector3 position)
        {
            if (_voxelWorld == null) return true;

            float radius = _characterController.radius;
            float height = _characterController.height;

            // Check blocks in player volume at test position
            int minX = Mathf.FloorToInt(position.x - radius);
            int maxX = Mathf.FloorToInt(position.x + radius);
            int minY = Mathf.FloorToInt(position.y);
            int maxY = Mathf.FloorToInt(position.y + height);
            int minZ = Mathf.FloorToInt(position.z - radius);
            int maxZ = Mathf.FloorToInt(position.z + radius);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        var blockPos = new Vector3Int(x, y, z);
                        var block = _voxelWorld.GetBlock(blockPos);

                        if (block != null && block != BlockType.Air && block.IsSolid)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        #endregion
    }
}
