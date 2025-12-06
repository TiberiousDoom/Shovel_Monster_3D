using System;
using System.Collections.Generic;
using UnityEngine;
using VoxelRPG.Core;
using VoxelRPG.Utilities.Save;

namespace VoxelRPG.Player.Skills
{
    /// <summary>
    /// Central manager for player skills and experience.
    /// Handles skill progression, XP gains, and save/load.
    /// </summary>
    public class SkillSystem : MonoBehaviour, ISaveable
    {
        private const string SAVE_ID = "player_skills";

        [Header("Skill Definitions")]
        [SerializeField] private List<SkillDefinition> _skillDefinitions = new List<SkillDefinition>();

        private Dictionary<string, SkillData> _skills = new Dictionary<string, SkillData>();
        private ExperienceSystem _experienceSystem = new ExperienceSystem();
        private int _availableSkillPoints;

        /// <summary>
        /// Current player level.
        /// </summary>
        public int PlayerLevel => _experienceSystem.CurrentLevel;

        /// <summary>
        /// Current XP.
        /// </summary>
        public int CurrentXP => _experienceSystem.CurrentXP;

        /// <summary>
        /// XP required to reach the next level.
        /// </summary>
        public int XPToNextLevel => _experienceSystem.XPToNextLevel;

        /// <summary>
        /// Progress towards the next level (0-1).
        /// </summary>
        public float LevelProgress => _experienceSystem.LevelProgress;

        /// <summary>
        /// Available skill points to spend.
        /// </summary>
        public int AvailableSkillPoints => _availableSkillPoints;

        /// <summary>
        /// All skill definitions registered in the system.
        /// </summary>
        public IReadOnlyList<SkillDefinition> SkillDefinitions => _skillDefinitions;

        /// <summary>
        /// Event fired when XP is gained.
        /// </summary>
        public event Action<int> OnXPGained;

        /// <summary>
        /// Event fired when player levels up.
        /// </summary>
        public event Action<int> OnLevelUp;

        /// <summary>
        /// Event fired when a skill level changes.
        /// </summary>
        public event Action<string, int> OnSkillLevelChanged;

        /// <summary>
        /// Event fired when available skill points change.
        /// </summary>
        public event Action<int> OnSkillPointsChanged;

        #region ISaveable Implementation

        public string SaveId => SAVE_ID;

        public object CaptureState()
        {
            var saveData = new SkillSystemSaveData
            {
                ExperienceData = _experienceSystem.CreateSaveData(),
                AvailableSkillPoints = _availableSkillPoints,
                Skills = new List<SkillSaveData>()
            };

            foreach (var kvp in _skills)
            {
                saveData.Skills.Add(new SkillSaveData
                {
                    SkillId = kvp.Key,
                    Level = kvp.Value.CurrentLevel
                });
            }

            return saveData;
        }

        public void RestoreState(object state)
        {
            if (state is SkillSystemSaveData saveData)
            {
                _experienceSystem.LoadFromSaveData(saveData.ExperienceData);
                _availableSkillPoints = saveData.AvailableSkillPoints;

                foreach (var skillSave in saveData.Skills)
                {
                    if (_skills.TryGetValue(skillSave.SkillId, out var skillData))
                    {
                        skillData.SetLevel(skillSave.Level);
                    }
                }

                Debug.Log($"[SkillSystem] Loaded - Level {PlayerLevel}, {_availableSkillPoints} skill points");
            }
        }

        #endregion

        private void Awake()
        {
            ServiceLocator.Register(this);
            InitializeSkills();
            SubscribeToExperienceEvents();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<SkillSystem>();
            UnsubscribeFromExperienceEvents();
        }

        private void InitializeSkills()
        {
            _skills.Clear();

            foreach (var definition in _skillDefinitions)
            {
                if (definition == null) continue;
                if (string.IsNullOrEmpty(definition.SkillId))
                {
                    Debug.LogWarning($"[SkillSystem] Skill definition '{definition.name}' has no SkillId");
                    continue;
                }

                var skillData = new SkillData(definition);
                skillData.OnLevelChanged += HandleSkillLevelChanged;
                _skills[definition.SkillId] = skillData;
            }

            Debug.Log($"[SkillSystem] Initialized {_skills.Count} skills");
        }

