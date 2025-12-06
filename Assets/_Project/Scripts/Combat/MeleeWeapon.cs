using UnityEngine;
using VoxelRPG.Core.Items;
using VoxelRPG.Player.Skills;

namespace VoxelRPG.Combat
{
    /// <summary>
    /// Melee weapon component for equippable weapons like axes and swords.
    /// Handles attack input, animation, and damage dealing via Hitbox.
    /// </summary>
    public class MeleeWeapon : MonoBehaviour
    {
        [Header("Weapon Stats")]
        [SerializeField] private float _baseDamage = 15f;
        [SerializeField] private float _attackCooldown = 0.8f;
        [SerializeField] private float _attackRange = 2f;
        // Note: Knockback not yet implemented - add when physics-based combat is added

        [Header("Attack Timing")]
        [Tooltip("Delay before hitbox activates (for wind-up animation)")]
        [SerializeField] private float _hitboxActivateDelay = 0.1f;
        [Tooltip("Duration hitbox stays active")]
        [SerializeField] private float _hitboxActiveDuration = 0.2f;

        [Header("Components")]
        [SerializeField] private Hitbox _hitbox;
        [SerializeField] private Animator _animator;
        [SerializeField] private AudioSource _audioSource;

        [Header("Audio")]
        [SerializeField] private AudioClip _swingSound;
        [SerializeField] private AudioClip _hitSound;

        [Header("Visual Effects")]
        [SerializeField] private TrailRenderer _swingTrail;
        [SerializeField] private ParticleSystem _hitEffect;

        // State
        private float _cooldownTimer;
        private bool _isAttacking;
        private float _hitboxTimer;
        private bool _hitboxActive;

        // Owner reference
        private GameObject _owner;

        // Animator hashes
        private static readonly int AnimAttack = Animator.StringToHash("Attack");
        private static readonly int AnimAttackSpeed = Animator.StringToHash("AttackSpeed");

        /// <summary>
        /// Base damage of this weapon.
        /// </summary>
        public float BaseDamage => _baseDamage;

        /// <summary>
        /// Attack cooldown in seconds.
        /// </summary>
        public float AttackCooldown => _attackCooldown;

        /// <summary>
        /// Whether the weapon is currently attacking.
        /// </summary>
        public bool IsAttacking => _isAttacking;

        /// <summary>
        /// Whether the weapon is ready to attack.
        /// </summary>
        public bool CanAttack => _cooldownTimer <= 0 && !_isAttacking;

        private void Awake()
        {
            // Auto-find hitbox if not assigned
            if (_hitbox == null)
            {
                _hitbox = GetComponentInChildren<Hitbox>();
            }

            // Auto-find animator
            if (_animator == null)
            {
                _animator = GetComponentInParent<Animator>();
            }

            // Auto-find audio source
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
                if (_audioSource == null)
                {
                    _audioSource = gameObject.AddComponent<AudioSource>();
                    _audioSource.playOnAwake = false;
                    _audioSource.spatialBlend = 1f;
                }
            }

            // Initialize hitbox
            if (_hitbox != null)
            {
                _hitbox.SetDamage(_baseDamage);
                _hitbox.Deactivate();
                _hitbox.OnHit += OnWeaponHit;
            }

            // Disable trail by default
            if (_swingTrail != null)
            {
                _swingTrail.emitting = false;
            }
        }

        private void OnDestroy()
        {
            if (_hitbox != null)
            {
                _hitbox.OnHit -= OnWeaponHit;
            }
        }

        private void Update()
        {
            // Update cooldown
            if (_cooldownTimer > 0)
            {
                _cooldownTimer -= Time.deltaTime;
            }

            // Update hitbox timing during attack
            if (_isAttacking)
            {
                _hitboxTimer -= Time.deltaTime;

                if (!_hitboxActive && _hitboxTimer <= _hitboxActiveDuration)
                {
                    // Activate hitbox
                    ActivateHitbox();
                }
                else if (_hitboxActive && _hitboxTimer <= 0)
                {
                    // Deactivate hitbox, end attack
                    DeactivateHitbox();
                    EndAttack();
                }
            }
        }

        /// <summary>
        /// Sets the owner of this weapon (for self-damage prevention).
        /// </summary>
        public void SetOwner(GameObject owner)
        {
            _owner = owner;
            if (_hitbox != null)
            {
                _hitbox.SetOwner(owner);
            }
        }

        /// <summary>
        /// Configures the weapon from an ItemDefinition.
        /// Override stats if the item has weapon-specific data.
        /// </summary>
        public void ConfigureFromItem(ItemDefinition item)
        {
            // Future: Read weapon stats from item definition extension
            // For now, use serialized values
        }

        /// <summary>
        /// Attempts to perform an attack.
        /// </summary>
        /// <returns>True if attack started, false if on cooldown.</returns>
        public bool TryAttack()
        {
            if (!CanAttack) return false;

            StartAttack();
            return true;
        }

        private void StartAttack()
        {
            _isAttacking = true;
            _cooldownTimer = _attackCooldown;
            _hitboxTimer = _hitboxActivateDelay + _hitboxActiveDuration;

            // Play animation
            if (_animator != null)
            {
                _animator.SetTrigger(AnimAttack);
            }

            // Play swing sound
            if (_swingSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(_swingSound);
            }

            // Enable trail
            if (_swingTrail != null)
            {
                _swingTrail.emitting = true;
            }
        }

        private void ActivateHitbox()
        {
            _hitboxActive = true;
            if (_hitbox != null)
            {
                // Apply strength skill bonus to damage multiplier
                float strengthBonus = SkillModifiers.GetMeleeDamageBonus();
                _hitbox.SetMultiplier(1f + strengthBonus);
                _hitbox.Activate();
            }
        }

        private void DeactivateHitbox()
        {
            _hitboxActive = false;
            if (_hitbox != null)
            {
                _hitbox.Deactivate();
            }
        }

        private void EndAttack()
        {
            _isAttacking = false;

            // Disable trail
            if (_swingTrail != null)
            {
                _swingTrail.emitting = false;
            }
        }

        private void OnWeaponHit(IDamageable target, float damage)
        {
            // Play hit sound
            if (_hitSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(_hitSound);
            }

            // Play hit effect
            if (_hitEffect != null)
            {
                _hitEffect.Play();
            }

            Debug.Log($"[MeleeWeapon] Hit target for {damage} damage!");
        }

        /// <summary>
        /// Sets weapon damage (for upgrades, buffs, etc.).
        /// </summary>
        public void SetDamage(float damage)
        {
            _baseDamage = damage;
            if (_hitbox != null)
            {
                _hitbox.SetDamage(damage);
            }
        }

        /// <summary>
        /// Sets a damage multiplier (for crits, buffs, etc.).
        /// </summary>
        public void SetDamageMultiplier(float multiplier)
        {
            if (_hitbox != null)
            {
                _hitbox.SetMultiplier(multiplier);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw attack range
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _attackRange);
        }
#endif
    }
}
