using UnityEngine;

namespace VoxelRPG.Player.Skills
{
    /// <summary>
    /// Defines a skill type with its properties and bonuses.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkill", menuName = "VoxelRPG/Skills/Skill Definition")]
    public class SkillDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _skillId;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea(2, 4)] private string _description;
        [SerializeField] private Sprite _icon;

        [Header("Category")]
        [SerializeField] private SkillCategory _category = SkillCategory.Combat;

        [Header("Progression")]
        [SerializeField] private int _maxLevel = 10;
        [SerializeField] private float _bonusPerLevel = 0.05f;
        [SerializeField] private SkillBonusType _bonusType = SkillBonusType.Multiplicative;
        [SerializeField] private SkillEffectType _effectType = SkillEffectType.MeleeDamage;

        [Header("Display")]
        [SerializeField] private string _bonusFormat = "+{0:P0}";
        [SerializeField] private string _bonusDescription = "Melee Damage";

        /// <summary>
        /// Unique identifier for this skill.
        /// </summary>
        public string SkillId => _skillId;

        /// <summary>
        /// Display name shown in UI.
        /// </summary>
        public string DisplayName => _displayName;

        /// <summary>
        /// Detailed description of the skill.
        /// </summary>
        public string Description => _description;

        /// <summary>
        /// Icon shown in UI.
        /// </summary>
        public Sprite Icon => _icon;

        /// <summary>
        /// Category this skill belongs to.
        /// </summary>
        public SkillCategory Category => _category;

        /// <summary>
        /// Maximum level this skill can reach.
        /// </summary>
        public int MaxLevel => _maxLevel;

        /// <summary>
        /// Bonus gained per skill level.
        /// </summary>
        public float BonusPerLevel => _bonusPerLevel;

        /// <summary>
        /// How the bonus is applied (additive or multiplicative).
        /// </summary>
        public SkillBonusType BonusType => _bonusType;

        /// <summary>
        /// What gameplay aspect this skill affects.
        /// </summary>
        public SkillEffectType EffectType => _effectType;

        /// <summary>
        /// Format string for displaying the bonus value.
        /// </summary>
        public string BonusFormat => _bonusFormat;

        /// <summary>
        /// Description of what the bonus affects.
        /// </summary>
        public string BonusDescription => _bonusDescription;

        /// <summary>
        /// Calculates the total bonus at a given level.
        /// </summary>
        public float GetBonusAtLevel(int level)
        {
            return level * _bonusPerLevel;
        }

        /// <summary>
        /// Gets the formatted bonus string for display.
        /// </summary>
        public string GetFormattedBonus(int level)
        {
            float bonus = GetBonusAtLevel(level);
            return string.Format(_bonusFormat, bonus) + " " + _bonusDescription;
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_skillId))
            {
                _skillId = name.ToLower().Replace(" ", "_");
            }
        }
    }

    /// <summary>
    /// Categories for organizing skills.
    /// </summary>
    public enum SkillCategory
    {
        Combat,
        Gathering,
        Survival
    }

    /// <summary>
    /// How skill bonuses are applied.
    /// </summary>
    public enum SkillBonusType
    {
        /// <summary>
        /// Bonus is added directly (e.g., +50 HP).
        /// </summary>
        Additive,

        /// <summary>
        /// Bonus is a multiplier (e.g., +50% damage).
        /// </summary>
        Multiplicative
    }

    /// <summary>
    /// What gameplay aspect a skill affects.
    /// </summary>
    public enum SkillEffectType
    {
        MaxHealth,
        MeleeDamage,
        DamageReduction,
        MiningSpeed,
        WoodcuttingSpeed,
        HungerDecay,
        HealingReceived
    }
}
