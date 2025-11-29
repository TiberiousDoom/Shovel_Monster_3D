using UnityEngine;

namespace VoxelRPG.Combat
{
    /// <summary>
    /// Shared damage contract for all entities that can take damage.
    /// Implemented by players, NPCs, monsters, and destructible objects.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Current health points.
        /// </summary>
        float CurrentHealth { get; }

        /// <summary>
        /// Maximum health points.
        /// </summary>
        float MaxHealth { get; }

        /// <summary>
        /// Whether this entity is still alive.
        /// </summary>
        bool IsAlive { get; }

        /// <summary>
        /// Applies damage to this entity.
        /// </summary>
        /// <param name="damage">Amount of damage to apply.</param>
        /// <param name="source">Optional source of the damage (for knockback direction, etc.).</param>
        /// <returns>Actual damage dealt after modifiers.</returns>
        float TakeDamage(float damage, GameObject source = null);

        /// <summary>
        /// Heals this entity.
        /// </summary>
        /// <param name="amount">Amount to heal.</param>
        /// <returns>Actual amount healed (may be less if at max health).</returns>
        float Heal(float amount);
    }
}
