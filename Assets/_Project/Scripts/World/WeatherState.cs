namespace VoxelRPG.World
{
    /// <summary>
    /// Possible weather conditions in the game world.
    /// </summary>
    public enum WeatherState
    {
        /// <summary>
        /// Clear skies, no weather effects.
        /// </summary>
        Clear,

        /// <summary>
        /// Light clouds, slightly reduced visibility.
        /// </summary>
        Cloudy,

        /// <summary>
        /// Light rain with minor visibility reduction.
        /// </summary>
        LightRain,

        /// <summary>
        /// Heavy rain with significant visibility reduction.
        /// </summary>
        HeavyRain,

        /// <summary>
        /// Thunderstorm with rain, lightning, and poor visibility.
        /// </summary>
        Thunderstorm,

        /// <summary>
        /// Fog with severely reduced visibility.
        /// </summary>
        Fog,

        /// <summary>
        /// Light snow (biome-dependent).
        /// </summary>
        LightSnow,

        /// <summary>
        /// Heavy snowfall/blizzard (biome-dependent).
        /// </summary>
        Blizzard
    }
}
