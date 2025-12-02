using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using VoxelRPG.Core;

namespace VoxelRPG.Combat
{
    /// <summary>
    /// Spawns monsters during night time around the player.
    /// Manages spawn rates, limits, and despawning during day.
    /// </summary>
    public class MonsterSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [Tooltip("Monster types that can spawn")]
        [SerializeField] private MonsterDefinition[] _monsterTypes;

        [Tooltip("Maximum active monsters at once")]
        [SerializeField] private int _maxActiveMonsters = 20;

        [Tooltip("Spawn check interval in seconds")]
        [SerializeField] private float _spawnInterval = 5f;

        [Tooltip("Monsters to spawn per spawn event")]
        [SerializeField] private int _spawnsPerEvent = 1;

        [Header("Spawn Distance")]
        [Tooltip("Minimum distance from player to spawn")]
        [SerializeField] private float _minSpawnDistance = 20f;

        [Tooltip("Maximum distance from player to spawn")]
        [SerializeField] private float _maxSpawnDistance = 40f;

        [Tooltip("Height above ground to spawn")]
        [SerializeField] private float _spawnHeight = 1f;

        [Header("Despawn Settings")]
        [Tooltip("Distance from player before despawning")]
        [SerializeField] private float _despawnDistance = 60f;

        [Tooltip("Whether to despawn monsters at dawn")]
        [SerializeField] private bool _despawnAtDawn = true;

        [Tooltip("Whether night-only monsters burn at dawn instead of despawning")]
        [SerializeField] private bool _burnInsteadOfDespawn = true;

        [Header("References")]
        [Tooltip("Transform to spawn monsters around (usually player)")]
        [SerializeField] private Transform _spawnCenter;

        [Header("Debug")]
        [SerializeField] private bool _forceSpawning;
        [SerializeField] private bool _showDebugGizmos = true;

        private TimeManager _timeManager;
        private List<GameObject> _activeMonsters = new List<GameObject>();
        private float _spawnTimer;
        private bool _wasNight;

        /// <summary>
        /// Number of currently active monsters.
        /// </summary>
        public int ActiveMonsterCount => _activeMonsters.Count;

        /// <summary>
        /// Whether spawning is currently allowed.
        /// </summary>
        public bool CanSpawn => (_forceSpawning || (_timeManager != null && _timeManager.IsNight))
                               && _activeMonsters.Count < _maxActiveMonsters;

        private void Awake()
        {
            ServiceLocator.Register<MonsterSpawner>(this);
        }

        private void Start()
        {
            ServiceLocator.TryGet(out _timeManager);

            if (_timeManager != null)
            {
                _timeManager.OnNightStarted += OnNightStarted;
                _timeManager.OnDayStarted += OnDayStarted;
                _wasNight = _timeManager.IsNight;
            }

            // Find player if not assigned
            if (_spawnCenter == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    _spawnCenter = player.transform;
                }
            }

            if (_monsterTypes == null || _monsterTypes.Length == 0)
            {
                Debug.LogWarning("[MonsterSpawner] No monster types assigned!");
            }
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<MonsterSpawner>();

