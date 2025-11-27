using UnityEngine;
using VoxelRPG.Core.Events;

namespace VoxelRPG.Core
{
    /// <summary>
    /// Central game state manager. Coordinates initialization and state transitions.
    /// Not a singleton - registered with ServiceLocator for testability and multiplayer support.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Event Channels")]
        [SerializeField] private GameStateEventChannel _onGameStateChanged;

        [Header("Debug")]
        [SerializeField] private bool _logStateChanges = true;

        private GameState _currentState = GameState.Initializing;

        /// <summary>
        /// Current game state.
        /// </summary>
        public GameState CurrentState => _currentState;

        /// <summary>
        /// Whether the game is currently playable (Playing state).
        /// </summary>
        public bool IsPlaying => _currentState == GameState.Playing;

        /// <summary>
        /// Whether the game is paused.
        /// </summary>
        public bool IsPaused => _currentState == GameState.Paused;

        private void Awake()
        {
            // Register with ServiceLocator instead of using singleton
            ServiceLocator.Register<GameManager>(this);
        }

        private void Start()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<GameManager>();
        }

        /// <summary>
        /// Initializes the game manager and transitions to main menu or playing state.
        /// </summary>
        private void Initialize()
        {
            // For Phase 0A, go directly to playing state
            // Later phases will add main menu
            ChangeState(GameState.Playing);
            ServiceLocator.MarkInitialized();
        }

        /// <summary>
        /// Changes the game state.
        /// </summary>
        /// <param name="newState">The new state to transition to.</param>
        public void ChangeState(GameState newState)
        {
            if (_currentState == newState)
            {
                return;
            }

            var previousState = _currentState;
            _currentState = newState;

            OnStateChanged(previousState, newState);
        }

        /// <summary>
        /// Called when game state changes.
        /// </summary>
        private void OnStateChanged(GameState previousState, GameState newState)
        {
            if (_logStateChanges)
            {
                Debug.Log($"[GameManager] State changed: {previousState} -> {newState}");
            }

            // Handle time scale for pause
            Time.timeScale = newState == GameState.Paused ? 0f : 1f;

            // Raise event for other systems
            if (_onGameStateChanged != null)
            {
                _onGameStateChanged.RaiseEvent(previousState, newState);
            }
        }

        /// <summary>
        /// Toggles pause state.
        /// </summary>
        public void TogglePause()
        {
            if (_currentState == GameState.Playing)
            {
                ChangeState(GameState.Paused);
            }
            else if (_currentState == GameState.Paused)
            {
                ChangeState(GameState.Playing);
            }
        }

        /// <summary>
        /// Requests to quit the game.
        /// </summary>
        public void RequestQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
