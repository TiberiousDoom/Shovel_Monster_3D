using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoxelRPG.Core;
using VoxelRPG.Player.Skills;

namespace VoxelRPG.UI
{
    /// <summary>
    /// UI for displaying and upgrading player skills.
    /// </summary>
    public class SkillsUI : MonoBehaviour
    {
        [Header("Level Display")]
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private Slider _xpSlider;
        [SerializeField] private TextMeshProUGUI _xpText;
        [SerializeField] private TextMeshProUGUI _skillPointsText;

        [Header("Skill Containers")]
        [SerializeField] private Transform _combatSkillsContainer;
        [SerializeField] private Transform _gatheringSkillsContainer;
        [SerializeField] private Transform _survivalSkillsContainer;

        [Header("Skill Slot Prefab")]
        [SerializeField] private GameObject _skillSlotPrefab;

        [Header("Colors")]
        [SerializeField] private Color _canUpgradeColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _maxLevelColor = new Color(0.8f, 0.8f, 0.2f);
        [SerializeField] private Color _lockedColor = new Color(0.5f, 0.5f, 0.5f);

        private SkillSystem _skillSystem;
        private Dictionary<string, SkillSlotUI> _skillSlots = new Dictionary<string, SkillSlotUI>();

        private void OnEnable()
        {
            if (_skillSystem == null)
            {
                _skillSystem = ServiceLocator.Get<SkillSystem>();
            }

            if (_skillSystem != null)
            {
                _skillSystem.OnXPGained += OnXPGained;
                _skillSystem.OnLevelUp += OnLevelUp;
                _skillSystem.OnSkillLevelChanged += OnSkillLevelChanged;
                _skillSystem.OnSkillPointsChanged += OnSkillPointsChanged;
            }

            RefreshUI();
        }

        private void OnDisable()
        {
            if (_skillSystem != null)
            {
                _skillSystem.OnXPGained -= OnXPGained;
                _skillSystem.OnLevelUp -= OnLevelUp;
                _skillSystem.OnSkillLevelChanged -= OnSkillLevelChanged;
                _skillSystem.OnSkillPointsChanged -= OnSkillPointsChanged;
            }
        }

        /// <summary>
        /// Initializes the UI with skill slots for all registered skills.
        /// </summary>
        public void Initialize()
        {
            _skillSystem = ServiceLocator.Get<SkillSystem>();
            if (_skillSystem == null)
            {
                Debug.LogWarning("[SkillsUI] SkillSystem not found in ServiceLocator");
                return;
            }

            CreateSkillSlots();
            RefreshUI();
        }

        private void CreateSkillSlots()
        {
            if (_skillSystem == null) return;

            foreach (var skillData in _skillSystem.GetAllSkills())
            {
                Transform container = GetContainerForCategory(skillData.Definition.Category);
                if (container == null) continue;

                var slotUI = CreateSkillSlot(container, skillData);
                _skillSlots[skillData.Definition.SkillId] = slotUI;
            }
        }

        private Transform GetContainerForCategory(SkillCategory category)
        {
            return category switch
            {
                SkillCategory.Combat => _combatSkillsContainer,
                SkillCategory.Gathering => _gatheringSkillsContainer,
                SkillCategory.Survival => _survivalSkillsContainer,
                _ => null
            };
        }

        private SkillSlotUI CreateSkillSlot(Transform parent, SkillData skillData)
        {
            GameObject slotObj;

            if (_skillSlotPrefab != null)
            {
                slotObj = Instantiate(_skillSlotPrefab, parent);
            }
            else
            {
                slotObj = CreateDefaultSkillSlot(parent);
            }

            var slotUI = slotObj.GetComponent<SkillSlotUI>();
            if (slotUI == null)
            {
                slotUI = slotObj.AddComponent<SkillSlotUI>();
            }

            slotUI.Initialize(skillData, this);
            return slotUI;
        }

        private GameObject CreateDefaultSkillSlot(Transform parent)
        {
            var slotObj = new GameObject("SkillSlot");
            slotObj.transform.SetParent(parent, false);

            var rect = slotObj.AddComponent<RectTransform>();
            var layout = slotObj.AddComponent<LayoutElement>();
            layout.preferredHeight = 40;
            layout.flexibleWidth = 1;

            var bg = slotObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            var horizontalLayout = slotObj.AddComponent<HorizontalLayoutGroup>();
            horizontalLayout.spacing = 8;
            horizontalLayout.padding = new RectOffset(8, 8, 4, 4);
            horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
            horizontalLayout.childControlWidth = false;
            horizontalLayout.childControlHeight = true;
            horizontalLayout.childForceExpandWidth = false;
            horizontalLayout.childForceExpandHeight = true;

            // Icon placeholder
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slotObj.transform, false);
            var iconImage = iconObj.AddComponent<Image>();
            iconImage.color = new Color(0.4f, 0.4f, 0.4f);
            var iconLayout = iconObj.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 32;
            iconLayout.preferredHeight = 32;

