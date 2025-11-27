using System;
using UnityEngine;

namespace VoxelRPG.Core.Events
{
    /// <summary>
    /// ScriptableObject-based event channel for events with no parameters.
    /// Enables decoupled communication between systems.
    /// </summary>
    [CreateAssetMenu(menuName = "VoxelRPG/Events/Void Event Channel", fileName = "NewVoidEventChannel")]
    public class VoidEventChannel : ScriptableObject
    {
        /// <summary>
        /// Event raised when RaiseEvent is called.
        /// </summary>
        public event Action OnEventRaised;

        /// <summary>
        /// Raises the event, notifying all listeners.
        /// </summary>
        public void RaiseEvent()
        {
            OnEventRaised?.Invoke();
        }
    }
}
