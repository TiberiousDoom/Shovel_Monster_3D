using System;
using UnityEngine;

namespace VoxelRPG.Core
{
    /// <summary>
    /// Manages game time, day/night cycle, and time-related events.
    /// Designed to be server-authoritative for multiplayer.
    /// </summary>
    public class TimeManager : MonoBehaviour
    {
        [Header("Time Settings")]
        [Tooltip("Length of one full day in real-world seconds")]
        [SerializeField] private float _dayLengthInSeconds = 600f; // 10 minutes per day

        [Tooltip("Starting time of day (0-1, where 0.25 is sunrise, 0.75 is sunset)")]
        [SerializeField] private float _startingTimeOfDay = 0.25f; // Start at sunrise

        [Header("Day/Night Thresholds")]
        [Tooltip("Time when dawn begins (0-1)")]
        [SerializeField] private float _dawnStart = 0.20f;

        [Tooltip("Time when day begins (0-1)")]
        [SerializeField] private float _dayStart = 0.25f;

        [Tooltip("Time when dusk begins (0-1)")]
        [SerializeField] private float _duskStart = 0.70f;

        [Tooltip("Time when night begins (0-1)")]
        [SerializeField] private float _nightStart = 0.75f;

        [Header("Debug")]
        [SerializeField] private bool _pauseTime;
        [SerializeField] private float _timeScale = 1f;

        private float _currentTimeOfDay;
        private int _currentDay;
        private TimePhase _currentPhase;
        private TimePhase _previousPhase;

        /// <summary>
        /// Current time of day normalized (0-1).
        /// 0.0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset
        /// </summary>
        public float TimeOfDay => _currentTimeOfDay;

        /// <summary>
        /// Current day number (starts at 1).
        /// </summary>
        public int CurrentDay => _currentDay;

        /// <summary>
        /// Current time phase (Dawn, Day, Dusk, Night).
        /// </summary>
        public TimePhase CurrentPhase => _currentPhase;

        /// <summary>
        /// Whether it's currently nighttime.
        /// </summary>
        public bool IsNight => _currentPhase == TimePhase.Night;

        /// <summary>
        /// Whether it's currently daytime.
        /// </summary>
        public bool IsDay => _currentPhase == TimePhase.Day;

        /// <summary>
        /// Current hour in 24-hour format (0-23).
        /// </summary>
        public int CurrentHour => Mathf.FloorToInt(_currentTimeOfDay * 24f);

        /// <summary>
        /// Current minute (0-59).
        /// </summary>
        public int CurrentMinute => Mathf.FloorToInt((_currentTimeOfDay * 24f % 1f) * 60f);

        /// <summary>
        /// Event fired when a new day starts.
        /// </summary>
        public event Action<int> OnNewDay;

        /// <summary>
        /// Event fired when time phase changes (Dawn, Day, Dusk, Night).
        /// </summary>
        public event Action<TimePhase> OnPhaseChanged;

        /// <summary>
        /// Event fired when night begins. Useful for monster spawning.
        /// </summary>
        public event Action OnNightStarted;

        /// <summary>
        /// Event fired when day begins. Useful for monster despawning.
        /// </summary>
        public event Action OnDayStarted;

        /// <summary>
        /// Event fired every in-game hour.
        /// </summary>
        public event Action<int> OnHourChanged;

        private int _lastHour = -1;

        private void Awake()
        {
            ServiceLocator.Register<TimeManager>(this);
        }

        private void Start()
        {
            _currentTimeOfDay = _startingTimeOfDay;
            _currentDay = 1;
            _currentPhase = GetPhaseForTime(_currentTimeOfDay);
            _previousPhase = _currentPhase;
            _lastHour = CurrentHour;

            Debug.Log($"[TimeManager] Starting Day {_currentDay} at {CurrentHour:00}:{CurrentMinute:00} ({_currentPhase})");
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<TimeManager>();
        }

        private void Update()
        {
            if (_pauseTime) return;

            AdvanceTime(Time.deltaTime * _timeScale);
        }

