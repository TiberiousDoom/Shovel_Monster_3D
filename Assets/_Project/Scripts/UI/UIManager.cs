using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VoxelRPG.Core;

namespace VoxelRPG.UI
{
    /// <summary>
    /// Manages UI screen states and transitions.
    /// Controls which UI panels are visible and handles input mode switching.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Screen References")]
        [SerializeField] private GameObject _hudScreen;
        [SerializeField] private GameObject _pauseScreen;
        [SerializeField] private GameObject _inventoryScreen;
        [SerializeField] private GameObject _craftingScreen;
        [SerializeField] private GameObject _deathScreen;

        [Header("Input")]
        [SerializeField] private InputActionReference _pauseAction;
        [SerializeField] private InputActionReference _inventoryAction;
        [SerializeField] private InputActionReference _craftingAction;

        [Header("Settings")]
        [Tooltip("Whether to pause the game when pause menu is open")]
        [SerializeField] private bool _pauseOnMenu = true;

        private UIScreenState _currentState = UIScreenState.Gameplay;
        private Stack<UIScreenState> _screenStack = new Stack<UIScreenState>();
        private bool _inputEnabled = true;

        /// <summary>
        /// Current UI screen state.
        /// </summary>
        public UIScreenState CurrentState => _currentState;

        /// <summary>
        /// Whether the cursor is currently visible and unlocked.
        /// </summary>
        public bool IsCursorVisible => _currentState != UIScreenState.Gameplay;

        /// <summary>
        /// Whether gameplay input should be processed.
        /// </summary>
        public bool IsGameplayInputEnabled => _currentState == UIScreenState.Gameplay;

        /// <summary>
        /// Event fired when screen state changes.
        /// </summary>
        public event Action<UIScreenState> OnScreenChanged;

        /// <summary>
        /// Event fired when pause state changes.
        /// </summary>
        public event Action<bool> OnPauseChanged;

        private void Awake()
        {
            ServiceLocator.Register<UIManager>(this);
        }

        // Dynamically found actions (when InputActionReferences aren't assigned)
        private InputAction _dynamicPauseAction;
        private InputAction _dynamicInventoryAction;
        private InputAction _dynamicCraftingAction;

        private void Start()
        {
            // Initialize to gameplay state
            SetState(UIScreenState.Gameplay);

            // Subscribe to input actions (try references first, then find dynamically)
            SetupInputActions();
        }

        private void SetupInputActions()
        {
            // Try to find input actions from player if references aren't set
            if (_pauseAction == null || _inventoryAction == null || _craftingAction == null)
            {
                TryFindInputActionsFromPlayer();
            }

            // Subscribe to pause action
            if (_pauseAction != null)
            {
                _pauseAction.action.performed += OnPauseInput;
                _pauseAction.action.Enable();
            }
            else if (_dynamicPauseAction != null)
            {
                _dynamicPauseAction.performed += OnPauseInput;
                _dynamicPauseAction.Enable();
            }

            // Subscribe to inventory action
            if (_inventoryAction != null)
            {
                _inventoryAction.action.performed += OnInventoryInput;
                _inventoryAction.action.Enable();
            }
            else if (_dynamicInventoryAction != null)
            {
                _dynamicInventoryAction.performed += OnInventoryInput;
                _dynamicInventoryAction.Enable();
            }

            // Subscribe to crafting action
            if (_craftingAction != null)
            {
                _craftingAction.action.performed += OnCraftingInput;
                _craftingAction.action.Enable();
            }
            else if (_dynamicCraftingAction != null)
            {
                _dynamicCraftingAction.performed += OnCraftingInput;
                _dynamicCraftingAction.Enable();
            }
        }

        private void TryFindInputActionsFromPlayer()
        {
            var playerInput = FindFirstObjectByType<PlayerInput>();
            if (playerInput == null || playerInput.actions == null)
            {
                Debug.LogWarning("[UIManager] No PlayerInput found - UI input won't work.");
                return;
            }

            var playerActionMap = playerInput.actions.FindActionMap("Player");
            if (playerActionMap == null)
            {
                Debug.LogWarning("[UIManager] No 'Player' action map found.");
                return;
            }

            // Find and cache the actions
            if (_pauseAction == null)
            {
                _dynamicPauseAction = playerActionMap.FindAction("Pause");
                if (_dynamicPauseAction != null)
                {
                    Debug.Log("[UIManager] Found Pause action dynamically.");
                }
            }

            if (_inventoryAction == null)
            {
                _dynamicInventoryAction = playerActionMap.FindAction("Inventory");
                if (_dynamicInventoryAction != null)
                {
                    Debug.Log("[UIManager] Found Inventory action dynamically.");
                }
            }

            if (_craftingAction == null)
            {
                _dynamicCraftingAction = playerActionMap.FindAction("Crafting");
                if (_dynamicCraftingAction != null)
                {
                    Debug.Log("[UIManager] Found Crafting action dynamically.");
                }
            }
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<UIManager>();

            // Cleanup InputActionReference subscriptions
            if (_pauseAction != null)
            {
                _pauseAction.action.performed -= OnPauseInput;
            }

            if (_inventoryAction != null)
            {
                _inventoryAction.action.performed -= OnInventoryInput;
            }

            if (_craftingAction != null)
            {
                _craftingAction.action.performed -= OnCraftingInput;
            }

            // Cleanup dynamic action subscriptions
            if (_dynamicPauseAction != null)
            {
                _dynamicPauseAction.performed -= OnPauseInput;
            }

            if (_dynamicInventoryAction != null)
            {
                _dynamicInventoryAction.performed -= OnInventoryInput;
            }

            if (_dynamicCraftingAction != null)
            {
                _dynamicCraftingAction.performed -= OnCraftingInput;
            }
        }

