using System;
using UnityEngine;
using VoxelRPG.Combat;
using VoxelRPG.Core.Events;
using VoxelRPG.Player.Skills;

namespace VoxelRPG.Player
{
    /// <summary>
    /// Manages player health, damage, and healing.
    /// Can be used standalone or coordinated by PlayerStats.
    /// </summary>
    public class HealthSystem : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _startingHealth = 100f;

        [Header("Event Channels")]
        [SerializeField] private FloatEventChannel _onHealthChanged;
        [SerializeField] private VoidEventChannel _onDeath;

        private float _currentHealth;
        private bool _isAlive = true;

        #region IDamageable Implementation

        /// <summary>
        /// Current health points.
        /// </summary>
        public float CurrentHealth => _currentHealth;

        /// <summary>
        /// Maximum health points (including skill bonuses).
        /// </summary>
        public float MaxHealth => _maxHealth + SkillModifiers.GetBonusMaxHealth();

        /// <summary>
        /// Base maximum health without skill bonuses.
        /// </summary>
        public float BaseMaxHealth => _maxHealth;

        /// <summary>
        /// Whether the entity is alive.
        /// </summary>
        public bool IsAlive => _isAlive;

        #endregion

        #region Properties

        /// <summary>
        /// Normalized health value (0-1).
        /// </summary>
        public float HealthNormalized => _maxHealth > 0 ? _currentHealth / _maxHealth : 0f;

        #endregion

        #region Events

        /// <summary>
        /// Raised when health changes. Parameters: current health, max health.
        /// </summary>
        public event Action<float, float> OnHealthChanged;

        /// <summary>
        /// Raised when entity dies.
        /// </summary>
        public event Action OnDeath;

        #endregion

        private void Awake()
        {
            _currentHealth = _startingHealth;
        }

        /// <summary>
        /// Applies damage to this entity.
        /// </summary>
        /// <param name="damage">Amount of damage to apply.</param>
        /// <param name="source">Optional source of the damage.</param>
        /// <returns>Actual damage dealt.</returns>
        public float TakeDamage(float damage, GameObject source = null)
        {
            if (!_isAlive || damage <= 0)
            {
                Debug.Log($"[HealthSystem] TakeDamage blocked: alive={_isAlive}, damage={damage}");
                return 0f;
            }

            // Apply toughness damage reduction from skills
            float reducedDamage = SkillModifiers.CalculateDamageTaken(damage);

            float healthBefore = _currentHealth;
            float actualDamage = Mathf.Min(reducedDamage, _currentHealth);
            _currentHealth -= actualDamage;

            Debug.Log($"[HealthSystem] TakeDamage: {healthBefore} - {actualDamage} = {_currentHealth} (raw: {damage}, reduced: {reducedDamage}, source: {source?.name ?? "null"})");

            RaiseHealthChanged();

            if (_currentHealth <= 0)
            {
                Die();
            }

            return actualDamage;
        }

        /// <summary>
        /// Heals this entity.
        /// </summary>
        /// <param name="amount">Amount to heal.</param>
        /// <returns>Actual amount healed.</returns>
        public float Heal(float amount)
        {
            if (!_isAlive || amount <= 0) return 0f;

            // Apply fortitude healing bonus from skills
            float boostedHeal = SkillModifiers.CalculateHealing(amount);
            float effectiveMax = MaxHealth; // Uses skill-boosted max health

            float actualHeal = Mathf.Min(boostedHeal, effectiveMax - _currentHealth);
            _currentHealth += actualHeal;

            RaiseHealthChanged();

            return actualHeal;
        }

        /// <summary>
        /// Sets health to a specific value.
        /// </summary>
        /// <param name="health">Health value to set.</param>
        public void SetHealth(float health)
        {
            _currentHealth = Mathf.Clamp(health, 0, MaxHealth);
            RaiseHealthChanged();

            if (_currentHealth <= 0 && _isAlive)
            {
                Die();
            }
        }

        /// <summary>
        /// Sets base maximum health, optionally scaling current health.
        /// Note: Skill bonuses are applied on top of this base value.
        /// </summary>
        /// <param name="maxHealth">New base maximum health.</param>
        /// <param name="scaleCurrentHealth">If true, current health scales proportionally.</param>
        public void SetMaxHealth(float maxHealth, bool scaleCurrentHealth = false)
        {
            if (maxHealth <= 0) return;

            if (scaleCurrentHealth && _maxHealth > 0)
            {
                float ratio = _currentHealth / _maxHealth;
                _maxHealth = maxHealth;
                _currentHealth = MaxHealth * ratio; // Use effective max with skill bonuses
            }
            else
            {
                _maxHealth = maxHealth;
                _currentHealth = Mathf.Min(_currentHealth, MaxHealth);
            }

            RaiseHealthChanged();
        }

        /// <summary>
        /// Revives the entity with specified health.
        /// </summary>
        /// <param name="healthPercent">Percentage of max health to revive with (0-1).</param>
        public void Revive(float healthPercent = 1f)
        {
            _isAlive = true;
            _currentHealth = MaxHealth * Mathf.Clamp01(healthPercent);
            RaiseHealthChanged();
        }

        /// <summary>
        /// Resets health to starting values.
        /// </summary>
        public void Reset()
        {
            _isAlive = true;
            _currentHealth = _startingHealth;
            RaiseHealthChanged();
        }

        private void Die()
        {
            if (!_isAlive) return;

            _isAlive = false;
            _currentHealth = 0;

            OnDeath?.Invoke();
            _onDeath?.RaiseEvent();
        }

        private void RaiseHealthChanged()
        {
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            _onHealthChanged?.RaiseEvent(HealthNormalized);
        }

        #region Save/Load Support

        /// <summary>
        /// Gets save data for this component.
        /// </summary>
        public HealthSaveData GetSaveData()
        {
            return new HealthSaveData
            {
                CurrentHealth = _currentHealth,
                MaxHealth = _maxHealth,
                IsAlive = _isAlive
            };
        }

        /// <summary>
        /// Loads save data into this component.
        /// </summary>
        public void LoadSaveData(HealthSaveData data)
        {
            _maxHealth = data.MaxHealth;
            _currentHealth = data.CurrentHealth;
            _isAlive = data.IsAlive;
            RaiseHealthChanged();
        }

        #endregion
    }

    /// <summary>
    /// Serializable save data for HealthSystem.
    /// </summary>
    [System.Serializable]
    public class HealthSaveData
    {
        public float CurrentHealth;
        public float MaxHealth;
        public bool IsAlive;
    }
}
