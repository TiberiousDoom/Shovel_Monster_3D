using UnityEngine;
using VoxelRPG.Core;

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
        [SerializeField] private KeyCode _damageKey = KeyCode.F1;
        [SerializeField] private KeyCode _healKey = KeyCode.F2;
        [SerializeField] private KeyCode _feedKey = KeyCode.F3;
        [SerializeField] private KeyCode _logStatsKey = KeyCode.F4;

        private PlayerStats _playerStats;
        private HealthSystem _healthSystem;
        private HungerSystem _hungerSystem;
        private PlayerController _playerController;

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
            if (Input.GetKeyDown(_damageKey))
            {
                if (_healthSystem != null)
                {
                    float dealt = _healthSystem.TakeDamage(_testDamageAmount);
                    Debug.Log($"[SurvivalDebug] Dealt {dealt} damage");
                }
            }

            if (Input.GetKeyDown(_healKey))
            {
                if (_healthSystem != null)
                {
                    float healed = _healthSystem.Heal(_testHealAmount);
                    Debug.Log($"[SurvivalDebug] Healed {healed} HP");
                }
            }

            if (Input.GetKeyDown(_feedKey))
            {
                if (_hungerSystem != null)
                {
                    float fed = _hungerSystem.Feed(_testFeedAmount);
                    Debug.Log($"[SurvivalDebug] Restored {fed} hunger");
                }
            }

            if (Input.GetKeyDown(_logStatsKey))
            {
                LogCurrentStats();
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
    }
}
