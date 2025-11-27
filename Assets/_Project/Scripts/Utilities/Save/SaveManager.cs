using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VoxelRPG.Utilities.Save
{
    /// <summary>
    /// Manages save file operations including saving, loading, and listing saves.
    /// Phase 0A implementation - basic JSON serialization.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private string _saveFolder = "Saves";
        [SerializeField] private string _saveExtension = ".sav";

        private readonly Dictionary<string, ISaveable> _saveables = new Dictionary<string, ISaveable>();
        private SaveFileHeader _currentHeader;
        private float _sessionStartTime;

        /// <summary>
        /// Currently loaded save header, or null if no save loaded.
        /// </summary>
        public SaveFileHeader CurrentHeader => _currentHeader;

        /// <summary>
        /// Full path to the saves directory.
        /// </summary>
        public string SaveDirectory => Path.Combine(Application.persistentDataPath, _saveFolder);

        /// <summary>
        /// Event raised before saving begins.
        /// </summary>
        public event Action OnBeforeSave;

        /// <summary>
        /// Event raised after saving completes.
        /// </summary>
        public event Action OnAfterSave;

        /// <summary>
        /// Event raised before loading begins.
        /// </summary>
        public event Action OnBeforeLoad;

        /// <summary>
        /// Event raised after loading completes.
        /// </summary>
        public event Action OnAfterLoad;

        private void Awake()
        {
            EnsureSaveDirectoryExists();
            _sessionStartTime = Time.realtimeSinceStartup;
        }

        private void EnsureSaveDirectoryExists()
        {
            if (!Directory.Exists(SaveDirectory))
            {
                Directory.CreateDirectory(SaveDirectory);
            }
        }

        /// <summary>
        /// Registers a saveable object for serialization.
        /// </summary>
        /// <param name="saveable">The saveable to register.</param>
        public void Register(ISaveable saveable)
        {
            if (saveable == null)
            {
                Debug.LogWarning("[SaveManager] Attempted to register null saveable.");
                return;
            }

            if (_saveables.ContainsKey(saveable.SaveId))
            {
                Debug.LogWarning($"[SaveManager] Saveable with ID '{saveable.SaveId}' already registered.");
                return;
            }

            _saveables[saveable.SaveId] = saveable;
        }

        /// <summary>
        /// Unregisters a saveable object.
        /// </summary>
        /// <param name="saveable">The saveable to unregister.</param>
        public void Unregister(ISaveable saveable)
        {
            if (saveable != null)
            {
                _saveables.Remove(saveable.SaveId);
            }
        }

        /// <summary>
        /// Saves the current game state to a file.
        /// </summary>
        /// <param name="saveName">Name for the save file.</param>
        /// <returns>True if successful.</returns>
        public bool Save(string saveName)
        {
            try
            {
                OnBeforeSave?.Invoke();

                // Update or create header
                if (_currentHeader == null)
                {
                    _currentHeader = SaveFileHeader.Create(saveName, Application.version);
                }
                else
                {
                    _currentHeader.SaveName = saveName;
                    _currentHeader.UpdateModifiedTime();
                }

                // Update play time
                _currentHeader.PlayTimeSeconds += Time.realtimeSinceStartup - _sessionStartTime;
                _sessionStartTime = Time.realtimeSinceStartup;

                // Capture all saveable states
                var saveData = new SaveData
                {
                    Header = _currentHeader,
                    States = new Dictionary<string, string>()
                };

                foreach (var kvp in _saveables)
                {
                    var state = kvp.Value.CaptureState();
                    if (state != null)
                    {
                        saveData.States[kvp.Key] = JsonUtility.ToJson(state);
                    }
                }

                // Write to file
                var filePath = GetSaveFilePath(saveName);
                var json = JsonUtility.ToJson(saveData, true);
                File.WriteAllText(filePath, json);

                OnAfterSave?.Invoke();

                Debug.Log($"[SaveManager] Game saved to: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Save failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads a save file.
        /// </summary>
        /// <param name="saveName">Name of the save to load.</param>
        /// <returns>True if successful.</returns>
        public bool Load(string saveName)
        {
            try
            {
                var filePath = GetSaveFilePath(saveName);

                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[SaveManager] Save file not found: {filePath}");
                    return false;
                }

                OnBeforeLoad?.Invoke();

                var json = File.ReadAllText(filePath);
                var saveData = JsonUtility.FromJson<SaveData>(json);

                if (saveData?.Header == null || !saveData.Header.IsValid())
                {
                    Debug.LogError("[SaveManager] Invalid save file format.");
                    return false;
                }

                if (!saveData.Header.IsCompatible())
                {
                    Debug.LogError($"[SaveManager] Save file version {saveData.Header.Version} is not compatible.");
                    return false;
                }

                _currentHeader = saveData.Header;
                _sessionStartTime = Time.realtimeSinceStartup;

                // Restore all saveable states
                foreach (var kvp in saveData.States)
                {
                    if (_saveables.TryGetValue(kvp.Key, out var saveable))
                    {
                        saveable.RestoreState(kvp.Value);
                    }
                    else
                    {
                        Debug.LogWarning($"[SaveManager] No saveable registered for ID: {kvp.Key}");
                    }
                }

                OnAfterLoad?.Invoke();

                Debug.Log($"[SaveManager] Game loaded from: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Load failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the header for a save file without loading it.
        /// </summary>
        /// <param name="saveName">Name of the save.</param>
        /// <returns>The header, or null if invalid.</returns>
        public SaveFileHeader GetSaveHeader(string saveName)
        {
            try
            {
                var filePath = GetSaveFilePath(saveName);

                if (!File.Exists(filePath))
                {
                    return null;
                }

                var json = File.ReadAllText(filePath);
                var saveData = JsonUtility.FromJson<SaveData>(json);

                return saveData?.Header;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets list of all save files.
        /// </summary>
        /// <returns>Array of save file names (without extension).</returns>
        public string[] GetSaveFiles()
        {
            EnsureSaveDirectoryExists();

            var files = Directory.GetFiles(SaveDirectory, $"*{_saveExtension}");
            var names = new string[files.Length];

            for (int i = 0; i < files.Length; i++)
            {
                names[i] = Path.GetFileNameWithoutExtension(files[i]);
            }

            return names;
        }

        /// <summary>
        /// Deletes a save file.
        /// </summary>
        /// <param name="saveName">Name of the save to delete.</param>
        /// <returns>True if deleted.</returns>
        public bool DeleteSave(string saveName)
        {
            var filePath = GetSaveFilePath(saveName);

            if (!File.Exists(filePath))
            {
                return false;
            }

            File.Delete(filePath);
            Debug.Log($"[SaveManager] Deleted save: {saveName}");
            return true;
        }

        /// <summary>
        /// Checks if a save file exists.
        /// </summary>
        /// <param name="saveName">Name of the save.</param>
        /// <returns>True if exists.</returns>
        public bool SaveExists(string saveName)
        {
            return File.Exists(GetSaveFilePath(saveName));
        }

        private string GetSaveFilePath(string saveName)
        {
            // Sanitize save name to prevent path traversal
            var sanitized = string.Join("_", saveName.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(SaveDirectory, sanitized + _saveExtension);
        }

        /// <summary>
        /// Internal save data structure.
        /// </summary>
        [Serializable]
        private class SaveData
        {
            public SaveFileHeader Header;
            public Dictionary<string, string> States;
        }
    }
}
