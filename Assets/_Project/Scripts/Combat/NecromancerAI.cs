using UnityEngine;
using UnityEngine.AI;
using VoxelRPG.Core;

namespace VoxelRPG.Combat
{
    /// <summary>
    /// Necromancer AI that extends basic monster behavior with summoning abilities.
    /// Summons skeleton minions and prefers to stay at range.
    /// </summary>
    [RequireComponent(typeof(MonsterHealth))]
    public class NecromancerAI : MonoBehaviour, IMonsterAI
    {
        [Header("Configuration")]
        [SerializeField] private MonsterDefinition _definition;

        [Header("Detection")]
        [SerializeField] private LayerMask _targetLayers = -1;
        [SerializeField] private float _targetUpdateInterval = 0.5f;

        [Header("Movement")]
        [SerializeField] private float _wanderRadius = 10f;
        [SerializeField] private float _wanderWaitTime = 3f;
        [SerializeField] private float _preferredCombatDistance = 8f;
        [SerializeField] private float _retreatDistance = 4f;

        [Header("Summoning")]
        [SerializeField] private GameObject _minionPrefab;
        [SerializeField] private int _maxMinions = 3;
        [SerializeField] private float _summonCooldown = 10f;
        [SerializeField] private float _summonRadius = 3f;
        [SerializeField] private Transform _summonPoint;

        [Header("Combat")]
        [SerializeField] private Transform _attackPoint;
        [SerializeField] private float _attackRadius = 1f;
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private float _projectileSpeed = 10f;

        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem _summonEffect;
        [SerializeField] private ParticleSystem _castEffect;

        [Header("Debug")]
        [SerializeField] private bool _showDebugGizmos = true;

        // Cached components
        private MonsterHealth _health;
        private NavMeshAgent _navAgent;
        private Animator _animator;

        // State tracking
        private MonsterState _currentState = MonsterState.Idle;
        private Transform _currentTarget;
        private Vector3 _spawnPosition;
        private Vector3 _wanderDestination;

        // Timers
        private float _stateTimer;
        private float _targetSearchTimer;
        private float _attackCooldownTimer;
        private float _summonCooldownTimer;
        private float _wanderTimer;

        // Minion tracking
        private int _activeMinions;

        // Knockback
        private Vector3 _knockbackVelocity;
        private float _knockbackTimer;

        // Time manager reference for daylight check
        private TimeManager _timeManager;

        // Animator hashes
        private static readonly int AnimIdle = Animator.StringToHash("Idle");
        private static readonly int AnimWalk = Animator.StringToHash("Walk");
        private static readonly int AnimAttack = Animator.StringToHash("Attack");
        private static readonly int AnimSummon = Animator.StringToHash("Summon");
        private static readonly int AnimDeath = Animator.StringToHash("Death");

        /// <inheritdoc/>
        public MonsterDefinition Definition => _definition;

        /// <inheritdoc/>
        public MonsterState CurrentState => _currentState;

        /// <inheritdoc/>
        public Transform CurrentTarget => _currentTarget;

        /// <inheritdoc/>
        public bool IsAlive => _health != null && _health.IsAlive;

        /// <summary>
        /// Current number of active minions.
        /// </summary>
        public int ActiveMinions => _activeMinions;

        private void Awake()
        {
            _health = GetComponent<MonsterHealth>();
            _navAgent = GetComponent<NavMeshAgent>();
            _animator = GetComponent<Animator>();

            if (_attackPoint == null)
            {
                _attackPoint = transform;
            }

            if (_summonPoint == null)
            {
                _summonPoint = transform;
            }
        }

        private void Start()
        {
            _spawnPosition = transform.position;

            ServiceLocator.TryGet(out _timeManager);

            if (_definition != null)
            {
                Initialize(_definition);
            }

            ChangeState(MonsterState.Idle);
        }

