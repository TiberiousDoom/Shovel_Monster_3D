using VoxelRPG.Core;

namespace VoxelRPG.Player.Skills
{
    /// <summary>
    /// Static helper class for retrieving skill bonuses.
    /// Provides convenient methods for other systems to query skill effects.
    /// </summary>
    public static class SkillModifiers
    {
        /// <summary>
        /// Gets the bonus max health from Vitality skill.
        /// </summary>
        public static float GetBonusMaxHealth()
        {
            var skillSystem = ServiceLocator.Get<SkillSystem>();
            if (skillSystem == null) return 0f;
            return skillSystem.GetEffectBonus(SkillEffectType.MaxHealth);
        }

        /// <summary>
        /// Gets the melee damage multiplier from Strength skill.
        /// Returns a multiplier (e.g., 0.5 for +50% damage).
        /// </summary>
        public static float GetMeleeDamageBonus()
        {
            var skillSystem = ServiceLocator.Get<SkillSystem>();
            if (skillSystem == null) return 0f;
            return skillSystem.GetEffectBonus(SkillEffectType.MeleeDamage);
        }

        /// <summary>
        /// Gets the damage reduction from Toughness skill.
        /// Returns a reduction (e.g., 0.3 for 30% less damage taken).
        /// </summary>
        public static float GetDamageReduction()
        {
            var skillSystem = ServiceLocator.Get<SkillSystem>();
            if (skillSystem == null) return 0f;
            return skillSystem.GetEffectBonus(SkillEffectType.DamageReduction);
        }

        /// <summary>
        /// Gets the mining speed bonus from Mining skill.
        /// Returns a bonus (e.g., 0.8 for +80% faster mining).
        /// </summary>
        public static float GetMiningSpeedBonus()
        {
            var skillSystem = ServiceLocator.Get<SkillSystem>();
            if (skillSystem == null) return 0f;
            return skillSystem.GetEffectBonus(SkillEffectType.MiningSpeed);
        }

        /// <summary>
        /// Gets the woodcutting speed bonus from Woodcutting skill.
        /// Returns a bonus (e.g., 0.8 for +80% faster chopping).
        /// </summary>
        public static float GetWoodcuttingSpeedBonus()
        {
            var skillSystem = ServiceLocator.Get<SkillSystem>();
            if (skillSystem == null) return 0f;
            return skillSystem.GetEffectBonus(SkillEffectType.WoodcuttingSpeed);
        }

        /// <summary>
        /// Gets the hunger decay modifier from Endurance skill.
        /// Returns a modifier (e.g., 0.5 for 50% slower hunger decay).
        /// </summary>
        public static float GetHungerDecayReduction()
        {
            var skillSystem = ServiceLocator.Get<SkillSystem>();
            if (skillSystem == null) return 0f;
            return skillSystem.GetEffectBonus(SkillEffectType.HungerDecay);
        }

        /// <summary>
        /// Gets the healing bonus from Fortitude skill.
        /// Returns a bonus (e.g., 0.5 for +50% healing received).
        /// </summary>
        public static float GetHealingBonus()
        {
            var skillSystem = ServiceLocator.Get<SkillSystem>();
            if (skillSystem == null) return 0f;
            return skillSystem.GetEffectBonus(SkillEffectType.HealingReceived);
        }

        /// <summary>
        /// Calculates the effective damage after applying skill modifiers.
        /// </summary>
        /// <param name="baseDamage">Base damage before skills.</param>
        /// <returns>Damage with strength bonus applied.</returns>
        public static float CalculateMeleeDamage(float baseDamage)
        {
            float bonus = GetMeleeDamageBonus();
            return baseDamage * (1f + bonus);
        }

        /// <summary>
        /// Calculates the effective damage taken after applying toughness.
        /// </summary>
        /// <param name="incomingDamage">Damage before reduction.</param>
        /// <returns>Damage after toughness reduction.</returns>
        public static float CalculateDamageTaken(float incomingDamage)
        {
            float reduction = GetDamageReduction();
            return incomingDamage * (1f - reduction);
        }

        /// <summary>
        /// Calculates effective healing after applying fortitude.
        /// </summary>
        /// <param name="baseHealing">Base healing amount.</param>
        /// <returns>Healing with fortitude bonus applied.</returns>
        public static float CalculateHealing(float baseHealing)
        {
            float bonus = GetHealingBonus();
            return baseHealing * (1f + bonus);
        }

        /// <summary>
        /// Calculates the effective hunger decay rate.
        /// </summary>
        /// <param name="baseDecayRate">Base decay rate.</param>
        /// <returns>Decay rate reduced by endurance.</returns>
        public static float CalculateHungerDecay(float baseDecayRate)
        {
            float reduction = GetHungerDecayReduction();
            return baseDecayRate * (1f - reduction);
        }

        /// <summary>
        /// Calculates the effective mining speed.
        /// </summary>
        /// <param name="baseSpeed">Base mining speed.</param>
        /// <returns>Speed with mining skill bonus applied.</returns>
        public static float CalculateMiningSpeed(float baseSpeed)
        {
            float bonus = GetMiningSpeedBonus();
            return baseSpeed * (1f + bonus);
        }

        /// <summary>
        /// Calculates the effective woodcutting speed.
        /// </summary>
        /// <param name="baseSpeed">Base chopping speed.</param>
        /// <returns>Speed with woodcutting skill bonus applied.</returns>
        public static float CalculateWoodcuttingSpeed(float baseSpeed)
        {
            float bonus = GetWoodcuttingSpeedBonus();
            return baseSpeed * (1f + bonus);
        }
    }
}
