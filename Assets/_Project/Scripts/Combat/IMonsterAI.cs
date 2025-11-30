using UnityEngine;

namespace VoxelRPG.Combat
{
    /// <summary>
    /// Shared interface for all monster AI implementations.
    /// Defined in Phase 1.6 to prevent rework in Phase 3.
    /// Phase 3 will add swarm coordination and portal commands.
    /// </summary>
    public interface IMonsterAI
    {
        /// <summary>
        /// The monster's definition (stats, behavior parameters).
        /// </summary>
        MonsterDefinition Definition { get; }

        /// <summary>
        /// Current AI state.
        /// </summary>
        MonsterState CurrentState { get; }

        /// <summary>
        /// Current target being pursued (if any).
        /// </summary>
        Transform CurrentTarget { get; }

        /// <summary>
        /// Whether this monster is currently alive.
        /// </summary>
        bool IsAlive { get; }

        /// <summary>
        /// Initializes the monster with its definition.
        /// </summary>
        /// <param name="definition">Monster stats and behavior config.</param>
        void Initialize(MonsterDefinition definition);

        /// <summary>
        /// Sets the current target for the monster to pursue.
        /// </summary>
        /// <param name="target">Target transform to chase/attack.</param>
        void SetTarget(Transform target);

        /// <summary>
        /// Clears the current target.
        /// </summary>
        void ClearTarget();

        /// <summary>
        /// Called when the monster takes damage.
        /// </summary>
        /// <param name="damage">Damage amount.</param>
        /// <param name="knockback">Knockback direction and force.</param>
        void OnDamaged(float damage, Vector3 knockback);

        /// <summary>
        /// Called when the monster dies.
        /// </summary>
        void OnDeath();

        /// <summary>
        /// Forces a state change (for external control).
        /// </summary>
        /// <param name="newState">State to transition to.</param>
        void ForceState(MonsterState newState);

        // Phase 3 will add these methods:
        // void JoinSwarm(SwarmCoordinator swarm);
        // void LeaveSwarm();
        // void OnPortalCommand(PortalCommand command);
        // void SetHomePortal(Portal portal);
    }
}
