using System;
using UnityEngine;
using VoxelRPG.Core.Events;
using VoxelRPG.Player.Skills;

namespace VoxelRPG.Player
{
    /// <summary>
    /// Manages player hunger, food consumption, and starvation.
    /// Works with HealthSystem to apply starvation damage.
    /// </summary>
    public class HungerSystem : MonoBehaviour
    {
        [Header("Hunger Settings")]
        [SerializeField] private float _maxHunger = 100f;
        [SerializeField] private float _startingHunger = 100f;
        [SerializeField] private float _hungerDecayRate = 1f;

        [Header("Starvation Settings")]
        [Tooltip("Hunger level below which starvation begins")]
        [SerializeField] private float _starvationThreshold = 10f;
        [Tooltip("Health damage per second when starving")]
        [SerializeField] private float _starvationDamageRate = 2f;

        [Header("References")]
        [SerializeField] private HealthSystem _healthSystem;

        [Header("Event Channels")]
        [SerializeField] private FloatEventChannel _onHungerChanged;
        [SerializeField] private VoidEventChannel _onStarvationStarted;
        [SerializeField] private VoidEventChannel _onStarvationEnded;

        private float _currentHunger;
        private bool _wasStarving;
        private bool _isActive = true;

        #region Properties

        /// <summary>
        /// Current hunger level (0 = empty, max = full).
        /// </summary>
        public float CurrentHunger => _currentHunger;

        /// <summary>
        /// Maximum hunger capacity.
        /// </summary>
        public float MaxHunger => _maxHunger;

        /// <summary>
        /// Whether the player is currently starving.
        /// </summary>
        public bool IsStarving => _currentHunger <= _starvationThreshold;

        /// <summary>
        /// Normalized hunger value (0-1).
        /// </summary>
        public float HungerNormalized => _maxHunger > 0 ? _currentHunger / _maxHunger : 0f;

        /// <summary>
        /// Hunger decay rate per second.
        /// </summary>
        public float HungerDecayRate
        {
            get => _hungerDecayRate;
            set => _hungerDecayRate = Mathf.Max(0, value);
        }

        /// <summary>
        /// Whether the hunger system is actively updating.
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }

        #endregion

        #region Events

        /// <summary>
        /// Raised when hunger changes. Parameters: current hunger, max hunger.
        /// </summary>
        public event Action<float, float> OnHungerChanged;

        /// <summary>
        /// Raised when starvation begins.
        /// </summary>
        public event Action OnStarvationStarted;

        /// <summary>
        /// Raised when starvation ends.
        /// </summary>
        public event Action OnStarvationEnded;

        #endregion

        private void Awake()
        {
            _currentHunger = _startingHunger;

            if (_healthSystem == null)
            {
                _healthSystem = GetComponent<HealthSystem>();
            }
        }

        private void Update()
        {
            if (!_isActive) return;

            UpdateHungerDecay();
            CheckStarvationState();
            HandleStarvationDamage();
        }

        private void UpdateHungerDecay()
        {
            if (_hungerDecayRate <= 0) return;

            // Apply endurance skill reduction to hunger decay
            float effectiveDecayRate = SkillModifiers.CalculateHungerDecay(_hungerDecayRate);

            float previousHunger = _currentHunger;
            _currentHunger -= effectiveDecayRate * Time.deltaTime;
            _currentHunger = Mathf.Max(0, _currentHunger);

            if (!Mathf.Approximately(previousHunger, _currentHunger))
            {
                RaiseHungerChanged();
            }
        }

        private void CheckStarvationState()
        {
            bool isCurrentlyStarving = IsStarving;

            if (isCurrentlyStarving && !_wasStarving)
            {
                OnStarvationStarted?.Invoke();
                _onStarvationStarted?.RaiseEvent();
            }
            else if (!isCurrentlyStarving && _wasStarving)
            {
                OnStarvationEnded?.Invoke();
                _onStarvationEnded?.RaiseEvent();
            }

            _wasStarving = isCurrentlyStarving;
        }