        private void Update()
        {
            if (!IsAlive)
            {
                if (_currentState != MonsterState.Dead)
                {
                    ChangeState(MonsterState.Dead);
                }
                return;
            }

            // Update timers
            UpdateTimers();

            // Check for daylight burning
            if (_definition != null && _definition.BurnsInDaylight)
            {
                CheckDaylightBurn();
            }

            // Handle knockback
            if (_knockbackTimer > 0)
            {
                ApplyKnockback();
                return;
            }

            // Periodic target search
            if (_targetSearchTimer <= 0)
            {
                SearchForTarget();
                _targetSearchTimer = _targetUpdateInterval;
            }

            // State machine
            switch (_currentState)
            {
                case MonsterState.Idle:
                    UpdateIdle();
                    break;
                case MonsterState.Wandering:
                    UpdateWandering();
                    break;
                case MonsterState.Chasing:
                    UpdateCombat();
                    break;
                case MonsterState.Attacking:
                    UpdateAttacking();
                    break;
                case MonsterState.Fleeing:
                    UpdateFleeing();
                    break;
                case MonsterState.Returning:
                    UpdateReturning();
                    break;
                case MonsterState.Stunned:
                    UpdateStunned();
                    break;
            }
        }

        private void UpdateTimers()
        {
            _stateTimer += Time.deltaTime;
            _targetSearchTimer -= Time.deltaTime;
            _attackCooldownTimer -= Time.deltaTime;
            _summonCooldownTimer -= Time.deltaTime;
            _wanderTimer -= Time.deltaTime;
            _knockbackTimer -= Time.deltaTime;
        }

        /// <inheritdoc/>
        public void Initialize(MonsterDefinition definition)
        {
            _definition = definition;

            if (_health != null)
            {
                _health.Initialize(definition);
            }

            if (_navAgent != null)
            {
                _navAgent.speed = definition.WanderSpeed;
            }

            Debug.Log($"[NecromancerAI] Initialized: {definition.DisplayName}");
        }

        /// <inheritdoc/>
        public void SetTarget(Transform target)
        {
            _currentTarget = target;
            if (target != null && _currentState != MonsterState.Attacking)
            {
                ChangeState(MonsterState.Chasing);
            }
        }

        /// <inheritdoc/>
        public void ClearTarget()
        {
            _currentTarget = null;
            if (_currentState == MonsterState.Chasing || _currentState == MonsterState.Attacking)
            {
                ChangeState(MonsterState.Returning);
            }
        }

        /// <inheritdoc/>
        public void OnDamaged(float damage, Vector3 knockback)
        {
            if (!IsAlive) return;

            // Apply knockback
            if (knockback.sqrMagnitude > 0.01f)
            {
                _knockbackVelocity = knockback * 3f;
                _knockbackTimer = 0.15f;
            }

            // Necromancers are cowardly - flee when damaged
            if (_definition != null && _definition.FleesAtLowHealth)
            {
                if (_health.HealthNormalized <= _definition.FleeHealthThreshold)
                {
                    ChangeState(MonsterState.Fleeing);
                }
            }

            // Try to summon minions when hurt
            if (_summonCooldownTimer <= 0 && _activeMinions < _maxMinions)
            {
                TrySummonMinion();
            }
        }

        /// <inheritdoc/>
        public void OnDeath()
        {
            ChangeState(MonsterState.Dead);

            // Stop movement
            if (_navAgent != null && _navAgent.isOnNavMesh)
            {
                _navAgent.isStopped = true;
                _navAgent.enabled = false;
            }

            // Play death animation
            if (_animator != null)
            {
                _animator.SetTrigger(AnimDeath);
            }

            Debug.Log($"[NecromancerAI] {_definition?.DisplayName ?? "Necromancer"} died!");

            // Destroy after delay
            Destroy(gameObject, 3f);
        }

        /// <inheritdoc/>
        public void ForceState(MonsterState newState)
        {
            ChangeState(newState);
        }

        /// <summary>
        /// Called by minions when they die.
        /// </summary>
        public void OnMinionDeath()
        {
            _activeMinions = Mathf.Max(0, _activeMinions - 1);
        }

