using UnityEngine;

namespace VoxelRPG.Core
{
    /// <summary>
    /// Controls the directional light to simulate sun/moon movement.
    /// Rotates light based on TimeManager's time of day.
    /// </summary>
    public class SunController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The directional light representing the sun")]
        [SerializeField] private Light _sunLight;

        [Header("Rotation Settings")]
        [Tooltip("East direction (where sun rises)")]
        [SerializeField] private Vector3 _sunriseDirection = new Vector3(1f, 0f, 0f);

        [Tooltip("Maximum sun elevation angle at noon")]
        [SerializeField] private float _maxElevation = 80f;

        [Header("Lighting Settings")]
        [Tooltip("Sun color during full day")]
        [SerializeField] private Color _dayColor = new Color(1f, 0.95f, 0.85f);

        [Tooltip("Sun color during dawn/dusk")]
        [SerializeField] private Color _sunriseColor = new Color(1f, 0.6f, 0.4f);

        [Tooltip("Moon color during night")]
        [SerializeField] private Color _nightColor = new Color(0.4f, 0.5f, 0.7f);

        [Header("Intensity Settings")]
        [Tooltip("Sun intensity during day")]
        [SerializeField] private float _dayIntensity = 1.2f;

        [Tooltip("Sun intensity during dawn/dusk")]
        [SerializeField] private float _transitionIntensity = 0.6f;

        [Tooltip("Moon intensity during night")]
        [SerializeField] private float _nightIntensity = 0.2f;

        [Header("Ambient Light")]
        [Tooltip("Ambient color during day")]
        [SerializeField] private Color _dayAmbient = new Color(0.4f, 0.45f, 0.5f);

        [Tooltip("Ambient color during night")]
        [SerializeField] private Color _nightAmbient = new Color(0.05f, 0.05f, 0.1f);

        private TimeManager _timeManager;

        private void Start()
        {
            if (_sunLight == null)
            {
                _sunLight = GetComponent<Light>();
            }

            if (_sunLight == null)
            {
                Debug.LogError("[SunController] No directional light assigned or found!");
                enabled = false;
                return;
            }

            ServiceLocator.TryGet(out _timeManager);

            if (_timeManager == null)
            {
                Debug.LogWarning("[SunController] TimeManager not found. Sun will not update.");
            }
        }

        private void Update()
        {
            if (_timeManager == null)
            {
                ServiceLocator.TryGet(out _timeManager);
                if (_timeManager == null) return;
            }

            UpdateSunPosition();
            UpdateLighting();
        }

        private void UpdateSunPosition()
        {
            float timeOfDay = _timeManager.TimeOfDay;

            // Calculate sun position
            // Time 0.25 = sunrise (east), 0.5 = noon (overhead), 0.75 = sunset (west)
            float sunAngle = (timeOfDay - 0.25f) * 360f; // Degrees from sunrise

            // Create rotation
            Quaternion baseRotation = Quaternion.LookRotation(_sunriseDirection);
            Quaternion sunRotation = Quaternion.Euler(sunAngle, 0f, 0f) * baseRotation;

            // Apply elevation curve (higher at noon)
            float elevationFactor = Mathf.Sin((timeOfDay - 0.25f) * Mathf.PI * 2f);
            float elevation = Mathf.Lerp(0f, _maxElevation, Mathf.Clamp01(elevationFactor));

            // Calculate final rotation looking down from the sun's position
            float yaw = (timeOfDay - 0.25f) * 360f;
            _sunLight.transform.rotation = Quaternion.Euler(elevation, yaw, 0f);
        }

