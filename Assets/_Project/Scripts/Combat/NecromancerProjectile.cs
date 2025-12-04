using UnityEngine;

namespace VoxelRPG.Combat
{
    /// <summary>
    /// Projectile fired by the Necromancer.
    /// Travels toward target, deals damage on impact, and spawns effects.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class NecromancerProjectile : MonoBehaviour
    {
        [Header("Damage")]
        [SerializeField] private float _damage = 10f;
        [SerializeField] private LayerMask _targetLayers = -1;
        [SerializeField] private LayerMask _blockingLayers = -1;

        [Header("Movement")]
        [SerializeField] private float _speed = 12f;
        [SerializeField] private bool _homing;
        [SerializeField] private float _homingStrength = 2f;
        [SerializeField] private float _maxLifetime = 5f;

        [Header("Effects")]
        [SerializeField] private ParticleSystem _trailEffect;
        [SerializeField] private ParticleSystem _impactEffect;
        [SerializeField] private AudioClip _launchSound;
        [SerializeField] private AudioClip _impactSound;

        [Header("Visual")]
        [SerializeField] private GameObject _projectileVisual;
        [SerializeField] private Light _glowLight;
        [SerializeField] private Color _glowColor = new Color(0.5f, 0f, 0.8f);

        // Runtime state
        private Rigidbody _rigidbody;
        private Collider _collider;
        private Transform _target;
        private GameObject _owner;
        private float _lifetimeTimer;
        private bool _hasHit;

        /// <summary>
        /// Configures the projectile with damage and owner.
        /// </summary>
        public void Initialize(float damage, GameObject owner, Transform target = null)
        {
            _damage = damage;
            _owner = owner;
            _target = target;
        }

        /// <summary>
        /// Sets the projectile's target for homing behavior.
        /// </summary>
        public void SetTarget(Transform target)
        {
            _target = target;
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();

            // Configure rigidbody for projectile behavior
            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = false;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Ensure collider is trigger
            _collider.isTrigger = true;

            // Set up glow
            if (_glowLight != null)
            {
                _glowLight.color = _glowColor;
            }
        }

        private void Start()
        {
            // Play launch sound
            if (_launchSound != null)
            {
                AudioSource.PlayClipAtPoint(_launchSound, transform.position);
            }

            // Start trail
            if (_trailEffect != null)
            {
                _trailEffect.Play();
            }

            // Set initial velocity
            _rigidbody.linearVelocity = transform.forward * _speed;
        }

        private void Update()
        {
            if (_hasHit) return;

            // Update lifetime
            _lifetimeTimer += Time.deltaTime;
            if (_lifetimeTimer >= _maxLifetime)
            {
                DestroyProjectile(false);
                return;
            }

            // Homing behavior
            if (_homing && _target != null)
            {
                Vector3 directionToTarget = (_target.position - transform.position).normalized;
                Vector3 currentDirection = _rigidbody.linearVelocity.normalized;
                Vector3 newDirection = Vector3.Lerp(currentDirection, directionToTarget, _homingStrength * Time.deltaTime);

                _rigidbody.linearVelocity = newDirection * _speed;
                transform.rotation = Quaternion.LookRotation(newDirection);
            }

            // Animate glow
            if (_glowLight != null)
            {
                _glowLight.intensity = 1f + Mathf.Sin(Time.time * 10f) * 0.3f;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasHit) return;

            // Skip owner
            if (_owner != null && (other.gameObject == _owner || other.transform.IsChildOf(_owner.transform)))
            {
                return;
            }

            // Check if hit blocking layer (walls, ground, etc.)
            if ((_blockingLayers.value & (1 << other.gameObject.layer)) != 0)
            {
                OnHitEnvironment(other);
                return;
            }

            // Check if hit valid target
            if ((_targetLayers.value & (1 << other.gameObject.layer)) != 0)
            {
                // Try to find damageable via hurtbox first
                var hurtbox = other.GetComponent<Hurtbox>();
                if (hurtbox != null && hurtbox.Damageable != null)
                {
                    OnHitTarget(hurtbox.Damageable, other);
                    return;
                }

                // Fallback: direct damageable check
                var damageable = other.GetComponent<IDamageable>();
                if (damageable == null)
                {
                    damageable = other.GetComponentInParent<IDamageable>();
                }

                if (damageable != null && damageable.IsAlive)
                {
                    OnHitTarget(damageable, other);
                }
            }
        }

        private void OnHitTarget(IDamageable target, Collider hitCollider)
        {
            _hasHit = true;

            // Deal damage
            target.TakeDamage(_damage, _owner);

            Debug.Log($"[NecromancerProjectile] Hit target for {_damage} damage!");

            // Spawn impact effect at hit point
            SpawnImpactEffect(hitCollider.ClosestPoint(transform.position));

            DestroyProjectile(true);
        }

        private void OnHitEnvironment(Collider hitCollider)
        {
            _hasHit = true;

            Debug.Log("[NecromancerProjectile] Hit environment");

            // Spawn impact effect
            SpawnImpactEffect(hitCollider.ClosestPoint(transform.position));

            DestroyProjectile(true);
        }

        private void SpawnImpactEffect(Vector3 position)
        {
            // Play impact sound
            if (_impactSound != null)
            {
                AudioSource.PlayClipAtPoint(_impactSound, position);
            }

            // Spawn impact particles
            if (_impactEffect != null)
            {
                var impact = Instantiate(_impactEffect, position, Quaternion.identity);
                impact.Play();
                Destroy(impact.gameObject, impact.main.duration + 1f);
            }
        }

        private void DestroyProjectile(bool immediate)
        {
            // Stop trail
            if (_trailEffect != null)
            {
                _trailEffect.Stop();
            }

            // Hide visual immediately
            if (_projectileVisual != null)
            {
                _projectileVisual.SetActive(false);
            }

            // Disable light
            if (_glowLight != null)
            {
                _glowLight.enabled = false;
            }

            // Stop movement
            _rigidbody.linearVelocity = Vector3.zero;
            _collider.enabled = false;

            // Destroy after trail fades
            float destroyDelay = immediate ? 0.5f : 0f;
            if (_trailEffect != null)
            {
                destroyDelay = Mathf.Max(destroyDelay, _trailEffect.main.startLifetime.constantMax);
            }

            Destroy(gameObject, destroyDelay);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Draw projectile direction
            Gizmos.color = _glowColor;
            Gizmos.DrawRay(transform.position, transform.forward * 2f);
            Gizmos.DrawWireSphere(transform.position, 0.2f);
        }
#endif
    }
}