        private void HandleStarvationDamage()
        {
            if (!IsStarving || _healthSystem == null) return;

            float starvationDamage = _starvationDamageRate * Time.deltaTime;
            _healthSystem.TakeDamage(starvationDamage, gameObject);
        }

        /// <summary>
        /// Feeds the player, restoring hunger.
        /// </summary>
        /// <param name="amount">Amount of hunger to restore.</param>
        /// <returns>Actual amount restored.</returns>
        public float Feed(float amount)
        {
            if (amount <= 0) return 0f;

            float actualFeed = Mathf.Min(amount, _maxHunger - _currentHunger);
            _currentHunger += actualFeed;

            RaiseHungerChanged();

            return actualFeed;
        }

        /// <summary>
        /// Consumes hunger directly (for special actions, sprinting, etc.).
        /// </summary>
        /// <param name="amount">Amount of hunger to consume.</param>
        /// <returns>Actual amount consumed.</returns>
        public float ConsumeHunger(float amount)
        {
            if (amount <= 0) return 0f;

            float actualConsume = Mathf.Min(amount, _currentHunger);
            _currentHunger -= actualConsume;

            RaiseHungerChanged();

            return actualConsume;
        }

        /// <summary>
        /// Checks if the player has enough hunger to perform an action.
        /// </summary>
        /// <param name="cost">Hunger cost of the action.</param>
        /// <returns>True if player has enough hunger.</returns>
        public bool CanAfford(float cost)
        {
            return _currentHunger >= cost;
        }

        /// <summary>
        /// Sets hunger to a specific value.
        /// </summary>
        /// <param name="hunger">Hunger value to set.</param>
        public void SetHunger(float hunger)
        {
            _currentHunger = Mathf.Clamp(hunger, 0, _maxHunger);
            RaiseHungerChanged();
        }

        /// <summary>
        /// Sets maximum hunger, optionally scaling current hunger.
        /// </summary>
        /// <param name="maxHunger">New maximum hunger.</param>
        /// <param name="scaleCurrentHunger">If true, current hunger scales proportionally.</param>
        public void SetMaxHunger(float maxHunger, bool scaleCurrentHunger = false)
        {
            if (maxHunger <= 0) return;

            if (scaleCurrentHunger && _maxHunger > 0)
            {
                float ratio = _currentHunger / _maxHunger;
                _maxHunger = maxHunger;
                _currentHunger = _maxHunger * ratio;
            }
            else
            {
                _maxHunger = maxHunger;
                _currentHunger = Mathf.Min(_currentHunger, _maxHunger);
            }

            RaiseHungerChanged();
        }

        /// <summary>
        /// Resets hunger to starting values.
        /// </summary>
        public void Reset()
        {
            _currentHunger = _startingHunger;
            _wasStarving = false;
            RaiseHungerChanged();
        }

        /// <summary>
        /// Sets the health system reference for starvation damage.
        /// </summary>
        /// <param name="healthSystem">The health system to damage on starvation.</param>
        public void SetHealthSystem(HealthSystem healthSystem)
        {
            _healthSystem = healthSystem;
        }

        private void RaiseHungerChanged()
        {
            OnHungerChanged?.Invoke(_currentHunger, _maxHunger);
            _onHungerChanged?.RaiseEvent(HungerNormalized);
        }

        #region Save/Load Support

        /// <summary>
        /// Gets save data for this component.
        /// </summary>
        public HungerSaveData GetSaveData()
        {
            return new HungerSaveData
            {
                CurrentHunger = _currentHunger,
                MaxHunger = _maxHunger
            };
        }

        /// <summary>
        /// Loads save data into this component.
        /// </summary>
        public void LoadSaveData(HungerSaveData data)
        {
            _maxHunger = data.MaxHunger;
            _currentHunger = data.CurrentHunger;
            RaiseHungerChanged();
        }

        #endregion
    }

    /// <summary>
    /// Serializable save data for HungerSystem.
    /// </summary>
    [System.Serializable]
    public class HungerSaveData
    {
        public float CurrentHunger;
        public float MaxHunger;
    }
}
