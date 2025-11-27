namespace VoxelRPG.Utilities.Save
{
    /// <summary>
    /// Interface for objects that can be saved and loaded.
    /// Implement this on MonoBehaviours or ScriptableObjects that need persistence.
    /// </summary>
    public interface ISaveable
    {
        /// <summary>
        /// Unique identifier for this saveable object.
        /// Must be consistent across saves/loads.
        /// </summary>
        string SaveId { get; }

        /// <summary>
        /// Captures the current state for saving.
        /// </summary>
        /// <returns>Serializable data object.</returns>
        object CaptureState();

        /// <summary>
        /// Restores state from loaded data.
        /// </summary>
        /// <param name="state">Previously saved state object.</param>
        void RestoreState(object state);
    }
}
