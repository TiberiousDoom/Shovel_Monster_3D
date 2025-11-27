using System;

namespace VoxelRPG.Utilities.Save
{
    /// <summary>
    /// Header information stored at the beginning of save files.
    /// Contains metadata for version compatibility and save management.
    /// </summary>
    [Serializable]
    public class SaveFileHeader
    {
        /// <summary>
        /// Magic number to identify valid save files.
        /// </summary>
        public const string MAGIC = "VRPG";

        /// <summary>
        /// Current save format version.
        /// Increment when making breaking changes to save format.
        /// </summary>
        public const int CURRENT_VERSION = 1;

        /// <summary>
        /// Magic identifier to validate file type.
        /// </summary>
        public string Magic;

        /// <summary>
        /// Save format version for migration support.
        /// </summary>
        public int Version;

        /// <summary>
        /// Display name for the save slot.
        /// </summary>
        public string SaveName;

        /// <summary>
        /// UTC timestamp when save was created.
        /// </summary>
        public long CreatedTimestamp;

        /// <summary>
        /// UTC timestamp when save was last modified.
        /// </summary>
        public long ModifiedTimestamp;

        /// <summary>
        /// Total play time in seconds for this save.
        /// </summary>
        public float PlayTimeSeconds;

        /// <summary>
        /// Game version string when save was created.
        /// </summary>
        public string GameVersion;

        /// <summary>
        /// Creates a new save header with current values.
        /// </summary>
        /// <param name="saveName">Display name for the save.</param>
        /// <param name="gameVersion">Current game version string.</param>
        /// <returns>New SaveFileHeader instance.</returns>
        public static SaveFileHeader Create(string saveName, string gameVersion)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            return new SaveFileHeader
            {
                Magic = MAGIC,
                Version = CURRENT_VERSION,
                SaveName = saveName,
                CreatedTimestamp = now,
                ModifiedTimestamp = now,
                PlayTimeSeconds = 0f,
                GameVersion = gameVersion
            };
        }

        /// <summary>
        /// Validates that this header is from a valid save file.
        /// </summary>
        /// <returns>True if valid.</returns>
        public bool IsValid()
        {
            return Magic == MAGIC;
        }

        /// <summary>
        /// Checks if this save can be loaded by the current version.
        /// </summary>
        /// <returns>True if compatible.</returns>
        public bool IsCompatible()
        {
            // For now, only exact version match is supported
            // Future: implement migration for older versions
            return IsValid() && Version <= CURRENT_VERSION;
        }

        /// <summary>
        /// Updates the modified timestamp to now.
        /// </summary>
        public void UpdateModifiedTime()
        {
            ModifiedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        /// <summary>
        /// Gets the created date as DateTime.
        /// </summary>
        public DateTime GetCreatedDate()
        {
            return DateTimeOffset.FromUnixTimeSeconds(CreatedTimestamp).LocalDateTime;
        }

        /// <summary>
        /// Gets the modified date as DateTime.
        /// </summary>
        public DateTime GetModifiedDate()
        {
            return DateTimeOffset.FromUnixTimeSeconds(ModifiedTimestamp).LocalDateTime;
        }

        /// <summary>
        /// Gets formatted play time string (HH:MM:SS).
        /// </summary>
        public string GetFormattedPlayTime()
        {
            var timeSpan = TimeSpan.FromSeconds(PlayTimeSeconds);
            return $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }
    }
}