        private void SubscribeToExperienceEvents()
        {
            _experienceSystem.OnXPGained += HandleXPGained;
            _experienceSystem.OnLevelUp += HandleLevelUp;
        }

        private void UnsubscribeFromExperienceEvents()
        {
            _experienceSystem.OnXPGained -= HandleXPGained;
            _experienceSystem.OnLevelUp -= HandleLevelUp;
        }

        private void HandleXPGained(int amount)
        {
            OnXPGained?.Invoke(amount);
        }

        private void HandleLevelUp(int newLevel)
        {
            // Grant skill point on level up
            _availableSkillPoints++;
            OnSkillPointsChanged?.Invoke(_availableSkillPoints);
            OnLevelUp?.Invoke(newLevel);
            Debug.Log($"[SkillSystem] Level up to {newLevel}! Skill points: {_availableSkillPoints}");
        }

        private void HandleSkillLevelChanged(SkillData skillData)
        {
            OnSkillLevelChanged?.Invoke(skillData.Definition.SkillId, skillData.CurrentLevel);
        }

        /// <summary>
        /// Awards experience points to the player.
        /// </summary>
        public void AddExperience(int amount)
        {
            _experienceSystem.AddExperience(amount);
        }

        /// <summary>
        /// Gets the current level of a skill.
        /// </summary>
        public int GetSkillLevel(string skillId)
        {
            return _skills.TryGetValue(skillId, out var skillData) ? skillData.CurrentLevel : 0;
        }

        /// <summary>
        /// Gets the current bonus value of a skill.
        /// </summary>
        public float GetSkillBonus(string skillId)
        {
            return _skills.TryGetValue(skillId, out var skillData) ? skillData.CurrentBonus : 0f;
        }

        /// <summary>
        /// Gets the skill data for a specific skill.
        /// </summary>
        public SkillData GetSkillData(string skillId)
        {
            return _skills.TryGetValue(skillId, out var skillData) ? skillData : null;
        }

        /// <summary>
        /// Gets the bonus for a specific effect type by aggregating all relevant skills.
        /// </summary>
        public float GetEffectBonus(SkillEffectType effectType)
        {
            float totalBonus = 0f;

            foreach (var kvp in _skills)
            {
                var skill = kvp.Value;
                if (skill.Definition.EffectType == effectType)
                {
                    totalBonus += skill.CurrentBonus;
                }
            }

            return totalBonus;
        }

        /// <summary>
        /// Attempts to level up a skill.
        /// </summary>
        /// <returns>True if successful.</returns>
        public bool TryLevelUpSkill(string skillId)
        {
            if (_availableSkillPoints <= 0)
            {
                Debug.Log("[SkillSystem] No skill points available");
                return false;
            }

            if (!_skills.TryGetValue(skillId, out var skillData))
            {
                Debug.LogWarning($"[SkillSystem] Unknown skill: {skillId}");
                return false;
            }

            if (skillData.IsMaxLevel)
            {
                Debug.Log($"[SkillSystem] Skill '{skillId}' is already at max level");
                return false;
            }

            if (skillData.TryLevelUp())
            {
                _availableSkillPoints--;
                OnSkillPointsChanged?.Invoke(_availableSkillPoints);
                Debug.Log($"[SkillSystem] Leveled up '{skillId}' to level {skillData.CurrentLevel}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets all skills in a specific category.
        /// </summary>
        public List<SkillData> GetSkillsByCategory(SkillCategory category)
        {
            var result = new List<SkillData>();
            foreach (var kvp in _skills)
            {
                if (kvp.Value.Definition.Category == category)
                {
                    result.Add(kvp.Value);
                }
            }
            return result;
        }

        /// <summary>
        /// Gets all skill data.
        /// </summary>
        public IEnumerable<SkillData> GetAllSkills()
        {
            return _skills.Values;
        }
    }

    /// <summary>
    /// Save data for the entire skill system.
    /// </summary>
    [Serializable]
    public class SkillSystemSaveData
    {
        public ExperienceSaveData ExperienceData;
        public int AvailableSkillPoints;
        public List<SkillSaveData> Skills;
    }

    /// <summary>
    /// Save data for a single skill.
    /// </summary>
    [Serializable]
    public class SkillSaveData
    {
        public string SkillId;
        public int Level;
    }
}
