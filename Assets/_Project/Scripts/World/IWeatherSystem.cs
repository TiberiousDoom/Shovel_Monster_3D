using System;

namespace VoxelRPG.World
{
    /// <summary>
    /// Interface for weather system.
    /// Stub in Phase 1.5, fully implemented in Phase 5.
    /// Weather affects NPC behavior, monster spawning, and visibility.
    /// </summary>
    public interface IWeatherSystem
    {
        /// <summary>
        /// Current weather condition.
        /// </summary>
        WeatherState CurrentWeather { get; }

        /// <summary>
        /// Previous weather (for transition effects).
        /// </summary>
        WeatherState PreviousWeather { get; }

        /// <summary>
        /// Visibility factor (0-1).
        /// 1 = full visibility, 0 = no visibility.
        /// Affects NPC/monster behavior and rendering distance.
        /// </summary>
        float Visibility { get; }

        /// <summary>
        /// Weather intensity (0-1).
        /// Used for particle effects and audio levels.
        /// </summary>
        float Intensity { get; }

        /// <summary>
        /// Whether it's currently precipitating (rain or snow).
        /// </summary>
        bool IsPrecipitating { get; }

        /// <summary>
        /// Wind strength (0-1).
        /// Affects vegetation movement and projectiles.
        /// </summary>
        float WindStrength { get; }

        /// <summary>
        /// Wind direction (normalized).
        /// </summary>
        UnityEngine.Vector3 WindDirection { get; }

        /// <summary>
        /// Event fired when weather changes.
        /// </summary>
        event Action<WeatherState> OnWeatherChanged;

        /// <summary>
        /// Event fired when visibility changes significantly.
        /// </summary>
        event Action<float> OnVisibilityChanged;

        /// <summary>
        /// Forces a specific weather state (for debugging or quests).
        /// </summary>
        /// <param name="weather">Weather to set.</param>
        /// <param name="transitionTime">Time to transition to new weather.</param>
        void SetWeather(WeatherState weather, float transitionTime = 5f);

        /// <summary>
        /// Enables/disables weather transitions.
        /// </summary>
        void SetWeatherLocked(bool locked);
    }
}