        private void AdvanceTime(float deltaTime)
        {
            float timeAdvance = deltaTime / _dayLengthInSeconds;
            _currentTimeOfDay += timeAdvance;

            // Check for day rollover
            if (_currentTimeOfDay >= 1f)
            {
                _currentTimeOfDay -= 1f;
                _currentDay++;
                OnNewDay?.Invoke(_currentDay);
                Debug.Log($"[TimeManager] Day {_currentDay} has begun!");
            }

            // Check for phase change
            _currentPhase = GetPhaseForTime(_currentTimeOfDay);
            if (_currentPhase != _previousPhase)
            {
                OnPhaseChanged?.Invoke(_currentPhase);

                if (_currentPhase == TimePhase.Night)
                {
                    OnNightStarted?.Invoke();
                    Debug.Log("[TimeManager] Night has fallen...");
                }
                else if (_currentPhase == TimePhase.Day)
                {
                    OnDayStarted?.Invoke();
                    Debug.Log("[TimeManager] A new day dawns.");
                }

                _previousPhase = _currentPhase;
            }

            // Check for hour change
            int currentHour = CurrentHour;
            if (currentHour != _lastHour)
            {
                _lastHour = currentHour;
                OnHourChanged?.Invoke(currentHour);
            }
        }

        private TimePhase GetPhaseForTime(float time)
        {
            if (time >= _dawnStart && time < _dayStart)
                return TimePhase.Dawn;
            if (time >= _dayStart && time < _duskStart)
                return TimePhase.Day;
            if (time >= _duskStart && time < _nightStart)
                return TimePhase.Dusk;
            return TimePhase.Night;
        }

        /// <summary>
        /// Sets the current time of day. Useful for debugging or save/load.
        /// </summary>
        /// <param name="timeOfDay">Normalized time (0-1).</param>
        public void SetTimeOfDay(float timeOfDay)
        {
            _currentTimeOfDay = Mathf.Clamp01(timeOfDay);
            _currentPhase = GetPhaseForTime(_currentTimeOfDay);
            _previousPhase = _currentPhase;
            _lastHour = CurrentHour;
        }

        /// <summary>
        /// Sets the current day number.
        /// </summary>
        /// <param name="day">Day number (1+).</param>
        public void SetDay(int day)
        {
            _currentDay = Mathf.Max(1, day);
        }

        /// <summary>
        /// Pauses or resumes time progression.
        /// </summary>
        public void SetPaused(bool paused)
        {
            _pauseTime = paused;
        }

        /// <summary>
        /// Sets the time scale multiplier.
        /// </summary>
        public void SetTimeScale(float scale)
        {
            _timeScale = Mathf.Max(0f, scale);
        }

        /// <summary>
        /// Skips to the next phase transition.
        /// </summary>
        public void SkipToNextPhase()
        {
            switch (_currentPhase)
            {
                case TimePhase.Night:
                    SetTimeOfDay(_dawnStart);
                    break;
                case TimePhase.Dawn:
                    SetTimeOfDay(_dayStart);
                    break;
                case TimePhase.Day:
                    SetTimeOfDay(_duskStart);
                    break;
                case TimePhase.Dusk:
                    SetTimeOfDay(_nightStart);
                    break;
            }
        }

        /// <summary>
        /// Gets sun angle for lighting calculations.
        /// Returns 0 at midnight, 90 at noon, 180 at midnight again.
        /// </summary>
        public float GetSunAngle()
        {
            // Map time to sun angle: 0.25 (sunrise) = 0°, 0.5 (noon) = 90°, 0.75 (sunset) = 180°
            float dayProgress = (_currentTimeOfDay - _dayStart) / (_nightStart - _dayStart);
            if (dayProgress < 0f || dayProgress > 1f)
            {
                // Night time - sun below horizon
                return dayProgress < 0f ? dayProgress * 180f : 180f + (dayProgress - 1f) * 180f;
            }
            return dayProgress * 180f;
        }

        /// <summary>
        /// Gets save data for the time system.
        /// </summary>
        public TimeSaveData GetSaveData()
        {
            return new TimeSaveData
            {
                TimeOfDay = _currentTimeOfDay,
                CurrentDay = _currentDay
            };
        }

        /// <summary>
        /// Loads time data from save.
        /// </summary>
        public void LoadSaveData(TimeSaveData data)
        {
            SetDay(data.CurrentDay);
            SetTimeOfDay(data.TimeOfDay);
        }

        /// <summary>
        /// Formats current time as HH:MM string.
        /// </summary>
        public string GetFormattedTime()
        {
            return $"{CurrentHour:00}:{CurrentMinute:00}";
        }
    }

    /// <summary>
    /// Time phases for day/night cycle.
    /// </summary>
    public enum TimePhase
    {
        Night,  // Full darkness
        Dawn,   // Sunrise transition
        Day,    // Full daylight
        Dusk    // Sunset transition
    }

    /// <summary>
    /// Serializable save data for time system.
    /// </summary>
    [Serializable]
    public class TimeSaveData
    {
        public float TimeOfDay;
        public int CurrentDay;
    }
}
