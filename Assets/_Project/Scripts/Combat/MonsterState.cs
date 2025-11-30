namespace VoxelRPG.Combat
{
    /// <summary>
    /// Possible states for monster AI behavior.
    /// </summary>
    public enum MonsterState
    {
        /// <summary>
        /// Monster is idle, not pursuing any target.
        /// </summary>
        Idle,

        /// <summary>
        /// Monster is wandering randomly.
        /// </summary>
        Wandering,

        /// <summary>
        /// Monster has detected a target and is chasing.
        /// </summary>
        Chasing,

        /// <summary>
        /// Monster is in attack range and attacking.
        /// </summary>
        Attacking,

        /// <summary>
        /// Monster is fleeing from danger.
        /// </summary>
        Fleeing,

        /// <summary>
        /// Monster is stunned/knocked back.
        /// </summary>
        Stunned,

        /// <summary>
        /// Monster is returning to patrol/spawn point.
        /// </summary>
        Returning,

        /// <summary>
        /// Monster is dead.
        /// </summary>
        Dead
    }
}
