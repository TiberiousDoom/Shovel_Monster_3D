using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoxelRPG.Player.Skills;

namespace VoxelRPG.UI
{
    /// <summary>
    /// UI component for a single skill slot in the skills panel.
    /// </summary>
    public class SkillSlotUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _bonusText;
        [SerializeField] private Button _upgradeButton;
        [SerializeField] private Image _upgradeButtonImage;

        [Header("Colors")]
        [SerializeField] private Color _canUpgradeColor = new Color(0.3f, 0.7f, 0.3f);
        [SerializeField] private Color _maxLevelColor = new Color(0.7f, 0.7f, 0.2f);
        [SerializeField] private Color _lockedColor = new Color(0.4f, 0.4f, 0.4f);

        private SkillData _skillData;
        private SkillsUI _parentUI;

        /// <summary>
        /// Initializes the slot with skill data.
        /// </summary>
        public void Initialize(SkillData skillData, SkillsUI parentUI)
        {
            _skillData = skillData;
            _parentUI = parentUI;

            if (_upgradeButton != null)
            {
                _upgradeButton.onClick.AddListener(OnUpgradeClicked);
            }

            if (_upgradeButtonImage == null && _upgradeButton != null)
            {
                _upgradeButtonImage = _upgradeButton.GetComponent<Image>();
            }

            Refresh();
        }

        /// <summary>
        /// Refreshes the visual state of this slot.
        /// </summary>
        public void Refresh()
        {
            if (_skillData == null) return;

            var definition = _skillData.Definition;

            // Update icon
            if (_iconImage != null && definition.Icon != null)
            {
                _iconImage.sprite = definition.Icon;
                _iconImage.color = Color.white;
            }

            // Update name
            if (_nameText != null)
            {
                _nameText.text = definition.DisplayName;
            }

            // Update level
            if (_levelText != null)
            {
                _levelText.text = $"Lv. {_skillData.CurrentLevel}";
            }

            // Update bonus
            if (_bonusText != null)
            {
                _bonusText.text = _skillData.GetFormattedBonus();
            }

            // Update upgrade button state
            UpdateUpgradeButton();
        }

        private void UpdateUpgradeButton()
        {
            if (_upgradeButton == null) return;

            bool canUpgrade = _parentUI != null && _parentUI.CanUpgradeSkill(_skillData.Definition.SkillId);
            bool isMaxLevel = _skillData.IsMaxLevel;

            _upgradeButton.interactable = canUpgrade;

            if (_upgradeButtonImage != null)
            {
                if (isMaxLevel)
                {
                    _upgradeButtonImage.color = _maxLevelColor;
                }
                else if (canUpgrade)
                {
                    _upgradeButtonImage.color = _canUpgradeColor;
                }
                else
                {
                    _upgradeButtonImage.color = _lockedColor;
                }
            }
        }

        private void OnUpgradeClicked()
        {
            if (_skillData == null || _parentUI == null) return;
            _parentUI.OnUpgradeClicked(_skillData.Definition.SkillId);
        }

        private void OnDestroy()
        {
            if (_upgradeButton != null)
            {
                _upgradeButton.onClick.RemoveListener(OnUpgradeClicked);
            }
        }
    }
}
