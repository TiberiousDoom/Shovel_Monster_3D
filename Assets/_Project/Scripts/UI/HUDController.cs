using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoxelRPG.Core;
using VoxelRPG.Core.Items;
using VoxelRPG.Player;

namespace VoxelRPG.UI
{
    /// <summary>
    /// Controls the in-game HUD displaying health, hunger, and hotbar.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Health Display")]
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private Image _healthFill;
        [SerializeField] private TextMeshProUGUI _healthText;
        [SerializeField] private Color _healthFullColor = Color.green;
        [SerializeField] private Color _healthLowColor = Color.red;
        [SerializeField] private float _lowHealthThreshold = 0.25f;

        [Header("Hunger Display")]
        [SerializeField] private Slider _hungerSlider;
        [SerializeField] private Image _hungerFill;
        [SerializeField] private TextMeshProUGUI _hungerText;
        [SerializeField] private Color _hungerFullColor = new Color(0.8f, 0.6f, 0.2f);
        [SerializeField] private Color _hungerLowColor = new Color(0.4f, 0.2f, 0f);
        [SerializeField] private float _lowHungerThreshold = 0.25f;

        [Header("Hotbar")]
        [SerializeField] private HotbarSlotUI[] _hotbarSlots;
        [SerializeField] private int _hotbarSize = 9;

        [Header("Time Display")]
        [SerializeField] private TextMeshProUGUI _timeText;
        [SerializeField] private TextMeshProUGUI _dayText;
        [SerializeField] private Image _dayNightIcon;
        [SerializeField] private Sprite _dayIcon;
        [SerializeField] private Sprite _nightIcon;

        [Header("Status Effects")]
        [SerializeField] private GameObject _starvingIndicator;

        [Header("Animation")]
        [SerializeField] private float _smoothSpeed = 5f;

        private HealthSystem _healthSystem;
        private HungerSystem _hungerSystem;
        private PlayerInventory _playerInventory;
        private TimeManager _timeManager;

        private float _targetHealthValue;
        private float _targetHungerValue;
        private bool _isInitialized;

        private void Start()
        {
            // Cache references
            CacheReferences();

            // Subscribe to events
            SubscribeToEvents();

            // Initial update
            UpdateHealthDisplay();
            UpdateHungerDisplay();
            UpdateTimeDisplay();

            // Mark as initialized if we found the health system
            _isInitialized = _healthSystem != null;
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            // Retry initialization if player wasn't found at Start
            if (!_isInitialized && _healthSystem == null)
            {
                CacheReferences();
                if (_healthSystem != null)
                {
                    SubscribeToEvents();
                    UpdateHealthDisplay();
                    UpdateHungerDisplay();
                    _isInitialized = true;
                    Debug.Log("[HUDController] Late initialization - player found.");
                }
            }

            // Smooth animation for bars
            if (_healthSlider != null)
            {
                _healthSlider.value = Mathf.Lerp(_healthSlider.value, _targetHealthValue, Time.deltaTime * _smoothSpeed);
            }

            if (_hungerSlider != null)
            {
                _hungerSlider.value = Mathf.Lerp(_hungerSlider.value, _targetHungerValue, Time.deltaTime * _smoothSpeed);
            }

            // Update time display
            UpdateTimeDisplay();
        }

        private void CacheReferences()
        {
            // Try to find player stats
            ServiceLocator.TryGet(out PlayerStats playerStats);
            if (playerStats != null)
            {
                _healthSystem = playerStats.Health;
                _hungerSystem = playerStats.Hunger;
            }
            else
            {
                // Try to find directly
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    _healthSystem = player.GetComponent<HealthSystem>();
                    _hungerSystem = player.GetComponent<HungerSystem>();
                }
            }

