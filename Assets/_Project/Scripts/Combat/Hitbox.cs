using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelRPG.Combat
{
    /// <summary>
    /// Active damage-dealing collision zone.
    /// Activated during attacks to deal damage to overlapping Hurtboxes.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Hitbox : MonoBehaviour
    {
        [Header("Damage Settings")]
        [Tooltip("Base damage dealt by this hitbox")]
        [SerializeField] private float _baseDamage = 10f;

        [Tooltip("Damage multiplier for this hitbox")]
        [SerializeField] private float _damageMultiplier = 1f;

        [Tooltip("Knockback force applied")]
        [SerializeField] private float _knockbackForce = 5f;

        [Header("Behavior")]
        [Tooltip("Layers that can be hit")]
        [SerializeField] private LayerMask _targetLayers = -1;

        [Tooltip("Can hit multiple targets per activation")]
        [SerializeField] private bool _multiHit = true;

        [Tooltip("Time before same target can be hit again")]
        [SerializeField] private float _hitCooldown = 0.5f;

        [Header("Owner")]
        [Tooltip("The GameObject that owns this hitbox (for self-damage prevention)")]
        [SerializeField] private GameObject _owner;

        private Collider _collider;
        private HashSet<IDamageable> _hitTargets = new HashSet<IDamageable>();
        private Dictionary<IDamageable, float> _hitCooldowns = new Dictionary<IDamageable, float>();
        private bool _isActive;

        /// <summary>
        /// Current damage this hitbox deals (base * multiplier).
        /// </summary>
        public float Damage => _baseDamage * _damageMultiplier;

        /// <summary>
        /// Whether this hitbox is currently active.
        /// </summary>
        public bool IsActive => _isActive;

        /// <summary>
        /// Event fired when this hitbox hits a target.
        /// </summary>
        public event Action<IDamageable, float> OnHit;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;

            if (_owner == null)
            {
                _owner = transform.root.gameObject;
            }

            // Start inactive
            SetActive(false);
        }

        private void Update()
        {
            // Update cooldowns
            var expired = new List<IDamageable>();
            foreach (var kvp in _hitCooldowns)
            {
                if (Time.time >= kvp.Value)
                {
                    expired.Add(kvp.Key);
                }
            }
            foreach (var target in expired)
            {
                _hitCooldowns.Remove(target);
            }
        }

        /// <summary>
        /// Activates the hitbox to start detecting collisions.
        /// </summary>
        public void Activate()
        {
            SetActive(true);
            _hitTargets.Clear();
        }

        /// <summary>
        /// Deactivates the hitbox.
        /// </summary>
        public void Deactivate()
        {
            SetActive(false);
            _hitTargets.Clear();
        }

        /// <summary>
        /// Sets the hitbox active state.
        /// </summary>
        public void SetActive(bool active)
        {
            _isActive = active;
            _collider.enabled = active;

            if (!active)
            {
                _hitTargets.Clear();
            }
        }

        /// <summary>
        /// Sets the damage for this hitbox.
        /// </summary>
        public void SetDamage(float damage)
        {
            _baseDamage = damage;
        }

        /// <summary>
        /// Sets the damage multiplier.
        /// </summary>
        public void SetMultiplier(float multiplier)
        {
            _damageMultiplier = multiplier;
        }

        /// <summary>
        /// Sets the owner GameObject.
        /// </summary>
        public void SetOwner(GameObject owner)
        {
            _owner = owner;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive) return;
            TryDealDamage(other);
        }

        private void OnTriggerStay(Collider other)
        {
            if (!_isActive) return;
            if (_multiHit)
            {
                TryDealDamage(other);
            }
        }

        private void TryDealDamage(Collider other)
        {
            // Check layer
            if ((_targetLayers.value & (1 << other.gameObject.layer)) == 0) return;

            // Skip owner
            if (_owner != null && (other.gameObject == _owner || other.transform.IsChildOf(_owner.transform)))
            {
                return;
            }

            // Check for hurtbox
            var hurtbox = other.GetComponent<Hurtbox>();
            if (hurtbox != null)
            {
                var damageable = hurtbox.Damageable;
                if (damageable != null && damageable.IsAlive)
                {
                    ProcessHit(damageable, other);
                }
                return;
            }

            // Fallback: check for damageable directly
            var directDamageable = other.GetComponent<IDamageable>();
            if (directDamageable != null && directDamageable.IsAlive)
            {
                ProcessHit(directDamageable, other);
            }
        }

        private void ProcessHit(IDamageable target, Collider other)
        {
            // Check if already hit (single-hit mode)
            if (!_multiHit && _hitTargets.Contains(target)) return;

            // Check hit cooldown
            if (_hitCooldowns.TryGetValue(target, out float cooldownEnd))
            {
                if (Time.time < cooldownEnd) return;
            }

            // Deal damage
            float damageDealt = target.TakeDamage(Damage, _owner);

            // Apply knockback
            if (_knockbackForce > 0 && other.attachedRigidbody != null)
            {
                Vector3 knockbackDir = (other.transform.position - transform.position).normalized;
                other.attachedRigidbody.AddForce(knockbackDir * _knockbackForce, ForceMode.Impulse);
            }

            // Track hit
            _hitTargets.Add(target);
            _hitCooldowns[target] = Time.time + _hitCooldown;

            // Fire event
            OnHit?.Invoke(target, damageDealt);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var col = GetComponent<Collider>();
            if (col == null) return;

            Gizmos.color = _isActive ? new Color(1f, 0f, 0f, 0.5f) : new Color(1f, 0.5f, 0f, 0.2f);

            if (col is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
            }
            else if (col is CapsuleCollider capsule)
            {
                Gizmos.DrawWireSphere(transform.position + capsule.center, capsule.radius);
            }
        }
#endif
    }
}