            if (_timeManager != null)
            {
                _timeManager.OnNightStarted -= OnNightStarted;
                _timeManager.OnDayStarted -= OnDayStarted;
            }
        }

        private void Update()
        {
            // Clean up destroyed monsters
            CleanupDestroyedMonsters();

            // Check for despawning distant monsters
            DespawnDistantMonsters();

            // Spawn check
            if (_spawnCenter != null)
            {
                if (CanSpawn)
                {
                    _spawnTimer -= Time.deltaTime;
                    if (_spawnTimer <= 0)
                    {
                        _spawnTimer = _spawnInterval;
                        TrySpawnMonsters();
                    }
                }
            }
            else
            {
                // Try to find player if spawn center not set
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    _spawnCenter = player.transform;
                    Debug.Log($"[MonsterSpawner] Found player, setting as spawn center");
                }
            }
        }

        private void OnNightStarted()
        {
            Debug.Log("[MonsterSpawner] Night started - monster spawning enabled");
            _spawnTimer = 2f; // Brief delay before first spawn
        }

        private void OnDayStarted()
        {
            Debug.Log("[MonsterSpawner] Day started - handling monster despawn");

            if (_despawnAtDawn)
            {
                HandleDayDespawn();
            }
        }

        private void HandleDayDespawn()
        {
            List<GameObject> toDespawn = new List<GameObject>();

            foreach (var monster in _activeMonsters)
            {
                if (monster == null) continue;

                var ai = monster.GetComponent<IMonsterAI>();
                if (ai?.Definition != null && ai.Definition.NightOnly)
                {
                    if (_burnInsteadOfDespawn && ai.Definition.BurnsInDaylight)
                    {
                        // Monster will burn naturally via BasicMonsterAI
                        continue;
                    }
                    else
                    {
                        toDespawn.Add(monster);
                    }
                }
            }

            foreach (var monster in toDespawn)
            {
                DespawnMonster(monster);
            }
        }

        private void TrySpawnMonsters()
        {
            if (_monsterTypes == null || _monsterTypes.Length == 0) return;
            if (_spawnCenter == null) return;

            for (int i = 0; i < _spawnsPerEvent && _activeMonsters.Count < _maxActiveMonsters; i++)
            {
                // Select monster type by weight
                var definition = SelectMonsterType();
                if (definition == null) continue;

                // Skip day-only spawning of night-only monsters
                if (definition.NightOnly && _timeManager != null && !_timeManager.IsNight && !_forceSpawning)
                {
                    continue;
                }

                // Find spawn position
                if (TryFindSpawnPosition(out Vector3 spawnPos))
                {
                    SpawnMonster(definition, spawnPos);
                }
            }
        }

        private MonsterDefinition SelectMonsterType()
        {
            float totalWeight = 0f;
            foreach (var def in _monsterTypes)
            {
                if (def != null)
                {
                    totalWeight += def.SpawnWeight;
                }
            }

            if (totalWeight <= 0) return _monsterTypes[0];

            float random = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var def in _monsterTypes)
            {
                if (def == null) continue;
                cumulative += def.SpawnWeight;
                if (random <= cumulative)
                {
                    return def;
                }
            }

            return _monsterTypes[0];
        }

        private bool TryFindSpawnPosition(out Vector3 position)
        {
            position = Vector3.zero;

            for (int attempt = 0; attempt < 10; attempt++)
            {
                // Random angle around player
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = Random.Range(_minSpawnDistance, _maxSpawnDistance);

                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * distance,
                    0f,
                    Mathf.Sin(angle) * distance
                );

                Vector3 testPos = _spawnCenter.position + offset;

                // Try to find valid NavMesh position
                if (NavMesh.SamplePosition(testPos, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                {
                    position = hit.position + Vector3.up * _spawnHeight;
                    return true;
                }

                // Fallback: raycast to find ground
                testPos.y = _spawnCenter.position.y + 50f;
                if (Physics.Raycast(testPos, Vector3.down, out RaycastHit groundHit, 100f))
                {
                    position = groundHit.point + Vector3.up * _spawnHeight;
                    return true;
                }
            }

            Debug.LogWarning("[MonsterSpawner] Failed to find spawn position after 10 attempts");
            return false;
        }

        /// <summary>
        /// Spawns a monster at the specified position.
        /// </summary>
        public GameObject SpawnMonster(MonsterDefinition definition, Vector3 position)
        {
            if (definition == null || definition.Prefab == null)
            {
                Debug.LogWarning($"[MonsterSpawner] Cannot spawn: definition or prefab is null");
                return null;
            }

            // Instantiate monster
            var monster = Instantiate(definition.Prefab, position, Quaternion.identity);
            monster.name = $"{definition.DisplayName}_{_activeMonsters.Count}";

            // Initialize AI
            var ai = monster.GetComponent<IMonsterAI>();
            if (ai != null)
            {
                ai.Initialize(definition);
            }

            _activeMonsters.Add(monster);

            Debug.Log($"[MonsterSpawner] Spawned {definition.DisplayName} at {position}");

            return monster;
        }

        /// <summary>
        /// Spawns a group of monsters around a position.
        /// </summary>
        public void SpawnGroup(MonsterDefinition definition, Vector3 centerPosition)
        {
            if (definition == null) return;

            int groupSize = definition.GetRandomGroupSize();
            float groupSpread = 3f;

            for (int i = 0; i < groupSize && _activeMonsters.Count < _maxActiveMonsters; i++)
            {
                Vector3 offset = Random.insideUnitSphere * groupSpread;
                offset.y = 0f;

                Vector3 spawnPos = centerPosition + offset;

                if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, groupSpread, NavMesh.AllAreas))
                {
                    SpawnMonster(definition, hit.position + Vector3.up * _spawnHeight);
                }
            }
        }

        private void DespawnDistantMonsters()
        {
            if (_spawnCenter == null) return;

            List<GameObject> toDespawn = new List<GameObject>();

            foreach (var monster in _activeMonsters)
            {
                if (monster == null) continue;

                float distance = Vector3.Distance(monster.transform.position, _spawnCenter.position);
                if (distance > _despawnDistance)
                {
                    toDespawn.Add(monster);
                }
            }

            foreach (var monster in toDespawn)
            {
                DespawnMonster(monster);
            }
        }

        private void DespawnMonster(GameObject monster)
        {
            if (monster == null) return;

            _activeMonsters.Remove(monster);
            Destroy(monster);
        }

        private void CleanupDestroyedMonsters()
        {
            _activeMonsters.RemoveAll(m => m == null);
        }

        /// <summary>
        /// Despawns all active monsters.
        /// </summary>
        public void DespawnAll()
        {
            foreach (var monster in _activeMonsters)
            {
                if (monster != null)
                {
                    Destroy(monster);
                }
            }
            _activeMonsters.Clear();
        }

        /// <summary>
        /// Sets the spawn center (usually player transform).
        /// </summary>
        public void SetSpawnCenter(Transform center)
        {
            _spawnCenter = center;
            Debug.Log($"[MonsterSpawner] Spawn center set to: {center?.name ?? "null"}");
        }

        /// <summary>
        /// Sets the monster types that can spawn.
        /// </summary>
        public void SetMonsterTypes(MonsterDefinition[] monsterTypes)
        {
            _monsterTypes = monsterTypes;
            Debug.Log($"[MonsterSpawner] Monster types set: {monsterTypes?.Length ?? 0} types");
        }

        /// <summary>
        /// Enables or disables force spawning (ignores day/night).
        /// </summary>
        public void SetForceSpawning(bool force)
        {
            _forceSpawning = force;
            Debug.Log($"[MonsterSpawner] Force spawning: {force}");
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!_showDebugGizmos) return;
            if (_spawnCenter == null) return;

            // Min spawn distance
            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(_spawnCenter.position, _minSpawnDistance);

            // Max spawn distance
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(_spawnCenter.position, _maxSpawnDistance);

            // Despawn distance
            Gizmos.color = new Color(1f, 0f, 0f, 0.1f);
            Gizmos.DrawWireSphere(_spawnCenter.position, _despawnDistance);
        }
#endif
    }
}