            // Name text
            var nameObj = new GameObject("Name");
            nameObj.transform.SetParent(slotObj.transform, false);
            var nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = "Skill Name";
            nameText.fontSize = 14;
            nameText.alignment = TextAlignmentOptions.Left;
            var nameLayout = nameObj.AddComponent<LayoutElement>();
            nameLayout.preferredWidth = 100;

            // Level text
            var levelObj = new GameObject("Level");
            levelObj.transform.SetParent(slotObj.transform, false);
            var levelText = levelObj.AddComponent<TextMeshProUGUI>();
            levelText.text = "Lv. 0";
            levelText.fontSize = 14;
            levelText.alignment = TextAlignmentOptions.Center;
            var levelLayout = levelObj.AddComponent<LayoutElement>();
            levelLayout.preferredWidth = 50;

            // Bonus text
            var bonusObj = new GameObject("Bonus");
            bonusObj.transform.SetParent(slotObj.transform, false);
            var bonusText = bonusObj.AddComponent<TextMeshProUGUI>();
            bonusText.text = "+0%";
            bonusText.fontSize = 12;
            bonusText.alignment = TextAlignmentOptions.Right;
            var bonusLayout = bonusObj.AddComponent<LayoutElement>();
            bonusLayout.preferredWidth = 100;
            bonusLayout.flexibleWidth = 1;

            // Upgrade button
            var upgradeBtn = new GameObject("UpgradeButton");
            upgradeBtn.transform.SetParent(slotObj.transform, false);
            var btnImage = upgradeBtn.AddComponent<Image>();
            btnImage.color = new Color(0.3f, 0.6f, 0.3f);
            var btn = upgradeBtn.AddComponent<Button>();
            var btnLayout = upgradeBtn.AddComponent<LayoutElement>();
            btnLayout.preferredWidth = 32;
            btnLayout.preferredHeight = 32;

            var btnTextObj = new GameObject("Text");
            btnTextObj.transform.SetParent(upgradeBtn.transform, false);
            var btnRect = btnTextObj.AddComponent<RectTransform>();
            btnRect.anchorMin = Vector2.zero;
            btnRect.anchorMax = Vector2.one;
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;
            var btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
            btnText.text = "+";
            btnText.fontSize = 20;
            btnText.alignment = TextAlignmentOptions.Center;

            // Add SkillSlotUI component with references
            var slotUI = slotObj.AddComponent<SkillSlotUI>();
            SetPrivateField(slotUI, "_iconImage", iconImage);
            SetPrivateField(slotUI, "_nameText", nameText);
            SetPrivateField(slotUI, "_levelText", levelText);
            SetPrivateField(slotUI, "_bonusText", bonusText);
            SetPrivateField(slotUI, "_upgradeButton", btn);

            return slotObj;
        }

        private void RefreshUI()
        {
            if (_skillSystem == null) return;

            // Update level display
            if (_levelText != null)
            {
                _levelText.text = $"Level {_skillSystem.PlayerLevel}";
            }

            // Update XP bar
            if (_xpSlider != null)
            {
                _xpSlider.value = _skillSystem.LevelProgress;
            }

            if (_xpText != null)
            {
                int currentXPInLevel = _skillSystem.CurrentXP - ExperienceSystem.CalculateTotalXPForLevel(_skillSystem.PlayerLevel);
                int xpNeeded = ExperienceSystem.CalculateXPForLevel(_skillSystem.PlayerLevel + 1);
                _xpText.text = $"{currentXPInLevel} / {xpNeeded} XP";
            }

            // Update skill points
            if (_skillPointsText != null)
            {
                _skillPointsText.text = $"Skill Points: {_skillSystem.AvailableSkillPoints}";
            }

            // Update all skill slots
            foreach (var kvp in _skillSlots)
            {
                kvp.Value.Refresh();
            }
        }

        /// <summary>
        /// Called when a skill slot's upgrade button is clicked.
        /// </summary>
        public void OnUpgradeClicked(string skillId)
        {
            if (_skillSystem == null) return;

            if (_skillSystem.TryLevelUpSkill(skillId))
            {
                RefreshUI();
            }
        }

        /// <summary>
        /// Gets whether a skill can be upgraded.
        /// </summary>
        public bool CanUpgradeSkill(string skillId)
        {
            if (_skillSystem == null) return false;

            var skillData = _skillSystem.GetSkillData(skillId);
            if (skillData == null) return false;

            return _skillSystem.AvailableSkillPoints > 0 && !skillData.IsMaxLevel;
        }

        private void OnXPGained(int amount)
        {
            RefreshUI();
        }

        private void OnLevelUp(int newLevel)
        {
            RefreshUI();
        }

        private void OnSkillLevelChanged(string skillId, int newLevel)
        {
            if (_skillSlots.TryGetValue(skillId, out var slotUI))
            {
                slotUI.Refresh();
            }
            RefreshUI();
        }

        private void OnSkillPointsChanged(int newPoints)
        {
            RefreshUI();
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
        }
    }
}