        private void OnPauseInput(InputAction.CallbackContext context)
        {
            if (!_inputEnabled) return;

            switch (_currentState)
            {
                case UIScreenState.Gameplay:
                    OpenPauseMenu();
                    break;

                case UIScreenState.Paused:
                    ClosePauseMenu();
                    break;

                case UIScreenState.Inventory:
                case UIScreenState.Crafting:
                    // ESC closes current screen
                    ReturnToGameplay();
                    break;
            }
        }

        private void OnInventoryInput(InputAction.CallbackContext context)
        {
            if (!_inputEnabled) return;
            if (_currentState == UIScreenState.Paused) return;

            if (_currentState == UIScreenState.Inventory)
            {
                ReturnToGameplay();
            }
            else if (_currentState == UIScreenState.Gameplay)
            {
                OpenInventory();
            }
        }

        private void OnCraftingInput(InputAction.CallbackContext context)
        {
            if (!_inputEnabled) return;
            if (_currentState == UIScreenState.Paused) return;

            if (_currentState == UIScreenState.Crafting)
            {
                ReturnToGameplay();
            }
            else if (_currentState == UIScreenState.Gameplay)
            {
                OpenCrafting();
            }
        }

        /// <summary>
        /// Sets the current UI state.
        /// </summary>
        public void SetState(UIScreenState newState)
        {
            if (_currentState == newState) return;

            var previousState = _currentState;
            _currentState = newState;

            // Update screen visibility
            UpdateScreenVisibility();

            // Update cursor state
            UpdateCursorState();

            // Handle pause
            if (_pauseOnMenu)
            {
                bool shouldPause = newState == UIScreenState.Paused;
                Time.timeScale = shouldPause ? 0f : 1f;
                OnPauseChanged?.Invoke(shouldPause);
            }

            OnScreenChanged?.Invoke(newState);

            Debug.Log($"[UIManager] Screen changed: {previousState} -> {newState}");
        }

        private void UpdateScreenVisibility()
        {
            if (_hudScreen != null)
            {
                _hudScreen.SetActive(_currentState == UIScreenState.Gameplay);
            }

            if (_pauseScreen != null)
            {
                _pauseScreen.SetActive(_currentState == UIScreenState.Paused);
            }

            if (_inventoryScreen != null)
            {
                _inventoryScreen.SetActive(_currentState == UIScreenState.Inventory);
            }

            if (_craftingScreen != null)
            {
                _craftingScreen.SetActive(_currentState == UIScreenState.Crafting);
            }

            if (_deathScreen != null)
            {
                _deathScreen.SetActive(_currentState == UIScreenState.Death);
            }
        }

        private void UpdateCursorState()
        {
            if (_currentState == UIScreenState.Gameplay)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }

        /// <summary>
        /// Opens the pause menu.
        /// </summary>
        public void OpenPauseMenu()
        {
            _screenStack.Push(_currentState);
            SetState(UIScreenState.Paused);
        }

        /// <summary>
        /// Closes the pause menu.
        /// </summary>
        public void ClosePauseMenu()
        {
            ReturnToGameplay();
        }

        /// <summary>
        /// Opens the inventory screen.
        /// </summary>
        public void OpenInventory()
        {
            _screenStack.Push(_currentState);
            SetState(UIScreenState.Inventory);
        }

        /// <summary>
        /// Opens the crafting screen.
        /// </summary>
        public void OpenCrafting()
        {
            _screenStack.Push(_currentState);
            SetState(UIScreenState.Crafting);
        }

        /// <summary>
        /// Shows the death screen.
        /// </summary>
        public void ShowDeathScreen()
        {
            _screenStack.Clear();
            SetState(UIScreenState.Death);
        }

        /// <summary>
        /// Returns to gameplay state.
        /// </summary>
        public void ReturnToGameplay()
        {
            _screenStack.Clear();
            SetState(UIScreenState.Gameplay);
        }

        /// <summary>
        /// Returns to the previous screen state.
        /// </summary>
        public void GoBack()
        {
            if (_screenStack.Count > 0)
            {
                SetState(_screenStack.Pop());
            }
            else
            {
                SetState(UIScreenState.Gameplay);
            }
        }

        /// <summary>
        /// Temporarily disables UI input handling.
        /// </summary>
        public void DisableInput()
        {
            _inputEnabled = false;
        }

        /// <summary>
        /// Re-enables UI input handling.
        /// </summary>
        public void EnableInput()
        {
            _inputEnabled = true;
        }

        /// <summary>
        /// Called when player respawns to return to gameplay.
        /// </summary>
        public void OnPlayerRespawn()
        {
            ReturnToGameplay();
        }
    }

    /// <summary>
    /// Possible UI screen states.
    /// </summary>
    public enum UIScreenState
    {
        /// <summary>
        /// Normal gameplay with HUD visible.
        /// </summary>
        Gameplay,

        /// <summary>
        /// Pause menu is open.
        /// </summary>
        Paused,

        /// <summary>
        /// Inventory screen is open.
        /// </summary>
        Inventory,

        /// <summary>
        /// Crafting screen is open.
        /// </summary>
        Crafting,

        /// <summary>
        /// Player death screen.
        /// </summary>
        Death,

        /// <summary>
        /// Main menu (pre-game).
        /// </summary>
        MainMenu,

        /// <summary>
        /// Loading screen.
        /// </summary>
        Loading
    }
}
