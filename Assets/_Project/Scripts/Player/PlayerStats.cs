using System;
using UnityEngine;
using VoxelRPG.Combat;
using VoxelRPG.Core;
using VoxelRPG.Core.Events;

namespace VoxelRPG.Player
{
    /// <summary>
    /// Central container for all player statistics.
    /// Coordinates HealthSystem, HungerSystem, and other survival systems.
    /// Provides a unified facade for accessing player stats.
    /// Implements IDamageable to delegate to HealthSystem.
    /// </summary>
    public class PlayerStats : MonoBehaviour, IDamageable
    {
        [Header("Systems")]
        [SerializeField] private HealthSystem _healthSystem;
        [SerializeField] private HungerSystem _hungerSystem;

        [Header("Event Channels")]
        [SerializeField] private VoidEventChannel _onPlayerDeath;
        [SerializeField] private VoidEventChannel _onPlayerRespawn;

        #region IDamageable Implementation (delegates to HealthSystem)

        /// <summary>
        /// Current health points.
        /// </summary>
        public float CurrentHealth => _healthSystem != null ? _healthSystem.CurrentHealth : 0f;

        /// <summary>
        /// Maximum health points.
        /// </summary>
        public float MaxHealth => _healthSystem != null ? _healthSystem.MaxHealth : 0f;

        /// <summary>
        /// Whether the player is alive.
        /// </summary>
        public bool IsAlive => _healthSystem != null && _healthSystem.IsAlive;

        /// <summary>
        /// Applies damage to the player.
        /// </summary>
        public float TakeDamage(float damage, GameObject source = null)
        {
            return _healthSystem != null ? _healthSystem.TakeDamage(damage, source) : 0f;
        }

        /// <summary>
        /// Heals the player.
        /// </summary>
        public float Heal(float amount)
        {
            return _healthSystem != null ? _healthSystem.Heal(amount) : 0f;
        }

        #endregion

        #region Hunger Properties (delegates to HungerSystem)

        /// <summary>
        /// Current hunger level (0 = starving, max = full).
        /// </summary>
        public float CurrentHunger => _hungerSystem != null ? _hungerSystem.CurrentHunger : 0f;

        /// <summary>
        /// Maximum hunger capacity.
        /// </summary>
        public float MaxHunger => _hungerSystem != null ? _hungerSystem.MaxHunger : 0f;

        /// <summary>
        /// Whether the player is currently starving.
        /// </summary>
        public bool IsStarving => _hungerSystem != null && _hungerSystem.IsStarving;

        /// <summary>
        /// Normalized health value (0-1).
        /// </summary>
        public float HealthNormalized => _healthSystem != null ? _healthSystem.HealthNormalized : 0f;

        /// <summary>
        /// Normalized hunger value (0-1).
        /// </summary>
        public float HungerNormalized => _hungerSystem != null ? _hungerSystem.HungerNormalized : 0f;

        #endregion

        #region System References

        /// <summary>
        /// Direct access to the health system.
        /// </summary>
        public HealthSystem Health => _healthSystem;

        /// <summary>
        /// Direct access to the hunger system.
        /// </summary>
        public HungerSystem Hunger => _hungerSystem;

        #endregion

        #region Events

        /// <summary>
        /// Raised when health changes. Parameters: current health, max health.
        /// </summary>
        public event Action<float, float> OnHealthChanged;

        /// <summary>
        /// Raised when hunger changes. Parameters: current hunger, max hunger.
        /// </summary>
        public event Action<float, float> OnHungerChanged;

        /// <summary>
        /// Raised when the player dies.
        /// </summary>
        public event Action OnDeath;

        /// <summary>
        /// Raised when the player respawns.
        /// </summary>
        public event Action OnRespawn;

        #endregion

        private void Awake()
        {
            CacheReferences();
            ServiceLocator.Register<PlayerStats>(this);
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            ServiceLocator.Unregister<PlayerStats>();
        }

        private void Start()
        {
            SetupHungerSystemReference();
            SubscribeToEvents();
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
        }

