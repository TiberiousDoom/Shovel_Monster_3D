using UnityEngine;

namespace VoxelRPG.Combat
{
    /// <summary>
    /// ScriptableObject defining monster stats and behavior parameters.
    /// Used by IMonsterAI implementations to configure behavior.
    /// </summary>
    [CreateAssetMenu(fileName = "New Monster", menuName = "VoxelRPG/Combat/Monster Definition")]
    public class MonsterDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique identifier for this monster type")]
        [SerializeField] private string _id;

        [Tooltip("Display name for UI")]
        [SerializeField] private string _displayName;

        [Tooltip("Monster description")]
        [TextArea(2, 4)]
        [SerializeField] private string _description;

        [Header("Stats")]
        [Tooltip("Maximum health points")]
        [SerializeField] private float _maxHealth = 50f;

        [Tooltip("Base damage per attack")]
        [SerializeField] private float _attackDamage = 10f;

        [Tooltip("Time between attacks in seconds")]
        [SerializeField] private float _attackCooldown = 1.5f;

        [Tooltip("Attack range in units")]
        [SerializeField] private float _attackRange = 2f;

        [Header("Movement")]
        [Tooltip("Movement speed while wandering")]
        [SerializeField] private float _wanderSpeed = 2f;

        [Tooltip("Movement speed while chasing")]
        [SerializeField] private float _chaseSpeed = 5f;

        [Tooltip("Detection range for targets")]
        [SerializeField] private float _detectionRange = 15f;

        [Tooltip("Distance before giving up chase")]
        [SerializeField] private float _loseTargetRange = 25f;

        [Header("Behavior")]
        [Tooltip("How aggressive this monster is (0=passive, 1=always hostile)")]
        [Range(0f, 1f)]
        [SerializeField] private float _aggression = 0.8f;

        [Tooltip("Whether this monster flees at low health")]
        [SerializeField] private bool _fleesAtLowHealth;

        [Tooltip("Health percentage to start fleeing")]
        [Range(0f, 1f)]
        [SerializeField] private float _fleeHealthThreshold = 0.2f;

        [Tooltip("Whether this monster can attack structures")]
        [SerializeField] private bool _attacksStructures;

        [Tooltip("Whether this monster only spawns at night")]
        [SerializeField] private bool _nightOnly = true;

        [Tooltip("Whether this monster burns in daylight")]
        [SerializeField] private bool _burnsInDaylight;

        [Header("Spawning")]
        [Tooltip("Spawn weight relative to other monsters")]
        [SerializeField] private float _spawnWeight = 1f;

        [Tooltip("Minimum spawn group size")]
        [SerializeField] private int _minGroupSize = 1;

        [Tooltip("Maximum spawn group size")]
        [SerializeField] private int _maxGroupSize = 3;

        [Header("Loot")]
        [Tooltip("Experience points when killed")]
        [SerializeField] private int _experienceValue = 10;

        // Note: Loot drops will use ItemDefinition references when inventory is integrated

        [Header("Audio")]
        [Tooltip("Sound played when monster attacks")]
        [SerializeField] private AudioClip _attackSound;

        [Tooltip("Sound played when monster is hurt")]
        [SerializeField] private AudioClip _hurtSound;

        [Tooltip("Sound played when monster dies")]
        [SerializeField] private AudioClip _deathSound;

        [Tooltip("Ambient/idle sounds")]
        [SerializeField] private AudioClip[] _ambientSounds;

        [Header("Visuals")]
        [Tooltip("Prefab to spawn for this monster")]
        [SerializeField] private GameObject _prefab;

        // Properties
        public string Id => string.IsNullOrEmpty(_id) ? name : _id;
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public string Description => _description;

        public float MaxHealth => _maxHealth;
        public float AttackDamage => _attackDamage;
        public float AttackCooldown => _attackCooldown;
        public float AttackRange => _attackRange;

        public float WanderSpeed => _wanderSpeed;
        public float ChaseSpeed => _chaseSpeed;
        public float DetectionRange => _detectionRange;
        public float LoseTargetRange => _loseTargetRange;

        public float Aggression => _aggression;
        public bool FleesAtLowHealth => _fleesAtLowHealth;
        public float FleeHealthThreshold => _fleeHealthThreshold;
        public bool AttacksStructures => _attacksStructures;
        public bool NightOnly => _nightOnly;
        public bool BurnsInDaylight => _burnsInDaylight;

        public float SpawnWeight => _spawnWeight;
        public int MinGroupSize => _minGroupSize;
        public int MaxGroupSize => _maxGroupSize;

        public int ExperienceValue => _experienceValue;

        public AudioClip AttackSound => _attackSound;
        public AudioClip HurtSound => _hurtSound;
        public AudioClip DeathSound => _deathSound;
        public AudioClip[] AmbientSounds => _ambientSounds;

        public GameObject Prefab => _prefab;

        /// <summary>
        /// Gets a random group size for spawning.
        /// </summary>
        public int GetRandomGroupSize()
        {
            return Random.Range(_minGroupSize, _maxGroupSize + 1);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _maxHealth = Mathf.Max(1f, _maxHealth);
            _attackDamage = Mathf.Max(0f, _attackDamage);
            _attackCooldown = Mathf.Max(0.1f, _attackCooldown);
            _attackRange = Mathf.Max(0.5f, _attackRange);
            _wanderSpeed = Mathf.Max(0f, _wanderSpeed);
            _chaseSpeed = Mathf.Max(0f, _chaseSpeed);
            _detectionRange = Mathf.Max(1f, _detectionRange);
            _loseTargetRange = Mathf.Max(_detectionRange, _loseTargetRange);
            _spawnWeight = Mathf.Max(0f, _spawnWeight);
            _minGroupSize = Mathf.Max(1, _minGroupSize);
            _maxGroupSize = Mathf.Max(_minGroupSize, _maxGroupSize);
            _experienceValue = Mathf.Max(0, _experienceValue);
        }
#endif
    }
}
