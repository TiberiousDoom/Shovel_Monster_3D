using System;
using UnityEngine;

namespace VoxelRPG.Player.Skills
{
    /// <summary>
    /// Manages player experience points and level progression.
    /// </summary>
    [Serializable]
    public class ExperienceSystem
    {
        private const int BASE_XP = 100;
        private const float EXPONENT_FACTOR = 1.5f;
        private const int MAX_LEVEL = 50;

        [SerializeField] private int _currentXP;
        [SerializeField] private int _currentLevel = 1;

        /// <summary>
        /// Current total experience points.
        /// </summary>
        public int CurrentXP => _currentXP;

        /// <summary>
        /// Current player level.
        /// </summary>
        public int CurrentLevel => _currentLevel;

        /// <summary>
        /// XP required to reach the next level.
        /// </summary>
        public int XPToNextLevel => CalculateXPForLevel(_currentLevel + 1);

        /// <summary>
        /// XP earned towards the next level.
        /// </summary>
        public int XPInCurrentLevel => _currentXP - CalculateTotalXPForLevel(_currentLevel);

        /// <summary>
        /// Progress towards next level (0-1).
        /// </summary>
        public float LevelProgress
        {
            get
            {
                if (_currentLevel >= MAX_LEVEL) return 1f;
                int xpForCurrent = CalculateTotalXPForLevel(_currentLevel);
                int xpForNext = CalculateTotalXPForLevel(_currentLevel + 1);
                int xpNeeded = xpForNext - xpForCurrent;
                int xpProgress = _currentXP - xpForCurrent;
                return xpNeeded > 0 ? (float)xpProgress / xpNeeded : 1f;
            }
        }

        /// <summary>
        /// Whether the player has reached maximum level.
        /// </summary>
        public bool IsMaxLevel => _currentLevel >= MAX_LEVEL;

        /// <summary>
        /// Event fired when player gains XP.
        /// </summary>
        public event Action<int> OnXPGained;

        /// <summary>
        /// Event fired when player levels up.
        /// </summary>
        public event Action<int> OnLevelUp;

        /// <summary>
        /// Adds experience points and handles level ups.
        /// </summary>
        /// <param name="amount">Amount of XP to add.</param>
        public void AddExperience(int amount)
        {
            if (amount <= 0 || IsMaxLevel) return;

            _currentXP += amount;
            OnXPGained?.Invoke(amount);

            // Check for level ups
            while (!IsMaxLevel && _currentXP >= CalculateTotalXPForLevel(_currentLevel + 1))
            {
                _currentLevel++;
                OnLevelUp?.Invoke(_currentLevel);
                Debug.Log($"[ExperienceSystem] Level up! Now level {_currentLevel}");
            }
        }

        /// <summary>
        /// Calculates XP required to go from one level to the next.
        /// </summary>
        public static int CalculateXPForLevel(int level)
        {
            if (level <= 1) return 0;
            return Mathf.RoundToInt(BASE_XP * Mathf.Pow(level - 1, EXPONENT_FACTOR));
        }

        /// <summary>
        /// Calculates total XP needed to reach a specific level.
        /// </summary>
        public static int CalculateTotalXPForLevel(int level)
        {
            int total = 0;
            for (int i = 2; i <= level; i++)
            {
                total += CalculateXPForLevel(i);
            }
            return total;
        }

        /// <summary>
        /// Sets the level directly (used for loading saves).
        /// </summary>
        public void SetLevel(int level)
        {
            _currentLevel = Mathf.Clamp(level, 1, MAX_LEVEL);
        }

        /// <summary>
        /// Sets the XP directly (used for loading saves).
        /// </summary>
        public void SetXP(int xp)
        {
            _currentXP = Mathf.Max(0, xp);
        }

        /// <summary>
        /// Creates save data for this experience system.
        /// </summary>
        public ExperienceSaveData CreateSaveData()
        {
            return new ExperienceSaveData
            {
                CurrentXP = _currentXP,
                CurrentLevel = _currentLevel
            };
        }

        /// <summary>
        /// Loads data from save.
        /// </summary>
        public void LoadFromSaveData(ExperienceSaveData data)
        {
            if (data == null) return;
            _currentXP = data.CurrentXP;
            _currentLevel = data.CurrentLevel;
        }
    }

    /// <summary>
    /// Save data for the experience system.
    /// </summary>
    [Serializable]
    public class ExperienceSaveData
    {
        public int CurrentXP;
        public int CurrentLevel;
    }
}
