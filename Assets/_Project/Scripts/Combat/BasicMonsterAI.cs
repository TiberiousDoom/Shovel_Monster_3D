using UnityEngine;
using VoxelRPG.Core;

namespace VoxelRPG.Combat
{
    /// <summary>
    /// Basic monster AI implementation for Phase 1.
    /// Simple state machine: Idle → Wander → Chase → Attack → repeat.
    /// Uses simple transform-based movement with ground detection (no NavMesh required).
    /// </summary>
    [RequireComponent(typeof(MonsterHealth))]
    public class BasicMonsterAI : MonoBehaviour, IMonsterAI
    {
        [Header("Configuration")]
        [SerializeField] private MonsterDefinition _definition;

        [Header("Detection")]
        [SerializeField] private LayerMask _targetLayers = -1;
        [SerializeField] private float _targetUpdateInterval = 0.5f;

        [Header("Movement")]
        [SerializeField] private float _wanderRadius = 10f;
        [SerializeField] private float _wanderWaitTime = 3f;
        [SerializeField] private float _stuckThreshold = 0.1f;
        [SerializeField] private float _stuckCheckTime = 2f;
        [SerializeField] private LayerMask _groundLayer = -1;
        [SerializeField] private float _groundCheckDistance = 2f;
        [SerializeField] private float _gravity = 20f;
        [SerializeField] private float _stepHeight = 0.5f;

        [Header("Combat")]
        [SerializeField] private Transform _attackPoint;
        [SerializeField] private float _attackRadius = 1f;

        [Header("Debug")]
        [SerializeField] private bool _showDebugGizmos = true;

        // Cached components
        private MonsterHealth _health;
        private CharacterController _characterController;
        private Rigidbody _rigidbody;

        // State tracking
        private MonsterState _currentState = MonsterState.Idle;
        private Transform _currentTarget;
        private Vector3 _spawnPosition;
        private Vector3 _wanderDestination;
        private Vector3 _lastPosition;

        // Movement state
        private Vector3 _currentDestination;
        private bool _hasDestination;
        private float _currentSpeed;
        private float _verticalVelocity;
        private bool _isGrounded;

        // Timers
        private float _stateTimer;
        private float _targetSearchTimer;
        private float _attackCooldownTimer;
        private float _wanderTimer;
        private float _stuckTimer;

        // Knockback
        private Vector3 _knockbackVelocity;
        private float _knockbackDuration;
        private float _knockbackTimer;

        // Time manager reference for daylight check
        private TimeManager _timeManager;

        /// <inheritdoc/>
        public MonsterDefinition Definition => _definition;

        /// <inheritdoc/>
        public MonsterState CurrentState => _currentState;

        /// <inheritdoc/>
        public Transform CurrentTarget => _currentTarget;

        /// <inheritdoc/>
        public bool IsAlive => _health != null && _health.IsAlive;

        private void Awake()
        {
            _health = GetComponent<MonsterHealth>();
            _characterController = GetComponent<CharacterController>();
            _rigidbody = GetComponent<Rigidbody>();

            if (_attackPoint == null)
            {
                _attackPoint = transform;
            }
        }

        private void Start()
        {
            _spawnPosition = transform.position;
            _lastPosition = transform.position;

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
                    UpdateChasing();
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

            // Apply movement towards destination
            MoveTowardsDestination();

            // Check if stuck
            CheckIfStuck();
        }

        private void UpdateTimers()
        {
            _stateTimer += Time.deltaTime;
            _targetSearchTimer -= Time.deltaTime;
            _attackCooldownTimer -= Time.deltaTime;
            _wanderTimer -= Time.deltaTime;
            _stuckTimer += Time.deltaTime;
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

            _currentSpeed = definition.WanderSpeed;

            Debug.Log($"[BasicMonsterAI] Initialized: {definition.DisplayName}");
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
                _knockbackVelocity = knockback * 5f;
                _knockbackTimer = 0.2f;
            }

            // Check if should flee
            if (_definition != null && _definition.FleesAtLowHealth)
            {
                if (_health.HealthNormalized <= _definition.FleeHealthThreshold)
                {
                    ChangeState(MonsterState.Fleeing);
                }
            }
        }

        /// <inheritdoc/>
        public void OnDeath()
        {
            ChangeState(MonsterState.Dead);

            // Stop movement
            _hasDestination = false;

            // Spawn death effects, loot, etc. would go here
            Debug.Log($"[BasicMonsterAI] {_definition?.DisplayName ?? "Monster"} died!");

            // Destroy after delay
            Destroy(gameObject, 3f);
        }

        /// <inheritdoc/>
        public void ForceState(MonsterState newState)
        {
            ChangeState(newState);
        }

        private void ChangeState(MonsterState newState)
        {
            if (_currentState == newState) return;

            // Exit current state
            OnExitState(_currentState);

            _currentState = newState;
            _stateTimer = 0f;

            // Enter new state
            OnEnterState(newState);
        }

        private void OnExitState(MonsterState state)
        {
            // Cleanup for exiting state
        }

