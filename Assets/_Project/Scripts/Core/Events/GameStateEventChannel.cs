using System;
using UnityEngine;

namespace VoxelRPG.Core.Events
{
    /// <summary>
    /// ScriptableObject-based event channel for game state changes.
    /// </summary>
    [CreateAssetMenu(menuName = "VoxelRPG/Events/Game State Event Channel", fileName = "NewGameStateEventChannel")]
    public class GameStateEventChannel : ScriptableObject
    {
        /// <summary>
        /// Event raised when game state changes.
        /// Parameters: previous state, new state.
        /// </summary>
        public event Action<GameState, GameState> OnEventRaised;

        /// <summary>
        /// Raises the event with previous and new state.
        /// </summary>
        /// <param name="previousState">The state being transitioned from.</param>
        /// <param name="newState">The state being transitioned to.</param>
        public void RaiseEvent(GameState previousState, GameState newState)
        {
            OnEventRaised?.Invoke(previousState, newState);
        }
    }
}