        private void SetupHungerSystemReference()
        {
            // Ensure hunger system knows about health system for starvation damage
            if (_hungerSystem != null && _healthSystem != null)
            {
                _hungerSystem.SetHealthSystem(_healthSystem);
            }
        }

        private void SubscribeToEvents()
        {
            if (_healthSystem != null)
            {
                _healthSystem.OnHealthChanged += HandleHealthChanged;
                _healthSystem.OnDeath += HandleDeath;
            }

            if (_hungerSystem != null)
            {
                _hungerSystem.OnHungerChanged += HandleHungerChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_healthSystem != null)
            {
                _healthSystem.OnHealthChanged -= HandleHealthChanged;
                _healthSystem.OnDeath -= HandleDeath;
            }

            if (_hungerSystem != null)
            {
                _hungerSystem.OnHungerChanged -= HandleHungerChanged;
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            OnHealthChanged?.Invoke(current, max);
        }

        private void HandleHungerChanged(float current, float max)
        {
            OnHungerChanged?.Invoke(current, max);
        }

        private void HandleDeath()
        {
            Debug.Log("[PlayerStats] Player died");
            OnDeath?.Invoke();
            _onPlayerDeath?.RaiseEvent();
        }

        #region Convenience Methods

        /// <summary>
        /// Sets health to a specific value.
        /// </summary>
        public void SetHealth(float health)
        {
            _healthSystem?.SetHealth(health);
        }

        /// <summary>
        /// Feeds the player, restoring hunger.
        /// </summary>
        public float Feed(float amount)
        {
            return _hungerSystem != null ? _hungerSystem.Feed(amount) : 0f;
        }

        /// <summary>
        /// Consumes hunger (for special actions, etc.).
        /// </summary>
        public float ConsumeHunger(float amount)
        {
            return _hungerSystem != null ? _hungerSystem.ConsumeHunger(amount) : 0f;
        }

        /// <summary>
        /// Sets hunger to a specific value.
        /// </summary>
        public void SetHunger(float hunger)
        {
            _hungerSystem?.SetHunger(hunger);
        }

        /// <summary>
        /// Respawns the player with specified stats.
        /// </summary>
        /// <param name="healthPercent">Percentage of max health (0-1).</param>
        /// <param name="hungerPercent">Percentage of max hunger (0-1).</param>
        public void Respawn(float healthPercent = 1f, float hungerPercent = 0.5f)
        {
            _healthSystem?.Revive(healthPercent);

            if (_hungerSystem != null)
            {
                _hungerSystem.SetHunger(_hungerSystem.MaxHunger * hungerPercent);
            }

            Debug.Log($"[PlayerStats] Player respawned with {CurrentHealth}/{MaxHealth} HP, {CurrentHunger}/{MaxHunger} hunger");

            OnRespawn?.Invoke();
            _onPlayerRespawn?.RaiseEvent();
        }

        /// <summary>
        /// Resets all stats to starting values (for new game).
        /// </summary>
        public void ResetStats()
        {
            _healthSystem?.Reset();
            _hungerSystem?.Reset();
        }

        #endregion

        #region Save/Load Support

        /// <summary>
        /// Gets save data for all player stats.
        /// </summary>
        public PlayerStatsSaveData GetSaveData()
        {
            return new PlayerStatsSaveData
            {
                Health = _healthSystem?.GetSaveData(),
                Hunger = _hungerSystem?.GetSaveData()
            };
        }

        /// <summary>
        /// Loads save data for all player stats.
        /// </summary>
        public void LoadSaveData(PlayerStatsSaveData data)
        {
            if (data.Health != null && _healthSystem != null)
            {
                _healthSystem.LoadSaveData(data.Health);
            }

            if (data.Hunger != null && _hungerSystem != null)
            {
                _hungerSystem.LoadSaveData(data.Hunger);
            }
        }

        #endregion
    }

    /// <summary>
    /// Serializable save data for PlayerStats.
    /// Aggregates save data from all subsystems.
    /// </summary>
    [System.Serializable]
    public class PlayerStatsSaveData
    {
        public HealthSaveData Health;
        public HungerSaveData Hunger;
    }
}
