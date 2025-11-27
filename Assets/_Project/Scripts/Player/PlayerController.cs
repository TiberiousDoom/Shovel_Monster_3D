using UnityEngine;
using UnityEngine.InputSystem;
using VoxelRPG.Core;

namespace VoxelRPG.Player
{
    /// <summary>
    /// Handles player movement and physics.
    /// Phase 0A implementation - basic first-person movement.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _sprintMultiplier = 1.5f;
        [SerializeField] private float _jumpHeight = 1.2f;
        [SerializeField] private float _gravity = -15f;

        [Header("Ground Check")]
        [SerializeField] private Transform _groundCheck;
        [SerializeField] private float _groundDistance = 0.2f;
        [SerializeField] private LayerMask _groundMask;

        private CharacterController _characterController;
        private Vector3 _velocity;
        private bool _isGrounded;
        private bool _isSprinting;

        private Vector2 _moveInput;
        private bool _jumpPressed;

        /// <summary>
        /// Current movement speed accounting for sprint.
        /// </summary>
        public float CurrentSpeed => _isSprinting ? _moveSpeed * _sprintMultiplier : _moveSpeed;

        /// <summary>
        /// Whether the player is currently on the ground.
        /// </summary>
        public bool IsGrounded => _isGrounded;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            // Register with ServiceLocator
            ServiceLocator.Register<PlayerController>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<PlayerController>();
        }

        private void Update()
        {
            CheckGround();
            HandleMovement();
            HandleGravityAndJump();
        }

        private void CheckGround()
        {
            if (_groundCheck != null)
            {
                _isGrounded = Physics.CheckSphere(
                    _groundCheck.position,
                    _groundDistance,
                    _groundMask
                );
            }
            else
            {
                _isGrounded = _characterController.isGrounded;
            }

            // Reset downward velocity when grounded
            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f; // Small downward force to keep grounded
            }
        }

        private void HandleMovement()
        {
            var moveDirection = transform.right * _moveInput.x + transform.forward * _moveInput.y;
            _characterController.Move(moveDirection * CurrentSpeed * Time.deltaTime);
        }

        private void HandleGravityAndJump()
        {
            if (_jumpPressed && _isGrounded)
            {
                // v = sqrt(2 * g * h)
                _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
                _jumpPressed = false;
            }

            _velocity.y += _gravity * Time.deltaTime;
            _characterController.Move(_velocity * Time.deltaTime);
        }

        /// <summary>
        /// Called by Input System for movement.
        /// </summary>
        public void OnMove(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();
        }

        /// <summary>
        /// Called by Input System for jumping.
        /// </summary>
        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                _jumpPressed = true;
            }
        }

        /// <summary>
        /// Called by Input System for sprinting.
        /// </summary>
        public void OnSprint(InputAction.CallbackContext context)
        {
            _isSprinting = context.performed;
        }

        /// <summary>
        /// Teleports the player to a world position.
        /// </summary>
        /// <param name="position">World position to teleport to.</param>
        public void Teleport(Vector3 position)
        {
            _characterController.enabled = false;
            transform.position = position;
            _characterController.enabled = true;
            _velocity = Vector3.zero;
        }
    }
}
