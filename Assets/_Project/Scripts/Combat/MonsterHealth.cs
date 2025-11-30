using System;
using UnityEngine;

namespace VoxelRPG.Combat
{
    /// <summary>
    /// Health component for monsters implementing IDamageable.
    /// Separate from AI to allow flexible composition.
    /// </summary>
    public class MonsterHealth : MonoBehaviour, IDamageable
    {
        [Header("Health")]
        [SerializeField] private float _maxHealth = 50f;
        [SerializeField] private float _currentHealth;

        [Header("Defense")]
        [Tooltip("Damage reduction (0-1)")]
        [Range(0f, 0.9f)]
        [SerializeField] private float _damageReduction;

        [Tooltip("Minimum damage that can be dealt")]
        [SerializeField] private float _minimumDamage = 1f;

        [Header("Invincibility")]
        [Tooltip("Duration of invincibility after taking damage")]
        [SerializeField] private float _invincibilityDuration = 0.2f;

        private float _invincibilityTimer;
        private IMonsterAI _monsterAI;

        /// <inheritdoc/>
        public float CurrentHealth => _currentHealth;

        /// <inheritdoc/>
        public float MaxHealth => _maxHealth;

        /// <inheritdoc/>
        public bool IsAlive => _currentHealth > 0;

        /// <summary>
        /// Health as a normalized value (0-1).
        /// </summary>
        public float HealthNormalized => _maxHealth > 0 ? _currentHealth / _maxHealth : 0f;

        /// <summary>
        /// Event fired when health changes.
        /// </summary>
        public event Action<float, float> OnHealthChanged;

        /// <summary>
        /// Event fired when monster takes damage.
        /// </summary>
        public event Action<float, GameObject> OnDamaged;

        /// <summary>
        /// Event fired when monster dies.
        /// </summary>
        public event Action OnDeath;

        private void Awake()
        {
            _monsterAI = GetComponent<IMonsterAI>();
        }

        private void Start()
        {
            // Initialize to max health if not set
            if (_currentHealth <= 0)
            {
                _currentHealth = _maxHealth;
            }
        }

        private void Update()
        {
            if (_invincibilityTimer > 0)
            {
                _invincibilityTimer -= Time.deltaTime;
            }
        }

        /// <summary>
        /// Initializes health from a monster definition.
        /// </summary>
        public void Initialize(MonsterDefinition definition)
        {
            _maxHealth = definition.MaxHealth;
            _currentHealth = _maxHealth;
        }

        /// <inheritdoc/>
        public float TakeDamage(float damage, GameObject source = null)
        {
            if (!IsAlive) return 0f;
            if (_invincibilityTimer > 0) return 0f;

            // Apply damage reduction
            float reducedDamage = damage * (1f - _damageReduction);
            float actualDamage = Mathf.Max(reducedDamage, _minimumDamage);

            // Apply damage
            float previousHealth = _currentHealth;
            _currentHealth = Mathf.Max(0f, _currentHealth - actualDamage);
            float damageDealt = previousHealth - _currentHealth;

            // Trigger invincibility
            _invincibilityTimer = _invincibilityDuration;

            // Fire events
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            OnDamaged?.Invoke(damageDealt, source);

            // Calculate knockback direction
            Vector3 knockback = Vector3.zero;
            if (source != null)
            {
                knockback = (transform.position - source.transform.position).normalized;
            }

            // Notify AI
            _monsterAI?.OnDamaged(damageDealt, knockback);

            // Check for death
            if (!IsAlive)
            {
                Die();
            }

            return damageDealt;
        }

        /// <inheritdoc/>
        public float Heal(float amount)
        {
            if (!IsAlive) return 0f;

            float previousHealth = _currentHealth;
            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            float healedAmount = _currentHealth - previousHealth;

            if (healedAmount > 0)
            {
                OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            }

            return healedAmount;
        }

        /// <summary>
        /// Sets health to a specific value.
        /// </summary>
        public void SetHealth(float health)
        {
            _currentHealth = Mathf.Clamp(health, 0f, _maxHealth);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            if (!IsAlive)
            {
                Die();
            }
        }

        /// <summary>
        /// Instantly kills this monster.
        /// </summary>
        public void Kill()
        {
            if (!IsAlive) return;
            _currentHealth = 0f;
            Die();
        }

        private void Die()
        {
            OnDeath?.Invoke();
            _monsterAI?.OnDeath();
        }

        /// <summary>
        /// Resets health to max (for respawning/pooling).
        /// </summary>
        public void ResetHealth()
        {
            _currentHealth = _maxHealth;
            _invincibilityTimer = 0f;
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }
    }
}
