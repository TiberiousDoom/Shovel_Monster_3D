using System;
using UnityEngine;

namespace VoxelRPG.Core.Events
{
    /// <summary>
    /// ScriptableObject-based event channel for Vector3Int events.
    /// Used for block position changes.
    /// </summary>
    [CreateAssetMenu(menuName = "VoxelRPG/Events/Vector3Int Event Channel", fileName = "NewVector3IntEventChannel")]
    public class Vector3IntEventChannel : ScriptableObject
    {
        /// <summary>
        /// Event raised when RaiseEvent is called.
        /// </summary>
        public event Action<Vector3Int> OnEventRaised;

        /// <summary>
        /// Raises the event with the given position.
        /// </summary>
        /// <param name="position">The position value.</param>
        public void RaiseEvent(Vector3Int position)
        {
            OnEventRaised?.Invoke(position);
        }
    }
}