        private void OnEnterState(MonsterState state)
        {
            switch (state)
            {
                case MonsterState.Idle:
                    StopMovement();
                    break;

                case MonsterState.Wandering:
                    SetNewWanderDestination();
                    _currentSpeed = _definition?.WanderSpeed ?? 2f;
                    break;

                case MonsterState.Chasing:
                    _currentSpeed = _definition?.ChaseSpeed ?? 5f;
                    break;

                case MonsterState.Attacking:
                    StopMovement();
                    break;

                case MonsterState.Fleeing:
                    _currentSpeed = _definition?.ChaseSpeed ?? 5f;
                    SetFleeDestination();
                    break;

                case MonsterState.Returning:
                    _currentSpeed = _definition?.WanderSpeed ?? 2f;
                    SetDestination(_spawnPosition);
                    break;

                case MonsterState.Dead:
                    StopMovement();
                    break;
            }
        }

        #region State Updates

        private void UpdateIdle()
        {
            // Check for target
            if (_currentTarget != null)
            {
                ChangeState(MonsterState.Chasing);
                return;
            }

            // Start wandering after delay
            if (_stateTimer >= 2f)
            {
                ChangeState(MonsterState.Wandering);
            }
        }

        private void UpdateWandering()
        {
            // Check for target
            if (_currentTarget != null)
            {
                ChangeState(MonsterState.Chasing);
                return;
            }

            // Check if reached destination
            if (HasReachedDestination())
            {
                if (_wanderTimer <= 0)
                {
                    _wanderTimer = _wanderWaitTime;
                    SetNewWanderDestination();
                }
            }
        }

        private void UpdateChasing()
        {
            if (_currentTarget == null)
            {
                ChangeState(MonsterState.Returning);
                return;
            }

            float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.position);

            // Check if target is too far (lose aggro)
            if (distanceToTarget > (_definition?.LoseTargetRange ?? 25f))
            {
                ClearTarget();
                return;
            }

            // Check if in attack range
            if (distanceToTarget <= (_definition?.AttackRange ?? 2f))
            {
                ChangeState(MonsterState.Attacking);
                return;
            }

