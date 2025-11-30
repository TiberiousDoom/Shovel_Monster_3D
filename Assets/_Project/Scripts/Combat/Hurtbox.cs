using UnityEngine;

namespace VoxelRPG.Combat
{
    /// <summary>
    /// Passive damage-receiving collision zone.
    /// Always active, represents a vulnerable area on an entity.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Hurtbox : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Damage multiplier for hits on this hurtbox (e.g., 2.0 for headshots)")]
        [SerializeField] private float _damageMultiplier = 1f;

        [Tooltip("Whether this hurtbox is currently active")]
        [SerializeField] private bool _isActive = true;

        [Header("Owner")]
        [Tooltip("The damageable entity this hurtbox belongs to")]
        [SerializeField] private MonoBehaviour _damageableComponent;

        private Collider _collider;
        private IDamageable _damageable;

        /// <summary>
        /// The damageable entity this hurtbox is attached to.
        /// </summary>
        public IDamageable Damageable => _damageable;

        /// <summary>
        /// Damage multiplier for this specific hurtbox.
        /// </summary>
        public float DamageMultiplier => _damageMultiplier;

        /// <summary>
        /// Whether this hurtbox is currently active.
        /// </summary>
        public bool IsActive => _isActive;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;

            // Find damageable
            if (_damageableComponent != null)
            {
                _damageable = _damageableComponent as IDamageable;
            }

            if (_damageable == null)
            {
                _damageable = GetComponent<IDamageable>();
            }

            if (_damageable == null)
            {
                _damageable = GetComponentInParent<IDamageable>();
            }

            if (_damageable == null)
            {
                Debug.LogWarning($"[Hurtbox] No IDamageable found on {gameObject.name} or parent!");
            }
        }

        /// <summary>
        /// Sets whether this hurtbox is active.
        /// </summary>
        public void SetActive(bool active)
        {
            _isActive = active;
            _collider.enabled = active;
        }

        /// <summary>
        /// Sets the damage multiplier for this hurtbox.
        /// </summary>
        public void SetMultiplier(float multiplier)
        {
            _damageMultiplier = multiplier;
        }

        /// <summary>
        /// Applies damage to the associated damageable with this hurtbox's multiplier.
        /// </summary>
        /// <param name="baseDamage">Base damage before multiplier.</param>
        /// <param name="source">Source of the damage.</param>
        /// <returns>Actual damage dealt.</returns>
        public float ApplyDamage(float baseDamage, GameObject source = null)
        {
            if (!_isActive || _damageable == null || !_damageable.IsAlive)
            {
                return 0f;
            }

            float finalDamage = baseDamage * _damageMultiplier;
            return _damageable.TakeDamage(finalDamage, source);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var col = GetComponent<Collider>();
            if (col == null) return;

            Gizmos.color = _isActive ? new Color(0f, 1f, 0f, 0.3f) : new Color(0.5f, 0.5f, 0.5f, 0.2f);

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