        private void ChangeState(MonsterState newState)
        {
            if (_currentState == newState) return;

            _currentState = newState;
            _stateTimer = 0f;

            OnEnterState(newState);
        }

        private void OnEnterState(MonsterState state)
        {
            switch (state)
            {
                case MonsterState.Idle:
                    StopMovement();
                    SetAnimation(AnimIdle);
                    break;

                case MonsterState.Wandering:
                    SetNewWanderDestination();
                    if (_navAgent != null)
                    {
                        _navAgent.speed = _definition?.WanderSpeed ?? 2f;
                    }
                    SetAnimation(AnimWalk);
                    break;

                case MonsterState.Chasing:
                    if (_navAgent != null)
                    {
                        _navAgent.speed = _definition?.ChaseSpeed ?? 5f;
                    }
                    SetAnimation(AnimWalk);
                    break;

                case MonsterState.Attacking:
                    StopMovement();
                    break;

                case MonsterState.Fleeing:
                    if (_navAgent != null)
                    {
                        _navAgent.speed = _definition?.ChaseSpeed ?? 5f;
                    }
                    SetFleeDestination();
                    SetAnimation(AnimWalk);
                    break;

                case MonsterState.Returning:
                    if (_navAgent != null)
                    {
                        _navAgent.speed = _definition?.WanderSpeed ?? 2f;
                        SetDestination(_spawnPosition);
                    }
                    SetAnimation(AnimWalk);
                    break;

                case MonsterState.Dead:
                    StopMovement();
                    break;
            }
        }

        #region State Updates

        private void UpdateIdle()
        {
            if (_currentTarget != null)
            {
                ChangeState(MonsterState.Chasing);
                return;
            }

            if (_stateTimer >= 2f)
            {
                ChangeState(MonsterState.Wandering);
            }
        }

        private void UpdateWandering()
        {
            if (_currentTarget != null)
            {
                ChangeState(MonsterState.Chasing);
                return;
            }

            if (HasReachedDestination())
            {
                if (_wanderTimer <= 0)
                {
                    _wanderTimer = _wanderWaitTime;
                    SetNewWanderDestination();
                }
            }
        }

        private void UpdateCombat()
        {
            if (_currentTarget == null)
            {
                ChangeState(MonsterState.Returning);
                return;
            }

            float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.position);

            // Check if target is too far
            if (distanceToTarget > (_definition?.LoseTargetRange ?? 25f))
            {
                ClearTarget();
                return;
            }

            // Necromancers prefer to stay at range
            if (distanceToTarget < _retreatDistance)
            {
                // Too close - back away
                SetFleeDestination();
            }
            else if (distanceToTarget > _preferredCombatDistance)
            {
                // Too far - move closer but not too close
                Vector3 targetPos = _currentTarget.position;
                Vector3 direction = (transform.position - targetPos).normalized;
                Vector3 idealPos = targetPos + direction * _preferredCombatDistance;
                SetDestination(idealPos);
            }
            else
            {
                // At ideal range - stop and attack
                StopMovement();
            }

            // Look at target
            LookAtTarget();

            // Try to summon minions
            if (_summonCooldownTimer <= 0 && _activeMinions < _maxMinions)
            {
                TrySummonMinion();
            }
            // Or attack if cooldown ready
            else if (_attackCooldownTimer <= 0)
            {
                PerformAttack();
            }
        }

        private void UpdateAttacking()
        {
            if (_currentTarget == null)
            {
                ChangeState(MonsterState.Idle);
                return;
            }

            LookAtTarget();

            // Return to combat after attack animation
            if (_stateTimer >= 0.5f)
            {
                ChangeState(MonsterState.Chasing);
            }
        }

