using System;

namespace VoxelRPG.Player.Skills
{
    /// <summary>
    /// Runtime data for a single skill instance.
    /// </summary>
    [Serializable]
    public class SkillData
    {
        private SkillDefinition _definition;
        private int _currentLevel;

        /// <summary>
        /// The skill definition this data is based on.
        /// </summary>
        public SkillDefinition Definition => _definition;

        /// <summary>
        /// Current level of this skill (0 = not learned).
        /// </summary>
        public int CurrentLevel => _currentLevel;

        /// <summary>
        /// Whether this skill has reached maximum level.
        /// </summary>
        public bool IsMaxLevel => _currentLevel >= _definition.MaxLevel;

        /// <summary>
        /// Current bonus value based on level.
        /// </summary>
        public float CurrentBonus => _definition.GetBonusAtLevel(_currentLevel);

        /// <summary>
        /// Event fired when skill level changes.
        /// </summary>
        public event Action<SkillData> OnLevelChanged;

        public SkillData(SkillDefinition definition)
        {
            _definition = definition;
            _currentLevel = 0;
        }

        /// <summary>
        /// Attempts to level up the skill.
        /// </summary>
        /// <returns>True if level up succeeded.</returns>
        public bool TryLevelUp()
        {
            if (IsMaxLevel)
            {
                return false;
            }

            _currentLevel++;
            OnLevelChanged?.Invoke(this);
            return true;
        }

        /// <summary>
        /// Sets the skill level directly (used for loading saves).
        /// </summary>
        public void SetLevel(int level)
        {
            _currentLevel = Math.Clamp(level, 0, _definition.MaxLevel);
            OnLevelChanged?.Invoke(this);
        }

        /// <summary>
        /// Gets the formatted bonus string for UI display.
        /// </summary>
        public string GetFormattedBonus()
        {
            return _definition.GetFormattedBonus(_currentLevel);
        }
    }
}