            ServiceLocator.TryGet(out _playerInventory);
            ServiceLocator.TryGet(out _timeManager);
        }

        private void SubscribeToEvents()
        {
            if (_healthSystem != null)
            {
                _healthSystem.OnHealthChanged += OnHealthChanged;
            }

            if (_hungerSystem != null)
            {
                _hungerSystem.OnHungerChanged += OnHungerChanged;
                _hungerSystem.OnStarvationStarted += OnStarvationStarted;
                _hungerSystem.OnStarvationEnded += OnStarvationEnded;
            }

            if (_playerInventory != null)
            {
                _playerInventory.OnSlotChanged += OnHotbarSlotChanged;
                _playerInventory.OnHotbarSelectionChanged += OnSelectedSlotChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_healthSystem != null)
            {
                _healthSystem.OnHealthChanged -= OnHealthChanged;
            }

            if (_hungerSystem != null)
            {
                _hungerSystem.OnHungerChanged -= OnHungerChanged;
                _hungerSystem.OnStarvationStarted -= OnStarvationStarted;
                _hungerSystem.OnStarvationEnded -= OnStarvationEnded;
            }

            if (_playerInventory != null)
            {
                _playerInventory.OnSlotChanged -= OnHotbarSlotChanged;
                _playerInventory.OnHotbarSelectionChanged -= OnSelectedSlotChanged;
            }
        }

        #region Health

        private void OnHealthChanged(float current, float max)
        {
            UpdateHealthDisplay();
        }

        private void UpdateHealthDisplay()
        {
            if (_healthSystem == null) return;

            float normalized = _healthSystem.HealthNormalized;
            _targetHealthValue = normalized;

            // Update text
            if (_healthText != null)
            {
                _healthText.text = $"{Mathf.CeilToInt(_healthSystem.CurrentHealth)}/{Mathf.CeilToInt(_healthSystem.MaxHealth)}";
            }

            // Update fill color
            if (_healthFill != null)
            {
                _healthFill.color = normalized <= _lowHealthThreshold
                    ? Color.Lerp(_healthLowColor, _healthFullColor, normalized / _lowHealthThreshold)
                    : _healthFullColor;
            }
        }

        #endregion

        #region Hunger

        private void OnHungerChanged(float current, float max)
        {
            UpdateHungerDisplay();
        }

        private void UpdateHungerDisplay()
        {
            if (_hungerSystem == null) return;

            float normalized = _hungerSystem.HungerNormalized;
            _targetHungerValue = normalized;

            // Update text
            if (_hungerText != null)
            {
                _hungerText.text = $"{Mathf.CeilToInt(_hungerSystem.CurrentHunger)}/{Mathf.CeilToInt(_hungerSystem.MaxHunger)}";
            }

            // Update fill color
            if (_hungerFill != null)
            {
                _hungerFill.color = normalized <= _lowHungerThreshold
                    ? Color.Lerp(_hungerLowColor, _hungerFullColor, normalized / _lowHungerThreshold)
                    : _hungerFullColor;
            }
        }

        private void OnStarvationStarted()
        {
            if (_starvingIndicator != null)
            {
                _starvingIndicator.SetActive(true);
            }
        }

        private void OnStarvationEnded()
        {
            if (_starvingIndicator != null)
            {
                _starvingIndicator.SetActive(false);
            }
        }

        #endregion

        #region Hotbar

        private void OnHotbarSlotChanged(int slot, ItemStack stack)
        {
            // Only update if this is a hotbar slot (first N slots are hotbar)
            if (_hotbarSlots != null && slot >= 0 && slot < _hotbarSlots.Length && _playerInventory.IsHotbarSlot(slot))
            {
                _hotbarSlots[slot]?.UpdateSlot(stack);
            }
        }

        private void OnSelectedSlotChanged(int slot)
        {
            UpdateHotbarSelection(slot);
        }

        private void UpdateHotbarSelection(int selectedSlot)
        {
            if (_hotbarSlots == null) return;

            for (int i = 0; i < _hotbarSlots.Length; i++)
            {
                if (_hotbarSlots[i] != null)
                {
                    _hotbarSlots[i].SetSelected(i == selectedSlot);
                }
            }
        }

        /// <summary>
        /// Initializes hotbar slots.
        /// </summary>
        public void InitializeHotbar()
        {
            if (_hotbarSlots == null || _playerInventory == null) return;

            for (int i = 0; i < _hotbarSlots.Length && i < _hotbarSize; i++)
            {
                var stack = _playerInventory.GetHotbarSlot(i);
                _hotbarSlots[i]?.UpdateSlot(stack);
            }

            UpdateHotbarSelection(_playerInventory.SelectedHotbarSlot);
        }

        #endregion

        #region Time

        private void UpdateTimeDisplay()
        {
            if (_timeManager == null) return;

            // Update time text
            if (_timeText != null)
            {
                _timeText.text = _timeManager.GetFormattedTime();
            }

            // Update day text
            if (_dayText != null)
            {
                _dayText.text = $"Day {_timeManager.CurrentDay}";
            }

            // Update day/night icon
            if (_dayNightIcon != null)
            {
                _dayNightIcon.sprite = _timeManager.IsDay ? _dayIcon : _nightIcon;
            }
        }

        #endregion

        /// <summary>
        /// Refreshes all HUD elements.
        /// </summary>
        public void RefreshAll()
        {
            CacheReferences();
            SubscribeToEvents();
            UpdateHealthDisplay();
            UpdateHungerDisplay();
            UpdateTimeDisplay();
            InitializeHotbar();
        }
    }
}
