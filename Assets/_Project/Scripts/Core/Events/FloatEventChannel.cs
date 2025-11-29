using System;
using UnityEngine;

namespace VoxelRPG.Core.Events
{
    /// <summary>
    /// ScriptableObject-based event channel for events with a float parameter.
    /// Used for stat changes (health, hunger, etc.).
    /// </summary>
    [CreateAssetMenu(menuName = "VoxelRPG/Events/Float Event Channel", fileName = "NewFloatEventChannel")]
    public class FloatEventChannel : ScriptableObject
    {
        /// <summary>
        /// Event raised when RaiseEvent is called.
        /// Parameter: the float value.
        /// </summary>
        public event Action<float> OnEventRaised;

        /// <summary>
        /// Raises the event with the given value.
        /// </summary>
        /// <param name="value">The float value to pass to listeners.</param>
        public void RaiseEvent(float value)
        {
            OnEventRaised?.Invoke(value);
        }
    }
}
