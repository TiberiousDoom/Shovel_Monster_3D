using UnityEngine;
using UnityEngine.InputSystem;
using VoxelRPG.Core;

namespace VoxelRPG.Player
{
    /// <summary>
    /// Handles first-person camera rotation.
    /// </summary>
    public class PlayerCamera : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _playerBody;

        [Header("Sensitivity")]
        [SerializeField] private float _mouseSensitivity = 100f;

        [Header("Vertical Look Limits")]
        [SerializeField] private float _minVerticalAngle = -90f;
        [SerializeField] private float _maxVerticalAngle = 90f;

        private float _verticalRotation;
        private Vector2 _lookInput;
        private bool _cursorLocked = true;

        /// <summary>
        /// Current mouse sensitivity.
        /// </summary>
        public float MouseSensitivity
        {
            get => _mouseSensitivity;
            set => _mouseSensitivity = Mathf.Max(0.1f, value);
        }

        /// <summary>
        /// Whether the cursor is currently locked.
        /// </summary>
        public bool CursorLocked => _cursorLocked;

        private void Awake()
        {
            // Register with ServiceLocator
            ServiceLocator.Register<PlayerCamera>(this);
        }

        private void Start()
        {
            LockCursor();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<PlayerCamera>();
        }

        private void LateUpdate()
        {
            if (!_cursorLocked)
            {
                return;
            }

            HandleLook();
        }

        private void HandleLook()
        {
            float mouseX = _lookInput.x * _mouseSensitivity * Time.deltaTime;
            float mouseY = _lookInput.y * _mouseSensitivity * Time.deltaTime;

            // Vertical rotation (camera pitch)
            _verticalRotation -= mouseY;
            _verticalRotation = Mathf.Clamp(_verticalRotation, _minVerticalAngle, _maxVerticalAngle);
            transform.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);

            // Horizontal rotation (player body yaw)
            if (_playerBody != null)
            {
                _playerBody.Rotate(Vector3.up * mouseX);
            }
        }

        /// <summary>
        /// Called by Input System for look input.
        /// </summary>
        public void OnLook(InputAction.CallbackContext context)
        {
            _lookInput = context.ReadValue<Vector2>();
        }

        /// <summary>
        /// Locks the cursor to the center of the screen.
        /// </summary>
        public void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _cursorLocked = true;
        }

        /// <summary>
        /// Unlocks the cursor for UI interaction.
        /// </summary>
        public void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _cursorLocked = false;
        }

        /// <summary>
        /// Toggles cursor lock state.
        /// </summary>
        public void ToggleCursor()
        {
            if (_cursorLocked)
            {
                UnlockCursor();
            }
            else
            {
                LockCursor();
            }
        }

        /// <summary>
        /// Sets the camera's vertical rotation directly.
        /// </summary>
        /// <param name="angle">Vertical angle in degrees.</param>
        public void SetVerticalRotation(float angle)
        {
            _verticalRotation = Mathf.Clamp(angle, _minVerticalAngle, _maxVerticalAngle);
            transform.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);
        }

        /// <summary>
        /// Sets the player body transform for horizontal rotation.
        /// </summary>
        /// <param name="playerBody">The player body transform.</param>
        public void SetPlayerBody(Transform playerBody)
        {
            _playerBody = playerBody;
        }
    }
}