            // Move toward target
            SetDestination(_currentTarget.position);
        }

        private void UpdateAttacking()
        {
            if (_currentTarget == null)
            {
                ChangeState(MonsterState.Idle);
                return;
            }

            float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.position);

            // Check if target moved out of range
            if (distanceToTarget > (_definition?.AttackRange ?? 2f) * 1.2f)
            {
                ChangeState(MonsterState.Chasing);
                return;
            }

            // Look at target
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

            // Attack if cooldown is ready
            if (_attackCooldownTimer <= 0)
            {
                PerformAttack();
            }
        }

        private void UpdateFleeing()
        {
            // Check if far enough from danger
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

            // Update flee direction periodically
            if (_stateTimer > 2f)
            {
                SetFleeDestination();
                _stateTimer = 0f;
            }

            // Check if reached destination
            if (HasReachedDestination())
            {
                SetFleeDestination();
            }
        }

        private void UpdateReturning()
        {
            // Check for new target while returning
            if (_currentTarget != null)
            {
                ChangeState(MonsterState.Chasing);
                return;
            }

            // Check if reached spawn point
            if (Vector3.Distance(transform.position, _spawnPosition) < 2f)
            {
                ChangeState(MonsterState.Idle);
            }
        }

        private void UpdateStunned()
        {
            // Recover from stun after duration
            if (_stateTimer >= 1f)
            {
                ChangeState(_currentTarget != null ? MonsterState.Chasing : MonsterState.Idle);
            }
        }

        #endregion

        #region Movement

        private void SetDestination(Vector3 destination)
        {
            _currentDestination = destination;
            _hasDestination = true;
        }

        private void StopMovement()
        {
            _hasDestination = false;
        }

        private bool HasReachedDestination()
        {
            if (!_hasDestination) return true;
            float distance = Vector3.Distance(
                new Vector3(transform.position.x, 0, transform.position.z),
                new Vector3(_currentDestination.x, 0, _currentDestination.z)
            );
            return distance <= 1f;
        }

        private void SetNewWanderDestination()
        {
            Vector3 randomDir = Random.insideUnitSphere * _wanderRadius;
            randomDir += _spawnPosition;
            randomDir.y = transform.position.y;

            // Sample ground position
            if (SampleGroundPosition(randomDir, out Vector3 groundPos))
            {
                _wanderDestination = groundPos;
                SetDestination(_wanderDestination);
            }
            else
            {
                // Fallback to current height
                _wanderDestination = randomDir;
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

            // Flee away from target
            Vector3 fleeDir = (transform.position - _currentTarget.position).normalized;
            Vector3 fleePoint = transform.position + fleeDir * 15f;

            if (SampleGroundPosition(fleePoint, out Vector3 groundPos))
            {
                SetDestination(groundPos);
            }
            else
            {
                SetDestination(fleePoint);
            }
        }

        private bool SampleGroundPosition(Vector3 position, out Vector3 groundPos)
        {
            // Raycast down from above the position to find ground
            Vector3 rayStart = position + Vector3.up * 10f;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 20f, _groundLayer))
            {
                groundPos = hit.point;
                return true;
            }
            groundPos = position;
            return false;
        }

        private void MoveTowardsDestination()
        {
            if (!_hasDestination) return;

            // Calculate direction to destination (horizontal only)
            Vector3 direction = _currentDestination - transform.position;
            direction.y = 0;

            if (direction.sqrMagnitude < 0.01f) return;

            direction.Normalize();

            // Rotate towards movement direction
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);

            // Calculate movement
            Vector3 movement = direction * _currentSpeed * Time.deltaTime;

            // Apply gravity
            CheckGrounded();
            if (_isGrounded)
            {
                _verticalVelocity = -2f; // Small downward force to stay grounded
            }
            else
            {
                _verticalVelocity -= _gravity * Time.deltaTime;
            }
            movement.y = _verticalVelocity * Time.deltaTime;

            // Check for step-up (small obstacles)
            if (_isGrounded && CanStepUp(direction))
            {
                movement.y = _stepHeight;
            }

            // Apply movement
            if (_characterController != null)
            {
                _characterController.Move(movement);
            }
            else if (_rigidbody != null)
            {
                _rigidbody.MovePosition(transform.position + movement);
            }
            else
            {
                transform.position += movement;
            }
        }

        private void CheckGrounded()
        {
            // Raycast down to check for ground
            Vector3 rayStart = transform.position + Vector3.up * 0.1f;
            _isGrounded = Physics.Raycast(rayStart, Vector3.down, 0.3f, _groundLayer);
        }

        private bool CanStepUp(Vector3 direction)
        {
            // Check if there's a small obstacle we can step over
            Vector3 feetPos = transform.position + Vector3.up * 0.1f;
            Vector3 stepPos = feetPos + Vector3.up * _stepHeight;

            // Check if blocked at feet level
            if (!Physics.Raycast(feetPos, direction, 0.5f, _groundLayer))
            {
                return false; // Nothing to step over
            }

            // Check if clear at step height
            if (Physics.Raycast(stepPos, direction, 0.5f, _groundLayer))
            {
                return false; // Blocked at step height too
            }

            return true;
        }

        private void ApplyKnockback()
        {
            float t = _knockbackTimer / 0.2f;
            Vector3 knockback = _knockbackVelocity * t * Time.deltaTime;

            if (_characterController != null)
            {
                _characterController.Move(knockback);
            }
            else if (_rigidbody != null)
            {
                _rigidbody.MovePosition(transform.position + knockback);
            }
            else
            {
                transform.position += knockback;
            }
        }

        private void CheckIfStuck()
        {
            if (_currentState != MonsterState.Chasing && _currentState != MonsterState.Wandering) return;

            if (_stuckTimer >= _stuckCheckTime)
            {
                float distanceMoved = Vector3.Distance(transform.position, _lastPosition);
                if (distanceMoved < _stuckThreshold)
                {
                    // We're stuck - pick new destination or return
                    if (_currentState == MonsterState.Wandering)
                    {
                        SetNewWanderDestination();
                    }
                    else if (_currentState == MonsterState.Chasing)
                    {
                        // Try to jump or find alternate path
                        // For now, just lose the target
                        ClearTarget();
                    }
                }
                _lastPosition = transform.position;
                _stuckTimer = 0f;
            }
        }

        #endregion

        #region Combat

        private void SearchForTarget()
        {
            if (_definition == null) return;
            if (_currentTarget != null) return; // Already have a target

            // Find potential targets
            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                _definition.DetectionRange,
                _targetLayers
            );

            Transform bestTarget = null;
            float bestDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                // Skip self
                if (hit.transform == transform) continue;
                if (hit.transform.IsChildOf(transform)) continue;

                // Check for player or damageable
                var damageable = hit.GetComponent<IDamageable>();
                if (damageable == null || !damageable.IsAlive) continue;

                // Check if it's actually a valid target (player)
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
                // Check aggression chance
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

            // Check for targets in attack range
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
                    Debug.Log($"[BasicMonsterAI] {_definition.DisplayName} dealt {_definition.AttackDamage} damage");
                }
            }

            // Play attack sound
            if (_definition.AttackSound != null)
            {
                AudioSource.PlayClipAtPoint(_definition.AttackSound, transform.position);
            }
        }

        #endregion

        #region Daylight

        private void CheckDaylightBurn()
        {
            if (_timeManager == null) return;
            if (!_timeManager.IsDay) return;

            // Take damage from sunlight
            float burnDamage = Time.deltaTime * 10f; // 10 HP/sec in sunlight
            _health.TakeDamage(burnDamage);
        }

        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!_showDebugGizmos) return;
            if (_definition == null) return;

            // Detection range
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, _definition.DetectionRange);

            // Attack range
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(_attackPoint != null ? _attackPoint.position : transform.position, _definition.AttackRange);

            // Spawn position
            Gizmos.color = Color.green;
            if (Application.isPlaying)
            {
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