        private void UpdateFleeing()
        {
            if (_currentTarget != null)
            {
                float distanceToThreat = Vector3.Distance(transform.position, _currentTarget.position);
                if (distanceToThreat > (_definition?.LoseTargetRange ?? 25f))
                {
                    ClearTarget();
                    ChangeState(MonsterState.Returning);
                    return;
                }
            }

            if (_stateTimer > 2f)
            {
                SetFleeDestination();
                _stateTimer = 0f;
            }

            if (HasReachedDestination())
            {
                SetFleeDestination();
            }
        }

        private void UpdateReturning()
        {
            if (_currentTarget != null)
            {
                ChangeState(MonsterState.Chasing);
                return;
            }

            if (Vector3.Distance(transform.position, _spawnPosition) < 2f)
            {
                ChangeState(MonsterState.Idle);
            }
        }

        private void UpdateStunned()
        {
            if (_stateTimer >= 1f)
            {
                ChangeState(_currentTarget != null ? MonsterState.Chasing : MonsterState.Idle);
            }
        }

        #endregion

        #region Movement

        private void SetDestination(Vector3 destination)
        {
            if (_navAgent != null && _navAgent.isOnNavMesh)
            {
                _navAgent.isStopped = false;
                _navAgent.SetDestination(destination);
            }
        }

        private void StopMovement()
        {
            if (_navAgent != null && _navAgent.isOnNavMesh)
            {
                _navAgent.isStopped = true;
            }
        }

        private bool HasReachedDestination()
        {
            if (_navAgent == null || !_navAgent.isOnNavMesh) return true;
            return !_navAgent.pathPending && _navAgent.remainingDistance <= _navAgent.stoppingDistance;
        }

