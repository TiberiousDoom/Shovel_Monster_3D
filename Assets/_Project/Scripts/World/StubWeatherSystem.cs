using System;
using UnityEngine;
using VoxelRPG.Core;

namespace VoxelRPG.World
{
    /// <summary>
    /// Stub implementation of IWeatherSystem for Phase 1.
    /// Always returns clear weather with full visibility.
    /// Will be replaced with full implementation in Phase 5.
    /// </summary>
    public class StubWeatherSystem : MonoBehaviour, IWeatherSystem
    {
        [Header("Debug Override")]
        [Tooltip("Enable to override weather for testing")]
        [SerializeField] private bool _useDebugWeather;

        [Tooltip("Weather state when debug is enabled")]
        [SerializeField] private WeatherState _debugWeather = WeatherState.Clear;

        [Range(0f, 1f)]
        [Tooltip("Visibility when debug is enabled")]
        [SerializeField] private float _debugVisibility = 1f;

        private WeatherState _currentWeather = WeatherState.Clear;
        private WeatherState _previousWeather = WeatherState.Clear;
        private bool _isLocked;

        /// <inheritdoc/>
        public WeatherState CurrentWeather => _useDebugWeather ? _debugWeather : _currentWeather;

        /// <inheritdoc/>
        public WeatherState PreviousWeather => _previousWeather;

        /// <inheritdoc/>
        public float Visibility => _useDebugWeather ? _debugVisibility : 1f;

        /// <inheritdoc/>
        public float Intensity => 0f;

        /// <inheritdoc/>
        public bool IsPrecipitating => false;

        /// <inheritdoc/>
        public float WindStrength => 0f;

        /// <inheritdoc/>
        public Vector3 WindDirection => Vector3.right;

        /// <inheritdoc/>
        public event Action<WeatherState> OnWeatherChanged;

        /// <inheritdoc/>
        public event Action<float> OnVisibilityChanged;

        private void Awake()
        {
            ServiceLocator.Register<IWeatherSystem>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IWeatherSystem>();
        }

        private void Start()
        {
            Debug.Log("[StubWeatherSystem] Weather system stub active. Always clear weather. Full implementation in Phase 5.");
        }

        /// <inheritdoc/>
        public void SetWeather(WeatherState weather, float transitionTime = 5f)
        {
            if (_isLocked)
            {
                Debug.Log($"[StubWeatherSystem] Weather locked, ignoring request to set {weather}");
                return;
            }

            if (_useDebugWeather)
            {
                _previousWeather = _debugWeather;
                _debugWeather = weather;
            }
            else
            {
                _previousWeather = _currentWeather;
                _currentWeather = weather;
            }

            Debug.Log($"[StubWeatherSystem] Weather set to {weather} (stub - no visual effects)");
            OnWeatherChanged?.Invoke(CurrentWeather);
        }

        /// <inheritdoc/>
        public void SetWeatherLocked(bool locked)
        {
            _isLocked = locked;
            Debug.Log($"[StubWeatherSystem] Weather lock: {locked}");
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_useDebugWeather && Application.isPlaying)
            {
                OnWeatherChanged?.Invoke(_debugWeather);
                OnVisibilityChanged?.Invoke(_debugVisibility);
            }
        }
#endif
    }
}