        private void UpdateLighting()
        {
            var phase = _timeManager.CurrentPhase;
            float timeOfDay = _timeManager.TimeOfDay;

            Color targetColor;
            float targetIntensity;
            Color targetAmbient;

            switch (phase)
            {
                case TimePhase.Dawn:
                    float dawnProgress = GetPhaseProgress(timeOfDay, 0.20f, 0.25f);
                    targetColor = Color.Lerp(_nightColor, _sunriseColor, dawnProgress);
                    targetIntensity = Mathf.Lerp(_nightIntensity, _transitionIntensity, dawnProgress);
                    targetAmbient = Color.Lerp(_nightAmbient, _dayAmbient, dawnProgress * 0.5f);
                    break;

                case TimePhase.Day:
                    float dayProgress = GetPhaseProgress(timeOfDay, 0.25f, 0.70f);
                    // Blend from sunrise to full day at start, back to sunset at end
                    if (dayProgress < 0.1f)
                    {
                        float earlyDayBlend = dayProgress / 0.1f;
                        targetColor = Color.Lerp(_sunriseColor, _dayColor, earlyDayBlend);
                        targetIntensity = Mathf.Lerp(_transitionIntensity, _dayIntensity, earlyDayBlend);
                    }
                    else if (dayProgress > 0.9f)
                    {
                        float lateDayBlend = (dayProgress - 0.9f) / 0.1f;
                        targetColor = Color.Lerp(_dayColor, _sunriseColor, lateDayBlend);
                        targetIntensity = Mathf.Lerp(_dayIntensity, _transitionIntensity, lateDayBlend);
                    }
                    else
                    {
                        targetColor = _dayColor;
                        targetIntensity = _dayIntensity;
                    }
                    targetAmbient = _dayAmbient;
                    break;

                case TimePhase.Dusk:
                    float duskProgress = GetPhaseProgress(timeOfDay, 0.70f, 0.75f);
                    targetColor = Color.Lerp(_sunriseColor, _nightColor, duskProgress);
                    targetIntensity = Mathf.Lerp(_transitionIntensity, _nightIntensity, duskProgress);
                    targetAmbient = Color.Lerp(_dayAmbient, _nightAmbient, duskProgress);
                    break;

                case TimePhase.Night:
                default:
                    targetColor = _nightColor;
                    targetIntensity = _nightIntensity;
                    targetAmbient = _nightAmbient;
                    break;
            }

            // Apply smooth transitions
            _sunLight.color = Color.Lerp(_sunLight.color, targetColor, Time.deltaTime * 2f);
            _sunLight.intensity = Mathf.Lerp(_sunLight.intensity, targetIntensity, Time.deltaTime * 2f);
            RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, targetAmbient, Time.deltaTime * 2f);
        }

        private float GetPhaseProgress(float timeOfDay, float start, float end)
        {
            if (end < start) end += 1f; // Handle wrap around midnight
            if (timeOfDay < start) timeOfDay += 1f;
            return Mathf.Clamp01((timeOfDay - start) / (end - start));
        }

        /// <summary>
        /// Gets current sun direction for shadow calculations.
        /// </summary>
        public Vector3 GetSunDirection()
        {
            return _sunLight != null ? -_sunLight.transform.forward : Vector3.down;
        }

        /// <summary>
        /// Returns true if the sun is currently below the horizon.
        /// </summary>
        public bool IsSunBelowHorizon()
        {
            if (_sunLight == null) return false;
            return Vector3.Dot(_sunLight.transform.forward, Vector3.up) > 0;
        }

#if UNITY_EDITOR
        [Header("Editor Debug")]
        [SerializeField] private bool _previewTimeOfDay;
        [Range(0f, 1f)]
        [SerializeField] private float _previewTime = 0.5f;

        private void OnValidate()
        {
            if (_previewTimeOfDay && _sunLight != null)
            {
                // Preview sun position in editor
                float sunAngle = (_previewTime - 0.25f) * 360f;
                float elevationFactor = Mathf.Sin((_previewTime - 0.25f) * Mathf.PI * 2f);
                float elevation = Mathf.Lerp(0f, _maxElevation, Mathf.Clamp01(elevationFactor));
                float yaw = (_previewTime - 0.25f) * 360f;
                _sunLight.transform.rotation = Quaternion.Euler(elevation, yaw, 0f);
            }
        }
#endif
    }
}