        private void SetNewWanderDestination()
        {
            Vector3 randomDir = Random.insideUnitSphere * _wanderRadius;
            randomDir += _spawnPosition;
            randomDir.y = transform.position.y;

            if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, _wanderRadius, NavMesh.AllAreas))
            {
                _wanderDestination = hit.position;
                SetDestination(_wanderDestination);
            }
        }

        private void SetFleeDestination()
        {
            if (_currentTarget == null)
            {
                ChangeState(MonsterState.Returning);
                return;
            }

            Vector3 fleeDir = (transform.position - _currentTarget.position).normalized;
            Vector3 fleePoint = transform.position + fleeDir * 10f;

            if (NavMesh.SamplePosition(fleePoint, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                SetDestination(hit.position);
            }
        }

        private void ApplyKnockback()
        {
            float t = _knockbackTimer / 0.15f;
            Vector3 knockback = _knockbackVelocity * t * Time.deltaTime;
            transform.position += knockback;
        }

        private void LookAtTarget()
        {
            if (_currentTarget == null) return;

            Vector3 lookDir = (_currentTarget.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(lookDir),
                    Time.deltaTime * 10f
                );
            }
        }

        #endregion

        #region Combat

        private void SearchForTarget()
        {
            if (_definition == null) return;
            if (_currentTarget != null) return;

            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                _definition.DetectionRange,
                _targetLayers
            );

            Transform bestTarget = null;
            float bestDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                if (hit.transform == transform) continue;
                if (hit.transform.IsChildOf(transform)) continue;

                var damageable = hit.GetComponent<IDamageable>();
                if (damageable == null || !damageable.IsAlive) continue;

                if (hit.CompareTag("Player"))
                {
                    float distance = Vector3.Distance(transform.position, hit.transform.position);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestTarget = hit.transform;
                    }
                }
            }

            if (bestTarget != null)
            {
                if (Random.value < _definition.Aggression)
                {
                    SetTarget(bestTarget);
                }
            }
        }

        private void PerformAttack()
        {
            if (_definition == null || _currentTarget == null) return;

            _attackCooldownTimer = _definition.AttackCooldown;

            ChangeState(MonsterState.Attacking);
            SetAnimation(AnimAttack);

            // Play cast effect
            if (_castEffect != null)
            {
                _castEffect.Play();
            }

            // Fire projectile if we have one
            if (_projectilePrefab != null)
            {
                Vector3 spawnPos = _attackPoint.position;
                Vector3 direction = (_currentTarget.position - spawnPos).normalized;

                GameObject projectile = Instantiate(_projectilePrefab, spawnPos, Quaternion.LookRotation(direction));

                // Configure projectile damage
                var hitbox = projectile.GetComponent<Hitbox>();
                if (hitbox != null)
                {
                    hitbox.SetDamage(_definition.AttackDamage);
                    hitbox.SetOwner(gameObject);
                    hitbox.Activate();
                }

                // Add velocity
                var rb = projectile.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = direction * _projectileSpeed;
                }

                // Destroy after time
                Destroy(projectile, 5f);
            }
            else
            {
                // Fallback to melee-style attack
                Collider[] hits = Physics.OverlapSphere(
                    _attackPoint.position,
                    _attackRadius,
                    _targetLayers
                );

                foreach (var hit in hits)
                {
                    if (hit.transform == transform) continue;

                    var damageable = hit.GetComponent<IDamageable>();
                    if (damageable != null && damageable.IsAlive)
                    {
                        damageable.TakeDamage(_definition.AttackDamage, gameObject);
                    }
                }
            }

            // Play attack sound
            if (_definition.AttackSound != null)
            {
                AudioSource.PlayClipAtPoint(_definition.AttackSound, transform.position);
            }
        }

        private void TrySummonMinion()
        {
            if (_minionPrefab == null) return;
            if (_activeMinions >= _maxMinions) return;

            _summonCooldownTimer = _summonCooldown;

            // Play summon animation and effect
            SetAnimation(AnimSummon);
            if (_summonEffect != null)
            {
                _summonEffect.Play();
            }

            // Find spawn position
            Vector3 spawnPos = _summonPoint.position + Random.insideUnitSphere * _summonRadius;
            spawnPos.y = transform.position.y;

            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, _summonRadius * 2, NavMesh.AllAreas))
            {
                GameObject minion = Instantiate(_minionPrefab, hit.position, Quaternion.identity);

                // Set minion's target to our target
                var minionAI = minion.GetComponent<IMonsterAI>();
                if (minionAI != null && _currentTarget != null)
                {
                    minionAI.SetTarget(_currentTarget);
                }

                // Track minion death
                var minionHealth = minion.GetComponent<MonsterHealth>();
                if (minionHealth != null)
                {
                    minionHealth.OnDeath += () => OnMinionDeath();
                }

                _activeMinions++;

                Debug.Log($"[NecromancerAI] Summoned minion! Active: {_activeMinions}/{_maxMinions}");
            }
        }

        #endregion

        #region Daylight

        private void CheckDaylightBurn()
        {
            if (_timeManager == null) return;
            if (!_timeManager.IsDay) return;

            float burnDamage = Time.deltaTime * 10f;
            _health.TakeDamage(burnDamage);
        }

        #endregion

        #region Animation

        private void SetAnimation(int animHash)
        {
            if (_animator != null)
            {
                _animator.SetTrigger(animHash);
            }
        }

        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!_showDebugGizmos) return;

            // Detection range
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, _definition?.DetectionRange ?? 15f);

            // Preferred combat distance
            Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, _preferredCombatDistance);

            // Retreat distance
            Gizmos.color = new Color(1f, 0f, 1f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, _retreatDistance);

            // Attack range
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(_attackPoint != null ? _attackPoint.position : transform.position,
                _definition?.AttackRange ?? 2f);

            // Summon radius
            Gizmos.color = new Color(0.5f, 0f, 0.5f, 0.3f);
            Gizmos.DrawWireSphere(_summonPoint != null ? _summonPoint.position : transform.position, _summonRadius);

            // Spawn position
            if (Application.isPlaying)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, _spawnPosition);
                Gizmos.DrawWireCube(_spawnPosition, Vector3.one * 0.5f);
            }

            // Current target line
            if (_currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, _currentTarget.position);
            }
        }
#endif
    }
}
